using AgentInfrastructure.Entities;

namespace AgentCore.Services;

public class SpecQaResult
{
    public bool HasIssues { get; set; }
    public List<string> Issues { get; set; } = new();
    public string? ImprovedSpec { get; set; }
}

public interface IAgentService
{
    Task<AgentSession> CreateSession(string title, Guid userId, string gitUser = "");
    Task<string> Chat(Guid sessionId, string message, Guid userId, Guid? specId = null, Guid? projectId = null, List<string>? selectedModules = null, string? agentType = null, string? projectKey = null, string gitUser = "");
    Task<SpecQaResult> QaSpecAsync(Guid versionId, Guid userId, Guid? projectId = null, List<string>? selectedModules = null);
    Task<List<AgentMessage>> GetHistory(Guid sessionId);
    Task<List<AgentSession>> GetSessionsByUser(Guid userId);
    Task<List<AgentSession>> GetAllSessions();
    Task DeleteSession(Guid sessionId);
    Task RenameSession(Guid sessionId, string newTitle);
    Task SetFavorite(Guid sessionId, bool isFavorite);
    Task SetArchived(Guid sessionId, bool isArchived);
}
