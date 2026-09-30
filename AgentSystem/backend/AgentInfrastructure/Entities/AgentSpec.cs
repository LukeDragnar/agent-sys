namespace AgentInfrastructure.Entities;

public class AgentSpec
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JsonSpec { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }

    public AgentSession Session { get; set; } = null!;
}
