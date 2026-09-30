using System.Text.Json;
using System.Text.RegularExpressions;
using AgentInfrastructure.Clients;
using AgentInfrastructure.Entities;
using AgentInfrastructure.Repositories;
using AgentInfrastructure.Tools;

namespace AgentCore.Services;

public class AgentService : IAgentService
{
    private readonly IAgentRepository _repo;
    private readonly IClaudeClient _claude;
    private readonly ISpecService _spec;
    private readonly IUsageService _usage;
    private readonly IProjectService _project;
    private readonly IToolService _tools;
    private readonly IAgentLoader _loader;

    public AgentService(IAgentRepository repo, IClaudeClient claude, ISpecService spec, IUsageService usage, IProjectService project, IToolService tools, IAgentLoader loader)
    {
        _repo = repo;
        _claude = claude;
        _spec = spec;
        _usage = usage;
        _project = project;
        _tools = tools;
        _loader = loader;
    }

    // ── Agentic loop ────────────────────────────────────────────────────────
    // Calls Claude with tools, executes tool calls, loops until end_turn.
    // Returns (finalText, totalInput, totalOutput)
    private async Task<(string text, int inputTokens, int outputTokens)> RunAgenticLoop(
        List<object> messages, string systemPrompt)
    {
        var tools = _tools.GetDefinitions();
        var totalInput = 0;
        var totalOutput = 0;
        const int maxIterations = 5;

        for (int i = 0; i < maxIterations; i++)
        {
            var result = await _claude.ChatWithTools(messages, systemPrompt, tools);
            totalInput += result.InputTokens;
            totalOutput += result.OutputTokens;

            if (result.StopReason != "tool_use" || result.ToolCalls.Count == 0)
                return (result.Text ?? "", totalInput, totalOutput);

            // Add assistant message (with tool_use blocks) to history
            messages.Add(new { role = "assistant", content = result.RawContentBlocks });

            // Execute each tool and collect results
            var toolResults = new List<object>();
            foreach (var call in result.ToolCalls)
            {
                var output = await _tools.Execute(call.Name, call.Input);
                toolResults.Add(new
                {
                    type = "tool_result",
                    tool_use_id = call.Id,
                    content = output
                });
            }

            // Add tool results as user message
            messages.Add(new { role = "user", content = toolResults });
        }

        return ("Đã vượt quá số lần gọi tool cho phép.", totalInput, totalOutput);
    }

    public Task<AgentSession> CreateSession(string title, Guid userId, string gitUser = "")
        => _repo.CreateSession(title, userId, gitUser);

