using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Calibration;
using Prose.Core.Services.Obligations;

namespace Prose.UnitTests;

/// <summary>
/// The calibration harness must measure the instrument, not the residue of every run before it.
/// Pinned after GCSH runs 1–4 (2026-09-15/16), which were not independent measurements: run 3's
/// resurfacing judge closed the injected "stopped clock" plant on an unrelated quote, and run 4
/// then scored that same row as a miss — its close event was still run 3's, with no run-4 event on
/// it at all. A false close is terminal, because the judge only revisits Open/Advanced rows, so
/// nothing could ever correct it. Every score must start from a clean ledger.
/// </summary>
[TestFixture]
public class ObligationCalibrationServiceTests
{
    private static readonly Guid GutenbergId = new("0197e9c9-0001-7000-8000-0000000f00d5");
    private static readonly Guid OtherUniverseId = new("0197e9c9-0001-7000-8000-0000000f00d6");

    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private ObligationCalibrationService svc = null!;
    private Guid bookId, chapterId, plantBeatId, payoffBeatId;
    private Guid machineRowId, authoredRowId;

    [SetUp]
    public async Task SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        await using var db = dbFactory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        db.Universes.Add(new Universe { Id = GutenbergId, Slug = ObligationCalibrationService.CalibrationUniverseSlug, Name = "Gutenberg" });
        db.Universes.Add(new Universe { Id = OtherUniverseId, Slug = "glmz", Name = "GLMZ" });

        bookId = Guid.CreateVersion7(); chapterId = Guid.CreateVersion7();
        db.Nodes.Add(new BookNode { Id = bookId, Slug = "gcsh-test", NodeCode = "GCSHT", Title = "Calibration Book", Kind = "book", UniverseId = GutenbergId, Status = "Complete - publication ready" });
        db.Nodes.Add(new ChapterNode { Id = chapterId, Slug = "gcsh-ch1", Title = "Chapter 1", Kind = "chapter", ParentNodeId = bookId, UniverseId = GutenbergId, SortKey = 100 });

        var texts = new[]
        {
            "In the hall a clock had been stopped at a quarter past three, and nobody set it going again.",
            "When I went down there I found him talking with his son, so I smoked a cigar and waited behind a tree until he should be alone.",
        };
        var ids = new List<Guid>();
        for (var i = 0; i < texts.Length; i++)
        {
            var id = Guid.CreateVersion7();
            ids.Add(id);
            // Every beat starts STAMPED — a reset that leaves the stamp makes the rescan a no-op.
            db.Beats.Add(new Beat { Id = id, Number = i + 1, Text = texts[i], TextHash = Beat.ComputeHash(texts[i]), ObligationScanHash = "stamped-by-an-earlier-run" });
            db.BeatNodes.Add(new BeatNode { NodeId = chapterId, BeatId = id, SortKey = (i + 1) * 100 });
        }
        plantBeatId = ids[0]; payoffBeatId = ids[1];

        // The run-3 artefact: a machine row wrongly CLOSED on a grounded-but-unrelated quote.
        machineRowId = Guid.CreateVersion7();
        db.NarrativeObligations.Add(new NarrativeObligation
        {
            Id = machineRowId, NodeId = bookId, Kind = ObligationKind.Plant,
            Description = "The significance of the stopped clock at a quarter past three.",
            Provenance = ClaimProvenance.Observed, OriginBeatId = plantBeatId,
            OriginQuote = texts[0], DueByKind = ObligationDueKind.BookEnd,
            State = ObligationState.Closed, ClosingBeatId = payoffBeatId, ClosingQuote = texts[1],
            AuthorLocked = false,
            DedupKey = NarrativeObligationService.DedupKey(bookId, ObligationKind.Plant, "stopped clock"),
        });
        db.NarrativeObligationEvents.Add(new NarrativeObligationEvent
        {
            ObligationId = machineRowId, Action = ObligationEventAction.Close, BeatId = payoffBeatId,
            Quote = texts[1], Actor = ObligationActor.SystemDeep, Note = "resurfacing judge",
        });
        db.ObligationJudgeCache.Add(new ObligationJudgeCache
        {
            ObligationId = machineRowId, CandidateBeatId = payoffBeatId, CandidateTextHash = Beat.ComputeHash(texts[1]),
            PromptVersion = ObligationResurfacingJudge.PromptVersion, Relation = "closes", Quote = texts[1],
        });

