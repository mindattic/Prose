using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample CorponationData the relational
/// round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the same logical
/// content as the original domain object. All against in-memory SQLite (TestDbFactory).
/// </summary>
[TestFixture]
public class CorponationRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private CorponationRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_corp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "corponation");
        repo = new CorponationRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    private static CorponationData MakeCorp(string name, string? id = null) => new()
    {
        Id                  = id ?? Guid.NewGuid().ToString("N"),
        Name                = name,
        Number              = 42,
        FullLegalName       = $"{name} Sovereign Corporation",
        StockDesignation    = "CORP:NXS",
        Sector              = "Defense",
        Valuation           = "Φ 4.2T",
        Revenue             = "Φ 800B",
        Employees           = "1.2M",
        SovereignTerritory  = "New Chicago Corridor",
        FoundingStory       = "Founded in 2089 from the ashes of three collapsed megafirms.",
        SecurityForce       = "Iron Veil",
        KeyDetail           = "Controls 70% of anti-resonance shielding contracts.",
        RelationshipToBig20 = "Member, board seat on Mutual Defense Accord.",
        FullText            = "Full lore dump placeholder.",
        Rating              = 82.0,
        VoteCount           = 11,
        MidjourneyPrompt    = "futuristic corporate tower, neon, dark sky",
        Dalle3Prompt        = "Minimalist corporate logo on steel, cyberpunk style",
        CommonNames         = new List<string> { "NexCorp", "The Iron 42" },
        Tags                = new List<string> { "big-20", "defense", "new-chicago" },
    };

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeCorp("NexCorp Industries");
        repo.Save(original);

        var got = repo.GetById(original.Id);
        Assert.That(got, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,               Is.EqualTo(original.Name));
            Assert.That(got.Number,              Is.EqualTo(original.Number));
            Assert.That(got.FullLegalName,       Is.EqualTo(original.FullLegalName));
            Assert.That(got.StockDesignation,    Is.EqualTo(original.StockDesignation));
            Assert.That(got.Sector,              Is.EqualTo(original.Sector));
            Assert.That(got.Valuation,           Is.EqualTo(original.Valuation));
            Assert.That(got.Revenue,             Is.EqualTo(original.Revenue));
            Assert.That(got.Employees,           Is.EqualTo(original.Employees));
            Assert.That(got.SovereignTerritory,  Is.EqualTo(original.SovereignTerritory));
            Assert.That(got.FoundingStory,       Is.EqualTo(original.FoundingStory));
            Assert.That(got.SecurityForce,       Is.EqualTo(original.SecurityForce));
            Assert.That(got.KeyDetail,           Is.EqualTo(original.KeyDetail));
            Assert.That(got.RelationshipToBig20, Is.EqualTo(original.RelationshipToBig20));
            Assert.That(got.FullText,            Is.EqualTo(original.FullText));
            Assert.That(got.Rating,              Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,           Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt,    Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,        Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_CommonNames_RoundTrip()
    {
        var original = MakeCorp("CommonNames Corp");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.CommonNames, Is.EqualTo(original.CommonNames));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeCorp("Tag Corp");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags));
    }

    [Test]
    public void GetAll_ReturnsAllSaved()
    {
        var a = MakeCorp("Alpha Corp");
        var b = MakeCorp("Beta Corp");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Alpha Corp"), Is.True);
        Assert.That(all.Any(x => x.Name == "Beta Corp"),  Is.True);
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeCorp("Editable Corp");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.FullLegalName = "Updated Legal Name Corp";
        loaded.CommonNames = new List<string> { "UCN" };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.FullLegalName, Is.EqualTo("Updated Legal Name Corp"));
        Assert.That(got.CommonNames, Is.EqualTo(new List<string> { "UCN" }));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeCorp("Blob Corp");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "corponation",
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
            var written = await CorponationMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1));
        }

        using (var db = factory.CreateDbContext())
        {
            var got = CorponationMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,        Is.EqualTo(src.Name));
            Assert.That(got.FullLegalName,Is.EqualTo(src.FullLegalName));
            Assert.That(got.CommonNames,  Is.EqualTo(src.CommonNames));
            Assert.That(got.Tags,         Is.EquivalentTo(src.Tags));
        }
    }
}
