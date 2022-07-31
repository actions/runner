using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Runner.Server.Migrations
{
    public partial class AzureDevops : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Capability",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    System = table.Column<bool>(type: "INTEGER", nullable: false),
                    AgentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Capability_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_AgentId",
                table: "Capability",
                column: "AgentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Capability");
        }
    }
}
