using Microsoft.EntityFrameworkCore.Migrations;

namespace Runner.Server.Migrations
{
    public partial class AdvancedWorkflowRunProperties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ref",
                table: "WorkflowRunAttempt",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Result",
                table: "WorkflowRunAttempt",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sha",
                table: "WorkflowRunAttempt",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ref",
                table: "WorkflowRunAttempt");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "WorkflowRunAttempt");

            migrationBuilder.DropColumn(
                name: "Sha",
                table: "WorkflowRunAttempt");
        }
    }
}
