using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-10 <see cref="BeatVerification.RuleVersion"/> addition.
/// This exact staleness gap — a check-logic fix lands, but nothing records which books' cached
/// <see cref="BeatVerification"/> rows still reflect the OLD logic — was found and manually
/// re-diffed against commit timestamps twice in one session (first for 6 books, then for 5 more
/// the first pass missed). <see cref="BeatVerificationService.GetStaleBookSlugsAsync"/> answers
/// "which books need a re-run" as a direct query instead.
/// </summary>
[TestFixture]
public class BeatVerificationServiceStalenessTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-beatverification-staleness-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<(Guid NodeId, Guid BeatId)> SeedNodeWithBeatAndVerificationAsync(string? ruleVersion)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = Guid.CreateVersion7();
        var node = NodeFactory.Create("book");
        node.Id = nodeId;
        node.Slug = "s-" + Guid.NewGuid().ToString("N")[..8];
        node.Title = "Staleness Test Book";
        node.Status = "draft";
        node.SortKey = 100;
        db.Nodes.Add(node);

        var beat = new Beat { Id = Guid.CreateVersion7(), Number = new Random().Next(1, 999999), Text = "Some prose." };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beat.Id, SortKey = 1, IsEnabled = true });

        db.BeatVerifications.Add(new BeatVerification
        {
            Id = Guid.NewGuid(),
            BeatId = beat.Id,
            CheckType = "BannedPattern",
            Result = "Pass",
            Severity = "BLOCKER",
            VerifiedBy = "mechanical",
            RuleVersion = ruleVersion,
        });

        await db.SaveChangesAsync();
        return (nodeId, beat.Id);
    }

    [Test]
    public async Task LegacyNullRuleVersion_IsReportedStale()
    {
        var (_, _) = await SeedNodeWithBeatAndVerificationAsync(ruleVersion: null);

        var svc = new BeatVerificationService(dbFactory, NullLogger<BeatVerificationService>.Instance);
        var stale = await svc.GetStaleBookSlugsAsync();

        Assert.That(stale, Has.Count.EqualTo(1));
        Assert.That(stale[0].StaleRows, Is.EqualTo(1));
        Assert.That(stale[0].TotalRows, Is.EqualTo(1));
    }

    [Test]
    public async Task OldVersionString_IsReportedStale()
    {
        await SeedNodeWithBeatAndVerificationAsync(ruleVersion: "v0-does-not-exist");

        var svc = new BeatVerificationService(dbFactory, NullLogger<BeatVerificationService>.Instance);
        var stale = await svc.GetStaleBookSlugsAsync();

        Assert.That(stale, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CurrentRuleVersion_IsNotReportedStale()
    {
        await SeedNodeWithBeatAndVerificationAsync(ruleVersion: BeatVerificationService.CurrentRuleVersion);

        var svc = new BeatVerificationService(dbFactory, NullLogger<BeatVerificationService>.Instance);
        var stale = await svc.GetStaleBookSlugsAsync();

        Assert.That(stale, Is.Empty);
    }

    [Test]
    public async Task VerifyBeatAsync_StampsCurrentRuleVersion()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = Guid.CreateVersion7();
        var node = NodeFactory.Create("book");
        node.Id = nodeId;
        node.Slug = "s-" + Guid.NewGuid().ToString("N")[..8];
        node.Title = "T";
        node.Status = "draft";
        node.SortKey = 100;
        db.Nodes.Add(node);
        var beat = new Beat { Id = Guid.CreateVersion7(), Number = new Random().Next(1, 999999), Text = "Some prose to verify." };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beat.Id, SortKey = 1, IsEnabled = true });
        await db.SaveChangesAsync();

        var svc = new BeatVerificationService(dbFactory, NullLogger<BeatVerificationService>.Instance);
        await svc.VerifyBeatAsync(beat.Id);

        await using var verifyDb = await dbFactory.CreateDbContextAsync();
        var rows = await verifyDb.BeatVerifications.Where(v => v.BeatId == beat.Id).ToListAsync();
        Assert.That(rows, Is.Not.Empty);
        Assert.That(rows, Has.All.Property(nameof(BeatVerification.RuleVersion)).EqualTo(BeatVerificationService.CurrentRuleVersion));
    }

    /// <summary>
    /// Regression cover for the 2026-08-10 orphaned-row bug: TRUCE beat #16241's
    /// BeatBlueprintDecision was deleted (a blueprint restructuring), so VerifyBeatAsync
    /// correctly stopped producing EventType/SubplotCarrier/EscalationFloor/DeclaredPurpose
    /// results for it — but the OLD rows from when the decision existed were never cleaned up,
    /// so they survived 3+ separate --audit-book re-runs across one session, immune to every
    /// refresh because nothing was ever in `results` to overwrite them. VerifyBeatAsync must
    /// reap any existing per-beat-check row whose CheckType isn't in the current run's results.
    /// </summary>
    [Test]
    public async Task VerifyBeatAsync_ReapsOrphanedRowsWhenDecisionNoLongerExists()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var nodeId = Guid.CreateVersion7();
        var node = NodeFactory.Create("book");
        node.Id = nodeId;
        node.Slug = "s-" + Guid.NewGuid().ToString("N")[..8];
        node.Title = "T";
        node.Status = "draft";
        node.SortKey = 100;
        db.Nodes.Add(node);
        var beat = new Beat { Id = Guid.CreateVersion7(), Number = new Random().Next(1, 999999), Text = "Some prose." };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beat.Id, SortKey = 1, IsEnabled = true });

        // Simulate the "checked while a decision existed" state directly — these four rows are
        // exactly what a prior VerifyBeatAsync run would have left behind before the decision
        // that produced them was deleted.
        foreach (var checkType in new[] { "EventType", "SubplotCarrier", "EscalationFloor", "DeclaredPurpose" })
        {
            db.BeatVerifications.Add(new BeatVerification
            {
                Id = Guid.NewGuid(),
                BeatId = beat.Id,
                CheckType = checkType,
                Result = "Partial",
                Severity = "MINOR",
                VerifiedBy = "mechanical",
                RuleVersion = null,
            });
        }
        await db.SaveChangesAsync();
        // No BeatBlueprintDecision row exists for this beat — matches the bug's real condition.

        var svc = new BeatVerificationService(dbFactory, NullLogger<BeatVerificationService>.Instance);
        await svc.VerifyBeatAsync(beat.Id);

        await using var verifyDb = await dbFactory.CreateDbContextAsync();
        var remaining = await verifyDb.BeatVerifications.Where(v => v.BeatId == beat.Id).ToListAsync();

        Assert.That(remaining.Select(r => r.CheckType), Is.EquivalentTo(new[] { "BannedPattern" }),
            "the four orphaned rows (no longer producible without a BeatBlueprintDecision) must be reaped, leaving only the current run's BannedPattern result");
    }
}
