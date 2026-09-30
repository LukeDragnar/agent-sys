using System.Text.Json;

namespace AgentInfrastructure.Clients;

public class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object InputSchema { get; set; } = new();
}

public class ToolUseBlock
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public JsonElement Input { get; set; }
}

public class ClaudeToolResult
{
    public string? Text { get; set; }
    public string StopReason { get; set; } = string.Empty;
    public List<ToolUseBlock> ToolCalls { get; set; } = new();
    public List<object> RawContentBlocks { get; set; } = new();
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}
