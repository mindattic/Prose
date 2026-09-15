using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddObligationInstruments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalibrationInjections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriorText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sentence = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    PayoffBeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PayoffPriorText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayoffSentence = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Seed = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationInjections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NarrativeHealthSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TakenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BookTextHash = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    TotalBeats = table.Column<int>(type: "int", nullable: false),
                    ExaminedBeats = table.Column<int>(type: "int", nullable: false),
                    Open = table.Column<int>(type: "int", nullable: false),
                    Overdue = table.Column<int>(type: "int", nullable: false),
                    Closed = table.Column<int>(type: "int", nullable: false),
                    Dropped = table.Column<int>(type: "int", nullable: false),
                    Deferred = table.Column<int>(type: "int", nullable: false),
                    PayoffCoveragePct = table.Column<double>(type: "float", nullable: false),
                    UnnamedReferentDensity10k = table.Column<double>(type: "float", nullable: false),
                    ConsistencyErrorDensity10k = table.Column<double>(type: "float", nullable: false),
                    UnentailedRecordClaimPct = table.Column<double>(type: "float", nullable: true),
                    InstrumentVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarrativeHealthSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObligationJudgeCache",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObligationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateBeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateTextHash = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PromptVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Relation = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Quote = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObligationJudgeCache", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationInjections_NodeId",
                table: "CalibrationInjections",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeHealthSnapshots_NodeId_TakenAt",
                table: "NarrativeHealthSnapshots",
                columns: new[] { "NodeId", "TakenAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ObligationJudgeCache_ObligationId_CandidateBeatId_CandidateTextHash_PromptVersion",
                table: "ObligationJudgeCache",
                columns: new[] { "ObligationId", "CandidateBeatId", "CandidateTextHash", "PromptVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalibrationInjections");

            migrationBuilder.DropTable(
                name: "NarrativeHealthSnapshots");

            migrationBuilder.DropTable(
                name: "ObligationJudgeCache");
        }
    }
}
