using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNodeStructuralBlueprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsensusCliches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Device = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FirstFlaggedInSlug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FlagCount = table.Column<int>(type: "int", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsensusCliches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NodeStructuralBlueprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HasSubplot = table.Column<bool>(type: "bit", nullable: false),
                    SubplotSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubplotTheme = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TemporalScheme = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnachronyPlan = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolutionMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResolutionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MoralPolarity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MoralPolarityNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EscalationCurveJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventTypePaletteJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FormDevice = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EndingStyle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NoEpilogue = table.Column<bool>(type: "bit", nullable: false),
                    EndingNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IntertextualAnchorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeStructuralBlueprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeStructuralBlueprints_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeStructuralBlueprintBeatTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlueprintId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeStructuralBlueprintBeatTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeStructuralBlueprintBeatTags_Beats_BeatId",
                        column: x => x.BeatId,
                        principalTable: "Beats",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NodeStructuralBlueprintBeatTags_NodeStructuralBlueprints_BlueprintId",
                        column: x => x.BlueprintId,
                        principalTable: "NodeStructuralBlueprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsensusCliches_UniverseId",
                table: "ConsensusCliches",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeStructuralBlueprintBeatTags_BeatId",
                table: "NodeStructuralBlueprintBeatTags",
                column: "BeatId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeStructuralBlueprintBeatTags_BlueprintId",
                table: "NodeStructuralBlueprintBeatTags",
                column: "BlueprintId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeStructuralBlueprints_NodeId",
                table: "NodeStructuralBlueprints",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NodeStructuralBlueprints_UniverseId",
                table: "NodeStructuralBlueprints",
                column: "UniverseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsensusCliches");

            migrationBuilder.DropTable(
                name: "NodeStructuralBlueprintBeatTags");

            migrationBuilder.DropTable(
                name: "NodeStructuralBlueprints");
        }
    }
}
