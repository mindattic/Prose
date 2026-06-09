using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

// RETIRED by the 2026-05-08 JSON→SQL canon migration. These tests seed file-based
// engine_data/people/*.json fixtures and assert the rule scan flags violations from them,
// but WorldConsistencyService now scans the SQL DB (an empty in-memory TestDbFactory here),
// so written files are ignored. The "clean → no violations" case passes only vacuously.
// To re-enable: rewrite to seed the SQL test DB instead of writing JSON files.
[TestFixture]
[Ignore("Retired file-based path (2026-05-08 JSON→SQL migration); rewrite to seed the SQL test DB. See class comment.")]
public class WorldConsistencyServiceTests
{
    private string tempDir = "";
    private string peopleDir = "";
    private WorldConsistencyService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir    = Path.Combine(Path.GetTempPath(), $"ss_wcs_{Guid.NewGuid():N}");
        peopleDir  = Path.Combine(tempDir, "engine_data", "people");
        Directory.CreateDirectory(peopleDir);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient<ClaudeService>();
        services.AddSingleton(new SettingsService(tempDir));
        var provider = services.BuildServiceProvider();

        var paths = new TestPathProviderWithRoot(tempDir);
        svc = new WorldConsistencyService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            StreetSamurai.Core.Data.TestDbFactory.For(paths, "consistency"),
            NullLoggers.For<WorldConsistencyService>());

        svc.RunRuleScan      = true;
        svc.RunConflictCheck = false; // disable Claude-dependent check
        svc.RunDedup         = false;
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private void WriteEntity(string dir, object data)
    {
        var path = Path.Combine(dir, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(data));
    }

    // ── Rule scan: no violations ───────────────────────────────────────────

    [Test]
    public async Task RuleScan_CleanEntities_NoViolations()
    {
        WriteEntity(peopleDir, new { name = "Kira Voss", description = "A freelancer in Z2." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations, Is.Empty);
    }

    // ── Rule: no city police ──────────────────────────────────────────────

    [Test]
    public async Task RuleScan_MetroPoliceInDescription_ReportsViolation()
    {
        WriteEntity(peopleDir, new { name = "Bent Cop", description = "Works for the metro police precinct in Z1." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations, Has.Count.GreaterThan(0));
        Assert.That(svc.RuleViolations[0].Rule, Does.Contain("city police"));
    }

    [Test]
    public async Task RuleScan_MeridianPdInDescription_ReportsViolation()
    {
        WriteEntity(peopleDir, new { name = "Badge", description = "Active Meridian PD detective patrolling Z2." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations.Any(v => v.Rule.Contains("city police")), Is.True);
    }

    // ── Rule: Behemoths not alive ─────────────────────────────────────────

    [Test]
    public async Task RuleScan_BehemothIsAlive_ReportsViolation()
    {
        WriteEntity(peopleDir, new { name = "Watcher", description = "The Iowan Behemoth is a living machine." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations.Any(v => v.Rule.Contains("Behemoth")), Is.True);
    }

    // ── Rule: No Shelf ────────────────────────────────────────────────────

    [Test]
    public async Task RuleScan_TheShelfDistrict_ReportsViolation()
    {
        WriteEntity(peopleDir, new { name = "Resident", description = "Lives in the shelf district near the wall." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations.Any(v => v.Rule.Contains("Shelf")), Is.True);
    }

    // ── Rule: No wedding cake ─────────────────────────────────────────────

    [Test]
    public async Task RuleScan_WeddingCakeTiers_ReportsViolation()
    {
        WriteEntity(peopleDir, new { name = "Architect", description = "Designed the tiered wedding cake city layout." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations.Any(v => v.Rule.Contains("wedding cake")), Is.True);
    }

    // ── Rule: Ferrogate ───────────────────────────────────────────────────

    [Test]
    public async Task RuleScan_FerrogateRailroad_ReportsViolation()
    {
        WriteEntity(peopleDir, new { name = "Foreman", description = "Manages the Ferrogate railroad cargo operations." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations.Any(v => v.Rule.Contains("Ferrogate")), Is.True);
    }

    // ── Violation includes entity name and matched text ────────────────────

    [Test]
    public async Task RuleScan_ViolationContainsEntityName()
    {
        WriteEntity(peopleDir, new { name = "Dirty Badge", description = "Currently a Meridian PD officer in active service." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations[0].EntityName, Is.EqualTo("Dirty Badge"));
    }

    [Test]
    public async Task RuleScan_ViolationContainsMatchedText()
    {
        WriteEntity(peopleDir, new { name = "Cop", description = "Reports to the metro police precinct." });

        await svc.RunAsync();

        Assert.That(svc.RuleViolations[0].MatchedText, Is.Not.Empty);
    }

    // ── Dedup with no Claude ──────────────────────────────────────────────

    [Test]
    public async Task Dedup_IdenticalNames_ReportedAsDuplicate()
    {
        var names = new[] { "Alex Kron", "Alex Kron" };
        foreach (var n in names)
            WriteEntity(peopleDir, new { name = n, description = "A person." });

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
        WriteEntity(peopleDir, new { name = "Alex Kron", description = "Person." });
        WriteEntity(peopleDir, new { name = "Zephyr Nakamura-Bell", description = "Other person." });

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
        WriteEntity(peopleDir, new { name = "Kira Voss", description = "First." });
        WriteEntity(peopleDir, new { name = "Kira Voss", description = "Second." });

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
        WriteEntity(peopleDir, new { name = "Alex Kron", description = "." });
        WriteEntity(peopleDir, new { name = "Alexa Krone", description = "." }); // similar but not above 0.95

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
