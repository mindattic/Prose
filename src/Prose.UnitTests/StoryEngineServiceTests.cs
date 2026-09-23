using Prose.Core.Interfaces;
using Prose.Core.Services;
using Prose.Core.Models.Canon;

namespace Prose.UnitTests;

[TestFixture]
public class PacingServiceTests
{
    [Test]
    public void FirstBeat_IsBreathe()
    {
        var pacing = PacingService.GetPacing(0, 5);
        Assert.That(pacing.Mode, Is.EqualTo(PacingService.PaceMode.Breathe));
    }

    [Test]
    public void MiddleBeat_IsFlowOrTighten()
    {
        var pacing = PacingService.GetPacing(2, 5);
        Assert.That(pacing.Mode, Is.EqualTo(PacingService.PaceMode.Tighten).Or.EqualTo(PacingService.PaceMode.Flow));
    }

    [Test]
    public void LastBeat_IsSettle()
    {
        var pacing = PacingService.GetPacing(4, 5);
        Assert.That(pacing.Mode, Is.EqualTo(PacingService.PaceMode.Settle));
    }

    [Test]
    public void FightGoal_OverridesToStrike()
    {
        var pacing = PacingService.GetPacing(0, 5, "fight scene in the alley");
        Assert.That(pacing.Mode, Is.EqualTo(PacingService.PaceMode.Strike));
    }

    [Test]
    public void ExploreGoal_OverridesToBreathe()
    {
        var pacing = PacingService.GetPacing(3, 5, "explore the abandoned building");
        Assert.That(pacing.Mode, Is.EqualTo(PacingService.PaceMode.Breathe));
    }

    [Test]
    public void AftermathGoal_OverridesToSettle()
    {
        var pacing = PacingService.GetPacing(1, 5, "aftermath of the explosion");
        Assert.That(pacing.Mode, Is.EqualTo(PacingService.PaceMode.Settle));
    }

    [Test]
    public void ProseGuidance_IsNotEmpty()
    {
        foreach (var mode in Enum.GetValues<PacingService.PaceMode>())
        {
            var instruction = new PacingInstruction(mode);
            Assert.That(instruction.ProseGuidance, Is.Not.Empty, $"ProseGuidance missing for {mode}");
        }
    }

    [Test]
    public void SingleBeat_IsBreathe()
    {
        var pacing = PacingService.GetPacing(0, 1);
        Assert.That(pacing.Mode, Is.EqualTo(PacingService.PaceMode.Breathe));
    }
}

[TestFixture]
public class ConsequenceServiceTests
{
    private string rootDir = "";
    private ConsequenceService svc = null!;
    private CharacterRepository repo = null!;

    [SetUp]
    public void Setup()
    {
        rootDir = Path.Combine(Path.GetTempPath(), $"ss_consequence_{Guid.NewGuid():N}");
        var charDir = Path.Combine(rootDir, "engine_data", "people");
        Directory.CreateDirectory(charDir);
        var paths = new TestPathProviderWithRoot(rootDir);
        repo = new CharacterRepository(paths);
        svc = new ConsequenceService(repo);
    }

    [TearDown]
    public void Cleanup() { if (Directory.Exists(rootDir)) Directory.Delete(rootDir, true); }

    [Test]
    public async Task EmptyCharacters_ReturnsEmpty()
    {
        var result = await svc.BuildConstraintsAsync(["Nobody"]);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task DeadCharacter_IncludesHardConstraint()
    {
        var character = new CharacterData { Name = "Kyle", Status = "dead" };
        repo.Save(character);
        repo.Reload();

        var result = await svc.BuildConstraintsAsync(["Kyle"]);
        Assert.That(result, Does.Contain("dead"));
        Assert.That(result, Does.Contain("HARD CONSTRAINT"));
    }

    [Test]
    public async Task CharacterWithCyberware_ListsChrome()
    {
        var character = new CharacterData
        {
            Name = "Sable",
            CyberwareInventory = [new CyberwareEntry { Name = "Thermal Eyes", BodyLocation = "eyes", Condition = "functional" }]
        };
        repo.Save(character);
        repo.Reload();

        var result = await svc.BuildConstraintsAsync(["Sable"]);
        Assert.That(result, Does.Contain("Thermal Eyes"));
    }

    [Test]
    public async Task CharacterWithWeapon_ListsGear()
    {
        var character = new CharacterData
        {
            Name = "Vex",
            Belongings = new CharacterBelongings { PrimaryWeapon = "Hearthstone HM-7" }
        };
        repo.Save(character);
        repo.Reload();

        var result = await svc.BuildConstraintsAsync(["Vex"]);
        Assert.That(result, Does.Contain("Hearthstone HM-7"));
    }
}

[TestFixture]
public class SceneContextBuilderTests
{
    [Test]
    public void BuildAmbientContext_WithNoData_DoesNotThrow()
    {
        var rootDir = Path.Combine(Path.GetTempPath(), $"ss_context_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(rootDir, "engine_data", "documents"));
        Directory.CreateDirectory(Path.Combine(rootDir, "engine_data", "places"));
        var paths = new TestPathProviderWithRoot(rootDir);
        var docRepo = new WorldbuildingDocRepository(paths);
        var districtRepo = new DistrictRepository(paths);
        var svc = new SceneContextBuilder(docRepo, districtRepo);

        var result = svc.BuildAmbientContext("Shelf", "night", "raining");
        Assert.That(result, Is.Not.Null);

        Directory.Delete(rootDir, true);
    }
}
