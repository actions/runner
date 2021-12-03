using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Runner.Server.Migrations
{
    public partial class PersistentJobs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArtifactContainer_WorkflowRunAttempt_AttemptId",
                table: "ArtifactContainer");

            migrationBuilder.DropForeignKey(
                name: "FK_ArtifactFileContainer_ArtifactContainer_ContainerId",
                table: "ArtifactFileContainer");

            migrationBuilder.DropForeignKey(
                name: "FK_ArtifactRecord_ArtifactFileContainer_FileContainerId",
                table: "ArtifactRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_Job_WorkflowRunAttempt_WorkflowRunAttemptId",
                table: "Job");

            migrationBuilder.DropForeignKey(
                name: "FK_JobOutput_Job_JobId",
                table: "JobOutput");

            migrationBuilder.DropForeignKey(
                name: "FK_TimelineRecord_Job_JobId",
                table: "TimelineRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_TimelineRecord_TaskLogReference_LogId",
                table: "TimelineRecord");

            migrationBuilder.DropForeignKey(
                name: "FK_TimelineRecord_TimelineReference_DetailsId",
                table: "TimelineRecord");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TimelineRecord",
                table: "TimelineRecord");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Job",
                table: "Job");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArtifactRecord",
                table: "ArtifactRecord");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArtifactContainer",
                table: "ArtifactContainer");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ArtifactContainer");

            migrationBuilder.RenameTable(
                name: "TimelineRecord",
                newName: "TimeLineRecords");

            migrationBuilder.RenameTable(
                name: "Job",
                newName: "Jobs");

            migrationBuilder.RenameTable(
                name: "ArtifactRecord",
                newName: "ArtifactRecords");

            migrationBuilder.RenameTable(
                name: "ArtifactContainer",
                newName: "Artifacts");

            migrationBuilder.RenameIndex(
                name: "IX_TimelineRecord_LogId",
                table: "TimeLineRecords",
                newName: "IX_TimeLineRecords_LogId");

            migrationBuilder.RenameIndex(
                name: "IX_TimelineRecord_JobId",
                table: "TimeLineRecords",
                newName: "IX_TimeLineRecords_JobId");

            migrationBuilder.RenameIndex(
                name: "IX_TimelineRecord_DetailsId",
                table: "TimeLineRecords",
                newName: "IX_TimeLineRecords_DetailsId");

            migrationBuilder.RenameIndex(
                name: "IX_Job_WorkflowRunAttemptId",
                table: "Jobs",
                newName: "IX_Jobs_WorkflowRunAttemptId");

            migrationBuilder.RenameIndex(
                name: "IX_ArtifactRecord_FileContainerId",
                table: "ArtifactRecords",
                newName: "IX_ArtifactRecords_FileContainerId");

            migrationBuilder.RenameIndex(
                name: "IX_ArtifactContainer_AttemptId",
                table: "Artifacts",
                newName: "IX_Artifacts_AttemptId");

            migrationBuilder.AddColumn<long>(
                name: "RepositoryId",
                table: "Workflows",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TimeLineId",
                table: "WorkflowRunAttempt",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "WorkflowRun",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "WorkflowRun",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "TaskLogReference",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "TaskLogReference",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IndexLocation",
                table: "TaskLogReference",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastChangedOn",
                table: "TaskLogReference",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LineCount",
                table: "TaskLogReference",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "TaskLogReference",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RepositoryId",
                table: "Pools",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Size",
                table: "ArtifactFileContainer",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Matrix",
                table: "Jobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkflowIdentifier",
                table: "Jobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreName",
                table: "ArtifactRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TimeLineRecords",
                table: "TimeLineRecords",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Jobs",
                table: "Jobs",
                column: "JobId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArtifactRecords",
                table: "ArtifactRecords",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Artifacts",
                table: "Artifacts",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Caches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: true),
                    Repo = table.Column<string>(type: "TEXT", nullable: true),
                    Ref = table.Column<string>(type: "TEXT", nullable: true),
                    Storage = table.Column<string>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    RefId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Logs_TaskLogReference_RefId",
                        column: x => x.RefId,
                        principalTable: "TaskLogReference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Repository",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    PrivateKey = table.Column<string>(type: "TEXT", nullable: true),
                    PublicKey = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repository", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Repository_Owner_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Owner",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_RepositoryId",
                table: "Workflows",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Pools_RepositoryId",
                table: "Pools",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_RefId",
                table: "Logs",
                column: "RefId");

            migrationBuilder.CreateIndex(
                name: "IX_Repository_OwnerId",
                table: "Repository",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArtifactFileContainer_Artifacts_ContainerId",
                table: "ArtifactFileContainer",
                column: "ContainerId",
                principalTable: "Artifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ArtifactRecords_ArtifactFileContainer_FileContainerId",
                table: "ArtifactRecords",
                column: "FileContainerId",
                principalTable: "ArtifactFileContainer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Artifacts_WorkflowRunAttempt_AttemptId",
                table: "Artifacts",
                column: "AttemptId",
                principalTable: "WorkflowRunAttempt",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobOutput_Jobs_JobId",
                table: "JobOutput",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_WorkflowRunAttempt_WorkflowRunAttemptId",
                table: "Jobs",
                column: "WorkflowRunAttemptId",
                principalTable: "WorkflowRunAttempt",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pools_Repository_RepositoryId",
                table: "Pools",
                column: "RepositoryId",
                principalTable: "Repository",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeLineRecords_Jobs_JobId",
                table: "TimeLineRecords",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeLineRecords_TaskLogReference_LogId",
                table: "TimeLineRecords",
                column: "LogId",
                principalTable: "TaskLogReference",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TimeLineRecords_TimelineReference_DetailsId",
                table: "TimeLineRecords",
                column: "DetailsId",
                principalTable: "TimelineReference",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Workflows_Repository_RepositoryId",
                table: "Workflows",
                column: "RepositoryId",
                principalTable: "Repository",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArtifactFileContainer_Artifacts_ContainerId",
                table: "ArtifactFileContainer");

            migrationBuilder.DropForeignKey(
                name: "FK_ArtifactRecords_ArtifactFileContainer_FileContainerId",
                table: "ArtifactRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Artifacts_WorkflowRunAttempt_AttemptId",
                table: "Artifacts");

            migrationBuilder.DropForeignKey(
                name: "FK_JobOutput_Jobs_JobId",
                table: "JobOutput");

            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_WorkflowRunAttempt_WorkflowRunAttemptId",
                table: "Jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_Pools_Repository_RepositoryId",
                table: "Pools");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeLineRecords_Jobs_JobId",
                table: "TimeLineRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeLineRecords_TaskLogReference_LogId",
                table: "TimeLineRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_TimeLineRecords_TimelineReference_DetailsId",
                table: "TimeLineRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_Workflows_Repository_RepositoryId",
                table: "Workflows");

            migrationBuilder.DropTable(
                name: "Caches");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "Repository");

            migrationBuilder.DropIndex(
                name: "IX_Workflows_RepositoryId",
                table: "Workflows");

            migrationBuilder.DropIndex(
                name: "IX_Pools_RepositoryId",
                table: "Pools");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TimeLineRecords",
                table: "TimeLineRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Jobs",
                table: "Jobs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Artifacts",
                table: "Artifacts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArtifactRecords",
                table: "ArtifactRecords");

            migrationBuilder.DropColumn(
                name: "RepositoryId",
                table: "Workflows");

            migrationBuilder.DropColumn(
                name: "TimeLineId",
                table: "WorkflowRunAttempt");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "WorkflowRun");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "WorkflowRun");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "TaskLogReference");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "TaskLogReference");

            migrationBuilder.DropColumn(
                name: "IndexLocation",
                table: "TaskLogReference");

            migrationBuilder.DropColumn(
                name: "LastChangedOn",
                table: "TaskLogReference");

            migrationBuilder.DropColumn(
                name: "LineCount",
                table: "TaskLogReference");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "TaskLogReference");

            migrationBuilder.DropColumn(
                name: "RepositoryId",
                table: "Pools");

            migrationBuilder.DropColumn(
                name: "Size",
                table: "ArtifactFileContainer");

            migrationBuilder.DropColumn(
                name: "Matrix",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WorkflowIdentifier",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "StoreName",
                table: "ArtifactRecords");

            migrationBuilder.RenameTable(
                name: "TimeLineRecords",
                newName: "TimelineRecord");

            migrationBuilder.RenameTable(
                name: "Jobs",
                newName: "Job");

            migrationBuilder.RenameTable(
                name: "Artifacts",
                newName: "ArtifactContainer");

            migrationBuilder.RenameTable(
                name: "ArtifactRecords",
                newName: "ArtifactRecord");

            migrationBuilder.RenameIndex(
                name: "IX_TimeLineRecords_LogId",
                table: "TimelineRecord",
                newName: "IX_TimelineRecord_LogId");

            migrationBuilder.RenameIndex(
                name: "IX_TimeLineRecords_JobId",
                table: "TimelineRecord",
                newName: "IX_TimelineRecord_JobId");

            migrationBuilder.RenameIndex(
                name: "IX_TimeLineRecords_DetailsId",
                table: "TimelineRecord",
                newName: "IX_TimelineRecord_DetailsId");

            migrationBuilder.RenameIndex(
                name: "IX_Jobs_WorkflowRunAttemptId",
                table: "Job",
                newName: "IX_Job_WorkflowRunAttemptId");

            migrationBuilder.RenameIndex(
                name: "IX_Artifacts_AttemptId",
                table: "ArtifactContainer",
                newName: "IX_ArtifactContainer_AttemptId");

            migrationBuilder.RenameIndex(
                name: "IX_ArtifactRecords_FileContainerId",
                table: "ArtifactRecord",
                newName: "IX_ArtifactRecord_FileContainerId");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ArtifactContainer",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TimelineRecord",
                table: "TimelineRecord",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Job",
                table: "Job",
                column: "JobId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArtifactContainer",
                table: "ArtifactContainer",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArtifactRecord",
                table: "ArtifactRecord",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ArtifactContainer_WorkflowRunAttempt_AttemptId",
                table: "ArtifactContainer",
                column: "AttemptId",
                principalTable: "WorkflowRunAttempt",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ArtifactFileContainer_ArtifactContainer_ContainerId",
                table: "ArtifactFileContainer",
                column: "ContainerId",
                principalTable: "ArtifactContainer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ArtifactRecord_ArtifactFileContainer_FileContainerId",
                table: "ArtifactRecord",
                column: "FileContainerId",
                principalTable: "ArtifactFileContainer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Job_WorkflowRunAttempt_WorkflowRunAttemptId",
                table: "Job",
                column: "WorkflowRunAttemptId",
                principalTable: "WorkflowRunAttempt",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobOutput_Job_JobId",
                table: "JobOutput",
                column: "JobId",
                principalTable: "Job",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TimelineRecord_Job_JobId",
                table: "TimelineRecord",
                column: "JobId",
                principalTable: "Job",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TimelineRecord_TaskLogReference_LogId",
                table: "TimelineRecord",
                column: "LogId",
                principalTable: "TaskLogReference",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TimelineRecord_TimelineReference_DetailsId",
                table: "TimelineRecord",
                column: "DetailsId",
                principalTable: "TimelineReference",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
