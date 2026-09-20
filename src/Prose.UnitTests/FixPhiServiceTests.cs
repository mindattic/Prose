using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Rewritten for the 2026-05-08 JSON→SQL canon migration: FixPhiService now
/// scans Records.Json blobs (via DataScanUtility), not engine_data/*.json files.
/// Seeds an Entity + Record row per fixture instead of writing a file.
/// </summary>
[TestFixture]
public class FixPhiServiceTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;
    private FixPhiService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_phi_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "phi");
        svc = new FixPhiService(factory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private Guid SeedEntity(object data)
    {
        var id = Guid.NewGuid();
        using var db = factory.CreateDbContext();
        db.Entities.Add(new Entity
        {
            Id         = id,
            EntityType = "character",
            Name       = "Test Entity",
            Slug       = $"test-entity-{id:N}",
            Status     = "canon",
            
            CreatedAt  = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow,
        });
        db.Records.Add(new Record { EntityId = id, Json = JsonSerializer.Serialize(data) });
        db.SaveChanges();
        return id;
    }

    private JsonObject ReadEntity(Guid id)
    {
        using var db = factory.CreateDbContext();
        var row = db.Records.First(r => r.EntityId == id);
        return JsonNode.Parse(row.Json) as JsonObject ?? throw new InvalidOperationException();
    }

    // ── Phi → Quanta replacement ──────────────────────────────────────────────

    [Test]
    public async Task Run_PhiUppercaseInDescription_ReplacedWithQuanta()
    {
        var id = SeedEntity(new { name = "Test", description = "Costs 50 Phi per dose." });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["description"]?.GetValue<string>(), Is.EqualTo("Costs 50 Quanta per dose."));
    }

    [Test]
    public async Task Run_PhiLowercaseInDescription_ReplacedWithQuanta()
    {
        var id = SeedEntity(new { name = "Test", description = "Price is 100 phi." });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["description"]?.GetValue<string>(), Is.EqualTo("Price is 100 quanta."));
    }

    [Test]
    public async Task Run_PhiSymbolUnchanged()
    {
        var id = SeedEntity(new { name = "Test", description = "Costs Φ500 per unit." });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["description"]?.GetValue<string>(), Is.EqualTo("Costs Φ500 per unit."));
    }

    [Test]
    public async Task Run_MixedPhiAndSymbol_OnlyWordFormReplaced()
    {
        var id = SeedEntity(new { name = "Test", description = "Φ50 phi per transaction." });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["description"]?.GetValue<string>(), Is.EqualTo("Φ50 quanta per transaction."));
    }

    // ── Word-boundary: no partial replacements ────────────────────────────────

    [Test]
    public async Task Run_PhiAsPartOfWord_NotReplaced()
    {
        // "Philip" starts with "Phil" but \bPhi\b should not match "Philip"
        var id = SeedEntity(new { name = "Test", description = "Philip owes 50 Phi." });

        await svc.RunAsync();

        var desc = ReadEntity(id)["description"]?.GetValue<string>();
        Assert.That(desc, Does.Contain("Philip"));
        Assert.That(desc, Does.Contain("Quanta"));
    }

    // ── Skip identity fields ─────────────────────────────────────────────────

    [Test]
    public async Task Run_PhiInNameField_NotReplaced()
    {
        var id = SeedEntity(new { name = "Phi Korvann", description = "A person." });

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["name"]?.GetValue<string>(), Is.EqualTo("Phi Korvann"));
    }

    [Test]
    public async Task Run_PhiInTitle_NotReplaced()
    {
        var id = SeedEntity(new { name = "Person", title = "Phi Chancellor", description = "Costs 10 Phi." });

        await svc.RunAsync();

        var obj = ReadEntity(id);
        Assert.That(obj["title"]?.GetValue<string>(), Is.EqualTo("Phi Chancellor"));
        Assert.That(obj["description"]?.GetValue<string>(), Is.EqualTo("Costs 10 Quanta."));
    }

    [Test]
    public async Task Run_PhiInCodename_NotReplaced()
    {
        var id = SeedEntity(new { name = "Agent", codename = "Operation Phi", description = "Budget is 200 phi." });

        await svc.RunAsync();

        var obj = ReadEntity(id);
        Assert.That(obj["codename"]?.GetValue<string>(), Is.EqualTo("Operation Phi"));
        Assert.That(obj["description"]?.GetValue<string>(), Is.EqualTo("Budget is 200 quanta."));
    }

    // ── Nested objects ───────────────────────────────────────────────────────

    [Test]
    public async Task Run_PhiInNestedObject_Replaced()
    {
        var id = SeedEntity(new { name = "Test", stats = new { note = "Earns 500 phi annually." } });

        await svc.RunAsync();

        var obj = ReadEntity(id);
        var stats = obj["stats"] as JsonObject;
        Assert.That(stats?["note"]?.GetValue<string>(), Is.EqualTo("Earns 500 quanta annually."));
    }

    // ── Arrays ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Run_PhiInArrayElement_Replaced()
    {
        var id = SeedEntity(new { name = "Test", notes = new[] { "Costs 100 phi.", "No phi here actually." } });

        await svc.RunAsync();

        var obj = ReadEntity(id);
        var notes = obj["notes"] as JsonArray;
        Assert.That(notes?[0]?.GetValue<string>(), Is.EqualTo("Costs 100 quanta."));
        Assert.That(notes?[1]?.GetValue<string>(), Is.EqualTo("No quanta here actually."));
    }

    // ── No false positives ────────────────────────────────────────────────────

    [Test]
    public async Task Run_NoPhiPresent_NoChange()
    {
        var id = SeedEntity(new { name = "Test", description = "Nothing currency-related here." });
        var before = ReadEntity(id).ToJsonString();

        await svc.RunAsync();

        Assert.That(ReadEntity(id).ToJsonString(), Is.EqualTo(before));
    }

    // ── Result count ─────────────────────────────────────────────────────────

    [Test]
    public async Task Run_MultiplePhi_ReturnsCorrectChangeCount()
    {
        SeedEntity(new { name = "A", description = "100 Phi for service." });
        SeedEntity(new { name = "B", description = "No currency." });

        var result = await svc.RunAsync();

        Assert.That(result.ChangesApplied, Is.EqualTo(1));
        Assert.That(result.FilesModified, Is.EqualTo(1));
    }

    // ── dryRun (2026-08-09): preview without writing ────────────────────────────
    // This is a mass-mutation utility with no other confirmation step, so a caller
    // must be able to see what WOULD change before committing to a real write.

    [Test]
    public async Task Run_DryRun_ReportsChangeCountButDoesNotWrite()
    {
        var id = SeedEntity(new { name = "Test", description = "Costs 50 Phi per dose." });

        var result = await svc.RunAsync(dryRun: true);

        Assert.That(result.ChangesApplied, Is.EqualTo(1), "dry run must still report what would change");
        Assert.That(ReadEntity(id)["description"]?.GetValue<string>(), Is.EqualTo("Costs 50 Phi per dose."),
            "dry run must never write the mutation back to the DB");
    }

    [Test]
    public async Task Run_DryRunThenRealRun_RealRunStillAppliesTheChange()
    {
        var id = SeedEntity(new { name = "Test", description = "Costs 50 Phi per dose." });

        await svc.RunAsync(dryRun: true);
        await svc.RunAsync();

        Assert.That(ReadEntity(id)["description"]?.GetValue<string>(), Is.EqualTo("Costs 50 Quanta per dose."));
    }
}
