using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Integration tests verifying the story layer services work together correctly.
/// These test the closed-loop pipeline: state → events → knowledge synced together.
/// </summary>
[TestFixture]
public class IntegrationTests
{
    private string _testDir = "";
    private StoryStateService _storyState = null!;
    private EventLogService _eventLog = null!;
    private KnowledgeMapService _knowledge = null!;

    [SetUp]
    public void Setup()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"ss_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_testDir, "story_blocks"));
        var paths = new TestPathProviderWithRoot(_testDir);
        var llm = new FakeLlmService();

        _storyState = new StoryStateService(llm);
        _eventLog = new EventLogService(llm, paths);
        _knowledge = new KnowledgeMapService(paths);
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir)) Directory.Delete(_testDir, true);
    }

    [Test]
    public void StoryState_And_KnowledgeMap_SyncTogether()
    {
        var pid = "integration1";

        // Setup: Kyle and Sable at the Shelf
        _storyState.InitializeCharacter(pid, "Kyle", "The Shelf", ["katana"]);
        _storyState.InitializeCharacter(pid, "Sable", "The Shelf");

        // Event: Kyle reveals something to Sable
        _eventLog.AddEvent(pid, new StoryEvent
        {
            Id = "e1", BeatIndex = 1,
            Summary = "Kyle told Sable about the facility",
            Participants = ["Kyle", "Sable"],
            Location = "The Shelf",
            Type = "revelation",
        });

        // Sync knowledge from state + events
        var state = _storyState.GetState(pid);
        state.CurrentLocation = "The Shelf";
        var events = _eventLog.GetEvents(pid);
        _knowledge.SyncFromState(pid, state, events, 1);

        // Both Kyle and Sable should know about the facility
        Assert.That(_knowledge.CharacterKnows(pid, "Kyle", "facility"), Is.True);
        Assert.That(_knowledge.CharacterKnows(pid, "Sable", "facility"), Is.True);

        // Reader should know too
        var map = _knowledge.GetMap(pid);
        Assert.That(map.ReaderKnowledge.Any(k => k.Fact.Contains("facility")));
    }

    [Test]
    public void DramaticIrony_WorksAcrossServices()
    {
        var pid = "integration2";

        _storyState.InitializeCharacter(pid, "Kyle", "The Shelf");
        _storyState.InitializeCharacter(pid, "Sable", "The Circuit");

        // Secret known only to Sable
        _knowledge.AddSecret(pid, "Sable has Kyle's facility records", "Sable");

        // Reader learns this but Kyle doesn't
        _knowledge.ReaderLearned(pid, "Sable has Kyle's facility records", 2, "narration");

        // Check dramatic irony
        var irony = _knowledge.GetDramaticIrony(pid, "Kyle");
        Assert.That(irony, Has.Count.EqualTo(1));
        Assert.That(irony[0], Does.Contain("facility records"));

        // POV constraints should warn the LLM
        var constraints = _knowledge.BuildPovConstraints(pid, "Kyle");
        Assert.That(constraints, Does.Contain("DRAMATIC IRONY"));
        Assert.That(constraints, Does.Contain("DO NOT have Kyle act on"));
    }

    [Test]
    public void DeadCharacter_ConstrainedAcrossServices()
    {
        var pid = "integration3";

        _storyState.InitializeCharacter(pid, "Seo", "Old Harbor");
        _storyState.GetState(pid).Characters["Seo"].Status = "dead";

        // Story state should flag dead characters
        var constraints = _storyState.BuildConstraints(pid);
        Assert.That(constraints, Does.Contain("DO NOT write these characters as alive"));
        Assert.That(constraints, Does.Contain("Seo"));
    }

    [Test]
    public void EventLog_And_KnowledgeMap_TrackBystanders()
    {
        var pid = "integration4";

        // Three characters in the same location
        _storyState.InitializeCharacter(pid, "Kyle", "The Shelf");
        _storyState.InitializeCharacter(pid, "Sable", "The Shelf");
        _storyState.InitializeCharacter(pid, "Mrs Chen", "The Shelf");

        var state = _storyState.GetState(pid);
        state.CurrentLocation = "The Shelf";

        // Event between Kyle and Sable — Mrs Chen is a bystander
        _eventLog.AddEvent(pid, new StoryEvent
        {
            Id = "e1", BeatIndex = 1,
            Summary = "Kyle accepted Sable's contract",
            Participants = ["Kyle", "Sable"],
            Location = "The Shelf",
        });

        _knowledge.SyncFromState(pid, state, _eventLog.GetEvents(pid), 1);

        // Kyle and Sable participated
        Assert.That(_knowledge.CharacterKnows(pid, "Kyle", "contract"), Is.True);
        Assert.That(_knowledge.CharacterKnows(pid, "Sable", "contract"), Is.True);
        // Mrs Chen witnessed (same location)
        Assert.That(_knowledge.CharacterKnows(pid, "Mrs Chen", "contract"), Is.True);
    }

    [Test]
    public void AbsentCharacter_DoesNotLearnEvents()
    {
        var pid = "integration5";

        _storyState.InitializeCharacter(pid, "Kyle", "The Shelf");
        _storyState.InitializeCharacter(pid, "Pixel", "The Circuit"); // Different location

        var state = _storyState.GetState(pid);
        state.CurrentLocation = "The Shelf";

        _eventLog.AddEvent(pid, new StoryEvent
        {
            Id = "e1", BeatIndex = 1,
            Summary = "Kyle found a hidden door",
            Participants = ["Kyle"],
            Location = "The Shelf",
        });

        _knowledge.SyncFromState(pid, state, _eventLog.GetEvents(pid), 1);

        Assert.That(_knowledge.CharacterKnows(pid, "Kyle", "hidden door"), Is.True);
        Assert.That(_knowledge.CharacterKnows(pid, "Pixel", "hidden door"), Is.False);
    }

    [Test]
    public void FullPipeline_StateConstraintsAccumulate()
    {
        var pid = "pipeline1";

        // Beat 0: Setup
        _storyState.InitializeCharacter(pid, "Kyle", "The Shelf", ["katana", "stabilizers"]);
        _storyState.InitializeCharacter(pid, "Sable", "The Circuit");

        // Beat 1: Kyle moves, gets hurt
        var state = _storyState.GetState(pid);
        state.Characters["Kyle"].Location = "The Circuit";
        state.Characters["Kyle"].EmotionalState = "wary";
        state.Characters["Kyle"].Injuries.Add("cut on forearm");
        state.CurrentLocation = "The Circuit";
        state.TensionLevel = 6;
        state.BeatCount = 1;

        _eventLog.AddEvent(pid, new StoryEvent
        {
            Id = "e1", BeatIndex = 1, Type = "arrival",
            Summary = "Kyle arrived at the Circuit", Participants = ["Kyle"],
        });

        // Beat 2: Kyle meets Sable
        state.Characters["Kyle"].EmotionalState = "guarded";
        state.TensionLevel = 7;
        state.BeatCount = 2;

        _eventLog.AddEvent(pid, new StoryEvent
        {
            Id = "e2", BeatIndex = 2, Type = "dialogue",
            Summary = "Kyle and Sable discussed the contract",
            Participants = ["Kyle", "Sable"],
        });

        _knowledge.SyncFromState(pid, state, _eventLog.GetEvents(pid), 2);

        // Verify constraints capture the full state
        var constraints = _storyState.BuildConstraints(pid);
        Assert.That(constraints, Does.Contain("Kyle"));
        Assert.That(constraints, Does.Contain("The Circuit"));
        Assert.That(constraints, Does.Contain("guarded"));
        Assert.That(constraints, Does.Contain("cut on forearm"));
        Assert.That(constraints, Does.Contain("katana"));
        Assert.That(constraints, Does.Contain("7/10"));

        // Event history is correct
        var kyleEvents = _eventLog.GetEventsForCharacter(pid, "Kyle");
        Assert.That(kyleEvents, Has.Count.EqualTo(2));

        var lastMeeting = _eventLog.GetLastInteraction(pid, "Kyle", "Sable");
        Assert.That(lastMeeting, Is.Not.Null);
        Assert.That(lastMeeting!.BeatIndex, Is.EqualTo(2));

        // Recent context for LLM
        var recentCtx = _eventLog.BuildRecentContext(pid);
        Assert.That(recentCtx, Does.Contain("arrived"));
        Assert.That(recentCtx, Does.Contain("discussed"));
    }
}
