using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample NewsData the relational
/// round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the same logical
/// content as the original domain object, including the EntitiesInvolved and
/// Locations bridge lists.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class NewsRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private NewsRepository repo = null!;
    private IDbContextFactory<StreetSamuraiDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_nrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "news");
        repo = new NewsRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static NewsData MakeNews(string headline, string? id = null) => new()
    {
        Id              = id ?? Guid.NewGuid().ToString("N"),
        Type            = "news",
        Headline        = headline,
        Date            = "2144-06-01",
        Category        = "corporate",
        Source          = "GLMZ Sentinel",
        Reporter        = "Fern Takahashi",
        Body            = "A routine sweep of South Loop turned into a firefight when two corpo security details met at the same time.",
        Aftermath       = "Three dead, two hospitalized. RMA on scene within four minutes.",
        Casualties      = "3 dead, 2 critical",
        RunnerRelevance = "Both corpos are now hiring; the market opened.",
        Rating          = 72.5,
        VoteCount       = 8,
        MidjourneyPrompt = "neon rain, corpo firefight, chicago 2144",
        Dalle3Prompt    = "Corporate security standoff in a rain-soaked Chicago underpass",
        EntitiesInvolved = new List<string> { "Atlas-9 Security", "Carrion Enterprises" },
        Locations       = new List<string> { "South Loop", "35th & Halsted" },
        Tags            = new List<string> { "corporate", "violence", "south-loop" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeNews("Corporate Standoff Turns Lethal");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Headline,        Is.EqualTo(original.Headline));
            Assert.That(got.Date,             Is.EqualTo(original.Date));
            Assert.That(got.Category,         Is.EqualTo(original.Category));
            Assert.That(got.Source,           Is.EqualTo(original.Source));
            Assert.That(got.Reporter,         Is.EqualTo(original.Reporter));
            Assert.That(got.Body,             Is.EqualTo(original.Body));
            Assert.That(got.Aftermath,        Is.EqualTo(original.Aftermath));
            Assert.That(got.Casualties,       Is.EqualTo(original.Casualties));
            Assert.That(got.RunnerRelevance,  Is.EqualTo(original.RunnerRelevance));
            Assert.That(got.Rating,           Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,        Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt, Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,     Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_EntitiesInvolved_RoundTrip()
    {
        var original = MakeNews("EntitiesInvolved News");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.EntitiesInvolved, Is.EqualTo(original.EntitiesInvolved));
    }

    [Test]
    public void Save_Then_LoadOne_Locations_RoundTrip()
    {
        var original = MakeNews("Locations News");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Locations, Is.EqualTo(original.Locations));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeNews("Tags News");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "news-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeNews("News Alpha");
        var b = MakeNews("News Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(n => n.Headline == "News Alpha"), Is.True);
        Assert.That(all.Any(n => n.Headline == "News Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_HeadlineAndTags_Only()
    {
        var original = MakeNews("Lite News Test");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(n => n.Headline == "Lite News Test");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include news-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeNews("Editable News");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Body = "Updated body text.";
        loaded.Locations = new List<string> { "Updated Location" };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Body,      Is.EqualTo("Updated body text."), "re-save must overwrite scalars");
        Assert.That(got.Locations, Is.EqualTo(new List<string> { "Updated Location" }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.EntitiesInvolved.Count, Is.EqualTo(original.EntitiesInvolved.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeNews("Parity News — the loop never sleeps.");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Headline,       Is.EqualTo(original.Headline));
            Assert.That(relational.Date,           Is.EqualTo(original.Date));
            Assert.That(relational.Category,       Is.EqualTo(original.Category));
            Assert.That(relational.Body,           Is.EqualTo(original.Body));
            Assert.That(relational.Rating,         Is.EqualTo(original.Rating));
            Assert.That(relational.VoteCount,      Is.EqualTo(original.VoteCount));
        });
        Assert.That(relational.EntitiesInvolved, Is.EqualTo(original.EntitiesInvolved));
        Assert.That(relational.Locations,        Is.EqualTo(original.Locations));
        Assert.That(relational.Tags,             Is.EquivalentTo(original.Tags));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeNews("Blob News — corpo war, south side, nobody wins.");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "news",
                Name       = src.Headline,
                Slug       = WorldGraphService.Slugify(src.Headline),
                Status     = "canon",
                IsActive   = true,
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
            var written = await NewsMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded news item");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = NewsMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Headline,       Is.EqualTo(src.Headline));
            Assert.That(got.Body,            Is.EqualTo(src.Body));
            Assert.That(got.EntitiesInvolved, Is.EqualTo(src.EntitiesInvolved),
                "EntitiesInvolved bridge rows must be written by RebuildAllAsync");
            Assert.That(got.Locations,        Is.EqualTo(src.Locations),
                "Locations bridge rows must be written by RebuildAllAsync");
            Assert.That(got.Tags,            Is.EquivalentTo(src.Tags));
        }
    }
}