    public async Task<string> Chat(Guid sessionId, string message, Guid userId, Guid? specId = null, Guid? projectId = null, List<string>? selectedModules = null, string? agentType = null, string? projectKey = null, string gitUser = "")
    {
        if (await _usage.IsOverWeeklyLimit(userId))
            throw new InvalidOperationException("Weekly token limit exceeded.");

        await _repo.AddMessage(sessionId, "user", message);

        // Build project context prefix for BA prompt
        var projectContext = string.Empty;
        if (projectId.HasValue)
        {
            var proj = await _project.GetById(projectId.Value);
            if (proj?.ProfileJson != null)
            {
                projectContext = $"\n\n{_project.BuildProfilePrompt(proj.ProfileJson)}";

                // Inject selected module names + descriptions so BA understands scope
                if (selectedModules is { Count: > 0 })
                {
                    var modLines = await BuildSelectedModuleContext(projectId.Value, userId, selectedModules);
                    projectContext += $"\n\n## MODULES ĐANG LÀM VIỆC\n{modLines}";
                }
            }
        }

        var latestVersion = specId.HasValue
            ? await _spec.GetVersionById(specId.Value)
            : await _spec.GetLatestVersion(sessionId);

        // If user references a specific version ("v1", "v2"...), load that version for context
        var referencedVersion = await ResolveReferencedVersion(message, latestVersion);
        var contextVersion = referencedVersion ?? latestVersion;

        var intent = DetectIntent(message, contextVersion != null);
        string context;
        string? lastAssistantContent = null;
        var chatMessages = new List<AgentMessage>();

        if (intent == Intent.Update)
        {
            context = $@"MODE: REFINEMENT
Spec summary: {BuildSpecSummary(contextVersion!.JsonSpec)}
Full spec (minified): {MinifyJson(contextVersion!.JsonSpec)}
User request: {message}
RULES: No questions. Patch only changed fields (no feature_name). New screen → full spec with new feature_name.";
        }
        else if (intent == Intent.Chat && contextVersion != null)
        {
            var versionLabel = referencedVersion != null ? $"v{referencedVersion.Version}" : "current";
            var specContext = NeedsFullSpec(message)
                ? $"Spec ({versionLabel}):\n```json\n{contextVersion.JsonSpec}\n```"
                : $"Spec ({versionLabel}) summary: {BuildSpecSummary(contextVersion.JsonSpec)}";
            context = $"MODE: CHAT\n{specContext}\nUser message: {message}";

            // Include last few messages for conversational context
            var history = await _repo.GetMessages(sessionId);
            foreach (var m in history.TakeLast(6).SkipLast(1)) // exclude the message we just added
                chatMessages.Add(m);
        }
        else
        {
            // Discovery: new spec or no spec yet
            context = message;
            var history = await _repo.GetMessages(sessionId);
            var lastAssistant = history
                .Where(m => m.Role == "assistant")
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefault();
            if (lastAssistant != null)
            {
                chatMessages.Add(lastAssistant);
                lastAssistantContent = lastAssistant.Content;
            }
        }

        // Build messages list for tool-capable agentic loop
        var agentMessages = new List<object>();
        foreach (var m in chatMessages.Where(m => m.Role == "user" || m.Role == "assistant"))
            agentMessages.Add(new { role = m.Role, content = m.Content });

        // Add current user message (with context injected)
        agentMessages.Add(new { role = "user", content = context });

        // Build full system prompt
        string systemPromptFull;
        if (!string.IsNullOrWhiteSpace(agentType))
        {
            systemPromptFull = await _loader.BuildSystemPrompt(agentType, projectKey);
            if (projectContext.Length > 0) systemPromptFull += projectContext;
        }
        else
        {
            systemPromptFull = LoadSystemPromptText();
            if (projectContext.Length > 0) systemPromptFull += projectContext;
        }

        var (responseText, inputTokens, outputTokens) = await RunAgenticLoop(agentMessages, systemPromptFull);
        await _repo.AddMessage(sessionId, "assistant", responseText);

        // Module: từ message ([SCREEN_DETAIL]) → từ version đang chọn → từ screen_list trong history → fallback "General"
        var moduleName = ExtractModuleFromMessage(message)
            ?? latestVersion?.Task?.Spec?.Name
            ?? ExtractModuleFromScreenList(lastAssistantContent)
            ?? "General";

        if (intent == Intent.Update && latestVersion != null)
        {
            await _spec.TryMergeAndSave(sessionId, latestVersion, responseText);
        }
        else if (intent == Intent.Chat)
        {
            // Pure conversation — do not touch spec
        }
        else if (message.TrimStart().StartsWith("[SCREEN_DETAIL]", StringComparison.OrdinalIgnoreCase))
        {
            await _spec.TryExtractAndSave(sessionId, responseText, moduleName);
        }
        else
        {
            var splitVersions = await _spec.TryExtractSplitAndSave(sessionId, responseText, _claude, moduleName);
            if (splitVersions.Count == 0)
                await _spec.TryExtractAndSave(sessionId, responseText, moduleName);
        }

        await _usage.LogUsage(userId, sessionId, inputTokens, outputTokens, gitUser);
        return responseText;
    }

    public Task<List<AgentMessage>> GetHistory(Guid sessionId)
        => _repo.GetMessages(sessionId);

    public Task<List<AgentSession>> GetSessionsByUser(Guid userId)
        => _repo.GetSessionsByUser(userId);

    public Task<List<AgentSession>> GetAllSessions()
        => _repo.GetAllSessions();

    public Task DeleteSession(Guid sessionId)
        => _repo.DeleteSession(sessionId);

    public Task RenameSession(Guid sessionId, string newTitle)
        => _repo.RenameSession(sessionId, newTitle);

    public Task SetFavorite(Guid sessionId, bool isFavorite)
        => _repo.SetFavorite(sessionId, isFavorite);

