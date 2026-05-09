using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class EventLogServiceTests
{
    private EventLogService svc = null!;
    private string testDir = "";

    [SetUp]
    public void Setup()
    {
        testDir = Path.Combine(Path.GetTempPath(), $"ss_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(testDir, "story_blocks"));
        var paths = new TestPathProviderWithRoot(testDir);
        svc = new EventLogService(
            new FakeLlmService(),
            paths,
            StreetSamurai.Core.Data.TestDbFactory.For(paths, "events"),
            NullLoggers.For<EventLogService>());
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
    }

    [Test]
    public void GetEvents_EmptyProject_ReturnsEmpty()
    {
        Assert.That(svc.GetEvents("proj1"), Is.Empty);
    }

    [Test]
    public void AddEvent_StoresAndRetrieves()
    {
        svc.AddEvent("proj1", new StoryEvent
        {
            Id = "e1", BeatIndex = 0, Type = "action",
            Summary = "Kyle drew his katana", Participants = ["Kyle"],
            Location = "The Shelf", EmotionalWeight = 6,
        });

        var events = svc.GetEvents("proj1");
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Summary, Is.EqualTo("Kyle drew his katana"));
    }

    [Test]
    public void GetEventsForCharacter_FiltersCorrectly()
    {
        svc.AddEvent("proj1", new StoryEvent { Id = "e1", Summary = "Kyle fights", Participants = ["Kyle"] });
        svc.AddEvent("proj1", new StoryEvent { Id = "e2", Summary = "Sable talks", Participants = ["Sable"] });
        svc.AddEvent("proj1", new StoryEvent { Id = "e3", Summary = "Kyle meets Sable", Participants = ["Kyle", "Sable"] });

        var kyleEvents = svc.GetEventsForCharacter("proj1", "Kyle");
        Assert.That(kyleEvents, Has.Count.EqualTo(2));

        var sableEvents = svc.GetEventsForCharacter("proj1", "Sable");
        Assert.That(sableEvents, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetLastInteraction_FindsMostRecent()
    {
        svc.AddEvent("proj1", new StoryEvent { Id = "e1", BeatIndex = 1, Summary = "First meeting", Participants = ["Kyle", "Sable"] });
        svc.AddEvent("proj1", new StoryEvent { Id = "e2", BeatIndex = 5, Summary = "Argument", Participants = ["Kyle", "Sable"] });

        var last = svc.GetLastInteraction("proj1", "Kyle", "Sable");
        Assert.That(last, Is.Not.Null);
        Assert.That(last!.Summary, Is.EqualTo("Argument"));
        Assert.That(last.BeatIndex, Is.EqualTo(5));
    }

    [Test]
    public void GetEventsByType_Filters()
    {
        svc.AddEvent("proj1", new StoryEvent { Id = "e1", Type = "dialogue", Summary = "talk" });
        svc.AddEvent("proj1", new StoryEvent { Id = "e2", Type = "action", Summary = "fight" });
        svc.AddEvent("proj1", new StoryEvent { Id = "e3", Type = "dialogue", Summary = "more talk" });

        Assert.That(svc.GetEventsByType("proj1", "dialogue"), Has.Count.EqualTo(2));
        Assert.That(svc.GetEventsByType("proj1", "action"), Has.Count.EqualTo(1));
    }

    [Test]
    public void GetEventsByTag_Filters()
    {
        svc.AddEvent("proj1", new StoryEvent { Id = "e1", Tags = ["betrayal", "trust"] });
        svc.AddEvent("proj1", new StoryEvent { Id = "e2", Tags = ["violence"] });

        Assert.That(svc.GetEventsByTag("proj1", "betrayal"), Has.Count.EqualTo(1));
    }

    [Test]
    public void BuildRecentContext_FormatsForLlm()
    {
        svc.AddEvent("proj1", new StoryEvent { Id = "e1", BeatIndex = 0, Summary = "Kyle arrived", Participants = ["Kyle"], Location = "The Shelf" });
        svc.AddEvent("proj1", new StoryEvent { Id = "e2", BeatIndex = 1, Summary = "Sable offered a contract", Participants = ["Sable", "Kyle"] });

        var context = svc.BuildRecentContext("proj1");
        Assert.That(context, Does.Contain("RECENT EVENTS"));
        Assert.That(context, Does.Contain("Kyle arrived"));
        Assert.That(context, Does.Contain("Sable offered a contract"));
    }

    [Test]
    public void BuildCharacterHistory_FormatsForLlm()
    {
        svc.AddEvent("proj1", new StoryEvent { Id = "e1", BeatIndex = 0, Summary = "Drew katana", Participants = ["Kyle"] });
        svc.AddEvent("proj1", new StoryEvent { Id = "e2", BeatIndex = 1, Summary = "Ate noodles", Participants = ["Kyle"] });

        var history = svc.BuildCharacterHistory("proj1", "Kyle");
        Assert.That(history, Does.Contain("KYLE HAS DONE"));
        Assert.That(history, Does.Contain("Drew katana"));
    }

    [Test]
    public void PersistsToDb()
    {
        // Seed a Chapter row so SaveToDb actually persists. The service is a
        // no-op write when no Chapters row exists for the project id.
        var paths = new TestPathProviderWithRoot(testDir);
        var dbFactory = StreetSamurai.Core.Data.TestDbFactory.For(paths, "events");
        var chapterId = Guid.NewGuid();
        using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(new StreetSamurai.Core.Data.Entities.Entity
            {
                Id = chapterId,
                EntityType = "chapter",
                Name = "PersistsToDb test",
                Slug = $"persists-to-db-{chapterId:N}",
                Status = "canon",
            });
            db.Chapters.Add(new StreetSamurai.Core.Data.Entities.Chapter
            {
                Id = chapterId,
                Title = "PersistsToDb test",
            });
            db.SaveChanges();
        }

        // Build an EventLogService bound to the same in-memory DB instance.
        var svcLocal = new EventLogService(
            new FakeLlmService(), paths, dbFactory, NullLoggers.For<EventLogService>());
        svcLocal.AddEvent(chapterId.ToString("N"),
            new StoryEvent { Id = "e1", Summary = "test event" });

        // New service instance, same DbContextFactory → reads from DB.
        var svcLocal2 = new EventLogService(
            new FakeLlmService(), paths, dbFactory, NullLoggers.For<EventLogService>());
        var events = svcLocal2.GetEvents(chapterId.ToString("N"));
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0].Summary, Is.EqualTo("test event"));
    }

    [Test]
    public void Clear_RemovesAllEvents()
    {
        svc.AddEvent("proj1", new StoryEvent { Id = "e1", Summary = "event" });
        svc.Clear("proj1");
        Assert.That(svc.GetEvents("proj1"), Is.Empty);
    }
}
