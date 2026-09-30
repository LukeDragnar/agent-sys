using AgentInfrastructure.Entities;

namespace AgentInfrastructure.Clients;

public interface IClaudeClient
{
    Task<ClaudeResult> Chat(List<AgentMessage> messages, string? systemSuffix = null);
    Task<ClaudeResult> ChatWithPrompt(string systemPrompt, string userMessage);
    Task<ClaudeToolResult> ChatWithTools(List<object> messages, string systemPrompt, List<ToolDefinition> tools);
}
