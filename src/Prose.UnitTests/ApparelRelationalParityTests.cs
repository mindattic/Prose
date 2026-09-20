using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample ApparelData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class ApparelRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private ApparelRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_aprp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "apparel");
        repo = new ApparelRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static ApparelData MakeApparel(string name, string? id = null) => new()
    {
        Id               = id ?? Guid.NewGuid().ToString("N"),
        Name             = name,
        Category         = "jacket",
        Manufacturer     = "Sable Couture",
        TierAssociation  = "tier-2",
        Description      = "A reinforced urban jacket with concealed plating. Looks expensive. Is.",
        Functionality    = "Subdermal mesh backing; integrated climate control.",
        WhatItSays       = "You have money and you've been hit before.",
        PriceRange       = "2400–3800 Φ",
        AugCompatible    = true,
        GeneCompatible   = false,
        Rating           = 82.0,
        VoteCount        = 5,
        MidjourneyPrompt = "reinforced urban jacket, matte black, cyberpunk, street fashion",
        Dalle3Prompt     = "Technical diagram of reinforced jacket with hidden plating panels",
        Materials        = new List<string> { "ballistic weave", "carbon-fiber panel", "memory foam lining" },
        WornBy           = new List<string> { "Kyle", "Lotus syndicate enforcers" },
        StoryHooks       = new List<string> { "The lining conceals a data chip.", "Someone wore this to a Sable board meeting and lived." },
        Tags             = new List<string> { "sable", "tier-2", "reinforced" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeApparel("Sable Urban Jacket");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,            Is.EqualTo(original.Name));
            Assert.That(got.Category,         Is.EqualTo(original.Category));
            Assert.That(got.Manufacturer,     Is.EqualTo(original.Manufacturer));
            Assert.That(got.TierAssociation,  Is.EqualTo(original.TierAssociation));
            Assert.That(got.Description,      Is.EqualTo(original.Description));
            Assert.That(got.Functionality,    Is.EqualTo(original.Functionality));
            Assert.That(got.WhatItSays,       Is.EqualTo(original.WhatItSays));
            Assert.That(got.PriceRange,       Is.EqualTo(original.PriceRange));
            Assert.That(got.AugCompatible,    Is.EqualTo(original.AugCompatible));
            Assert.That(got.GeneCompatible,   Is.EqualTo(original.GeneCompatible));
            Assert.That(got.Rating,           Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,        Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt, Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,     Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Materials_RoundTrip()
    {
        var original = MakeApparel("Material Jacket");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Materials, Is.EqualTo(original.Materials));
    }

    [Test]
    public void Save_Then_LoadOne_WornBy_RoundTrip()
    {
        var original = MakeApparel("WornBy Jacket");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        // WornBy is stored as aliases in ApparelWornBy.Alias; check the string list
        Assert.That(got.WornBy, Is.EqualTo(original.WornBy));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeApparel("Hook Jacket");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeApparel("Tag Jacket");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "apparel-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeApparel("Jacket Alpha");
        var b = MakeApparel("Jacket Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Jacket Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Jacket Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndCategoryAndTags()
    {
        var original = MakeApparel("Lite Jacket");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(a => a.Name == "Lite Jacket");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Category, Is.EqualTo(original.Category));
        Assert.That(hit.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include apparel-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeApparel("Editable Jacket");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.Materials = new List<string> { "Updated weave only." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.Materials, Is.EqualTo(new List<string> { "Updated weave only." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.StoryHooks.Count, Is.EqualTo(original.StoryHooks.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeApparel("Parity Jacket");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Name,           Is.EqualTo(original.Name));
            Assert.That(relational.Category,       Is.EqualTo(original.Category));
            Assert.That(relational.Manufacturer,   Is.EqualTo(original.Manufacturer));
            Assert.That(relational.TierAssociation,Is.EqualTo(original.TierAssociation));
            Assert.That(relational.Description,    Is.EqualTo(original.Description));
            Assert.That(relational.Functionality,  Is.EqualTo(original.Functionality));
            Assert.That(relational.WhatItSays,     Is.EqualTo(original.WhatItSays));
            Assert.That(relational.PriceRange,     Is.EqualTo(original.PriceRange));
            Assert.That(relational.AugCompatible,  Is.EqualTo(original.AugCompatible));
            Assert.That(relational.GeneCompatible, Is.EqualTo(original.GeneCompatible));
            Assert.That(relational.Rating,         Is.EqualTo(original.Rating));
            Assert.That(relational.VoteCount,      Is.EqualTo(original.VoteCount));
        });

        Assert.That(relational.Materials,  Is.EqualTo(original.Materials));
        Assert.That(relational.WornBy,     Is.EqualTo(original.WornBy));
        Assert.That(relational.StoryHooks, Is.EqualTo(original.StoryHooks));
        Assert.That(relational.Tags,       Is.EquivalentTo(original.Tags));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeApparel("Blob Jacket");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "apparel",
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
            var written = await ApparelMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded apparel entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = ApparelMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,           Is.EqualTo(src.Name));
            Assert.That(got.Category,        Is.EqualTo(src.Category));
            Assert.That(got.TierAssociation, Is.EqualTo(src.TierAssociation));
            Assert.That(got.Functionality,   Is.EqualTo(src.Functionality));
            Assert.That(got.AugCompatible,   Is.EqualTo(src.AugCompatible));
            Assert.That(got.Materials,       Is.EqualTo(src.Materials));
            Assert.That(got.WornBy,          Is.EqualTo(src.WornBy));
            Assert.That(got.Tags,            Is.EquivalentTo(src.Tags));
        }
    }
}
