namespace AgentInfrastructure.Entities;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ProfileJson { get; set; }
    public DateTime? LastScannedAt { get; set; }
    public Guid? ScannedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    public List<UserProjectConfig> UserConfigs { get; set; } = new();
}
