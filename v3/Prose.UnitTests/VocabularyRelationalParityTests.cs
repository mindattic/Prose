using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample VocabularyData the relational
/// round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the same logical
/// content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class VocabularyRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private VocabularyRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_vrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "vocabulary");
        repo = new VocabularyRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static VocabularyData MakeVocab(string term, string? id = null) => new()
    {
        Id         = id ?? Guid.NewGuid().ToString("N"),
        Term       = term,
        Definition = "A runner who burns bridges and still expects to cross them.",
        Origin     = "South Loop street slang, circa 2130s",
        Usage      = "Derogatory. Used between runners who've been burned.",
        Tier       = "street",
        Category   = "runner culture",
        Example    = "Don't hire a sandbridge — you'll find out what it means the hard way.",
        Tags       = new List<string> { "runner", "slang", "derogatory" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeVocab("sandbridge");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Term,       Is.EqualTo(original.Term));
            Assert.That(got.Definition,  Is.EqualTo(original.Definition));
            Assert.That(got.Origin,      Is.EqualTo(original.Origin));
            Assert.That(got.Usage,       Is.EqualTo(original.Usage));
            Assert.That(got.Tier,        Is.EqualTo(original.Tier));
            Assert.That(got.Category,    Is.EqualTo(original.Category));
            Assert.That(got.Example,     Is.EqualTo(original.Example));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeVocab("flatline-friend");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "vocabulary-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeVocab("ghost-step");
        var b = MakeVocab("wire-widow");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(v => v.Term == "ghost-step"), Is.True);
        Assert.That(all.Any(v => v.Term == "wire-widow"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_TermAndTags_Only()
    {
        var original = MakeVocab("lite-term-test");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(v => v.Term == "lite-term-test");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include vocabulary-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeVocab("editable-term");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Definition = "Updated definition.";
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Definition, Is.EqualTo("Updated definition."),
            "re-save must overwrite scalars");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeVocab("parity-term");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Term,       Is.EqualTo(original.Term));
            Assert.That(relational.Definition, Is.EqualTo(original.Definition));
            Assert.That(relational.Origin,     Is.EqualTo(original.Origin));
            Assert.That(relational.Usage,      Is.EqualTo(original.Usage));
            Assert.That(relational.Tier,       Is.EqualTo(original.Tier));
            Assert.That(relational.Category,   Is.EqualTo(original.Category));
            Assert.That(relational.Example,    Is.EqualTo(original.Example));
        });
        Assert.That(relational.Tags, Is.EquivalentTo(original.Tags));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeVocab("blob-term");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "vocabulary",
                Name       = src.Term,
                Slug       = WorldGraphService.Slugify(src.Term),
                Status     = "canon",
                
                CreatedAt  = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            });
            var json = System.Text.Json.JsonSerializer.Serialize(src, new System.Text.Json.JsonSerializerOptions
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
            var written = await VocabularyMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded vocabulary entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = VocabularyMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Term,       Is.EqualTo(src.Term));
            Assert.That(got.Definition,  Is.EqualTo(src.Definition));
            Assert.That(got.Tags,        Is.EquivalentTo(src.Tags));
        }
    }
}
