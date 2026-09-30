using AgentInfrastructure.Data;
using AgentInfrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentInfrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AgentDbContext _db;
    public ProjectRepository(AgentDbContext db) { _db = db; }

    public Task<List<Project>> GetAll()
        => _db.Projects.OrderBy(p => p.Name).ToListAsync();

    public Task<Project?> GetById(Guid id)
        => _db.Projects.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Project> Create(string name, string description, Guid userId)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    public async Task<Project?> Update(Guid id, string name, string description)
    {
        var p = await _db.Projects.FindAsync(id);
        if (p == null) return null;
        p.Name = name;
        p.Description = description;
        await _db.SaveChangesAsync();
        return p;
    }

    public async Task UpdateProfile(Guid id, string profileJson, Guid scannedBy)
    {
        var p = await _db.Projects.FindAsync(id);
        if (p == null) return;
        p.ProfileJson = profileJson;
        p.LastScannedAt = DateTime.UtcNow;
        p.ScannedByUserId = scannedBy;
        await _db.SaveChangesAsync();
    }

    public async Task Delete(Guid id)
    {
        var p = await _db.Projects.FindAsync(id);
        if (p == null) return;
        _db.Projects.Remove(p);
        await _db.SaveChangesAsync();
    }

    public Task<UserProjectConfig?> GetUserConfig(Guid projectId, Guid userId)
        => _db.UserProjectConfigs
            .FirstOrDefaultAsync(c => c.ProjectId == projectId && c.UserId == userId);

    public async Task<UserProjectConfig> UpsertUserConfig(Guid projectId, Guid userId, string bePath, string fePath, string outputDir, string? modulesJson = null)
    {
        var config = await _db.UserProjectConfigs
            .FirstOrDefaultAsync(c => c.ProjectId == projectId && c.UserId == userId);

        if (config == null)
        {
            config = new UserProjectConfig { Id = Guid.NewGuid(), UserId = userId, ProjectId = projectId };
            _db.UserProjectConfigs.Add(config);
        }

        config.LocalBePath = bePath;
        config.LocalFePath = fePath;
        config.LocalOutputDir = outputDir;
        if (modulesJson != null) config.LocalModulesJson = modulesJson;
        config.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return config;
    }
}
