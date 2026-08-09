using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix to SanityScanService.ScanAsync's SS-A43 child-rollup
/// fallback. It only pulled beats from a book's chapter children when the book's OWN direct beat
/// count was exactly zero — so a book with even one stray/orphaned direct BeatNode (architecturally
/// unexpected for a chaptered book, but confirmed live on a real GLMZ book: one enabled direct
/// beat sitting alongside 547 real beats spread across its 6 chapters) silently scanned only that
/// one leftover beat and ignored the entire real book. Fixed by aggregating children's beats
/// whenever children exist, concatenated with (not replacing) any direct beats.
/// </summary>
[TestFixture]
public class SanityScanServiceChildRollupTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private SanityScanService svc = null!;
    private Guid universeId;
    private int beatNumber;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-sanityscan-rollup-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "sanity-rollup");
        svc = new SanityScanService(dbFactory);
        universeId = Guid.CreateVersion7();
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<Beat> AddBeatAsync(ProseDbContext db, Guid nodeId, string text, double sortKey, bool enabled = true)
    {
        var beat = new Beat { Id = Guid.CreateVersion7(), Number = ++beatNumber, Text = text };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beat.Id, SortKey = sortKey, IsEnabled = enabled });
        return beat;
    }

    [Test]
    public async Task BookWithOneStrayDirectBeat_StillAggregatesRealContentFromChapterChildren()
    {
        var bookId = Guid.CreateVersion7();
        var chapterId = Guid.CreateVersion7();

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Universes.Add(new Universe { Id = universeId, Slug = "u-" + Guid.NewGuid().ToString("N")[..8], Name = "U" });

            var book = NodeFactory.Create("book");
            book.Id = bookId; book.Slug = "b-" + Guid.NewGuid().ToString("N")[..8];
            book.Title = "Testament"; book.Status = "draft"; book.SortKey = 100; book.UniverseId = universeId;
            db.Nodes.Add(book);

            var chapter = NodeFactory.Create("chapter");
            chapter.Id = chapterId; chapter.Slug = "c-" + Guid.NewGuid().ToString("N")[..8];
            chapter.Title = "Part One"; chapter.Status = "draft"; chapter.SortKey = 100;
            chapter.UniverseId = universeId; chapter.ParentNodeId = bookId;
            db.Nodes.Add(chapter);

            // The stray direct beat on the BOOK node itself — small, architecturally anomalous.
            await AddBeatAsync(db, bookId, "Bear raised his hand.", sortKey: 100);

            // The real content — many beats, on the CHAPTER child, forming most of the word count.
            for (var i = 0; i < 20; i++)
                await AddBeatAsync(db, chapterId,
                    "This chapter beat carries real narrative weight and many words of actual prose content.",
                    sortKey: 100 + i);

            await db.SaveChangesAsync();
        }

        var report = await svc.ScanAsync(bookId);

        // The real chapter content is ~20 beats x ~13 words = ~260 words, plus the stray direct
        // beat's 4 words. If the bug were present, WordCount would be just 4 (the stray beat
        // alone, since orderedBeats.Count == 1 != 0 would have skipped the child-rollup entirely).
        Assert.That(report.WordCount, Is.GreaterThan(200),
            "must aggregate the chapter child's beats even though the book has its own stray direct beat");
        Assert.That(report.BeatCount, Is.EqualTo(21), "the stray direct beat (1) plus every chapter beat (20)");
    }

    [Test]
    public async Task LeafBookWithNoChildren_StillUsesItsOwnDirectBeatsUnaffected()
    {
        // Control: a genuine standalone leaf book (no chapter children) must be completely
        // unaffected by this fix — it should scan its own direct beats exactly as before.
        var bookId = Guid.CreateVersion7();

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Universes.Add(new Universe { Id = universeId, Slug = "u-" + Guid.NewGuid().ToString("N")[..8], Name = "U" });

            var book = NodeFactory.Create("book");
            book.Id = bookId; book.Slug = "b-" + Guid.NewGuid().ToString("N")[..8];
            book.Title = "Standalone"; book.Status = "draft"; book.SortKey = 100; book.UniverseId = universeId;
            db.Nodes.Add(book);

            await AddBeatAsync(db, bookId, "A short standalone beat with a handful of words in it.", sortKey: 100);
            await db.SaveChangesAsync();
        }

        var report = await svc.ScanAsync(bookId);

        Assert.That(report.BeatCount, Is.EqualTo(1));
        Assert.That(report.WordCount, Is.EqualTo(11));
    }

    [Test]
    public async Task DisabledDirectBeat_IsExcludedButChildBeatsStillAggregate()
    {
        // Mirrors the exact real scenario: several disabled (soft-deleted) direct beats on the
        // book plus one enabled one, alongside real chapter content — only the enabled direct
        // beat should count, concatenated with (not instead of) the children's beats.
        var bookId = Guid.CreateVersion7();
        var chapterId = Guid.CreateVersion7();

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Universes.Add(new Universe { Id = universeId, Slug = "u-" + Guid.NewGuid().ToString("N")[..8], Name = "U" });

            var book = NodeFactory.Create("book");
            book.Id = bookId; book.Slug = "b-" + Guid.NewGuid().ToString("N")[..8];
            book.Title = "Mixed"; book.Status = "draft"; book.SortKey = 100; book.UniverseId = universeId;
            db.Nodes.Add(book);

            var chapter = NodeFactory.Create("chapter");
            chapter.Id = chapterId; chapter.Slug = "c-" + Guid.NewGuid().ToString("N")[..8];
            chapter.Title = "Ch1"; chapter.Status = "draft"; chapter.SortKey = 100;
            chapter.UniverseId = universeId; chapter.ParentNodeId = bookId;
            db.Nodes.Add(chapter);

            await AddBeatAsync(db, bookId, "This disabled beat should never be counted at all here.", sortKey: 50, enabled: false);
            await AddBeatAsync(db, bookId, "Enabled direct beat five words.", sortKey: 100);
            await AddBeatAsync(db, chapterId, "Real chapter content beat with several words here.", sortKey: 100);

            await db.SaveChangesAsync();
        }

        var report = await svc.ScanAsync(bookId);

        Assert.That(report.BeatCount, Is.EqualTo(2), "the disabled direct beat must not be counted");
    }
}
