using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Where a chapter begins. The transition between UNITS is the answer and nothing else is — where a
/// unit is a beat's node, or the nearest ancestor of it that is not a scene or a sequence. These
/// tests pin that against the three things that have been mistaken for it: a beat flagged
/// <c>IsChapterStart</c>, a beat whose prose opens with the words "Chapter 7", and — the reason the
/// rule is stated in units rather than nodes — a scene layer derived under a chapter, which used to
/// turn one chapter into twenty-one chapters headed <c>sidewalk</c> and <c>home terminal</c>.
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

    private async Task<SceneNode> MakeSceneAsync(Guid parentId, string title, double sortKey,
                                                 bool sequel = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var scene = sequel ? SceneNode.Sequel() : new SceneNode();
        scene.Id = Guid.CreateVersion7();
        scene.Slug = "sc-" + Guid.NewGuid().ToString("N")[..8];
        scene.Title = title;
        scene.Status = "draft";
        scene.ParentNodeId = parentId;
        scene.SortKey = sortKey;
        db.Nodes.Add(scene);
        await db.SaveChangesAsync();
        return scene;
    }

    private async Task<SequenceNode> MakeSequenceAsync(Guid parentId, string title, double sortKey)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var sequence = new SequenceNode
        {
            Id = Guid.CreateVersion7(),
            Slug = "sq-" + Guid.NewGuid().ToString("N")[..8],
            Title = title,
            Status = "draft",
            ParentNodeId = parentId,
            SortKey = sortKey,
        };
        db.Nodes.Add(sequence);
        await db.SaveChangesAsync();
        return sequence;
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

    // ── The scene layer: structure the reader must never see ──────────────

    /// <summary>The defect this rule exists for. A chapter given a scene layer holds its beats on
    /// its scenes, so the walk reports scene ids where the chapter id used to be and the chapter —
    /// now holding no beats of its own — never appears at all. Before the unit rule that exported as
    /// three chapters headed "sidewalk", "kitchen" and "transit platform", with the chapter's own
    /// title nowhere in the book.</summary>
    [Test]
    public async Task SceneChildren_RollUpIntoTheirChapter()
    {
        var book = await MakeBookAsync();
        var chapter = await MakeChapterAsync(book.Id, ChapterTitle.Format(15, "Work Order"), 100);
        var sidewalk = await MakeSceneAsync(chapter.Id, "sidewalk", 100);
        var kitchen = await MakeSceneAsync(chapter.Id, "kitchen, apartment below Halsted", 200);
        var platform = await MakeSceneAsync(chapter.Id, "transit platform", 300);
        await AddBeatAsync(sidewalk.Id, "a", 100);
        await AddBeatAsync(sidewalk.Id, "b", 200);
        await AddBeatAsync(kitchen.Id, "c", 100);
        await AddBeatAsync(platform.Id, "d", 100);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(1));
        Assert.That(spine.BeatCount, Is.EqualTo(4));
        Assert.That(spine.Chapters[0].Heading, Is.EqualTo("Chapter 15 — Work Order"));
        Assert.That(spine.Chapters[0].NodeId, Is.EqualTo(chapter.Id));
        // The number the validator checks is parsed off the unit's title, which is why it was
        // reading "sidewalk" and reporting a numbering gap where there was none.
        Assert.That(spine.Chapters[0].Parsed.Number, Is.EqualTo(15));
        Assert.That(spine.Chapters.Select(c => c.Heading), Has.None.EqualTo("sidewalk"));
        // Reading order is unbroken across the scene boundaries.
        Assert.That(spine.Chapters[0].Beats.Select(b => b.Ordinal), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        // And the scene layer survives, on the beats, for anything that needs it.
        Assert.That(spine.Chapters[0].SubUnitNodeIds,
                    Is.EqualTo(new[] { sidewalk.Id, kitchen.Id, platform.Id }));
    }

    /// <summary>Two levels roll up, and a sequel rolls up with them. A sequel is a SceneNode with
    /// Kind "sequel", so a NodeType test covers it without naming it — which is the argument for
    /// testing the discriminator rather than Kind.</summary>
    [Test]
    public async Task SequenceAndSequelLayers_BothRollUp()
    {
        var book = await MakeBookAsync();
        var chapter = await MakeChapterAsync(book.Id, ChapterTitle.Format(1, "Part One"), 100);
        var sequence = await MakeSequenceAsync(chapter.Id, "The Approach", 100);
        var scene = await MakeSceneAsync(sequence.Id, "Arrival", 100);
        var sequel = await MakeSceneAsync(sequence.Id, "The Reckoning", 200, sequel: true);
        await AddBeatAsync(scene.Id, "a", 100);
        await AddBeatAsync(sequel.Id, "b", 100);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(1));
        Assert.That(spine.Chapters[0].Heading, Is.EqualTo("Chapter 1 — Part One"));
        Assert.That(spine.Chapters[0].Beats, Has.Count.EqualTo(2));
        Assert.That(spine.Chapters[0].SubUnitNodeIds, Is.EqualTo(new[] { scene.Id, sequel.Id }));
    }

    /// <summary>A mixed book: one chapter with a scene layer, one without. Both print as one unit
    /// each, and the roll-up does not leak across the boundary between them.</summary>
    [Test]
    public async Task MixedBook_SceneChapterAndPlainChapter_AreOneUnitEach()
    {
        var book = await MakeBookAsync();
        var deep = await MakeChapterAsync(book.Id, ChapterTitle.Format(1, "Deep"), 100);
        var scene = await MakeSceneAsync(deep.Id, "a rooftop", 100);
        await AddBeatAsync(scene.Id, "a", 100);
        var flat = await MakeChapterAsync(book.Id, ChapterTitle.Format(2, "Flat"), 200);
        await AddBeatAsync(flat.Id, "b", 100);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.Chapters.Select(c => c.Heading),
                    Is.EqualTo(new[] { "Chapter 1 — Deep", "Chapter 2 — Flat" }));
    }

    /// <summary>A chapter holding its own beats AND scene children stays ONE contiguous unit. The
    /// walk emits a node's direct beats before recursing into its children, so a naive rule reopens
    /// the chapter when the scenes arrive.</summary>
    [Test]
    public async Task ChapterWithOwnBeatsAndScenes_IsOneContiguousUnit()
    {
        var book = await MakeBookAsync();
        var chapter = await MakeChapterAsync(book.Id, ChapterTitle.Format(3, "Both"), 100);
        await AddBeatAsync(chapter.Id, "direct", 100);
        var scene = await MakeSceneAsync(chapter.Id, "a stairwell", 100);
        await AddBeatAsync(scene.Id, "in the scene", 100);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(1));
        Assert.That(spine.Chapters[0].Beats, Has.Count.EqualTo(2));
    }

    /// <summary>The flat-book exception is computed on the beat-bearing nodes, not on units. A
    /// single scene-derived chapter hangs its beats off several scenes, so a unit-based test would
    /// call this book flat and let a stray legacy marker shatter it.</summary>
    [Test]
    public async Task SceneDerivedSingleChapter_IsNotTreatedAsFlat()
    {
        var book = await MakeBookAsync();
        var chapter = await MakeChapterAsync(book.Id, ChapterTitle.Format(1, "Only"), 100);
        var first = await MakeSceneAsync(chapter.Id, "a dock", 100);
        var second = await MakeSceneAsync(chapter.Id, "a van", 200);
        await AddBeatAsync(first.Id, "a", 100);
        await AddBeatAsync(second.Id, "b", 100, isChapterStart: true, title: "Chapter 9 — Stray");

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(1));
        Assert.That(spine.Chapters.Select(c => c.OpenedByBeatMarker), Is.All.False);
        Assert.That(spine.Chapters[0].Heading, Is.EqualTo("Chapter 1 — Only"));
    }

    /// <summary>A scene parented straight to the book has no chapter to roll up into, and must not
    /// collapse into the book root — that would merge a whole book into one headingless unit. It
    /// stays its own unit, where --validate-chapters can see and report it.</summary>
    [Test]
    public async Task SceneParentedToTheBook_StaysItsOwnUnit()
    {
        var book = await MakeBookAsync();
        var loose = await MakeSceneAsync(book.Id, "an alley", 100);
        var other = await MakeSceneAsync(book.Id, "a roof", 200);
        await AddBeatAsync(loose.Id, "a", 100);
        await AddBeatAsync(other.Id, "b", 100);

        var spine = await svc.GetAsync(book.Id);

        Assert.That(spine.ChapterCount, Is.EqualTo(2));
        Assert.That(spine.Chapters.Select(c => c.IsBookRoot), Is.All.False);
        Assert.That(spine.Chapters.Select(c => c.Heading), Is.EqualTo(new[] { "an alley", "a roof" }));
    }

    // ── Counting the words in a beat ──────────────────────────────────────

    /// <summary>Beat text is tagged, so a bare whitespace split counts the tags. On a heavily
    /// tagged 192,148-word book that overstated the total by 7,260 words — and the error moves
    /// with the TAGS, which every beat save re-derives, so the number could change while not one
    /// word of prose had. That is why this is a named helper and not an inline Split.</summary>
    [Test]
    public void ProseWordCount_DoesNotCountEntityTagsAsWords()
    {
        const string tagged =
            "<entity repo=\"character\" guid=\"019d6143-a648-7876-9688-0f6d38d70075\">Kyle</entity> " +
            "didn't correct her.";

        Assert.That(ProseWordCount.Count(tagged), Is.EqualTo(4), "Kyle / didn't / correct / her.");
        Assert.That(tagged.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length,
                    Is.GreaterThan(4),
                    "The naive split this replaced — kept as the contrast that makes the rule readable.");
    }

    [Test]
    public void ProseWordCount_IsUnchangedByRetagging()
    {
        const string plain = "He looked at it in the hall.";
        const string retagged = "He looked at <entity repo=\"place\" guid=\"019d6143-a927\">it</entity> in the hall.";

        Assert.That(ProseWordCount.Count(retagged), Is.EqualTo(ProseWordCount.Count(plain)),
                    "Re-deriving entity mentions must never move a word count.");
    }

    [Test]
    public void ProseWordCount_EmptyAndNull_AreZero()
    {
        Assert.That(ProseWordCount.Count(null), Is.Zero);
        Assert.That(ProseWordCount.Count("   \n\t "), Is.Zero);
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
