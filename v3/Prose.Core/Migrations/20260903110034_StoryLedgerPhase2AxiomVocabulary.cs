using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <summary>
    /// Widens the axiom shape columns and calibrates the Phase 2 exclusion axioms against the
    /// vocabulary the ledger ACTUALLY uses.
    ///
    /// <para><b>Found the honest way, minutes after Phase 2's axioms shipped.</b> Dry-running the
    /// Tuned Read against BCODA — the book whose fabricated-father defect motivated the entire
    /// program — produced ZERO candidates. Not because the defect was gone from the ledger (it is
    /// still there, four claims of it CONFIRMED), but because the axioms named <c>father</c> and
    /// <c>origin</c> while extraction had recorded <c>father_name</c>, <c>father_occupation</c>,
    /// <c>father_profession</c>, <c>father_status</c>, <c>construction_type</c>,
    /// <c>marrow_subject_number</c> and <c>carrier_number</c>. Predicate matching is equality by
    /// design (so <c>father</c> can never quietly mean <c>grandfather</c>), so every seeded axiom
    /// matched nothing at all — exactly the failure mode <c>PredicateExclusionTests</c>' own
    /// header warns about, and the reason <c>--dry</c> exists.</para>
    ///
    /// <para>The fix has three parts: the <c>*</c> anchored-prefix family form in
    /// <c>PredicateExclusionService.PredicateMatchesPattern</c>; wider shape columns (a family
    /// alternation is 150+ chars, and <c>nvarchar(120)</c> truncated the first attempt into a
    /// failed migration the Hub's fail-loud guardrail refused to start on); and the concrete
    /// construct-side predicate names below, taken from the live ledger rather than guessed.</para>
    ///
    /// <para>The unique shape index becomes a plain lookup index. Across four 400-char
    /// alternation columns its key would be ~3.2KB against SQL Server's 1700-byte limit — created
    /// with a warning, then failing at INSERT on a genuinely long rule, i.e. breaking precisely
    /// when an author wrote the most expressive axiom. Shape dedup stays enforced in
    /// <c>ProposeLearnedRuleAsync</c>, the only automated insert path.</para>
    ///
    /// <para>The narrow Phase 2 rows are left in place rather than deleted: a bare <c>father</c>
    /// predicate is a legitimate shape another book's extraction may yet produce, and deleting
    /// seeded rows an author may since have approved or rejected would overwrite their
    /// decision.</para>
    /// </summary>
    public partial class StoryLedgerPhase2AxiomVocabulary : Migration
    {
        /// <summary>Predicate families asserting biological parentage or a pre-existing
        /// childhood. Anchored prefixes, so <c>father*</c> covers father_name /
        /// father_occupation / father_status but never grandfather_name.</summary>
        private const string ParentagePredicates =
            "father*|mother*|parent*|parents*|birthplace*|childhood*|hometown*|birth_date*|" +
            "family_origin*|born_in*|birth_city*";

        /// <summary>Predicate families asserting a manufactured origin. Names taken from BCODA's
        /// live ledger, not guessed: construction_type, marrow_subject_number, carrier_number and
        /// origin_program are all real rows.</summary>
        private const string ConstructedPredicates =
            "origin*|nature*|true_nature*|construction_type*|constructed*|marrow_subject_number*|" +
            "carrier_number*|subject_number*|configuration_number*";

        /// <summary>Object filter for the constructed side, widened with the wording the live
        /// ledger uses ("Marrow program composite", "nine configurations", "subject 10",
        /// "tenth") on top of the plainer terms.</summary>
        private const string ConstructedObjects =
            "constructed|construct|composite|no prior life|no before|fabricated|manufactured|" +
            "assembled|grown|vat-grown|configuration|marrow|subject 10|subject ten|tenth|" +
            "clone|copy|personality construct";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_PredicateExclusions_Shape",
                table: "PredicateExclusions");

            migrationBuilder.AlterColumn<string>(
                name: "PredicateB",
                table: "PredicateExclusions",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "PredicateA",
                table: "PredicateExclusions",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.CreateIndex(
                name: "IX_PredicateExclusions_Shape",
                table: "PredicateExclusions",
                columns: new[] { "UniverseId", "PredicateA" });

            // One row, not eleven: the parentage side is a single family pattern now, so the
            // axiom is expressed once instead of once per predicate name.
            migrationBuilder.InsertData(
                table: "PredicateExclusions",
                columns: ["UniverseId", "PredicateA", "ObjectPatternA", "PredicateB", "ObjectPatternB",
                          "Symmetric", "Source", "Status", "Rationale", "CreatedAt", "ApprovedAt", "ApprovedBy"],
                values: [Guid.Empty, ConstructedPredicates, ConstructedObjects, ParentagePredicates, (string)null,
                         true, "builtin", "active",
                         "A constructed being has no biological parentage, birthplace or childhood. If the ledger holds both a manufactured origin and a named parent for the same entity, one of them is fabricated lore — this is the exact shape of the BCODA 'Dae-jung Seo' defect that motivated the Story Ledger.",
                         new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc), (DateTime?)null, (string)null]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the seeded row BEFORE narrowing the columns back — a 152-char PredicateA
            // cannot survive an AlterColumn to nvarchar(120), which is what broke the first
            // attempt at this migration in the first place.
            migrationBuilder.Sql(
                "DELETE FROM [PredicateExclusions] WHERE [Source] = 'builtin' " +
                $"AND [PredicateA] = N'{ConstructedPredicates}' AND [PredicateB] = N'{ParentagePredicates}';");

            migrationBuilder.DropIndex(
                name: "IX_PredicateExclusions_Shape",
                table: "PredicateExclusions");

            migrationBuilder.AlterColumn<string>(
                name: "PredicateB",
                table: "PredicateExclusions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400);

            migrationBuilder.AlterColumn<string>(
                name: "PredicateA",
                table: "PredicateExclusions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400);

            migrationBuilder.CreateIndex(
                name: "UX_PredicateExclusions_Shape",
                table: "PredicateExclusions",
                columns: new[] { "UniverseId", "PredicateA", "ObjectPatternA", "PredicateB", "ObjectPatternB" },
                unique: true);
        }
    }
}
