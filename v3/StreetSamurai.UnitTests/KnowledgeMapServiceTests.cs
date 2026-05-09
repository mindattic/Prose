using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class KnowledgeMapServiceTests
{
    private KnowledgeMapService svc = null!;
    private string testDir = "";

    [SetUp]
    public void Setup()
    {
        testDir = Path.Combine(Path.GetTempPath(), $"ss_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(testDir, "story_blocks"));
        var paths = new TestPathProviderWithRoot(testDir);
        svc = new KnowledgeMapService(
            paths,
            StreetSamurai.Core.Data.TestDbFactory.For(paths, "knowledge"),
            NullLoggers.For<KnowledgeMapService>());
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
    }

    [Test]
    public void CharacterLearned_TracksKnowledge()
    {
        svc.CharacterLearned("proj1", "Kyle", "Sable has facility files", 3);

        Assert.That(svc.CharacterKnows("proj1", "Kyle", "facility files"), Is.True);
        Assert.That(svc.CharacterKnows("proj1", "Sable", "facility files"), Is.False);
    }

    [Test]
    public void CharacterLearned_NoDuplicates()
    {
        svc.CharacterLearned("proj1", "Kyle", "The sky is blue", 1);
        svc.CharacterLearned("proj1", "Kyle", "The sky is blue", 2);

        var map = svc.GetMap("proj1");
        Assert.That(map.CharacterKnowledge["Kyle"], Has.Count.EqualTo(1));
    }

    [Test]
    public void ReaderLearned_TracksReaderKnowledge()
    {
        svc.ReaderLearned("proj1", "Sable is hiding something", 2);

        var map = svc.GetMap("proj1");
        Assert.That(map.ReaderKnowledge, Has.Count.EqualTo(1));
        Assert.That(map.ReaderKnowledge[0].Fact, Is.EqualTo("Sable is hiding something"));
    }

    [Test]
    public void GetDramaticIrony_FindsReaderOnlyKnowledge()
    {
        svc.ReaderLearned("proj1", "Sable has the facility files", 2);
        svc.ReaderLearned("proj1", "The Shelf is being demolished", 3);
        svc.CharacterLearned("proj1", "Kyle", "The Shelf is being demolished", 3);

        var irony = svc.GetDramaticIrony("proj1", "Kyle");
        Assert.That(irony, Has.Count.EqualTo(1));
        Assert.That(irony[0], Does.Contain("facility files"));
    }

    [Test]
    public void AddSecret_TracksUnrevealed()
    {
        svc.AddSecret("proj1", "Sable was born in the facility", "Sable");

        var secrets = svc.GetUnrevealedSecrets("proj1");
        Assert.That(secrets, Has.Count.EqualTo(1));
        Assert.That(secrets[0].KnownBy, Is.EqualTo("Sable"));
        Assert.That(secrets[0].Revealed, Is.False);
    }

    [Test]
    public void RevealSecret_MarksAsRevealed()
    {
        svc.AddSecret("proj1", "Sable was born in the facility", "Sable");
        svc.RevealSecret("proj1", "Sable was born in the facility", "Kyle", 5);

        Assert.That(svc.GetUnrevealedSecrets("proj1"), Is.Empty);
        Assert.That(svc.CharacterKnows("proj1", "Kyle", "facility"), Is.True);
    }

    [Test]
    public void RevealSecret_ToReader()
    {
        svc.AddSecret("proj1", "The hardware is a tracker", "Axiom");
        svc.RevealSecret("proj1", "The hardware is a tracker", "reader", 7);

        var map = svc.GetMap("proj1");
        Assert.That(map.ReaderKnowledge.Any(k => k.Fact.Contains("tracker")));
    }

    [Test]
    public void BuildPovConstraints_IncludesKnowledge()
    {
        svc.CharacterLearned("proj1", "Kyle", "Mrs Chen is in danger", 1);
        svc.ReaderLearned("proj1", "Sable sent the assassin", 2);

        var constraints = svc.BuildPovConstraints("proj1", "Kyle");
        Assert.That(constraints, Does.Contain("KYLE KNOWS"));
        Assert.That(constraints, Does.Contain("Mrs Chen is in danger"));
        Assert.That(constraints, Does.Contain("DRAMATIC IRONY"));
        Assert.That(constraints, Does.Contain("Sable sent the assassin"));
        Assert.That(constraints, Does.Contain("DO NOT have Kyle act on"));
    }

    [Test]
    public void BuildPovConstraints_IncludesSecrets()
    {
        svc.AddSecret("proj1", "Sable knows Kyle's real identity", "Sable");

        var constraints = svc.BuildPovConstraints("proj1", "Kyle");
        Assert.That(constraints, Does.Contain("HIDDEN INFORMATION"));
        Assert.That(constraints, Does.Contain("Sable is hiding"));
    }

    [Test]
    public void BuildPovConstraints_Empty_ReturnsEmpty()
    {
        Assert.That(svc.BuildPovConstraints("proj1", "Nobody"), Is.Empty);
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

        svc.SyncFromState("proj1", state, events, 3);

        // Kyle participated — knows
        Assert.That(svc.CharacterKnows("proj1", "Kyle", "katana"), Is.True);
        // Sable was present — witnessed
        Assert.That(svc.CharacterKnows("proj1", "Sable", "katana"), Is.True);
        // Reader always learns
        var map = svc.GetMap("proj1");
        Assert.That(map.ReaderKnowledge.Any(k => k.Fact.Contains("katana")));
    }

    [Test]
    public void PersistsToDb()
    {
        // Seed a Chapter row so SaveToDb actually persists.
        var paths = new TestPathProviderWithRoot(testDir);
        var dbFactory = StreetSamurai.Core.Data.TestDbFactory.For(paths, "knowledge");
        var chapterId = Guid.NewGuid();
        using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(new StreetSamurai.Core.Data.Entities.Entity
            {
                Id = chapterId,
                EntityType = "chapter",
                Name = "PersistsToDb test",
                Slug = $"persists-knowledge-{chapterId:N}",
                Status = "canon",
            });
            db.Chapters.Add(new StreetSamurai.Core.Data.Entities.Chapter
            {
                Id = chapterId,
                Title = "PersistsToDb test",
            });
            db.SaveChanges();
        }

        var svcLocal = new KnowledgeMapService(paths, dbFactory, NullLoggers.For<KnowledgeMapService>());
        svcLocal.CharacterLearned(chapterId.ToString("N"), "Kyle", "test fact", 1);

        var svcLocal2 = new KnowledgeMapService(paths, dbFactory, NullLoggers.For<KnowledgeMapService>());
        Assert.That(svcLocal2.CharacterKnows(chapterId.ToString("N"), "Kyle", "test fact"), Is.True);
    }

    [Test]
    public void Clear_RemovesEverything()
    {
        svc.CharacterLearned("proj1", "Kyle", "something", 1);
        svc.AddSecret("proj1", "a secret", "Sable");
        svc.Clear("proj1");

        Assert.That(svc.CharacterKnows("proj1", "Kyle", "something"), Is.False);
        Assert.That(svc.GetUnrevealedSecrets("proj1"), Is.Empty);
    }
}
