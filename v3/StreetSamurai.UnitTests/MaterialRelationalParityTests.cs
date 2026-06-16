using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a fully-populated MaterialData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// The CanonicalFields test uses JSON round-trip equality across ALL fields so
/// no new field can be added to MaterialData without the test catching the omission.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class MaterialRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private MaterialRepository repo = null!;
    private IDbContextFactory<StreetSamuraiDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_mrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "material");
        repo = new MaterialRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder — every field populated with non-default values ─────

    private static MaterialData MakeMaterial(string name, string? id = null) => new()
    {
        Id               = id ?? Guid.NewGuid().ToString("N"),
        Type             = "material",
        Name             = name,
        BrandName        = "CarbonEdge",
        ProductName      = "Hexalon-7",
        Category         = "composite",
        TierAvailability = "Tier 3–4 industrial suppliers",
        Cost             = "Φ4,200 per sq-meter sheet",
        Description      = "A carbon nanotube weave used in high-impact armor and structural reinforcement.",
        Rating           = 82.0,
        VoteCount        = 9,
        MidjourneyPrompt = "carbon nanotube weave, iridescent black surface, macro photography",
        Dalle3Prompt     = "Cross-section of carbon nanotube composite material, 2144",
        Aliases          = new List<string> { "CNT Weave", "Black Lattice" },
        Properties       = new List<string> { "Tensile strength: 63 GPa", "Conductivity: semi-insulating", "Thermal tolerance: 800°C continuous" },
        Developers       = new List<string> { "Arcturus Materials", "Crucible Genomics Structural Division" },
        Applications     = new List<string> { "Body armor plating", "APC chassis reinforcement", "Building facade panels" },
        StoryHooks       = new List<string> { "A sample was found at the crime scene.", "The formula was stolen from Arcturus." },
        Tags             = new List<string> { "composite", "armor", "tier-3" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeMaterial("Hexalon Weave");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,             Is.EqualTo(original.Name));
            Assert.That(got.BrandName,         Is.EqualTo(original.BrandName));
            Assert.That(got.ProductName,       Is.EqualTo(original.ProductName));
            Assert.That(got.Category,          Is.EqualTo(original.Category));
            Assert.That(got.TierAvailability,  Is.EqualTo(original.TierAvailability));
            Assert.That(got.Cost,              Is.EqualTo(original.Cost));
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
        var original = MakeMaterial("Alias Material");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_Properties_RoundTrip()
    {
        var original = MakeMaterial("Property Material");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Properties, Is.EqualTo(original.Properties),
            "Properties must survive the relational round-trip via MaterialProperties bridge");
    }

    [Test]
    public void Save_Then_LoadOne_Developers_RoundTrip()
    {
        var original = MakeMaterial("Developer Material");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Developers, Is.EqualTo(original.Developers),
            "Developers must survive the relational round-trip via MaterialDevelopers bridge");
    }

    [Test]
    public void Save_Then_LoadOne_Applications_RoundTrip()
    {
        var original = MakeMaterial("Application Material");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Applications, Is.EqualTo(original.Applications),
            "Applications must survive the relational round-trip via MaterialApplications bridge");
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeMaterial("Hook Material");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeMaterial("Tag Material");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "material-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeMaterial("Material Alpha");
        var b = MakeMaterial("Material Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(m => m.Name == "Material Alpha"), Is.True);
        Assert.That(all.Any(m => m.Name == "Material Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndTags_Only()
    {
        var original = MakeMaterial("Lite Material");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(m => m.Name == "Lite Material");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include material-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeMaterial("Editable Material");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.StoryHooks = new List<string> { "Only one hook now." };
        loaded.Properties = new List<string> { "Updated property." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.StoryHooks, Is.EqualTo(new List<string> { "Only one hook now." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Properties, Is.EqualTo(new List<string> { "Updated property." }),
            "re-save must wipe and re-insert properties bridge rows");
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        // Use JSON serialization equality across ALL fields — if any field is
        // dropped by the mapper this test will fail even if no explicit assertion
        // names it.
        var original = MakeMaterial("Parity Material");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        var opts = new JsonSerializerOptions { WriteIndented = false, PropertyNameCaseInsensitive = true };
        // Normalize Type field (relational mapper always writes "material")
        original.Type = "material";
        var originalJson   = JsonSerializer.Serialize(original, opts);
        var relationalJson = JsonSerializer.Serialize(relational, opts);

        Assert.That(relationalJson, Is.EqualTo(originalJson),
            "Full JSON round-trip equality across ALL MaterialData fields. " +
            "Any field dropped by the mapper will appear as a diff here.");
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeMaterial("Blob Material");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "material",
                Name       = src.Name,
                Slug       = WorldGraphService.Slugify(src.Name),
                Status     = "canon",
                IsActive   = true,
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
            var written = await MaterialMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded material");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = MaterialMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,         Is.EqualTo(src.Name));
            Assert.That(got.Aliases,       Is.EqualTo(src.Aliases));
            Assert.That(got.Properties,    Is.EqualTo(src.Properties));
            Assert.That(got.Developers,    Is.EqualTo(src.Developers));
            Assert.That(got.Applications,  Is.EqualTo(src.Applications));
            Assert.That(got.StoryHooks,    Is.EqualTo(src.StoryHooks));
            Assert.That(got.Tags,          Is.EquivalentTo(src.Tags));
            Assert.That(got.BrandName,     Is.EqualTo(src.BrandName));
            Assert.That(got.ProductName,   Is.EqualTo(src.ProductName));
            Assert.That(got.TierAvailability, Is.EqualTo(src.TierAvailability));
            Assert.That(got.Cost,          Is.EqualTo(src.Cost));
        }
    }
}
