using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Runner.Server.Migrations
{
    public partial class Workflows : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Owner",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    PrivateKey = table.Column<string>(type: "TEXT", nullable: true),
                    PublicKey = table.Column<string>(type: "TEXT", nullable: true),
                    Token = table.Column<string>(type: "TEXT", nullable: true),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Owner", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OwnerPool",
                columns: table => new
                {
                    OwnersId = table.Column<long>(type: "INTEGER", nullable: false),
                    PoolsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnerPool", x => new { x.OwnersId, x.PoolsId });
                    table.ForeignKey(
                        name: "FK_OwnerPool_Owner_OwnersId",
                        column: x => x.OwnersId,
                        principalTable: "Owner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OwnerPool_Pools_PoolsId",
                        column: x => x.PoolsId,
                        principalTable: "Pools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowRun",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkflowId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowRun", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowRun_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowRunAttempt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkflowRunId = table.Column<int>(type: "INTEGER", nullable: true),
                    Attempt = table.Column<int>(type: "INTEGER", nullable: false),
                    EventName = table.Column<string>(type: "TEXT", nullable: true),
                    EventPayload = table.Column<string>(type: "TEXT", nullable: true),
                    Workflow = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowRunAttempt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowRunAttempt_WorkflowRun_WorkflowRunId",
                        column: x => x.WorkflowRunId,
                        principalTable: "WorkflowRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Job",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestId = table.Column<long>(type: "INTEGER", nullable: false),
                    TimeLineId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    repo = table.Column<string>(type: "TEXT", nullable: true),
                    name = table.Column<string>(type: "TEXT", nullable: true),
                    workflowname = table.Column<string>(type: "TEXT", nullable: true),
                    runid = table.Column<long>(type: "INTEGER", nullable: false),
                    Cancelled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TimeoutMinutes = table.Column<double>(type: "REAL", nullable: false),
                    CancelTimeoutMinutes = table.Column<double>(type: "REAL", nullable: false),
                    ContinueOnError = table.Column<bool>(type: "INTEGER", nullable: false),
                    WorkflowRunAttemptId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Job", x => x.JobId);
                    table.ForeignKey(
                        name: "FK_Job_WorkflowRunAttempt_WorkflowRunAttemptId",
                        column: x => x.WorkflowRunAttemptId,
                        principalTable: "WorkflowRunAttempt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Job_WorkflowRunAttemptId",
                table: "Job",
                column: "WorkflowRunAttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnerPool_PoolsId",
                table: "OwnerPool",
                column: "PoolsId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRun_WorkflowId",
                table: "WorkflowRun",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRunAttempt_WorkflowRunId",
                table: "WorkflowRunAttempt",
                column: "WorkflowRunId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Job");

            migrationBuilder.DropTable(
                name: "OwnerPool");

            migrationBuilder.DropTable(
                name: "WorkflowRunAttempt");

            migrationBuilder.DropTable(
                name: "Owner");

            migrationBuilder.DropTable(
                name: "WorkflowRun");

            migrationBuilder.DropTable(
                name: "Workflows");
        }
    }
}
