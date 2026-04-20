using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Captures the last system and user prompts — lets tests assert what the
/// writer told the LLM without making a real API call.
/// </summary>
public class CapturingLlmService : ILlmService
{
    public string LastSystem { get; private set; } = "";
    public string LastUser { get; private set; } = "";
    public int CallCount { get; private set; }
    public string Response { get; set; } = "He swung. She ducked. The blade sang.";

    public Task<bool> IsConfiguredAsync() => Task.FromResult(true);

    public Task<string> GenerateAsync(
        string system, string user, double temperature = 0.8,
        int maxTokens = 4096, string? model = null, CancellationToken ct = default)
    {
        LastSystem = system;
        LastUser = user;
        CallCount++;
        return Task.FromResult(Response);
    }
}

[TestFixture]
public class CombatSceneWriterTests
{
    private string rootDir = "";
    private DatabaseService db = null!;
    private TestPathProviderWithRoot paths = null!;
    private CombatSceneWriter svc = null!;
    private CapturingLlmService llm = null!;
    private WeaponryRepository weapons = null!;
    private EquipmentRepository equipment = null!;
    private DistrictRepository districts = null!;

    [SetUp]
    public void Setup()
    {
        (db, paths, rootDir) = TestDatabaseFactory.Create();
        llm = new CapturingLlmService();
        weapons = new WeaponryRepository(paths);
        equipment = new EquipmentRepository(paths);
        districts = new DistrictRepository(paths);
        svc = new CombatSceneWriter(llm, db, weapons, equipment, districts);
    }

    [TearDown]
    public void Cleanup() { if (Directory.Exists(rootDir)) Directory.Delete(rootDir, true); }

    [Test]
    public async Task WriteCombatScene_EmptyRequest_GeneratesBeats()
    {
        var request = new CombatSceneRequest { NumExchanges = 2 };
        var result = await svc.WriteCombatSceneAsync(request);

        Assert.That(result.Beats, Has.Count.EqualTo(2));
        Assert.That(result.FullText, Does.Contain("He swung"));
    }

    [Test]
    public async Task WriteCombatScene_EmitsActionProseRules()
    {
        var request = new CombatSceneRequest { NumExchanges = 1 };
        await svc.WriteCombatSceneAsync(request);

        Assert.That(llm.LastSystem, Does.Contain("ACTION PROSE"));
        Assert.That(llm.LastSystem, Does.Contain("Verbs lead"));
        Assert.That(llm.LastSystem, Does.Contain("Damage persists"));
    }

    [Test]
    public async Task WriteCombatScene_IncludesCombatantWeapon()
    {
        var charRepo = new CharacterRepository(paths);
        charRepo.Save(new CharacterData
        {
            Name = "Kyle",
            Belongings = new CharacterBelongings { PrimaryWeapon = "Noctis .38" }
        });
        weapons.Save(new WeaponryData
        {
            Name = "Noctis .38",
            Category = "revolver",
            TacticalUse = "Close-quarters stopping power, no electronics to jam."
        });
        db.Reload();

        var request = new CombatSceneRequest
        {
            NumExchanges = 1,
            Sides = [new CombatSide { Label = "runner", Combatants = ["Kyle"] }]
        };
        await svc.WriteCombatSceneAsync(request);

        Assert.That(llm.LastSystem, Does.Contain("Noctis .38"));
        Assert.That(llm.LastSystem, Does.Contain("revolver"));
        Assert.That(llm.LastSystem, Does.Contain("stopping power"));
    }

    [Test]
    public async Task WriteCombatScene_IncludesCyberwareFunctionalAndDamaged()
    {
        var charRepo = new CharacterRepository(paths);
        charRepo.Save(new CharacterData
        {
            Name = "Sable",
            CyberwareInventory =
            [
                new CyberwareEntry { Name = "Smartlink", BodyLocation = "cortex", Condition = "functional" },
                new CyberwareEntry { Name = "Reflex Booster", BodyLocation = "spine", Condition = "damaged" },
            ]
        });
        db.Reload();

        var request = new CombatSceneRequest
        {
            NumExchanges = 1,
            Sides = [new CombatSide { Label = "operator", Combatants = ["Sable"] }]
        };
        await svc.WriteCombatSceneAsync(request);

        Assert.That(llm.LastSystem, Does.Contain("Smartlink"));
        Assert.That(llm.LastSystem, Does.Contain("Damaged chrome"));
        Assert.That(llm.LastSystem, Does.Contain("Reflex Booster"));
    }

    [Test]
    public async Task WriteCombatScene_DeadCharacter_AddsHardConstraint()
    {
        var charRepo = new CharacterRepository(paths);
        charRepo.Save(new CharacterData { Name = "Ghost", Status = "dead" });
        db.Reload();

        var request = new CombatSceneRequest
        {
            NumExchanges = 1,
            Sides = [new CombatSide { Label = "revenant", Combatants = ["Ghost"] }]
        };
        await svc.WriteCombatSceneAsync(request);

        Assert.That(llm.LastSystem, Does.Contain("HARD CONSTRAINT"));
        Assert.That(llm.LastSystem, Does.Contain("dead"));
    }

