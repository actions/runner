using Microsoft.EntityFrameworkCore.Migrations;

namespace Runner.Server.Migrations
{
    public partial class Workflows2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArtifactContainer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AttemptId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtifactContainer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtifactContainer_WorkflowRunAttempt_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "WorkflowRunAttempt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArtifactFileContainer",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ContainerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtifactFileContainer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtifactFileContainer_ArtifactContainer_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "ArtifactContainer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArtifactRecord",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileContainerId = table.Column<int>(type: "INTEGER", nullable: true),
                    FileName = table.Column<string>(type: "TEXT", nullable: true),
                    GZip = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtifactRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArtifactRecord_ArtifactFileContainer_FileContainerId",
                        column: x => x.FileContainerId,
                        principalTable: "ArtifactFileContainer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactContainer_AttemptId",
                table: "ArtifactContainer",
                column: "AttemptId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactFileContainer_ContainerId",
                table: "ArtifactFileContainer",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtifactRecord_FileContainerId",
                table: "ArtifactRecord",
                column: "FileContainerId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtifactRecord");

            migrationBuilder.DropTable(
                name: "ArtifactFileContainer");

            migrationBuilder.DropTable(
                name: "ArtifactContainer");
        }
    }
}
