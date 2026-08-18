using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Rewritten for the 2026-05-08 JSON→SQL canon migration: WorldConsistencyService's
/// rule-scan phase now reads Records.Json blobs directly from SQL (CollectRecords),
/// not engine_data/*.json files. Seeds an Entity + Record row per fixture instead of
/// writing a file.
/// </summary>
[TestFixture]
public class WorldConsistencyServiceTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;
    private WorldConsistencyService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_wcs_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "consistency");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient<ClaudeService>();
        services.AddSingleton(new SettingsService(tempDir));
        var provider = services.BuildServiceProvider();

        svc = new WorldConsistencyService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            factory,
            NullLoggers.For<WorldConsistencyService>());

        svc.RunRuleScan      = true;
        svc.RunConflictCheck = false; // disable Claude-dependent check
        svc.RunDedup         = false;
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private void SeedEntity(object data, string entityType = "character")
    {
        var id = Guid.NewGuid();
        using var db = factory.CreateDbContext();
        db.Entities.Add(new Entity
        {
            Id         = id,
            EntityType = entityType,
            Name       = "Test Entity",
            Slug       = $"test-entity-{id:N}",
            Status     = "canon",
            
            CreatedAt  = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
        });
        db.Records.Add(new Record { EntityId = id, Json = JsonSerializer.Serialize(data) });
        db.SaveChanges();
    }

    // ── Rule scan: no violations ───────────────────────────────────────────

    [Test]
    public async Task RuleScan_CleanEntities_NoViolations()
    {
        SeedEntity(new { name = "Kira Voss", description = "A freelancer in Z2." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations, Is.Empty);
    }

    // ── Rule: no city police ──────────────────────────────────────────────

    [Test]
    public async Task RuleScan_MetroPoliceInDescription_ReportsViolation()
    {
        SeedEntity(new { name = "Bent Cop", description = "Works for the metro police precinct in Z1." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations, Has.Count.GreaterThan(0));
        Assert.That(svc.RuleViolations[0].Rule, Does.Contain("city police"));
    }

    [Test]
    public async Task RuleScan_MeridianPdInDescription_ReportsViolation()
    {
        SeedEntity(new { name = "Badge", description = "Active Meridian PD detective patrolling Z2." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations.Any(v => v.Rule.Contains("city police")), Is.True);
    }

    // ── Rule: Behemoths not alive ─────────────────────────────────────────

    [Test]
    public async Task RuleScan_BehemothIsAlive_ReportsViolation()
    {
        SeedEntity(new { name = "Watcher", description = "The Iowan Behemoth is a living machine." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations.Any(v => v.Rule.Contains("Behemoth")), Is.True);
    }

    // ── Rule: Phi symbol ────────────────────────────────────────────────────

    [Test]
    public async Task RuleScan_PhiSymbolMisdescribed_ReportsViolation()
    {
        SeedEntity(new { name = "Currency Note", description = "The phi symbol represents the letter phi in Greek." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations.Any(v => v.Rule.Contains("phi") || v.Rule.Contains("Φ")), Is.True);
    }

    // ── Rule: The Shelf ─────────────────────────────────────────────────────

    [Test]
    public async Task RuleScan_TheShelfDistrict_ReportsViolation()
    {
        SeedEntity(new { name = "Resident", description = "Lives in the shelf district near the wall." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations.Any(v => v.Rule.Contains("Shelf")), Is.True);
    }

    // ── Rule: wedding cake tiers ────────────────────────────────────────────

    [Test]
    public async Task RuleScan_WeddingCakeTiers_ReportsViolation()
    {
        SeedEntity(new { name = "Skyline", description = "GLMZ rises in a wedding cake tiers pattern above the bay." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations.Any(v => v.Rule.Contains("wedding cake")), Is.True);
    }

    // ── Rule: Ferrogate railroad ────────────────────────────────────────────

    [Test]
    public async Task RuleScan_FerrogateRailroad_ReportsViolation()
    {
        SeedEntity(new { name = "Old Line", description = "Cargo moves on the Ferrogate railroad south of the wall." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations.Any(v => v.Rule.Contains("Ferrogate")), Is.True);
    }

    // ── Violation record shape ──────────────────────────────────────────────

    [Test]
    public async Task RuleScan_ViolationContainsEntityName()
    {
        SeedEntity(new { name = "Bent Cop", description = "Works for the metro police precinct." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations[0].EntityName, Is.EqualTo("Bent Cop"));
    }

    [Test]
    public async Task RuleScan_ViolationContainsMatchedText()
    {
        SeedEntity(new { name = "Bent Cop", description = "Works for the metro police precinct." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations[0].MatchedText, Does.Contain("metro police"));
    }

    // ── Dedup with no Claude ──────────────────────────────────────────────

    [Test]
    public async Task Dedup_IdenticalNames_ReportedAsDuplicate()
    {
        var names = new[] { "Alex Kron", "Alex Kron" };
        foreach (var n in names)
            SeedEntity(new { name = n, description = "A person." });

        svc.RunRuleScan      = false;
        svc.RunConflictCheck = false;
        svc.RunDedup         = true;
        svc.DedupThreshold   = 0.90;

        await svc.RunAsync();

        Assert.That(svc.Duplicates, Has.Count.GreaterThan(0));
    }

    [Test]
    public async Task Dedup_VeryDifferentNames_NotDuplicates()
    {
        SeedEntity(new { name = "Alex Kron", description = "Person." });
        SeedEntity(new { name = "Zephyr Nakamura-Bell", description = "Other person." });

        svc.RunRuleScan      = false;
        svc.RunConflictCheck = false;
        svc.RunDedup         = true;
        svc.DedupThreshold   = 0.90;

        await svc.RunAsync();

        Assert.That(svc.Duplicates, Is.Empty);
    }

    [Test]
    public async Task Dedup_NearMatchNames_ReportedAboveThreshold()
    {
        // "Kira Voss" vs "Kira Voss" — identical, score = 1.0
        SeedEntity(new { name = "Kira Voss", description = "First." });
        SeedEntity(new { name = "Kira Voss", description = "Second." });

        svc.RunRuleScan      = false;
        svc.RunConflictCheck = false;
        svc.RunDedup         = true;
        svc.DedupThreshold   = 0.95;

        await svc.RunAsync();

        Assert.That(svc.Duplicates.Any(d => d.Score >= 0.95), Is.True);
    }

    [Test]
    public async Task Dedup_ThresholdFiltersLowSimilarity()
    {
        SeedEntity(new { name = "Alex Kron", description = "." });
        SeedEntity(new { name = "Alexa Krone", description = "." }); // similar but not above 0.95

        svc.RunRuleScan      = false;
        svc.RunConflictCheck = false;
        svc.RunDedup         = true;
        svc.DedupThreshold   = 0.99; // extremely strict

        await svc.RunAsync();

        Assert.That(svc.Duplicates, Is.Empty);
    }

    // ── Cancel clears results ─────────────────────────────────────────────

    [Test]
    public void Cancel_ClearsAccumulatedResults()
    {
        // Manually set some fake results to verify Cancel clears them
        svc.RuleViolations.Add(new WorldConsistencyService.RuleViolation("f", "E", "R", "M"));

        svc.Cancel();

        Assert.That(svc.RuleViolations, Is.Empty);
    }
}
