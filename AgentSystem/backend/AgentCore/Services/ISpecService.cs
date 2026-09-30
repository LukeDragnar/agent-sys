using AgentInfrastructure.Clients;
using AgentInfrastructure.Entities;

namespace AgentCore.Services;

public interface ISpecService
{
    Task<SpecTaskVersion?> TryExtractAndSave(Guid sessionId, string response, string moduleName = "General");
    Task<List<SpecTaskVersion>> TryExtractSplitAndSave(Guid sessionId, string response, IClaudeClient claude, string moduleName = "General");
    Task TryMergeAndSave(Guid sessionId, SpecTaskVersion existing, string response);

    Task<SpecTaskVersion?> GetLatestVersion(Guid sessionId);
    Task<SpecTaskVersion?> GetVersionById(Guid versionId);
    Task<SpecTaskVersion?> GetVersionByNumber(Guid taskId, int versionNumber);
    Task<List<Spec>> GetAllSpecs(Guid sessionId);

    Task<SpecTaskVersion?> UpdateVersion(Guid versionId, string jsonSpec);
    Task DeleteVersion(Guid versionId);
    Task DeleteTask(Guid taskId);
    Task DeleteSpec(Guid specId);
}
