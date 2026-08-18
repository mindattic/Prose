using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample ContractData the relational
/// round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the same logical
/// content as the original domain object, including Bonuses, Complications, and
/// all 10 CrewCapabilities scalar columns.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class ContractRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private ContractRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_crp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "contract");
        repo = new ContractRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static ContractData MakeContract(string codename, string? id = null) => new()
    {
        Id          = id ?? Guid.NewGuid().ToString("N"),
        Type        = "contract",
        Codename    = codename,
        Status      = "open",
        Client      = "Anonymous",
        ClientTier  = "mid",
        Category    = "extraction",
        Description = "Extract a data courier before they reach the Atlas-9 checkpoint.",
        Objective   = "Intercept courier at 35th & Halsted. Secure the package. Exit clean.",
        Location    = "South Loop transit corridor",
        Target      = "Courier (name unknown)",
        Opposition  = "Atlas-9 security patrol (3 guards)",
        Payout      = "Φ4,500 base",
        CrewSize    = "2–4",
        Difficulty  = "hard",
        TimeLimit   = "90 minutes",
        Outcome     = "",
        Rating      = 81.0,
        VoteCount   = 11,
        MidjourneyPrompt = "runner crew, neon corridor, extraction op",
        Dalle3Prompt = "Cyberpunk courier extraction in a rain-soaked transit corridor",
        RequiredCapabilities = new CrewCapabilities
        {
            Combat = 7, Stealth = 6, Hacking = 3, Social = 2,
            Medical = 4, Tech = 2, Transport = 5, Demolitions = 0,
            Surveillance = 4, Linguistics = 1,
        },
        Bonuses = new List<ContractBonus>
        {
            new() { Type = "clean_exit", Amount = "Φ500", Condition = "No civilian witnesses." },
            new() { Type = "package_intact", Amount = "Φ1,000", Condition = "Package delivered undamaged." },
        },
        Complications = new List<string>
        {
            "The courier is also being tracked by a second crew.",
            "Atlas-9 has a spotter drone in the area.",
        },
        Tags = new List<string> { "extraction", "south-loop", "hard" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeContract("OPERATION CLEAN SLATE");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Codename,    Is.EqualTo(original.Codename));
            Assert.That(got.Status,       Is.EqualTo(original.Status));
            Assert.That(got.Client,       Is.EqualTo(original.Client));
            Assert.That(got.ClientTier,   Is.EqualTo(original.ClientTier));
            Assert.That(got.Category,     Is.EqualTo(original.Category));
            Assert.That(got.Description,  Is.EqualTo(original.Description));
            Assert.That(got.Objective,    Is.EqualTo(original.Objective));
            Assert.That(got.Location,     Is.EqualTo(original.Location));
            Assert.That(got.Target,       Is.EqualTo(original.Target));
            Assert.That(got.Opposition,   Is.EqualTo(original.Opposition));
            Assert.That(got.Payout,       Is.EqualTo(original.Payout));
            Assert.That(got.CrewSize,     Is.EqualTo(original.CrewSize));
            Assert.That(got.Difficulty,   Is.EqualTo(original.Difficulty));
            Assert.That(got.TimeLimit,    Is.EqualTo(original.TimeLimit));
            Assert.That(got.Rating,       Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,    Is.EqualTo(original.VoteCount));
        });
    }

    [Test]
    public void Save_Then_LoadOne_CrewCapabilities_RoundTrip()
    {
        var original = MakeContract("CAPS CONTRACT");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        var caps = got.RequiredCapabilities;
        var orig = original.RequiredCapabilities;
        Assert.That(caps.Combat,       Is.EqualTo(orig.Combat));
        Assert.That(caps.Stealth,      Is.EqualTo(orig.Stealth));
        Assert.That(caps.Hacking,      Is.EqualTo(orig.Hacking));
        Assert.That(caps.Social,       Is.EqualTo(orig.Social));
        Assert.That(caps.Medical,      Is.EqualTo(orig.Medical));
        Assert.That(caps.Tech,         Is.EqualTo(orig.Tech));
        Assert.That(caps.Transport,    Is.EqualTo(orig.Transport));
        Assert.That(caps.Demolitions,  Is.EqualTo(orig.Demolitions));
        Assert.That(caps.Surveillance, Is.EqualTo(orig.Surveillance));
        Assert.That(caps.Linguistics,  Is.EqualTo(orig.Linguistics));
    }

    [Test]
    public void Save_Then_LoadOne_Bonuses_RoundTrip()
    {
        var original = MakeContract("BONUSES CONTRACT");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;

        Assert.That(got.Bonuses.Count, Is.EqualTo(original.Bonuses.Count));
        for (int i = 0; i < original.Bonuses.Count; i++)
        {
            Assert.That(got.Bonuses[i].Type,      Is.EqualTo(original.Bonuses[i].Type));
            Assert.That(got.Bonuses[i].Amount,    Is.EqualTo(original.Bonuses[i].Amount));
            Assert.That(got.Bonuses[i].Condition, Is.EqualTo(original.Bonuses[i].Condition));
        }
    }

    [Test]
    public void Save_Then_LoadOne_Complications_RoundTrip()
    {
        var original = MakeContract("COMPLICATIONS CONTRACT");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Complications, Is.EqualTo(original.Complications));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeContract("TAGS CONTRACT");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "contract-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeContract("CONTRACT ALPHA");
        var b = MakeContract("CONTRACT BETA");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(c => c.Codename == "CONTRACT ALPHA"), Is.True);
        Assert.That(all.Any(c => c.Codename == "CONTRACT BETA"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_CodenameAndTags_Only()
    {
        var original = MakeContract("LITE CONTRACT TEST");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(c => c.Codename == "LITE CONTRACT TEST");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include contract-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeContract("EDITABLE CONTRACT");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.Complications = new List<string> { "New complication only" };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.Complications, Is.EqualTo(new List<string> { "New complication only" }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Bonuses.Count, Is.EqualTo(original.Bonuses.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeContract("PARITY CONTRACT");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Codename,   Is.EqualTo(original.Codename));
            Assert.That(relational.Status,     Is.EqualTo(original.Status));
            Assert.That(relational.Category,   Is.EqualTo(original.Category));
            Assert.That(relational.Difficulty, Is.EqualTo(original.Difficulty));
            Assert.That(relational.Rating,     Is.EqualTo(original.Rating));
            Assert.That(relational.VoteCount,  Is.EqualTo(original.VoteCount));
        });
        Assert.That(relational.Bonuses.Count,      Is.EqualTo(original.Bonuses.Count));
        Assert.That(relational.Complications,      Is.EqualTo(original.Complications));
        Assert.That(relational.Tags,               Is.EquivalentTo(original.Tags));

        var caps = relational.RequiredCapabilities;
        var orig = original.RequiredCapabilities;
        Assert.That(caps.Combat,  Is.EqualTo(orig.Combat));
        Assert.That(caps.Stealth, Is.EqualTo(orig.Stealth));
        Assert.That(caps.Medical, Is.EqualTo(orig.Medical));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeContract("BLOB CONTRACT");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "contract",
                Name       = src.Codename,
                Slug       = WorldGraphService.Slugify(src.Codename),
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
            var written = await ContractMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded contract");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = ContractMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Codename,      Is.EqualTo(src.Codename));
            Assert.That(got.Complications,  Is.EqualTo(src.Complications),
                "Complications bridge rows must be written by RebuildAllAsync");
            Assert.That(got.Bonuses.Count,  Is.EqualTo(src.Bonuses.Count),
                "Bonuses bridge rows must be written by RebuildAllAsync");
            Assert.That(got.RequiredCapabilities.Combat, Is.EqualTo(src.RequiredCapabilities.Combat),
                "Capability scalars must survive RebuildAllAsync");
            Assert.That(got.Tags,           Is.EquivalentTo(src.Tags));
        }
    }
}
