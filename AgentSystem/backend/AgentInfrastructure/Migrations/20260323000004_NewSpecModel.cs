using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentInfrastructure.Migrations;

public partial class NewSpecModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Drop old Specs table safely (any index name variant)
        migrationBuilder.Sql(@"
            DECLARE @sql NVARCHAR(MAX) = '';
            SELECT @sql += 'DROP INDEX [' + i.name + '] ON [Specs]; '
            FROM sys.indexes i
            JOIN sys.objects o ON i.object_id = o.object_id
            WHERE o.name = 'Specs' AND i.type > 0 AND i.is_primary_key = 0;
            IF LEN(@sql) > 0 EXEC sp_executesql @sql;

            IF OBJECT_ID('SpecTaskVersions','U') IS NOT NULL DROP TABLE [SpecTaskVersions];
            IF OBJECT_ID('SpecTasks','U') IS NOT NULL DROP TABLE [SpecTasks];
            IF OBJECT_ID('Specs','U') IS NOT NULL DROP TABLE [Specs];
        ");

        // Create Specs (module level)
        migrationBuilder.CreateTable(
            name: "Specs",
            columns: table => new
            {
                Id          = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SessionId   = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name        = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                CreatedAt   = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Specs", x => x.Id);
            });

        // Create SpecTasks (task/screen level)
        migrationBuilder.CreateTable(
            name: "SpecTasks",
            columns: table => new
            {
                Id          = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SpecId      = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name        = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                CreatedAt   = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SpecTasks", x => x.Id);
                table.ForeignKey(
                    name: "FK_SpecTasks_Specs_SpecId",
                    column: x => x.SpecId,
                    principalTable: "Specs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create SpecTaskVersions
        migrationBuilder.CreateTable(
            name: "SpecTaskVersions",
            columns: table => new
            {
                Id        = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TaskId    = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                JsonSpec  = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Version   = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SpecTaskVersions", x => x.Id);
                table.ForeignKey(
                    name: "FK_SpecTaskVersions_SpecTasks_TaskId",
                    column: x => x.TaskId,
                    principalTable: "SpecTasks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SpecTasks_SpecId",
            table: "SpecTasks",
            column: "SpecId");

        migrationBuilder.CreateIndex(
            name: "IX_SpecTaskVersions_TaskId_Version",
            table: "SpecTaskVersions",
            columns: new[] { "TaskId", "Version" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SpecTaskVersions");
        migrationBuilder.DropTable(name: "SpecTasks");
        migrationBuilder.DropTable(name: "Specs");
    }
}
