using AgentInfrastructure.Data;
using AgentInfrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentInfrastructure.Repositories;

public class OrchestratorLogRepository : IOrchestratorLogRepository
{
    private readonly AgentDbContext _db;

    public OrchestratorLogRepository(AgentDbContext db)
    {
        _db = db;
    }

    public async Task Add(OrchestratorLog log)
    {
        log.Id = Guid.NewGuid();
        log.CreatedAt = DateTime.UtcNow;
        _db.OrchestratorLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public Task<List<OrchestratorLog>> GetAll(string? agentType = null)
    {
        var q = _db.OrchestratorLogs.AsQueryable();
        if (!string.IsNullOrEmpty(agentType))
            q = q.Where(x => x.AgentType == agentType);
        return q.OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync();
    }
}