        // Human ground truth: authored AND locked. The reset must not touch it.
        authoredRowId = Guid.CreateVersion7();
        db.NarrativeObligations.Add(new NarrativeObligation
        {
            Id = authoredRowId, NodeId = bookId, Kind = ObligationKind.Promise,
            Description = "An authored promise the writer entered by hand.",
            Provenance = ClaimProvenance.Authored, OriginBeatId = plantBeatId,
            OriginQuote = texts[0], DueByKind = ObligationDueKind.BookEnd,
            State = ObligationState.Open, AuthorLocked = true,
            DedupKey = NarrativeObligationService.DedupKey(bookId, ObligationKind.Promise, "authored promise"),
        });
        db.NarrativeObligationEvents.Add(new NarrativeObligationEvent
        {
            ObligationId = authoredRowId, Action = ObligationEventAction.Open, BeatId = plantBeatId,
            Quote = texts[0], Actor = ObligationActor.AuthorCli, Note = "hand-entered",
        });

        await db.SaveChangesAsync();

        svc = new ObligationCalibrationService(dbFactory, null!, null!, null!, NullLogger<ObligationCalibrationService>.Instance);
    }

    [TearDown]
    public void TearDown() { conn.Close(); conn.Dispose(); }

    [Test]
    public async Task ResetLedger_DropsMachineRowsAndTheirJournalAndJudgeCache_AndClearsScanStamps()
    {
        var dropped = await svc.ResetLedgerAsync(bookId);

        Assert.That(dropped, Is.EqualTo(1), "only the machine-produced row is dropped");

        await using var db = dbFactory.CreateDbContext();

        // The falsely-closed row and everything hanging off it is gone.
        Assert.That(await db.NarrativeObligations.AnyAsync(o => o.Id == machineRowId), Is.False, "the wrongly-closed row must not survive into the next run");
        Assert.That(await db.NarrativeObligationEvents.AnyAsync(e => e.ObligationId == machineRowId), Is.False, "its close event goes too");
        Assert.That(await db.ObligationJudgeCache.AnyAsync(c => c.ObligationId == machineRowId), Is.False, "a cached verdict would replay the same mistake");

        // Human ground truth survives intact.
        var authored = await db.NarrativeObligations.SingleAsync();
        Assert.That(authored.Id, Is.EqualTo(authoredRowId));
        Assert.That(authored.State, Is.EqualTo(ObligationState.Open));
        Assert.That(await db.NarrativeObligationEvents.CountAsync(e => e.ObligationId == authoredRowId), Is.EqualTo(1), "an authored row keeps its journal");

        // Stamps cleared, or the rescan would skip every beat and score an empty ledger.
        var stamps = await db.Beats.Select(b => b.ObligationScanHash).ToListAsync();
        Assert.That(stamps, Has.All.Null, "ScanBeatAsync short-circuits on a current stamp");
    }

    [Test]
    public void ResetLedger_RefusesABookOutsideTheCalibrationUniverse()
    {
        var authorBookId = Guid.CreateVersion7();
        using (var db = dbFactory.CreateDbContext())
        {
            db.Nodes.Add(new BookNode { Id = authorBookId, Slug = "author-book", NodeCode = "REAL", Title = "An Author's Book", Kind = "book", UniverseId = OtherUniverseId, Status = "Complete - publication ready" });
            db.SaveChanges();
        }

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await svc.ResetLedgerAsync(authorBookId));
        Assert.That(ex!.Message, Does.Contain(ObligationCalibrationService.CalibrationUniverseSlug));
        Assert.That(ex.Message, Does.Contain("Refusing to write"));
    }

    private sealed class TestFactory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
    {
        private readonly DbContextOptions<ProseDbContext> opts = new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;
        public ProseDbContext CreateDbContext() => new(opts);
        public Task<ProseDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
    }
}
