using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReconciliationDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookSlug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DivergenceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Predicate = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    WinningSourceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WinningValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DecisionReasoning = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DecisionConfidence = table.Column<double>(type: "float", nullable: false),
                    LosingClaimUidsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EditMechanism = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EditTargetJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreEditSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DryRun = table.Column<bool>(type: "bit", nullable: false),
                    Reverted = table.Column<bool>(type: "bit", nullable: false),
                    RevertedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationDecisions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationDecisions_BookSlug",
                table: "ReconciliationDecisions",
                column: "BookSlug");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationDecisions_EntityId_Predicate",
                table: "ReconciliationDecisions",
                columns: new[] { "EntityId", "Predicate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationDecisions_Reverted",
                table: "ReconciliationDecisions",
                column: "Reverted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReconciliationDecisions");
        }
    }
}
