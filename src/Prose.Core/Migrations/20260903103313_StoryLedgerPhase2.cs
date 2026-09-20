using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prose.Core.Migrations
{
    /// <inheritdoc />
    public partial class StoryLedgerPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExclusionRuleId",
                table: "ContinuityClaims",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provenance",
                table: "ContinuityClaims",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "inferred");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceBeatId",
                table: "ContinuityClaims",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PredicateExclusions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniverseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PredicateA = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ObjectPatternA = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    PredicateB = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ObjectPatternB = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Symmetric = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Rationale = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredicateExclusions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TunedReadAdjudications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CacheKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ClaimAUid = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ClaimBUid = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ExclusionRuleId = table.Column<int>(type: "int", nullable: true),
                    BookSlug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IsContradiction = table.Column<bool>(type: "bit", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EvidenceQuote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RejectedReason = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    AdjudicatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TunedReadAdjudications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContinuityClaims_Provenance",
                table: "ContinuityClaims",
                column: "Provenance");

            migrationBuilder.CreateIndex(
                name: "IX_ContinuityClaims_SourceBeatId",
                table: "ContinuityClaims",
                column: "SourceBeatId");

            migrationBuilder.CreateIndex(
                name: "IX_PredicateExclusions_UniverseId_Status",
                table: "PredicateExclusions",
                columns: new[] { "UniverseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_PredicateExclusions_Shape",
                table: "PredicateExclusions",
                columns: new[] { "UniverseId", "PredicateA", "ObjectPatternA", "PredicateB", "ObjectPatternB" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TunedReadAdjudications_BookSlug",
                table: "TunedReadAdjudications",
                column: "BookSlug");

            migrationBuilder.CreateIndex(
                name: "UX_TunedReadAdjudications_CacheKey",
                table: "TunedReadAdjudications",
                column: "CacheKey",
                unique: true);

            // ── Grandfather every pre-existing claim ─────────────────────────
            // AddColumn's defaultValue backfills existing rows as "inferred", which would be a
            // false claim about ~12,888 rows: we do not know how they came to be believed. Author
            // ruling for this program was explicit — grandfather existing rows as
            // "legacy-unknown", then flag only the suspicious ones; do NOT mass-flag everything.
            // An unknown grade is not evidence of a defect, and treating the whole ledger as
            // suspect would bury the rows that genuinely are.
            //
            // Runs once, at migration time, so only rows that predate the column are touched;
            // "inferred" remains the correct default for everything inserted afterwards.
            // ContinuityClaims is system-versioned, so this writes a history row per claim —
            // that is the point: the grandfathering is itself auditable.
            migrationBuilder.Sql(
                "UPDATE [ContinuityClaims] SET [Provenance] = 'legacy-unknown';");

            // ── Seed the builtin exclusion axioms ────────────────────────────
            // Deliberately only two families. Every false-positive flood this project has hit
            // came from a rule that was NEARLY right applied corpus-wide, so the ontology ships
            // thin and grows by confirmed incident (Source="learned", Status="proposed") rather
            // than by guesswork. UniverseId 00000000-... means EVERY universe, which is correct
            // for a logical axiom and NOT for a canon one.
            //
            // Axiom 1 is the Dae-jung Seo defect, made expressible: a constructed being has no
            // biological parentage or childhood. "Kyle -> father -> Dae-jung Seo" against
            // "Kyle -> origin -> constructed, no prior life" is invisible to the same-predicate
            // rule and is the entire reason this table exists.
            //
            // NOT seeded, though it looks tempting: an axiom pairing a "never real / construct"
            // existence claim against relationship predicates like `mentor`. BCODA's canon is
            // precisely that Kyle DID have a mentor called Seito and that Seito was later
            // revealed to be a personality construct — both true at once. That rule would flag
            // correct canon as a contradiction, which is worse than having no rule.
            var builtins = new[]
            {
                new object[]
                {
                    "origin|nature|true_nature",
                    "constructed|construct|no prior life|no before|fabricated|manufactured|assembled|grown|vat-grown|configuration",
                    "father", (string)null,
                    "A constructed being has no biological father. If the prose asserts both an origin as a construct and a named father, one of them is fabricated lore.",
                },
                new object[]
                {
                    "origin|nature|true_nature",
                    "constructed|construct|no prior life|no before|fabricated|manufactured|assembled|grown|vat-grown|configuration",
                    "mother", (string)null,
                    "A constructed being has no biological mother. Same shape as the father axiom.",
                },
                new object[]
                {
                    "origin|nature|true_nature",
                    "constructed|construct|no prior life|no before|fabricated|manufactured|assembled|grown|vat-grown|configuration",
                    "parents", (string)null,
                    "A constructed being has no biological parents.",
                },
                new object[]
                {
                    "origin|nature|true_nature",
                    "constructed|construct|no prior life|no before|fabricated|manufactured|assembled|grown|vat-grown|configuration",
                    "birthplace", (string)null,
                    "A being with no prior life was not born anywhere; a named birthplace contradicts a constructed origin.",
                },
                new object[]
                {
                    "origin|nature|true_nature",
                    "constructed|construct|no prior life|no before|fabricated|manufactured|assembled|grown|vat-grown|configuration",
                    "childhood", (string)null,
                    "A being with no prior life had no childhood.",
                },
                new object[]
                {
                    "origin|nature|true_nature",
                    "constructed|construct|no prior life|no before|fabricated|manufactured|assembled|grown|vat-grown|configuration",
                    "hometown", (string)null,
                    "A being with no prior life has no hometown.",
                },
            };

            foreach (var r in builtins)
            {
                migrationBuilder.InsertData(
                    table: "PredicateExclusions",
                    columns: ["UniverseId", "PredicateA", "ObjectPatternA", "PredicateB", "ObjectPatternB",
                              "Symmetric", "Source", "Status", "Rationale", "CreatedAt", "ApprovedAt", "ApprovedBy"],
                    values: [Guid.Empty, r[0], r[1], r[2], r[3],
                             true, "builtin", "active", r[4], new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc), null, null]);
            }

            // Axiom family 2 — CANON-declared, GLMZ only (docs/BIBLE.md / CLAUDE.md World Rules:
            // "Iowan Behemoths are autonomous machines, NOT synthetic life. They are not alive.").
            // That is already a canon law; this is it made machine-readable. Scoped to GLMZ
            // because it is a fact about one universe's technology, not a logical necessity.
            var glmzId = new Guid("0197e9c9-0001-7000-8000-000000000001");
            var intentPredicates = new[] { "motive", "intent", "desire", "wants", "emotion", "feels" };
            foreach (var p in intentPredicates)
            {
                migrationBuilder.InsertData(
                    table: "PredicateExclusions",
                    columns: ["UniverseId", "PredicateA", "ObjectPatternA", "PredicateB", "ObjectPatternB",
                              "Symmetric", "Source", "Status", "Rationale", "CreatedAt", "ApprovedAt", "ApprovedBy"],
                    values: [glmzId, "nature|is_alive|life_status",
                             "autonomous machine|not alive|machine|not synthetic life|non-sentient|false",
                             p, null,
                             true, "canon", "active",
                             "GLMZ canon (CLAUDE.md World Rules): Iowan Behemoths and their kind are autonomous machines, NOT synthetic life. A machine that is not alive cannot be given an interior motive, desire, or felt emotion as an established fact.",
                             new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc), null, null]);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredicateExclusions");

            migrationBuilder.DropTable(
                name: "TunedReadAdjudications");

            migrationBuilder.DropIndex(
                name: "IX_ContinuityClaims_Provenance",
                table: "ContinuityClaims");

            migrationBuilder.DropIndex(
                name: "IX_ContinuityClaims_SourceBeatId",
                table: "ContinuityClaims");

            migrationBuilder.DropColumn(
                name: "ExclusionRuleId",
                table: "ContinuityClaims");

            migrationBuilder.DropColumn(
                name: "Provenance",
                table: "ContinuityClaims");

            migrationBuilder.DropColumn(
                name: "SourceBeatId",
                table: "ContinuityClaims");
        }
    }
}
