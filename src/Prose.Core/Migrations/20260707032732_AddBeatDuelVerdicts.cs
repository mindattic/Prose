using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddBeatDuelVerdicts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BeatDuelVerdicts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalHash = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    RevisionHash = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Verdict = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RoundsRun = table.Column<int>(type: "int", nullable: false),
                    BetterVotes = table.Column<int>(type: "int", nullable: false),
                    WorseVotes = table.Column<int>(type: "int", nullable: false),
                    SameVotes = table.Column<int>(type: "int", nullable: false),
                    BallotsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatDuelVerdicts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeatDuelVerdicts_BeatId",
                table: "BeatDuelVerdicts",
                column: "BeatId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatDuelVerdicts_OriginalHash_RevisionHash",
                table: "BeatDuelVerdicts",
                columns: new[] { "OriginalHash", "RevisionHash" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeatDuelVerdicts");
        }
    }
}
