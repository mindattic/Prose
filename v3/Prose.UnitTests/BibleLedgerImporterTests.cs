using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Obligations;

namespace Prose.UnitTests;

/// <summary>
/// The bible §14 importer (RFC 0013, BCODA runbook step 3) against a miniature of the real table:
/// one closed plant whose "Ch<n> SK:<k>" labels match the tree, one whose payoff label is a place
/// name (the real "35th and Halsted SK:5000" row), one dropped finding whose reason later says
/// CLOSED (the real sword row). Exact anchors resolve; lossy ones are reported, never guessed;
/// re-runs are no-ops; imported rows raise no finding in the trial balance.
/// </summary>
[TestFixture]
public class BibleLedgerImporterTests
{
    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private BibleLedgerImporter importer = null!;
    private NarrativeObligationService ledger = null!;

    private Guid bookId;
    private readonly List<Guid> chapterIds = new();
    private readonly Dictionary<(int Chapter, double SortKey), Guid> beats = new();

    private const string Bible = """
        ## 13. Something earlier {#SS-X-13}

        Prose about the Way.

        ## 14. Open plant / payoff ledger {#SS-X-14}

        ### 14a. Closed plants (payoff written)

        | Plant | Plant location | Payoff | Payoff location | Closed |
        |---|---|---|---|---|
        | Alley figure's left hand runs 3° colder (echoes <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity>'s diagnosis) | Ch1 SK:200 | <entity repo="character" guid="019d6143-a648-7876-9688-0f6d38d70075">Kyle</entity> recognizes the thermal spoofing mask | Ch2 SK:100 | 2026-07-10 |
        | Vulture fledglings throw interference rounds | Ch2 SK:17000 | Kyle finds ordnance casings - same batch stamp | 35th and Halsted SK:5000 | 2026-07-10 |

        ### 14b. Dropped findings

        | Finding | Reason dropped | Date |
        |---|---|---|
        | Imani registers "recognition" at the Schism | No causal grounding; line removed per user decision | 2026-07-10 |
        | BRING THE SWORD summons card / stolen-sword mystery (Ch30) | Logic-sweep (m4): sword confirmed stolen. **CLOSED 2026-08-03** - resolution beat spliced on-page. | 2026-08-01 |

        ## 15. Brand Environment {#SS-X-15}

        | Not | a ledger | table |
        |---|---|---|
        | x | y | z |
        """;

    [SetUp]
    public async Task SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        importer = new BibleLedgerImporter(dbFactory, NullLogger<BibleLedgerImporter>.Instance);
        ledger = new NarrativeObligationService(dbFactory, NullLogger<NarrativeObligationService>.Instance,
            new NarrativeObligationExtractor(new NeverCalledLlm(), NullLogger<NarrativeObligationExtractor>.Instance));

        await using var db = dbFactory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        bookId = Guid.CreateVersion7();
        db.Nodes.Add(new BookNode { Id = bookId, Slug = "bible-book", NodeCode = "BIB", Title = "Bible Book", Kind = "book", UniverseId = Universe.GlmzId, NodeOutline = Bible });
        chapterIds.Clear(); beats.Clear();
        var number = 0;
        for (var c = 1; c <= 2; c++)
        {
            var chId = Guid.CreateVersion7();
            chapterIds.Add(chId);
            db.Nodes.Add(new ChapterNode { Id = chId, Slug = $"bible-ch{c}", Title = $"Chapter {c}", Kind = "chapter", ParentNodeId = bookId, UniverseId = Universe.GlmzId, SortKey = c * 100 });
            foreach (var sk in new[] { 100.0, 200.0 })
            {
                var text = $"Chapter {c} beat at {sk}: Kyle rode north through the rain and said nothing to anyone.";
                var id = Guid.CreateVersion7();
                beats[(c, sk)] = id;
                db.Beats.Add(new Beat { Id = id, Number = ++number, Text = text, TextHash = Beat.ComputeHash(text), ObligationScanHash = "seeded" });
                db.BeatNodes.Add(new BeatNode { NodeId = chId, BeatId = id, SortKey = sk });
            }
        }
        await db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() { conn.Close(); conn.Dispose(); }

