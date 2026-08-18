using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// RFC 0007 parity gate — proves that for a sample AutomatonData the
/// relational round-trip (Save → LoadOne / LoadAll / LoadAllLite) produces the
/// same logical content as the original domain object.
///
/// All against the in-memory SQLite test DB (TestDbFactory). No live DB required.
/// </summary>
[TestFixture]
public class AutomatonRelationalParityTests
{
    private string tempDir = "";
    private TestPathProviderWithRoot paths = null!;
    private AutomatonRepository repo = null!;
    private IDbContextFactory<ProseDbContext> factory = null!;

    [SetUp]
    public void SetUp()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ss_atrp_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        paths = new TestPathProviderWithRoot(tempDir);
        TestDbFactory.Reset(paths);
        factory = TestDbFactory.For(paths, "automaton");
        repo = new AutomatonRepository(paths);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
    }

    // ── Fixture builder ────────────────────────────────────────────────────

    private static AutomatonData MakeAutomaton(string name, string? id = null) => new()
    {
        Id               = id ?? Guid.NewGuid().ToString("N"),
        Type             = "automaton",
        Name             = name,
        Classification   = "bipedal combat platform",
        Manufacturer     = "Arcturus Defense Systems",
        Description      = "Heavy infantry suppression unit deployed in urban pacification roles.",
        TierAvailability = "tier-4",
        Legality         = "military only",
        AutonomyLevel    = "semi-autonomous — requires operator authorization for lethal action",
        Dimensions       = "2.4m tall, 1.1m shoulder width",
        Weight           = "380kg",
        PowerSource      = "microfusion cell, 72h rated",
        Locomotion       = "bipedal articulated legs, rubber treads in low-grav mode",
        Countermeasures  = "EMP shielding, anti-spoofing IFF, reactive armor panels",
        CulturalContext  = "Arcturus markets them as 'Sentinel' units. Street name: Ironback.",
        Rating           = 88.0,
        VoteCount        = 14,
        MidjourneyPrompt = "heavy bipedal combat robot, urban rubble, searchlight eyes",
        Dalle3Prompt     = "Technical schematic of a bipedal military automaton with reactive armor",
        Aliases          = new List<string> { "Sentinel", "Ironback" },
        Armament         = new List<string> { "Dual 20mm rotary cannon", "Shoulder-mount missile pod" },
        Sensors          = new List<string> { "Thermal imaging", "LIDAR array", "Acoustic sensor net" },
        KnownDeployments = new List<string> { "Eastside Pacification 2141", "Waxwing Spire lockdown 2143" },
        StoryHooks       = new List<string> { "Kyle's Neuretics can model its kill cone at 40m.", "Someone deactivated three of them and left the pilot locked inside." },
        Tags             = new List<string> { "combat", "arcturus", "heavy" },
    };

    // ── Tests ──────────────────────────────────────────────────────────────

    [Test]
    public void Save_Then_LoadOne_RoundTrips_AllScalars()
    {
        var original = MakeAutomaton("Sentinel Mk-III");
        repo.Save(original);

        var got = repo.GetById(original.Id);

        Assert.That(got, Is.Not.Null, "GetById must return a result after Save");
        Assert.Multiple(() =>
        {
            Assert.That(got!.Name,             Is.EqualTo(original.Name));
            Assert.That(got.Classification,    Is.EqualTo(original.Classification));
            Assert.That(got.Manufacturer,      Is.EqualTo(original.Manufacturer));
            Assert.That(got.Description,       Is.EqualTo(original.Description));
            Assert.That(got.TierAvailability,  Is.EqualTo(original.TierAvailability));
            Assert.That(got.Legality,          Is.EqualTo(original.Legality));
            Assert.That(got.AutonomyLevel,     Is.EqualTo(original.AutonomyLevel));
            Assert.That(got.Dimensions,        Is.EqualTo(original.Dimensions));
            Assert.That(got.Weight,            Is.EqualTo(original.Weight));
            Assert.That(got.PowerSource,       Is.EqualTo(original.PowerSource));
            Assert.That(got.Locomotion,        Is.EqualTo(original.Locomotion));
            Assert.That(got.Countermeasures,   Is.EqualTo(original.Countermeasures));
            Assert.That(got.CulturalContext,   Is.EqualTo(original.CulturalContext));
            Assert.That(got.Rating,            Is.EqualTo(original.Rating));
            Assert.That(got.VoteCount,         Is.EqualTo(original.VoteCount));
            Assert.That(got.MidjourneyPrompt,  Is.EqualTo(original.MidjourneyPrompt));
            Assert.That(got.Dalle3Prompt,      Is.EqualTo(original.Dalle3Prompt));
        });
    }

    [Test]
    public void Save_Then_LoadOne_Aliases_RoundTrip()
    {
        var original = MakeAutomaton("Alias Automaton");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Aliases, Is.EqualTo(original.Aliases));
    }

    [Test]
    public void Save_Then_LoadOne_Armament_RoundTrip()
    {
        var original = MakeAutomaton("Armament Automaton");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Armament, Is.EqualTo(original.Armament));
    }

    [Test]
    public void Save_Then_LoadOne_Sensors_RoundTrip()
    {
        var original = MakeAutomaton("Sensor Automaton");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Sensors, Is.EqualTo(original.Sensors));
    }

    [Test]
    public void Save_Then_LoadOne_KnownDeployments_RoundTrip()
    {
        var original = MakeAutomaton("Deployment Automaton");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.KnownDeployments, Is.EqualTo(original.KnownDeployments));
    }

    [Test]
    public void Save_Then_LoadOne_StoryHooks_RoundTrip()
    {
        var original = MakeAutomaton("Hook Automaton");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.StoryHooks, Is.EqualTo(original.StoryHooks));
    }

    [Test]
    public void Save_Then_LoadOne_Tags_RoundTrip_ViaEntityTags()
    {
        var original = MakeAutomaton("Tag Automaton");
        repo.Save(original);
        var got = repo.GetById(original.Id)!;
        Assert.That(got.Tags, Is.EquivalentTo(original.Tags),
            "automaton-level tags must survive the relational round-trip via EntityTags");
    }

    [Test]
    public void GetAll_ReturnsAllSaved_WithFullFields()
    {
        var a = MakeAutomaton("Automaton Alpha");
        var b = MakeAutomaton("Automaton Beta");
        repo.Save(a);
        repo.Save(b);

        var all = repo.GetAll();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(all.Any(x => x.Name == "Automaton Alpha"), Is.True);
        Assert.That(all.Any(x => x.Name == "Automaton Beta"),  Is.True);
    }

    [Test]
    public void GetAllLite_Returns_NameAndClassificationAndTags_Only()
    {
        var original = MakeAutomaton("Lite Automaton");
        repo.Save(original);

        var lite = repo.GetAllLite();
        var hit = lite.FirstOrDefault(a => a.Name == "Lite Automaton");
        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.Classification, Is.EqualTo(original.Classification));
        Assert.That(hit.Tags, Is.EquivalentTo(original.Tags),
            "lite projection must include automaton-level tags from EntityTags");
    }

    [Test]
    public void Edit_Then_GetById_ReflectsChange()
    {
        var original = MakeAutomaton("Editable Automaton");
        repo.Save(original);

        var loaded = repo.GetById(original.Id)!;
        loaded.Description = "Updated description.";
        loaded.Armament = new List<string> { "Only one weapon now." };
        repo.Save(loaded);

        var got = repo.GetById(original.Id)!;
        Assert.That(got.Description, Is.EqualTo("Updated description."),
            "re-save must overwrite scalars");
        Assert.That(got.Armament, Is.EqualTo(new List<string> { "Only one weapon now." }),
            "re-save must wipe and re-insert bridge rows");
        Assert.That(got.Aliases.Count, Is.EqualTo(original.Aliases.Count),
            "unchanged bridges must survive the wipe-and-reinsert");
    }

    [Test]
    public void CanonicalFields_BlobEqualsRelational_AfterRoundTrip()
    {
        var original = MakeAutomaton("Parity Automaton");
        repo.Save(original);

        var relational = repo.GetById(original.Id)!;

        Assert.Multiple(() =>
        {
            Assert.That(relational.Name,            Is.EqualTo(original.Name));
            Assert.That(relational.Classification,  Is.EqualTo(original.Classification));
            Assert.That(relational.Manufacturer,    Is.EqualTo(original.Manufacturer));
            Assert.That(relational.Description,     Is.EqualTo(original.Description));
            Assert.That(relational.TierAvailability,Is.EqualTo(original.TierAvailability));
            Assert.That(relational.Legality,        Is.EqualTo(original.Legality));
            Assert.That(relational.AutonomyLevel,   Is.EqualTo(original.AutonomyLevel));
            Assert.That(relational.Dimensions,      Is.EqualTo(original.Dimensions));
            Assert.That(relational.Weight,          Is.EqualTo(original.Weight));
            Assert.That(relational.PowerSource,     Is.EqualTo(original.PowerSource));
            Assert.That(relational.Locomotion,      Is.EqualTo(original.Locomotion));
            Assert.That(relational.Countermeasures, Is.EqualTo(original.Countermeasures));
            Assert.That(relational.CulturalContext, Is.EqualTo(original.CulturalContext));
            Assert.That(relational.Rating,          Is.EqualTo(original.Rating));
            Assert.That(relational.VoteCount,       Is.EqualTo(original.VoteCount));
        });

        Assert.That(relational.Aliases,          Is.EqualTo(original.Aliases));
        Assert.That(relational.Armament,         Is.EqualTo(original.Armament));
        Assert.That(relational.Sensors,          Is.EqualTo(original.Sensors));
        Assert.That(relational.KnownDeployments, Is.EqualTo(original.KnownDeployments));
        Assert.That(relational.StoryHooks,       Is.EqualTo(original.StoryHooks));
        Assert.That(relational.Tags,             Is.EquivalentTo(original.Tags));
    }

    [Test]
    public async Task RebuildAllAsync_BackfillsFromBlobRows()
    {
        var src = MakeAutomaton("Blob Automaton");
        var id  = Guid.ParseExact(src.Id, "N");

        using (var db = factory.CreateDbContext())
        {
            db.Entities.Add(new Entity
            {
                Id         = id,
                EntityType = "automaton",
                Name       = src.Name,
                Slug       = WorldGraphService.Slugify(src.Name),
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
            var written = await AutomatonMapper.RebuildAllAsync(db);
            Assert.That(written, Is.GreaterThanOrEqualTo(1), "RebuildAllAsync must process at least the seeded automaton entry");
        }

        using (var db = factory.CreateDbContext())
        {
            var got = AutomatonMapper.LoadOne(db, id);
            Assert.That(got, Is.Not.Null);
            Assert.That(got!.Name,            Is.EqualTo(src.Name));
            Assert.That(got.TierAvailability, Is.EqualTo(src.TierAvailability));
            Assert.That(got.Aliases,          Is.EqualTo(src.Aliases));
            Assert.That(got.Armament,         Is.EqualTo(src.Armament));
            Assert.That(got.Sensors,          Is.EqualTo(src.Sensors));
            Assert.That(got.KnownDeployments, Is.EqualTo(src.KnownDeployments));
            Assert.That(got.StoryHooks,       Is.EqualTo(src.StoryHooks));
            Assert.That(got.Tags,             Is.EquivalentTo(src.Tags));
        }
    }
}
