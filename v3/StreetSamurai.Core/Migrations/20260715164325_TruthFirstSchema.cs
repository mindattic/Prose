using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class TruthFirstSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BeatBlueprintDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BlueprintId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    EscalationFloor = table.Column<decimal>(type: "decimal(4,2)", nullable: true),
                    SubplotCarrier = table.Column<bool>(type: "bit", nullable: false),
                    AnachronyType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    DeclaredPurpose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorldStatePre = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorldStatePost = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PacingDirective = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatBlueprintDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeatBlueprintDecisions_Beats_BeatId",
                        column: x => x.BeatId,
                        principalTable: "Beats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BeatBlueprintDecisions_NodeStructuralBlueprints_BlueprintId",
                        column: x => x.BlueprintId,
                        principalTable: "NodeStructuralBlueprints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BeatVerifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CheckType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeatVerifications_Beats_BeatId",
                        column: x => x.BeatId,
                        principalTable: "Beats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CanonDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    LastChecksum = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EntityStateAtBeats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BeatId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    StateValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Inferred"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityStateAtBeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EntityStateAtBeats_Beats_BeatId",
                        column: x => x.BeatId,
                        principalTable: "Beats",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EntityStateAtBeats_Entities_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Entities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EntityStateAtBeats_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "NodeBibleSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeBibleSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeBibleSections_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CanonDocumentSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SectionTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortKey = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonDocumentSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanonDocumentSections_CanonDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "CanonDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BeatBlueprintDecisions_BlueprintId",
                table: "BeatBlueprintDecisions",
                column: "BlueprintId");

            migrationBuilder.CreateIndex(
                name: "UX_BeatBlueprintDecisions_Beat",
                table: "BeatBlueprintDecisions",
                column: "BeatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BeatVerifications_BeatId",
                table: "BeatVerifications",
                column: "BeatId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatVerifications_Result_Severity",
                table: "BeatVerifications",
                columns: new[] { "Result", "Severity" });

            migrationBuilder.CreateIndex(
                name: "UX_BeatVerifications_Beat_CheckType",
                table: "BeatVerifications",
                columns: new[] { "BeatId", "CheckType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CanonDocuments_UniverseId",
                table: "CanonDocuments",
                column: "UniverseId");

            migrationBuilder.CreateIndex(
                name: "UX_CanonDocuments_Universe_Type",
                table: "CanonDocuments",
                columns: new[] { "UniverseId", "DocumentType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CanonDocumentSections_DocumentId",
                table: "CanonDocumentSections",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CanonDocumentSections_DocumentId_SortKey",
                table: "CanonDocumentSections",
                columns: new[] { "DocumentId", "SortKey" });

            migrationBuilder.CreateIndex(
                name: "UX_CanonDocumentSections_Doc_Key",
                table: "CanonDocumentSections",
                columns: new[] { "DocumentId", "SectionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityStateAtBeats_BeatId",
                table: "EntityStateAtBeats",
                column: "BeatId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityStateAtBeats_EntityId",
                table: "EntityStateAtBeats",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityStateAtBeats_NodeId_BeatId",
                table: "EntityStateAtBeats",
                columns: new[] { "NodeId", "BeatId" });

            migrationBuilder.CreateIndex(
                name: "UX_EntityStateAtBeat_Entity_Beat_Type",
                table: "EntityStateAtBeats",
                columns: new[] { "EntityId", "BeatId", "StateType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NodeBibleSections_NodeId",
                table: "NodeBibleSections",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "UX_NodeBibleSections_Node_Type",
                table: "NodeBibleSections",
                columns: new[] { "NodeId", "SectionType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BeatBlueprintDecisions");

            migrationBuilder.DropTable(
                name: "BeatVerifications");

            migrationBuilder.DropTable(
                name: "CanonDocumentSections");

            migrationBuilder.DropTable(
                name: "EntityStateAtBeats");

            migrationBuilder.DropTable(
                name: "NodeBibleSections");

            migrationBuilder.DropTable(
                name: "CanonDocuments");
        }
    }
}
