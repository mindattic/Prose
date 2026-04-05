using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class StoryStateServiceTests
{
    private StoryStateService svc = null!;

    [SetUp]
    public void Setup()
    {
        // StoryStateService needs ILlmService for UpdateFromTextAsync, but state
        // management methods work without it
        svc = new StoryStateService(new FakeLlmService(), NullLoggers.For<StoryStateService>());
    }

    [Test]
    public void GetState_CreatesNewIfMissing()
    {
        var state = svc.GetState("proj1");
        Assert.That(state, Is.Not.Null);
        Assert.That(state.ProjectId, Is.EqualTo("proj1"));
        Assert.That(state.Characters, Is.Empty);
    }

    [Test]
    public void GetState_ReturnsSameInstance()
    {
        var a = svc.GetState("proj1");
        var b = svc.GetState("proj1");
        Assert.That(a, Is.SameAs(b));
    }

    [Test]
    public void InitializeCharacter_SetsLocationAndInventory()
    {
        svc.InitializeCharacter("proj1", "Kyle", "The Shelf", ["katana", "stabilizers"]);
        var state = svc.GetState("proj1");

        Assert.That(state.Characters.ContainsKey("Kyle"));
        Assert.That(state.Characters["Kyle"].Location, Is.EqualTo("The Shelf"));
        Assert.That(state.Characters["Kyle"].Inventory, Does.Contain("katana"));
        Assert.That(state.Characters["Kyle"].Inventory, Does.Contain("stabilizers"));
    }

    [Test]
    public void InitializeCharacter_DoesNotOverwriteExisting()
    {
        svc.InitializeCharacter("proj1", "Kyle", "The Shelf");
        svc.InitializeCharacter("proj1", "Kyle", "The Circuit");

        Assert.That(svc.GetState("proj1").Characters["Kyle"].Location, Is.EqualTo("The Circuit"));
    }

    [Test]
    public void BuildConstraints_Empty_ReturnsEmpty()
    {
        var constraints = svc.BuildConstraints("empty_project");
        Assert.That(constraints, Is.Empty);
    }

    [Test]
    public void BuildConstraints_IncludesCharacterState()
    {
        svc.InitializeCharacter("proj1", "Kyle", "The Shelf", ["katana"]);
        var state = svc.GetState("proj1");
        state.Characters["Kyle"].EmotionalState = "tense";
        state.Characters["Kyle"].Injuries.Add("left shoulder");
        state.CurrentLocation = "The Shelf";
        state.TensionLevel = 7;

        var constraints = svc.BuildConstraints("proj1");

        Assert.That(constraints, Does.Contain("Kyle"));
        Assert.That(constraints, Does.Contain("The Shelf"));
        Assert.That(constraints, Does.Contain("tense"));
        Assert.That(constraints, Does.Contain("left shoulder"));
        Assert.That(constraints, Does.Contain("katana"));
        Assert.That(constraints, Does.Contain("7/10"));
    }

    [Test]
    public void BuildConstraints_FlagsDeadCharacters()
    {
        svc.InitializeCharacter("proj1", "Seo");
        svc.GetState("proj1").Characters["Seo"].Status = "dead";

        var constraints = svc.BuildConstraints("proj1");
        Assert.That(constraints, Does.Contain("DO NOT write these characters as alive"));
        Assert.That(constraints, Does.Contain("Seo"));
    }

    [Test]
    public void BuildConstraints_FlagsAbsentCharacters()
    {
        svc.InitializeCharacter("proj1", "Kyle", "The Shelf");
        svc.InitializeCharacter("proj1", "Sable", "The Circuit");
        svc.GetState("proj1").CurrentLocation = "The Shelf";

        var constraints = svc.BuildConstraints("proj1");
        Assert.That(constraints, Does.Contain("NOT PRESENT"));
        Assert.That(constraints, Does.Contain("Sable"));
    }

    [Test]
    public void Reset_ClearsState()
    {
        svc.InitializeCharacter("proj1", "Kyle", "The Shelf");
        svc.Reset("proj1");

        var state = svc.GetState("proj1");
        Assert.That(state.Characters, Is.Empty);
        Assert.That(state.BeatCount, Is.EqualTo(0));
    }
}
