namespace AgentInfrastructure.Entities;

public class AgentMessage
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public AgentSession Session { get; set; } = null!;
}
