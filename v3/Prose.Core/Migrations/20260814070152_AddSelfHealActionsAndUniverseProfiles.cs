using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSelfHealActionsAndUniverseProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SelfHealActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TargetTable = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BeforeStateJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FindingId = table.Column<long>(type: "bigint", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UndoneAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelfHealActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UniverseProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetricKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SampleSize = table.Column<int>(type: "int", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniverseProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SelfHealActions_NodeId",
                table: "SelfHealActions",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SelfHealActions_RunId_Sequence",
                table: "SelfHealActions",
                columns: new[] { "RunId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_UniverseProfiles_UniverseId_MetricKey",
                table: "UniverseProfiles",
                columns: new[] { "UniverseId", "MetricKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SelfHealActions");

            migrationBuilder.DropTable(
                name: "UniverseProfiles");
        }
    }
}
