using AgentInfrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgentInfrastructure.Data;

public class AgentDbContext : DbContext
{
    public AgentDbContext(DbContextOptions<AgentDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AgentSession> Sessions => Set<AgentSession>();
    public DbSet<AgentMessage> Messages => Set<AgentMessage>();
    public DbSet<Spec> Specs => Set<Spec>();
    public DbSet<SpecTask> SpecTasks => Set<SpecTask>();
    public DbSet<SpecTaskVersion> SpecTaskVersions => Set<SpecTaskVersion>();
    public DbSet<UsageLog> UsageLogs => Set<UsageLog>();
    public DbSet<OrchestratorLog> OrchestratorLogs => Set<OrchestratorLog>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<UserProjectConfig> UserProjectConfigs => Set<UserProjectConfig>();

    // CodeMap
    public DbSet<CodeSymbol> CodeSymbols => Set<CodeSymbol>();
    public DbSet<CodeApiEndpoint> CodeApiEndpoints => Set<CodeApiEndpoint>();
    public DbSet<CodeDependency> CodeDependencies => Set<CodeDependency>();

    // Embedding
    public DbSet<CodeEmbedding> CodeEmbeddings => Set<CodeEmbedding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).IsRequired().HasMaxLength(200);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<AgentSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Title).IsRequired().HasMaxLength(500);
            e.Property(s => s.AgentType).IsRequired().HasMaxLength(100);
            e.Property(s => s.CreatedAt).IsRequired();

            e.HasMany(s => s.Messages)
             .WithOne(m => m.Session)
             .HasForeignKey(m => m.SessionId)
             .HasConstraintName(null)
             .OnDelete(DeleteBehavior.NoAction);

            e.HasMany(s => s.UsageLogs)
             .WithOne(u => u.Session)
             .HasForeignKey(u => u.SessionId)
             .HasConstraintName(null)
             .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AgentMessage>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Role).IsRequired().HasMaxLength(50);
            e.Property(m => m.Content).IsRequired();
            e.HasIndex(m => m.SessionId);
        });

        modelBuilder.Entity<Spec>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).IsRequired().HasMaxLength(500);
            e.Property(s => s.Description).HasMaxLength(1000).HasDefaultValue("");
            e.HasOne(s => s.Session)
             .WithMany()
             .HasForeignKey(s => s.SessionId)
             .HasConstraintName(null)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasMany(s => s.Tasks)
             .WithOne(t => t.Spec)
             .HasForeignKey(t => t.SpecId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SpecTask>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(500);
            e.Property(t => t.Description).HasMaxLength(1000).HasDefaultValue("");
            e.HasOne(t => t.Spec)
             .WithMany(s => s.Tasks)
             .HasForeignKey(t => t.SpecId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(t => t.Versions)
             .WithOne(v => v.Task)
             .HasForeignKey(v => v.TaskId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SpecTaskVersion>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.JsonSpec).IsRequired();
            e.HasOne(v => v.Task)
             .WithMany(t => t.Versions)
             .HasForeignKey(v => v.TaskId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(v => new { v.TaskId, v.Version }).IsUnique();
        });

        modelBuilder.Entity<OrchestratorLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AgentType).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.Property(p => p.Description).HasMaxLength(1000).HasDefaultValue("");
            e.HasMany(p => p.UserConfigs)
             .WithOne(c => c.Project)
             .HasForeignKey(c => c.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserProjectConfig>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.UserId, c.ProjectId }).IsUnique();
            e.HasOne(c => c.User)
             .WithMany()
             .HasForeignKey(c => c.UserId)
             .HasConstraintName(null)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(c => c.Project)
             .WithMany(p => p.UserConfigs)
             .HasForeignKey(c => c.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── CodeMap ──
        modelBuilder.Entity<CodeSymbol>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(500);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(1000);
            e.Property(x => x.SymbolType).IsRequired().HasMaxLength(50);
            e.Property(x => x.FilePath).IsRequired().HasMaxLength(1000);
            e.Property(x => x.Namespace).HasMaxLength(500);
            e.Property(x => x.Accessibility).HasMaxLength(50);
            e.Property(x => x.ReturnType).HasMaxLength(500);
            e.Property(x => x.Parameters).HasMaxLength(2000);
            e.Property(x => x.BaseTypes).HasMaxLength(1000);
            e.Property(x => x.ExtraInfo).HasColumnType("nvarchar(max)");

            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId)
             .HasConstraintName(null).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId)
             .HasConstraintName(null).OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(x => new { x.ProjectId, x.Name });
            e.HasIndex(x => new { x.ProjectId, x.SymbolType });
            e.HasIndex(x => x.FullName);
        });

        modelBuilder.Entity<CodeApiEndpoint>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.HttpMethod).IsRequired().HasMaxLength(10);
            e.Property(x => x.RouteTemplate).IsRequired().HasMaxLength(500);
            e.Property(x => x.ControllerName).IsRequired().HasMaxLength(200);
            e.Property(x => x.ActionName).IsRequired().HasMaxLength(200);
            e.Property(x => x.FilePath).IsRequired().HasMaxLength(1000);

            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId)
             .HasConstraintName(null).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Symbol).WithMany().HasForeignKey(x => x.SymbolId)
             .HasConstraintName(null).OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(x => new { x.ProjectId, x.RouteTemplate });
            e.HasIndex(x => new { x.ProjectId, x.HttpMethod });
        });

        modelBuilder.Entity<CodeDependency>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ToSymbolName).IsRequired().HasMaxLength(500);
            e.Property(x => x.DependencyType).IsRequired().HasMaxLength(50);

            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId)
             .HasConstraintName(null).OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.FromSymbol).WithMany().HasForeignKey(x => x.FromSymbolId)
             .HasConstraintName(null).OnDelete(DeleteBehavior.NoAction);

            e.HasOne(x => x.ToSymbol).WithMany().HasForeignKey(x => x.ToSymbolId)
             .HasConstraintName(null).OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(x => new { x.ProjectId, x.FromSymbolId });
            e.HasIndex(x => new { x.ProjectId, x.ToSymbolName });
        });

        // ── Embedding ──
        modelBuilder.Entity<CodeEmbedding>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FilePath).IsRequired().HasMaxLength(1000);
            e.Property(x => x.ChunkType).IsRequired().HasMaxLength(20);
            e.Property(x => x.SymbolName).HasMaxLength(500);
            e.Property(x => x.Content).HasColumnType("nvarchar(max)");
            e.Property(x => x.Vector).HasColumnType("varbinary(max)");
            e.Property(x => x.ContentHash).IsRequired().HasMaxLength(64);

            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId)
             .HasConstraintName(null).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.ProjectId, x.ContentHash });
            e.HasIndex(x => new { x.ProjectId, x.FilePath });
        });

        modelBuilder.Entity<AgentSession>(e2 =>
        {
            e2.Property(s => s.GitUser).HasMaxLength(200).HasDefaultValue("");
        });

        modelBuilder.Entity<UsageLog>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.GitUser).HasMaxLength(200).HasDefaultValue("");
            e.HasOne(u => u.User)
             .WithMany(a => a.UsageLogs)
             .HasForeignKey(u => u.UserId)
             .HasConstraintName(null)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(u => u.Session)
             .WithMany(s => s.UsageLogs)
             .HasForeignKey(u => u.SessionId)
             .HasConstraintName(null)
             .OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(u => new { u.UserId, u.IsoWeek, u.IsoYear });
            e.HasIndex(u => new { u.GitUser, u.IsoWeek, u.IsoYear });
        });
    }
}
