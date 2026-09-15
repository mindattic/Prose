using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Audit;
using Prose.Core.Services.Obligations;

namespace Prose.UnitTests;

/// <summary>
/// The reconciliation instrument (RFC 0013 D6a) against a miniature ledger: the deterministic
/// rules, the findings they file, COULD NOT LOOK on an empty ledger, and the parse contracts of the
/// two LLM-backed instruments (resurfacing judge, record grounding) without any LLM.
/// </summary>
[TestFixture]
public class ObligationReconciliationTests
{
    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private string tempRoot = "";
    private FindingsService findings = null!;
    private NarrativeObligationService ledger = null!;
    private ObligationReconciliationService recon = null!;

    private Guid bookId;
    private readonly List<Guid> beatIds = new();

    [SetUp]
    public async Task SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-obl-recon-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        await using var db = dbFactory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        bookId = Guid.CreateVersion7();
        beatIds.Clear();
        db.Nodes.Add(new BookNode { Id = bookId, Slug = "recon-book", NodeCode = "RCN", Title = "Recon Book", Kind = "book", UniverseId = Universe.GlmzId, Status = "draft" });
        var number = 0;
        for (var c = 1; c <= 4; c++)
        {
            var chId = Guid.CreateVersion7();
            db.Nodes.Add(new ChapterNode { Id = chId, Slug = $"recon-ch{c}", Title = $"Chapter {c}", Kind = "chapter", ParentNodeId = bookId, UniverseId = Universe.GlmzId, SortKey = c * 100 });
            for (var b = 0; b < 3; b++)
            {
                var text = $"Chapter {c} beat {b}. Kyle rode north and the rain kept its own counsel.";
                var id = Guid.CreateVersion7();
                beatIds.Add(id);
                db.Beats.Add(new Beat { Id = id, Number = ++number, Text = text, TextHash = Beat.ComputeHash(text), ObligationScanHash = "seeded" });
                db.BeatNodes.Add(new BeatNode { NodeId = chId, BeatId = id, SortKey = (b + 1) * 100 });
            }
        }
        await db.SaveChangesAsync();

