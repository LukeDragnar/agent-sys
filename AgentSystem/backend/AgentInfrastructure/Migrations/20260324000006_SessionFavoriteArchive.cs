using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentInfrastructure.Migrations;

public partial class SessionFavoriteArchive : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsFavorite",
            table: "Sessions",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "IsArchived",
            table: "Sessions",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsFavorite", table: "Sessions");
        migrationBuilder.DropColumn(name: "IsArchived", table: "Sessions");
    }
}
