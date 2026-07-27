using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.UnitTests;

/// <summary>
/// Locks in the contract of <see cref="NodeMigrationService"/>:
/// <list type="bullet">
/// <item>Books / Chapters / ChapterBeats / Episodes / EpisodeBeats translate
///   to Nodes / Beats / BeatNodes with parent-child wiring preserved.</item>
/// <item>Re-running on a partially-migrated DB picks up only the new rows
///   (idempotent — a key property since the migration runs on startup and
///   when new episodes are generated via the CLI).</item>
/// </list>
/// </summary>
[TestFixture]
public class NodeMigrationServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<StreetSamuraiDbContext> dbFactory = null!;
    private NodeMigrationService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-node-mig-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "node-migration");
        svc = new NodeMigrationService(dbFactory, NullLogger<NodeMigrationService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task SeedLegacyAsync(int books, int chaptersPerBook, int beatsPerChapter)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        for (int bi = 0; bi < books; bi++)
        {
            var book = new Book { Id = Guid.CreateVersion7(), Title = $"Book {bi}", Slug = $"book-{bi}-{Guid.NewGuid().ToString("N")[..6]}" };
            db.Books.Add(book);
            for (int ci = 0; ci < chaptersPerBook; ci++)
            {
                var chapter = new Chapter
                {
                    Id = Guid.CreateVersion7(), BookId = book.Id, Number = ci + 1,
                    Title = $"Book {bi} Chapter {ci + 1}", Synopsis = "Synopsis", Status = "draft",
                };
                db.Chapters.Add(chapter);
                for (int beatI = 0; beatI < beatsPerChapter; beatI++)
                {
                    db.ChapterBeats.Add(new ChapterBeat
                    {
                        BeatGuid = Guid.CreateVersion7(), ChapterId = chapter.Id,
                        Index = beatI, SortKey = beatI * 100.0,
                        Title = $"Beat {beatI}",
                        Text = $"Prose for book {bi} chapter {ci} beat {beatI}.",
                        Act = 1, SceneType = "scene",
                    });
                }
            }
        }
        await db.SaveChangesAsync();
    }

    [Test]
    public async Task MigrateAll_OnFreshLegacyData_ProducesMatchingNodeRows()
    {
        await SeedLegacyAsync(books: 2, chaptersPerBook: 3, beatsPerChapter: 4);

        var report = await svc.MigrateAllAsync();

        Assert.That(report.BooksAdded,    Is.EqualTo(2));
        Assert.That(report.ChaptersAdded, Is.EqualTo(6));
        Assert.That(report.BeatsAdded,    Is.EqualTo(24));

        await using var db = await dbFactory.CreateDbContextAsync();
        Assert.That(await db.Nodes.CountAsync(s => s.Kind == "book"),    Is.EqualTo(2));
        Assert.That(await db.Nodes.CountAsync(s => s.Kind == "chapter"), Is.EqualTo(6));
        Assert.That(await db.Beats.CountAsync(),                            Is.EqualTo(24));
        Assert.That(await db.BeatNodes.CountAsync(),                      Is.EqualTo(24));

        // Parent-child wiring: every chapter node points at a book node.
        var orphans = await db.Nodes
            .Where(s => s.Kind == "chapter" && s.ParentNodeId == null)
            .CountAsync();
        Assert.That(orphans, Is.EqualTo(0));
    }

    [Test]
    public async Task MigrateAll_IsIdempotent_OnReRun()
    {
        await SeedLegacyAsync(books: 1, chaptersPerBook: 2, beatsPerChapter: 3);

        var first = await svc.MigrateAllAsync();
        var second = await svc.MigrateAllAsync();

        Assert.That(first.BeatsAdded,  Is.EqualTo(6));
        Assert.That(second.BooksAdded, Is.EqualTo(0));
        Assert.That(second.ChaptersAdded, Is.EqualTo(0));
        Assert.That(second.BeatsAdded, Is.EqualTo(0));
        Assert.That(second.JunctionRowsAdded, Is.EqualTo(0));

        await using var db = await dbFactory.CreateDbContextAsync();
        Assert.That(await db.Beats.CountAsync(),       Is.EqualTo(6),  "Re-run must not duplicate beat rows");
        Assert.That(await db.BeatNodes.CountAsync(), Is.EqualTo(6),  "Re-run must not duplicate junction rows");
    }

    [Test]
    public async Task MigrateAll_PicksUpNewChapter_OnReRun()
    {
        // First seed → migrate.
        await SeedLegacyAsync(books: 1, chaptersPerBook: 1, beatsPerChapter: 2);
        await svc.MigrateAllAsync();

        // Add a new chapter to the existing book → migrate again.
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var book = await db.Books.FirstAsync();
            var newChapter = new Chapter { Id = Guid.CreateVersion7(), BookId = book.Id, Number = 2, Title = "Late add", Synopsis = "", Status = "draft" };
            db.Chapters.Add(newChapter);
            db.ChapterBeats.Add(new ChapterBeat
            {
                BeatGuid = Guid.CreateVersion7(), ChapterId = newChapter.Id,
                Index = 0, SortKey = 0, Title = "B", Text = "New beat",
                Act = 1, SceneType = "scene",
            });
            await db.SaveChangesAsync();
        }

        var second = await svc.MigrateAllAsync();
        Assert.That(second.ChaptersAdded, Is.EqualTo(1), "Second run should pick up the freshly-added chapter");
        Assert.That(second.BeatsAdded,    Is.EqualTo(1));
    }
}
