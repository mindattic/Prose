using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <summary>
    /// Adds the ordering constraint that makes the second declared built-in axiom family
    /// expressible, and seeds it.
    ///
    /// <para>The Story Ledger plan named three built-in axiom families. Only the first —
    /// "a constructed being has no biological parents" — was ever built, because it is the only
    /// one that is a statement about two predicates alone. "A dead character does not later act"
    /// is a statement about two predicates <b>in an order</b>, and expressed without the order it
    /// fires on every character who dies on the page. A corpus dry run on 2026-09-03 made the
    /// consequence measurable: 32 books, 13 active axioms, <b>zero</b> candidates anywhere,
    /// because every shipped axiom encoded one idea whose only live instance had already been
    /// purged.</para>
    /// </summary>
    public partial class StoryLedgerTemporalAxioms : Migration
    {
        // Named constants so Down deletes exactly what Up inserted, and the patterns are
        // reviewable in one place — same discipline as the Phase 2 axiom migration. The predicate
        // families come from the live vocabulary (prose --continuity predicates), not from a
        // guess: naming "father" when extraction had written father_name/father_occupation is the
        // near-miss that made the last axiom match nothing at all.
        private const string DeathPredicates =
            "death_status*|life_status*|vital_status*|fate*|demise*|killed*|died*";
        private const string DeathObjects =
            "dead|deceased|died|killed|slain|murdered|executed|no longer alive";
        private const string ActionPredicates =
            "action*|acts*|speaks*|says*|decides*|attends*|arrives*|departs*|travels*|meets*";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TemporalOrder",
                table: "PredicateExclusions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Symmetric MUST be false: swapping the sides asserts the opposite ordering, which is
            // the opposite axiom. PredicateExclusionService.Matches ignores Symmetric for a
            // temporal rule regardless, but a row claiming otherwise would misinform its reader.
            migrationBuilder.InsertData(
                table: "PredicateExclusions",
                columns: ["UniverseId", "PredicateA", "ObjectPatternA", "PredicateB", "ObjectPatternB",
                          "Symmetric", "TemporalOrder", "Source", "Status", "Rationale",
                          "CreatedAt", "ApprovedAt", "ApprovedBy"],
                values: [System.Guid.Empty, DeathPredicates, DeathObjects, ActionPredicates, (string)null,
                         false, "b_after_a", "builtin", "active",
                         "A character established dead does not act afterwards. The ordering is the whole axiom: it asks only about an action the book anchors to a LATER beat than the death, so a life that ends mid-story — the normal shape of a story — is never the question. A confirmed hit means the prose has a dead character acting with nothing in between to explain it.",
                         new System.DateTime(2026, 9, 4, 3, 0, 0, System.DateTimeKind.Utc),
                         (System.DateTime?)null, (string)null]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete the seeded row BEFORE dropping the column it uses — the same ordering trap
            // that broke the first attempt at the Phase 2 axiom migration.
            migrationBuilder.Sql(
                "DELETE FROM [PredicateExclusions] WHERE [Source] = 'builtin' " +
                $"AND [PredicateA] = N'{DeathPredicates}' AND [PredicateB] = N'{ActionPredicates}';");

            migrationBuilder.DropColumn(
                name: "TemporalOrder",
                table: "PredicateExclusions");
        }
    }
}
