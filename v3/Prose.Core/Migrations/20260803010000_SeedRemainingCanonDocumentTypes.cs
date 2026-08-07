using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Prose.Core.Data;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <summary>
    /// Seeds the five <c>CanonDocumentTypes</c> rows that existed only in developer databases.
    ///
    /// <para><b>Why this exists.</b> <c>20260730173650_AddCanonDocumentTypes</c> seeded four
    /// legacy types (WorldBible, WorldMaster, Franchise, UniverseCanon). Every type added since —
    /// UniverseCraft, CraftGuide, CharacterDoctrine, DelightGuide, and EngineGuide — was inserted
    /// by hand-written SQL against a local database and never committed. There is no code path
    /// that creates a <c>CanonDocumentTypes</c> row: <c>--generate-canon-md</c> only iterates
    /// already-migrated documents, and <c>CanonDocumentService.UpsertSectionAsync</c> returns
    /// <c>document_not_found</c> when the row is absent. Combined with every generated doc being
    /// gitignored (SS-A45), a fresh clone or CI could not produce <c>docs/CRAFT.md</c>,
    /// <c>docs/DELIGHT.md</c>, <c>docs/CHARACTER.md</c>, <c>docs/GLMZ.md</c>, <c>docs/SCRY.md</c>,
    /// <c>docs/SOURCE.md</c> or <c>docs/ENGINE.md</c> at all — the entire craft layer plus the
    /// tier-0 engine doc were machine-local state.</para>
    ///
    /// <para>Values are the exact literals live in the authoring database, so this is a no-op there
    /// (guarded by NOT EXISTS) and reproduces that state exactly anywhere else. One deliberate
    /// change: EngineGuide gains <c>scope: *</c>, which it was created without — every other
    /// always-tier doc carries it (<c>docs/BIBLE.digest.md</c> has <c>"*"</c>), and without it
    /// <c>SyncFromCanonDbAsync</c> defaults the scope to <c>""</c>, which fails
    /// <c>ScopeMatches</c> the moment any scope filtering is applied to the always pass.</para>
    ///
    /// <para>This seeds type CONFIGURATION only, not document content. Canon prose lives in
    /// <c>CanonDocumentSections</c> and remains DB-only by design (SS-LAW-1) — that content is not
    /// reproducible from the repo, which is a separate and pre-existing concern.</para>
    /// </summary>
    /// <inheritdoc />
    [DbContext(typeof(ProseDbContext))]
    [Migration("20260803010000_SeedRemainingCanonDocumentTypes")]
    public partial class SeedRemainingCanonDocumentTypes : Migration
    {
        // Raw strings so the embedded newline in ExtraFrontMatter is explicit and LF, matching
        // what CanonDocumentTypeRegistry.GetFrontMatterAsync appends verbatim to the YAML block.
        private const string EngineExtra =
            "tier: always\nscope: *\ntriggers: engine, audit, sweep, finding, beat, sortkey, enforce, rule, defect, continuity";
        private const string UniverseCraftExtra =
            "tier: series\nrelated: docs/CRAFT.md";
        private const string CraftExtra =
            "tier: topic\ntriggers: prose, beat, write, voice, sentence, dialogue, interiority, scene, pov, character, narrat";
        private const string CharacterExtra =
            "tier: topic\ntriggers: character, cast, protagonist, antagonist, relationship, interpersonal, dialogue, motive, arc, growth, behavior, pov, depth";
        private const string DelightExtra =
            "tier: topic\ntriggers: prose, beat, write, delight, love, praise, positive, craft, move, reader, score";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Seed(migrationBuilder, "EngineGuide",       "docs/ENGINE.md",    "ENGINE - What the Engine Enforces", "base",     "engine",   EngineExtra,        0);
            Seed(migrationBuilder, "UniverseCraft",     "docs/{slug}.md",    "{name} - Universe Craft Rules",     "universe", "universe", UniverseCraftExtra, 45);
            Seed(migrationBuilder, "CraftGuide",        "docs/CRAFT.md",     "CRAFT - Universal Prose Rules",     "base",     "craft",    CraftExtra,         50);
            Seed(migrationBuilder, "CharacterDoctrine", "docs/CHARACTER.md", "CHARACTER - The Character Doctrine", "base",    "craft",    CharacterExtra,     55);
            Seed(migrationBuilder, "DelightGuide",      "docs/DELIGHT.md",   "DELIGHT - What Readers Love",       "base",     "craft",    DelightExtra,       60);

            // Bring an already-present EngineGuide row (the authoring DB) up to the same shape,
            // since the NOT EXISTS guard above skips it there.
            migrationBuilder.Sql($"""
                UPDATE [CanonDocumentTypes]
                   SET [ExtraFrontMatter] = '{EngineExtra.Replace("'", "''")}',
                       [UpdatedAt]        = SYSUTCDATETIME()
                 WHERE [DocumentType] = 'EngineGuide'
                   AND ([ExtraFrontMatter] IS NULL OR [ExtraFrontMatter] NOT LIKE '%scope: *%');
                """);
        }

        private static void Seed(
            MigrationBuilder mb, string documentType, string pathTemplate, string titleTemplate,
            string scope, string layer, string extraFrontMatter, int sortKey) =>
            mb.Sql($"""
                IF NOT EXISTS (SELECT 1 FROM [CanonDocumentTypes] WHERE [DocumentType] = '{documentType}')
                INSERT INTO [CanonDocumentTypes]
                    ([DocumentType], [PathTemplate], [TitleTemplate], [Scope], [FrontMatterLayer],
                     [ExtraFrontMatter], [SortKey], [IsActive], [CreatedAt], [UpdatedAt])
                VALUES
                    ('{documentType}', '{pathTemplate}', '{titleTemplate.Replace("'", "''")}',
                     '{scope}', '{layer}', '{extraFrontMatter.Replace("'", "''")}',
                     {sortKey}, 1, SYSUTCDATETIME(), SYSUTCDATETIME());
                """);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only remove rows that no CanonDocuments row depends on — the FK added in
            // 20260730173650 is Restrict, so deleting a type still in use would fail the rollback.
            migrationBuilder.Sql("""
                DELETE FROM [CanonDocumentTypes]
                 WHERE [DocumentType] IN ('EngineGuide','UniverseCraft','CraftGuide','CharacterDoctrine','DelightGuide')
                   AND [DocumentType] NOT IN (SELECT DISTINCT [DocumentType] FROM [CanonDocuments]);
                """);
        }
    }
}
