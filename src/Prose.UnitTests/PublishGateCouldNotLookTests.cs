using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.UnitTests;

/// <summary>
/// The publish gate must not report "clean" for an instrument that never ran.
///
/// <para><b>What this pins.</b> Three of the gate's six checks were plain <c>Findings</c> counts —
/// read zero, print "clean" — with nothing recording whether the instrument behind them had ever
/// looked at the book. On 2026-09-22 that produced a readiness report for <c>bushido-coda</c>
/// containing both of these, three lines apart, about the same sweep:</para>
/// <code>
/// ✅ logic-sweep BLOCKER/MODERATE = 0 — clean
/// ❌ 2 consecutive dry sweep rounds — not converged
/// </code>
/// <para>The gate's other two checks (story ledger, obligation ledger) already refused to pass
/// silently, each with its own private evidence. <see cref="InstrumentRun"/> generalizes that, and
/// these tests are what stop the false green coming back.</para>
/// </summary>
[TestFixture]
public class PublishGateCouldNotLookTests
{
    // ── the decision, as a pure function ──────────────────────────────────────

    private static InstrumentRunLedger.InstrumentRunSummary Run(
        int examined = 10, int total = 10, string? fingerprint = null) =>
        new(new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc), examined, total, fingerprint);

    [Test]
    public void NeverRan_IsCouldNotLook_NotClean()
    {
        var (outcome, detail) = InstrumentRunLedger.Evaluate("the logic sweep", null, 0, "run it");

        Assert.That(outcome, Is.EqualTo(CheckOutcome.CouldNotLook));
        Assert.That(detail, Does.Contain("never run"));
        Assert.That(detail, Does.Not.Contain("clean"));
    }

    [Test]
    public void RanButReadNothing_IsCouldNotLook()
    {
        var (outcome, detail) = InstrumentRunLedger.Evaluate(
            "the logic sweep", Run(examined: 0, total: 521), 0, "run it");

        Assert.That(outcome, Is.EqualTo(CheckOutcome.CouldNotLook));
        Assert.That(detail, Does.Contain("0 of 521"));
    }

    [Test]
    public void PartialRead_IsVoid_NotAPass()
    {
        // RFC 0013's run 6: the obligation extractor read 55 of 96 beats and reported a
        // precision/recall pair over 57% of a book. A partial run measures nothing.
        var (outcome, detail) = InstrumentRunLedger.Evaluate(
            "the obligation scan", Run(examined: 55, total: 96), 0, "run it");

        Assert.That(outcome, Is.EqualTo(CheckOutcome.CouldNotLook));
        Assert.That(detail, Does.Contain("55 of 96"));
        Assert.That(detail, Does.Contain("void"));
    }

    [Test]
    public void RanAgainstProseThatHasSinceChanged_IsCouldNotLook()
    {
        var (outcome, detail) = InstrumentRunLedger.Evaluate(
            "the logic sweep", Run(fingerprint: "OLDHASH"), 0, "run it", currentFingerprint: "NEWHASH");

        Assert.That(outcome, Is.EqualTo(CheckOutcome.CouldNotLook));
        Assert.That(detail, Does.Contain("has changed since"));
    }

    [Test]
    public void RanFullyAndFoundNothing_IsAGenuinePass()
    {
        // The positive control: the guard must still let a real clean result through, and must say
        // what it read, so "clean" is a claim with evidence behind it rather than an assertion.
        var (outcome, detail) = InstrumentRunLedger.Evaluate(
            "the logic sweep", Run(examined: 521, total: 521, fingerprint: "SAME"), 0, "run it",
            currentFingerprint: "SAME");

        Assert.That(outcome, Is.EqualTo(CheckOutcome.Pass));
        Assert.That(detail, Does.Contain("read 521/521"));
    }

    [Test]
    public void RanFullyAndFoundSomething_Fails()
    {
        var (outcome, detail) = InstrumentRunLedger.Evaluate("the logic sweep", Run(), 3, "run it");

        Assert.That(outcome, Is.EqualTo(CheckOutcome.Fail));
        Assert.That(detail, Does.Contain("3 open finding(s)"));
    }

    [Test]
    public void OpenFindingsWithNoRunRow_FailRatherThanClaimingTheInstrumentNeverRan()
    {
        // Findings are themselves proof the instrument looked — nothing else could have filed
        // them. Without this the gate would print "never ran" and "3 open findings" about the
        // same check, which is the exact shape of contradiction this whole exercise is about.
        // It also matters in practice: the run ledger was added after years of runs that left
        // findings and no row.
        var (outcome, detail) = InstrumentRunLedger.Evaluate(
            "the blast-radius recheck", null, 3, "run it");

        Assert.That(outcome, Is.EqualTo(CheckOutcome.Fail));
        Assert.That(detail, Does.Contain("3 open finding(s)"));
        Assert.That(detail, Does.Not.Contain("never run"));
    }

    [Test]
    public void CouldNotLook_BlocksPublication_JustLikeAFailure()
    {
        var check = new PublishReadinessCheck("x", CheckOutcome.CouldNotLook, "never ran");
        Assert.That(check.Pass, Is.False);
    }

    // ── the clamp: beats handed in are not beats seen ─────────────────────────

    [Test]
    public void VisibleBeatCount_OnAnOversizedBook_IsFarBelowTheBeatsHandedIn()
    {
        // BCODA is ~193k words — over a million characters — against a 100k-char clamp. A
        // "full-book sweep" of it shows the model under a tenth of the manuscript, and the run
        // ledger has to record what was SEEN or the gate inherits the same lie one level up.
        var beats = Enumerable.Range(1, 400)
            .Select(i => new AuditBeat(Guid.CreateVersion7(), i, new string('x', 5_000), i, "Ch"))
            .ToList();

        var visible = LogicSweepService.VisibleBeatCount(beats);

        Assert.That(visible, Is.LessThan(beats.Count));
        Assert.That(visible, Is.GreaterThan(0), "head and tail are still genuinely read");
    }

    [Test]
    public void VisibleBeatCount_WhenTheWholeBookFits_IsEveryBeat()
    {
        var beats = Enumerable.Range(1, 5)
            .Select(i => new AuditBeat(Guid.CreateVersion7(), i, "short", i, "Ch"))
            .ToList();

        Assert.That(LogicSweepService.VisibleBeatCount(beats), Is.EqualTo(5));
    }

    // ── end to end, against a real database ───────────────────────────────────

    [Test]
    public async Task AnUnexaminedBook_DoesNotReportAnyCheckAsClean()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ss-gate-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var paths = new TestPathProviderWithRoot(tempRoot);
        var dbFactory = TestDbFactory.For(paths, "nodes");
        try
        {
            var bookId = Guid.CreateVersion7();
            var chapterId = Guid.CreateVersion7();
            await using (var db = dbFactory.CreateDbContext())
            {
                db.Nodes.Add(new BookNode
                {
                    Id = bookId, Slug = "unexamined", NodeCode = "UNX", Title = "Unexamined",
                    Kind = "book", UniverseId = Universe.GlmzId,
                });
                db.Nodes.Add(new ChapterNode
                {
                    Id = chapterId, Slug = "unexamined-ch1", Title = "Chapter 1",
                    Kind = "chapter", ParentNodeId = bookId, UniverseId = Universe.GlmzId,
                });
                var beatId = Guid.CreateVersion7();
                db.Beats.Add(new Beat { Id = beatId, Number = 1, Text = "Prose.", TextHash = Beat.ComputeHash("Prose.") });
                db.BeatNodes.Add(new BeatNode { NodeId = chapterId, BeatId = beatId, SortKey = 1 });
                await db.SaveChangesAsync();
            }

            var findings = new FindingsService(dbFactory, paths);
            var auditRunner = new AuditRunner(new FakeLlmService(), findings, NullLogger<AuditRunner>.Instance);
            var logicSweep = new LogicSweepService(
                auditRunner, new PlantPayoffService(dbFactory), dbFactory, findings);
            var health = new BookHealthService(dbFactory, findings, logicSweep, new ContinuityService(dbFactory));

            var report = await health.PublishReadinessAsync(bookId);

            Assert.That(report.Ready, Is.False);

            // The point of the whole exercise: not one check may claim "clean" on a book no
            // instrument has ever been pointed at.
            var claimingClean = report.Checks.Where(c => c.Detail.Contains("clean")).ToList();
            Assert.That(claimingClean, Is.Empty,
                "no check may report clean without evidence that its instrument ran: " +
                string.Join(" | ", claimingClean.Select(c => $"{c.Name} → {c.Detail}")));

            // And the three that used to pass silently now say why they cannot answer.
            foreach (var name in new[]
                     {
                         "logic-sweep BLOCKER/MODERATE = 0",
                         "blast-radius recheck clean",
                         "Reader-Proxy QA High/BLOCKER = 0",
                     })
            {
                var check = report.Checks.Single(c => c.Name == name);
                Assert.That(check.Outcome, Is.EqualTo(CheckOutcome.CouldNotLook), name);
            }
        }
        finally
        {
            TestDbFactory.Reset(paths);
            try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Test]
    public async Task OnceTheSweepHasActuallyRun_TheCheckCanReportClean()
    {
        // The end-to-end positive control. Without it, "no check says clean" would also pass on a
        // gate that had simply been broken into always answering CouldNotLook.
        var tempRoot = Path.Combine(Path.GetTempPath(), "ss-gate-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var paths = new TestPathProviderWithRoot(tempRoot);
        var dbFactory = TestDbFactory.For(paths, "nodes");
        try
        {
            var bookId = Guid.CreateVersion7();
            var chapterId = Guid.CreateVersion7();
            await using (var db = dbFactory.CreateDbContext())
            {
                db.Nodes.Add(new BookNode
                {
                    Id = bookId, Slug = "swept", NodeCode = "SWP", Title = "Swept",
                    Kind = "book", UniverseId = Universe.GlmzId,
                });
                db.Nodes.Add(new ChapterNode
                {
                    Id = chapterId, Slug = "swept-ch1", Title = "Chapter 1",
                    Kind = "chapter", ParentNodeId = bookId, UniverseId = Universe.GlmzId,
                });
                var beatId = Guid.CreateVersion7();
                db.Beats.Add(new Beat { Id = beatId, Number = 1, Text = "Prose.", TextHash = Beat.ComputeHash("Prose.") });
                db.BeatNodes.Add(new BeatNode { NodeId = chapterId, BeatId = beatId, SortKey = 1 });
                await db.SaveChangesAsync();
            }

            var findings = new FindingsService(dbFactory, paths);
            var ledger = new InstrumentRunLedger(dbFactory);
            string fingerprint;
            await using (var db = dbFactory.CreateDbContext())
                fingerprint = await LogicSweepService.ComputeBookFingerprintAsync(db, bookId, default);
            await ledger.RecordAsync(bookId, InstrumentRunLedger.LogicSweep, 1, 1, fingerprint);

            var auditRunner = new AuditRunner(new FakeLlmService(), findings, NullLogger<AuditRunner>.Instance);
            var logicSweep = new LogicSweepService(
                auditRunner, new PlantPayoffService(dbFactory), dbFactory, findings, ledger);
            var health = new BookHealthService(dbFactory, findings, logicSweep, new ContinuityService(dbFactory));

            var report = await health.PublishReadinessAsync(bookId);
            var sweepCheck = report.Checks.Single(c => c.Name == "logic-sweep BLOCKER/MODERATE = 0");

            Assert.That(sweepCheck.Outcome, Is.EqualTo(CheckOutcome.Pass));
            Assert.That(sweepCheck.Detail, Does.Contain("clean"));
        }
        finally
        {
            TestDbFactory.Reset(paths);
            try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort */ }
        }
    }
}
