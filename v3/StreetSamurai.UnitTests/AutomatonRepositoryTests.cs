using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class AutomatonRepositoryTests
{
    private string rootDir = "";
    private string repoDir = "";
    private AutomatonRepository repo = null!;

    [SetUp]
    public void Setup()
    {
        rootDir = Path.Combine(Path.GetTempPath(), $"ss_automaton_{Guid.NewGuid():N}");
        repoDir = Path.Combine(rootDir, "engine_data", "automata");
        Directory.CreateDirectory(repoDir);
        var paths = new TestPathProviderWithRoot(rootDir);
        repo = new AutomatonRepository(paths);
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(rootDir)) Directory.Delete(rootDir, true);
    }

    [Test]
    public void Save_And_Retrieve_Automaton()
    {
        var automaton = new AutomatonData
        {
            Name = "KS-4 Knitter",
            Classification = "Spider Platform — Antipersonnel",
            Manufacturer = "ARCTURUS DEFENSE SOLUTIONS",
            TierAvailability = "Tier 4+",
            Tags = ["automaton", "spider", "antipersonnel"]
        };

        repo.Save(automaton);
        repo.Reload();

        var all = repo.GetAll();
        Assert.That(all, Has.Count.EqualTo(1));
        Assert.That(all[0].Name, Is.EqualTo("KS-4 Knitter"));
        Assert.That(all[0].Classification, Is.EqualTo("Spider Platform — Antipersonnel"));
        Assert.That(all[0].Tags, Has.Count.EqualTo(3));
    }

    [Test]
    public void Automaton_HasAllFields()
    {
        var a = new AutomatonData
        {
            Name = "Test Bot",
            Classification = "Test",
            Manufacturer = "Test Corp",
            Description = "A test automaton",
            TierAvailability = "Tier 1",
            Legality = "Licensed",
            AutonomyLevel = "Semi-autonomous",
            Dimensions = "1m x 1m",
            Weight = "50 kg",
            PowerSource = "Lithium cell",
            Locomotion = "Quadruped",
            Armament = ["laser", "missile"],
            Sensors = ["thermal", "acoustic"],
            Countermeasures = "EMP",
            KnownDeployments = ["Test site"],
            StoryHooks = ["A hook"],
            CulturalContext = "Feared",
            Tags = ["automaton", "test"]
        };

        repo.Save(a);
        repo.Reload();

        var loaded = repo.GetByName("Test Bot");
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Armament, Has.Count.EqualTo(2));
        Assert.That(loaded.Sensors, Has.Count.EqualTo(2));
        Assert.That(loaded.Locomotion, Is.EqualTo("Quadruped"));
    }

    [Test]
    public void Automaton_DefaultType_IsAutomaton()
    {
        var a = new AutomatonData { Name = "Default" };
        Assert.That(a.Type, Is.EqualTo("automaton"));
    }

    [Test]
    public void RepoName_IsAutomata()
    {
        Assert.That(repo.RepoName, Is.EqualTo("automata"));
    }
}
