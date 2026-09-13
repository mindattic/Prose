using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Where a chapter begins. The node transition is the answer and nothing else is — these tests pin
/// that against the two things that used to be mistaken for it: a beat flagged
/// <c>IsChapterStart</c>, and a beat whose prose opens with the words "Chapter 7".
/// </summary>
[TestFixture]
public class BookSpineServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private BookSpineService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-spine-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "spine");
        svc = new BookSpineService(dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    // ── Fixture building ──────────────────────────────────────────────────

    private async Task<BookNode> MakeBookAsync(string title = "Test Book")
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var book = new BookNode
        {
            Id = Guid.CreateVersion7(),
            Slug = "book-" + Guid.NewGuid().ToString("N")[..8],
            Title = title,
            Kind = "book",
            Status = "draft",
            SortKey = 100,
        };
        db.Nodes.Add(book);
        await db.SaveChangesAsync();
        return book;
    }

    private async Task<ChapterNode> MakeChapterAsync(Guid bookId, string title, double sortKey)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var chapter = new ChapterNode
        {
            Id = Guid.CreateVersion7(),
            Slug = "ch-" + Guid.NewGuid().ToString("N")[..8],
            Title = title,
            Status = "draft",
            ParentNodeId = bookId,
            SortKey = sortKey,
        };
        db.Nodes.Add(chapter);
        await db.SaveChangesAsync();
        return chapter;
    }

    private async Task<Beat> AddBeatAsync(Guid nodeId, string text, double sortKey,
                                          bool isChapterStart = false, string? title = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        // Beat.Number is a globally-unique human handle with a UNIQUE index — allocate it the way
        // NodeWorkbenchService does, or the second beat in any fixture collides on 0.
        var number = (await db.Beats.MaxAsync(b => (int?)b.Number) ?? 0) + 1;
        var beat = new Beat
        {
            Id = Guid.CreateVersion7(),
            Number = number,
            Text = text,
            Title = title,
            IsChapterStart = isChapterStart,
        };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beat.Id, SortKey = sortKey });
        await db.SaveChangesAsync();
        return beat;
    }

    // ── Tests ─────────────────────────────────────────────────────────────

    [Test]
    public async Task EmptyBook_HasNoChapters()
    {
        var book = await MakeBookAsync();
        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(0));
        Assert.That(spine.BeatCount, Is.EqualTo(0));
        Assert.That(spine.Entries.ToList(), Is.Empty);
    }

    [Test]
    public async Task OneGroupPerChapterNode_InReadingOrder()
    {
        var book = await MakeBookAsync();
        var one = await MakeChapterAsync(book.Id, ChapterTitle.Format(1, "Teeth"), 100);
        var two = await MakeChapterAsync(book.Id, ChapterTitle.Format(2, "Static"), 200);
        await AddBeatAsync(one.Id, "First.", 100);
        await AddBeatAsync(one.Id, "Second.", 200);
        await AddBeatAsync(two.Id, "Third.", 100);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(2));
        Assert.That(spine.BeatCount, Is.EqualTo(3));
        Assert.That(spine.Chapters[0].Title, Is.EqualTo("Chapter 1 — Teeth"));
        Assert.That(spine.Chapters[0].Beats, Has.Count.EqualTo(2));
        Assert.That(spine.Chapters[1].Title, Is.EqualTo("Chapter 2 — Static"));
        Assert.That(spine.Chapters[1].Beats, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task BeatOrdinals_AreContinuousAcrossTheWholeBook()
    {
        var book = await MakeBookAsync();
        var one = await MakeChapterAsync(book.Id, ChapterTitle.Format(1, null), 100);
        var two = await MakeChapterAsync(book.Id, ChapterTitle.Format(2, null), 200);
        await AddBeatAsync(one.Id, "a", 100);
        await AddBeatAsync(one.Id, "b", 200);
        await AddBeatAsync(two.Id, "c", 100);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.Chapters[0].Beats.Select(b => b.Ordinal), Is.EqualTo(new[] { 1, 2 }));
        Assert.That(spine.Chapters[1].Beats.Select(b => b.Ordinal), Is.EqualTo(new[] { 3 }));
        Assert.That(spine.Chapters.Select(c => c.Ordinal), Is.EqualTo(new[] { 1, 2 }));
    }

    /// <summary>The old overloaded signal. A beat marked IsChapterStart whose title is NOT itself
    /// chapter-shaped is a sub-heading — BCODA's "Three Barrels" — and must not open a new unit.</summary>
    [Test]
    public async Task IsChapterStartWithAnOrdinaryTitle_IsASubHeadingNotAChapter()
    {
        var book = await MakeBookAsync();
        var one = await MakeChapterAsync(book.Id, ChapterTitle.Format(1, null), 100);
        await AddBeatAsync(one.Id, "Opening.", 100);
        await AddBeatAsync(one.Id, "Three barrels.", 200, isChapterStart: true, title: "Three Barrels");
        await AddBeatAsync(one.Id, "Closing.", 300);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(1));
        Assert.That(spine.Chapters[0].Beats, Has.Count.EqualTo(3));
        Assert.That(spine.Chapters[0].Beats[1].IsSubHeading, Is.True);
        Assert.That(spine.Chapters[0].Beats[0].IsSubHeading, Is.False);
    }

    /// <summary>An IsChapterStart beat with no title at all is neither a chapter nor a sub-heading
    /// — there is nothing to print.</summary>
    [Test]
    public async Task IsChapterStartWithNoTitle_IsNeither()
    {
        var book = await MakeBookAsync();
        var one = await MakeChapterAsync(book.Id, ChapterTitle.Format(1, null), 100);
        await AddBeatAsync(one.Id, "Opening.", 100);
        await AddBeatAsync(one.Id, "Unmarked.", 200, isChapterStart: true);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(1));
        Assert.That(spine.Chapters[0].Beats[1].IsSubHeading, Is.False);
    }

    // ── The legacy flat-book exception ────────────────────────────────────

    /// <summary>
    /// A flat single-node book with chapter-shaped IsChapterStart beats: the markers DO open units.
    /// This is the shape the three exporters disagreed about — docx printed one chapter, epub/pdf
    /// and markdown printed N. N is what a reader needs, so N is what the spine says.
    /// </summary>
    [Test]
    public async Task FlatBook_ChapterShapedMarkers_OpenUnits()
    {
        var book = await MakeBookAsync();
        var only = await MakeChapterAsync(book.Id, "Vigil's End", 100);
        await AddBeatAsync(only.Id, "Lead-in.", 100);
        await AddBeatAsync(only.Id, "Rain.", 200, isChapterStart: true, title: "Chapter 2 - Provenance");
        await AddBeatAsync(only.Id, "More rain.", 300);
        await AddBeatAsync(only.Id, "Static.", 400, isChapterStart: true, title: "Chapter 3 — Teeth");

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(3));
        Assert.That(spine.BeatCount, Is.EqualTo(4));
        Assert.That(spine.Chapters.Select(c => c.Heading),
                    Is.EqualTo(new[] { "Vigil's End", "Chapter 2 - Provenance", "Chapter 3 — Teeth" }));
        Assert.That(spine.Chapters.Select(c => c.OpenedByBeatMarker),
                    Is.EqualTo(new[] { false, true, true }));
        Assert.That(spine.Chapters[0].Beats, Has.Count.EqualTo(1));
        Assert.That(spine.Chapters[1].Beats, Has.Count.EqualTo(2));
    }

    /// <summary>A properly chaptered book ignores the markers entirely — the exception is only for
    /// the flat shape, or a stray legacy marker would shatter a real chapter.</summary>
    [Test]
    public async Task ChapteredBook_IgnoresFlatMarkers()
    {
        var book = await MakeBookAsync();
        var one = await MakeChapterAsync(book.Id, ChapterTitle.Format(1, null), 100);
        var two = await MakeChapterAsync(book.Id, ChapterTitle.Format(2, null), 200);
        await AddBeatAsync(one.Id, "a", 100);
        await AddBeatAsync(one.Id, "b", 200, isChapterStart: true, title: "Chapter 9 — Stray");
        await AddBeatAsync(two.Id, "c", 100);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(2));
        Assert.That(spine.Chapters.Select(c => c.OpenedByBeatMarker), Is.All.False);
    }

    /// <summary>The heading precedence both exporters implement: a chapter-shaped beat title
    /// outranks the node's own title, because the author's punctuation is usually better.</summary>
    [Test]
    public async Task ChapterShapedBeatTitle_OutranksTheNodeTitle()
    {
        var book = await MakeBookAsync();
        var one = await MakeChapterAsync(book.Id, "Chapter 1", 100);
        var two = await MakeChapterAsync(book.Id, "Chapter 2", 200);
        await AddBeatAsync(one.Id, "a", 100, title: "Chapter 1 — The Floor Is Hard");
        await AddBeatAsync(two.Id, "b", 100, title: "Across the Hall");

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.Chapters[0].Heading, Is.EqualTo("Chapter 1 — The Floor Is Hard"));
        // Not chapter-shaped, so the node's title stands rather than being replaced.
        Assert.That(spine.Chapters[1].Heading, Is.EqualTo("Chapter 2"));
        // Title stays the node's own — that is what a rename edits.
        Assert.That(spine.Chapters[0].Title, Is.EqualTo("Chapter 1"));
    }

    /// <summary>The other old signal: legacy draft debris whose prose repeats the chapter title.
    /// It is reported, never acted on.</summary>
    [Test]
    public async Task ProseThatOpensWithAHeading_IsReportedNotSplitOn()
    {
        var book = await MakeBookAsync();
        var one = await MakeChapterAsync(book.Id, ChapterTitle.Format(2, "Provenance"), 100);
        await AddBeatAsync(one.Id, "Chapter 2 - Provenance\n\nThe rain came.", 100);
        await AddBeatAsync(one.Id, "The chapter had ended badly.", 200);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(1));
        Assert.That(spine.Chapters[0].Beats[0].LooksLikeStrayHeading, Is.True);
        Assert.That(spine.Chapters[0].Beats[1].LooksLikeStrayHeading, Is.False);
    }

    /// <summary>Beats hanging off the book node itself — the "unfiled" state
    /// <c>WrapInSingleChapterAsync</c> repairs. A real state in the corpus, so it is labelled
    /// rather than hidden.</summary>
    [Test]
    public async Task BeatsOnTheBookNode_AreTheirOwnGroupAndFlagged()
    {
        var book = await MakeBookAsync();
        await AddBeatAsync(book.Id, "Unfiled opening.", 100);
        var one = await MakeChapterAsync(book.Id, ChapterTitle.Format(1, null), 100);
        await AddBeatAsync(one.Id, "Filed.", 100);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(2));
        Assert.That(spine.Chapters[0].IsBookRoot, Is.True);
        Assert.That(spine.Chapters[1].IsBookRoot, Is.False);
    }

    /// <summary>A chapter split into a nested Collection: each leaf is its own unit, which is what
    /// the exporters already print.</summary>
    [Test]
    public async Task NestedSubChapters_EachBecomeTheirOwnUnit()
    {
        var book = await MakeBookAsync();
        var parent = await MakeChapterAsync(book.Id, ChapterTitle.Format(1, null), 100);
        var subA = await MakeChapterAsync(parent.Id, ChapterTitle.Format(1, "Part One"), 100);
        var subB = await MakeChapterAsync(parent.Id, ChapterTitle.Format(2, "Part Two"), 200);
        await AddBeatAsync(subA.Id, "a", 100);
        await AddBeatAsync(subB.Id, "b", 100);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(2));
        Assert.That(spine.Chapters.Select(c => c.Title),
                    Is.EqualTo(new[] { "Chapter 1 — Part One", "Chapter 2 — Part Two" }));
    }

    [Test]
    public async Task Entries_InterleavesBreaksAndBeatsInReadingOrder()
    {
        var book = await MakeBookAsync();
        var one = await MakeChapterAsync(book.Id, ChapterTitle.Format(1, null), 100);
        var two = await MakeChapterAsync(book.Id, ChapterTitle.Format(2, null), 200);
        await AddBeatAsync(one.Id, "a", 100);
        await AddBeatAsync(two.Id, "b", 100);

        var entries = (await svc.GetAsync(book.Id)).Entries.ToList();

        Assert.That(entries.Select(e => e.IsChapterBreak), Is.EqualTo(new[] { true, false, true, false }));
        Assert.That(entries[1].Chapter.Ordinal, Is.EqualTo(1));
        Assert.That(entries[3].Chapter.Ordinal, Is.EqualTo(2));
    }

    /// <summary>Word counts come off the words a reader sees, not the markup. This is the whole
    /// reason the spine does not reuse the exporters' whitespace split.</summary>
    [Test]
    public async Task WordCount_IgnoresEntityTagsAndEmphasisMarkers()
    {
        var book = await MakeBookAsync();
        var one = await MakeChapterAsync(book.Id, ChapterTitle.Format(1, null), 100);
        await AddBeatAsync(one.Id,
            """<entity repo="character" guid="01a0030b-0000-7000-8000-000000000001">Declan Doyle</entity> was **late**.""",
            100);

        var spine = await svc.GetAsync(book.Id);

        // "Declan Doyle was late." — four words.
        Assert.That(spine.Chapters[0].Beats[0].WordCount, Is.EqualTo(4));
        Assert.That(spine.WordCount, Is.EqualTo(4));
    }

    [Test]
    public async Task Preview_IsTheFirstLineOfPlainProse()
    {
        var book = await MakeBookAsync();
        var one = await MakeChapterAsync(book.Id, ChapterTitle.Format(1, null), 100);
        await AddBeatAsync(one.Id, "The rain came *sideways*.\n\nAnd then it stopped.", 100);
        await AddBeatAsync(one.Id, "", 200);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.Chapters[0].Beats[0].Preview, Is.EqualTo("The rain came sideways."));
        Assert.That(spine.Chapters[0].Beats[1].Preview, Is.EqualTo("(empty)"));
    }

    /// <summary>The number a title claims and the position it actually occupies are kept apart so a
    /// numbering defect is visible rather than silently normalised.</summary>
    [Test]
    public async Task ParsedNumber_AndOrdinal_AreReportedSeparately()
    {
        var book = await MakeBookAsync();
        var one = await MakeChapterAsync(book.Id, "Chapter 1", 100);
        var skipped = await MakeChapterAsync(book.Id, "Chapter 7", 200);
        await AddBeatAsync(one.Id, "a", 100);
        await AddBeatAsync(skipped.Id, "b", 100);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.Chapters[1].Ordinal, Is.EqualTo(2));
        Assert.That(spine.Chapters[1].Parsed.Number, Is.EqualTo(7));
    }

    [Test]
    public async Task Heading_FallsBackOnlyWhenTheNodeHasNoTitle()
    {
        var book = await MakeBookAsync();
        var titled = await MakeChapterAsync(book.Id, "Chapter 1 — Teeth", 100);
        var blank = await MakeChapterAsync(book.Id, "", 200);
        await AddBeatAsync(titled.Id, "a", 100);
        await AddBeatAsync(blank.Id, "b", 100);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.Chapters[0].Heading, Is.EqualTo("Chapter 1 — Teeth"));
        Assert.That(spine.Chapters[1].Heading, Is.EqualTo("Chapter 2"));
    }
}
