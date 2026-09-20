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

    // ── The scoring rule (ObligationCalibrationService.Classify) ──────────────────────────────
    // Pinned after GCSH run 5 reported "recall 1.000" that covered only the four ABANDONED
    // injections. The resolved half never reached the tally at all: a row the extractor never
    // opened — a total miss — printed "ok — no row (never opened)" and scored as a PASS.

    private static CalibrationInjection Injection(string kind) => new()
    {
        NodeId = Guid.CreateVersion7(), BeatId = Guid.CreateVersion7(), Kind = kind,
        Sentence = "The letter carried a violet seal that none of them recognised, and no one remarked upon it.",
        PayoffBeatId = Guid.CreateVersion7(),
    };

    private static NarrativeObligation Row(string state) => new()
    {
        Id = Guid.CreateVersion7(), NodeId = Guid.CreateVersion7(), Kind = ObligationKind.Plant,
        Description = "The violet seal.", State = state,
    };

    [Test]
    public void Classify_ResolvedInjectionNeverOpened_IsAMiss_NotAPass()
    {
        var (outcome, detail) = ObligationCalibrationService.Classify(Injection("resolved"), match: null, flaggedByRule: false);

        Assert.That(outcome, Is.EqualTo(ObligationCalibrationService.InjectionOutcome.FalseNegative),
            "a planted debt the extractor never opened is the worst outcome available; scoring it 'ok' hid extraction misses behind a perfect recall number");
        Assert.That(detail, Does.StartWith("FN"));
        Assert.That(detail, Does.Contain("never opened"));
    }

    [Test]
    public void Classify_ResolvedInjectionOpenedAndClosed_IsATruePositive()
    {
        var (outcome, detail) = ObligationCalibrationService.Classify(
            Injection("resolved"), Row(ObligationState.Closed), flaggedByRule: false);

        Assert.That(outcome, Is.EqualTo(ObligationCalibrationService.InjectionOutcome.TruePositive),
            "counting the miss as FN without crediting the success as TP would bias recall the other way");
        Assert.That(detail, Does.StartWith("TP"));
    }

    [Test]
    public void Classify_ResolvedInjectionLeftOutstanding_IsAMisflag_NotAMiss()
    {
        foreach (var state in new[] { ObligationState.Open, ObligationState.Advanced })
        {
            var (outcome, detail) = ObligationCalibrationService.Classify(
                Injection("resolved"), Row(state), flaggedByRule: false);

            Assert.That(outcome, Is.EqualTo(ObligationCalibrationService.InjectionOutcome.ResolvedMisflagged),
                $"{state}: the debt was opened correctly — the defect is the unrecognised payoff, which costs precision, not recall");
            Assert.That(detail, Does.StartWith("FP"));
        }
    }

    [Test]
    public void Classify_ResolvedInjectionNeitherCarriedNorPaid_IsAMiss()
    {
        foreach (var state in new[] { ObligationState.Dropped, ObligationState.Withdrawn })
        {
            var (outcome, _) = ObligationCalibrationService.Classify(
                Injection("resolved"), Row(state), flaggedByRule: false);

            Assert.That(outcome, Is.EqualTo(ObligationCalibrationService.InjectionOutcome.FalseNegative),
                $"{state}: the row is gone without the payoff ever being recognised");
        }
    }

    [Test]
    public void Classify_AbandonedInjection_ScoresOnOutstandingOrTheFiredRule()
    {
        var inj = Injection("abandoned");

        Assert.That(ObligationCalibrationService.Classify(inj, Row(ObligationState.Open), false).Outcome,
            Is.EqualTo(ObligationCalibrationService.InjectionOutcome.TruePositive),
            "outstanding at the end is the right answer even when no rule fired — due=book-end may not be overdue yet");
        Assert.That(ObligationCalibrationService.Classify(inj, Row(ObligationState.Open), true).Outcome,
            Is.EqualTo(ObligationCalibrationService.InjectionOutcome.TruePositive));
        Assert.That(ObligationCalibrationService.Classify(inj, null, false).Outcome,
            Is.EqualTo(ObligationCalibrationService.InjectionOutcome.FalseNegative),
            "a debt the text never pays and the ledger never opened is a miss");
        Assert.That(ObligationCalibrationService.Classify(inj, Row(ObligationState.Closed), false).Outcome,
            Is.EqualTo(ObligationCalibrationService.InjectionOutcome.FalseNegative),
            "closing a debt the text never pays is a miss — this is the false-close class that run 3 hit");
    }

    [Test]
    public void APartialRead_CannotMeetTheBar_HoweverGoodTheArithmetic()
    {
        // Perfect numbers: 8 TP, no misses, no mis-flags, no control findings.
        var perfect = new ObligationCalibrationService.Score(
            bookId, 8, 4, 4, TruePositives: 8, FalseNegatives: 0, ResolvedMisflagged: 0,
            ControlFindingsModeratePlus: 0, WordCount: 100_000,
            Precision: 1.0, Recall: 1.0, F1: 1.0, ControlFalsePositivesPer10k: 0, Details: Array.Empty<string>());

        Assert.That((perfect with { BeatsTotal = 96, BeatsRead = 96 }).MeetsBar(), Is.True,
            "a complete read with perfect numbers passes");

        var partial = perfect with { BeatsTotal = 96, BeatsRead = 55 };
        Assert.That(partial.CouldNotLook, Is.True);
        Assert.That(partial.MeetsBar(), Is.False,
            "GCSH run 6 read 55 of 96 beats and reported a score as if it had read the book; numbers over a partial read describe the beats that were read and nothing else");
    }

    [Test]
    public void TheBar_SplitsByStructuralCompleteness()
    {
        // 9 control findings on a 17,300-word text, every one of them "the book has not paid this
        // yet" — the measured shape of a PERFECT hand-read of A Tale of Two Cities, Book the First
        // (RFC 0013 §6a). The bar allows ~1.0 MODERATE+ per 10k words, i.e. ~1.7 for this book.
        var base9 = new ObligationCalibrationService.Score(
            bookId, 8, 4, 4, TruePositives: 8, FalseNegatives: 0, ResolvedMisflagged: 0,
            ControlFindingsModeratePlus: 9, WordCount: 17_300,
            Precision: 1.0, Recall: 1.0, F1: 1.0, ControlFalsePositivesPer10k: 0, Details: Array.Empty<string>())
            { BeatsTotal = 17, BeatsRead = 17, ControlStillOpen = 9 };

        var incomplete = base9 with { StructurallyComplete = false };
        Assert.That(incomplete.ControlStructural, Is.EqualTo(0));
        Assert.That(incomplete.ControlAgainstBar, Is.EqualTo(0),
            "act one of a novel carries its unpaid debts forward; they are the structure, not defects");
        Assert.That(incomplete.BarBasis, Does.Contain("INCOMPLETE"));

        var complete = base9 with { StructurallyComplete = true };
        Assert.That(complete.ControlAgainstBar, Is.EqualTo(9),
            "a text that finishes its own story and still owes 9 debts really did abandon them");

        var unset = base9 with { StructurallyComplete = null };
        Assert.That(unset.ControlAgainstBar, Is.EqualTo(9),
            "unset takes the STRICT branch — an unset flag must never silently loosen a bar");
        Assert.That(unset.BarBasis, Does.Contain("NOT SET"));
    }

    [Test]
    public void AFullRead_IsNotCouldNotLook()
    {
        var score = new ObligationCalibrationService.Score(
            bookId, 8, 4, 4, 0, 0, 0, 0, 1000, 0, 0, 0, 0, Array.Empty<string>())
            { BeatsTotal = 96, BeatsRead = 96 };

        Assert.That(score.CouldNotLook, Is.False);
    }

    [Test]
    public void ExtractorRecordsWhyABeatCouldNotBeRead()
    {
        // A response cut off at the token ceiling: an opening brace, no closing one.
        var truncated = NarrativeObligationExtractor.Parse(
            "{\"reasoning\":\"the beat opens a great many debts and the list runs on", beatText: "irrelevant", openCount: 0);
        Assert.That(truncated.Evaluated, Is.False);
        Assert.That(truncated.Failure, Does.Contain("truncated"),
            "the truncation signature must be named in the log, or a half-read book looks like a clean one");

        var garbage = NarrativeObligationExtractor.Parse("I'm sorry, I can't help with that.", "irrelevant", 0);
        Assert.That(garbage.Evaluated, Is.False);
        Assert.That(garbage.Failure, Is.Not.Null.And.Not.Empty);

        var empty = NarrativeObligationExtractor.Parse("", "irrelevant", 0);
        Assert.That(empty.Evaluated, Is.False);
        Assert.That(empty.Failure, Is.EqualTo("empty response"));
    }

    [Test]
    public void ScorerVersion_IsStampedOnEveryScore()
    {
        var score = new ObligationCalibrationService.Score(
            bookId, 8, 4, 4, 0, 0, 0, 0, 1000, 0, 0, 0, 0, Array.Empty<string>());

        Assert.That(score.ScorerVersion, Is.EqualTo(ObligationCalibrationService.ScorerVersion),
            "a recall number is only comparable to another computed under the same rules");
    }

    private sealed class TestFactory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
    {
        private readonly DbContextOptions<ProseDbContext> opts = new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;
        public ProseDbContext CreateDbContext() => new(opts);
        public Task<ProseDbContext> CreateDbContextAsync(CancellationToken ct = default) => Task.FromResult(CreateDbContext());
    }
}
