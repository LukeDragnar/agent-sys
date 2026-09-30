namespace AgentInfrastructure.Entities;

public class OrchestratorLog
{
    public Guid Id { get; set; }
    public string AgentType { get; set; } = string.Empty;   // BA | DEV | QA | SPLIT | FIX_COMPILE | GIT_PUSH
    public string Input { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public DateTime CreatedAt { get; set; }
}
