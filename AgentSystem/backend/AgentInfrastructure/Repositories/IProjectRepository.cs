using AgentInfrastructure.Entities;

namespace AgentInfrastructure.Repositories;

public interface IProjectRepository
{
    Task<List<Project>> GetAll();
    Task<Project?> GetById(Guid id);
    Task<Project> Create(string name, string description, Guid userId);
    Task<Project?> Update(Guid id, string name, string description);
    Task UpdateProfile(Guid id, string profileJson, Guid scannedBy);
    Task Delete(Guid id);

    Task<UserProjectConfig?> GetUserConfig(Guid projectId, Guid userId);
    Task<UserProjectConfig> UpsertUserConfig(Guid projectId, Guid userId, string bePath, string fePath, string outputDir, string? modulesJson = null);
}
