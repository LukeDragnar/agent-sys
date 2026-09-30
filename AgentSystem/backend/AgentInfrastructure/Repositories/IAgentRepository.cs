using AgentInfrastructure.Entities;

namespace AgentInfrastructure.Repositories;

public interface IAgentRepository
{
    Task<AgentSession> CreateSession(string title, Guid userId, string gitUser = "");
    Task AddMessage(Guid sessionId, string role, string content);
    Task<List<AgentMessage>> GetMessages(Guid sessionId);
    Task<List<AgentSession>> GetSessionsByUser(Guid userId);
    Task<List<AgentSession>> GetAllSessions();
    Task DeleteSession(Guid sessionId);
    Task RenameSession(Guid sessionId, string newTitle);
    Task SetFavorite(Guid sessionId, bool isFavorite);
    Task SetArchived(Guid sessionId, bool isArchived);
}
