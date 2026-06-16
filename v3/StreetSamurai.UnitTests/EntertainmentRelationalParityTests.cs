using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample EntertainmentData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class EntertainmentRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private EntertainmentRepository repo = null!;
    private IDbContextFactory<StreetSamuraiDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_enrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "entertainment");
        repo = new EntertainmentRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static EntertainmentData MakeEntertainment(string name, string? id = null) => new()
    {
        Id             = id ?? Guid.NewGuid().ToString("N"),
        Type           = "entertainment",
        Name           = name,
        Category       = "music",
        Subcategory    = "band",
        Creator        = "Axiom Collective",
        Distributor    = "Neural Groove Records",
        TierAvailability = "tier-2",
        Legality       = "legal",
        Genre          = "industrial drone",
        Medium         = "neural feed",
        Audience       = "corp workers, GLMZ youth",
        CulturalImpact = "Normalized 17Hz exposure as ambient music; sold Lure proximity.",
        Description    = "An industrial drone band whose live sets register at exactly 17Hz.",
        Rating         = 84.5,
        VoteCount      = 9,
        MidjourneyPrompt = "industrial drone band, neon-lit stage, cyberpunk venue, 2225",
        Dalle3Prompt     = "Album cover art for industrial drone band, dark geometric design",
        Aliases        = new List<string> { "Axiom Drone", "The 17s" },
        KnownFans      = new List<string> { "Kyle", "Ezra the biomod" },
        StoryHooks     = new List<string> { "Their last show ended with three audience members in RZ proximity.", "The promoter vanished after booking their final set." },
        Tags           = new List<string> { "music", "17hz", "lure-adjacent" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeEntertainment("Axiom Collective Live");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,            Is.EqualTo(original.Name));
            Assert.That(got.Category,         Is.EqualTo(original.Category));
            Assert.That(got.Subcategory,      Is.EqualTo(original.Subcategory));
            Assert.That(got.Creator,          Is.EqualTo(original.Creator));
            Assert.That(got.Distributor,      Is.EqualTo(original.Distributor));
            Assert.That(got.TierAvailability, Is.EqualTo(original.TierAvailability));
            Assert.That(got.Legality,         Is.EqualTo(original.Legality));
            Assert.That(got.Genre,            Is.EqualTo(original.Genre));
            Assert.That(got.Medium,           Is.EqualTo(original.Medium));
            Assert.That(got.Audience,         Is.EqualTo(original.Audience));
            Assert.That(got.CulturalImpact,   Is.EqualTo(original.CulturalImpact));
            Assert.That(got.Description,      Is.EqualTo(original.Description));
            Assert.That(got.Rating,           Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,        Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt, Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,     Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Aliases_RoundTrip()
    {
        var original = MakeEntertainment("Alias Entertainment");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_KnownFans_RoundTrip()
    {
        var original = MakeEntertainment("KnownFans Entertainment");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.KnownFans, Is.EqualTo(original.KnownFans));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeEntertainment("Hook Entertainment");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeEntertainment("Tag Entertainment");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "entertainment-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeEntertainment("Entertainment Alpha");
        var b = MakeEntertainment("Entertainment Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Entertainment Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Entertainment Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndCategoryAndTags()
    {
        var original = MakeEntertainment("Lite Entertainment");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(e => e.Name == "Lite Entertainment");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Category, Is.EqualTo(original.Category));
        Assert.That(hit.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include entertainment-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeEntertainment("Editable Entertainment");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.KnownFans = new List<string> { "Only one fan now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.KnownFans, Is.EqualTo(new List<string> { "Only one fan now." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeEntertainment("Parity Entertainment");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Name,            Is.EqualTo(original.Name));
            Assert.That(relational.Category,        Is.EqualTo(original.Category));
            Assert.That(relational.Subcategory,     Is.EqualTo(original.Subcategory));
            Assert.That(relational.Creator,         Is.EqualTo(original.Creator));
            Assert.That(relational.Distributor,     Is.EqualTo(original.Distributor));
            Assert.That(relational.TierAvailability,Is.EqualTo(original.TierAvailability));
            Assert.That(relational.Genre,           Is.EqualTo(original.Genre));
            Assert.That(relational.Medium,          Is.EqualTo(original.Medium));
            Assert.That(relational.CulturalImpact,  Is.EqualTo(original.CulturalImpact));
            Assert.That(relational.Description,     Is.EqualTo(original.Description));
            Assert.That(relational.Rating,          Is.EqualTo(original.Rating));
            Assert.That(relational.VoteCount,       Is.EqualTo(original.VoteCount));
        });

        Assert.That(relational.Aliases,    Is.EqualTo(original.Aliases));
        Assert.That(relational.KnownFans,  Is.EqualTo(original.KnownFans));
        Assert.That(relational.StoryHooks, Is.EqualTo(original.StoryHooks));
        Assert.That(relational.Tags,       Is.EquivalentTo(original.Tags));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeEntertainment("Blob Entertainment");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "entertainment",
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
            var written = await EntertainmentMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded entertainment entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = EntertainmentMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,            Is.EqualTo(src.Name));
            Assert.That(got.Category,         Is.EqualTo(src.Category));
            Assert.That(got.TierAvailability, Is.EqualTo(src.TierAvailability));
            Assert.That(got.Aliases,          Is.EqualTo(src.Aliases));
            Assert.That(got.KnownFans,        Is.EqualTo(src.KnownFans));
            Assert.That(got.Tags,             Is.EquivalentTo(src.Tags));
        }
    }
}
