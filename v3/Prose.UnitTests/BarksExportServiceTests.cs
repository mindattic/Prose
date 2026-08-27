using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Locks in the contract of <see cref="BarksExportService"/> (portable-writing-service plan,
/// Phase 4): a beat with text but no recorded POV (<c>BeatEntityPresence.PresenceType = 'pov'</c>
/// — a raw-SQL-only table with no EF mapping, created ad hoc in the SQLite fixture, same pattern
/// BlastRadiusServiceTests already uses) is skipped and counted, never silently dropped.
///
/// The positive "exports the single-POV beat" path is NOT covered here: <see
/// cref="VerificationContextService.GetPovEntityIdAsync"/> (which this service deliberately
/// reuses rather than re-deriving POV resolution — see this service's own doc comment) issues a
/// raw <c>SELECT TOP 1 ...</c> query, valid T-SQL against the real SQL Server database this
/// project actually runs on, but invalid syntax against the SQLite provider these tests use — the
/// query throws, the method's own catch-all swallows it, and POV resolution silently comes back
/// null regardless of whether a matching row exists. This is a pre-existing, already-accepted
/// test/prod SQL-dialect split (not something this plan's scope touches — VerificationContextService
/// is a widely-shared service across the whole enrichment chain), evidenced by
/// BlastRadiusServiceTests itself never routing through this same method for its own
/// BeatEntityPresence reads. The export path is verified manually instead — see the plan doc's
/// "prose --barks-export" smoke check.
/// </summary>
[TestFixture]
public class BarksExportServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private FakeUniverseContext universe = null!;
    private BarksExportService svc = null!;

    [SetUp]
    public async Task SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-barks-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "barks-export");

        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS BeatEntityPresence (BeatId TEXT NOT NULL, EntityId TEXT NOT NULL, EntityName TEXT NOT NULL, PresenceType TEXT NOT NULL)");

        var testUniverseId = Guid.NewGuid();
        universe = new FakeUniverseContext(testUniverseId, "test-universe");

        var audioStore = new LocalDiskAudioStore(paths, NullLogger<LocalDiskAudioStore>.Instance);
        var workbench = new NodeWorkbenchService(dbFactory, null!, paths, audioStore, NullLogger<NodeWorkbenchService>.Instance,
            null!, null!, null!, null!, null!);
        var verificationContext = new VerificationContextService(dbFactory, NullLogger<VerificationContextService>.Instance);

        svc = new BarksExportService(dbFactory, universe, workbench, verificationContext);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    [Test]
    public async Task ExportAsync_BeatWithNoPovRecorded_IsSkippedAndCounted_NotExported()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var chapter = new ChapterNode { Id = Guid.NewGuid(), Slug = "ch1-" + Guid.NewGuid().ToString("N")[..8], Title = "Chapter 1" };
        db.Nodes.Add(chapter);

        var noPovBeat = new Beat { Id = Guid.NewGuid(), Number = 0, Text = "Two figures argued in the dark, neither backing down." };
        var emptyBeat = new Beat { Id = Guid.NewGuid(), Number = 1, Text = "" }; // not yet authored — not a "skip"
        db.Beats.AddRange(noPovBeat, emptyBeat);
        db.BeatNodes.Add(new BeatNode { NodeId = chapter.Id, BeatId = noPovBeat.Id, SortKey = 0 });
        db.BeatNodes.Add(new BeatNode { NodeId = chapter.Id, BeatId = emptyBeat.Id, SortKey = 100 });
        await db.SaveChangesAsync();

        var result = await svc.ExportAsync("test-universe", chapter.Slug);

        Assert.That(result.Barks, Is.Empty);
        Assert.That(result.Skipped, Is.EqualTo(1), "the no-POV beat must be counted, not silently dropped");
    }

    [Test]
    public void ExportAsync_UnknownUniverse_ThrowsInvalidOperationException()
    {
        Assert.ThrowsAsync<InvalidOperationException>(() => svc.ExportAsync("no-such-universe"));
    }

    // ── A minimal IUniverseContext for the tests. ──
    private sealed class FakeUniverseContext(Guid initialId, string slug) : IUniverseContext
    {
        public Guid CurrentId { get; private set; } = initialId;
        public string CurrentSlug => slug;
        public UniverseInfo? CurrentUniverse => new(CurrentId, slug, "Test", null, null, true, 100);
        public IReadOnlyList<UniverseInfo> ListUniverses() => new List<UniverseInfo> { CurrentUniverse! };
        public bool IsGlmz => false;
        public string UniverseGroundingOr(string glmzFallback) => glmzFallback;
        public void UseUniverse(Guid newId) { CurrentId = newId; }
        public bool UseUniverseBySlug(string s) => false;
        public void SetFlowUniverse(Guid? newId) { }
        public void PersistAsDefault(Guid newId) { }
        public void Refresh() { }
    }
}
