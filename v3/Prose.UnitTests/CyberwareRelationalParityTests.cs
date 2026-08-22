using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample CyberwareData the relational
/// round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the same logical
/// content as the original domain object. All against in-memory SQLite (TestDbFactory).
/// </summary>
[TestFixture]
public class CyberwareRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private CyberwareRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_cw_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "cyberware");
        repo = new CyberwareRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private static CyberwareData MakeCyberware(string name, string? id = null) => new()
    {
        Id                       = id ?? Guid.NewGuid().ToString("N"),
        Type                     = "cyberware",
        Name                     = name,
        Manufacturer             = "Arcturus Biotech",
        BrandName                = "ArcBio",
        ProductName              = "SubDermal Mesh 2140",
        Category                 = "subdermal",
        BodyLocation             = "Torso",
        TierAvailability         = "tier-3",
        Legality                 = "licensed-medical",
        Description              = "Subdermal trauma mesh with fragmentation-arrest properties.",
        InstallationRequirements = "Full surgical suite, 6-hour procedure.",
        RejectionRisk            = "Low with Arcturus gene-matched substrate.",
        Maintenance              = "Annual calibration, Φ 800.",
        Specifications           = "Stops 2 rounds 9mm-equiv; compromised after.",
        CulturalContext          = "Standard for Sable extraction ops; visible seam is a status marker.",
        StreetPrice              = "Φ 28,000",
        LicensedPrice            = "Φ 45,000",
        Rating                   = 88.0,
        VoteCount                = 12,
        MidjourneyPrompt         = "subdermal mesh implant, visible seam, cyberpunk",
        Dalle3Prompt             = "Medical cross-section of subdermal mesh, technical illustration",
        Aliases                  = new List<string> { "Mesh", "The Weave" },
        SideEffects              = new List<string> { "Scar tissue accumulation at seam", "Mild MRI interference" },
        KnownUsers               = new List<string> { "Kyle", "Ledger" },
        StoryHooks               = new List<string> { "The mesh stopped two rounds — it won't stop three.", "Pixel can feel the seam when she patches Kyle." },
        Tags                     = new List<string> { "subdermal", "arcturus", "tier-3" },
    };

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeCyberware("SubDermal Mesh 2140");
        repo.Save(original);

        var got = repo.GetById(original.Id);
        Assert.That(got, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,                     Is.EqualTo(original.Name));
            Assert.That(got.Manufacturer,              Is.EqualTo(original.Manufacturer));
            Assert.That(got.BrandName,                 Is.EqualTo(original.BrandName));
            Assert.That(got.ProductName,               Is.EqualTo(original.ProductName));
            Assert.That(got.Category,                  Is.EqualTo(original.Category));
            Assert.That(got.BodyLocation,              Is.EqualTo(original.BodyLocation));
            Assert.That(got.TierAvailability,          Is.EqualTo(original.TierAvailability));
            Assert.That(got.Legality,                  Is.EqualTo(original.Legality));
            Assert.That(got.Description,               Is.EqualTo(original.Description));
            Assert.That(got.InstallationRequirements,  Is.EqualTo(original.InstallationRequirements));
            Assert.That(got.RejectionRisk,             Is.EqualTo(original.RejectionRisk));
            Assert.That(got.Maintenance,               Is.EqualTo(original.Maintenance));
            Assert.That(got.Specifications,            Is.EqualTo(original.Specifications));
            Assert.That(got.CulturalContext,           Is.EqualTo(original.CulturalContext));
            Assert.That(got.StreetPrice,               Is.EqualTo(original.StreetPrice));
            Assert.That(got.LicensedPrice,             Is.EqualTo(original.LicensedPrice));
            Assert.That(got.Rating,                    Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,                 Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt,          Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,              Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Aliases_RoundTrip()
    {
        var original = MakeCyberware("Alias CW");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_SideEffects_RoundTrip()
    {
        var original = MakeCyberware("SideEffect CW");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.SideEffects, Is.EqualTo(original.SideEffects));
    }

    [Test]
    public void Save_Then_LoadOne_KnownUsers_RoundTrip()
    {
        var original = MakeCyberware("User CW");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.KnownUsers, Is.EqualTo(original.KnownUsers));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeCyberware("Hook CW");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeCyberware("Tag CW");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags));
    }

    [Test]
    public void GetAll_ReturnsAllSaved()
    {
        var a = MakeCyberware("CW Alpha");
        var b = MakeCyberware("CW Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "CW Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "CW Beta"),  Is.True);
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeCyberware("Editable CW");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.SideEffects = new List<string> { "Only one side effect now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."));
        Assert.That(got.SideEffects, Is.EqualTo(new List<string> { "Only one side effect now." }));
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeCyberware("Blob CW");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "cyberware",
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
            var written = await CyberwareMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1));
        }

        using (var db = factory.CreateDbContext())
        {
            var got = CyberwareMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,            Is.EqualTo(src.Name));
            Assert.That(got.TierAvailability, Is.EqualTo(src.TierAvailability));
            Assert.That(got.Aliases,          Is.EqualTo(src.Aliases));
            Assert.That(got.SideEffects,      Is.EqualTo(src.SideEffects));
            Assert.That(got.Tags,             Is.EquivalentTo(src.Tags));
        }
    }
}
