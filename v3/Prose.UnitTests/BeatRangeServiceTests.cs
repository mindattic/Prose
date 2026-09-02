using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Unit tests for <see cref="BeatRangeService"/> (2026-09-02) — the beat-scoped Edge validity
/// mechanism that replaces the dead DateTime StoryValidFrom/StoryValidUntil path (see that
/// service's own doc comment for the investigation). Covers the tri-state contract: real
/// bool for a same-book, non-anachrony window; null (indeterminate) for a cross-book bound or a
/// flagged anachrony beat.
/// </summary>
[TestFixture]
public class BeatRangeServiceTests
{
    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private BeatRangeService svc = null!;

    [SetUp]
    public void SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        using var ctx = dbFactory.CreateDbContext();
        ctx.Database.EnsureCreated();
        svc = new BeatRangeService(dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        conn.Close();
        conn.Dispose();
    }

    private static int nextNumberBase = 0;

    /// <summary>Builds a book with one chapter and <paramref name="count"/> beats, in reading
    /// order (SortKey = index * 100). Beat.Number is globally unique (a real DB constraint) —
    /// offset per call so tests that build more than one book don't collide.</summary>
    private static async Task<(Guid BookId, List<Beat> Beats)> BuildBookAsync(IDbContextFactory<ProseDbContext> dbFactory, int count)
    {
        var numberBase = Interlocked.Add(ref nextNumberBase, 1000);

        await using var db = await dbFactory.CreateDbContextAsync();
        var book = new BookNode { Id = Guid.NewGuid(), Slug = "book-" + Guid.NewGuid().ToString("N")[..8], Title = "Book" };
        var chapter = new ChapterNode { Id = Guid.NewGuid(), ParentNodeId = book.Id, Slug = "ch-" + Guid.NewGuid().ToString("N")[..8], Title = "Chapter 1" };
        db.Nodes.AddRange(book, chapter);

        var beats = Enumerable.Range(0, count).Select(i => new Beat { Id = Guid.NewGuid(), Number = numberBase + i, Text = $"beat {i}" }).ToList();
        db.Beats.AddRange(beats);
        for (var i = 0; i < beats.Count; i++)
            db.BeatNodes.Add(new BeatNode { NodeId = chapter.Id, BeatId = beats[i].Id, SortKey = i * 100 });

        await db.SaveChangesAsync();
        return (book.Id, beats);
    }

    [Test]
    public async Task Unbounded_IsAlwaysInRange()
    {
        var (_, beats) = await BuildBookAsync(dbFactory, 5);
        var result = await svc.CheckBeatInRangeAsync(beats[2].Id, null, null);
        Assert.That(result.InRange, Is.True);
    }

    [Test]
    public async Task WithinFromUntilWindow_IsTrue()
    {
        var (_, beats) = await BuildBookAsync(dbFactory, 10);
        // Window is [beats[3], beats[7]) — beats[5] is inside.
        var result = await svc.CheckBeatInRangeAsync(beats[5].Id, beats[3].Id, beats[7].Id);
        Assert.That(result.InRange, Is.True);
    }

    [Test]
    public async Task BeforeFromBound_IsFalse()
    {
        var (_, beats) = await BuildBookAsync(dbFactory, 10);
        var result = await svc.CheckBeatInRangeAsync(beats[1].Id, beats[3].Id, null);
        Assert.That(result.InRange, Is.False);
    }

    [Test]
    public async Task AtOrAfterUntilBound_IsFalse()
    {
        var (_, beats) = await BuildBookAsync(dbFactory, 10);
        // ValidUntilBeatId is exclusive — the bound beat itself is already out of range.
        var result = await svc.CheckBeatInRangeAsync(beats[7].Id, null, beats[7].Id);
        Assert.That(result.InRange, Is.False);
    }

    [Test]
    public async Task FromBoundInclusive_IsTrue()
    {
        var (_, beats) = await BuildBookAsync(dbFactory, 10);
        var result = await svc.CheckBeatInRangeAsync(beats[3].Id, beats[3].Id, null);
        Assert.That(result.InRange, Is.True);
    }

    [Test]
    public async Task CrossBookBound_IsIndeterminate()
    {
        var (_, bookA) = await BuildBookAsync(dbFactory, 5);
        var (_, bookB) = await BuildBookAsync(dbFactory, 5);

        var result = await svc.CheckBeatInRangeAsync(bookA[2].Id, bookB[0].Id, null);

        Assert.That(result.InRange, Is.Null);
        Assert.That(result.Reason, Does.Contain("different book"));
    }

    [Test]
    public async Task FlaggedAnachronyBeat_IsIndeterminate()
    {
        var (bookId, beats) = await BuildBookAsync(dbFactory, 10);

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var blueprint = new NodeStructuralBlueprint { Id = Guid.NewGuid(), NodeId = bookId, UniverseId = Guid.NewGuid() };
            db.NodeStructuralBlueprints.Add(blueprint);
            db.BeatBlueprintDecisions.Add(new BeatBlueprintDecision
            {
                Id = Guid.NewGuid(),
                BeatId = beats[5].Id,
                BlueprintId = blueprint.Id,
                AnachronyType = "Flashback",
            });
            await db.SaveChangesAsync();
        }

        var result = await svc.CheckBeatInRangeAsync(beats[5].Id, beats[3].Id, beats[7].Id);

        Assert.That(result.InRange, Is.Null);
        Assert.That(result.Reason, Does.Contain("anachrony"));
    }

    [Test]
    public async Task LinearAnachronyType_IsTreatedAsOrdinary()
    {
        // AnachronyType == "Linear" is the explicit non-anachrony value (BeatBlueprintDecision's
        // own doc comment: "Linear | Flashback | FlashForward | Parallel") — must not trip the
        // indeterminate path the way an actual flagged anachrony does.
        var (bookId, beats) = await BuildBookAsync(dbFactory, 10);

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var blueprint = new NodeStructuralBlueprint { Id = Guid.NewGuid(), NodeId = bookId, UniverseId = Guid.NewGuid() };
            db.NodeStructuralBlueprints.Add(blueprint);
            db.BeatBlueprintDecisions.Add(new BeatBlueprintDecision
            {
                Id = Guid.NewGuid(),
                BeatId = beats[5].Id,
                BlueprintId = blueprint.Id,
                AnachronyType = "Linear",
            });
            await db.SaveChangesAsync();
        }

        var result = await svc.CheckBeatInRangeAsync(beats[5].Id, beats[3].Id, beats[7].Id);
        Assert.That(result.InRange, Is.True);
    }

    // ── SQLite in-memory factory (same pattern as WorldStateAtBeatServiceTests) ──────
    private sealed class TestFactory : IDbContextFactory<ProseDbContext>
    {
        private readonly DbContextOptions<ProseDbContext> opts;
        public TestFactory(SqliteConnection conn)
        {
            opts = new DbContextOptionsBuilder<ProseDbContext>()
                .UseSqlite(conn)
                .Options;
        }
        public ProseDbContext CreateDbContext() => new(opts);
        public Task<ProseDbContext> CreateDbContextAsync(CancellationToken ct = default)
            => Task.FromResult(CreateDbContext());
    }
}
