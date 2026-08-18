using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a fully-populated TransportationData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// The CanonicalFields test uses JSON round-trip equality across ALL fields so
/// no new field can be added to TransportationData without the test catching the omission.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class TransportationRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private TransportationRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_trp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "transportation");
        repo = new TransportationRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder — every field populated with non-default values ─────

    private static TransportationData MakeTransport(string name, string? id = null) => new()
    {
        Id               = id ?? Guid.NewGuid().ToString("N"),
        Type             = "transportation",
        Name             = name,
        Manufacturer     = "Axiom Transit",
        Category         = "rail",
        Propulsion       = "linear induction motor (magnetic levitation)",
        Speed            = "120 km/h sustained; 180 km/h express",
        Capacity         = "320 passengers per car, 8-car consist",
        Range            = "Inner loop 42 km circuit, continuous service",
        TierAvailability = "Tier 1 public transit",
        Cost             = "Φ2 flat fare; Φ12/day pass",
        Autonomy         = "Level 4 — human operator on board, AI guidance",
        Armament         = "none",
        CommonUsage      = "Daily commute, cargo last-mile, emergency evacuation routes",
        Description      = "Elevated magnetic rail system serving the GLMZ inner loop. 24-hour service.",
        Rating           = 71.0,
        VoteCount        = 14,
        MidjourneyPrompt = "elevated rail station, neon-lit platform, rain, cyberpunk city, 2144",
        Dalle3Prompt     = "Interior of a futuristic metro car with holographic route displays",
        Aliases          = new List<string> { "The L-Train", "Axiom Metro" },
        StoryHooks       = new List<string> { "The last car has no cameras.", "Someone left a package under the seat." },
        Tags             = new List<string> { "rail", "axiom", "tier-1", "public" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeTransport("GLMZ Inner Loop Rail");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,             Is.EqualTo(original.Name));
            Assert.That(got.Manufacturer,      Is.EqualTo(original.Manufacturer));
            Assert.That(got.Category,          Is.EqualTo(original.Category));
            Assert.That(got.Propulsion,        Is.EqualTo(original.Propulsion));
            Assert.That(got.Speed,             Is.EqualTo(original.Speed));
            Assert.That(got.Capacity,          Is.EqualTo(original.Capacity));
            Assert.That(got.Range,             Is.EqualTo(original.Range));
            Assert.That(got.TierAvailability,  Is.EqualTo(original.TierAvailability));
            Assert.That(got.Cost,              Is.EqualTo(original.Cost));
            Assert.That(got.Autonomy,          Is.EqualTo(original.Autonomy));
            Assert.That(got.Armament,          Is.EqualTo(original.Armament));
            Assert.That(got.CommonUsage,       Is.EqualTo(original.CommonUsage));
            Assert.That(got.Description,       Is.EqualTo(original.Description));
            Assert.That(got.Rating,            Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,         Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt,  Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,      Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Aliases_RoundTrip()
    {
        var original = MakeTransport("Alias Transport");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeTransport("Hook Transport");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeTransport("Tag Transport");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "transportation-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeTransport("Transport Alpha");
        var b = MakeTransport("Transport Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(t => t.Name == "Transport Alpha"), Is.True);
        Assert.That(all.Any(t => t.Name == "Transport Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndManufacturerAndTags_Only()
    {
        var original = MakeTransport("Lite Transport");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(t => t.Name == "Lite Transport");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Manufacturer, Is.EqualTo(original.Manufacturer));
        Assert.That(hit.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include transportation-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeTransport("Editable Transport");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.Propulsion  = "Updated propulsion.";
        loaded.StoryHooks  = new List<string> { "Only one hook now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.Propulsion, Is.EqualTo("Updated propulsion."),
            "re-save must overwrite new scalar columns");
        Assert.That(got.StoryHooks, Is.EqualTo(new List<string> { "Only one hook now." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        // Use JSON serialization equality across ALL fields — if any field is
        // dropped by the mapper this test will fail even if no explicit assertion
        // names it.
        var original = MakeTransport("Parity Transport");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        var opts = new JsonSerializerOptions { WriteIndented = false, PropertyNameCaseInsensitive = true };
        // Normalize Type field (relational mapper always writes "transportation")
        original.Type = "transportation";
        var originalJson   = JsonSerializer.Serialize(original, opts);
        var relationalJson = JsonSerializer.Serialize(relational, opts);

        Assert.That(relationalJson, Is.EqualTo(originalJson),
            "Full JSON round-trip equality across ALL TransportationData fields. " +
            "Any field dropped by the mapper will appear as a diff here.");
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeTransport("Blob Transport");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "transportation",
                Name       = src.Name,
                Slug       = WorldGraphService.Slugify(src.Name),
                Status     = "canon",
                
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            });
            var json = JsonSerializer.Serialize(src, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            });
            db.Records.Add(new Record { EntityId = id, Json = json });
            db.SaveChanges();
        }

        using (var db = factory.CreateDbContext())
        {
            var written = await TransportationMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded transportation entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = TransportationMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,             Is.EqualTo(src.Name));
            Assert.That(got.Aliases,           Is.EqualTo(src.Aliases));
            Assert.That(got.StoryHooks,        Is.EqualTo(src.StoryHooks));
            Assert.That(got.Tags,              Is.EquivalentTo(src.Tags));
            Assert.That(got.Propulsion,        Is.EqualTo(src.Propulsion));
            Assert.That(got.Speed,             Is.EqualTo(src.Speed));
            Assert.That(got.Capacity,          Is.EqualTo(src.Capacity));
            Assert.That(got.Range,             Is.EqualTo(src.Range));
            Assert.That(got.TierAvailability,  Is.EqualTo(src.TierAvailability));
            Assert.That(got.Cost,              Is.EqualTo(src.Cost));
            Assert.That(got.Autonomy,          Is.EqualTo(src.Autonomy));
            Assert.That(got.Armament,          Is.EqualTo(src.Armament));
            Assert.That(got.CommonUsage,       Is.EqualTo(src.CommonUsage));
        }
    }
}
