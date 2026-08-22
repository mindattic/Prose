using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddDcmRunAndBeatSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DcmBeatSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatIndex = table.Column<int>(type: "int", nullable: false),
                    BeatId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BeatTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMs = table.Column<double>(type: "float", nullable: false),
                    ProseChars = table.Column<int>(type: "int", nullable: false),
                    DocsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullActiveSetJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DcmBeatSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DcmRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeSlug = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DocContextEnabled = table.Column<bool>(type: "bit", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BaselineScore = table.Column<double>(type: "float", nullable: false),
                    BaselineFlow = table.Column<double>(type: "float", nullable: false),
                    FinalScore = table.Column<double>(type: "float", nullable: false),
                    FinalFlow = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DcmRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DcmBeatSnapshots_RunId",
                table: "DcmBeatSnapshots",
                column: "RunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DcmBeatSnapshots");

            migrationBuilder.DropTable(
                name: "DcmRuns");
        }
    }
}
