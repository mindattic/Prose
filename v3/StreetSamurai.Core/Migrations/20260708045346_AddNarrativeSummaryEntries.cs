using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNarrativeSummaryEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NarrativeSummaryEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortKey = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarrativeSummaryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NarrativeSummaryEntries_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeSummaryEntries_NodeId_SortKey",
                table: "NarrativeSummaryEntries",
                columns: new[] { "NodeId", "SortKey" });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NarrativeSummaryEntries");
        }
    }
}
