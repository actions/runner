using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Runner.Server.Migrations
{
    public partial class Workflows5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Result",
                table: "Job",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobOutput",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobOutput", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobOutput_Job_JobId",
                        column: x => x.JobId,
                        principalTable: "Job",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobOutput_JobId",
                table: "JobOutput",
                column: "JobId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobOutput");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "Job");
        }
    }
}