    // ── Parsing ───────────────────────────────────────────────────────────────────

    [Test]
    public void Parse_ReadsBothTables_StripsEntityTagsAndBold_IgnoresOtherTables()
    {
        var parsed = BibleLedgerImporter.Parse(Bible);

        Assert.That(parsed.Closed, Has.Count.EqualTo(2));
        Assert.That(parsed.Dropped, Has.Count.EqualTo(2));
        Assert.That(parsed.Warnings, Is.Empty);

        var first = parsed.Closed[0];
        Assert.That(first.Plant, Does.StartWith("Alley figure's left hand runs 3° colder (echoes Kyle's diagnosis)"));
        Assert.That(first.Plant, Does.Not.Contain("<entity"));
        Assert.That(first.Payoff, Is.EqualTo("Kyle recognizes the thermal spoofing mask"));
        Assert.That(first.ClosedOn, Is.EqualTo("2026-07-10"));

        var sword = parsed.Dropped[1];
        Assert.That(sword.Reason, Does.Contain("CLOSED 2026-08-03"));
        Assert.That(sword.Reason, Does.Not.Contain("**"));
        Assert.That(sword.Date, Is.EqualTo("2026-08-01"));
    }

    [TestCase("Ch14 SK:18000", 14, 18000)]
    [TestCase("Ch2 SK 100", 2, 100)]
    [TestCase("ch30 sk:800", 30, 800)]
    [TestCase("Ch21 SK80000", 21, 80000)]
    public void ParseLocation_CanonicalForms(string cell, int chapter, double sortKey)
    {
        var loc = BibleLedgerImporter.ParseLocation(cell);
        Assert.That(loc.IsCanonical, Is.True);
        Assert.That(loc.Chapter, Is.EqualTo(chapter));
        Assert.That(loc.SortKey, Is.EqualTo(sortKey));
    }

    [TestCase("35th and Halsted SK:5000")]
    [TestCase("the Narrows ramen-ya")]
    [TestCase("")]
    public void ParseLocation_NonCanonical_KeepsRawOnly(string cell)
    {
        var loc = BibleLedgerImporter.ParseLocation(cell);
        Assert.That(loc.IsCanonical, Is.False);
        Assert.That(loc.Chapter, Is.Null);
    }

    [Test]
    public void HasLedger_FalseWithoutA14Heading()
    {
        Assert.That(BibleLedgerImporter.HasLedger("## 13. Nope\n| a | b |\n|---|---|\n| 1 | 2 |"), Is.False);
        Assert.That(BibleLedgerImporter.HasLedger(Bible), Is.True);
        Assert.That(BibleLedgerImporter.Parse("## 12. no ledger here").IsEmpty, Is.True);
    }

    // ── Import ────────────────────────────────────────────────────────────────────

