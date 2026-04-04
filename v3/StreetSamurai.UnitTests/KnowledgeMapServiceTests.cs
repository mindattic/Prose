using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class KnowledgeMapServiceTests
{
    private KnowledgeMapService _svc = null!;
    private string _testDir = "";

    [SetUp]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"ss_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_testDir, "story_blocks"));
        _svc = new KnowledgeMapService(new TestPathProviderWithRoot(_testDir));
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
    }

    [Test]
    public void CharacterLearned_TracksKnowledge()
    {
        _svc.CharacterLearned("proj1", "Kyle", "Sable has facility files", 3);

        Assert.That(_svc.CharacterKnows("proj1", "Kyle", "facility files"), Is.True);
        Assert.That(_svc.CharacterKnows("proj1", "Sable", "facility files"), Is.False);
    }

    [Test]
    public void CharacterLearned_NoDuplicates()
    {
        _svc.CharacterLearned("proj1", "Kyle", "The sky is blue", 1);
        _svc.CharacterLearned("proj1", "Kyle", "The sky is blue", 2);

        var map = _svc.GetMap("proj1");
        Assert.That(map.CharacterKnowledge["Kyle"], Has.Count.EqualTo(1));
    }

    [Test]
    public void ReaderLearned_TracksReaderKnowledge()
    {
        _svc.ReaderLearned("proj1", "Sable is hiding something", 2);

        var map = _svc.GetMap("proj1");
        Assert.That(map.ReaderKnowledge, Has.Count.EqualTo(1));
        Assert.That(map.ReaderKnowledge[0].Fact, Is.EqualTo("Sable is hiding something"));
    }

    [Test]
    public void GetDramaticIrony_FindsReaderOnlyKnowledge()
    {
        _svc.ReaderLearned("proj1", "Sable has the facility files", 2);
        _svc.ReaderLearned("proj1", "The Shelf is being demolished", 3);
        _svc.CharacterLearned("proj1", "Kyle", "The Shelf is being demolished", 3);

        var irony = _svc.GetDramaticIrony("proj1", "Kyle");
        Assert.That(irony, Has.Count.EqualTo(1));
        Assert.That(irony[0], Does.Contain("facility files"));
    }

    [Test]
    public void AddSecret_TracksUnrevealed()
    {
        _svc.AddSecret("proj1", "Sable was born in the facility", "Sable");

        var secrets = _svc.GetUnrevealedSecrets("proj1");
        Assert.That(secrets, Has.Count.EqualTo(1));
        Assert.That(secrets[0].KnownBy, Is.EqualTo("Sable"));
        Assert.That(secrets[0].Revealed, Is.False);
    }

    [Test]
    public void RevealSecret_MarksAsRevealed()
    {
        _svc.AddSecret("proj1", "Sable was born in the facility", "Sable");
        _svc.RevealSecret("proj1", "Sable was born in the facility", "Kyle", 5);

        Assert.That(_svc.GetUnrevealedSecrets("proj1"), Is.Empty);
        Assert.That(_svc.CharacterKnows("proj1", "Kyle", "facility"), Is.True);
    }

    [Test]
    public void RevealSecret_ToReader()
    {
        _svc.AddSecret("proj1", "The hardware is a tracker", "Axiom");
        _svc.RevealSecret("proj1", "The hardware is a tracker", "reader", 7);

        var map = _svc.GetMap("proj1");
        Assert.That(map.ReaderKnowledge.Any(k => k.Fact.Contains("tracker")));
    }

    [Test]
    public void BuildPovConstraints_IncludesKnowledge()
    {
        _svc.CharacterLearned("proj1", "Kyle", "Mrs Chen is in danger", 1);
        _svc.ReaderLearned("proj1", "Sable sent the assassin", 2);

        var constraints = _svc.BuildPovConstraints("proj1", "Kyle");
        Assert.That(constraints, Does.Contain("KYLE KNOWS"));
        Assert.That(constraints, Does.Contain("Mrs Chen is in danger"));
        Assert.That(constraints, Does.Contain("DRAMATIC IRONY"));
        Assert.That(constraints, Does.Contain("Sable sent the assassin"));
        Assert.That(constraints, Does.Contain("DO NOT have Kyle act on"));
    }

    [Test]
    public void BuildPovConstraints_IncludesSecrets()
    {
        _svc.AddSecret("proj1", "Sable knows Kyle's real identity", "Sable");

        var constraints = _svc.BuildPovConstraints("proj1", "Kyle");
        Assert.That(constraints, Does.Contain("HIDDEN INFORMATION"));
        Assert.That(constraints, Does.Contain("Sable is hiding"));
    }

    [Test]
    public void BuildPovConstraints_Empty_ReturnsEmpty()
    {
        Assert.That(_svc.BuildPovConstraints("proj1", "Nobody"), Is.Empty);
    }

    [Test]
    public void SyncFromState_DistributesKnowledge()
    {
        var state = new StoryState
        {
            CurrentLocation = "The Shelf",
            Characters = new()
            {
                ["Kyle"] = new CharacterState { Location = "The Shelf" },
                ["Sable"] = new CharacterState { Location = "The Shelf" },
            }
        };

        var events = new List<StoryEvent>
        {
            new() { Id = "e1", BeatIndex = 3, Summary = "Kyle drew his katana", Participants = ["Kyle"] },
        };

        _svc.SyncFromState("proj1", state, events, 3);

        // Kyle participated — knows
        Assert.That(_svc.CharacterKnows("proj1", "Kyle", "katana"), Is.True);
        // Sable was present — witnessed
        Assert.That(_svc.CharacterKnows("proj1", "Sable", "katana"), Is.True);
        // Reader always learns
        var map = _svc.GetMap("proj1");
        Assert.That(map.ReaderKnowledge.Any(k => k.Fact.Contains("katana")));
    }

    [Test]
    public void PersistsToDisk()
    {
        _svc.CharacterLearned("proj1", "Kyle", "test fact", 1);

        var svc2 = new KnowledgeMapService(new TestPathProviderWithRoot(_testDir));
        Assert.That(svc2.CharacterKnows("proj1", "Kyle", "test fact"), Is.True);
    }

    [Test]
    public void Clear_RemovesEverything()
    {
        _svc.CharacterLearned("proj1", "Kyle", "something", 1);
        _svc.AddSecret("proj1", "a secret", "Sable");
        _svc.Clear("proj1");

        Assert.That(_svc.CharacterKnows("proj1", "Kyle", "something"), Is.False);
        Assert.That(_svc.GetUnrevealedSecrets("proj1"), Is.Empty);
    }
}