    [Test]
    public async Task WriteCombatScene_BattlefieldDistrict_IncludesTerrainAndAtmosphere()
    {
        districts.Save(new DistrictData
        {
            Name = "The Shelf",
            Description = "Vertical slum strapped to an elevated highway.",
            Atmosphere = new AtmosphereData
            {
                Sights = ["neon signage", "laundry lines"],
                Sounds = ["maglev hum"],
                Smells = ["ozone"]
            },
            Dangers = ["weak railings", "dropping from gantries"]
        });

        var request = new CombatSceneRequest
        {
            NumExchanges = 1,
            BattlefieldLocation = "The Shelf",
            Environment = "raining sideways"
        };
        await svc.WriteCombatSceneAsync(request);

        Assert.That(llm.LastSystem, Does.Contain("The Shelf"));
        Assert.That(llm.LastSystem, Does.Contain("neon signage"));
        Assert.That(llm.LastSystem, Does.Contain("weak railings"));
        Assert.That(llm.LastSystem, Does.Contain("raining sideways"));
    }

    [Test]
    public async Task WriteCombatScene_MultipleSides_AlternatesInitiative()
    {
        var request = new CombatSceneRequest
        {
            NumExchanges = 4,
            Sides =
            [
                new CombatSide { Label = "runners" },
                new CombatSide { Label = "security" },
            ]
        };

        var progressEvents = new List<CombatBeatProgress>();
        svc.OnBeatProgress += progressEvents.Add;

        var result = await svc.WriteCombatSceneAsync(request);

        Assert.That(result.Beats[0].ActingSide, Is.EqualTo("runners"));
        Assert.That(result.Beats[1].ActingSide, Is.EqualTo("security"));
        Assert.That(result.Beats[2].ActingSide, Is.EqualTo("runners"));
        Assert.That(result.Beats[3].ActingSide, Is.EqualTo("security"));
        Assert.That(progressEvents, Has.Count.EqualTo(4));
    }

    [Test]
    public async Task WriteCombatScene_ToneBrutal_UsesBrutalRegister()
    {
        var request = new CombatSceneRequest { NumExchanges = 1, Tone = CombatTone.Brutal };
        await svc.WriteCombatSceneAsync(request);

        Assert.That(llm.LastSystem, Does.Contain("BRUTAL"));
        Assert.That(llm.LastSystem, Does.Contain("Violence is labor"));
    }

    [Test]
    public async Task WriteCombatScene_ToneDesperate_UsesDesperateRegister()
    {
        var request = new CombatSceneRequest { NumExchanges = 1, Tone = CombatTone.Desperate };
        await svc.WriteCombatSceneAsync(request);

        Assert.That(llm.LastSystem, Does.Contain("DESPERATE"));
        Assert.That(llm.LastSystem, Does.Contain("Fragmented perception"));
    }

    [Test]
    public async Task WriteCombatScene_OpeningBeat_LeadsFirstBeat()
    {
        var request = new CombatSceneRequest
        {
            NumExchanges = 2,
            OpeningBeat = "the door blows inward"
        };
        await svc.WriteCombatSceneAsync(request);

        // The opening beat seeds the first user prompt; by the time the second call
        // happens the scene-so-far already contains beat 1's text, so the directive
        // is only on the first call.
        Assert.That(llm.CallCount, Is.EqualTo(2));
        Assert.That(llm.LastUser, Does.Not.Contain("OPENING MOVE")); // final call, not the first
    }

    [Test]
    public async Task WriteCombatScene_PrecedingContext_CarriedIntoFirstPrompt()
    {
        var request = new CombatSceneRequest
        {
            NumExchanges = 1,
            PrecedingContext = "She stepped through the beaded curtain."
        };
        await svc.WriteCombatSceneAsync(request);

        Assert.That(llm.LastUser, Does.Contain("She stepped through the beaded curtain"));
    }

    [Test]
    public async Task WriteCombatScene_UnnamedCombatants_IncludedAsExtras()
    {
        var request = new CombatSceneRequest
        {
            NumExchanges = 1,
            Sides =
            [
                new CombatSide
                {
                    Label = "mall security",
                    UnnamedCombatants = ["three rentacops with stun batons"],
                    SharedLoadout = "cheap polymer armor, last-gen commlinks",
                }
            ]
        };
        await svc.WriteCombatSceneAsync(request);

        Assert.That(llm.LastSystem, Does.Contain("three rentacops"));
        Assert.That(llm.LastSystem, Does.Contain("cheap polymer armor"));
    }

    [Test]
    public async Task WriteCombatScene_DamageStatePersistsAcrossBeats()
    {
        var request = new CombatSceneRequest
        {
            NumExchanges = 3,
            Sides = [new CombatSide { Label = "runners", InitialPosition = "behind the bar" }],
        };
        var result = await svc.WriteCombatSceneAsync(request);

        Assert.That(result.Beats[0].DamageState, Does.Contain("beat 1"));
        Assert.That(result.Beats[2].DamageState, Does.Contain("beat 1"));
        Assert.That(result.Beats[2].DamageState, Does.Contain("beat 2"));
        Assert.That(result.Beats[2].DamageState, Does.Contain("beat 3"));
    }

    [Test]
    public void CombatSceneRequest_Defaults_AreSensible()
    {
        var r = new CombatSceneRequest();
        Assert.That(r.NumExchanges, Is.EqualTo(4));
        Assert.That(r.Tone, Is.EqualTo(CombatTone.Brutal));
        Assert.That(r.Sides, Is.Empty);
    }

    [Test]
    public void GeneratedCombatScene_FullText_JoinsBeats()
    {
        var scene = new GeneratedCombatScene
        {
            Beats =
            [
                new CombatBeat { Text = "one" },
                new CombatBeat { Text = "two" },
            ]
        };
        Assert.That(scene.FullText, Is.EqualTo("one\n\ntwo"));
    }
}
