using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeConvergenceState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NodeConvergenceStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsecutiveDryRounds = table.Column<int>(type: "int", nullable: false),
                    TotalRoundsRun = table.Column<int>(type: "int", nullable: false),
                    LastBookFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastRoundAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeConvergenceStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeConvergenceStates_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NodeConvergenceStates_NodeId",
                table: "NodeConvergenceStates",
                column: "NodeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NodeConvergenceStates");
        }
    }
}
