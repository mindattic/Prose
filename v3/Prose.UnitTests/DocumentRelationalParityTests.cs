using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample WorldbuildingDocument the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class DocumentRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private WorldbuildingDocRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_dcrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "document");
        repo = new WorldbuildingDocRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static WorldbuildingDocument MakeDoc(string fileName, string? id = null) => new()
    {
        Id               = id ?? Guid.NewGuid().ToString("N"),
        FileName         = fileName,
        Title            = $"Title of {fileName}",
        Category         = "world-lore",
        Body             = "The Network runs everything and nothing. It is the substrate and the symptom.",
        LineCount        = 42,
        Rating           = 83.0,
        VoteCount        = 6,
        MidjourneyPrompt = "cyberpunk world lore document, neon text, dark background",
        Dalle3Prompt     = "Technical diagram of the GLMZ network infrastructure, labeled nodes",
        Headings         = new List<string> { "Overview", "History", "Current State", "Notable Entities" },
        Tags             = new List<string> { "glmz", "world-lore", "network" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeDoc("the_network_in_2225.md");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.FileName,         Is.EqualTo(original.FileName));
            Assert.That(got.Title,             Is.EqualTo(original.Title));
            Assert.That(got.Category,          Is.EqualTo(original.Category));
            Assert.That(got.Body,              Is.EqualTo(original.Body));
            Assert.That(got.LineCount,         Is.EqualTo(original.LineCount));
            Assert.That(got.Rating,            Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,         Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt,  Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,      Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Headings_RoundTrip()
    {
        var original = MakeDoc("headings_doc.md");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Headings, Is.EqualTo(original.Headings));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeDoc("tag_doc.md");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "document-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeDoc("doc_alpha.md");
        var b = MakeDoc("doc_beta.md");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.FileName == "doc_alpha.md"), Is.True);
        Assert.That(all.Any(x => x.FileName == "doc_beta.md"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_FileNameAndCategoryAndTags()
    {
        var original = MakeDoc("lite_doc.md");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(d => d.FileName == "lite_doc.md");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Category, Is.EqualTo(original.Category));
        Assert.That(hit.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include document-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeDoc("editable_doc.md");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Body = "Updated body text.";
        loaded.Headings = new List<string> { "Only One Section" };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Body, Is.EqualTo("Updated body text."),
            "re-save must overwrite scalars");
        Assert.That(got.Headings, Is.EqualTo(new List<string> { "Only One Section" }),
            "re-save must wipe and re-insert bridge rows");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeDoc("parity_doc.md");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.FileName,  Is.EqualTo(original.FileName));
            Assert.That(relational.Title,     Is.EqualTo(original.Title));
            Assert.That(relational.Category,  Is.EqualTo(original.Category));
            Assert.That(relational.Body,      Is.EqualTo(original.Body));
            Assert.That(relational.LineCount, Is.EqualTo(original.LineCount));
            Assert.That(relational.Rating,    Is.EqualTo(original.Rating));
            Assert.That(relational.VoteCount, Is.EqualTo(original.VoteCount));
        });

        Assert.That(relational.Headings, Is.EqualTo(original.Headings));
        Assert.That(relational.Tags,     Is.EquivalentTo(original.Tags));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeDoc("blob_doc.md");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "document",
                Name       = src.FileName,
                Slug       = UniverseGraphService.Slugify(src.FileName),
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
            var written = await DocumentMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded document entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = DocumentMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.FileName, Is.EqualTo(src.FileName));
            Assert.That(got.Title,     Is.EqualTo(src.Title));
            Assert.That(got.Category,  Is.EqualTo(src.Category));
            Assert.That(got.Headings,  Is.EqualTo(src.Headings));
            Assert.That(got.Tags,      Is.EquivalentTo(src.Tags));
        }
    }
}
