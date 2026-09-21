using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Composition.Obligations;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Obligations;

namespace Prose.UnitTests;

/// <summary>
/// <see cref="OutlineSpineService"/> — the outline used as a spine instead of a fallback.
///
/// <para>Real rows, real SQLite, no mocked services and no LLM: everything this service does is
/// parsing plus queries, so there is nothing here that a fake would make more honest. The one
/// property worth stating plainly is the third test — the slice is present whenever a spine
/// exists, unconditionally. A block that appears only when some other block is missing is what
/// the outline used to be, and is the defect this replaces.</para>
/// </summary>
[TestFixture]
public class OutlineSpineServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private OutlineSpineService svc = null!;
    private NarrativeObligationService obligations = null!;
    private Guid bookId, ch1, ch2;
    private List<Guid> beatIds = [];

    private const string Spine = """
        ## BEAT SPINE

        1. [setup] The Empty Room — Vance finds the apartment already cleared out.
        2. [rising-action] The Ledger — the accounts do not reconcile, and someone knew.
        3. [climax] The Roof — Vance chooses which of the two debts to pay.
        4. [resolution] After — what the choice cost, counted honestly.
        """;

    [SetUp]
    public async Task SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-spine-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "spine");

        var audioStore = new LocalDiskAudioStore(paths, NullLogger<LocalDiskAudioStore>.Instance);
        var workbench = new NodeWorkbenchService(dbFactory, null!, paths, audioStore,
            NullLogger<NodeWorkbenchService>.Instance, null!, null!, null!, null!, null!);
        obligations = new NarrativeObligationService(dbFactory, NullLogger<NarrativeObligationService>.Instance);
        svc = new OutlineSpineService(dbFactory, workbench, obligations);

        bookId = Guid.CreateVersion7();
        ch1 = Guid.CreateVersion7();
        ch2 = Guid.CreateVersion7();

        await using var db = await dbFactory.CreateDbContextAsync();
        db.Nodes.Add(new BookNode
        {
            Id = bookId, Slug = "spine-book", NodeCode = "SPN", Title = "Spine Book",
            Kind = "book", UniverseId = Universe.GlmzId,
        });
        db.Nodes.Add(new ChapterNode
        {
            Id = ch1, Slug = "spine-book-ch1", Title = "Chapter 1", Kind = "chapter",
            ParentNodeId = bookId, SortKey = 1, UniverseId = Universe.GlmzId,
        });
        db.Nodes.Add(new ChapterNode
        {
            Id = ch2, Slug = "spine-book-ch2", Title = "Chapter 2", Kind = "chapter",
            ParentNodeId = bookId, SortKey = 2, UniverseId = Universe.GlmzId,
        });

        beatIds = [];
        for (var i = 0; i < 8; i++)
        {
            var id = Guid.CreateVersion7();
            beatIds.Add(id);
            var text = $"Beat {i} body text.";
            db.Beats.Add(new Beat { Id = id, Number = i + 1, Text = text, TextHash = Beat.ComputeHash(text) });
            db.BeatNodes.Add(new BeatNode { NodeId = i < 4 ? ch1 : ch2, BeatId = id, SortKey = i + 1 });
        }
        await db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task SetSpineSectionAsync(string body)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        db.NodeOutlineSections.Add(new NodeOutlineSection
        {
            Id = Guid.NewGuid(), NodeId = bookId, SectionType = "BeatSpine", Content = body,
        });
        await db.SaveChangesAsync();
    }

    private async Task SetOutlineBlobAsync(string text)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var n = await db.Nodes.FirstAsync(x => x.Id == bookId);
        n.NodeOutline = text;
        await db.SaveChangesAsync();
    }

    // ── reading the spine ────────────────────────────────────────────────────

    [Test]
    public async Task GetSpine_ReadsTheStructuredSection()
    {
        // The section row holds the BODY, with no "## BEAT SPINE" heading of its own.
        await SetSpineSectionAsync("""
            1. [setup] The Empty Room — Vance finds the apartment already cleared out.
            2. [rising-action] The Ledger — the accounts do not reconcile, and someone knew.
            """);

        var spine = await svc.GetSpineAsync(bookId);

        Assert.That(spine, Has.Count.EqualTo(2));
        Assert.That(spine[0].Title, Is.EqualTo("The Empty Room"));
        Assert.That(spine[0].StructureRole, Is.EqualTo("setup"));
        Assert.That(spine[1].Goal, Does.Contain("do not reconcile"));
    }

    [Test]
    public async Task GetSpine_FallsBackToTheOutlineBlobTheSectionsReplaced()
    {
        // Most books in the corpus predate the structured rows; their spine is still in the blob.
        await SetOutlineBlobAsync("# Book\n\n" + Spine + "\n\n## SOMETHING ELSE\nnot a beat.\n");

        var spine = await svc.GetSpineAsync(bookId);

        Assert.That(spine, Has.Count.EqualTo(4), "the blob's spine must be found, and the next ## must end it");
        Assert.That(spine[3].Title, Is.EqualTo("After"));
    }

    [Test]
    public async Task GetSpine_NoSpineIsEmptyNotAnError()
    {
        // Normal state for most of this corpus — it must not throw and must not invent entries.
        Assert.That(await svc.GetSpineAsync(bookId), Is.Empty);
    }

    // ── the slice (the fallback-only defect this replaces) ───────────────────

    [Test]
    public async Task GetSlice_IsPresentForEveryBeat_NotOnlyWhenSomethingElseIsMissing()
    {
        await SetOutlineBlobAsync(Spine);

        foreach (var beatId in beatIds)
        {
            var slice = await svc.GetSliceAsync(bookId, beatId);
            Assert.That(slice, Is.Not.Null, "a book with a spine must always get one");
            Assert.That(slice!.Block, Does.Contain("WHAT THE OUTLINE SAYS HAPPENS HERE"));
            Assert.That(slice.Entries, Is.Not.Empty);
        }
    }

    [Test]
    public async Task GetSlice_AdvancesThroughTheSpineAsTheBookProgresses()
    {
        await SetOutlineBlobAsync(Spine);

        var atStart = await svc.GetSliceAsync(bookId, beatIds[0]);
        var atEnd   = await svc.GetSliceAsync(bookId, beatIds[^1]);

        Assert.That(atStart!.Entries[0].Index, Is.EqualTo(1));
        Assert.That(atEnd!.Entries[0].Index, Is.GreaterThan(atStart.Entries[0].Index),
            "the outline slice has to move with the book, or it is a static header rather than a spine");
    }

    [Test]
    public async Task GetSlice_LooksAheadButNeverPastTheEnd()
    {
        await SetOutlineBlobAsync(Spine);

        var slice = await svc.GetSliceAsync(bookId, beatIds[^1], lookahead: 3);

        Assert.That(slice!.Entries, Is.Not.Empty);
        Assert.That(slice.Entries.Count, Is.LessThanOrEqualTo(3));
        Assert.That(slice.Entries[^1].Index, Is.LessThanOrEqualTo(slice.TotalEntries));
    }

    [Test]
    public async Task GetSlice_NoSpineReturnsNullRatherThanAnEmptyBlock()
    {
        // An empty block would read to the writer as "the outline says nothing happens here".
        Assert.That(await svc.GetSliceAsync(bookId, beatIds[0]), Is.Null);
    }

    // ── registration ─────────────────────────────────────────────────────────

    [Test]
    public async Task RegisterReached_OpensAuthoredObligationsForEntriesReached()
    {
        await SetOutlineBlobAsync(Spine);

        var opened = await svc.RegisterReachedAsync(bookId, beatIds[0], actor: "test");

        Assert.That(opened, Is.GreaterThan(0));
        var rows = await obligations.ListAsync(bookId, state: ObligationState.Open);
        Assert.That(rows, Is.Not.Empty);

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.NarrativeObligations.AsNoTracking().FirstAsync(o => o.NodeId == bookId);
        Assert.That(row.Provenance, Is.EqualTo(ClaimProvenance.Authored),
            "an outline entry is the author's own statement of intent, not something inferred from prose");
        Assert.That(row.DueByKind, Is.EqualTo(ObligationDueKind.Chapter));
    }

    [Test]
    public async Task RegisterReached_IsIdempotent()
    {
        await SetOutlineBlobAsync(Spine);

        var first  = await svc.RegisterReachedAsync(bookId, beatIds[^1], actor: "test");
        var second = await svc.RegisterReachedAsync(bookId, beatIds[^1], actor: "test");

        Assert.That(first, Is.GreaterThan(0));
        Assert.That(second, Is.Zero, "re-running over a registered book must cost nothing and change nothing");

        var rows = await obligations.ListAsync(bookId);
        Assert.That(rows.Count, Is.EqualTo(first), "no duplicate rows on the second pass");
    }

    [Test]
    public async Task RegisterReached_OpensEarlierEntriesThatWereSkipped()
    {
        await SetOutlineBlobAsync(Spine);

        // Jump straight to the last beat: everything before it was skipped, and a skipped
        // outline entry is precisely what this is supposed to make visible.
        var opened = await svc.RegisterReachedAsync(bookId, beatIds[^1], actor: "test");

        Assert.That(opened, Is.GreaterThan(1), "entries the book jumped over must still be recorded");
    }

    // ── the chapter close ────────────────────────────────────────────────────

    [Test]
    public async Task DiffChapter_ReportsEntriesDueByNowThatAreStillOpen()
    {
        await SetOutlineBlobAsync(Spine);
        await svc.RegisterReachedAsync(bookId, beatIds[^1], actor: "test");

        var filed = await svc.DiffChapterAsync(bookId, chapterOrdinal: 2);

        Assert.That(filed, Is.Not.Empty);
        Assert.That(filed[0], Does.StartWith("OUTLINE-UNADDRESSED"));
    }

    [Test]
    public async Task DiffChapter_SaysNothingWhenTheOutlineWasFollowed()
    {
        await SetOutlineBlobAsync(Spine);
        await svc.RegisterReachedAsync(bookId, beatIds[^1], actor: "test");

        // Close every outline obligation, as writing the beats it describes would.
        var open = await obligations.ListAsync(bookId, state: ObligationState.Open);
        foreach (var o in open)
            await obligations.CloseAsync(o.Id, beatIds[^1], "Beat 7 body text.", note: null, actor: "test");

        var filed = await svc.DiffChapterAsync(bookId, chapterOrdinal: 2);

        Assert.That(filed, Is.Empty, "a followed outline must be silent — noise here would train the author to ignore it");
    }

    [Test]
    public async Task DiffChapter_NoSpineIsSilent()
    {
        Assert.That(await svc.DiffChapterAsync(bookId, chapterOrdinal: 1), Is.Empty);
    }
}
