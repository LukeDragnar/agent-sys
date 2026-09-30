using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentInfrastructure.Migrations;

public partial class AddAgentSpec : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Specs",
            columns: table => new
            {
                Id          = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SessionId   = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name        = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                JsonSpec    = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                Version     = table.Column<int>(type: "int", nullable: false),
                CreatedAt   = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Specs", x => x.Id);
                table.ForeignKey(
                    name: "FK_Specs_Sessions_SessionId",
                    column: x => x.SessionId,
                    principalTable: "Sessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Specs_SessionId_Version",
            table: "Specs",
            columns: new[] { "SessionId", "Version" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Specs");
    }
}
