using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Runner.Host.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskAgentPool",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AutoProvision = table.Column<bool>(type: "INTEGER", nullable: true),
                    AutoSize = table.Column<bool>(type: "INTEGER", nullable: true),
                    TargetSize = table.Column<int>(type: "INTEGER", nullable: true),
                    AgentCloudId = table.Column<int>(type: "INTEGER", nullable: true),
                    Scope = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    IsHosted = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsInternal = table.Column<bool>(type: "INTEGER", nullable: false),
                    PoolType = table.Column<int>(type: "INTEGER", nullable: false),
                    Size = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLegacy = table.Column<bool>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAgentPool", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskAgentReference",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    OSDescription = table.Column<string>(type: "TEXT", nullable: true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ProvisioningState = table.Column<string>(type: "TEXT", nullable: true),
                    AccessPoint = table.Column<string>(type: "TEXT", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", nullable: false),
                    MaxParallelism = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StatusChangedOn = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAgentReference", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pools",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskAgentPoolId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pools_TaskAgentPool_TaskAgentPoolId",
                        column: x => x.TaskAgentPoolId,
                        principalTable: "TaskAgentPool",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentLabel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    TaskAgentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentLabel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentLabel_TaskAgentReference_TaskAgentId",
                        column: x => x.TaskAgentId,
                        principalTable: "TaskAgentReference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PoolId = table.Column<int>(type: "INTEGER", nullable: true),
                    TaskAgentId = table.Column<int>(type: "INTEGER", nullable: true),
                    Exponent = table.Column<byte[]>(type: "BLOB", nullable: true),
                    Modulus = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agents_Pools_PoolId",
                        column: x => x.PoolId,
                        principalTable: "Pools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Agents_TaskAgentReference_TaskAgentId",
                        column: x => x.TaskAgentId,
                        principalTable: "TaskAgentReference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentLabel_TaskAgentId",
                table: "AgentLabel",
                column: "TaskAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_PoolId",
                table: "Agents",
                column: "PoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_TaskAgentId",
                table: "Agents",
                column: "TaskAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Pools_TaskAgentPoolId",
                table: "Pools",
                column: "TaskAgentPoolId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentLabel");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "Pools");

            migrationBuilder.DropTable(
                name: "TaskAgentReference");

            migrationBuilder.DropTable(
                name: "TaskAgentPool");
        }
    }
}
