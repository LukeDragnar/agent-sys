namespace AgentInfrastructure.Entities;

public class SpecTaskVersion
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string JsonSpec { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public SpecTask Task { get; set; } = null!;
}
