using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample QuoteData the relational
/// round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the same logical
/// content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class QuoteRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private QuoteRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_qrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "quote");
        repo = new QuoteRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static QuoteData MakeQuote(string text, string? id = null) => new()
    {
        Id          = id ?? Guid.NewGuid().ToString("N"),
        Quote       = text,
        Attribution = "Street proverb, GLMZ circa 2144",
        Source      = "Spoken word",
        Context     = "Used when a runner accepts a job they know will go wrong.",
        Category    = "philosophy",
        InWorld     = true,
        Tags        = new List<string> { "street", "wisdom", "runner" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeQuote("The city doesn't owe you a soft landing.");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.That(got!.Quote,       Is.EqualTo(original.Quote));
        Assert.That(got.Attribution,  Is.EqualTo(original.Attribution));
        Assert.That(got.Source,       Is.EqualTo(original.Source));
        Assert.That(got.Context,      Is.EqualTo(original.Context));
        Assert.That(got.Category,     Is.EqualTo(original.Category));
        Assert.That(got.InWorld,      Is.EqualTo(original.InWorld));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeQuote("Silence costs less than apologies.");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "quote-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeQuote("Quote Alpha");
        var b = MakeQuote("Quote Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(q => q.Quote == "Quote Alpha"), Is.True);
        Assert.That(all.Any(q => q.Quote == "Quote Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_QuoteAndTags_Only()
    {
        var original = MakeQuote("Lite Quote Test");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(q => q.Quote == "Lite Quote Test");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include quote-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeQuote("Editable Quote");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Attribution = "Updated attribution.";
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Attribution, Is.EqualTo("Updated attribution."),
            "re-save must overwrite scalars");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeQuote("Parity Quote — the city never forgets.");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Quote,       Is.EqualTo(original.Quote));
            Assert.That(relational.Attribution, Is.EqualTo(original.Attribution));
            Assert.That(relational.Source,      Is.EqualTo(original.Source));
            Assert.That(relational.Context,     Is.EqualTo(original.Context));
            Assert.That(relational.Category,    Is.EqualTo(original.Category));
            Assert.That(relational.InWorld,     Is.EqualTo(original.InWorld));
        });
        Assert.That(relational.Tags, Is.EquivalentTo(original.Tags));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeQuote("Blob Quote — pay your debts before the city calls them in.");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "quote",
                Name       = src.Quote,
                Slug       = WorldGraphService.Slugify(src.Quote.Length > 40 ? src.Quote[..40] : src.Quote),
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
            var written = await QuoteMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded quote");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = QuoteMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Quote,      Is.EqualTo(src.Quote));
            Assert.That(got.Attribution, Is.EqualTo(src.Attribution));
            Assert.That(got.Tags,        Is.EquivalentTo(src.Tags));
        }
    }
}
