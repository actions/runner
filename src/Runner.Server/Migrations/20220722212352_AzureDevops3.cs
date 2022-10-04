using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Runner.Server.Migrations
{
    public partial class AzureDevops3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimelineIssues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecordId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    IsInfrastructureIssue = table.Column<bool>(type: "INTEGER", nullable: true),
                    Data = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimelineIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimelineIssues_TimeLineRecords_RecordId",
                        column: x => x.RecordId,
                        principalTable: "TimeLineRecords",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TimelineVariables",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RecordId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimelineVariables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimelineVariables_TimeLineRecords_RecordId",
                        column: x => x.RecordId,
                        principalTable: "TimeLineRecords",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimelineIssues_RecordId",
                table: "TimelineIssues",
                column: "RecordId");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineVariables_RecordId",
                table: "TimelineVariables",
                column: "RecordId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimelineIssues");

            migrationBuilder.DropTable(
                name: "TimelineVariables");
        }
    }
}
