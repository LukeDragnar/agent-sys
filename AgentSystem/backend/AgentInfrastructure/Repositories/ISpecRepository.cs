using AgentInfrastructure.Entities;

namespace AgentInfrastructure.Repositories;

public interface ISpecRepository
{
    // Module (Spec)
    Task<Spec> GetOrCreateSpec(Guid sessionId, string moduleName, string description);
    Task<List<Spec>> GetAllSpecs(Guid sessionId);
    Task DeleteSpec(Guid specId);

    // Task (SpecTask)
    Task<SpecTask> GetOrCreateTask(Guid specId, string taskName, string description);
    Task DeleteTask(Guid taskId);

    // Version (SpecTaskVersion)
    Task<SpecTaskVersion> SaveVersion(Guid taskId, string jsonSpec);
    Task<SpecTaskVersion?> GetVersionById(Guid versionId);
    Task<SpecTaskVersion?> GetVersionByNumber(Guid taskId, int versionNumber);
    Task<SpecTaskVersion?> GetLatestVersionInSession(Guid sessionId);
    Task<SpecTaskVersion?> UpdateVersion(Guid versionId, string jsonSpec);
    Task DeleteVersion(Guid versionId);
}
