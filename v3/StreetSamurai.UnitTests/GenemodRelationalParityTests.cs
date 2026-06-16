using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a fully-populated GenemodData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// The CanonicalFields test uses JSON round-trip equality across ALL fields so
/// no new field can be added to GenemodData without the test catching the omission.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class GenemodRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private GenemodRepository repo = null!;
    private IDbContextFactory<StreetSamuraiDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_grp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "genemod");
        repo = new GenemodRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder — every field populated with non-default values ─────

    private static GenemodData MakeGenemod(string name, string? id = null) => new()
    {
        Id               = id ?? Guid.NewGuid().ToString("N"),
        Type             = "genemods",
        Name             = name,
        BrandName        = "ChromaGenix",
        ProductName      = "NoctisMod v2",
        Manufacturer     = "Crucible Genomics",
        Category         = "sensory",
        TargetSystem     = "ocular",
        SourceOrganism   = "Felis catus (domestic cat)",
        Legality         = "licensed",
        Procedure        = "Outpatient retinal cell replacement, 2-hour procedure.",
        ExpressionTime   = "72 hours",
        Reversibility    = "irreversible without surgical extraction",
        SocialPerception = "Common in security work; occasionally draws suspicion off-hours.",
        TierAvailability = "Tier 2–3 licensed clinics",
        Description      = "Retinal modification granting low-light vision down to 0.01 lux.",
        Rating           = 78.5,
        VoteCount        = 12,
        MidjourneyPrompt = "glowing cat-eye pupils, cyberpunk back-alley clinic",
        Dalle3Prompt     = "Close-up of a modified human eye with vertical slit pupils, 2144",
        Aliases          = new List<string> { "Night Eyes", "Cat Mod" },
        SideEffects      = new List<string> { "Heightened sensitivity to bright light for 30 days.", "Occasional chromatic aberration in high-contrast environments." },
        StoryHooks       = new List<string> { "The modification works too well — you see things others can't.", "Corneal rejection begins on day 30." },
        Tags             = new List<string> { "sensory", "tier-2", "legal" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeGenemod("Noctis Retinal Mod");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,             Is.EqualTo(original.Name));
            Assert.That(got.BrandName,         Is.EqualTo(original.BrandName));
            Assert.That(got.ProductName,       Is.EqualTo(original.ProductName));
            Assert.That(got.Manufacturer,      Is.EqualTo(original.Manufacturer));
            Assert.That(got.Category,          Is.EqualTo(original.Category));
            Assert.That(got.TargetSystem,      Is.EqualTo(original.TargetSystem));
            Assert.That(got.SourceOrganism,    Is.EqualTo(original.SourceOrganism));
            Assert.That(got.Legality,          Is.EqualTo(original.Legality));
            Assert.That(got.Procedure,         Is.EqualTo(original.Procedure));
            Assert.That(got.ExpressionTime,    Is.EqualTo(original.ExpressionTime));
            Assert.That(got.Reversibility,     Is.EqualTo(original.Reversibility));
            Assert.That(got.SocialPerception,  Is.EqualTo(original.SocialPerception));
            Assert.That(got.TierAvailability,  Is.EqualTo(original.TierAvailability));
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
        var original = MakeGenemod("Alias Genemod");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_SideEffects_RoundTrip()
    {
        var original = MakeGenemod("SideEffect Genemod");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.SideEffects, Is.EqualTo(original.SideEffects),
            "SideEffects must survive the relational round-trip via GenemodSideEffects bridge");
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeGenemod("Hook Genemod");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeGenemod("Tag Genemod");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "genemod-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeGenemod("Genemod Alpha");
        var b = MakeGenemod("Genemod Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(g => g.Name == "Genemod Alpha"), Is.True);
        Assert.That(all.Any(g => g.Name == "Genemod Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndTags_Only()
    {
        var original = MakeGenemod("Lite Genemod");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(g => g.Name == "Lite Genemod");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include genemod-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeGenemod("Editable Genemod");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.StoryHooks = new List<string> { "Only one hook now." };
        loaded.SideEffects = new List<string> { "Updated side effect." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.StoryHooks, Is.EqualTo(new List<string> { "Only one hook now." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.SideEffects, Is.EqualTo(new List<string> { "Updated side effect." }),
            "re-save must wipe and re-insert side effects bridge rows");
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        // Use JSON serialization equality across ALL fields — if any field is
        // dropped by the mapper this test will fail even if no explicit assertion
        // names it.
        var original = MakeGenemod("Parity Genemod");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        var opts = new JsonSerializerOptions { WriteIndented = false, PropertyNameCaseInsensitive = true };
        // Normalize Type field (relational mapper always writes "genemods")
        original.Type = "genemods";
        var originalJson   = JsonSerializer.Serialize(original, opts);
        var relationalJson = JsonSerializer.Serialize(relational, opts);

        Assert.That(relationalJson, Is.EqualTo(originalJson),
            "Full JSON round-trip equality across ALL GenemodData fields. " +
            "Any field dropped by the mapper will appear as a diff here.");
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeGenemod("Blob Genemod");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "genemod",
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
            var written = await GenemodMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded genemod");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = GenemodMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,        Is.EqualTo(src.Name));
            Assert.That(got.Aliases,      Is.EqualTo(src.Aliases));
            Assert.That(got.SideEffects,  Is.EqualTo(src.SideEffects));
            Assert.That(got.StoryHooks,   Is.EqualTo(src.StoryHooks));
            Assert.That(got.Tags,         Is.EquivalentTo(src.Tags));
            Assert.That(got.TargetSystem, Is.EqualTo(src.TargetSystem));
            Assert.That(got.Legality,     Is.EqualTo(src.Legality));
            Assert.That(got.BrandName,    Is.EqualTo(src.BrandName));
            Assert.That(got.ProductName,  Is.EqualTo(src.ProductName));
        }
    }
}
