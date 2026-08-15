using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Covers the <see cref="CrossBookConflict.VariantCount"/> / <see cref="CrossBookConflict.MajorityClaimUids"/>
/// / <see cref="CrossBookConflict.MinorityClaimUids"/> fields added 2026-08-14 so
/// <see cref="Prose.Core.Services.AutoCorrectOrchestratorService"/> can tell an unambiguous
/// two-value majority (auto-resolvable) apart from a genuinely ambiguous three-or-more-value split
/// (must stay flag-only) and knows exactly which claims to pass to
/// <see cref="ContinuityService.Resolve"/>.
/// </summary>
[TestFixture]
public class CrossBookConsistencyServiceVariantTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private CrossBookConsistencyService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-crossbook-variant-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "crossbook-variant");
        svc = new CrossBookConsistencyService(dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task SeedClaimAsync(string entityId, string entityName, string predicate, string obj, string bookSlug, string status = "NEW")
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.ContinuityClaims.Add(new ContinuityClaim
        {
            ClaimUid = Guid.NewGuid().ToString("N"),
            EntityId = entityId,
            EntityName = entityName,
            EntityKind = "character",
            Predicate = predicate,
            Object = obj,
            SourceType = "extracted",
            Status = status,
            FirstAssertedAt = DateTime.UtcNow.ToString("o"),
            LastConfirmedAt = DateTime.UtcNow.ToString("o"),
            BookSlug = bookSlug,
            ExtractedBy = ["test"],
        });
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task TwoDistinctValues_CleanMajority_IsFlaggedResolvable()
    {
        var entityId = Guid.NewGuid().ToString("N");
        await SeedClaimAsync(entityId, "Kyle", "eye_color", "brown", "BCODA");
        await SeedClaimAsync(entityId, "Kyle", "eye_color", "brown", "ATTE");
        await SeedClaimAsync(entityId, "Kyle", "eye_color", "blue", "VATD");

        var report = await svc.GetCrossBookConflictsAsync();

        Assert.That(report.Conflicts, Has.Count.EqualTo(1));
        var conflict = report.Conflicts[0];
        Assert.That(conflict.VariantCount, Is.EqualTo(2));
        Assert.That(conflict.MajorityObject, Is.EqualTo("brown"));
        Assert.That(conflict.MajorityCount, Is.EqualTo(2));
        Assert.That(conflict.MajorityClaimUids, Has.Count.EqualTo(2));
        Assert.That(conflict.MinorityObject, Is.EqualTo("blue"));
        Assert.That(conflict.MinorityCount, Is.EqualTo(1));
        Assert.That(conflict.MinorityClaimUids, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ThreeDistinctValues_IsAmbiguous_NotACleanTwoWaySplit()
    {
        var entityId = Guid.NewGuid().ToString("N");
        await SeedClaimAsync(entityId, "Bear", "home_turf", "Sektor 9", "BCODA");
        await SeedClaimAsync(entityId, "Bear", "home_turf", "The Loop", "ATTE");
        await SeedClaimAsync(entityId, "Bear", "home_turf", "Bloom Quarter", "VATD");

        var report = await svc.GetCrossBookConflictsAsync();

        Assert.That(report.Conflicts, Has.Count.EqualTo(1));
        Assert.That(report.Conflicts[0].VariantCount, Is.EqualTo(3),
            "three distinct claimed values across three books is genuinely ambiguous — AutoCorrect must not guess which is real");
    }

    [Test]
    public async Task TiedMajority_ReportsEqualCounts_SoCallerCanRefuseToAutoResolve()
    {
        var entityId = Guid.NewGuid().ToString("N");
        await SeedClaimAsync(entityId, "Wren", "affiliation", "Iron Lotus", "BCODA");
        await SeedClaimAsync(entityId, "Wren", "affiliation", "Independent", "ATTE");

        var report = await svc.GetCrossBookConflictsAsync();

        Assert.That(report.Conflicts, Has.Count.EqualTo(1));
        var conflict = report.Conflicts[0];
        Assert.That(conflict.VariantCount, Is.EqualTo(2));
        Assert.That(conflict.MajorityCount, Is.EqualTo(conflict.MinorityCount),
            "a genuine 1-vs-1 tie must be visible as equal counts, not silently resolved to whichever LINQ happened to order first");
    }

    [Test]
    public async Task SameBookOnly_IsNotSurfaced()
    {
        var entityId = Guid.NewGuid().ToString("N");
        await SeedClaimAsync(entityId, "Bear", "eye_color", "brown", "BCODA");
        await SeedClaimAsync(entityId, "Bear", "eye_color", "blue", "BCODA");

        var report = await svc.GetCrossBookConflictsAsync();

        Assert.That(report.Conflicts, Is.Empty, "same-book contradictions are the per-book continuity system's job, not cross-book");
    }
}
