using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample EquipmentData the relational
/// round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the same logical
/// content as the original domain object. All against in-memory SQLite (TestDbFactory).
/// </summary>
[TestFixture]
public class EquipmentRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private EquipmentRepository repo = null!;
    private IDbContextFactory<StreetSamuraiDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_eqrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "equipment");
        repo = new EquipmentRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private static EquipmentData MakeEquip(string name, string? id = null) => new()
    {
        Id               = id ?? Guid.NewGuid().ToString("N"),
        Type             = "equipment",
        Name             = name,
        Manufacturer     = "Arcturus Tactical",
        BrandName        = "ArcTac",
        ProductName      = "Reaper Kit Mk2",
        Category         = "field-kit",
        TierAvailability = "tier-2",
        Legality         = "licensed",
        Description      = "Compact multi-role field kit for urban operators.",
        TacticalUse      = "Breach and clear, rapid deployment.",
        CulturalContext  = "Standard issue for Sable contractor teams.",
        Rating           = 84.0,
        VoteCount        = 9,
        MidjourneyPrompt = "tactical field kit, dark ops, cyberpunk",
        Dalle3Prompt     = "Military gear spread on dark surface, technical diagram",
        Aliases          = new List<string> { "Reaper Kit", "RK2" },
        BaseTechnologies = new List<string> { "Nanite Sealing Gel", "Arcturus Adaptive Frame" },
        KnownUsers       = new List<string> { "Kyle", "Maeve Okafor" },
        Specifications   = new Dictionary<string, string>
        {
            ["Weight"]    = "4.2 kg",
            ["Dims"]      = "32x28x14 cm",
            ["Modules"]   = "3 swappable",
        },
        StoryHooks       = new List<string> { "The serial number was scraped off.", "Sable still owns the warranty." },
        Tags             = new List<string> { "tactical", "sable", "tier-2" },
    };

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeEquip("Reaper Kit Mk2");
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
            Assert.That(got.TierAvailability, Is.EqualTo(original.TierAvailability));
            Assert.That(got.Legality,         Is.EqualTo(original.Legality));
            Assert.That(got.Description,      Is.EqualTo(original.Description));
            Assert.That(got.TacticalUse,      Is.EqualTo(original.TacticalUse));
            Assert.That(got.CulturalContext,  Is.EqualTo(original.CulturalContext));
            Assert.That(got.Rating,           Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,        Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt, Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,     Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Aliases_RoundTrip()
    {
        var original = MakeEquip("Alias Equip");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_BaseTechnologies_RoundTrip()
    {
        var original = MakeEquip("Tech Equip");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.BaseTechnologies, Is.EqualTo(original.BaseTechnologies));
    }

    [Test]
    public void Save_Then_LoadOne_KnownUsers_RoundTrip()
    {
        var original = MakeEquip("User Equip");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.KnownUsers, Is.EqualTo(original.KnownUsers));
    }

    [Test]
    public void Save_Then_LoadOne_Specifications_RoundTrip()
    {
        var original = MakeEquip("Spec Equip");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Specifications.Count, Is.EqualTo(original.Specifications.Count));
        foreach (var kvp in original.Specifications)
            Assert.That(got.Specifications[kvp.Key], Is.EqualTo(kvp.Value));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeEquip("Hook Equip");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeEquip("Tag Equip");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags));
    }

    [Test]
    public void GetAll_ReturnsAllSaved()
    {
        var a = MakeEquip("Equip Alpha");
        var b = MakeEquip("Equip Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Equip Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Equip Beta"),  Is.True);
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeEquip("Editable Equip");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.StoryHooks = new List<string> { "Only one hook now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."));
        Assert.That(got.StoryHooks, Is.EqualTo(new List<string> { "Only one hook now." }));
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeEquip("Blob Equip");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "equipment",
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
            var written = await EquipmentMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1));
        }

        using (var db = factory.CreateDbContext())
        {
            var got = EquipmentMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,            Is.EqualTo(src.Name));
            Assert.That(got.TierAvailability, Is.EqualTo(src.TierAvailability));
            Assert.That(got.Aliases,          Is.EqualTo(src.Aliases));
            Assert.That(got.StoryHooks,       Is.EqualTo(src.StoryHooks));
            Assert.That(got.Tags,             Is.EquivalentTo(src.Tags));
        }
    }
}
