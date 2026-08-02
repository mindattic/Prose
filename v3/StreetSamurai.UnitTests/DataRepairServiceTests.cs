using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Rewritten for the 2026-05-08 JSON→SQL canon migration: DataRepairService's Zone Inference
/// and Wiki Link Writer tools now read/write Records.Json directly via SQL (ListEntitiesByType /
/// LoadEntityJson / SaveEntityJson), not engine_data/*.json files. Seeds an Entity + Record row
/// per fixture instead of writing a file. Tool 1 (Fact Repair, a separate lore-triples.db-backed
/// path) and Tool 2 (Territory Assignment) are untested here, matching the original file's scope.
/// </summary>
[TestFixture]
public class DataRepairServiceTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<StreetSamuraiDbContext> factory = null!;
    private DataRepairService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_dr_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "datarepair");

        svc = new DataRepairService(paths, factory, NullLoggers.For<DataRepairService>());

        // Start with all tools off; each test opts in
        svc.RunFactRepair          = false;
        svc.RunTerritoryAssignment = false;
        svc.RunZoneInference       = false;
        svc.RunWikiLinkWriter      = false;
        svc.DryRun                 = false;
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private Guid SeedEntity(string entityType, object data, string? name = null)
    {
        var id = Guid.NewGuid();
        using var db = factory.CreateDbContext();
        db.Entities.Add(new Entity
        {
            Id         = id,
            EntityType = entityType,
            Name       = name ?? "Test Entity",
            Slug       = $"test-entity-{id:N}",
            Status     = "canon",
            IsActive   = true,
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
        var raw = db.Records.AsNoTracking().Single(r => r.EntityId == id).Json;
        return JsonNode.Parse(raw) as JsonObject ?? throw new InvalidOperationException();
    }

    // ── Zone Inference: InferZone correctness ────────────────────────────────

    [Test]
    public async Task ZoneInference_SouthSideLatLng_AssignsZ6()
    {
        // lat 41.84, lng -87.6 → Z6 (South Side)
        var id = SeedEntity("place", new { name = "South Station", lat = 41.84, lng = -87.60 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["zone"]?.GetValue<string>(), Is.EqualTo("Z6"));
    }

    [Test]
    public async Task ZoneInference_IndianaArc_AssignsZ11()
    {
        // lat < 41.60 → Z11 (Indiana arc)
        var id = SeedEntity("place", new { name = "Gary Plant", lat = 41.55, lng = -87.34 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["zone"]?.GetValue<string>(), Is.EqualTo("Z11"));
    }

    [Test]
    public async Task ZoneInference_GoldCoast_AssignsZ2()
    {
        // lat 41.98 (>41.93, <42.01) → Z2
        var id = SeedEntity("place", new { name = "Lakeview Tower", lat = 41.97, lng = -87.65 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["zone"]?.GetValue<string>(), Is.EqualTo("Z2"));
    }

    [Test]
    public async Task ZoneInference_Evanston_AssignsZ3()
    {
        // lat 42.05 (>42.01, <42.13) → Z3
        var id = SeedEntity("place", new { name = "Evanston Hub", lat = 42.05, lng = -87.68 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["zone"]?.GetValue<string>(), Is.EqualTo("Z3"));
    }

    [Test]
    public async Task ZoneInference_NorthShore_AssignsZ4()
    {
        // lat 42.25 (>42.13, <42.40) → Z4
        var id = SeedEntity("place", new { name = "Waukegan Yard", lat = 42.25, lng = -87.84 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["zone"]?.GetValue<string>(), Is.EqualTo("Z4"));
    }

    [Test]
    public async Task ZoneInference_Kenosha_AssignsZ7()
    {
        // lat 42.60 (>42.40, <42.81) → Z7
        var id = SeedEntity("place", new { name = "Kenosha Docks", lat = 42.60, lng = -87.82 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["zone"]?.GetValue<string>(), Is.EqualTo("Z7"));
    }

    [Test]
    public async Task ZoneInference_Milwaukee_AssignsZ8()
    {
        // lat 43.05 (>42.81, <43.26) → Z8
        var id = SeedEntity("place", new { name = "Milwaukee Hub", lat = 43.05, lng = -87.90 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["zone"]?.GetValue<string>(), Is.EqualTo("Z8"));
    }

    [Test]
    public async Task ZoneInference_Sheboygan_AssignsZ9()
    {
        // lat 43.50 (>43.26, <43.79) → Z9
        var id = SeedEntity("place", new { name = "Sheboygan Breakwater", lat = 43.50, lng = -87.71 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["zone"]?.GetValue<string>(), Is.EqualTo("Z9"));
    }

    [Test]
    public async Task ZoneInference_GreenBay_AssignsZ10()
    {
        // lat > 43.79 → Z10
        var id = SeedEntity("place", new { name = "Green Bay Terminal", lat = 44.50, lng = -88.00 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["zone"]?.GetValue<string>(), Is.EqualTo("Z10"));
    }

    [Test]
    public async Task ZoneInference_PlaceWithExistingZone_Skipped()
    {
        var id = SeedEntity("place", new { name = "Already Zoned", lat = 41.97, lng = -87.65, zone = "Z1" });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        // Zone should NOT be overwritten
        Assert.That(ReadEntity(id)["zone"]?.GetValue<string>(), Is.EqualTo("Z1"));
    }

    [Test]
    public async Task ZoneInference_PlaceWithoutLatLng_Skipped()
    {
        var id = SeedEntity("place", new { name = "No Coords", description = "A mysterious place." });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(id)["zone"], Is.Null);
    }

    [Test]
    public async Task ZoneInference_DryRun_DoesNotWriteRecord()
    {
        var id = SeedEntity("place", new { name = "Dry Run Place", lat = 43.05, lng = -87.90 });
        svc.RunZoneInference = true;
        svc.DryRun = true;
        var before = ReadEntity(id).ToJsonString();

        await svc.RunAsync();

        Assert.That(ReadEntity(id).ToJsonString(), Is.EqualTo(before));
    }

    // ── Wiki Link Writer: InsertWikiLinks ────────────────────────────────────

    [Test]
    public async Task WikiLinks_EntityMentionedByName_IsLinked()
    {
        // Create two people: one mentions the other's name in description
        SeedEntity("character", new { name = "Kira Voss", description = "A fixer." }, name: "Kira Voss");
        var id = SeedEntity("character", new { name = "Marcus Wren", description = "Works with Kira Voss on contracts." }, name: "Marcus Wren");
        svc.RunWikiLinkWriter = true;

        await svc.RunAsync();

        var desc = ReadEntity(id)["description"]?.GetValue<string>();
        Assert.That(desc, Does.Contain("[[Kira Voss]]"));
    }

    [Test]
    public async Task WikiLinks_SelfNameNotLinked()
    {
        var id = SeedEntity("character", new { name = "Kira Voss", description = "Kira Voss is a fixer." }, name: "Kira Voss");
        svc.RunWikiLinkWriter = true;

        await svc.RunAsync();

        var desc = ReadEntity(id)["description"]?.GetValue<string>();
        Assert.That(desc, Does.Not.Contain("[[Kira Voss]]"));
    }

    [Test]
    public async Task WikiLinks_AlreadyLinkedName_NotDoubleLinked()
    {
        SeedEntity("character", new { name = "Kira Voss", description = "A fixer." }, name: "Kira Voss");
        var id = SeedEntity("character", new { name = "Marcus", description = "Reports to [[Kira Voss]] always." }, name: "Marcus");
        svc.RunWikiLinkWriter = true;

        await svc.RunAsync();

        var desc = ReadEntity(id)["description"]?.GetValue<string>() ?? "";
        // Count occurrences of [[Kira Voss]] — should be exactly 1
        Assert.That(System.Text.RegularExpressions.Regex.Matches(desc, @"\[\[Kira Voss\]\]").Count, Is.EqualTo(1));
    }

    [Test]
    public async Task WikiLinks_NameShorterThan4Chars_NotLinked()
    {
        // "Ax" is < 4 chars — should not be linked
        SeedEntity("character", new { name = "Ax", description = "Short name entity." }, name: "Ax");
        var id = SeedEntity("character", new { name = "Other", description = "Saw Ax near the zone." }, name: "Other");
        svc.RunWikiLinkWriter = true;

        await svc.RunAsync();

        var desc = ReadEntity(id)["description"]?.GetValue<string>();
        Assert.That(desc, Does.Not.Contain("[[Ax]]"));
    }

    [Test]
    public async Task WikiLinks_DryRun_DoesNotWriteLinks()
    {
        SeedEntity("character", new { name = "Kira Voss", description = "A fixer." }, name: "Kira Voss");
        var id = SeedEntity("character", new { name = "Marcus", description = "Kira Voss owes him one." }, name: "Marcus");
        svc.RunWikiLinkWriter = true;
        svc.DryRun = true;
        var before = ReadEntity(id).ToJsonString();

        await svc.RunAsync();

        Assert.That(ReadEntity(id).ToJsonString(), Is.EqualTo(before));
    }

    [Test]
    public async Task WikiLinks_DryRun_StillCountsLinks()
    {
        SeedEntity("character", new { name = "Kira Voss", description = "A fixer." }, name: "Kira Voss");
        SeedEntity("character", new { name = "Marcus", description = "Kira Voss owes him one." }, name: "Marcus");
        svc.RunWikiLinkWriter = true;
        svc.DryRun = true;

        await svc.RunAsync();

        Assert.That(svc.WikiLinksInserted, Is.GreaterThan(0));
    }

    // ── ChangeLog entries ─────────────────────────────────────────────────────

    [Test]
    public async Task ChangeLog_ZoneInferred_LogEntry()
    {
        SeedEntity("place", new { name = "Waukegan Yard", lat = 42.25, lng = -87.84 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(svc.ChangeLog.Any(e => e.Contains("[Zone]")), Is.True);
    }

    [Test]
    public async Task ChangeLog_DryRun_ContainsDryRunMarker()
    {
        SeedEntity("place", new { name = "Dry Zone", lat = 42.25, lng = -87.84 });
        svc.RunZoneInference = true;
        svc.DryRun = true;

        await svc.RunAsync();

        Assert.That(svc.ChangeLog.Any(e => e.Contains("[DRY RUN]")), Is.True);
    }
}
