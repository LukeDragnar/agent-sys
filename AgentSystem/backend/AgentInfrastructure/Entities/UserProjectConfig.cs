namespace AgentInfrastructure.Entities;

public class UserProjectConfig
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public string LocalBePath { get; set; } = string.Empty;
    public string LocalFePath { get; set; } = string.Empty;
    public string LocalOutputDir { get; set; } = string.Empty;
    /// <summary>JSON array: [{name, sourcePath, outputPath}] — sub-projects/modules</summary>
    public string? LocalModulesJson { get; set; }
    public DateTime UpdatedAt { get; set; }

    public AppUser User { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
