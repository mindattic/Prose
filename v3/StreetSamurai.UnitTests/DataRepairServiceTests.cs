using System.Text.Json;
using System.Text.Json.Nodes;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class DataRepairServiceTests
{
    private string tempDir = "";
    private string placesDir = "";
    private string peopleDir = "";
    private DataRepairService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir    = Path.Combine(Path.GetTempPath(), $"ss_dr_{Guid.NewGuid():N}");
        placesDir  = Path.Combine(tempDir, "engine_data", "places");
        peopleDir  = Path.Combine(tempDir, "engine_data", "people");
        Directory.CreateDirectory(placesDir);
        Directory.CreateDirectory(peopleDir);

        var paths = new TestPathProviderWithRoot(tempDir);
        svc = new DataRepairService(
            paths,
            StreetSamurai.Core.Data.TestDbFactory.For(paths, "datarepair"),
            NullLoggers.For<DataRepairService>());

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
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private string WritePlaceEntity(object data)
    {
        var path = Path.Combine(placesDir, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(data));
        return path;
    }

    private string WritePeopleEntity(object data)
    {
        var path = Path.Combine(peopleDir, $"{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(data));
        return path;
    }

    private JsonObject ReadEntity(string path) =>
        JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? throw new InvalidOperationException();

    // ── Zone Inference: InferZone correctness ────────────────────────────────

    [Test]
    public async Task ZoneInference_SouthSideLatLng_AssignsZ6()
    {
        // lat 41.84, lng -87.6 → Z6 (South Side)
        var path = WritePlaceEntity(new { name = "South Station", lat = 41.84, lng = -87.60 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["zone"]?.GetValue<string>(), Is.EqualTo("Z6"));
    }

    [Test]
    public async Task ZoneInference_IndianaArc_AssignsZ11()
    {
        // lat < 41.60 → Z11 (Indiana arc)
        var path = WritePlaceEntity(new { name = "Gary Plant", lat = 41.55, lng = -87.34 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["zone"]?.GetValue<string>(), Is.EqualTo("Z11"));
    }

    [Test]
    public async Task ZoneInference_GoldCoast_AssignsZ2()
    {
        // lat 41.98 (>41.93, <42.01) → Z2
        var path = WritePlaceEntity(new { name = "Lakeview Tower", lat = 41.97, lng = -87.65 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["zone"]?.GetValue<string>(), Is.EqualTo("Z2"));
    }

    [Test]
    public async Task ZoneInference_Evanston_AssignsZ3()
    {
        // lat 42.05 (>42.01, <42.13) → Z3
        var path = WritePlaceEntity(new { name = "Evanston Hub", lat = 42.05, lng = -87.68 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["zone"]?.GetValue<string>(), Is.EqualTo("Z3"));
    }

    [Test]
    public async Task ZoneInference_NorthShore_AssignsZ4()
    {
        // lat 42.25 (>42.13, <42.40) → Z4
        var path = WritePlaceEntity(new { name = "Waukegan Yard", lat = 42.25, lng = -87.84 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["zone"]?.GetValue<string>(), Is.EqualTo("Z4"));
    }

    [Test]
    public async Task ZoneInference_Kenosha_AssignsZ7()
    {
        // lat 42.60 (>42.40, <42.81) → Z7
        var path = WritePlaceEntity(new { name = "Kenosha Docks", lat = 42.60, lng = -87.82 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["zone"]?.GetValue<string>(), Is.EqualTo("Z7"));
    }

    [Test]
    public async Task ZoneInference_Milwaukee_AssignsZ8()
    {
        // lat 43.05 (>42.81, <43.26) → Z8
        var path = WritePlaceEntity(new { name = "Milwaukee Hub", lat = 43.05, lng = -87.90 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["zone"]?.GetValue<string>(), Is.EqualTo("Z8"));
    }

    [Test]
    public async Task ZoneInference_Sheboygan_AssignsZ9()
    {
        // lat 43.50 (>43.26, <43.79) → Z9
        var path = WritePlaceEntity(new { name = "Sheboygan Breakwater", lat = 43.50, lng = -87.71 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["zone"]?.GetValue<string>(), Is.EqualTo("Z9"));
    }

    [Test]
    public async Task ZoneInference_GreenBay_AssignsZ10()
    {
        // lat > 43.79 → Z10
        var path = WritePlaceEntity(new { name = "Green Bay Terminal", lat = 44.50, lng = -88.00 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["zone"]?.GetValue<string>(), Is.EqualTo("Z10"));
    }

    [Test]
    public async Task ZoneInference_PlaceWithExistingZone_Skipped()
    {
        var path = WritePlaceEntity(new { name = "Already Zoned", lat = 41.97, lng = -87.65, zone = "Z1" });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        // Zone should NOT be overwritten
        Assert.That(ReadEntity(path)["zone"]?.GetValue<string>(), Is.EqualTo("Z1"));
    }

    [Test]
    public async Task ZoneInference_PlaceWithoutLatLng_Skipped()
    {
        var path = WritePlaceEntity(new { name = "No Coords", description = "A mysterious place." });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(ReadEntity(path)["zone"], Is.Null);
    }

    [Test]
    public async Task ZoneInference_DryRun_DoesNotWriteFile()
    {
        var path = WritePlaceEntity(new { name = "Dry Run Place", lat = 43.05, lng = -87.90 });
        svc.RunZoneInference = true;
        svc.DryRun = true;
        var before = File.ReadAllText(path);

        await svc.RunAsync();

        Assert.That(File.ReadAllText(path), Is.EqualTo(before));
    }

    // ── Wiki Link Writer: InsertWikiLinks ────────────────────────────────────

    [Test]
    public async Task WikiLinks_EntityMentionedByName_IsLinked()
    {
        // Create two people: one mentions the other's name in description
        WritePeopleEntity(new { name = "Kira Voss", description = "A fixer." });
        var path = WritePeopleEntity(new { name = "Marcus Wren", description = "Works with Kira Voss on contracts." });
        svc.RunWikiLinkWriter = true;

        await svc.RunAsync();

        var desc = ReadEntity(path)["description"]?.GetValue<string>();
        Assert.That(desc, Does.Contain("[[Kira Voss]]"));
    }

    [Test]
    public async Task WikiLinks_SelfNameNotLinked()
    {
        var path = WritePeopleEntity(new { name = "Kira Voss", description = "Kira Voss is a fixer." });
        svc.RunWikiLinkWriter = true;

        await svc.RunAsync();

        var desc = ReadEntity(path)["description"]?.GetValue<string>();
        Assert.That(desc, Does.Not.Contain("[[Kira Voss]]"));
    }

    [Test]
    public async Task WikiLinks_AlreadyLinkedName_NotDoubleLinked()
    {
        WritePeopleEntity(new { name = "Kira Voss", description = "A fixer." });
        var path = WritePeopleEntity(new { name = "Marcus", description = "Reports to [[Kira Voss]] always." });
        svc.RunWikiLinkWriter = true;

        await svc.RunAsync();

        var desc = ReadEntity(path)["description"]?.GetValue<string>() ?? "";
        // Count occurrences of [[Kira Voss]] — should be exactly 1
        Assert.That(System.Text.RegularExpressions.Regex.Matches(desc, @"\[\[Kira Voss\]\]").Count, Is.EqualTo(1));
    }

    [Test]
    public async Task WikiLinks_NameShorterThan4Chars_NotLinked()
    {
        // "Ax" is < 4 chars — should not be linked
        WritePeopleEntity(new { name = "Ax", description = "Short name entity." });
        var path = WritePeopleEntity(new { name = "Other", description = "Saw Ax near the zone." });
        svc.RunWikiLinkWriter = true;

        await svc.RunAsync();

        var desc = ReadEntity(path)["description"]?.GetValue<string>();
        Assert.That(desc, Does.Not.Contain("[[Ax]]"));
    }

    [Test]
    public async Task WikiLinks_DryRun_DoesNotWriteLinks()
    {
        WritePeopleEntity(new { name = "Kira Voss", description = "A fixer." });
        var path = WritePeopleEntity(new { name = "Marcus", description = "Kira Voss owes him one." });
        svc.RunWikiLinkWriter = true;
        svc.DryRun = true;
        var before = File.ReadAllText(path);

        await svc.RunAsync();

        Assert.That(File.ReadAllText(path), Is.EqualTo(before));
    }

    [Test]
    public async Task WikiLinks_DryRun_StillCountsLinks()
    {
        WritePeopleEntity(new { name = "Kira Voss", description = "A fixer." });
        WritePeopleEntity(new { name = "Marcus", description = "Kira Voss owes him one." });
        svc.RunWikiLinkWriter = true;
        svc.DryRun = true;

        await svc.RunAsync();

        Assert.That(svc.WikiLinksInserted, Is.GreaterThan(0));
    }

    // ── ChangeLog entries ─────────────────────────────────────────────────────

    [Test]
    public async Task ChangeLog_ZoneInferred_LogEntry()
    {
        WritePlaceEntity(new { name = "Waukegan Yard", lat = 42.25, lng = -87.84 });
        svc.RunZoneInference = true;

        await svc.RunAsync();

        Assert.That(svc.ChangeLog.Any(e => e.Contains("[Zone]")), Is.True);
    }

    [Test]
    public async Task ChangeLog_DryRun_ContainsDryRunMarker()
    {
        WritePlaceEntity(new { name = "Dry Zone", lat = 42.25, lng = -87.84 });
        svc.RunZoneInference = true;
        svc.DryRun = true;

        await svc.RunAsync();

        Assert.That(svc.ChangeLog.Any(e => e.Contains("[DRY RUN]")), Is.True);
    }
}