        findings = new FindingsService(dbFactory, new TestPathProviderWithRoot(tempRoot));
        ledger = new NarrativeObligationService(dbFactory, NullLogger<NarrativeObligationService>.Instance, extractor: null);
        var runner = new AuditRunner(new FakeLlmService(), findings);
        recon = new ObligationReconciliationService(dbFactory, ledger, runner, NullLogger<ObligationReconciliationService>.Instance, judge: null);
    }

    [TearDown]
    public void TearDown()
    {
        conn.Close(); conn.Dispose();
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<NarrativeObligation> AddRowAsync(string kind, string desc, Guid originBeat, string state = ObligationState.Open,
        string dueKind = ObligationDueKind.Chapter, int? dueValue = 1, bool locked = false, Guid? closingBeat = null, string? closingQuote = null)
    {
        await using var db = dbFactory.CreateDbContext();
        var beat = await db.Beats.SingleAsync(b => b.Id == originBeat);
        var row = new NarrativeObligation
        {
            NodeId = bookId, Kind = kind, Description = desc, Provenance = ClaimProvenance.Observed,
            OriginBeatId = originBeat, OriginQuote = "Kyle rode north and the rain kept its own counsel", OriginTextHash = beat.TextHash,
            DueByKind = dueKind, DueByValue = dueValue, State = state, AuthorLocked = locked,
            ClosingBeatId = closingBeat, ClosingQuote = closingQuote, ClosingTextHash = closingBeat is Guid cb ? (await db.Beats.SingleAsync(b => b.Id == cb)).TextHash : null,
            DedupKey = NarrativeObligationService.DedupKey(bookId, kind, desc),
        };
        db.NarrativeObligations.Add(row);
        await db.SaveChangesAsync();
        return row;
    }

    [Test]
    public async Task EmptyLedger_CouldNotLook_FilesNothing()
    {
        await using (var db = dbFactory.CreateDbContext())
            await db.Beats.ExecuteUpdateAsync(s => s.SetProperty(b => b.ObligationScanHash, (string?)null));

        var r = await recon.RunAsync(bookId);

        Assert.That(r.CouldNotLook, Is.True);
        Assert.That(r.Examined, Is.EqualTo(0));
        Assert.That(findings.List(limit: 100, filePathPrefix: "node:recon-book"), Is.Empty);
        Assert.That(r.Snapshot, Is.Null, "a run that could not look must not write a health row");
    }

    [Test]
    public async Task OverdueEarlyReferent_IsABlocker_AndFiled()
    {
        await AddRowAsync(ObligationKind.IntroducedReferent, "girl behind the curtain", beatIds[0], dueKind: ObligationDueKind.Chapter, dueValue: 1);

        var r = await recon.RunAsync(bookId);

        Assert.That(r.CouldNotLook, Is.False);
        Assert.That(r.RuleCounts["overdue_open"], Is.EqualTo(1));
        var v = r.Verdicts.Single(x => x.RuleKey == "overdue_open");
        Assert.That(v.Severity, Is.EqualTo("BLOCKER"), "an unnamed referent from the first 15% of the book, never advanced, blocks");
        Assert.That(v.Evidence, Does.Contain("girl behind the curtain").And.Contain("never addressed"));

        var filed = findings.List(limit: 100, filePathPrefix: "node:recon-book#obligations");
        Assert.That(filed, Has.Count.EqualTo(1));
        Assert.That(filed[0].Category, Is.EqualTo(FindingCategory.NarrativeObligation));
        Assert.That(filed[0].Severity, Is.EqualTo(FindingSeverity.High));
        Assert.That(filed[0].Summary, Does.StartWith("OBLIGATION overdue_open@"));
        Assert.That(filed[0].SuggestedFix, Is.Null, "findings describe, never instruct (RFC 0009)");
        Assert.That(r.Snapshot, Is.Not.Null);
        Assert.That(r.Snapshot!.Overdue, Is.EqualTo(1));
    }

    [Test]
    public async Task DeferredAndAuthoredRows_AreNeverOverdue()
    {
        await AddRowAsync(ObligationKind.Question, "deferred mystery", beatIds[0], state: ObligationState.Deferred, dueKind: ObligationDueKind.BookEnd, dueValue: null, locked: true);
        await AddRowAsync(ObligationKind.Promise, "author-locked promise", beatIds[0], dueKind: ObligationDueKind.Chapter, dueValue: 1, locked: true);

        var r = await recon.RunAsync(bookId);

        Assert.That(r.RuleCounts["overdue_open"], Is.EqualTo(0));
        Assert.That(r.RuleCounts["deferred_expired"], Is.EqualTo(0));
        Assert.That(r.Balance.Balanced, Is.True);
    }

    [Test]
    public async Task OpenAtEnd_FiresOnlyWhenTheBookIsComplete()
    {
        await AddRowAsync(ObligationKind.Foreshadow, "the sword's maker", beatIds[0], dueKind: ObligationDueKind.BookEnd, dueValue: null);

        var draft = await recon.RunAsync(bookId);
        Assert.That(draft.RuleCounts["open_at_end"], Is.EqualTo(0));

        await using (var db = dbFactory.CreateDbContext())
        {
            var n = await db.Nodes.SingleAsync(x => x.Id == bookId);
            n.Status = "Complete - publication ready";
            await db.SaveChangesAsync();
        }
        var complete = await recon.RunAsync(bookId);
        Assert.That(complete.BookAtEnd, Is.True);
        Assert.That(complete.RuleCounts["open_at_end"], Is.EqualTo(1));
    }

    [Test]
    public async Task StaleClosure_WhenClosingBeatTextChanged_IsFiled()
    {
        var row = await AddRowAsync(ObligationKind.Promise, "paid promise", beatIds[0], state: ObligationState.Closed,
            dueKind: ObligationDueKind.BookEnd, dueValue: null, closingBeat: beatIds[5], closingQuote: "the rain kept its own counsel");
        await using (var db = dbFactory.CreateDbContext())
        {
            var b = await db.Beats.SingleAsync(x => x.Id == beatIds[5]);
            b.Text = "Rewritten entirely."; b.TextHash = Beat.ComputeHash(b.Text);
            await db.SaveChangesAsync();
        }

        var r = await recon.RunAsync(bookId);
        Assert.That(r.RuleCounts["stale_closure"], Is.EqualTo(1));
        Assert.That(r.Verdicts.Single(v => v.RuleKey == "stale_closure").Evidence, Does.Contain("no longer in that beat"));
    }

    [Test]
    public async Task RerunAfterDecision_ClearsTheFinding()
    {
        var row = await AddRowAsync(ObligationKind.IntroducedReferent, "girl behind the curtain", beatIds[0]);
        await recon.RunAsync(bookId);
        Assume.That(findings.List(limit: 100, filePathPrefix: "node:recon-book#obligations"), Has.Count.EqualTo(1));

        var dropped = await ledger.DropAsync(row.Id, ObligationDroppedReason.BackgroundTexture, "she is texture", ObligationActor.AuthorMcp);
        Assume.That(dropped.Ok, Is.True);

        await recon.RunAsync(bookId);
        Assert.That(findings.List(limit: 100, filePathPrefix: "node:recon-book#obligations"), Is.Empty, "delete-then-recreate must clear a finding the author decided");
    }

    [Test]
    public void IsBookAtEnd_Cases()
    {
        Assert.That(ObligationReconciliationService.IsBookAtEnd("draft", null, null), Is.False);
        Assert.That(ObligationReconciliationService.IsBookAtEnd("Complete - publication ready", null, null), Is.True);
        Assert.That(ObligationReconciliationService.IsBookAtEnd("draft", null, DateTime.UtcNow), Is.True);
        Assert.That(ObligationReconciliationService.IsBookAtEnd("narrating", "published", null), Is.True);
    }

    [Test]
    public void JudgeParse_KeepsOnlyKnownRelations()
    {
        var raw = """{"reasoning":"r","verdicts":[{"beat_number":1,"relation":"closes","quote":"nowhere else to leave her"},{"beat_number":2,"relation":"maybe","quote":null},{"beat_number":3,"relation":"not_addressed","quote":null}]}""";
        var v = ObligationResurfacingJudge.Parse(raw);
        Assert.That(v.Select(x => x.Number), Is.EqualTo(new[] { 1, 3 }));
        Assert.That(v[0].Relation, Is.EqualTo("closes"));
    }

    [Test]
    public void GroundingParse_ClaimsAndVerdicts()
    {
        var claims = EntityRecordGroundingService.ParseClaims("""{"reasoning":"a character record","claims":[{"field":"relationships.daughter","text":"Mrs. Chen has a living daughter Kyle once saved with a mercy delivery to her sickbed."},{"field":"x","text":"short"}]}""");
        Assert.That(claims, Has.Count.EqualTo(1));
        Assert.That(claims[0].Field, Is.EqualTo("relationships.daughter"));

        var verdicts = EntityRecordGroundingService.ParseVerdicts("""{"reasoning":"r","verdicts":[{"claim_id":1,"verdict":"not_found","beat_number":null,"quote":null},{"claim_id":2,"verdict":"entailed","beat_number":3,"quote":"she made him a second bowl"},{"claim_id":3,"verdict":"unsure"}]}""");
        Assert.That(verdicts.Select(v => v.ClaimId), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(verdicts[1].BeatNumber, Is.EqualTo(3));
    }

    private sealed class TestFactory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
    {
        private readonly DbContextOptions<ProseDbContext> opts = new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;
        public ProseDbContext CreateDbContext() => new(opts);
        public Task<ProseDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
    }
}
