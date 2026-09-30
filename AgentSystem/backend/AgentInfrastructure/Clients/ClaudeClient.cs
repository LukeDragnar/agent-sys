using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using AgentInfrastructure.Entities;
using Microsoft.Extensions.Configuration;

namespace AgentInfrastructure.Clients;

public class ClaudeClient : IClaudeClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _systemPrompt;

    private const string Endpoint = "https://api.anthropic.com/v1/messages";
    private const string Model = "claude-haiku-4-5-20251001";
    private const int MaxTokens = 8000;

    public ClaudeClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Claude:ApiKey"] ?? throw new InvalidOperationException("Claude:ApiKey is not configured.");
        _systemPrompt = LoadSystemPrompt();
    }

    public async Task<ClaudeResult> Chat(List<AgentMessage> messages, string? systemSuffix = null)
    {
        var system = string.IsNullOrWhiteSpace(systemSuffix) ? _systemPrompt : _systemPrompt + systemSuffix;
        var request = new ClaudeRequest
        {
            Model = Model,
            MaxTokens = MaxTokens,
            System = system,
            Messages = BuildMessages(messages)
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        req.Headers.Add("x-api-key", _apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = JsonContent.Create(request, options: JsonOptions);

        return await SendWithRetryAsync(req);
    }

    public async Task<ClaudeResult> ChatWithPrompt(string systemPrompt, string userMessage)
    {
        var request = new ClaudeRequest
        {
            Model = Model,
            MaxTokens = MaxTokens,
            System = systemPrompt,
            Messages = new List<object> { new ClaudeMessage { Role = "user", Content = userMessage } }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        req.Headers.Add("x-api-key", _apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = JsonContent.Create(request, options: JsonOptions);

        return await SendWithRetryAsync(req);
    }

    public async Task<ClaudeToolResult> ChatWithTools(List<object> messages, string systemPrompt, List<ToolDefinition> tools)
    {
        var toolDefs = tools.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            input_schema = t.InputSchema
        }).ToList<object>();

        var requestBody = new
        {
            model = Model,
            max_tokens = MaxTokens,
            system = systemPrompt,
            tools = toolDefs,
            messages
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        req.Headers.Add("x-api-key", _apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = JsonContent.Create(requestBody, options: JsonOptions);

        var clone = await CloneRequestAsync(req);
        using var res = await _http.SendAsync(clone);

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Claude API error {(int)res.StatusCode}: {body}");
        }

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var stopReason = root.GetProperty("stop_reason").GetString() ?? "end_turn";
        var usage = root.TryGetProperty("usage", out var u)
            ? (input: u.GetProperty("input_tokens").GetInt32(), output: u.GetProperty("output_tokens").GetInt32())
            : (input: 0, output: 0);

        var textParts = new List<string>();
        var toolCalls = new List<ToolUseBlock>();
        var rawBlocks = new List<object>();

        foreach (var block in root.GetProperty("content").EnumerateArray())
        {
            var type = block.GetProperty("type").GetString();
            if (type == "text")
            {
                var text = block.GetProperty("text").GetString() ?? "";
                textParts.Add(text);
                rawBlocks.Add(new { type = "text", text });
            }
            else if (type == "tool_use")
            {
                var toolUse = new ToolUseBlock
                {
                    Id = block.GetProperty("id").GetString() ?? "",
                    Name = block.GetProperty("name").GetString() ?? "",
                    Input = block.GetProperty("input").Clone()
                };
                toolCalls.Add(toolUse);
                // Preserve raw block for assistant message reconstruction
                rawBlocks.Add(JsonSerializer.Deserialize<object>(block.GetRawText(), JsonOptions)!);
            }
        }

        return new ClaudeToolResult
        {
            Text = textParts.Count > 0 ? string.Join("\n", textParts) : null,
            StopReason = stopReason,
            ToolCalls = toolCalls,
            RawContentBlocks = rawBlocks,
            InputTokens = usage.input,
            OutputTokens = usage.output
        };
    }

    private async Task<ClaudeResult> SendWithRetryAsync(HttpRequestMessage req)
    {
        const int maxRetries = 3;
        int delay = 10000; // 10s initial delay on 429

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            // Must recreate request content on retry (HttpRequestMessage can't be reused)
            var clone = await CloneRequestAsync(req);
            using var res = await _http.SendAsync(clone);

            if (res.StatusCode == System.Net.HttpStatusCode.TooManyRequests && attempt < maxRetries)
            {
                await Task.Delay(delay);
                delay *= 2; // exponential backoff
                continue;
            }

            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                var msg = $"Claude API error {(int)res.StatusCode}: {body}";
                AppFileLogger.Error("ClaudeClient", msg);
                throw new InvalidOperationException(msg);
            }

            var response = await res.Content.ReadFromJsonAsync<ClaudeResponse>(JsonOptions)
                ?? throw new InvalidOperationException("Empty response from Claude API.");

            return new ClaudeResult
            {
                Text = response.Content[0].Text,
                InputTokens = response.Usage?.InputTokens ?? 0,
                OutputTokens = response.Usage?.OutputTokens ?? 0
            };
        }

        throw new InvalidOperationException("Rate limit exceeded after retries.");
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);
        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        if (original.Content != null)
        {
            var body = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(body);
            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }

    private static List<object> BuildMessages(List<AgentMessage> history)
    {
        return history
            .Where(x => x.Role == "user" || x.Role == "assistant")
            .Select(x => (object)new ClaudeMessage
            {
                Role = x.Role,
                Content = x.Content
            })
            .ToList();
    }

    private static string LoadSystemPrompt()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Prompts", "ba-agent.txt");
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

internal class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(name[i]));
        }
        return sb.ToString();
    }
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

internal class ClaudeRequest
{
    public string Model { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public string? System { get; set; }
    public List<object> Messages { get; set; } = new();
}

internal class ClaudeMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

internal class ClaudeResponse
{
    public List<ClaudeContentBlock> Content { get; set; } = new();
    public ClaudeUsage? Usage { get; set; }
}

internal class ClaudeUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}

internal class ClaudeContentBlock
{
    public string Type { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
