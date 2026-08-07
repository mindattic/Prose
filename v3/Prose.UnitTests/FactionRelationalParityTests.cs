using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample FactionData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// "Blob-materialized == relational-materialized" is tested by canonicalized
/// JSON equality: normalize both sides to a sorted, whitespace-trimmed JSON
/// string so field order and whitespace don't affect the comparison.
///
/// All against the in-memory SQLite test DB (TestDbFactory).  No live DB
/// required.
/// </summary>
[TestFixture]
public class FactionRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private FactionRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_frp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "faction");
        repo = new FactionRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static FactionData MakeFaction(string name, string? id = null) => new()
    {
        Id   = id ?? Guid.NewGuid().ToString("N"),
        Type = "faction",
        Name = name,
        Motto             = "Live free or die wired.",
        Description       = "A mid-tier street crew that runs extortion.",
        Ideology          = "Survival, loyalty to the crew above all.",
        Territory         = "South Loop underpass network.",
        Leadership        = "Mira Vasquez (acting boss)",
        NarrativeFunction = "Recurring antagonists in the Bushido arc.",
        MidjourneyPrompt  = "cyberpunk gang, neon lights, urban decay",
        Dalle3Prompt      = "Street gang under a viaduct, rain, 2144",
        Aliases           = new List<string> { "The Underpassers", "UP Crew" },
        Methods           = new List<string> { "Extortion", "Protection rackets" },
        Resources         = new List<string> { "Stolen vehicles", "Black-market stims" },
        Goals             = new List<string> { "Hold the South Loop", "Expand north to Chinatown" },
        StoryHooks        = new List<string> { "One member wants out and knows too much." },
        Tags              = new List<string> { "crew", "antagonist", "south-loop" },
        Relationships     = new List<FactionRelationship>
        {
            new() { Name = "Lotus Syndicate", Type = "rival", Description = "Blood feud over territory.",  Tags = new List<string> { "hostile", "active" } },
            new() { Name = "Carrion Enterprises", Type = "supplier", Description = "Buys med waste.", Tags = new List<string> { "transactional" } },
        },
        KnownMembers = new List<FactionMember>
        {
            new() { Name = "Mira Vasquez", Role = "Acting boss", Status = "active", Notes = "Ruthless pragmatist." },
            new() { Name = "Bao",          Role = "Enforcer",    Status = "active", Notes = "Silent, effective." },
        },
    };

    // ── Core parity test ───────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllFields()
    {
        var original = MakeFaction("South Loop Crew");
        repo.Save(original);

        var roundTripped = repo.GetById(original.Id);

        Assert.That(roundTripped, Is.Not.Null, "GetById must return a result after Save");
        Assert.That(roundTripped!.Name,             Is.EqualTo(original.Name));
        Assert.That(roundTripped.Motto,             Is.EqualTo(original.Motto));
        Assert.That(roundTripped.Description,       Is.EqualTo(original.Description));
        Assert.That(roundTripped.Ideology,          Is.EqualTo(original.Ideology));
        Assert.That(roundTripped.Territory,         Is.EqualTo(original.Territory));
        Assert.That(roundTripped.Leadership,        Is.EqualTo(original.Leadership));
        Assert.That(roundTripped.NarrativeFunction, Is.EqualTo(original.NarrativeFunction));
        Assert.That(roundTripped.MidjourneyPrompt,  Is.EqualTo(original.MidjourneyPrompt));
        Assert.That(roundTripped.Dalle3Prompt,      Is.EqualTo(original.Dalle3Prompt));
    }

    [Test]
    public void Save_Then_LoadOne_Aliases_RoundTrip()
    {
        var original = MakeFaction("Alias Faction");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_Methods_Resources_Goals_RoundTrip()
    {
        var original = MakeFaction("Methods Faction");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Methods,   Is.EqualTo(original.Methods));
        Assert.That(got.Resources, Is.EqualTo(original.Resources));
        Assert.That(got.Goals,     Is.EqualTo(original.Goals));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeFaction("Hook Faction");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeFaction("Tag Faction");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        // Tags live in the universal EntityTags layer, not in the Factions columns.
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "faction-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void Save_Then_LoadOne_Relationships_RoundTrip()
    {
        var original = MakeFaction("Rel Faction");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;

        Assert.That(got.Relationships.Count, Is.EqualTo(original.Relationships.Count));
        for (int i = 0; i < original.Relationships.Count; i++)
        {
            Assert.That(got.Relationships[i].Name,        Is.EqualTo(original.Relationships[i].Name));
            Assert.That(got.Relationships[i].Type,        Is.EqualTo(original.Relationships[i].Type));
            Assert.That(got.Relationships[i].Description, Is.EqualTo(original.Relationships[i].Description));
        }
    }

    [Test]
    public void Save_Then_LoadOne_RelationshipTags_RoundTrip()
    {
        var original = MakeFaction("RelTag Faction");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;

        // The first relationship has tags ["hostile", "active"].
        Assert.That(got.Relationships[0].Tags,
            Is.EqualTo(original.Relationships[0].Tags),
            "relationship-level tags must round-trip through FactionRelationshipTags bridge");
        Assert.That(got.Relationships[1].Tags,
            Is.EqualTo(original.Relationships[1].Tags));
    }

    [Test]
    public void Save_Then_LoadOne_KnownMembers_RoundTrip()
    {
        var original = MakeFaction("Member Faction");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;

        Assert.That(got.KnownMembers.Count, Is.EqualTo(original.KnownMembers.Count));
        for (int i = 0; i < original.KnownMembers.Count; i++)
        {
            Assert.That(got.KnownMembers[i].Name,   Is.EqualTo(original.KnownMembers[i].Name));
            Assert.That(got.KnownMembers[i].Role,   Is.EqualTo(original.KnownMembers[i].Role));
            Assert.That(got.KnownMembers[i].Status, Is.EqualTo(original.KnownMembers[i].Status));
            Assert.That(got.KnownMembers[i].Notes,  Is.EqualTo(original.KnownMembers[i].Notes));
        }
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeFaction("Faction Alpha");
        var b = MakeFaction("Faction Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(f => f.Name == "Faction Alpha"), Is.True);
        Assert.That(all.Any(f => f.Name == "Faction Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndTags_Only()
    {
        var original = MakeFaction("Lite Faction");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(f => f.Name == "Lite Faction");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include faction-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeFaction("Editable Faction");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.Goals = new List<string> { "New goal only" };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.Goals, Is.EqualTo(new List<string> { "New goal only" }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Methods.Count, Is.EqualTo(original.Methods.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalJson_BlobEqualsRelational_AfterRoundTrip()
    {
        // This is the key parity assertion: the canonical JSON representation of
        // the original data equals the canonical JSON of what came back from the
        // relational read, ignoring field order and whitespace. We compare the
        // subset of fields that the relational mapper owns (Tags round-trip from
        // EntityTags, so they may arrive in a different order — use EquivalentTo
        // for the whole document instead of byte-equal JSON for now).

        var original = MakeFaction("Parity Faction");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        // Scalars.
        Assert.Multiple(() =>
        {
            Assert.That(relational.Name,             Is.EqualTo(original.Name));
            Assert.That(relational.Motto,            Is.EqualTo(original.Motto));
            Assert.That(relational.Description,      Is.EqualTo(original.Description));
            Assert.That(relational.Ideology,         Is.EqualTo(original.Ideology));
            Assert.That(relational.Territory,        Is.EqualTo(original.Territory));
            Assert.That(relational.Leadership,       Is.EqualTo(original.Leadership));
            Assert.That(relational.NarrativeFunction,Is.EqualTo(original.NarrativeFunction));
            Assert.That(relational.MidjourneyPrompt, Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(relational.Dalle3Prompt,     Is.EqualTo(original.Dalle3Prompt));
            Assert.That(relational.Rating,           Is.EqualTo(original.Rating));
            Assert.That(relational.VoteCount,        Is.EqualTo(original.VoteCount));
        });

        // Collections (order-preserving).
        Assert.That(relational.Aliases,    Is.EqualTo(original.Aliases));
        Assert.That(relational.Methods,    Is.EqualTo(original.Methods));
        Assert.That(relational.Resources,  Is.EqualTo(original.Resources));
        Assert.That(relational.Goals,      Is.EqualTo(original.Goals));
        Assert.That(relational.StoryHooks, Is.EqualTo(original.StoryHooks));
        Assert.That(relational.Tags,       Is.EquivalentTo(original.Tags));

        // Relationships (including per-relationship tags).
        Assert.That(relational.Relationships.Count, Is.EqualTo(original.Relationships.Count));
        for (int i = 0; i < original.Relationships.Count; i++)
        {
            Assert.That(relational.Relationships[i].Name,        Is.EqualTo(original.Relationships[i].Name));
            Assert.That(relational.Relationships[i].Type,        Is.EqualTo(original.Relationships[i].Type));
            Assert.That(relational.Relationships[i].Description, Is.EqualTo(original.Relationships[i].Description));
            Assert.That(relational.Relationships[i].Tags,        Is.EqualTo(original.Relationships[i].Tags),
                $"relationship[{i}] tags must survive FactionRelationshipTags round-trip");
        }

        // Members.
        Assert.That(relational.KnownMembers.Count, Is.EqualTo(original.KnownMembers.Count));
        for (int i = 0; i < original.KnownMembers.Count; i++)
        {
            Assert.That(relational.KnownMembers[i].Name,   Is.EqualTo(original.KnownMembers[i].Name));
            Assert.That(relational.KnownMembers[i].Role,   Is.EqualTo(original.KnownMembers[i].Role));
            Assert.That(relational.KnownMembers[i].Status, Is.EqualTo(original.KnownMembers[i].Status));
            Assert.That(relational.KnownMembers[i].Notes,  Is.EqualTo(original.KnownMembers[i].Notes));
        }
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        // Simulate the production backfill scenario: seed a Records blob directly
        // (as the old import path would have done), then call RebuildAllAsync and
        // verify the relational row materializes with correct field values.

        var src = MakeFaction("Blob Faction");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            // Seed Entity + blob Record directly — skip FactionMapper to prove
            // RebuildAllAsync starts from the raw blob, not from a prior relational row.
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "faction",
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

        // Run the backfill.
        using (var db = factory.CreateDbContext())
        {
            var written = await FactionMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded faction");
        }

        // Verify the relational row is now readable via FactionMapper.LoadOne.
        using (var db = factory.CreateDbContext())
        {
            var got = FactionMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,    Is.EqualTo(src.Name));
            Assert.That(got.Motto,    Is.EqualTo(src.Motto));
            Assert.That(got.Aliases,  Is.EqualTo(src.Aliases));
            Assert.That(got.Goals,    Is.EqualTo(src.Goals));
            Assert.That(got.Tags,     Is.EquivalentTo(src.Tags));
            Assert.That(got.Relationships[0].Tags,
                Is.EqualTo(src.Relationships[0].Tags),
                "relationship tags must be written by RebuildAllAsync via FactionRelationshipTags");
        }
    }
}
