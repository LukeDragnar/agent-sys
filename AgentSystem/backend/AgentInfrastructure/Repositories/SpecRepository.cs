using AgentInfrastructure.Data;
using AgentInfrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentInfrastructure.Repositories;

public class SpecRepository : ISpecRepository
{
    private readonly AgentDbContext _db;
    public SpecRepository(AgentDbContext db) { _db = db; }

    public async Task<Spec> GetOrCreateSpec(Guid sessionId, string moduleName, string description)
    {
        var existing = await _db.Specs
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.Name == moduleName);
        if (existing != null) return existing;

        var spec = new Spec
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = moduleName,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
        _db.Specs.Add(spec);
        await _db.SaveChangesAsync();
        return spec;
    }

    public async Task<List<Spec>> GetAllSpecs(Guid sessionId)
    {
        return await _db.Specs
            .Where(s => s.SessionId == sessionId)
            .Include(s => s.Tasks)
                .ThenInclude(t => t.Versions)
            .OrderBy(s => s.CreatedAt)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task DeleteSpec(Guid specId)
    {
        var spec = await _db.Specs.FindAsync(specId);
        if (spec is null) return;
        _db.Specs.Remove(spec);
        await _db.SaveChangesAsync();
    }

    public async Task<SpecTask> GetOrCreateTask(Guid specId, string taskName, string description)
    {
        var existing = await _db.SpecTasks
            .FirstOrDefaultAsync(t => t.SpecId == specId && t.Name == taskName);
        if (existing != null) return existing;

        var task = new SpecTask
        {
            Id = Guid.NewGuid(),
            SpecId = specId,
            Name = taskName,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
        _db.SpecTasks.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task DeleteTask(Guid taskId)
    {
        var task = await _db.SpecTasks.FindAsync(taskId);
        if (task is null) return;
        _db.SpecTasks.Remove(task);
        await _db.SaveChangesAsync();
    }

    public async Task<SpecTaskVersion> SaveVersion(Guid taskId, string jsonSpec)
    {
        var maxVersion = await _db.SpecTaskVersions
            .Where(v => v.TaskId == taskId)
            .Select(v => (int?)v.Version)
            .MaxAsync() ?? 0;

        var version = new SpecTaskVersion
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            JsonSpec = jsonSpec,
            Version = maxVersion + 1,
            CreatedAt = DateTime.UtcNow
        };
        _db.SpecTaskVersions.Add(version);
        await _db.SaveChangesAsync();
        return version;
    }

    public async Task<SpecTaskVersion?> GetVersionById(Guid versionId)
        => await _db.SpecTaskVersions
            .Include(v => v.Task)
                .ThenInclude(t => t.Spec)
            .FirstOrDefaultAsync(v => v.Id == versionId);

    public async Task<SpecTaskVersion?> GetVersionByNumber(Guid taskId, int versionNumber)
        => await _db.SpecTaskVersions
            .Include(v => v.Task)
                .ThenInclude(t => t.Spec)
            .FirstOrDefaultAsync(v => v.TaskId == taskId && v.Version == versionNumber);

    public async Task<SpecTaskVersion?> GetLatestVersionInSession(Guid sessionId)
        => await _db.SpecTaskVersions
            .Include(v => v.Task)
                .ThenInclude(t => t.Spec)
            .Where(v => v.Task.Spec.SessionId == sessionId)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<SpecTaskVersion?> UpdateVersion(Guid versionId, string jsonSpec)
    {
        var version = await _db.SpecTaskVersions.FindAsync(versionId);
        if (version is null) return null;
        version.JsonSpec = jsonSpec;
        await _db.SaveChangesAsync();
        return version;
    }

    public async Task DeleteVersion(Guid versionId)
    {
        var version = await _db.SpecTaskVersions.FindAsync(versionId);
        if (version is null) return;
        _db.SpecTaskVersions.Remove(version);
        await _db.SaveChangesAsync();
    }
}