    public Task SetArchived(Guid sessionId, bool isArchived)
        => _repo.SetArchived(sessionId, isArchived);

    private static readonly string QA_SPEC_PROMPT = LoadPrompt("qa-spec-agent.txt");
    private static readonly string BA_SYSTEM_PROMPT = LoadPrompt("ba-agent.txt");

    private static string LoadSystemPromptText() => BA_SYSTEM_PROMPT;

    private static string LoadPrompt(string fileName)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Prompts");
        var path = Path.Combine(dir, fileName);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    public async Task<SpecQaResult> QaSpecAsync(Guid versionId, Guid userId, Guid? projectId = null, List<string>? selectedModules = null)
    {
        var version = await _spec.GetVersionById(versionId)
            ?? throw new InvalidOperationException($"Version {versionId} not found.");

        // Build project context suffix for the reviewer (same modules context as BA)
        var systemSuffix = string.Empty;
        if (projectId.HasValue)
        {
            var proj = await _project.GetById(projectId.Value);
            if (proj?.ProfileJson != null)
            {
                systemSuffix = $"\n\n{_project.BuildProfilePrompt(proj.ProfileJson)}";
                if (selectedModules is { Count: > 0 })
                {
                    var modLines = await BuildSelectedModuleContext(projectId.Value, userId, selectedModules);
                    systemSuffix += $"\n\n## MODULES ĐANG LÀM VIỆC\n{modLines}";
                }
            }
        }

        var prompt = QA_SPEC_PROMPT + systemSuffix;
        var claudeResult = await _claude.ChatWithPrompt(prompt, version.JsonSpec);
        var rawOutput = claudeResult.Text;

        return ParseQaSpecResult(rawOutput);
    }

