using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Interfaces;
using Prose.Core.Models;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.UnitTests;

[TestFixture]
public class DialogueServiceTests
{
    private string rootDir = "";
    private DialogueService svc = null!;
    private DatabaseService db = null!;
    private TestPathProviderWithRoot paths = null!;

    [SetUp]
    public void Setup()
    {
        (db, paths, rootDir) = TestDatabaseFactory.Create();
        svc = new DialogueService(db);
    }

    [TearDown]
    public void Cleanup() { if (Directory.Exists(rootDir)) Directory.Delete(rootDir, true); }

    [Test]
    public void EmptyCharacters_ReturnsEmpty()
    {
        var result = svc.BuildDialogueContext([]);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void UnknownCharacter_ReturnsEmpty()
    {
        var result = svc.BuildDialogueContext(["Nobody"]);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void CharacterWithSpeechPatterns_IncludesCadence()
    {
        var charRepo = new CharacterRepository(paths);
        charRepo.Save(new CharacterData
        {
            Name = "TestChar",
            SpeechPatterns = new SpeechPatterns
            {
                Cadence = "Clipped, military precision",
                Vocabulary = "Technical jargon, acronyms"
            }
        });
        db.Reload();

        var result = svc.BuildDialogueContext(["TestChar"]);
        Assert.That(result, Does.Contain("Clipped, military precision"));
        Assert.That(result, Does.Contain("Technical jargon"));
    }

    [Test]
    public void MultipleCharacters_IncludesVoiceDistinction()
    {
        var charRepo = new CharacterRepository(paths);
        charRepo.Save(new CharacterData
        {
            Name = "Alpha",
            SpeechPatterns = new SpeechPatterns { Cadence = "Fast, nervous" }
        });
        charRepo.Save(new CharacterData
        {
            Name = "Beta",
            SpeechPatterns = new SpeechPatterns { Cadence = "Slow, deliberate" }
        });
        db.Reload();

        var result = svc.BuildDialogueContext(["Alpha", "Beta"]);
        Assert.That(result, Does.Contain("immediately distinct"));
        Assert.That(result, Does.Contain("Fast, nervous"));
        Assert.That(result, Does.Contain("Slow, deliberate"));
    }

    [Test]
    public void CharacterWithAncestry_IncludesCulturalBackground()
    {
        var charRepo = new CharacterRepository(paths);
        charRepo.Save(new CharacterData
        {
            Name = "Kenji",
            SpeechPatterns = new SpeechPatterns { Cadence = "Measured" },
            GeneticAncestry = new Dictionary<string, double> { ["East Asian"] = 45.0, ["Northern European"] = 30.0 }
        });
        db.Reload();

        var result = svc.BuildDialogueContext(["Kenji"]);
        Assert.That(result, Does.Contain("East Asian"));
    }

    [Test]
    public void CharacterAge_YoungGetsSlangNote()
    {
        var charRepo = new CharacterRepository(paths);
        charRepo.Save(new CharacterData
        {
            Name = "Kid",
            Age = 14,
            SpeechPatterns = new SpeechPatterns { Cadence = "Rapid-fire" }
        });
        db.Reload();

        var result = svc.BuildDialogueContext(["Kid"]);
        Assert.That(result, Does.Contain("young"));
    }
}
