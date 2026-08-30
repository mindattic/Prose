using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the Full-Order Read instrument (docs/LOGIC.md §10, docs/READER-QA.md §2
/// instrument 5) — the felt-pass proxy added alongside GripePassService's existing gripe jury.
/// <see cref="GripePassService.GroundQuote"/> and the token-dedup helpers are already covered by
/// <c>GripePassServiceGroundQuoteTests</c> and reused unchanged; these tests cover only the new
/// surface: severity derivation from the recovery signal, and finding-scope isolation between
/// this instrument and the gripe jury.
/// </summary>
[TestFixture]
public class GripePassServiceEngagementTests
{
    [Test]
    public void NeverRecovered_IsBlocker()
    {
        var severity = GripePassService.DeriveEngagementSeverity(startBeat: 40, recoveredAtBeat: null);

        Assert.That(severity, Is.EqualTo("blocker"));
    }

    [Test]
    public void RecoveredWithinMinorWindow_IsMinor()
    {
        // MinorRecoveryWindowBeats = 3 — recovering exactly at the boundary still counts as minor.
        var severity = GripePassService.DeriveEngagementSeverity(startBeat: 10, recoveredAtBeat: 13);

        Assert.That(severity, Is.EqualTo("minor"));
    }

    [Test]
    public void RecoveredJustBeyondMinorWindow_IsModerate()
    {
        var severity = GripePassService.DeriveEngagementSeverity(startBeat: 10, recoveredAtBeat: 14);

        Assert.That(severity, Is.EqualTo("moderate"));
    }

    [Test]
    public void RecoveredManyBeatsLater_IsModerate()
    {
        var severity = GripePassService.DeriveEngagementSeverity(startBeat: 10, recoveredAtBeat: 80);

        Assert.That(severity, Is.EqualTo("moderate"));
    }

    [Test]
    public void RecoveredImmediately_SameBeat_IsMinor()
    {
        // A span that "recovers" at the very beat it started is the smallest possible dip.
        var severity = GripePassService.DeriveEngagementSeverity(startBeat: 5, recoveredAtBeat: 5);

        Assert.That(severity, Is.EqualTo("minor"));
    }

    // ── finding-scope isolation ──────────────────────────────────────────────────
    //
    // The gripe jury (RunAsync) and the full-order read (RunFullOrderReadAsync) both file under
    // FindingCategory.ReaderGripe but must never clear each other's findings on re-run — the gripe
    // jury scopes under "node:{slug}" with a "GRIPE" summary prefix; the full-order read scopes
    // under "node:{slug}#fullorderread" with an "ENGAGEMENT" summary prefix. This exercises the
    // real FindingsService (SQLite-backed, same TestDbFactory pattern as
    // FindingsServiceStalenessTests) rather than re-deriving the string logic by inspection.
    [TestFixture]
    public class FindingScopeIsolation
    {
        private string tempRoot = "";
        private TestPathProviderWithRoot paths = null!;
        private IDbContextFactory<ProseDbContext> dbFactory = null!;
        private FindingsService svc = null!;

        [SetUp]
        public void SetUp()
        {
            tempRoot = Path.Combine(Path.GetTempPath(), "ss-engagement-scope-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            paths = new TestPathProviderWithRoot(tempRoot);
            dbFactory = TestDbFactory.For(paths, "nodes");
            svc = new FindingsService(dbFactory, paths);
        }

        [TearDown]
        public void TearDown()
        {
            TestDbFactory.Reset(paths);
            try { Directory.Delete(tempRoot, recursive: true); } catch { }
        }

        [Test]
        public void DeletingEngagementPrefix_LeavesGripePrefixUntouched()
        {
            const string slug = "test-book";
            var beatId = Guid.NewGuid();
            svc.Upsert($"node:{slug}/beat:{beatId:N}", null, FindingCategory.ReaderGripe, FindingSeverity.Low,
                "GRIPE beat #12 (2 voter(s)): the dialogue tag here is repetitive", null, null);
            svc.Upsert($"node:{slug}#fullorderread/beat:{beatId:N}", null, FindingCategory.ReaderGripe, FindingSeverity.High,
                "ENGAGEMENT beat #40 (3 voter(s), never recovered): the middle third goes flat", null, null);

            svc.DeleteBySummaryPrefix($"node:{slug}#fullorderread", "ENGAGEMENT");

            var remaining = svc.List();
            Assert.That(remaining.Any(f => f.Summary.StartsWith("GRIPE")), Is.True,
                "the gripe jury's own finding must survive a full-order-read re-run");
            Assert.That(remaining.Any(f => f.Summary.StartsWith("ENGAGEMENT")), Is.False,
                "the full-order read's own finding must be cleared by its own delete-then-recreate pass");
        }

        [Test]
        public void DeletingGripePrefix_LeavesEngagementPrefixUntouched()
        {
            const string slug = "test-book";
            var beatId = Guid.NewGuid();
            svc.Upsert($"node:{slug}/beat:{beatId:N}", null, FindingCategory.ReaderGripe, FindingSeverity.Low,
                "GRIPE beat #12 (2 voter(s)): the dialogue tag here is repetitive", null, null);
            svc.Upsert($"node:{slug}#fullorderread/beat:{beatId:N}", null, FindingCategory.ReaderGripe, FindingSeverity.High,
                "ENGAGEMENT beat #40 (3 voter(s), never recovered): the middle third goes flat", null, null);

            svc.DeleteBySummaryPrefix($"node:{slug}", "GRIPE");

            var remaining = svc.List();
            Assert.That(remaining.Any(f => f.Summary.StartsWith("ENGAGEMENT")), Is.True,
                "the full-order read's own finding must survive a gripe-jury re-run");
            Assert.That(remaining.Any(f => f.Summary.StartsWith("GRIPE")), Is.False);
        }
    }
}
