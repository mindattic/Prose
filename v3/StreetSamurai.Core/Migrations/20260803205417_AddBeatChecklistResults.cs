using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddBeatChecklistResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BeatChecklistResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatTextHash = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RuleSetVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PassFraction = table.Column<double>(type: "float", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatChecklistResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeatChecklistResults_Beats_BeatId",
                        column: x => x.BeatId,
                        principalTable: "Beats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeatChecklistResults_BeatId",
                table: "BeatChecklistResults",
                column: "BeatId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatChecklistResults_NodeId_BeatId",
                table: "BeatChecklistResults",
                columns: new[] { "NodeId", "BeatId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeatChecklistResults");
        }
    }
}
