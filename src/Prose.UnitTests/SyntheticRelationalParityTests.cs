using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample SyntheticLifeData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class SyntheticRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private SyntheticLifeRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_syrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "synthetic");
        repo = new SyntheticLifeRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static SyntheticLifeData MakeSynthetic(string name, string? id = null) => new()
    {
        Id                 = id ?? Guid.NewGuid().ToString("N"),
        Type               = "synthetic",
        Name               = name,
        KindOfBeing        = "ELF",
        Manufacturer       = "Unknown",
        Tier               = "undocumented",
        Classification     = "E.L.F. (Electronic Life Form)",
        Disposition        = "wisp",
        Habitat            = "resonance_zone",
        Origin             = "firmware_evolution",
        LifeStatus         = "active",
        Description        = "Manifests as anomalous light patterns in RZ-adjacent infrastructure.",
        ObservedBehavior   = "Modulates city lighting in patterns that may be communication.",
        EncounterFrequency = "rare",
        ConfirmedSightings = 3,
        Location           = "35th & Halsted",
        DtiRating          = 0.0,
        Paratechnological  = true,
        KnownAge           = "unknown",
        CrackPattern       = null,
        CurrentRole        = "observer",
        KnownLocation      = "RZ grid node under the rail",
        DiplomaticSpecialty= null,
        OperatingHistory   = null,
        BehavioralNotes    = "Responds to 72Hz audio signals.",
        DamageHistory      = null,
        FaceDecoration     = null,
        Rating             = 78.0,
        VoteCount          = 3,
        MidjourneyPrompt   = "ELF entity, neon light patterns, resonance zone, cyberpunk 2225",
        Dalle3Prompt       = "Diagram of ELF signal propagation through city grid infrastructure",
        Aliases            = new List<string> { "The Light", "Grid Ghost" },
        KnownAssociations  = new List<string> { "The Counter", "Ezra the biomod" },
        StoryHooks         = new List<string> { "The pattern repeated exactly 17 times — matching the Lure's frequency.", "It went dark the night of the rail incident." },
        Tags               = new List<string> { "synthetic", "elf", "rz-adjacent", "rare" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeSynthetic("The Grid Ghost");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,               Is.EqualTo(original.Name));
            Assert.That(got.KindOfBeing,         Is.EqualTo(original.KindOfBeing));
            Assert.That(got.Classification,      Is.EqualTo(original.Classification));
            Assert.That(got.Disposition,         Is.EqualTo(original.Disposition));
            Assert.That(got.Habitat,             Is.EqualTo(original.Habitat));
            Assert.That(got.Origin,              Is.EqualTo(original.Origin));
            Assert.That(got.LifeStatus,          Is.EqualTo(original.LifeStatus));
            Assert.That(got.Description,         Is.EqualTo(original.Description));
            Assert.That(got.ObservedBehavior,    Is.EqualTo(original.ObservedBehavior));
            Assert.That(got.EncounterFrequency,  Is.EqualTo(original.EncounterFrequency));
            Assert.That(got.ConfirmedSightings,  Is.EqualTo(original.ConfirmedSightings));
            Assert.That(got.Location,            Is.EqualTo(original.Location));
            Assert.That(got.Paratechnological,   Is.EqualTo(original.Paratechnological));
            Assert.That(got.CurrentRole,         Is.EqualTo(original.CurrentRole));
            Assert.That(got.KnownLocation,       Is.EqualTo(original.KnownLocation));
            Assert.That(got.BehavioralNotes,     Is.EqualTo(original.BehavioralNotes));
            Assert.That(got.Rating,              Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,           Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt,    Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,        Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Aliases_RoundTrip()
    {
        var original = MakeSynthetic("Alias Synthetic");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_KnownAssociations_RoundTrip()
    {
        var original = MakeSynthetic("KnownAssoc Synthetic");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.KnownAssociations, Is.EqualTo(original.KnownAssociations));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeSynthetic("Hook Synthetic");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeSynthetic("Tag Synthetic");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "synthetic-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeSynthetic("Synthetic Alpha");
        var b = MakeSynthetic("Synthetic Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Synthetic Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Synthetic Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndClassificationAndTags()
    {
        var original = MakeSynthetic("Lite Synthetic");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(s => s.Name == "Lite Synthetic");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Classification, Is.EqualTo(original.Classification));
        Assert.That(hit.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include synthetic-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeSynthetic("Editable Synthetic");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.StoryHooks = new List<string> { "Only one hook now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.StoryHooks, Is.EqualTo(new List<string> { "Only one hook now." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeSynthetic("Parity Synthetic");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Name,              Is.EqualTo(original.Name));
            Assert.That(relational.KindOfBeing,       Is.EqualTo(original.KindOfBeing));
            Assert.That(relational.Classification,    Is.EqualTo(original.Classification));
            Assert.That(relational.Disposition,       Is.EqualTo(original.Disposition));
            Assert.That(relational.Habitat,           Is.EqualTo(original.Habitat));
            Assert.That(relational.Origin,            Is.EqualTo(original.Origin));
            Assert.That(relational.LifeStatus,        Is.EqualTo(original.LifeStatus));
            Assert.That(relational.Description,       Is.EqualTo(original.Description));
            Assert.That(relational.ObservedBehavior,  Is.EqualTo(original.ObservedBehavior));
            Assert.That(relational.ConfirmedSightings,Is.EqualTo(original.ConfirmedSightings));
            Assert.That(relational.Paratechnological, Is.EqualTo(original.Paratechnological));
            Assert.That(relational.Rating,            Is.EqualTo(original.Rating));
            Assert.That(relational.VoteCount,         Is.EqualTo(original.VoteCount));
        });

        Assert.That(relational.Aliases,           Is.EqualTo(original.Aliases));
        Assert.That(relational.KnownAssociations,  Is.EqualTo(original.KnownAssociations));
        Assert.That(relational.StoryHooks,        Is.EqualTo(original.StoryHooks));
        Assert.That(relational.Tags,              Is.EquivalentTo(original.Tags));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeSynthetic("Blob Synthetic");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "synthetic",
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
            var written = await SyntheticMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded synthetic entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = SyntheticMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,             Is.EqualTo(src.Name));
            Assert.That(got.Classification,    Is.EqualTo(src.Classification));
            Assert.That(got.Disposition,       Is.EqualTo(src.Disposition));
            Assert.That(got.Paratechnological, Is.EqualTo(src.Paratechnological));
            Assert.That(got.Aliases,           Is.EqualTo(src.Aliases));
            Assert.That(got.KnownAssociations, Is.EqualTo(src.KnownAssociations));
            Assert.That(got.Tags,              Is.EquivalentTo(src.Tags));
        }
    }
}
