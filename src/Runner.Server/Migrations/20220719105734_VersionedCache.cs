using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Runner.Server.Migrations
{
    public partial class VersionedCache : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "Caches",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Caches");
        }
    }
}
