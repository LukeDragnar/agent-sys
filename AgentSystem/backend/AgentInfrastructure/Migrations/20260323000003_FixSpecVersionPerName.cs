using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentInfrastructure.Migrations;

public partial class FixSpecVersionPerName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Specs_SessionId_Version",
            table: "Specs");

        migrationBuilder.CreateIndex(
            name: "IX_Specs_SessionId_Name_Version",
            table: "Specs",
            columns: new[] { "SessionId", "Name", "Version" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Specs_SessionId_Name_Version",
            table: "Specs");

        migrationBuilder.CreateIndex(
            name: "IX_Specs_SessionId_Version",
            table: "Specs",
            columns: new[] { "SessionId", "Version" },
            unique: true);
    }
}
