using AgentInfrastructure.Entities;

namespace AgentCore.Services;

public interface IProjectService
{
    Task<List<Project>> GetAll();
    Task<Project?> GetById(Guid id);
    Task<Project> Create(string name, string description, Guid userId);
    Task<Project?> Update(Guid id, string name, string description);
    Task Delete(Guid id);

    /// <summary>Scan BE/FE source, extract conventions, save ProfileJson to project.</summary>
    Task<string> ScanAsync(Guid projectId, string bePath, string fePath, Guid userId);

    Task<UserProjectConfig?> GetUserConfig(Guid projectId, Guid userId);
    Task<UserProjectConfig> UpsertUserConfig(Guid projectId, Guid userId, string bePath, string fePath, string outputDir, string? modulesJson = null);

    /// <summary>Build a concise text summary of the project profile for use in prompts.</summary>
    string BuildProfilePrompt(string? profileJson);
}