    [Test]
    public async Task Import_CreatesLockedAuthoredRows_ExactAnchorsResolve_LossyOnesAreReportedNotGuessed()
    {
        var report = await importer.ImportAsync(bookId, dryRun: false);

        Assert.That(report.CouldNotLook, Is.False);
        Assert.That(report.Source, Is.EqualTo("Nodes.NodeOutline"));
        Assert.That(report.ClosedRows, Is.EqualTo(2));
        Assert.That(report.DroppedRows, Is.EqualTo(2));
        Assert.That(report.Created, Is.EqualTo(4));
        Assert.That(report.NeedsAnchorRows, Is.EqualTo(1));
        // NeedsAnchor rows are listed first — they are the author's work.
        Assert.That(report.Outcomes[0].NeedsAnchor, Is.Not.Empty);

        await using var db = dbFactory.CreateDbContext();
        var rows = await db.NarrativeObligations.Where(o => o.NodeId == bookId).ToListAsync();
        Assert.That(rows, Has.Count.EqualTo(4));
        Assert.That(rows.All(r => r.AuthorLocked), Is.True, "every imported row is author-locked");
        Assert.That(rows.All(r => r.Provenance == ClaimProvenance.Authored), Is.True);
        Assert.That(rows.All(r => r.OriginQuote == null && r.ClosingQuote == null), Is.True, "the bible is the author's word, not a page quote");

        var anchored = rows.Single(r => r.Description.StartsWith("Alley figure"));
        Assert.That(anchored.Kind, Is.EqualTo(ObligationKind.Plant));
        Assert.That(anchored.State, Is.EqualTo(ObligationState.Closed));
        Assert.That(anchored.OriginBeatId, Is.EqualTo(beats[(1, 200)]));
        Assert.That(anchored.ClosingBeatId, Is.EqualTo(beats[(2, 100)]));

        var lossy = rows.Single(r => r.Description.StartsWith("Vulture fledglings"));
        Assert.That(lossy.State, Is.EqualTo(ObligationState.Closed));
        Assert.That(lossy.OriginBeatId, Is.Null, "Ch2 SK:17000 matches no beat exactly — never guessed");
        Assert.That(lossy.ClosingBeatId, Is.Null, "a place name is not an anchor");
        Assert.That(lossy.AuthorNote, Does.Contain("NEEDS ANCHOR"));
        var lossyOutcome = report.Outcomes.Single(o => o.Description.StartsWith("Vulture fledglings"));
        Assert.That(lossyOutcome.NeedsAnchor, Has.Count.EqualTo(2));
        Assert.That(lossyOutcome.NeedsAnchor[0], Does.Contain("nearest is 200"));

        var dropped = rows.Where(r => r.State == ObligationState.Dropped).ToList();
        Assert.That(dropped, Has.Count.EqualTo(2));
        Assert.That(dropped.All(r => r.DroppedReason == ObligationDroppedReason.AuthorNote), Is.True);
        Assert.That(dropped.All(r => r.Kind == ObligationKind.Promise), Is.True);
        var sword = dropped.Single(r => r.Description.StartsWith("BRING THE SWORD"));
        Assert.That(sword.AuthorNote, Does.Contain("CLOSED 2026-08-03"));
        Assert.That(sword.AuthorNote, Does.EndWith("(2026-08-01)"));
        var swordOutcome = report.Outcomes.Single(o => o.Description.StartsWith("BRING THE SWORD"));
        Assert.That(swordOutcome.Warning, Does.Contain("CLOSED"));

        // Mirror: one PlantPayoff per §14a row, back-linked, with the resolved beats.
        var pairs = await db.PlantPayoffs.Where(p => p.NodeId == bookId).OrderBy(p => p.SortKey).ToListAsync();
        Assert.That(pairs, Has.Count.EqualTo(2));
        Assert.That(pairs[0].ObligationId, Is.EqualTo(anchored.Id));
        Assert.That(pairs[0].PlantBeatId, Is.EqualTo(beats[(1, 200)]));
        Assert.That(pairs[0].PayoffBeatId, Is.EqualTo(beats[(2, 100)]));
        Assert.That(pairs[1].ObligationId, Is.EqualTo(lossy.Id));
        Assert.That(pairs[1].PlantBeatId, Is.Null);

        // Journal: open+close for plants, open+drop for findings, all by the import actor.
        var events = await db.NarrativeObligationEvents.Where(e => rows.Select(r => r.Id).Contains(e.ObligationId)).ToListAsync();
        Assert.That(events, Has.Count.EqualTo(8));
        Assert.That(events.All(e => e.Actor == ObligationActor.ImportBible), Is.True);
        Assert.That(events.Count(e => e.Action == ObligationEventAction.Close), Is.EqualTo(2));
        Assert.That(events.Count(e => e.Action == ObligationEventAction.Drop), Is.EqualTo(2));
    }

    [Test]
    public async Task Import_IsIdempotent_SecondRunReportsExistsAndWritesNothing()
    {
        var first = await importer.ImportAsync(bookId, dryRun: false);
        Assert.That(first.Created, Is.EqualTo(4));

        var second = await importer.ImportAsync(bookId, dryRun: false);
        Assert.That(second.Created, Is.EqualTo(0));
        Assert.That(second.Existing, Is.EqualTo(4));

        await using var db = dbFactory.CreateDbContext();
        Assert.That(await db.NarrativeObligations.CountAsync(o => o.NodeId == bookId), Is.EqualTo(4));
        Assert.That(await db.PlantPayoffs.CountAsync(p => p.NodeId == bookId), Is.EqualTo(2));
        Assert.That(await db.NarrativeObligationEvents.CountAsync(), Is.EqualTo(8));
    }

