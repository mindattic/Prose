using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample SubsidiaryData the relational
/// round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the same logical
/// content as the original domain object. All against in-memory SQLite (TestDbFactory).
/// </summary>
[TestFixture]
public class SubsidiaryRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private SubsidiaryRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_sub_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "subsidiary");
        repo = new SubsidiaryRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private static SubsidiaryData MakeSub(string name, string? id = null) => new()
    {
        Id                = id ?? Guid.NewGuid().ToString("N"),
        Type              = "subsidiary",
        Name              = name,
        ParentCorponation = "Arcturus Defense",
        LineOfBusiness    = "Tactical pharmaceuticals and stimulant supply.",
        Description       = "Arcturus subsidiary handling military pharmaceutical contracts.",
        PublicFacing      = false,
        Rating            = 77.0,
        VoteCount         = 4,
        MidjourneyPrompt  = "corporate subsidiary logo, dark ops style, cyberpunk",
        Dalle3Prompt      = "Minimalist subsidiary branding, corporate noir",
        KnownProducts     = new List<string> { "Stim-7 Combat Pack", "Trauma Block Gel", "Cortisol Override" },
        Tags              = new List<string> { "arcturus", "pharma", "military" },
    };

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeSub("Arcturus Pharma Division");
        repo.Save(original);

        var got = repo.GetById(original.Id);
        Assert.That(got, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,              Is.EqualTo(original.Name));
            Assert.That(got.ParentCorponation,  Is.EqualTo(original.ParentCorponation));
            Assert.That(got.LineOfBusiness,     Is.EqualTo(original.LineOfBusiness));
            Assert.That(got.Description,        Is.EqualTo(original.Description));
            Assert.That(got.PublicFacing,       Is.EqualTo(original.PublicFacing));
            Assert.That(got.Rating,             Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,          Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt,   Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,       Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_KnownProducts_RoundTrip()
    {
        var original = MakeSub("Product Sub");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.KnownProducts, Is.EqualTo(original.KnownProducts));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeSub("Tag Sub");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags));
    }

    [Test]
    public void GetAll_ReturnsAllSaved()
    {
        var a = MakeSub("Sub Alpha");
        var b = MakeSub("Sub Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Sub Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Sub Beta"),  Is.True);
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeSub("Editable Sub");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.KnownProducts = new List<string> { "Only One Product" };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description,  Is.EqualTo("Updated description."));
        Assert.That(got.KnownProducts, Is.EqualTo(new List<string> { "Only One Product" }));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeSub("Blob Sub");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "subsidiary",
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
            var written = await SubsidiaryMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1));
        }

        using (var db = factory.CreateDbContext())
        {
            var got = SubsidiaryMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,             Is.EqualTo(src.Name));
            Assert.That(got.ParentCorponation, Is.EqualTo(src.ParentCorponation));
            Assert.That(got.KnownProducts,     Is.EqualTo(src.KnownProducts));
            Assert.That(got.Tags,              Is.EquivalentTo(src.Tags));
        }
    }
}
