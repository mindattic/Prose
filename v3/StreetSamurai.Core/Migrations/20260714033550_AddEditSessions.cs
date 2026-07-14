using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEditSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Confirmed",
                table: "NodeStructuralBlueprintBeatTags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "NodeStructuralBlueprintBeatTags",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConfirmedBySessionId",
                table: "NodeStructuralBlueprintBeatTags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EditSessions",
                columns: table => new
                {
                    EditSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SessionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "custom"),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BeatCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditSessions", x => x.EditSessionId);
                    table.ForeignKey(
                        name: "FK_EditSessions_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EditSessionBeats",
                columns: table => new
                {
                    EditSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    PriorVersion = table.Column<int>(type: "int", nullable: false),
                    PriorTextHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditSessionBeats", x => new { x.EditSessionId, x.BeatId });
                    table.ForeignKey(
                        name: "FK_EditSessionBeats_Beats_BeatId",
                        column: x => x.BeatId,
                        principalTable: "Beats",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EditSessionBeats_EditSessions_EditSessionId",
                        column: x => x.EditSessionId,
                        principalTable: "EditSessions",
                        principalColumn: "EditSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EditSessionBeats_BeatId",
                table: "EditSessionBeats",
                column: "BeatId");

            migrationBuilder.CreateIndex(
                name: "IX_EditSessions_NodeId",
                table: "EditSessions",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EditSessions_NodeId_ClosedAt",
                table: "EditSessions",
                columns: new[] { "NodeId", "ClosedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EditSessionBeats");

            migrationBuilder.DropTable(
                name: "EditSessions");

            migrationBuilder.DropColumn(
                name: "Confirmed",
                table: "NodeStructuralBlueprintBeatTags");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "NodeStructuralBlueprintBeatTags");

            migrationBuilder.DropColumn(
                name: "ConfirmedBySessionId",
                table: "NodeStructuralBlueprintBeatTags");
        }
    }
}
