namespace AgentInfrastructure.Clients;

public class ClaudeResult
{
    public string Text { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}
