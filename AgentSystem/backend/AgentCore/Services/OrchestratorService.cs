using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using AgentInfrastructure.Entities;
using AgentInfrastructure.Repositories;
using Microsoft.Extensions.Configuration;

namespace AgentCore.Services;

public class OrchestratorService : IOrchestratorService
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, OrchestratorJobState> _jobs = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, List<GeneratedFile>> _versionFiles = new();
    private static string? _projectContext = null;
    private static readonly string SCAN_PROMPT = LoadPrompt("scan-project-agent.txt");

    private const string Model = "claude-haiku-4-5-20251001";
    private const int MaxTokens = 8000;
    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const int MaxRetries = 3;

    private static readonly string BA_PROMPT = LoadPrompt("ba-agent.txt");
    private static readonly string SPLITTER_PROMPT = LoadPrompt("splitter-agent.txt");
    private static readonly string DEV_PROMPT = LoadPrompt("dev-agent.txt");
    private static readonly string QA_PROMPT = LoadPrompt("qa-agent - Copy.txt");
    private static readonly string SPLIT_PROMPT = LoadPrompt("split-files-agent.txt");
    private static readonly string FIX_COMPILE_PROMPT = LoadPrompt("fix-compile-agent.txt");
    private static readonly string GIT_PUSH_PROMPT = LoadPrompt("git-push-agent.txt");

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _defaultOutputDir;
    private readonly IOrchestratorLogRepository _logs;
    private readonly ISpecRepository _specRepo;
    private readonly IProjectRepository _projectRepo;

    public OrchestratorService(HttpClient http, IConfiguration config, IOrchestratorLogRepository logs, ISpecRepository specRepo, IProjectRepository projectRepo)
    {
        _http = http;
        _apiKey = config["Claude:ApiKey"] ?? throw new InvalidOperationException("Claude:ApiKey missing.");
        _defaultOutputDir = config["Orchestrator:DefaultOutputDir"] ?? string.Empty;
        _logs = logs;
        _specRepo = specRepo;
        _projectRepo = projectRepo;
    }

    public Task<OrchestratorResult> RunFromSpecAsync(string specJson)
        => RunPipelineAsync(specJson);

    public Task<OrchestratorResult> RunFromSpecSplitAsync(string specJson)
        => RunFromSpecSplitAsync(specJson, null, null);

    private async Task<(string? outputDir, string? profileText, string? modulesJson)> ResolveProjectContext(string? outputDir, Guid? projectId, Guid? userId)
    {
        if (projectId == null) return (outputDir, null, null);

        var project = await _projectRepo.GetById(projectId.Value);
        var profileText = project?.ProfileJson;
        string? modulesJson = null;

        if (userId != null)
        {
            var config = await _projectRepo.GetUserConfig(projectId.Value, userId.Value);
            if (string.IsNullOrWhiteSpace(outputDir) && !string.IsNullOrWhiteSpace(config?.LocalOutputDir))
                outputDir = config.LocalOutputDir;
            modulesJson = config?.LocalModulesJson;
        }

        return (outputDir, profileText, modulesJson);
    }

    private async Task<OrchestratorResult> RunFromSpecSplitAsync(string specJson, OrchestratorJobState? job, string? outputDir = null, string? profileText = null, string? modulesJson = null)
    {
        job?.AddLog("Phân tích spec để tách sub-tasks...");
        var splitOutput = await CallAsync(SPLITTER_PROMPT, specJson, "SPLITTER");
        var subSpecs = ParseSubSpecs(splitOutput);

        if (subSpecs.Count <= 1)
            return await RunPipelineAsync(specJson, job, outputDir, profileText, modulesJson);

        job?.AddLog($"Tách được {subSpecs.Count} sub-tasks, chạy tuần tự...");

        var subResults = new List<SubTaskResult>();
        var allFiles = new List<GeneratedFile>();

        foreach (var subSpec in subSpecs)
        {
            var specName = ExtractFeatureName(subSpec);
            job?.AddLog($"--- Bắt đầu sub-task: {specName} ---");
            try
            {
                var r = await RunPipelineAsync(subSpec, job, outputDir, profileText, modulesJson);
                subResults.Add(new SubTaskResult
                {
                    SpecName = specName,
                    Spec = subSpec,
                    Files = r.Files,
                    QaReport = r.QaReport,
                    Status = r.Status,
                    RetryCount = r.RetryCount,
                    Error = r.Error
                });
                allFiles.AddRange(r.Files);
            }
            catch (Exception ex)
            {
                subResults.Add(new SubTaskResult
                {
                    SpecName = specName,
                    Spec = subSpec,
                    Status = "FAIL",
                    Error = ex.Message
                });
            }
        }

        var overallStatus = subResults.All(r => r.Status == "PASS") ? "PASS" : "FAIL";
        return new OrchestratorResult
        {
            Spec = specJson,
            Files = allFiles,
            QaReport = string.Join("\n\n---\n\n", subResults.Select(r => $"[{r.SpecName}]\n{r.QaReport}")),
            Status = overallStatus,
            RetryCount = subResults.Sum(r => r.RetryCount),
            SubResults = subResults
        };
    }

    public async Task<OrchestratorResult> RunAsync(string featureRequest)
    {
        // ── Phase 1: BA → spec ────────────────────────────────────────
        var spec = await CallAsync(BA_PROMPT, featureRequest, "BA");

        return await RunPipelineAsync(spec);
    }

    public OrchestratorJobState? GetJob(Guid jobId)
        => _jobs.TryGetValue(jobId, out var state) ? state : null;

    public string? GetProjectContext() => _projectContext;
    public void SetProjectContext(string context) => _projectContext = context;
    public List<GeneratedFile>? GetFilesForVersion(Guid versionId)
        => _versionFiles.TryGetValue(versionId, out var f) ? f : null;

    public List<DirEntry> ListDir(string directoryPath)
    {
        var entries = new List<DirEntry>();
        if (!Directory.Exists(directoryPath)) return entries;
        foreach (var dir in Directory.GetDirectories(directoryPath).OrderBy(d => d))
            entries.Add(new DirEntry { Name = Path.GetFileName(dir), Path = dir.Replace('\\', '/'), IsFolder = true });
        foreach (var file in Directory.GetFiles(directoryPath).OrderBy(f => f))
            entries.Add(new DirEntry { Name = Path.GetFileName(file), Path = file.Replace('\\', '/'), IsFolder = false });
        return entries;
    }

    public async Task<string> ReadFileAsync(string filePath)
    {
        if (!File.Exists(filePath)) return string.Empty;
        return await File.ReadAllTextAsync(filePath);
    }

    public async Task<string> ScanProjectAsync(string bePath, string fePath)
    {
        var samples = new System.Text.StringBuilder();
        samples.AppendLine("=== BACKEND SAMPLES ===");
        AppendFileSamples(samples, bePath, "*.cs", new[] { "Controller", "Service", "Repository", "Entity" }, maxFiles: 6);
        samples.AppendLine("\n=== FRONTEND SAMPLES ===");
        AppendFileSamples(samples, fePath, "*.ts", new[] { "component", "service" }, maxFiles: 4);

        var input = samples.ToString();
        if (input.Length > 12000) input = input[..12000] + "\n...[truncated]";

        var context = await CallClaudeAsync(SCAN_PROMPT, input);
        _projectContext = context;
        return context;
    }

    // Resolve output dir for a feature by matching module names
    private static string? ResolveModuleOutputDir(string featureName, string? modulesJson)
    {
        if (string.IsNullOrWhiteSpace(modulesJson)) return null;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(modulesJson);
            foreach (var mod in doc.RootElement.EnumerateArray())
            {
                var name = mod.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var outPath = mod.TryGetProperty("outputPath", out var op) ? op.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(outPath)) continue;
                if (featureName.Contains(name, StringComparison.OrdinalIgnoreCase))
                    return outPath;
            }
        }
        catch { }
        return null;
    }

    // Filter modulesJson to only modules whose names are in selectedModules list
    private static string? FilterModules(string? modulesJson, List<string>? selectedModules)
    {
        if (string.IsNullOrWhiteSpace(modulesJson) || selectedModules is not { Count: > 0 })
            return modulesJson;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(modulesJson);
            var filtered = doc.RootElement.EnumerateArray()
                .Where(m =>
                {
                    var name = m.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    return selectedModules.Any(s => s.Equals(name, StringComparison.OrdinalIgnoreCase));
                })
                .Select(m => m.GetRawText())
                .ToList();
            return filtered.Count > 0 ? $"[{string.Join(",", filtered)}]" : modulesJson;
        }
        catch { return modulesJson; }
    }

    private static string BuildModulePathSummary(string modulesJson)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(modulesJson);
            var sb = new System.Text.StringBuilder();
            foreach (var mod in doc.RootElement.EnumerateArray())
            {
                var name = mod.TryGetProperty("name", out var n) ? n.GetString() : null;
                var desc = mod.TryGetProperty("description", out var d) ? d.GetString() : null;
                var src = mod.TryGetProperty("sourcePath", out var sp) ? sp.GetString() : null;
                var outP = mod.TryGetProperty("outputPath", out var op) ? op.GetString() : null;
                if (name == null) continue;
                var descPart = !string.IsNullOrWhiteSpace(desc) ? $" ({desc})" : "";
                sb.AppendLine($"- {name}{descPart}: output={outP ?? "n/a"}");
            }
            return sb.ToString();
        }
        catch { return modulesJson; }
    }

    // Convert profile JSON (from scan) to a concise text summary for injection into DEV prompt
    private static string BuildProfileSummary(string profileJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(profileJson);
            var root = doc.RootElement;
            var sb = new System.Text.StringBuilder();
            foreach (var prop in root.EnumerateObject())
                sb.AppendLine($"{prop.Name}: {prop.Value}");
            return sb.ToString();
        }
        catch { return profileJson; }
    }

    private static void AppendFileSamples(System.Text.StringBuilder sb, string basePath, string pattern, string[] keywords, int maxFiles)
    {
        if (!Directory.Exists(basePath)) return;
        var files = Directory.GetFiles(basePath, pattern, SearchOption.AllDirectories)
            .Where(f => keywords.Any(k => Path.GetFileNameWithoutExtension(f).Contains(k, StringComparison.OrdinalIgnoreCase)))
            .Take(maxFiles);

        foreach (var file in files)
        {
            try
            {
                var lines = File.ReadAllLines(file).Take(80);
                sb.AppendLine($"\n--- {Path.GetFileName(file)} ---");
                sb.AppendLine(string.Join('\n', lines));
            }
            catch { }
        }
    }

    public async Task<Guid> StartRunFromVersion(Guid versionId, string? outputDir = null, Guid? projectId = null, Guid? userId = null, List<string>? selectedModules = null)
    {
        var version = await _specRepo.GetVersionById(versionId)
            ?? throw new InvalidOperationException($"Version {versionId} not found.");

        var specJson = version.JsonSpec;
        var taskName = version.Task?.Name ?? versionId.ToString();

        // Resolve project profile and user outputDir in request scope (before Task.Run)
        var (resolvedOutputDir, profileText, modulesJson) = await ResolveProjectContext(outputDir, projectId, userId);
        var filteredModulesJson = FilterModules(modulesJson, selectedModules);

        var jobId = Guid.NewGuid();
        var job = new OrchestratorJobState();
        _jobs[jobId] = job;

        var capturedVersionId = versionId;
        _ = Task.Run(async () =>
        {
            try
            {
                job.AddLog($"Spec: {taskName}");
                job.Result = await RunFromSpecSplitAsync(specJson, job, resolvedOutputDir, profileText, filteredModulesJson);
                if (job.Result != null)
                    _versionFiles[capturedVersionId] = job.Result.Files;
            }
            catch (Exception ex)
            {
                job.Error = ex.Message;
                FileLogger.Error("Orchestrator.StartRunFromVersion", ex);
            }
            finally { job.IsComplete = true; }
        });

        return jobId;
    }

    public async Task<Guid> StartRunFromAction(Guid versionId, string actionName, string? outputDir = null, Guid? projectId = null, Guid? userId = null, List<string>? selectedModules = null)
    {
        var version = await _specRepo.GetVersionById(versionId)
            ?? throw new InvalidOperationException($"Version {versionId} not found.");

        var miniSpec = BuildActionSpec(version.JsonSpec, actionName, version.Task?.Name);

        // Resolve project profile and user outputDir in request scope (before Task.Run)
        var (resolvedOutputDir, profileText, modulesJson) = await ResolveProjectContext(outputDir, projectId, userId);
        var filteredModulesJson = FilterModules(modulesJson, selectedModules);

        var jobId = Guid.NewGuid();
        var job = new OrchestratorJobState();
        _jobs[jobId] = job;

        var capturedVersionId = versionId;
        _ = Task.Run(async () =>
        {
            try
            {
                job.AddLog($"Action: {actionName}");
                job.Result = await RunPipelineAsync(miniSpec, job, resolvedOutputDir, profileText, filteredModulesJson);
                if (job.Result != null)
                {
                    // Merge with existing version files (action adds to, not replaces)
                    _versionFiles.AddOrUpdate(capturedVersionId,
                        job.Result.Files,
                        (_, existing) =>
                        {
                            var merged = existing.ToList();
                            foreach (var f in job.Result.Files)
                            {
                                var idx = merged.FindIndex(e => e.FilePath.Equals(f.FilePath, StringComparison.OrdinalIgnoreCase));
                                if (idx >= 0) merged[idx] = f; else merged.Add(f);
                            }
                            return merged;
                        });
                }
            }
            catch (Exception ex)
            {
                job.Error = ex.Message;
                FileLogger.Error("Orchestrator.StartRunFromAction", ex);
            }
            finally { job.IsComplete = true; }
        });

        return jobId;
    }

    public async Task<OrchestratorResult> RunFromVersionAsync(Guid versionId)
    {
        var version = await _specRepo.GetVersionById(versionId)
            ?? throw new InvalidOperationException($"Version {versionId} not found.");
        return await RunFromSpecSplitAsync(version.JsonSpec);
    }

    public async Task<OrchestratorResult> RunFromActionAsync(Guid versionId, string actionName)
    {
        var version = await _specRepo.GetVersionById(versionId)
            ?? throw new InvalidOperationException($"Version {versionId} not found.");
        return await RunPipelineAsync(BuildActionSpec(version.JsonSpec, actionName, version.Task?.Name));
    }

    private static string BuildActionSpec(string jsonSpec, string actionName, string? taskName)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonSpec);
            var root = doc.RootElement;

            // Find the specific action
            var action = root.TryGetProperty("actions", out var actions)
                ? actions.EnumerateArray().FirstOrDefault(a =>
                    a.TryGetProperty("name", out var n) &&
                    string.Equals(n.GetString(), actionName, StringComparison.OrdinalIgnoreCase))
                : default;

            if (action.ValueKind == JsonValueKind.Undefined)
                return jsonSpec; // action not found, fall back to full spec

            var featureName = root.TryGetProperty("feature_name", out var fn) ? fn.GetString() : taskName;
            var actors = root.TryGetProperty("actors", out var ac) ? ac : default;

            // Only include fields referenced in this action's input/output — not all entity fields
            // This prevents DEV agent from inferring full CRUD from entity fields
            var actionInputKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (action.TryGetProperty("input", out var inp))
                foreach (var p in inp.EnumerateObject()) actionInputKeys.Add(p.Name);
            if (action.TryGetProperty("output", out var outp))
                foreach (var p in outp.EnumerateObject()) actionInputKeys.Add(p.Name);

            var relevantFields = root.TryGetProperty("fields", out var fl)
                ? fl.EnumerateArray()
                    .Where(f => !f.TryGetProperty("name", out var fn2) || actionInputKeys.Count == 0 || actionInputKeys.Contains(fn2.GetString() ?? ""))
                    .Select(f => JsonSerializer.Deserialize<object>(f.GetRawText()))
                    .ToArray()
                : Array.Empty<object>();

            return JsonSerializer.Serialize(new
            {
                feature_name = $"{featureName}.{actionName}",
                description = action.TryGetProperty("description", out var d) ? d.GetString() : actionName,
                actors = actors.ValueKind != JsonValueKind.Undefined ? JsonSerializer.Deserialize<object[]>(actors.GetRawText()) : Array.Empty<object>(),
                fields = relevantFields,
                actions = new[] { JsonSerializer.Deserialize<object>(action.GetRawText()) }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch { return jsonSpec; }
    }

    private async Task<OrchestratorResult> RunPipelineAsync(string spec, OrchestratorJobState? job = null, string? outputDir = null, string? profileText = null, string? modulesJson = null)
    {
        // Resolve output directory: module match → param → config default → temp fallback
        var featureName = ExtractFeatureName(spec);
        var moduleOutputDir = ResolveModuleOutputDir(featureName, modulesJson);
        var resolvedDir = !string.IsNullOrWhiteSpace(moduleOutputDir) ? moduleOutputDir
            : !string.IsNullOrWhiteSpace(outputDir) ? outputDir
            : !string.IsNullOrWhiteSpace(_defaultOutputDir) ? _defaultOutputDir
            : Path.Combine(Path.GetTempPath(), "orch-" + Guid.NewGuid().ToString("N")[..8]);

        if (moduleOutputDir != null)
            job?.AddLog($"Phase 2: Output → module '{featureName}' → {resolvedDir}");

        // ── Phase 2: DEV → multi-file code ───────────────────────────
        job?.AddLog("Phase 2: DEV Agent đang sinh code...");

        // Build shared project context block reused by DEV, QA, DEV_PATCH
        var effectiveProfile = profileText ?? _projectContext;
        var projectContextBlock = string.Empty;
        if (effectiveProfile != null)
            projectContextBlock += $"\n\n## PROJECT CONVENTIONS (follow these exactly)\n{BuildProfileSummary(effectiveProfile)}";
        if (!string.IsNullOrWhiteSpace(modulesJson))
            projectContextBlock += $"\n\n## MODULES IN SCOPE\n{BuildModulePathSummary(modulesJson)}";

        var devPrompt = DEV_PROMPT + projectContextBlock;
        if (!string.IsNullOrWhiteSpace(modulesJson))
            devPrompt += "\n\nOutput files under the matching module output path. If no match, use the default output directory.";

        var existingFiles = ReadRelevantFiles(resolvedDir, spec);
        var devInput = spec;
        if (existingFiles.Count > 0)
        {
            job?.AddLog($"Phase 2: Phát hiện {existingFiles.Count} file liên quan, sẽ update thay vì tạo mới.");
            devInput = $"{spec}\n\n## EXISTING CODE TO UPDATE (only output files that need changes)\n{TrimCode(FilesToText(existingFiles))}";
        }

        var devOutput = await CallAsync(devPrompt, devInput, "DEV");
        var files = ParseFiles(devOutput);
        if (files.Count == 0)
        {
            FileLogger.Error("OrchestratorService.DEV", $"ParseFiles got 0 files. Raw output (first 500):\n{devOutput[..Math.Min(500, devOutput.Length)]}");
            job?.AddLog("Phase 2: WARN — DEV output không parse được file nào. Xem log để debug.");
        }
        else
        {
            job?.AddLog($"Phase 2: Sinh được {files.Count} file(s): {string.Join(", ", files.Keys)}");
        }

        // ── Phase 3: Write files + build + fix loop ───────────────────
        WriteFiles(resolvedDir, files);

        job?.AddLog("Phase 3: Build...");
        var (buildOk, buildErrors) = RunBuild(resolvedDir);
        if (buildOk)
        {
            job?.AddLog("Phase 3: Build thành công ✓");
        }
        else
        {
            job?.AddLog($"Phase 3: Build lỗi, đang fix... ({buildErrors[..Math.Min(200, buildErrors.Length)]})");
        }

        for (int i = 0; i < MaxRetries && !buildOk; i++)
        {
            job?.AddLog($"Phase 3: Fix compile lần {i + 1}...");
            var fixInput = $"SOURCE CODE:\n{FilesToText(files)}\n\nERROR MESSAGES:\n{buildErrors}";
            var fixOutput = await CallAsync(FIX_COMPILE_PROMPT, fixInput, "FIX_COMPILE");
            var fixedFiles = ParseFiles(fixOutput);
            foreach (var f in fixedFiles)
                files[f.Key] = f.Value;
            WriteFiles(resolvedDir, files);
            (buildOk, buildErrors) = RunBuild(resolvedDir);
            job?.AddLog(buildOk ? "Phase 3: Build OK sau fix ✓" : $"Phase 3: Vẫn lỗi, thử lại...");
        }

        var buildNote = buildOk ? string.Empty : $"[Build warning] {buildErrors}";

        // ── Phase 4: QA ───────────────────────────────────────────────
        job?.AddLog("Phase 4: QA Agent đang review...");
        var qaPrompt = QA_PROMPT + projectContextBlock;
        var qaInput = $"SPEC:\n{MinifyJson(spec)}\n\nSOURCE CODE:\n{TrimCode(FilesToText(files))}";
        var qaReport = await CallAsync(qaPrompt, qaInput, "QA");

        var retryCount = 0;

        // ── Phase 5: Patch loop — per file, bugs only ─────────────────
        while (retryCount < MaxRetries)
        {
            var bugsByFile = ExtractBugsByFile(qaReport);
            if (bugsByFile.Count == 0)
            {
                job?.AddLog("Phase 5: Không có bug ✓");
                break;
            }

            retryCount++;
            job?.AddLog($"Phase 5: Patch lần {retryCount}, {bugsByFile.Count} file(s) có bug...");
            foreach (var (filePath, bugs) in bugsByFile)
            {
                job?.AddLog($"Phase 5: Patch {filePath}...");
                var existingContent = files.TryGetValue(filePath, out var c) ? c : string.Empty;
                var patchInput =
                    $"Fix ONLY these bugs for file: {filePath}\n\n" +
                    $"Bugs:\n{bugs}\n\n" +
                    $"Existing code:\n{existingContent}";
                var patchOutput = await CallAsync(devPrompt, patchInput, "DEV_PATCH");
                var patched = ParseFiles(patchOutput);
                if (patched.TryGetValue(filePath, out var newContent))
                    files[filePath] = newContent;
            }

            job?.AddLog("Phase 5: Re-run QA...");
            qaInput = $"SPEC:\n{MinifyJson(spec)}\n\nSOURCE CODE:\n{TrimCode(FilesToText(files))}";
            qaReport = await CallAsync(qaPrompt, qaInput, "QA");
        }

        // ── Phase 6: Git push instructions ───────────────────────────
        job?.AddLog("Phase 6: Sinh git instructions...");
        var gitInstructions = await CallAsync(GIT_PUSH_PROMPT,
            $"Output directory: {resolvedDir}", "GIT_PUSH");
        job?.AddLog($"Hoàn tất! Output: {resolvedDir}");

        var hasBugs = ExtractBugsByFile(qaReport).Count > 0;
        return new OrchestratorResult
        {
            Spec = spec,
            Files = ToFileList(files),
            QaReport = qaReport,
            GitInstructions = gitInstructions,
            Status = hasBugs ? "FAIL" : "PASS",
            RetryCount = retryCount,
            Error = string.IsNullOrEmpty(buildNote) ? null : buildNote,
            OutputDir = resolvedDir
        };
    }

    // ── Claude HTTP call ──────────────────────────────────────────────

    private async Task<string> CallAsync(string systemPrompt, string userMessage, string agentType = "")
    {
        await Task.Delay(6000); // tránh rate limit: 6s giữa các call
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await CallClaudeAsync(systemPrompt, userMessage);
        sw.Stop();
        try
        {
            await _logs.Add(new OrchestratorLog
            {
                AgentType = agentType,
                Input = userMessage.Length > 2000 ? userMessage[..2000] : userMessage,
                Output = result.Length > 2000 ? result[..2000] : result,
                DurationMs = (int)sw.ElapsedMilliseconds
            });
        }
        catch (Exception ex) { FileLogger.Error("Orchestrator.AuditLog", ex); }
        return result;
    }

    private async Task<string> CallClaudeAsync(string systemPrompt, string userMessage)
    {
        var payload = new
        {
            model = Model,
            max_tokens = MaxTokens,
            system = systemPrompt,
            messages = new[] { new { role = "user", content = userMessage } }
        };

        int delay = 15000;
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            req.Headers.Add("x-api-key", _apiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");
            req.Content = JsonContent.Create(payload);

            using var res = await _http.SendAsync(req);

            if (res.StatusCode == System.Net.HttpStatusCode.TooManyRequests && attempt < MaxRetries)
            {
                await Task.Delay(delay);
                delay *= 2; // 15s → 30s → 60s
                continue;
            }

            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                var msg = $"Claude API error {(int)res.StatusCode}: {body}";
                FileLogger.Error("OrchestratorService.CallClaude", msg);
                throw new InvalidOperationException(msg);
            }

            using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            return doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;
        }

        throw new InvalidOperationException("Rate limit exceeded after retries.");
    }

    // ── File parsing ──────────────────────────────────────────────────

    // Parses: FILE: path/to/File.cs\n```csharp\n...\n```
    private static Dictionary<string, string> ParseFiles(string output)
    {
        var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Try FILE: marker pattern first
        var filePattern = @"FILE:\s*([^\r\n]+)\r?\n```(?:csharp|cs)?\r?\n([\s\S]*?)```";
        foreach (Match m in Regex.Matches(output, filePattern, RegexOptions.IgnoreCase))
        {
            var path = m.Groups[1].Value.Trim().TrimStart('/', '\\');
            files[path] = m.Groups[2].Value;
        }

        // Fallback: try JSON array format [{"file_path":..., "content":...}]
        if (files.Count == 0)
        {
            try
            {
                var jsonMatch = Regex.Match(output, @"\[[\s\S]*\]");
                if (jsonMatch.Success)
                {
                    using var doc = JsonDocument.Parse(jsonMatch.Value);
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        var path = (item.TryGetProperty("file_path", out var fp) ? fp.GetString() : null)
                                   ?? item.GetProperty("path").GetString()
                                   ?? string.Empty;
                        var content = item.GetProperty("content").GetString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(path))
                            files[path.TrimStart('/', '\\')] = content;
                    }
                }
            }
            catch { /* ignore parse failures */ }
        }

        return files;
    }

    private static Dictionary<string, string> ReadRelevantFiles(string baseDir, string specJson)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(baseDir)) return result;

        // Build candidate names from feature_name: e.g. "TaskList" → {"TaskList", "Task"}
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(specJson);
            if (doc.RootElement.TryGetProperty("feature_name", out var fn))
            {
                var name = fn.GetString() ?? "";
                if (!string.IsNullOrEmpty(name))
                {
                    candidates.Add(name);
                    var entity = StripScreenSuffix(name);
                    if (entity != name) candidates.Add(entity);
                }
            }
        }
        catch { }

        if (candidates.Count == 0) return result;

        var exts = new[] { ".cs", ".ts", ".html", ".sql" };
        foreach (var file in Directory.GetFiles(baseDir, "*.*", SearchOption.AllDirectories))
        {
            if (!exts.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)) continue;
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (!candidates.Any(c => fileName.Contains(c, StringComparison.OrdinalIgnoreCase))) continue;
            try
            {
                var rel = Path.GetRelativePath(baseDir, file).Replace('\\', '/');
                result[rel] = File.ReadAllText(file);
            }
            catch { }
        }

        // Always include migration.sql — it needs to be updated with new tables
        var sqlPath = Path.Combine(baseDir, "db", "migration.sql");
        if (File.Exists(sqlPath))
            try { result["db/migration.sql"] = File.ReadAllText(sqlPath); } catch { }

        return result;
    }

    private static string StripScreenSuffix(string name)
    {
        var suffixes = new[] { "List", "Detail", "Create", "Edit", "Form", "Management", "Manager", "View", "Page" };
        foreach (var s in suffixes)
            if (name.EndsWith(s, StringComparison.OrdinalIgnoreCase) && name.Length > s.Length)
                return name[..^s.Length];
        return name;
    }

    private static void WriteFiles(string baseDir, Dictionary<string, string> files)
    {
        Directory.CreateDirectory(baseDir);
        foreach (var (path, content) in files)
        {
            var full = Path.Combine(baseDir, path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
        }
    }

    private static string FilesToText(Dictionary<string, string> files) =>
        string.Join("\n\n", files.Select(f => $"FILE: {f.Key}\n```csharp\n{f.Value}\n```"));

    private static List<GeneratedFile> ToFileList(Dictionary<string, string> files) =>
        files.Select(f => new GeneratedFile { FilePath = f.Key, Content = f.Value }).ToList();

    // ── Build ─────────────────────────────────────────────────────────

    private static (bool ok, string errors) RunBuild(string dir)
    {
        if (!Directory.GetFiles(dir, "*.csproj", SearchOption.TopDirectoryOnly).Any())
            File.WriteAllText(Path.Combine(dir, "GeneratedFeature.csproj"),
                "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
                "  <PropertyGroup>\n" +
                "    <TargetFramework>net6.0</TargetFramework>\n" +
                "    <OutputType>Library</OutputType>\n" +
                "    <Nullable>enable</Nullable>\n" +
                "    <ImplicitUsings>enable</ImplicitUsings>\n" +
                "  </PropertyGroup>\n" +
                "  <ItemGroup>\n" +
                "    <PackageReference Include=\"Microsoft.EntityFrameworkCore.SqlServer\" Version=\"6.0.25\" />\n" +
                "    <PackageReference Include=\"Microsoft.AspNetCore.App\" Version=\"2.2.8\" />\n" +
                "  </ItemGroup>\n" +
                "</Project>");

        try
        {
            var psi = new ProcessStartInfo("dotnet", $"build \"{dir}\" --nologo -v quiet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var proc = Process.Start(psi)!;
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            return proc.ExitCode == 0
                ? (true, string.Empty)
                : (false, $"{stdout}\n{stderr}".Trim());
        }
        catch (Exception ex)
        {
            return (false, $"dotnet build unavailable: {ex.Message}");
        }
    }

    // ── QA helpers ────────────────────────────────────────────────────

    // Returns bugs grouped by file_path: { "Services/Foo.cs": "[{...}]" }
    private static Dictionary<string, string> ExtractBugsByFile(string qaReport)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var jsonMatch = Regex.Match(qaReport, @"\{[\s\S]*\}");
            if (!jsonMatch.Success) return result;

            using var doc = JsonDocument.Parse(jsonMatch.Value);
            if (!doc.RootElement.TryGetProperty("bugs", out var bugs)) return result;
            if (bugs.GetArrayLength() == 0) return result;

            // Group bugs by file
            foreach (var bug in bugs.EnumerateArray())
            {
                var file = bug.TryGetProperty("file", out var fp) ? fp.GetString() ?? "unknown" : "unknown";
                file = file.TrimStart('/', '\\');
                if (!result.ContainsKey(file)) result[file] = string.Empty;
                result[file] += bug.GetRawText() + "\n";
            }
        }
        catch { /* ignore */ }
        return result;
    }

    private static string MinifyJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement);
        }
        catch { return json; }
    }

    // Giới hạn code gửi cho QA: tối đa 4000 ký tự mỗi file, tổng tối đa 8000
    private static string TrimCode(string code)
    {
        if (code.Length <= 8000) return code;
        return code[..8000] + "\n...[truncated]";
    }

    private static List<string> ParseSubSpecs(string splitterOutput)
    {
        var match = Regex.Match(splitterOutput, @"\[[\s\S]*\]");
        if (!match.Success) return new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(match.Value);
            return doc.RootElement.EnumerateArray()
                .Select(el => JsonSerializer.Serialize(el, new JsonSerializerOptions { WriteIndented = true }))
                .ToList();
        }
        catch { return new List<string>(); }
    }

    private static string ExtractFeatureName(string specJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(specJson);
            if (doc.RootElement.TryGetProperty("feature_name", out var fn))
                return fn.GetString() ?? "Task";
        }
        catch { }
        return "Task";
    }

    // ── Prompt loader ─────────────────────────────────────────────────

    private static string LoadPrompt(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Prompts", fileName);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
}
