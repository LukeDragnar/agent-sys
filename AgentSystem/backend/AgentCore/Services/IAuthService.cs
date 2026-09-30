using AgentInfrastructure.Entities;

namespace AgentCore.Services;

public interface IAuthService
{
    Task<string?> Login(string username, string password);
    Task<AppUser?> GetById(Guid id);
}
