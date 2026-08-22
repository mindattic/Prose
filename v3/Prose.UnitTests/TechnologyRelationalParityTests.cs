using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample TechnologyData the relational
/// round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the same logical
/// content as the original domain object. All against in-memory SQLite (TestDbFactory).
/// </summary>
[TestFixture]
public class TechnologyRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private TechnologyRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_tech_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "technology");
        repo = new TechnologyRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private static TechnologyData MakeTech(string name, string? id = null) => new()
    {
        Id               = id ?? Guid.NewGuid().ToString("N"),
        Type             = "technology",
        Name             = name,
        BrandName        = "Arcturus Labs",
        ProductName      = "NeuroLink v4",
        Subcategory      = "neural-interface",
        TierAvailability = "tier-3",
        Description      = "High-bandwidth neural bridge for cortex-to-network integration.",
        SocialImpact     = "Normalized by 2130; prejudice now economic, not cultural.",
        Rating           = 86.0,
        VoteCount        = 14,
        MidjourneyPrompt = "neural chip, glowing circuitry, cyberpunk",
        Dalle3Prompt     = "Cross-section of neural interface chip, technical cutaway",
        Aliases          = new List<string> { "NLink", "Neural Bridge v4" },
        Developers       = new List<string> { "Arcturus Labs", "Verdant Systems" },
        BaseTechnologies = new List<string> { "NeuroLink v3", "Synaptic Relay Array" },
        Enables          = new List<string> { "Full-Immersion VR", "Neuretics Tier 4" },
        StoryHooks       = new List<string> { "A v4 unit was found in a Lotus operative.", "Arcturus denies the batch." },
        Tags             = new List<string> { "neural", "arcturus", "tier-3" },
    };

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeTech("NeuroLink v4");
        repo.Save(original);

        var got = repo.GetById(original.Id);
        Assert.That(got, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,            Is.EqualTo(original.Name));
            Assert.That(got.BrandName,        Is.EqualTo(original.BrandName));
            Assert.That(got.ProductName,      Is.EqualTo(original.ProductName));
            Assert.That(got.Subcategory,      Is.EqualTo(original.Subcategory));
            Assert.That(got.TierAvailability, Is.EqualTo(original.TierAvailability));
            Assert.That(got.Description,      Is.EqualTo(original.Description));
            Assert.That(got.SocialImpact,     Is.EqualTo(original.SocialImpact));
            Assert.That(got.Rating,           Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,        Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt, Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,     Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Aliases_RoundTrip()
    {
        var original = MakeTech("Alias Tech");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_Developers_RoundTrip()
    {
        var original = MakeTech("Dev Tech");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Developers, Is.EqualTo(original.Developers));
    }

    [Test]
    public void Save_Then_LoadOne_BaseTechnologies_RoundTrip()
    {
        var original = MakeTech("Base Tech");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.BaseTechnologies, Is.EqualTo(original.BaseTechnologies));
    }

    [Test]
    public void Save_Then_LoadOne_Enables_RoundTrip()
    {
        var original = MakeTech("Enable Tech");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Enables, Is.EqualTo(original.Enables));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeTech("Hook Tech");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeTech("Tag Tech");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags));
    }

    [Test]
    public void GetAll_ReturnsAllSaved()
    {
        var a = MakeTech("Tech Alpha");
        var b = MakeTech("Tech Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Tech Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Tech Beta"),  Is.True);
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeTech("Editable Tech");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.Enables = new List<string> { "Only one enables now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."));
        Assert.That(got.Enables, Is.EqualTo(new List<string> { "Only one enables now." }));
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeTech("Blob Tech");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "technology",
                Name       = src.Name,
                Slug       = UniverseGraphService.Slugify(src.Name),
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
            var written = await TechnologyMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1));
        }

        using (var db = factory.CreateDbContext())
        {
            var got = TechnologyMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,            Is.EqualTo(src.Name));
            Assert.That(got.TierAvailability, Is.EqualTo(src.TierAvailability));
            Assert.That(got.Aliases,          Is.EqualTo(src.Aliases));
            Assert.That(got.Developers,       Is.EqualTo(src.Developers));
            Assert.That(got.Tags,             Is.EquivalentTo(src.Tags));
        }
    }
}
