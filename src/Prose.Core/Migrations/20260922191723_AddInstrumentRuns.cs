using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InstrumentRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Instrument = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemsExamined = table.Column<int>(type: "int", nullable: false),
                    ItemsTotal = table.Column<int>(type: "int", nullable: false),
                    BookFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FindingsFiled = table.Column<int>(type: "int", nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstrumentRuns_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentRuns_NodeId_Instrument_CompletedAt",
                table: "InstrumentRuns",
                columns: new[] { "NodeId", "Instrument", "CompletedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstrumentRuns");
        }
    }
}
