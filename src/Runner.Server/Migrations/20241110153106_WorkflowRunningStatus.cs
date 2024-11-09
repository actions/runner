using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Runner.Server.Migrations
{
    public partial class WorkflowRunningStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "WorkflowRunAttempt",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Jobs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "WorkflowRunAttempt");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Jobs");
        }
    }
}
