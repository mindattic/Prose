using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddLlmCallStageAndBeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "LlmPromptCaptures",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BeatId",
                table: "LlmCallHistories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ElapsedMs",
                table: "LlmCallHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "LlmCallHistories",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BeatWriteStageLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Ordinal = table.Column<int>(type: "int", nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ElapsedMs = table.Column<int>(type: "int", nullable: false),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    WrittenAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatWriteStageLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LlmCallHistories_BeatId",
                table: "LlmCallHistories",
                column: "BeatId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatWriteStageLogs_BeatId",
                table: "BeatWriteStageLogs",
                column: "BeatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeatWriteStageLogs");

            migrationBuilder.DropIndex(
                name: "IX_LlmCallHistories_BeatId",
                table: "LlmCallHistories");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "LlmPromptCaptures");

            migrationBuilder.DropColumn(
                name: "BeatId",
                table: "LlmCallHistories");

            migrationBuilder.DropColumn(
                name: "ElapsedMs",
                table: "LlmCallHistories");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "LlmCallHistories");
        }
    }
}
