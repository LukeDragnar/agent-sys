using AgentInfrastructure.Entities;

namespace AgentInfrastructure.Clients;

public class FakeClaudeClient : IClaudeClient
{
    public Task<ClaudeResult> Chat(List<AgentMessage> messages, string? systemSuffix = null)
    {
        var last = messages.LastOrDefault()?.Content;
        return Task.FromResult(new ClaudeResult
        {
            Text = $"[Fake] Nhận: {last}",
            InputTokens = 100,
            OutputTokens = 50
        });
    }

    public Task<ClaudeResult> ChatWithPrompt(string systemPrompt, string userMessage)
    {
        return Task.FromResult(new ClaudeResult
        {
            Text = "[]",
            InputTokens = 100,
            OutputTokens = 50
        });
    }

    public Task<ClaudeToolResult> ChatWithTools(List<object> messages, string systemPrompt, List<ToolDefinition> tools)
    {
        return Task.FromResult(new ClaudeToolResult
        {
            Text = "[Fake] Tool-enabled response",
            StopReason = "end_turn",
            InputTokens = 100,
            OutputTokens = 50
        });
    }
}
