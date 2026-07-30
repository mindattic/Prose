using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCanonDocumentTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CanonDocumentTypes",
                columns: table => new
                {
                    DocumentType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PathTemplate = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    TitleTemplate = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FrontMatterLayer = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ExtraFrontMatter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortKey = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonDocumentTypes", x => x.DocumentType);
                });

            // Seed the 4 legacy types with the exact literal values the hardcoded dictionaries
            // in CanonDocumentService/CanonDocumentCli/MigrateCanonDocsCli/MarkdownFileService
            // used before this migration — required before the FK below (existing CanonDocuments
            // rows must resolve), and the Phase-1 acceptance bar is byte-identical generated output.
            var now = new DateTime(2026, 7, 30, 17, 36, 50, DateTimeKind.Utc);
            migrationBuilder.InsertData(
                table: "CanonDocumentTypes",
                columns: new[] { "DocumentType", "PathTemplate", "TitleTemplate", "Scope", "FrontMatterLayer", "ExtraFrontMatter", "SortKey", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { "WorldBible",    "docs/BIBLE.md",             "{name} World Bible",     "universe", "bible",     null, 10, true, now, now },
                    { "WorldMaster",   "docs/WORLD.md",             "{name} World Master",    "universe", "world",     null, 20, true, now, now },
                    { "Franchise",     "docs/FRANCHISE.md",         "{name} Franchise Bible", "universe", "franchise", null, 30, true, now, now },
                    { "UniverseCanon", "docs/universes/ENTOS.md",   "{name} Universe Canon",  "universe", "universe",  null, 40, true, now, now },
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanonDocuments_DocumentType",
                table: "CanonDocuments",
                column: "DocumentType");

            migrationBuilder.AddForeignKey(
                name: "FK_CanonDocuments_CanonDocumentTypes_DocumentType",
                table: "CanonDocuments",
                column: "DocumentType",
                principalTable: "CanonDocumentTypes",
                principalColumn: "DocumentType",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CanonDocuments_CanonDocumentTypes_DocumentType",
                table: "CanonDocuments");

            migrationBuilder.DropTable(
                name: "CanonDocumentTypes");

            migrationBuilder.DropIndex(
                name: "IX_CanonDocuments_DocumentType",
                table: "CanonDocuments");
        }
    }
}
