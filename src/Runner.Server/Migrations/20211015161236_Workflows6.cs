using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Runner.Server.Migrations
{
    public partial class Workflows6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskLogReference",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Location = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskLogReference", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimelineReference",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChangeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimelineReference", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimelineRecord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TimelineId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RecordType = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinishTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CurrentOperation = table.Column<string>(type: "TEXT", nullable: true),
                    PercentComplete = table.Column<int>(type: "INTEGER", nullable: true),
                    State = table.Column<int>(type: "INTEGER", nullable: true),
                    Result = table.Column<int>(type: "INTEGER", nullable: true),
                    ResultCode = table.Column<string>(type: "TEXT", nullable: true),
                    ChangeId = table.Column<int>(type: "INTEGER", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WorkerName = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: true),
                    RefName = table.Column<string>(type: "TEXT", nullable: true),
                    LogId = table.Column<int>(type: "INTEGER", nullable: true),
                    DetailsId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: true),
                    WarningCount = table.Column<int>(type: "INTEGER", nullable: true),
                    NoticeCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Location = table.Column<string>(type: "TEXT", nullable: true),
                    Attempt = table.Column<int>(type: "INTEGER", nullable: false),
                    Identifier = table.Column<string>(type: "TEXT", nullable: true),
                    AgentPlatform = table.Column<string>(type: "TEXT", nullable: true),
                    JobId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimelineRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimelineRecord_Job_JobId",
                        column: x => x.JobId,
                        principalTable: "Job",
                        principalColumn: "JobId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimelineRecord_TaskLogReference_LogId",
                        column: x => x.LogId,
                        principalTable: "TaskLogReference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimelineRecord_TimelineReference_DetailsId",
                        column: x => x.DetailsId,
                        principalTable: "TimelineReference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimelineRecord_DetailsId",
                table: "TimelineRecord",
                column: "DetailsId");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineRecord_JobId",
                table: "TimelineRecord",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineRecord_LogId",
                table: "TimelineRecord",
                column: "LogId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimelineRecord");

            migrationBuilder.DropTable(
                name: "TaskLogReference");

            migrationBuilder.DropTable(
                name: "TimelineReference");
        }
    }
}
