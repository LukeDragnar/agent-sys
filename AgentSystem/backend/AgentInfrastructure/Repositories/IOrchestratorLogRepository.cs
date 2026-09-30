using AgentInfrastructure.Entities;

namespace AgentInfrastructure.Repositories;

public interface IOrchestratorLogRepository
{
    Task Add(OrchestratorLog log);
    Task<List<OrchestratorLog>> GetAll(string? agentType = null);
}
