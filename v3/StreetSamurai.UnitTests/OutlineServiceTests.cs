using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

[TestFixture]
public class OutlineServiceTests
{
    private string testDir = "";

    [SetUp]
    public void Setup()
    {
        testDir = Path.Combine(Path.GetTempPath(), $"ss_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(testDir, "story_blocks"));
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
    }

    [Test]
    public void GetNextBeat_ReturnsFirstUnwritten()
    {
        var outline = MakeTestOutline();
        var paths = new TestPathProviderWithRoot(testDir);
        var svc = new OutlineService(new FakeLlmService(), new TestDatabaseService(), paths, StreetSamurai.Core.Data.TestDbFactory.For(paths, "outline"), NullLoggers.For<OutlineService>());

        var next = svc.GetNextBeat(outline, -1);
        Assert.That(next, Is.Not.Null);
        Assert.That(next!.BeatIndex, Is.EqualTo(0));
    }

    [Test]
    public void GetNextBeat_SkipsWrittenBeats()
    {
        var outline = MakeTestOutline();
        outline.Acts[0].Beats[0].Written = true;

        var paths = new TestPathProviderWithRoot(testDir);
        var svc = new OutlineService(new FakeLlmService(), new TestDatabaseService(), paths, StreetSamurai.Core.Data.TestDbFactory.For(paths, "outline"), NullLoggers.For<OutlineService>());

        var next = svc.GetNextBeat(outline, -1);
        Assert.That(next, Is.Not.Null);
        Assert.That(next!.BeatIndex, Is.EqualTo(1));
    }

    [Test]
    public void GetNextBeat_AllWritten_ReturnsNull()
    {
        var outline = MakeTestOutline();
        foreach (var act in outline.Acts)
            foreach (var beat in act.Beats)
                beat.Written = true;

        var paths = new TestPathProviderWithRoot(testDir);
        var svc = new OutlineService(new FakeLlmService(), new TestDatabaseService(), paths, StreetSamurai.Core.Data.TestDbFactory.For(paths, "outline"), NullLoggers.For<OutlineService>());

        Assert.That(svc.GetNextBeat(outline, -1), Is.Null);
    }

    [Test]
    public void MarkBeatWritten_SetsFlag()
    {
        var outline = MakeTestOutline();
        var paths = new TestPathProviderWithRoot(testDir);
        var svc = new OutlineService(new FakeLlmService(), new TestDatabaseService(), paths, StreetSamurai.Core.Data.TestDbFactory.For(paths, "outline"), NullLoggers.For<OutlineService>());

        svc.MarkBeatWritten(outline, 0);
        Assert.That(outline.Acts[0].Beats[0].Written, Is.True);
        Assert.That(outline.Acts[0].Beats[1].Written, Is.False);
    }

    [Test]
    public void BuildBeatContext_IncludesArcInfo()
    {
        var outline = MakeTestOutline();
        var paths = new TestPathProviderWithRoot(testDir);
        var svc = new OutlineService(new FakeLlmService(), new TestDatabaseService(), paths, StreetSamurai.Core.Data.TestDbFactory.For(paths, "outline"), NullLoggers.For<OutlineService>());

        var ctx = svc.BuildBeatContext(outline, 0);
        Assert.That(ctx, Does.Contain("STORY OUTLINE CONTEXT"));
        Assert.That(ctx, Does.Contain("Test Story"));
        Assert.That(ctx, Does.Contain("Act 1"));
        Assert.That(ctx, Does.Contain("Introduce Kyle"));
        Assert.That(ctx, Does.Contain("Tension Target"));
    }

    [Test]
    public void BuildBeatContext_ShowsSeedsAndPayoffs()
    {
        var outline = MakeTestOutline();
        var paths = new TestPathProviderWithRoot(testDir);
        var svc = new OutlineService(new FakeLlmService(), new TestDatabaseService(), paths, StreetSamurai.Core.Data.TestDbFactory.For(paths, "outline"), NullLoggers.For<OutlineService>());

        var ctx = svc.BuildBeatContext(outline, 0);
        Assert.That(ctx, Does.Contain("PLANT these seeds"));
        Assert.That(ctx, Does.Contain("katana's origin"));
    }

    [Test]
    public void BuildBeatContext_ShowsNextBeat()
    {
        var outline = MakeTestOutline();
        var paths = new TestPathProviderWithRoot(testDir);
        var svc = new OutlineService(new FakeLlmService(), new TestDatabaseService(), paths, StreetSamurai.Core.Data.TestDbFactory.For(paths, "outline"), NullLoggers.For<OutlineService>());

        var ctx = svc.BuildBeatContext(outline, 0);
        Assert.That(ctx, Does.Contain("NEXT BEAT"));
    }

    [Test]
    public void SaveAndLoad_RoundTrips()
    {
        // Seed a Chapter row so OutlineService.Save actually persists.
        var outline = MakeTestOutline();
        var paths = new TestPathProviderWithRoot(testDir);
        var dbFactory = StreetSamurai.Core.Data.TestDbFactory.For(paths, "outline");
        var chapterId = Guid.NewGuid();
        using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(new StreetSamurai.Core.Data.Entities.Entity
            {
                Id = chapterId,
                EntityType = "chapter",
                Name = "SaveAndLoad test",
                Slug = $"save-load-outline-{chapterId:N}",
                Status = "canon",
            });
            db.Chapters.Add(new StreetSamurai.Core.Data.Entities.Chapter
            {
                Id = chapterId,
                Title = "SaveAndLoad test",
            });
            db.SaveChanges();
        }
        var svc = new OutlineService(new FakeLlmService(), new TestDatabaseService(),
            paths, dbFactory, NullLoggers.For<OutlineService>());

        svc.Save(chapterId.ToString("N"), outline);
        var loaded = svc.Load(chapterId.ToString("N"));

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Title, Is.EqualTo("Test Story"));
        Assert.That(loaded.Acts, Has.Count.EqualTo(1));
        Assert.That(loaded.Acts[0].Beats, Has.Count.EqualTo(3));
    }

    [Test]
    public void Load_NonExistent_ReturnsNull()
    {
        var paths = new TestPathProviderWithRoot(testDir);
        var svc = new OutlineService(new FakeLlmService(), new TestDatabaseService(), paths, StreetSamurai.Core.Data.TestDbFactory.For(paths, "outline"), NullLoggers.For<OutlineService>());

        Assert.That(svc.Load("nonexistent"), Is.Null);
    }

    private static StoryOutline MakeTestOutline() => new()
    {
        Title = "Test Story",
        Logline = "A street samurai faces his past",
        Theme = "Identity under pressure",
        Premise = "Kyle discovers the facility is still operating",
        Characters = ["Kyle", "Sable"],
        Acts =
        [
            new StoryAct
            {
                ActNumber = 1, Name = "Setup", Purpose = "Establish the world",
                Beats =
                [
                    new OutlineBeat { BeatIndex = 0, Title = "Opening", Goal = "Introduce Kyle on the Shelf",
                        Seeds = ["katana's origin"], Tension = 3, EmotionalArc = "melancholy" },
                    new OutlineBeat { BeatIndex = 1, Title = "The Contract", Goal = "Sable offers a job",
                        Tension = 5 },
                    new OutlineBeat { BeatIndex = 2, Title = "Discovery", Goal = "Kyle finds evidence",
                        Payoffs = ["katana's origin"], Tension = 7 },
                ]
            }
        ],
        CharacterArcs =
        [
            new CharacterArc { Character = "Kyle", StartState = "detached", EndState = "committed",
                TurningPoint = "Finding the evidence", Cost = "His anonymity" }
        ],
    };
}