    [Test]
    public async Task DryRun_ResolvesAndReports_ButWritesNothing()
    {
        var report = await importer.ImportAsync(bookId, dryRun: true);

        Assert.That(report.DryRun, Is.True);
        Assert.That(report.Outcomes, Has.Count.EqualTo(4));
        Assert.That(report.Outcomes.All(o => o.Action == BibleLedgerImporter.RowAction.DryRun), Is.True);
        Assert.That(report.NeedsAnchorRows, Is.EqualTo(1));
        var anchored = report.Outcomes.Single(o => o.Description.StartsWith("Alley figure"));
        Assert.That(anchored.OriginBeatId, Is.EqualTo(beats[(1, 200)]), "dry run still shows where the row WOULD anchor");

        await using var db = dbFactory.CreateDbContext();
        Assert.That(await db.NarrativeObligations.AnyAsync(), Is.False);
        Assert.That(await db.PlantPayoffs.AnyAsync(), Is.False);
        Assert.That(await db.NarrativeObligationEvents.AnyAsync(), Is.False);
    }

    [Test]
    public async Task Import_PrefersOutlineSectionOverLegacyBlob()
    {
        await using (var db = dbFactory.CreateDbContext())
        {
            db.NodeOutlineSections.Add(new NodeOutlineSection { NodeId = bookId, SectionType = "Full", Content = Bible });
            db.NodeOutlineSections.Add(new NodeOutlineSection { NodeId = bookId, SectionType = "ArcSummary", Content = "no ledger here" });
            await db.SaveChangesAsync();
        }
        var report = await importer.ImportAsync(bookId, dryRun: true);
        Assert.That(report.Source, Is.EqualTo("NodeOutlineSections.Full"));
    }

    [Test]
    public async Task Import_WithoutA14Table_CouldNotLook_WritesNothing()
    {
        await using (var db = dbFactory.CreateDbContext())
        {
            var node = await db.Nodes.IgnoreQueryFilters().FirstAsync(n => n.Id == bookId);
            node.NodeOutline = "## 12. Orchestration\n\nNothing about plants.";
            await db.SaveChangesAsync();
        }
        var report = await importer.ImportAsync(bookId, dryRun: false);
        Assert.That(report.CouldNotLook, Is.True);
        Assert.That(report.Outcomes, Is.Empty);
        Assert.That(report.Warnings, Has.Count.EqualTo(1));
        await using var check = dbFactory.CreateDbContext();
        Assert.That(await check.NarrativeObligations.AnyAsync(), Is.False);
    }

    [Test]
    public async Task ImportedRows_RaiseNoFinding_InTheTrialBalance()
    {
        await importer.ImportAsync(bookId, dryRun: false);
        var tb = await ledger.TrialBalanceAsync(bookId, null);

        Assert.That(tb.CouldNotLook, Is.False);
        Assert.That(tb.OverdueWithoutDecision, Is.Empty, "closed and dropped rows owe nothing");
        Assert.That(tb.StaleClosures, Is.Empty, "no closing quote means nothing can go stale");
        Assert.That(tb.DanglingOrigins, Is.Empty, "an authored row with a null anchor is not dangling");
        Assert.That(tb.Closed, Is.EqualTo(2));
        Assert.That(tb.Dropped, Is.EqualTo(2));
        Assert.That(tb.Balanced, Is.True);
    }

    // ── Test doubles ─────────────────────────────────────────────────────────────

    private sealed class NeverCalledLlm : Prose.Core.Interfaces.ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
            => throw new InvalidOperationException("The importer must not call an LLM.");
    }

    private sealed class TestFactory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
    {
        private readonly DbContextOptions<ProseDbContext> opts = new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;
        public ProseDbContext CreateDbContext() => new(opts);
        public Task<ProseDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
    }
}