    private static SpecQaResult ParseQaSpecResult(string raw)
    {
        try
        {
            var match = Regex.Match(raw, @"```json\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
            var json = match.Success ? match.Groups[1].Value.Trim() : raw.Trim();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var hasIssues = root.TryGetProperty("has_issues", out var hi) && hi.GetBoolean();
            var issues = new List<string>();
            if (root.TryGetProperty("issues", out var arr))
                foreach (var item in arr.EnumerateArray())
                    if (item.GetString() is { } s) issues.Add(s);

            string? improvedSpec = null;
            if (hasIssues && root.TryGetProperty("improved_spec", out var imp) && imp.ValueKind == System.Text.Json.JsonValueKind.Object)
                improvedSpec = imp.GetRawText();

            return new SpecQaResult { HasIssues = hasIssues, Issues = issues, ImprovedSpec = improvedSpec };
        }
        catch
        {
            return new SpecQaResult { HasIssues = true, Issues = new List<string> { "Không parse được kết quả QA. Raw: " + raw[..Math.Min(300, raw.Length)] } };
        }
    }

    private static string? ExtractModuleFromMessage(string message)
    {
        var match = Regex.Match(message, @"Module:\s*(.+)", RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static string? ExtractModuleFromScreenList(string? content)
    {
        if (content == null) return null;
        try
        {
            var jsonMatch = Regex.Match(content, @"```json\s*([\s\S]*?)```");
            if (!jsonMatch.Success) return null;
            using var doc = System.Text.Json.JsonDocument.Parse(jsonMatch.Groups[1].Value.Trim());
            var root = doc.RootElement;
            if (root.TryGetProperty("type", out var t) && t.GetString() == "screen_list"
                && root.TryGetProperty("module", out var m))
                return m.GetString();
        }
        catch { }
        return null;
    }

    private static string MinifyJson(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return System.Text.Json.JsonSerializer.Serialize(doc.RootElement);
        }
        catch { return json; }
    }

    private async Task<string> BuildSelectedModuleContext(Guid projectId, Guid userId, List<string> selectedModules)
    {
        try
        {
            // Get user config to fetch modulesJson with descriptions
            var config = await _project.GetUserConfig(projectId, userId);
            if (config?.LocalModulesJson == null)
                return string.Join(", ", selectedModules);

            using var doc = System.Text.Json.JsonDocument.Parse(config.LocalModulesJson);
            var sb = new System.Text.StringBuilder();
            foreach (var mod in doc.RootElement.EnumerateArray())
            {
                var name = mod.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                if (!selectedModules.Any(s => s.Equals(name, StringComparison.OrdinalIgnoreCase))) continue;
                var desc = mod.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                sb.AppendLine(string.IsNullOrWhiteSpace(desc) ? $"- {name}" : $"- {name}: {desc}");
            }
            return sb.Length > 0 ? sb.ToString().TrimEnd() : string.Join(", ", selectedModules);
        }
        catch { return string.Join(", ", selectedModules); }
    }

    private static string BuildSpecSummary(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.TryGetProperty("feature_name", out var fn) ? fn.GetString() : "?";

            var fields = root.TryGetProperty("fields", out var fa)
                ? string.Join(", ", fa.EnumerateArray().Select(f =>
                    f.TryGetProperty("name", out var n) ? n.GetString() : "?"))
                : "";

            var actions = root.TryGetProperty("actions", out var aa)
                ? string.Join(", ", aa.EnumerateArray().Select(a =>
                    a.TryGetProperty("name", out var n) ? n.GetString() : "?"))
                : "";

            return $"{name} | fields:[{fields}] | actions:[{actions}]";
        }
        catch { return ""; }
    }

    private async Task<SpecTaskVersion?> ResolveReferencedVersion(string message, SpecTaskVersion? currentVersion)
    {
        if (currentVersion == null) return null;

        // Match: v1, v2, V1, version 1, version 2, phiên bản 1
        var match = Regex.Match(message,
            @"\b(?:v|version\s*|phiên bản\s*)(\d+)\b",
            RegexOptions.IgnoreCase);
        if (!match.Success) return null;

        var num = int.Parse(match.Groups[1].Value);
        if (num == currentVersion.Version) return null; // already on that version

        return await _spec.GetVersionByNumber(currentVersion.TaskId, num);
    }

    private static bool NeedsFullSpec(string message)
    {
        var lower = message.ToLowerInvariant();
        var keywords = new[] {
            "field", "action", "spec", "màn", "screen",
            "show", "xem", "hiện", "list", "có gì", "gồm",
            "v1", "version", "chi tiết", "detail", "đầy đủ",
            "input", "output", "precondition", "validation"
        };
        return keywords.Any(k => lower.Contains(k));
    }

    private static Intent DetectIntent(string message, bool hasExistingSpec)
    {
        var msg = message.TrimStart();

        // [SCREEN_DETAIL] always creates a new spec
        if (msg.StartsWith("[SCREEN_DETAIL]", StringComparison.OrdinalIgnoreCase))
            return Intent.New;

        var lower = message.ToLowerInvariant();

        // Explicit "start over" keywords → New
        var newKeywords = new[] {
            "tạo mới", "làm mới", "làm lại từ đầu", "bắt đầu lại",
            "new spec", "start over", "reset spec",
            "làm tiếp", "tiếp theo", "màn hình tiếp", "tạo màn hình"
        };
        if (newKeywords.Any(k => lower.Contains(k))) return Intent.New;

        if (!hasExistingSpec) return Intent.New;

        // Explicit update keywords → Update
        var updateKeywords = new[] {
            "thêm ", "thêm\n", "bổ sung", "thêm vào",
            "sửa ", "chỉnh ", "cập nhật", "update",
            "xóa ", "bỏ ", "remove", "delete",
            "đổi tên", "rename",
            "thêm action", "thêm field", "thêm màn",
            "sửa action", "sửa field", "sửa màn",
            "bỏ action", "bỏ field", "bỏ màn",
        };
        if (updateKeywords.Any(k => lower.Contains(k))) return Intent.Update;

        // Question / discussion signals → Chat
        var chatSignals = new[] {
            "?", "là gì", "như thế nào", "tại sao", "khi nào",
            "giải thích", "mô tả", "ý nghĩa", "hiểu",
            "có nên", "nên không", "có thể không",
            "nghĩ sao", "bạn nghĩ", "theo bạn",
            "ổn không", "đúng không", "có đúng",
        };
        if (chatSignals.Any(k => lower.Contains(k))) return Intent.Chat;

        // Default when spec exists and no clear signal: treat as chat
        // (safer than accidentally overwriting spec)
        return Intent.Chat;
    }
}

internal enum Intent { New, Update, Chat }
