using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Tests for BookOutlineService.ParseGenerated — the LLM-generated-outline parser (chapters +
/// threads). Made internal (was private), dropped its unused BookOutline parameter (nothing in
/// the method body read it), and made GenChapter/GenThread/GenOutline internal too —
/// InternalsVisibleTo already covers this project.
///
/// Found and fixed a real bug while adding this coverage (4th instance of the same class this
/// session — LogicSweepService, ChekhovAuditService, EmotionalDepthService): JsonElement.
/// TryGetInt32 THROWS InvalidOperationException on a non-Number token despite the Try- name (e.g.
/// a hallucinated "number": null on one chapter), and the per-chapter/per-thread body had no
/// individual try/catch — one malformed chapter or thread would hit the outer catch and discard
/// the entire generated outline (every chapter, every thread) rather than just the bad one.
/// </summary>
[TestFixture]
public class BookOutlineServiceTests
{
    [Test]
    public void ParseGenerated_ValidOutline_ParsesChaptersAndThreads()
    {
        var raw = """
            {"theme":"betrayal","structure":"three-act","chapters":[
                {"number":1,"title":"The Setup","pov_character":"Kira","body":"Opens on the docks."}
            ],"threads":[
                {"name":"the stolen ledger","description":"who took it","planted_in_chapter_number":1,"pays_off_in_chapter_number":5}
            ]}
            """;
        var outline = BookOutlineService.ParseGenerated(raw);

        Assert.That(outline, Is.Not.Null);
        Assert.That(outline!.Theme, Is.EqualTo("betrayal"));
        Assert.That(outline.Chapters, Has.Count.EqualTo(1));
        Assert.That(outline.Chapters[0].Number, Is.EqualTo(1));
        Assert.That(outline.Chapters[0].Title, Is.EqualTo("The Setup"));
        Assert.That(outline.Threads, Has.Count.EqualTo(1));
        Assert.That(outline.Threads[0].PlantedNum, Is.EqualTo(1));
        Assert.That(outline.Threads[0].PaysOffNum, Is.EqualTo(5));
    }

    [Test]
    public void ParseGenerated_LegacyStructuredShape_FoldsIntoBody()
    {
        var raw = """
            {"chapters":[
                {"number":1,"long_synopsis":"A long take.","key_beats":["beat one","beat two"]}
            ]}
            """;
        var outline = BookOutlineService.ParseGenerated(raw);

        Assert.That(outline!.Chapters[0].Body, Does.Contain("A long take."));
        Assert.That(outline.Chapters[0].Body, Does.Contain("beat one"));
    }

    [Test]
    public void ParseGenerated_NoBraces_ReturnsNull()
    {
        Assert.That(BookOutlineService.ParseGenerated("no json here"), Is.Null);
    }

    [Test]
    public void ParseGenerated_MalformedJson_ReturnsNullInsteadOfThrowing()
    {
        Assert.DoesNotThrow(() =>
            Assert.That(BookOutlineService.ParseGenerated("{\"chapters\": oops}"), Is.Null));
    }

    [Test]
    public void ParseGenerated_MissingChapterNumber_DefaultsToZero()
    {
        var raw = """{"chapters":[{"title":"No Number"}]}""";
        var outline = BookOutlineService.ParseGenerated(raw);
        Assert.That(outline!.Chapters[0].Number, Is.EqualTo(0));
    }

    // ── Regression: a hallucinated null on one chapter/thread must not discard the whole outline ──

    [Test]
    public void ParseGenerated_NullChapterNumberOnOneChapter_OtherChaptersStillParsed()
    {
        var raw = """
            {"chapters":[
                {"number":null,"title":"Broken Chapter"},
                {"number":2,"title":"Good Chapter"}
            ]}
            """;
        var outline = BookOutlineService.ParseGenerated(raw);

        Assert.That(outline, Is.Not.Null,
            "one malformed chapter (null number) must not discard the whole generated outline");
        Assert.That(outline!.Chapters.Any(c => c.Title == "Good Chapter"), Is.True);
    }

    [Test]
    public void ParseGenerated_NullChapterNumber_FallsBackToZeroInsteadOfThrowing()
    {
        var raw = """{"chapters":[{"number":null,"title":"x"}]}""";
        BookOutlineService.GenOutline? outline = null;

        Assert.DoesNotThrow(() => outline = BookOutlineService.ParseGenerated(raw));
        Assert.That(outline, Is.Not.Null);
        Assert.That(outline!.Chapters, Has.Count.EqualTo(1));
        Assert.That(outline.Chapters[0].Number, Is.EqualTo(0), "a null number falls back to 0 rather than throwing");
    }

    [Test]
    public void ParseGenerated_NullThreadPlantedNum_OtherThreadsStillParsed()
    {
        var raw = """
            {"threads":[
                {"name":"broken","planted_in_chapter_number":null},
                {"name":"good","planted_in_chapter_number":3}
            ]}
            """;
        var outline = BookOutlineService.ParseGenerated(raw);

        Assert.That(outline, Is.Not.Null);
        Assert.That(outline!.Threads.Any(t => t.Name == "good" && t.PlantedNum == 3), Is.True);
    }

    [Test]
    public void ParseGenerated_EmptyObject_ReturnsEmptyOutline()
    {
        var outline = BookOutlineService.ParseGenerated("{}");
        Assert.That(outline, Is.Not.Null);
        Assert.That(outline!.Chapters, Is.Empty);
        Assert.That(outline.Threads, Is.Empty);
    }
}
