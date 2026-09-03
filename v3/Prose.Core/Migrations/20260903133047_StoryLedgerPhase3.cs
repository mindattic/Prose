using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class StoryLedgerPhase3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Provenance",
                table: "Entities",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "inferred");

            migrationBuilder.AddColumn<string>(
                name: "Provenance",
                table: "CharacterRelationships",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "inferred");

            migrationBuilder.CreateIndex(
                name: "IX_Entities_Universe_Provenance",
                table: "Entities",
                columns: new[] { "UniverseId", "Provenance" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelationships_Provenance",
                table: "CharacterRelationships",
                column: "Provenance");

            // ── Grandfather every pre-existing row ───────────────────────────
            // AddColumn's defaultValue backfills existing rows as "inferred", which would be a
            // false claim: we do not know how these came to be believed. Author ruling for this
            // program, applied identically to ContinuityClaims in Phase 2 — grandfather existing
            // rows as "legacy-unknown", then flag only the suspicious ones. An unknown grade is
            // not evidence of a defect, and treating the whole corpus as suspect would bury the
            // rows that genuinely are.
            //
            // Runs once, at migration time, so only rows that predate the column are touched;
            // "inferred" stays the correct default for everything inserted afterwards. Both
            // tables are system-versioned, so this writes one history row per row — that is the
            // point: the grandfathering is itself auditable and reversible.
            migrationBuilder.Sql("UPDATE [Entities] SET [Provenance] = 'legacy-unknown';");
            migrationBuilder.Sql("UPDATE [CharacterRelationships] SET [Provenance] = 'legacy-unknown';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Entities_Universe_Provenance",
                table: "Entities");

            migrationBuilder.DropIndex(
                name: "IX_CharacterRelationships_Provenance",
                table: "CharacterRelationships");

            migrationBuilder.DropColumn(
                name: "Provenance",
                table: "Entities");

            migrationBuilder.DropColumn(
                name: "Provenance",
                table: "CharacterRelationships");
        }
    }
}
