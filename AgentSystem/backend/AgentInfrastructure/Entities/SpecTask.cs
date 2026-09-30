namespace AgentInfrastructure.Entities;

public class SpecTask
{
    public Guid Id { get; set; }
    public Guid SpecId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Spec Spec { get; set; } = null!;
    public List<SpecTaskVersion> Versions { get; set; } = new();
}
