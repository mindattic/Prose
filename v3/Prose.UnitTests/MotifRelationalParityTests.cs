using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample MotifData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class MotifRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private MotifRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_mrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "motif");
        repo = new MotifRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static MotifData MakeMotif(string name, string? id = null) => new()
    {
        Id          = id ?? Guid.NewGuid().ToString("N"),
        Name        = name,
        Description = "Recurring image of rain on glass — the city watching itself.",
        Appearances = new List<MotifAppearanceData>
        {
            new() { Scene = 1, Meaning = "Isolation before action." },
            new() { Scene = 7, Meaning = "Return — the cycle closes." },
        },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeMotif("Rain on Glass");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,        Is.EqualTo(original.Name));
            Assert.That(got.Description,  Is.EqualTo(original.Description));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Appearances_RoundTrip()
    {
        var original = MakeMotif("Appearance Motif");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;

        Assert.That(got.Appearances.Count, Is.EqualTo(original.Appearances.Count));
        for (int i = 0; i < original.Appearances.Count; i++)
        {
            Assert.That(got.Appearances[i].Scene,   Is.EqualTo(original.Appearances[i].Scene));
            Assert.That(got.Appearances[i].Meaning, Is.EqualTo(original.Appearances[i].Meaning));
        }
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeMotif("Motif Alpha");
        var b = MakeMotif("Motif Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Motif Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Motif Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndDescription()
    {
        var original = MakeMotif("Lite Motif");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(m => m.Name == "Lite Motif");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Description, Is.EqualTo(original.Description));
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeMotif("Editable Motif");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.Appearances = new List<MotifAppearanceData> { new() { Scene = 99, Meaning = "Only one now." } };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.Appearances.Count, Is.EqualTo(1),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Appearances[0].Scene, Is.EqualTo(99));
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeMotif("Parity Motif");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Name,        Is.EqualTo(original.Name));
            Assert.That(relational.Description, Is.EqualTo(original.Description));
        });

        Assert.That(relational.Appearances.Count, Is.EqualTo(original.Appearances.Count));
        for (int i = 0; i < original.Appearances.Count; i++)
        {
            Assert.That(relational.Appearances[i].Scene,   Is.EqualTo(original.Appearances[i].Scene));
            Assert.That(relational.Appearances[i].Meaning, Is.EqualTo(original.Appearances[i].Meaning));
        }
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeMotif("Blob Motif");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "motif",
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
            var written = await MotifMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded motif entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = MotifMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,             Is.EqualTo(src.Name));
            Assert.That(got.Description,       Is.EqualTo(src.Description));
            Assert.That(got.Appearances.Count, Is.EqualTo(src.Appearances.Count));
            Assert.That(got.Appearances[0].Scene,   Is.EqualTo(src.Appearances[0].Scene));
            Assert.That(got.Appearances[0].Meaning, Is.EqualTo(src.Appearances[0].Meaning));
        }
    }
}
