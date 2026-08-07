using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample ConsumerGoodData the relational
/// round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the same logical
/// content as the original domain object. All against in-memory SQLite (TestDbFactory).
/// Schema note: 8 columns (BrandName/ProductName/Subcategory/FlavorProfile/Price/
/// PopularityRank/Slogan/CulturalContext) were added in
/// relationalize_consumer_goods_20260616.sql.
/// </summary>
[TestFixture]
public class ConsumerGoodRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private ConsumerGoodRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_cg_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "consumer_good");
        repo = new ConsumerGoodRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private static ConsumerGoodData MakeGood(string name, string? id = null) => new()
    {
        Id               = id ?? Guid.NewGuid().ToString("N"),
        Type             = "consumer_good",
        Name             = name,
        Manufacturer     = "Pixel Foods",
        BrandName        = "PixelBite",
        ProductName      = "NoodleBox Premium",
        Category         = "food",
        Subcategory      = "instant-meal",
        TierAvailability = "tier-1",
        Description      = "Premium instant noodle box with optional flavour packs.",
        FlavorProfile    = "Umami-forward with synthetic meat notes.",
        Price            = "Φ 4",
        PopularityRank   = 3,
        Slogan           = "Just add water. Skip the rest.",
        CulturalContext  = "Ubiquitous in transit corridors; comfort food for the displaced.",
        Rating           = 78.0,
        VoteCount        = 5,
        MidjourneyPrompt = "futuristic noodle box, neon packaging, cyberpunk convenience store",
        Dalle3Prompt     = "Minimalist instant noodle packaging, retro-futuristic style",
        StoryHooks       = new List<string> { "Kyle eats this when the rent is tight.", "Found a micro-tracker in the flavor packet." },
        Tags             = new List<string> { "food", "tier-1", "comfort" },
    };

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeGood("NoodleBox Premium");
        repo.Save(original);

        var got = repo.GetById(original.Id);
        Assert.That(got, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,            Is.EqualTo(original.Name));
            Assert.That(got.Manufacturer,     Is.EqualTo(original.Manufacturer));
            Assert.That(got.BrandName,        Is.EqualTo(original.BrandName));
            Assert.That(got.ProductName,      Is.EqualTo(original.ProductName));
            Assert.That(got.Category,         Is.EqualTo(original.Category));
            Assert.That(got.Subcategory,      Is.EqualTo(original.Subcategory));
            Assert.That(got.TierAvailability, Is.EqualTo(original.TierAvailability));
            Assert.That(got.Description,      Is.EqualTo(original.Description));
            Assert.That(got.FlavorProfile,    Is.EqualTo(original.FlavorProfile));
            Assert.That(got.Price,            Is.EqualTo(original.Price));
            Assert.That(got.PopularityRank,   Is.EqualTo(original.PopularityRank));
            Assert.That(got.Slogan,           Is.EqualTo(original.Slogan));
            Assert.That(got.CulturalContext,  Is.EqualTo(original.CulturalContext));
            Assert.That(got.Rating,           Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,        Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt, Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,     Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeGood("Hook Good");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeGood("Tag Good");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags));
    }

    [Test]
    public void GetAll_ReturnsAllSaved()
    {
        var a = MakeGood("Good Alpha");
        var b = MakeGood("Good Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Good Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Good Beta"),  Is.True);
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeGood("Editable Good");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.Slogan = "New slogan.";
        loaded.StoryHooks = new List<string> { "Only one hook now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."));
        Assert.That(got.Slogan,      Is.EqualTo("New slogan."));
        Assert.That(got.StoryHooks,  Is.EqualTo(new List<string> { "Only one hook now." }));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeGood("Blob Good");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "consumer_good",
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
            var written = await ConsumerGoodMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1));
        }

        using (var db = factory.CreateDbContext())
        {
            var got = ConsumerGoodMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,            Is.EqualTo(src.Name));
            Assert.That(got.TierAvailability, Is.EqualTo(src.TierAvailability));
            Assert.That(got.BrandName,        Is.EqualTo(src.BrandName));
            Assert.That(got.FlavorProfile,    Is.EqualTo(src.FlavorProfile));
            Assert.That(got.Slogan,           Is.EqualTo(src.Slogan));
            Assert.That(got.StoryHooks,       Is.EqualTo(src.StoryHooks));
            Assert.That(got.Tags,             Is.EquivalentTo(src.Tags));
        }
    }
}
