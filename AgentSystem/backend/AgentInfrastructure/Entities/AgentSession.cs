namespace AgentInfrastructure.Entities;

public class AgentSession
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public Guid? ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsArchived { get; set; }
    public string GitUser { get; set; } = string.Empty;

    public List<AgentMessage> Messages { get; set; } = new();
    public List<UsageLog> UsageLogs { get; set; } = new();
}
