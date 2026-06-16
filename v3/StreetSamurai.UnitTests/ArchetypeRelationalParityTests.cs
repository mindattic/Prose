using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample ArchetypeData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class ArchetypeRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private ArchetypeRepository repo = null!;
    private IDbContextFactory<StreetSamuraiDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_arp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "archetype");
        repo = new ArchetypeRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static ArchetypeData MakeArchetype(string name, string? id = null) => new()
    {
        Id                  = id ?? Guid.NewGuid().ToString("N"),
        Type                = "archetype",
        Name                = name,
        Category            = "predator",
        Description         = "An individual who takes what they need, consequences secondary.",
        BehavioralSignature = "Scans exits before sitting. Counts people before speaking.",
        UnderStress         = "Becomes colder, more deliberate. Targets the weakest link.",
        AtRest              = "Appears relaxed, but is always calculating angles.",
        WillAlways          = new List<string> { "Take the path of least resistance.", "Protect their supply line." },
        WillNever           = new List<string> { "Expose themselves unnecessarily.", "Fight fair." },
        Unless              = new List<string> { "Unless cornered with no exit.", "Unless their own kind is at risk." },
        SimilarTo           = new List<ArchetypeSimilarity>
        {
            new() { Archetype = "Opportunist", Threshold = 0.7, Context = "When resources are scarce." },
            new() { Archetype = "Survivor",    Threshold = 0.6, Context = "Under sustained pressure." },
        },
        OppositeOf          = new List<string> { "Protector", "Altruist" },
        Tags                = new List<string> { "predator", "street", "high-tier" },
    };

    // ── Core parity tests ──────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeArchetype("Apex Predator");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,               Is.EqualTo(original.Name));
            Assert.That(got.Category,            Is.EqualTo(original.Category));
            Assert.That(got.Description,         Is.EqualTo(original.Description));
            Assert.That(got.BehavioralSignature, Is.EqualTo(original.BehavioralSignature));
            Assert.That(got.UnderStress,         Is.EqualTo(original.UnderStress));
            Assert.That(got.AtRest,              Is.EqualTo(original.AtRest));
        });
    }

    [Test]
    public void Save_Then_LoadOne_WillAlways_WillNever_Unless_RoundTrip()
    {
        var original = MakeArchetype("Rule Archetype");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.WillAlways, Is.EqualTo(original.WillAlways));
        Assert.That(got.WillNever,  Is.EqualTo(original.WillNever));
        Assert.That(got.Unless,     Is.EqualTo(original.Unless));
    }

    [Test]
    public void Save_Then_LoadOne_SimilarTo_RoundTrip()
    {
        var original = MakeArchetype("Similar Archetype");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;

        Assert.That(got.SimilarTo.Count, Is.EqualTo(original.SimilarTo.Count));
        for (int i = 0; i < original.SimilarTo.Count; i++)
        {
            Assert.That(got.SimilarTo[i].Archetype, Is.EqualTo(original.SimilarTo[i].Archetype));
            Assert.That(got.SimilarTo[i].Threshold, Is.EqualTo(original.SimilarTo[i].Threshold));
            Assert.That(got.SimilarTo[i].Context,   Is.EqualTo(original.SimilarTo[i].Context));
        }
    }

    [Test]
    public void Save_Then_LoadOne_OppositeOf_RoundTrip()
    {
        var original = MakeArchetype("Opposite Archetype");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.OppositeOf, Is.EqualTo(original.OppositeOf));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeArchetype("Tag Archetype");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "archetype-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeArchetype("Archetype Alpha");
        var b = MakeArchetype("Archetype Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Archetype Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Archetype Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndCategory_Only()
    {
        var original = MakeArchetype("Lite Archetype");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(x => x.Name == "Lite Archetype");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Category, Is.EqualTo(original.Category));
        Assert.That(hit.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include archetype-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeArchetype("Editable Archetype");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.WillAlways = new List<string> { "Only one rule now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.WillAlways, Is.EqualTo(new List<string> { "Only one rule now." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.WillNever.Count, Is.EqualTo(original.WillNever.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeArchetype("Parity Archetype");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Name,               Is.EqualTo(original.Name));
            Assert.That(relational.Category,           Is.EqualTo(original.Category));
            Assert.That(relational.Description,        Is.EqualTo(original.Description));
            Assert.That(relational.BehavioralSignature,Is.EqualTo(original.BehavioralSignature));
            Assert.That(relational.UnderStress,        Is.EqualTo(original.UnderStress));
            Assert.That(relational.AtRest,             Is.EqualTo(original.AtRest));
        });

        Assert.That(relational.WillAlways, Is.EqualTo(original.WillAlways));
        Assert.That(relational.WillNever,  Is.EqualTo(original.WillNever));
        Assert.That(relational.Unless,     Is.EqualTo(original.Unless));
        Assert.That(relational.OppositeOf, Is.EqualTo(original.OppositeOf));
        Assert.That(relational.Tags,       Is.EquivalentTo(original.Tags));

        Assert.That(relational.SimilarTo.Count, Is.EqualTo(original.SimilarTo.Count));
        for (int i = 0; i < original.SimilarTo.Count; i++)
        {
            Assert.That(relational.SimilarTo[i].Archetype, Is.EqualTo(original.SimilarTo[i].Archetype));
            Assert.That(relational.SimilarTo[i].Threshold, Is.EqualTo(original.SimilarTo[i].Threshold));
            Assert.That(relational.SimilarTo[i].Context,   Is.EqualTo(original.SimilarTo[i].Context));
        }
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeArchetype("Blob Archetype");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "archetype",
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
            var written = await ArchetypeMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded archetype");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = ArchetypeMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,      Is.EqualTo(src.Name));
            Assert.That(got.WillAlways, Is.EqualTo(src.WillAlways));
            Assert.That(got.SimilarTo.Count, Is.EqualTo(src.SimilarTo.Count));
            Assert.That(got.Tags,       Is.EquivalentTo(src.Tags));
        }
    }
}
