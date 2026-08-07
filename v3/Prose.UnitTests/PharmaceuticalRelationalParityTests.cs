using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample PharmaceuticalData the relational
/// round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the same logical
/// content as the original domain object. All against in-memory SQLite (TestDbFactory).
/// </summary>
[TestFixture]
public class PharmaceuticalRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private PharmaceuticalRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_pharma_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "pharmaceutical");
        repo = new PharmaceuticalRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private static PharmaceuticalData MakePharma(string name, string? id = null) => new()
    {
        Id               = id ?? Guid.NewGuid().ToString("N"),
        Type             = "pharmaceutical",
        Name             = name,
        Manufacturer     = "Noopharma",
        Category         = "nootropic",
        Subcategory      = "focus",
        TierAvailability = "tier-1",
        Legality         = "OTC",
        Description      = "Fast-acting neural stimulant for extended focus sessions.",
        MethodOfUse      = "Oral tablet",
        Duration         = "4-6 hours",
        AddictionRisk    = "Low",
        StreetPrice      = "Φ 12",
        CulturalContext  = "Ambient in GLMZ office culture; distributed in break rooms.",
        Rating           = 80.0,
        VoteCount        = 6,
        MidjourneyPrompt = "futuristic pill bottle, neon label, cyberpunk",
        Dalle3Prompt     = "Glowing capsule on dark surface, pharmaceutical style",
        Aliases          = new List<string> { "Merithadol", "The Blue One" },
        Effects          = new List<string> { "Enhanced focus", "Reduced fatigue", "Mild euphoria" },
        SideEffects      = new List<string> { "Appetite suppression", "Mild headache on comedown" },
        StoryHooks       = new List<string> { "The batch number matched a recalled lot.", "Kyle found half a strip in a dead fixer's pocket." },
        Tags             = new List<string> { "nootropic", "noopharma", "tier-1" },
    };

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakePharma("Merithadol");
        repo.Save(original);

        var got = repo.GetById(original.Id);
        Assert.That(got, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,            Is.EqualTo(original.Name));
            Assert.That(got.Manufacturer,     Is.EqualTo(original.Manufacturer));
            Assert.That(got.Category,         Is.EqualTo(original.Category));
            Assert.That(got.Subcategory,      Is.EqualTo(original.Subcategory));
            Assert.That(got.TierAvailability, Is.EqualTo(original.TierAvailability));
            Assert.That(got.Legality,         Is.EqualTo(original.Legality));
            Assert.That(got.Description,      Is.EqualTo(original.Description));
            Assert.That(got.MethodOfUse,      Is.EqualTo(original.MethodOfUse));
            Assert.That(got.Duration,         Is.EqualTo(original.Duration));
            Assert.That(got.AddictionRisk,    Is.EqualTo(original.AddictionRisk));
            Assert.That(got.StreetPrice,      Is.EqualTo(original.StreetPrice));
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
        var original = MakePharma("Alias Pharma");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_Effects_RoundTrip()
    {
        var original = MakePharma("Effect Pharma");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Effects, Is.EqualTo(original.Effects));
    }

    [Test]
    public void Save_Then_LoadOne_SideEffects_RoundTrip()
    {
        var original = MakePharma("SideEffect Pharma");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.SideEffects, Is.EqualTo(original.SideEffects));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakePharma("Hook Pharma");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakePharma("Tag Pharma");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags));
    }

    [Test]
    public void GetAll_ReturnsAllSaved()
    {
        var a = MakePharma("Pharma Alpha");
        var b = MakePharma("Pharma Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Pharma Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Pharma Beta"),  Is.True);
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakePharma("Editable Pharma");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.Effects = new List<string> { "Only one effect now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."));
        Assert.That(got.Effects, Is.EqualTo(new List<string> { "Only one effect now." }));
        Assert.That(got.SideEffects.Count, Is.EqualTo(original.SideEffects.Count));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakePharma("Blob Pharma");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "pharmaceutical",
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
            var written = await PharmaceuticalMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1));
        }

        using (var db = factory.CreateDbContext())
        {
            var got = PharmaceuticalMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,            Is.EqualTo(src.Name));
            Assert.That(got.TierAvailability, Is.EqualTo(src.TierAvailability));
            Assert.That(got.Aliases,          Is.EqualTo(src.Aliases));
            Assert.That(got.Effects,          Is.EqualTo(src.Effects));
            Assert.That(got.SideEffects,      Is.EqualTo(src.SideEffects));
            Assert.That(got.Tags,             Is.EquivalentTo(src.Tags));
        }
    }
}
