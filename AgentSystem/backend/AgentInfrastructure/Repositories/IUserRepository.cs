using AgentInfrastructure.Entities;

namespace AgentInfrastructure.Repositories;

public interface IUserRepository
{
    Task<AppUser?> GetByUsername(string username);
    Task<AppUser?> GetById(Guid id);
    Task<List<AppUser>> GetAll();
    Task<AppUser> Create(string username, string passwordHash, string role, int weeklyTokenLimit);
    Task Update(AppUser user);
    Task Delete(Guid id);
}
