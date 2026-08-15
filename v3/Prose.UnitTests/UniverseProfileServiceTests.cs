using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Covers <see cref="UniverseProfileService"/>'s v1 scope — closed-form density baselines
/// (mean/stdev) aggregated from already-persisted <see cref="BeatProseMetrics"/> rows, grouped by
/// the owning book's <see cref="Node.UniverseId"/>. Pure EF LINQ, no SQL Server-specific raw SQL,
/// so (unlike most of the AutoCorrect fix machinery) this runs fine against the SQLite test
/// provider.
/// </summary>
[TestFixture]
public class UniverseProfileServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private UniverseProfileService svc = null!;
    private Guid universeId;
    private Guid otherUniverseId;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-universe-profile-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "universe-profile");
        svc = new UniverseProfileService(dbFactory);
        universeId = Guid.CreateVersion7();
        otherUniverseId = Guid.CreateVersion7();
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Guid> SeedBookAsync(Guid universe)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.CreateVersion7();
        db.Nodes.Add(new BookNode { Id = id, UniverseId = universe, Slug = "book-" + id.ToString("N"), Title = "Test Book" });
        await db.SaveChangesAsync();
        return id;
    }

    private static int nextBeatNumber = 1;

    private async Task SeedMetricsAsync(Guid nodeId, params double[] fleschScores)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        double sortKey = 100;
        foreach (var score in fleschScores)
        {
            var beatId = Guid.CreateVersion7();
            db.Beats.Add(new Beat { Id = beatId, Number = nextBeatNumber++, Text = "Test beat text." });
            db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beatId, SortKey = sortKey });
            db.BeatProseMetrics.Add(new BeatProseMetrics
            {
                BeatId = beatId,
                NodeId = nodeId,
                FleschReadingEase = score,
                FleschKincaidGrade = 8,
                TypeTokenRatio = 0.5,
                DialogueProportion = 0.2,
                AvgWordsPerSentence = 12,
            });
            sortKey += 100;
        }
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task RefreshDensityBaselinesAsync_BelowMinSampleSize_ReturnsZeroAndPersistsNothing()
    {
        var book = await SeedBookAsync(universeId);
        await SeedMetricsAsync(book, 60, 65); // only 2 — below the 5-sample floor

        var count = await svc.RefreshDensityBaselinesAsync(universeId);

        Assert.That(count, Is.EqualTo(0));
        var baseline = await svc.GetBaselineAsync(universeId, "density-baseline:flesch-reading-ease");
        Assert.That(baseline, Is.Null);
    }

    [Test]
    public async Task RefreshDensityBaselinesAsync_ComputesMeanAndStdDev_ScopedToItsOwnUniverse()
    {
        var book = await SeedBookAsync(universeId);
        await SeedMetricsAsync(book, 60, 62, 58, 61, 59); // mean 60, small spread

        var otherBook = await SeedBookAsync(otherUniverseId);
        await SeedMetricsAsync(otherBook, 10, 10, 10, 10, 10); // very different universe, must not leak in

        var count = await svc.RefreshDensityBaselinesAsync(universeId);
        Assert.That(count, Is.EqualTo(5));

        var baseline = await svc.GetBaselineAsync(universeId, "density-baseline:flesch-reading-ease");
        Assert.That(baseline, Is.Not.Null);
        Assert.That(baseline!.Mean, Is.EqualTo(60).Within(0.01));
        Assert.That(baseline.SampleSize, Is.EqualTo(5));
        Assert.That(baseline.StdDev, Is.GreaterThan(0));

        var otherBaseline = await svc.GetBaselineAsync(otherUniverseId, "density-baseline:flesch-reading-ease");
        Assert.That(otherBaseline, Is.Null, "other universe was never refreshed, so it should have no persisted baseline yet");
    }

    [Test]
    public async Task RefreshDensityBaselinesAsync_ReRun_UpsertsRatherThanDuplicating()
    {
        var book = await SeedBookAsync(universeId);
        await SeedMetricsAsync(book, 60, 62, 58, 61, 59);
        await svc.RefreshDensityBaselinesAsync(universeId);

        await SeedMetricsAsync(book, 90, 90, 90, 90, 90); // shift the distribution
        await svc.RefreshDensityBaselinesAsync(universeId);

        await using var db = await dbFactory.CreateDbContextAsync();
        var rows = await db.UniverseProfiles
            .Where(p => p.UniverseId == universeId && p.MetricKey == "density-baseline:flesch-reading-ease")
            .ToListAsync();
        Assert.That(rows, Has.Count.EqualTo(1), "a second refresh must update the existing row, not insert a duplicate");

        var baseline = await svc.GetBaselineAsync(universeId, "density-baseline:flesch-reading-ease");
        Assert.That(baseline!.SampleSize, Is.EqualTo(10));
    }
}
