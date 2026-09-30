using AgentInfrastructure.Data;
using AgentInfrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentInfrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AgentDbContext _db;

    public UserRepository(AgentDbContext db) => _db = db;

    public Task<AppUser?> GetByUsername(string username) =>
        _db.Users.FirstOrDefaultAsync(x => x.Username == username);

    public async Task<AppUser?> GetById(Guid id) =>
        await _db.Users.FindAsync(id);

    public Task<List<AppUser>> GetAll() =>
        _db.Users.OrderBy(x => x.Username).ToListAsync();

    public async Task<AppUser> Create(string username, string passwordHash, string role, int weeklyTokenLimit)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = passwordHash,
            Role = role,
            WeeklyTokenLimit = weeklyTokenLimit,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task Update(AppUser user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is not null)
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }
    }
}
