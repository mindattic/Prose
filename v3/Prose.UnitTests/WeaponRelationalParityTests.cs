using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample WeaponryData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class WeaponRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private WeaponryRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_wprp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "weapon");
        repo = new WeaponryRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static WeaponryData MakeWeapon(string name, string? id = null) => new()
    {
        Id               = id ?? Guid.NewGuid().ToString("N"),
        Type             = "weapon",
        Name             = name,
        Category         = "blade",
        Manufacturer     = "Sable Forge",
        TierAvailability = "tier-3",
        Legality         = "restricted",
        Description      = "A monomolecular-edge short sword with ceramic-carbon spine. Cuts through light plating.",
        Specifications   = "Blade length: 45cm; edge retention: 6 months field-use; weight: 380g",
        TacticalUse      = "Close-quarter breach; silent takedowns; counter-aug melee.",
        CulturalContext  = "Favored by Sable cleaners who prize quiet over firepower.",
        Rating           = 88.0,
        VoteCount        = 12,
        MidjourneyPrompt = "monomolecular short sword, ceramic spine, cyberpunk forge, dark background",
        Dalle3Prompt     = "Technical cross-section of monomolecular blade, ceramic-carbon spine labeled",
        Aliases          = new List<string> { "Sable Short", "Whisper Blade" },
        BaseTechnologies = new List<string> { "Monomolecular Edge Technology", "Ceramic-Carbon Composite" },
        KnownUsers       = new List<string> { "Sable extraction teams", "Black-tier freelancers" },
        AmmunitionType   = new List<string>(),
        StoryHooks       = new List<string> { "The blade was found clean — no prints, no blood.", "Only three people in the zone carry this model." },
        Tags             = new List<string> { "sable", "blade", "tier-3", "restricted" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeWeapon("Sable Whisper Short");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,            Is.EqualTo(original.Name));
            Assert.That(got.Category,         Is.EqualTo(original.Category));
            Assert.That(got.Manufacturer,     Is.EqualTo(original.Manufacturer));
            Assert.That(got.TierAvailability, Is.EqualTo(original.TierAvailability));
            Assert.That(got.Legality,         Is.EqualTo(original.Legality));
            Assert.That(got.Description,      Is.EqualTo(original.Description));
            Assert.That(got.Specifications,   Is.EqualTo(original.Specifications));
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
        var original = MakeWeapon("Alias Weapon");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_BaseTechnologies_RoundTrip()
    {
        var original = MakeWeapon("BaseTech Weapon");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.BaseTechnologies, Is.EqualTo(original.BaseTechnologies));
    }

    [Test]
    public void Save_Then_LoadOne_KnownUsers_RoundTrip()
    {
        var original = MakeWeapon("KnownUsers Weapon");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.KnownUsers, Is.EqualTo(original.KnownUsers));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeWeapon("Hook Weapon");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeWeapon("Tag Weapon");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "weapon-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeWeapon("Weapon Alpha");
        var b = MakeWeapon("Weapon Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Weapon Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Weapon Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndCategoryAndTags()
    {
        var original = MakeWeapon("Lite Weapon");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(w => w.Name == "Lite Weapon");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Category, Is.EqualTo(original.Category));
        Assert.That(hit.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include weapon-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeWeapon("Editable Weapon");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.KnownUsers = new List<string> { "Only one user now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.KnownUsers, Is.EqualTo(new List<string> { "Only one user now." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeWeapon("Parity Weapon");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Name,            Is.EqualTo(original.Name));
            Assert.That(relational.Category,        Is.EqualTo(original.Category));
            Assert.That(relational.Manufacturer,    Is.EqualTo(original.Manufacturer));
            Assert.That(relational.TierAvailability,Is.EqualTo(original.TierAvailability));
            Assert.That(relational.Legality,        Is.EqualTo(original.Legality));
            Assert.That(relational.Description,     Is.EqualTo(original.Description));
            Assert.That(relational.Specifications,  Is.EqualTo(original.Specifications));
            Assert.That(relational.TacticalUse,     Is.EqualTo(original.TacticalUse));
            Assert.That(relational.CulturalContext, Is.EqualTo(original.CulturalContext));
            Assert.That(relational.Rating,          Is.EqualTo(original.Rating));
            Assert.That(relational.VoteCount,       Is.EqualTo(original.VoteCount));
        });

        Assert.That(relational.Aliases,          Is.EqualTo(original.Aliases));
        Assert.That(relational.BaseTechnologies, Is.EqualTo(original.BaseTechnologies));
        Assert.That(relational.KnownUsers,       Is.EqualTo(original.KnownUsers));
        Assert.That(relational.StoryHooks,       Is.EqualTo(original.StoryHooks));
        Assert.That(relational.Tags,             Is.EquivalentTo(original.Tags));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeWeapon("Blob Weapon");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "weapon",
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
            var written = await WeaponMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded weapon entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = WeaponMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,             Is.EqualTo(src.Name));
            Assert.That(got.Category,          Is.EqualTo(src.Category));
            Assert.That(got.TierAvailability,  Is.EqualTo(src.TierAvailability));
            Assert.That(got.Aliases,           Is.EqualTo(src.Aliases));
            Assert.That(got.BaseTechnologies,  Is.EqualTo(src.BaseTechnologies));
            Assert.That(got.KnownUsers,        Is.EqualTo(src.KnownUsers));
            Assert.That(got.Tags,              Is.EquivalentTo(src.Tags));
        }
    }
}
