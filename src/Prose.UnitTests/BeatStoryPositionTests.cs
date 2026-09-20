using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// <c>Beat.StoryPosition</c> — beat order as the authoritative story clock, and the
/// <see cref="WorldStateAtBeatService"/> defect it exists to fix.
///
/// <para><b>Author ruling 2026-09-04.</b> "We are supposed to be using beats not a clock counting
/// hours and minutes; that still should be there to keep night and day aligned but I guess it has
/// to support both; if a bomb has a 20 minute timer then the timer needs to align but in the case
/// of owning then losing a motorcycle that can be tracked by beats."</para>
///
/// <para><b>The defect this closes.</b> A beat had no position on any timeline, so asked for the
/// world state at a beat with no extracted events of its own — which the service's own comments
/// document as the normal case while drafting forward — it fell back to the most recent story-time
/// fact ANYWHERE in the universe. For the beat at the front of the draft that is a reasonable
/// proxy. For any earlier beat it silently returns state from the END of the book and presents it
/// as the answer. The test below pins exactly that: an early beat must not see a later beat's
/// world.</para>
/// </summary>
[TestFixture]
public class BeatStoryPositionTests
{
    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private WorldStateAtBeatService worldState = null!;

    private static readonly DateTime Early = new(2226, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Late = new(2226, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    private Guid bookId, chapterId, firstBeat, lastBeat, kyle;

    [SetUp]
    public async Task SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        await using var db = dbFactory.CreateDbContext();
        await db.Database.EnsureCreatedAsync();

        bookId = Guid.CreateVersion7();
        chapterId = Guid.CreateVersion7();
        firstBeat = Guid.CreateVersion7();
        lastBeat = Guid.CreateVersion7();
        kyle = Guid.CreateVersion7();

        db.Nodes.Add(new BookNode
        {
            Id = bookId, Slug = "clock-book", NodeCode = "CLK", Title = "Clock Book",
            Kind = "book", UniverseId = Universe.GlmzId,
        });
        db.Nodes.Add(new ChapterNode
        {
            Id = chapterId, Slug = "clock-book-ch1", Title = "Chapter 1 — Order",
            Kind = "chapter", ParentNodeId = bookId, UniverseId = Universe.GlmzId,
        });

        void AddBeat(Guid id, int number, double sort, string text)
        {
            db.Beats.Add(new Beat { Id = id, Number = number, Text = text, TextHash = Beat.ComputeHash(text) });
            db.BeatNodes.Add(new BeatNode { NodeId = chapterId, BeatId = id, SortKey = sort });
        }
        AddBeat(firstBeat, 1, 1, "He still had the motorcycle.");
        AddBeat(Guid.CreateVersion7(), 2, 2, "A middle beat.");
        AddBeat(lastBeat, 3, 3, "The motorcycle was long gone.");

        db.Entities.Add(new Entity
        {
            Id = kyle, Name = "Kyle", Slug = kyle.ToString("N"),
            EntityType = "character", UniverseId = Universe.GlmzId,
        });

        // The whole point: an event that happens LATE in the book. An early beat must never see it.
        db.EntityStateEvents.Add(new EntityStateEvent
        {
            EntityId = kyle, AspectKey = "motorcycle", Verb = "set", NewValue = "lost",
            AtStoryTime = Late, BeatGuid = lastBeat, Source = "test", UniverseId = Universe.GlmzId,
        });
        db.EntityStateEvents.Add(new EntityStateEvent
        {
            EntityId = kyle, AspectKey = "motorcycle", Verb = "set", NewValue = "owned",
            AtStoryTime = Early, BeatGuid = firstBeat, Source = "test", UniverseId = Universe.GlmzId,
        });

        await db.SaveChangesAsync();

        worldState = new WorldStateAtBeatService(
            dbFactory, new WorldStateLedger(dbFactory, NullLogger<WorldStateLedger>.Instance));
    }

    [TearDown]
    public void TearDown() { conn.Close(); conn.Dispose(); }

    private BeatStoryPositionService BuildStamper()
    {
        var workbench = new NodeWorkbenchService(
            dbFactory, null!, null!, null!, NullLogger<NodeWorkbenchService>.Instance,
            null!, null!, null!, null!, null!);
        return new BeatStoryPositionService(
            dbFactory, workbench, NullLogger<BeatStoryPositionService>.Instance);
    }

    // ── stamping ─────────────────────────────────────────────────────────────

    [Test]
    public async Task Stamp_NumbersEveryBeatInReadingOrder_OneBased()
    {
        var result = await BuildStamper().StampBookAsync(bookId, apply: true);

        Assert.That(result.Beats, Is.EqualTo(3));
        Assert.That(result.Changed, Is.EqualTo(3));

        await using var db = dbFactory.CreateDbContext();
        var positions = await db.Beats.OrderBy(b => b.Number)
            .Select(b => b.StoryPosition).ToListAsync();
        Assert.That(positions, Is.EqualTo(new int?[] { 1, 2, 3 }),
            "positions are 1-based and dense over reading order — null would mean unknown");
    }

    [Test]
    public async Task Stamp_IsIdempotent_AndDryRunWritesNothing()
    {
        var svc = BuildStamper();
        await svc.StampBookAsync(bookId, apply: true);

        var second = await svc.StampBookAsync(bookId, apply: true);
        Assert.That(second.Changed, Is.Zero, "re-running must be free — reading order did not change");

        var dry = await svc.StampBookAsync(bookId, apply: false);
        Assert.That(dry.Changed, Is.Zero);
    }

    [Test]
    public async Task Stamp_DoesNotMarkBeatsDirty()
    {
        // A position is bookkeeping about where a beat sits, not an edit to its prose. Bumping
        // UpdatedAt/TextHash/Stale here would invalidate every hash-gated audit in the engine
        // (craft checklist, verification, continuity extraction) for no content change at all.
        DateTime before;
        string? hashBefore;
        await using (var db = dbFactory.CreateDbContext())
        {
            var b = await db.Beats.FirstAsync(x => x.Id == firstBeat);
            before = b.UpdatedAt;
            hashBefore = b.TextHash;
        }

        await BuildStamper().StampBookAsync(bookId, apply: true);

        await using (var db = dbFactory.CreateDbContext())
        {
            var b = await db.Beats.AsNoTracking().FirstAsync(x => x.Id == firstBeat);
            Assert.That(b.StoryPosition, Is.EqualTo(1));
            Assert.That(b.UpdatedAt, Is.EqualTo(before), "stamping must not look like a prose edit");
            Assert.That(b.TextHash, Is.EqualTo(hashBefore));
            Assert.That(b.Stale, Is.False);
        }
    }

    [Test]
    public async Task Coverage_ReportsTheCeiling()
    {
        var svc = BuildStamper();
        var (total, stampedBefore) = await svc.CoverageAsync();
        Assert.That(total, Is.EqualTo(3));
        Assert.That(stampedBefore, Is.Zero);

        await svc.StampBookAsync(bookId, apply: true);

        var (_, stampedAfter) = await svc.CoverageAsync();
        Assert.That(stampedAfter, Is.EqualTo(3),
            "coverage has to be visible, or a consumer falling back on nulls looks like one that " +
            "found the right answer");
    }

    // ── the defect ───────────────────────────────────────────────────────────

    [Test]
    public async Task WorldStateAtEarlyBeat_DoesNotSeeTheEndOfTheBook()
    {
        await BuildStamper().StampBookAsync(bookId, apply: true);

        var snap = await worldState.SnapshotAsync(firstBeat);

        var bike = snap.EntityStates.SingleOrDefault(s => s.AspectKey == "motorcycle");
        Assert.That(bike, Is.Not.Null, "the early beat's own state should still resolve");
        Assert.That(bike!.Value, Is.EqualTo("owned"),
            "at beat 1 he still has the motorcycle; 'lost' happens at beat 3 and must be invisible here");
        Assert.That(snap.StoryTime, Is.EqualTo(Early));
    }

    [Test]
    public async Task WorldStateAtLaterBeat_SeesEverythingUpToIt()
    {
        await BuildStamper().StampBookAsync(bookId, apply: true);

        var snap = await worldState.SnapshotAsync(lastBeat);

        var bike = snap.EntityStates.Single(s => s.AspectKey == "motorcycle");
        Assert.That(bike.Value, Is.EqualTo("lost"), "the duration is a difference of two positions");
    }

    [Test]
    public async Task UnstampedBeat_FallsBackRatherThanTreatingNullAsTheStart()
    {
        // No stamping run at all. A null position means UNKNOWN, so the old universe-wide proxy is
        // still the best available answer — what it must NOT do is read null as position 0 and
        // report the pre-story world as fact.
        var snap = await worldState.SnapshotAsync(firstBeat);

        Assert.That(snap.StoryTime, Is.Not.Null,
            "an unstamped beat still resolves, via the documented fallback");
    }

    private sealed class TestFactory(SqliteConnection conn) : IDbContextFactory<ProseDbContext>
    {
        private readonly DbContextOptions<ProseDbContext> opts =
            new DbContextOptionsBuilder<ProseDbContext>().UseSqlite(conn).Options;
        public ProseDbContext CreateDbContext() => new(opts);
        public Task<ProseDbContext> CreateDbContextAsync(CancellationToken ct = default)
            => Task.FromResult(CreateDbContext());
    }
}
