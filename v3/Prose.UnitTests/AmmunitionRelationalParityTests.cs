using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample AmmunitionData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class AmmunitionRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private AmmunitionRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_amrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "ammunition");
        repo = new AmmunitionRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static AmmunitionData MakeAmmo(string name, string? id = null) => new()
    {
        Id              = id ?? Guid.NewGuid().ToString("N"),
        Type            = "ammunition",
        Name            = name,
        Manufacturer    = "Sable Ordinance",
        Caliber         = "12.7mm",
        Category        = "anti-material",
        TierAvailability = "tier-3",
        Legality        = "restricted",
        Description     = "Armor-piercing incendiary round designed for urban breach operations.",
        Specifications  = "Muzzle velocity 920 m/s; penetrates 40mm RHA equivalent.",
        CulturalContext = "Street name: Torch. Preferred by Sable extraction teams.",
        Rating          = 85.0,
        VoteCount       = 7,
        MidjourneyPrompt = "high-caliber incendiary bullet, glowing tip, dark background",
        Dalle3Prompt     = "Cross-section cutaway of armor-piercing incendiary round, technical illustration",
        Aliases          = new List<string> { "Torch", "API-12" },
        CompatibleWeapons = new List<string> { "Sable M-17 Anti-Material Rifle", "Arcturus HeavyFrame" },
        Variants         = new List<string> { "Tracer", "Subsonic" },
        StoryHooks       = new List<string> { "A box was found at the scene — Sable doesn't sell retail.", "The casings were stamped with a recalled batch number." },
        Tags             = new List<string> { "anti-material", "restricted", "sable" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeAmmo("Torch API-12");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,           Is.EqualTo(original.Name));
            Assert.That(got.Manufacturer,    Is.EqualTo(original.Manufacturer));
            Assert.That(got.Caliber,         Is.EqualTo(original.Caliber));
            Assert.That(got.Category,        Is.EqualTo(original.Category));
            Assert.That(got.TierAvailability,Is.EqualTo(original.TierAvailability));
            Assert.That(got.Legality,        Is.EqualTo(original.Legality));
            Assert.That(got.Description,     Is.EqualTo(original.Description));
            Assert.That(got.Specifications,  Is.EqualTo(original.Specifications));
            Assert.That(got.CulturalContext, Is.EqualTo(original.CulturalContext));
            Assert.That(got.Rating,          Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,       Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt,Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,    Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Aliases_RoundTrip()
    {
        var original = MakeAmmo("Alias Ammo");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_CompatibleWeapons_RoundTrip()
    {
        var original = MakeAmmo("Compat Ammo");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.CompatibleWeapons, Is.EqualTo(original.CompatibleWeapons));
    }

    [Test]
    public void Save_Then_LoadOne_Variants_RoundTrip()
    {
        var original = MakeAmmo("Variant Ammo");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Variants, Is.EqualTo(original.Variants));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeAmmo("Hook Ammo");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeAmmo("Tag Ammo");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "ammunition-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeAmmo("Ammo Alpha");
        var b = MakeAmmo("Ammo Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Ammo Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Ammo Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndCaliberAndTags_Only()
    {
        var original = MakeAmmo("Lite Ammo");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(a => a.Name == "Lite Ammo");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Caliber, Is.EqualTo(original.Caliber));
        Assert.That(hit.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include ammunition-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeAmmo("Editable Ammo");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.Variants = new List<string> { "Only one variant now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.Variants, Is.EqualTo(new List<string> { "Only one variant now." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeAmmo("Parity Ammo");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Name,           Is.EqualTo(original.Name));
            Assert.That(relational.Manufacturer,   Is.EqualTo(original.Manufacturer));
            Assert.That(relational.Caliber,        Is.EqualTo(original.Caliber));
            Assert.That(relational.Category,       Is.EqualTo(original.Category));
            Assert.That(relational.TierAvailability,Is.EqualTo(original.TierAvailability));
            Assert.That(relational.Legality,       Is.EqualTo(original.Legality));
            Assert.That(relational.Description,    Is.EqualTo(original.Description));
            Assert.That(relational.Specifications, Is.EqualTo(original.Specifications));
            Assert.That(relational.CulturalContext,Is.EqualTo(original.CulturalContext));
            Assert.That(relational.Rating,         Is.EqualTo(original.Rating));
            Assert.That(relational.VoteCount,      Is.EqualTo(original.VoteCount));
        });

        Assert.That(relational.Aliases,           Is.EqualTo(original.Aliases));
        Assert.That(relational.CompatibleWeapons, Is.EqualTo(original.CompatibleWeapons));
        Assert.That(relational.Variants,          Is.EqualTo(original.Variants));
        Assert.That(relational.StoryHooks,        Is.EqualTo(original.StoryHooks));
        Assert.That(relational.Tags,              Is.EquivalentTo(original.Tags));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeAmmo("Blob Ammo");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "ammunition",
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
            var written = await AmmunitionMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded ammunition entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = AmmunitionMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,             Is.EqualTo(src.Name));
            Assert.That(got.Caliber,           Is.EqualTo(src.Caliber));
            Assert.That(got.TierAvailability,  Is.EqualTo(src.TierAvailability));
            Assert.That(got.Aliases,           Is.EqualTo(src.Aliases));
            Assert.That(got.CompatibleWeapons, Is.EqualTo(src.CompatibleWeapons));
            Assert.That(got.Variants,          Is.EqualTo(src.Variants));
            Assert.That(got.Tags,              Is.EquivalentTo(src.Tags));
        }
    }
}
