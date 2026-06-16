using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample FlyoverEntityData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class FlyoverEntityRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private FlyoverEntityRepository repo = null!;
    private IDbContextFactory<StreetSamuraiDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_ferp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "flyover_entity");
        repo = new FlyoverEntityRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static FlyoverEntityData MakeFlyover(string name, string? id = null) => new()
    {
        Id                  = id ?? Guid.NewGuid().ToString("N"),
        Type                = "flyover_entity",
        Name                = name,
        Classification      = "emergent megafauna",
        Origin              = "Natural selection pressure from abandoned farm territory since 2089.",
        Substrate           = "biological",
        Territory           = "Mississippi valley flatlands, abandoned grain belt.",
        PhysicalDescription = "Equine baseline, 600kg. Spinal ridges calcified into blade-like keratinous plates.",
        BehavioralProfile   = "Migratory. Herd structure. Bulls territorial during calving season.",
        ThreatLevel         = "low — unless cornered",
        HumanRemnants       = "Responds to whistle calls from agricultural era. Trusts slowly.",
        GlmzMigrationRisk   = "Negligible — too large to navigate tunnel transit infrastructure.",
        Rating              = 76.0,
        VoteCount           = 8,
        MidjourneyPrompt    = "blade-spined horse on golden plains, dramatic sky",
        Dalle3Prompt        = "Scientific illustration of armoured equine with keratinous spinal ridges",
        Aliases             = new List<string> { "Ridgeback", "Prairie Blade" },
        KnownLocations      = new List<string> { "Mississippi Basin Sector 4", "Old Iowa Grasslands" },
        StoryHooks          = new List<string> { "Kyle has never seen anything that large not trying to kill him.", "A herd crossed the supply corridor — the convoy waited three hours." },
        Tags                = new List<string> { "megafauna", "flyover", "docile" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeFlyover("Ridgeback");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,                Is.EqualTo(original.Name));
            Assert.That(got.Classification,       Is.EqualTo(original.Classification));
            Assert.That(got.Origin,               Is.EqualTo(original.Origin));
            Assert.That(got.Substrate,            Is.EqualTo(original.Substrate));
            Assert.That(got.Territory,            Is.EqualTo(original.Territory));
            Assert.That(got.PhysicalDescription,  Is.EqualTo(original.PhysicalDescription));
            Assert.That(got.BehavioralProfile,    Is.EqualTo(original.BehavioralProfile));
            Assert.That(got.ThreatLevel,          Is.EqualTo(original.ThreatLevel));
            Assert.That(got.HumanRemnants,        Is.EqualTo(original.HumanRemnants));
            Assert.That(got.GlmzMigrationRisk,    Is.EqualTo(original.GlmzMigrationRisk));
            Assert.That(got.Rating,               Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,            Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt,     Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,         Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Aliases_RoundTrip()
    {
        var original = MakeFlyover("Alias Flyover");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_KnownLocations_RoundTrip()
    {
        var original = MakeFlyover("Location Flyover");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.KnownLocations, Is.EqualTo(original.KnownLocations));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeFlyover("Hook Flyover");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeFlyover("Tag Flyover");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "flyover-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeFlyover("Flyover Alpha");
        var b = MakeFlyover("Flyover Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Flyover Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Flyover Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndClassificationAndTags_Only()
    {
        var original = MakeFlyover("Lite Flyover");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(f => f.Name == "Lite Flyover");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Classification, Is.EqualTo(original.Classification));
        Assert.That(hit.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include flyover-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeFlyover("Editable Flyover");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.ThreatLevel = "high";
        loaded.StoryHooks = new List<string> { "Only one hook now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.ThreatLevel, Is.EqualTo("high"),
            "re-save must overwrite scalars");
        Assert.That(got.StoryHooks, Is.EqualTo(new List<string> { "Only one hook now." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeFlyover("Parity Flyover");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Name,                Is.EqualTo(original.Name));
            Assert.That(relational.Classification,      Is.EqualTo(original.Classification));
            Assert.That(relational.Origin,              Is.EqualTo(original.Origin));
            Assert.That(relational.Substrate,           Is.EqualTo(original.Substrate));
            Assert.That(relational.Territory,           Is.EqualTo(original.Territory));
            Assert.That(relational.PhysicalDescription, Is.EqualTo(original.PhysicalDescription));
            Assert.That(relational.BehavioralProfile,   Is.EqualTo(original.BehavioralProfile));
            Assert.That(relational.ThreatLevel,         Is.EqualTo(original.ThreatLevel));
            Assert.That(relational.HumanRemnants,       Is.EqualTo(original.HumanRemnants));
            Assert.That(relational.GlmzMigrationRisk,   Is.EqualTo(original.GlmzMigrationRisk));
            Assert.That(relational.Rating,              Is.EqualTo(original.Rating));
            Assert.That(relational.VoteCount,           Is.EqualTo(original.VoteCount));
        });

        Assert.That(relational.Aliases,        Is.EqualTo(original.Aliases));
        Assert.That(relational.KnownLocations, Is.EqualTo(original.KnownLocations));
        Assert.That(relational.StoryHooks,     Is.EqualTo(original.StoryHooks));
        Assert.That(relational.Tags,           Is.EquivalentTo(original.Tags));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeFlyover("Blob Flyover");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "flyover_entity",
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
            var written = await FlyoverEntityMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded flyover entity entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = FlyoverEntityMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,              Is.EqualTo(src.Name));
            Assert.That(got.Classification,     Is.EqualTo(src.Classification));
            Assert.That(got.ThreatLevel,        Is.EqualTo(src.ThreatLevel));
            Assert.That(got.Aliases,            Is.EqualTo(src.Aliases));
            Assert.That(got.KnownLocations,     Is.EqualTo(src.KnownLocations));
            Assert.That(got.StoryHooks,         Is.EqualTo(src.StoryHooks));
            Assert.That(got.Tags,               Is.EquivalentTo(src.Tags));
        }
    }
}
