using System;
using AgentInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AgentInfrastructure.Migrations;

[DbContext(typeof(AgentDbContext))]
partial class AgentDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.3")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

        modelBuilder.Entity("AgentInfrastructure.Entities.AgentMessage", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<DateTime>("CreatedAt").IsRequired().HasColumnType("datetime2");
            b.Property<string>("Content").IsRequired().HasColumnType("nvarchar(max)");
            b.Property<string>("Role").IsRequired().HasMaxLength(50).HasColumnType("nvarchar(50)");
            b.Property<Guid>("SessionId").HasColumnType("uniqueidentifier");
            b.HasKey("Id");
            b.HasIndex("SessionId");
            b.ToTable("Messages");
        });

        modelBuilder.Entity("AgentInfrastructure.Entities.AgentSession", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<string>("AgentType").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            b.Property<DateTime>("CreatedAt").IsRequired().HasColumnType("datetime2");
            b.Property<string>("Title").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.HasKey("Id");
            b.ToTable("Sessions");
        });

        modelBuilder.Entity("AgentInfrastructure.Entities.Spec", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<Guid>("SessionId").HasColumnType("uniqueidentifier");
            b.Property<string>("Name").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("Description").IsRequired().HasMaxLength(1000).HasColumnType("nvarchar(1000)").HasDefaultValue("");
            b.Property<DateTime>("CreatedAt").IsRequired().HasColumnType("datetime2");
            b.HasKey("Id");
            b.ToTable("Specs");
        });

        modelBuilder.Entity("AgentInfrastructure.Entities.SpecTask", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<Guid>("SpecId").HasColumnType("uniqueidentifier");
            b.Property<string>("Name").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            b.Property<string>("Description").IsRequired().HasMaxLength(1000).HasColumnType("nvarchar(1000)").HasDefaultValue("");
            b.Property<DateTime>("CreatedAt").IsRequired().HasColumnType("datetime2");
            b.HasKey("Id");
            b.HasIndex("SpecId");
            b.ToTable("SpecTasks");
        });

        modelBuilder.Entity("AgentInfrastructure.Entities.SpecTaskVersion", b =>
        {
            b.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            b.Property<Guid>("TaskId").HasColumnType("uniqueidentifier");
            b.Property<string>("JsonSpec").IsRequired().HasColumnType("nvarchar(max)");
            b.Property<int>("Version").HasColumnType("int");
            b.Property<DateTime>("CreatedAt").IsRequired().HasColumnType("datetime2");
            b.HasKey("Id");
            b.HasIndex("TaskId", "Version").IsUnique();
            b.ToTable("SpecTaskVersions");
        });

        modelBuilder.Entity("AgentInfrastructure.Entities.AgentMessage", b =>
        {
            b.HasOne("AgentInfrastructure.Entities.AgentSession", "Session")
                .WithMany("Messages")
                .HasForeignKey("SessionId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            b.Navigation("Session");
        });

        modelBuilder.Entity("AgentInfrastructure.Entities.Spec", b =>
        {
            b.HasOne("AgentInfrastructure.Entities.AgentSession", "Session")
                .WithMany()
                .HasForeignKey("SessionId")
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired();
            b.Navigation("Session");
        });

        modelBuilder.Entity("AgentInfrastructure.Entities.SpecTask", b =>
        {
            b.HasOne("AgentInfrastructure.Entities.Spec", "Spec")
                .WithMany("Tasks")
                .HasForeignKey("SpecId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            b.Navigation("Spec");
        });

        modelBuilder.Entity("AgentInfrastructure.Entities.SpecTaskVersion", b =>
        {
            b.HasOne("AgentInfrastructure.Entities.SpecTask", "Task")
                .WithMany("Versions")
                .HasForeignKey("TaskId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
            b.Navigation("Task");
        });

        modelBuilder.Entity("AgentInfrastructure.Entities.AgentSession", b =>
        {
            b.Navigation("Messages");
        });

        modelBuilder.Entity("AgentInfrastructure.Entities.Spec", b =>
        {
            b.Navigation("Tasks");
        });

        modelBuilder.Entity("AgentInfrastructure.Entities.SpecTask", b =>
        {
            b.Navigation("Versions");
        });
#pragma warning restore 612, 618
    }
}
