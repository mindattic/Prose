using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddContinuityCompatibilityJudgments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TriggeredBy",
                table: "ReconciliationDecisions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ContinuityCompatibilityJudgments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Predicate = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ObjectSetHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reasoning = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClassifiedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContinuityCompatibilityJudgments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContinuityExtractionCursors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookSlug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SourceKind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastExtractedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContinuityExtractionCursors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContinuityCompatibilityJudgments_EntityId_Predicate_ObjectSetHash",
                table: "ContinuityCompatibilityJudgments",
                columns: new[] { "EntityId", "Predicate", "ObjectSetHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContinuityExtractionCursors_BookSlug_SourceKind_SourceKey",
                table: "ContinuityExtractionCursors",
                columns: new[] { "BookSlug", "SourceKind", "SourceKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContinuityCompatibilityJudgments");

            migrationBuilder.DropTable(
                name: "ContinuityExtractionCursors");

            migrationBuilder.DropColumn(
                name: "TriggeredBy",
                table: "ReconciliationDecisions");
        }
    }
}
