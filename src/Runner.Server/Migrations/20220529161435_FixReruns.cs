using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Runner.Server.Migrations
{
    public partial class FixReruns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArtifactsMinAttempt",
                table: "WorkflowRunAttempt",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArtifactsMinAttempt",
                table: "WorkflowRunAttempt");
        }
    }
}
