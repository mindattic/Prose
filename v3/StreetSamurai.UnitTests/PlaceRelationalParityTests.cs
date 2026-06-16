using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample DistrictData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class PlaceRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private DistrictRepository repo = null!;
    private IDbContextFactory<StreetSamuraiDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_plrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "place");
        repo = new DistrictRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static DistrictData MakePlace(string name, string? id = null) => new()
    {
        Id             = id ?? Guid.NewGuid().ToString("N"),
        Type           = "place",
        Name           = name,
        Description    = "A district where corporate money and street-level desperation meet.",
        Demographics   = "Mixed — migrant workers, displaced families, low-tier corp contractors.",
        Economy        = "Gray-market services, contract labor, underground pharmaceutical trade.",
        PowerStructure = "Lotus Syndicate controls the blocks east of the rail line.",
        Rating         = 80.0,
        VoteCount      = 4,
        MidjourneyPrompt = "cyberpunk district, neon-lit streets, urban decay, 2225",
        Dalle3Prompt     = "Aerial map of cyberpunk district with annotated zones",
        Aliases        = new List<string> { "The Seam", "Lotus Run" },
        Dangers        = new List<string> { "Lotus Syndicate enforcers patrol after dark.", "RZ bleed pockets near the rail underpass." },
        Opportunities  = new List<string> { "Black-market pharma contacts.", "Cheap lodging, no questions asked." },
        StoryHooks     = new List<string> { "A body was found at the rail underpass — no ID, no prints.", "Someone spray-painted the Counter's frequency on every wall." },
        Atmosphere = new AtmosphereData
        {
            Feel   = "A place where the city holds its breath.",
            Sights = new List<string> { "Cracked maglev pylons", "Neon pharmacy signs in three languages" },
            Sounds = new List<string> { "Rail hum", "Distant sirens that never get closer" },
            Smells = new List<string> { "Ozone and frying oil", "Lotus incense from the corner shrine" },
        },
        Connections = new DistrictConnections
        {
            AdjacentTo = new List<string> { "The Warrens", "Ashgrave Synthesis Corridor" },
            Exits = new List<PlaceExit>
            {
                new() { Direction = "north", Destination = "The Warrens", Type = "road", Description = "An elevated walkway runs three blocks north.", Restricted = false, DangerLevel = 2 },
                new() { Direction = "east", Destination = "Ashgrave Synthesis Corridor", Type = "tunnel", Description = "A maintenance corridor beneath the rail line.", Restricted = true, DangerLevel = 4 },
            },
        },
        FrequentedBy = new List<string> { "Lotus Syndicate enforcers", "Nadia" },
        NotableLocations = new List<NotableLocation>
        {
            new() { Name = "The Corner Shrine", Description = "Incense, circuit boards, and a photo of someone's mother." },
            new() { Name = "Rail Underpass", Description = "Where the bleed pockets form most often." },
        },
        Coordinates = new GeoCoordinates { Lat = 41.87, Lng = -87.63 },
        RelatedEntities = new List<string> { "Lotus Syndicate", "Crucible Genomics" },
        Tags           = new List<string> { "lotus", "glmz", "district" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakePlace("The Seam District");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,            Is.EqualTo(original.Name));
            Assert.That(got.Description,      Is.EqualTo(original.Description));
            Assert.That(got.Demographics,     Is.EqualTo(original.Demographics));
            Assert.That(got.Economy,          Is.EqualTo(original.Economy));
            Assert.That(got.PowerStructure,   Is.EqualTo(original.PowerStructure));
            Assert.That(got.Rating,           Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,        Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt, Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,     Is.EqualTo(original.Dalle3Prompt));
            Assert.That(got.Atmosphere.Feel,  Is.EqualTo(original.Atmosphere.Feel));
            Assert.That(got.Coordinates.Lat,  Is.EqualTo(original.Coordinates.Lat));
            Assert.That(got.Coordinates.Lng,  Is.EqualTo(original.Coordinates.Lng));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Aliases_RoundTrip()
    {
        var original = MakePlace("Alias Place");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_Dangers_RoundTrip()
    {
        var original = MakePlace("Danger Place");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Dangers, Is.EqualTo(original.Dangers));
    }

    [Test]
    public void Save_Then_LoadOne_Opportunities_RoundTrip()
    {
        var original = MakePlace("Opportunity Place");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Opportunities, Is.EqualTo(original.Opportunities));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakePlace("Hook Place");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_AtmosphereItems_RoundTrip()
    {
        var original = MakePlace("Atmosphere Place");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Atmosphere.Sights, Is.EqualTo(original.Atmosphere.Sights));
        Assert.That(got.Atmosphere.Sounds, Is.EqualTo(original.Atmosphere.Sounds));
        Assert.That(got.Atmosphere.Smells, Is.EqualTo(original.Atmosphere.Smells));
    }

    [Test]
    public void Save_Then_LoadOne_Adjacencies_RoundTrip()
    {
        var original = MakePlace("Adjacent Place");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Connections.AdjacentTo, Is.EqualTo(original.Connections.AdjacentTo));
    }

    [Test]
    public void Save_Then_LoadOne_Exits_RoundTrip()
    {
        var original = MakePlace("Exit Place");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;

        Assert.That(got.Connections.Exits.Count, Is.EqualTo(original.Connections.Exits.Count));
        for (int i = 0; i < original.Connections.Exits.Count; i++)
        {
            Assert.That(got.Connections.Exits[i].Direction,   Is.EqualTo(original.Connections.Exits[i].Direction));
            Assert.That(got.Connections.Exits[i].Destination, Is.EqualTo(original.Connections.Exits[i].Destination));
            Assert.That(got.Connections.Exits[i].Type,        Is.EqualTo(original.Connections.Exits[i].Type));
            Assert.That(got.Connections.Exits[i].Description, Is.EqualTo(original.Connections.Exits[i].Description));
            Assert.That(got.Connections.Exits[i].Restricted,  Is.EqualTo(original.Connections.Exits[i].Restricted));
            Assert.That(got.Connections.Exits[i].DangerLevel, Is.EqualTo(original.Connections.Exits[i].DangerLevel));
        }
    }

    [Test]
    public void Save_Then_LoadOne_FrequentedBy_RoundTrip()
    {
        var original = MakePlace("FrequentedBy Place");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.FrequentedBy, Is.EqualTo(original.FrequentedBy));
    }

    [Test]
    public void Save_Then_LoadOne_NotableLocations_RoundTrip()
    {
        var original = MakePlace("Notable Place");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;

        Assert.That(got.NotableLocations.Count, Is.EqualTo(original.NotableLocations.Count));
        for (int i = 0; i < original.NotableLocations.Count; i++)
        {
            Assert.That(got.NotableLocations[i].Name,        Is.EqualTo(original.NotableLocations[i].Name));
            Assert.That(got.NotableLocations[i].Description, Is.EqualTo(original.NotableLocations[i].Description));
        }
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakePlace("Tag Place");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "place-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakePlace("Place Alpha");
        var b = MakePlace("Place Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Place Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Place Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndTags()
    {
        var original = MakePlace("Lite Place");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(p => p.Name == "Lite Place");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include place-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakePlace("Editable Place");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.Dangers = new List<string> { "Only one danger now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.Dangers, Is.EqualTo(new List<string> { "Only one danger now." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakePlace("Blob Place");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "place",
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
            var written = await PlaceMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded place entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = PlaceMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,        Is.EqualTo(src.Name));
            Assert.That(got.Description,  Is.EqualTo(src.Description));
            Assert.That(got.Aliases,      Is.EqualTo(src.Aliases));
            Assert.That(got.Dangers,      Is.EqualTo(src.Dangers));
            Assert.That(got.Atmosphere.Sights, Is.EqualTo(src.Atmosphere.Sights));
            Assert.That(got.Connections.AdjacentTo, Is.EqualTo(src.Connections.AdjacentTo));
            Assert.That(got.Connections.Exits.Count, Is.EqualTo(src.Connections.Exits.Count));
            Assert.That(got.Tags,         Is.EquivalentTo(src.Tags));
        }
    }
}
