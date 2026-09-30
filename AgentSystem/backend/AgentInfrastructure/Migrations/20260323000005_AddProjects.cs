using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentInfrastructure.Migrations;

public partial class AddProjects : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Projects",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Name = table.Column<string>(maxLength: 200, nullable: false),
                Description = table.Column<string>(maxLength: 1000, nullable: false, defaultValue: ""),
                ProfileJson = table.Column<string>(nullable: true),
                LastScannedAt = table.Column<DateTime>(nullable: true),
                ScannedByUserId = table.Column<Guid>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                CreatedByUserId = table.Column<Guid>(nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Projects", x => x.Id));

        migrationBuilder.CreateTable(
            name: "UserProjectConfigs",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                UserId = table.Column<Guid>(nullable: false),
                ProjectId = table.Column<Guid>(nullable: false),
                LocalBePath = table.Column<string>(nullable: false, defaultValue: ""),
                LocalFePath = table.Column<string>(nullable: false, defaultValue: ""),
                LocalOutputDir = table.Column<string>(nullable: false, defaultValue: ""),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserProjectConfigs", x => x.Id);
                table.ForeignKey("FK_UserProjectConfigs_Projects_ProjectId",
                    x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_UserProjectConfigs_Users_UserId",
                    x => x.UserId, "Users", "Id", onDelete: ReferentialAction.NoAction);
            });

        migrationBuilder.CreateIndex("IX_UserProjectConfigs_UserId_ProjectId",
            "UserProjectConfigs", new[] { "UserId", "ProjectId" }, unique: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ProjectId",
            table: "Sessions",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("ProjectId", "Sessions");
        migrationBuilder.DropTable("UserProjectConfigs");
        migrationBuilder.DropTable("Projects");
    }
}
