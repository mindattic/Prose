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
    public void EmptyCharacters_ReturnsEmpty()
    {
        var result = svc.BuildConstraints(["Nobody"]);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void DeadCharacter_IncludesHardConstraint()
    {
        var character = new CharacterData { Name = "Kyle", Status = "dead" };
        repo.Save(character);
        repo.Reload();

        var result = svc.BuildConstraints(["Kyle"]);
        Assert.That(result, Does.Contain("dead"));
        Assert.That(result, Does.Contain("HARD CONSTRAINT"));
    }

    [Test]
    public void CharacterWithCyberware_ListsChrome()
    {
        var character = new CharacterData
        {
            Name = "Sable",
            CyberwareInventory = [new CyberwareEntry { Name = "Thermal Eyes", BodyLocation = "eyes", Condition = "functional" }]
        };
        repo.Save(character);
        repo.Reload();

        var result = svc.BuildConstraints(["Sable"]);
        Assert.That(result, Does.Contain("Thermal Eyes"));
    }

    [Test]
    public void CharacterWithWeapon_ListsGear()
    {
        var character = new CharacterData
        {
            Name = "Vex",
            Belongings = new CharacterBelongings { PrimaryWeapon = "Hearthstone HM-7" }
        };
        repo.Save(character);
        repo.Reload();

        var result = svc.BuildConstraints(["Vex"]);
        Assert.That(result, Does.Contain("Hearthstone HM-7"));
    }
}

[TestFixture]
public class NarrativeSummaryServiceTests
{
    [Test]
    public void InitialChain_IsEmpty()
    {
        var svc = new NarrativeSummaryService(new FakeLlmService());
        Assert.That(svc.GetSummaryChain(), Is.Empty);
        Assert.That(svc.SceneCount, Is.EqualTo(0));
    }

    [Test]
    public async Task SummarizeScene_AddsToChain()
    {
        var svc = new NarrativeSummaryService(new FakeLlmService());
        await svc.SummarizeSceneAsync("Kyle walked into the bar and ordered a drink.");

        Assert.That(svc.SceneCount, Is.EqualTo(1));
        Assert.That(svc.GetSummaryChain(), Does.Contain("Scene 1:"));
    }

    [Test]
    public void Reset_ClearsChain()
    {
        var svc = new NarrativeSummaryService(new FakeLlmService());
        svc.SummarizeSceneAsync("Something happened.").Wait();
        svc.Reset();

        Assert.That(svc.SceneCount, Is.EqualTo(0));
        Assert.That(svc.GetSummaryChain(), Is.Empty);
    }

    [Test]
    public async Task SummarizeScene_NullText_LlmNotCalled_SceneCountStaysZero()
    {
        var llm = new CountingLlm();
        var svc = new NarrativeSummaryService(llm);
        await svc.SummarizeSceneAsync(null!);
        Assert.That(llm.CallCount, Is.EqualTo(0));
        Assert.That(svc.SceneCount, Is.EqualTo(0));
    }

    [Test]
    public async Task SummarizeScene_WhitespaceText_LlmNotCalled_SceneCountStaysZero()
    {
        var llm = new CountingLlm();
        var svc = new NarrativeSummaryService(llm);
        await svc.SummarizeSceneAsync("   ");
        Assert.That(llm.CallCount, Is.EqualTo(0));
        Assert.That(svc.SceneCount, Is.EqualTo(0));
    }

    [Test]
    public async Task After10Calls_SceneCountIs10_ChainShowsScene1Through10()
    {
        var svc = new NarrativeSummaryService(new FixedSummaryLlm("summary"));
        for (int i = 0; i < 10; i++)
            await svc.SummarizeSceneAsync($"Scene text {i + 1}.");
        Assert.That(svc.SceneCount, Is.EqualTo(10));
        var chain = svc.GetSummaryChain();
        Assert.That(chain, Does.Contain("Scene 1:"));
        Assert.That(chain, Does.Contain("Scene 10:"));
    }

    [Test]
    public async Task After11Calls_SceneCountIs11_ChainShowsOnly10Numbered1To10()
    {
        var svc = new NarrativeSummaryService(new FixedSummaryLlm("summary"));
        for (int i = 0; i < 11; i++)
            await svc.SummarizeSceneAsync($"Scene text {i + 1}.");
        Assert.That(svc.SceneCount, Is.EqualTo(11));
        var chain = svc.GetSummaryChain();
        Assert.That(chain, Does.Contain("Scene 1:"));
        Assert.That(chain, Does.Contain("Scene 10:"));
        Assert.That(chain, Does.Not.Contain("Scene 11:"));
    }

    [Test]
    public async Task GetSummaryChain_NonEmpty_ContainsStoryHeader()
    {
        var svc = new NarrativeSummaryService(new FixedSummaryLlm("summary"));
        await svc.SummarizeSceneAsync("A scene happened.");
        Assert.That(svc.GetSummaryChain(), Does.Contain("STORY SO FAR"));
    }

    [Test]
    public async Task SummarizeScene_LlmResponseWithWhitespace_IsTrimmed()
    {
        var svc = new NarrativeSummaryService(new FixedSummaryLlm("  trimmed summary  "));
        await svc.SummarizeSceneAsync("Some scene text.");
        var chain = svc.GetSummaryChain();
        Assert.That(chain, Does.Contain("trimmed summary"));
        Assert.That(chain, Does.Not.Contain("  trimmed summary  "));
    }

    [Test]
    public async Task Reset_ThenSummarize_StartsFromSceneOne()
    {
        var svc = new NarrativeSummaryService(new FixedSummaryLlm("summary"));
        for (int i = 0; i < 3; i++)
            await svc.SummarizeSceneAsync($"Scene {i + 1}.");
        svc.Reset();
        await svc.SummarizeSceneAsync("Fresh start.");
        Assert.That(svc.SceneCount, Is.EqualTo(1));
        Assert.That(svc.GetSummaryChain(), Does.Contain("Scene 1:"));
    }

    class CountingLlm : ILlmService
    {
        public int CallCount { get; private set; }
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
        { CallCount++; return Task.FromResult("summary"); }
    }

    class FixedSummaryLlm(string response) : ILlmService
    {
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
            => Task.FromResult(response);
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
        // Use null-safe approach — SceneContextBuilder only needs docRepo and districtRepo for ambient context
        var svc = new SceneContextBuilder(null!, null!, docRepo, districtRepo);

        var result = svc.BuildAmbientContext("Shelf", "night", "raining");
        Assert.That(result, Is.Not.Null);

        Directory.Delete(rootDir, true);
    }
}
