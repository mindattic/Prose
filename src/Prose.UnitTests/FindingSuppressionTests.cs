using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// The 2026-10-08 feature: an author-declared exception (side-table, never inline prose/entity-tag
/// markup) that tells <see cref="FindingsService.Upsert"/> to pre-dismiss a finding instead of
/// landing it in the New queue. Covers: category-wide vs. sub-code-narrowed matching, book-wide vs.
/// beat-scoped suppressions, an unknown code's write-time rejection, and that suppression never
/// silently drops a finding — it files Dismissed with SuppressedBy recorded, every time.
/// </summary>
[TestFixture]
public class FindingSuppressionTests
{
    private SqliteConnection connection = null!;
    private TestFactory dbFactory = null!;
    private FindingSuppressionService suppressions = null!;
    private FindingsService findings = null!;
    private readonly Guid beatA = Guid.NewGuid();
    private readonly Guid beatB = Guid.NewGuid();
    private const string Slug = "test-book-01";

    [SetUp]
    public void SetUp()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using (var ctx = new ProseDbContext(new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(connection).Options))
            ctx.Database.EnsureCreated();

        dbFactory    = new TestFactory(connection);
        suppressions = new FindingSuppressionService(dbFactory);
        findings     = new FindingsService(dbFactory, new TestPathProvider(), suppressions);
    }

    [TearDown]
    public void TearDown() => connection.Dispose();

    private string BeatPath(Guid beatId) => $"node:{Slug}/beat:{beatId:D}";

    [Test]
    public void Unsuppressed_finding_files_New()
    {
        var id = findings.Upsert(BeatPath(beatA), null, FindingCategory.CraftChecklist, FindingSeverity.Low,
            "CHECKLIST beat: 1 banned-mannerism hit(s) — Cognitive-architecture tics: she filed it away", null, null);
        var f = findings.List(filePathPrefix: BeatPath(beatA))[0];
        Assert.That(f.Status, Is.EqualTo(FindingStatus.New));
        Assert.That(f.SuppressedBy, Is.Null);
    }

    [Test]
    public void Category_wide_suppression_matches_any_summary_in_that_category()
    {
        suppressions.Add(Slug, beatId: null, code: "CraftChecklist", reason: "ledger-keeper voice", createdBy: "test");

        findings.Upsert(BeatPath(beatA), null, FindingCategory.CraftChecklist, FindingSeverity.Low,
            "CHECKLIST beat: 1 banned-mannerism hit(s) — Mood-soup: anything at all", null, null);

        var f = findings.List(filePathPrefix: BeatPath(beatA))[0];
        Assert.That(f.Status, Is.EqualTo(FindingStatus.Dismissed));
        Assert.That(f.SuppressedBy, Does.Contain("CraftChecklist"));
    }

    [Test]
    public void Subcode_suppression_matches_only_its_own_phrase_not_siblings_in_the_same_category()
    {
        suppressions.Add(Slug, beatId: null, code: "CRAFT-8.2", reason: "established voice", createdBy: "test");

        findings.Upsert(BeatPath(beatA), null, FindingCategory.CraftChecklist, FindingSeverity.Low,
            "CHECKLIST beat: 1 banned-mannerism hit(s) — Cognitive-architecture tics: she filed it away", null, null);
        findings.Upsert(BeatPath(beatB), null, FindingCategory.CraftChecklist, FindingSeverity.Low,
            "CHECKLIST beat: 1 banned-mannerism hit(s) — Mood-soup: atmosphere crowds the plot", null, null);

        var matched   = findings.List(filePathPrefix: BeatPath(beatA))[0];
        var unmatched = findings.List(filePathPrefix: BeatPath(beatB))[0];
        Assert.That(matched.Status, Is.EqualTo(FindingStatus.Dismissed), "CRAFT-8.2 should match its own phrase");
        Assert.That(unmatched.Status, Is.EqualTo(FindingStatus.New), "CRAFT-8.2 must not bleed into Mood-soup");
    }

    [Test]
    public void Beat_scoped_suppression_does_not_apply_to_a_different_beat_in_the_same_book()
    {
        suppressions.Add(Slug, beatId: beatA, code: "CraftChecklist", reason: "only this one scene", createdBy: "test");

        findings.Upsert(BeatPath(beatA), null, FindingCategory.CraftChecklist, FindingSeverity.Low, "CHECKLIST beat: x", null, null);
        findings.Upsert(BeatPath(beatB), null, FindingCategory.CraftChecklist, FindingSeverity.Low, "CHECKLIST beat: x", null, null);

        Assert.That(findings.List(filePathPrefix: BeatPath(beatA))[0].Status, Is.EqualTo(FindingStatus.Dismissed));
        Assert.That(findings.List(filePathPrefix: BeatPath(beatB))[0].Status, Is.EqualTo(FindingStatus.New));
    }

    [Test]
    public void Adding_a_suppression_after_the_fact_flips_an_existing_New_finding_on_the_next_upsert()
    {
        findings.Upsert(BeatPath(beatA), null, FindingCategory.CraftChecklist, FindingSeverity.Low,
            "CHECKLIST beat: 1 banned-mannerism hit(s) — Cognitive-architecture tics: filed it", null, null);
        Assert.That(findings.List(filePathPrefix: BeatPath(beatA))[0].Status, Is.EqualTo(FindingStatus.New));

        suppressions.Add(Slug, beatId: null, code: "CRAFT-8.2", reason: "voice, confirmed by hand", createdBy: "test");

        // Same dedup key (filePath|category|summary) -> same row, re-upserted by a re-run.
        findings.Upsert(BeatPath(beatA), null, FindingCategory.CraftChecklist, FindingSeverity.Low,
            "CHECKLIST beat: 1 banned-mannerism hit(s) — Cognitive-architecture tics: filed it", null, null);

        var f = findings.List(filePathPrefix: BeatPath(beatA))[0];
        Assert.That(f.Status, Is.EqualTo(FindingStatus.Dismissed));
        Assert.That(f.SuppressedBy, Is.Not.Null);
    }

    [Test]
    public void A_dismissed_finding_never_matching_any_suppression_stays_untouched()
    {
        // Simulates a human-dismissed finding (no suppression ever matched it) surviving a rescan.
        var id = findings.Upsert(BeatPath(beatA), null, FindingCategory.CraftChecklist, FindingSeverity.Low,
            "CHECKLIST beat: 1 banned-mannerism hit(s) — Over-explanation: told, not shown", null, null);
        findings.SetStatus(id, FindingStatus.Dismissed);

        findings.Upsert(BeatPath(beatA), null, FindingCategory.CraftChecklist, FindingSeverity.Low,
            "CHECKLIST beat: 1 banned-mannerism hit(s) — Over-explanation: told, not shown", null, null);

        var f = findings.List(filePathPrefix: BeatPath(beatA))[0];
        Assert.That(f.Status, Is.EqualTo(FindingStatus.Dismissed));
        Assert.That(f.SuppressedBy, Is.Null, "a human Dismiss must never be mistaken for an auto-suppression");
    }

    [Test]
    public void Unknown_code_is_rejected_at_write_time()
    {
        Assert.That(() => suppressions.Add(Slug, null, "NOT-A-REAL-CODE", null, "test"),
            Throws.ArgumentException);
    }

    [Test]
    public void Deactivated_suppression_stops_matching()
    {
        var row = suppressions.Add(Slug, null, "CraftChecklist", null, "test");
        suppressions.Deactivate(row.Id);

        findings.Upsert(BeatPath(beatA), null, FindingCategory.CraftChecklist, FindingSeverity.Low, "CHECKLIST beat: x", null, null);

        Assert.That(findings.List(filePathPrefix: BeatPath(beatA))[0].Status, Is.EqualTo(FindingStatus.New));
    }

    private sealed class TestFactory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
    {
        private readonly DbContextOptions<ProseDbContext> opts =
            new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;
        public ProseDbContext CreateDbContext() => new(opts);
        public Task<ProseDbContext> CreateDbContextAsync(CancellationToken ct = default)
            => Task.FromResult(CreateDbContext());
    }
}
