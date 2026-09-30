using AgentInfrastructure.Data;
using AgentInfrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentInfrastructure.Repositories;

public class AgentRepository : IAgentRepository
{
    private readonly AgentDbContext _db;

    public AgentRepository(AgentDbContext db)
    {
        _db = db;
    }

    public async Task<AgentSession> CreateSession(string title, Guid userId, string gitUser = "")
    {
        var session = new AgentSession
        {
            Id = Guid.NewGuid(),
            Title = title,
            AgentType = "BA",
            UserId = userId,
            GitUser = gitUser,
            CreatedAt = DateTime.UtcNow
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task AddMessage(Guid sessionId, string role, string content)
    {
        var msg = new AgentMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();
    }

    public async Task<List<AgentMessage>> GetMessages(Guid sessionId)
    {
        return await _db.Messages
            .Where(x => x.SessionId == sessionId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<AgentSession>> GetSessionsByUser(Guid userId)
    {
        return await _db.Sessions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<AgentSession>> GetAllSessions()
    {
        return await _db.Sessions
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteSession(Guid sessionId)
    {
        var messages = _db.Messages.Where(x => x.SessionId == sessionId);
        _db.Messages.RemoveRange(messages);

        var specs = _db.Specs.Where(x => x.SessionId == sessionId);
        _db.Specs.RemoveRange(specs);

        var session = await _db.Sessions.FindAsync(sessionId);
        if (session != null) _db.Sessions.Remove(session);

        await _db.SaveChangesAsync();
    }

    public async Task RenameSession(Guid sessionId, string newTitle)
    {
        var session = await _db.Sessions.FindAsync(sessionId);
        if (session == null) return;
        session.Title = newTitle;
        await _db.SaveChangesAsync();
    }

    public async Task SetFavorite(Guid sessionId, bool isFavorite)
    {
        var session = await _db.Sessions.FindAsync(sessionId);
        if (session == null) return;
        session.IsFavorite = isFavorite;
        await _db.SaveChangesAsync();
    }

    public async Task SetArchived(Guid sessionId, bool isArchived)
    {
        var session = await _db.Sessions.FindAsync(sessionId);
        if (session == null) return;
        session.IsArchived = isArchived;
        await _db.SaveChangesAsync();
    }
}
