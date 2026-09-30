namespace AgentInfrastructure.Entities;

public class Spec
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public AgentSession Session { get; set; } = null!;
    public List<SpecTask> Tasks { get; set; } = new();
}
