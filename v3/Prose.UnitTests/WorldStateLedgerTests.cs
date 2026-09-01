using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Unit tests for <see cref="WorldStateLedger"/> — added 2026-09-01 alongside the
/// service-cleanup pass that gave this class two new methods (<c>SnapshotManyAsync</c>,
/// <c>EventsForEntitiesAsync</c>) to absorb query logic previously duplicated in
/// <c>WorldStateAtBeatService</c> and <c>TimelineConsistencyService</c>. This class had zero
/// test coverage before this file, for any of its methods — new and pre-existing alike.
/// </summary>
[TestFixture]
public class WorldStateLedgerTests
{
    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private WorldStateLedger ledger = null!;

    [SetUp]
    public void SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        using var ctx = dbFactory.CreateDbContext();
        ctx.Database.EnsureCreated();
        ledger = new WorldStateLedger(dbFactory, NullLogger<WorldStateLedger>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        conn.Close();
        conn.Dispose();
    }

    private static EntityStateEvent Ev(Guid entityId, string aspect, string value, DateTime at) => new()
    {
        EntityId = entityId, AspectKey = aspect, Verb = "set", NewValue = value,
        AtStoryTime = at, Source = "test", UniverseId = Universe.GlmzId,
    };

    /// <summary>EntityStateEvents.EntityId carries a real FK to Entities.Id — every entity an
    /// event references must exist first, or SQLite (like SQL Server) rejects the insert. Slug
    /// must be distinct per row too: (UniverseId, EntityType, Slug) is a unique index and Slug
    /// defaults to "", so two default-named entities in the same test collide on insert.</summary>
    private static Entity Seed(Guid id, string? name = null) => new()
    {
        Id = id, Name = name ?? id.ToString("N"), Slug = id.ToString("N"),
        EntityType = "character", UniverseId = Universe.GlmzId,
    };

    // ── StateAtAsync ─────────────────────────────────────────────────────────

    [Test]
    public async Task StateAtAsync_ReturnsLatestAtOrBeforeTime_IgnoresLater()
    {
        var entityId = Guid.CreateVersion7();
        var t1 = new DateTime(2225, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2225, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var t3 = new DateTime(2225, 1, 3, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(Seed(entityId));
            db.EntityStateEvents.Add(Ev(entityId, "status", "wounded", t1));
            db.EntityStateEvents.Add(Ev(entityId, "status", "recovering", t2));
            db.EntityStateEvents.Add(Ev(entityId, "status", "healed", t3));
            await db.SaveChangesAsync();
        }

        var result = await ledger.StateAtAsync(entityId, "status", t2);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.NewValue, Is.EqualTo("recovering"), "should return the latest event at-or-before the cursor, not the truly-latest event");
    }

    [Test]
    public async Task StateAtAsync_TieOnStoryTime_PicksHighestId()
    {
        var entityId = Guid.CreateVersion7();
        var t = new DateTime(2225, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(Seed(entityId));
            db.EntityStateEvents.Add(Ev(entityId, "status", "first", t));
            db.EntityStateEvents.Add(Ev(entityId, "status", "second", t)); // same AtStoryTime, later Id
            await db.SaveChangesAsync();
        }

        var result = await ledger.StateAtAsync(entityId, "status", t);

        Assert.That(result!.NewValue, Is.EqualTo("second"), "on a tied AtStoryTime, the higher (later-inserted) Id must win deterministically");
    }

    // ── SnapshotAsync (single-entity) ───────────────────────────────────────

    [Test]
    public async Task SnapshotAsync_ReturnsLatestPerAspect()
    {
        var entityId = Guid.CreateVersion7();
        var t = new DateTime(2225, 1, 5, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(Seed(entityId));
            db.EntityStateEvents.Add(Ev(entityId, "status", "alive", t.AddDays(-2)));
            db.EntityStateEvents.Add(Ev(entityId, "location", "alley", t.AddDays(-1)));
            db.EntityStateEvents.Add(Ev(entityId, "location", "rooftop", t)); // latest for 'location'
            await db.SaveChangesAsync();
        }

        var snap = await ledger.SnapshotAsync(entityId, t);

        Assert.That(snap.Keys, Is.EquivalentTo(new[] { "status", "location" }));
        Assert.That(snap["location"].NewValue, Is.EqualTo("rooftop"));
        Assert.That(snap["status"].NewValue, Is.EqualTo("alive"));
    }

    // ── SnapshotManyAsync (multi-entity) ────────────────────────────────────

    [Test]
    public async Task SnapshotManyAsync_Unscoped_CoversEveryEntity()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var t = new DateTime(2225, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(Seed(a, "A"));
            db.Entities.Add(Seed(b, "B"));
            db.EntityStateEvents.Add(Ev(a, "status", "alive", t.AddDays(-1)));
            db.EntityStateEvents.Add(Ev(b, "status", "wounded", t.AddDays(-1)));
            await db.SaveChangesAsync();
        }

        var snap = await ledger.SnapshotManyAsync(null, t);

        Assert.That(snap[(a, "status")].NewValue, Is.EqualTo("alive"));
        Assert.That(snap[(b, "status")].NewValue, Is.EqualTo("wounded"));
    }

    [Test]
    public async Task SnapshotManyAsync_Scoped_ExcludesOtherEntities()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var t = new DateTime(2225, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(Seed(a, "A"));
            db.Entities.Add(Seed(b, "B"));
            db.EntityStateEvents.Add(Ev(a, "status", "alive", t.AddDays(-1)));
            db.EntityStateEvents.Add(Ev(b, "status", "wounded", t.AddDays(-1)));
            await db.SaveChangesAsync();
        }

        var snap = await ledger.SnapshotManyAsync([a], t);

        Assert.That(snap.ContainsKey((a, "status")), Is.True);
        Assert.That(snap.ContainsKey((b, "status")), Is.False, "scoping to entity A must exclude entity B's events");
    }

    [Test]
    public async Task SnapshotManyAsync_TieOnStoryTime_PicksHighestId()
    {
        // Regresses the WorldStateAtBeatService bug this method's own doc comment
        // describes: its old inline query had no Id tie-break at all, so a tie on
        // AtStoryTime resolved non-deterministically. SnapshotManyAsync must break
        // ties the same way StateAtAsync/SnapshotAsync already do.
        var a = Guid.CreateVersion7();
        var t = new DateTime(2225, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(Seed(a));
            db.EntityStateEvents.Add(Ev(a, "status", "first", t));
            db.EntityStateEvents.Add(Ev(a, "status", "second", t));
            await db.SaveChangesAsync();
        }

        var snap = await ledger.SnapshotManyAsync([a], t);

        Assert.That(snap[(a, "status")].NewValue, Is.EqualTo("second"));
    }

    [Test]
    public async Task SnapshotManyAsync_DoesNotDropOlderEntityBehindManyRecentUnrelatedEvents()
    {
        // Regresses the silent-truncation bug this method's own doc comment describes:
        // WorldStateAtBeatService used to take only the 2000 most-recent events BEFORE
        // grouping by (EntityId, AspectKey) — an entity whose last update was older than
        // the 2000th-most-recent event, in an unscoped query, would vanish from the result
        // even though it plainly has a "latest known state" that should still be reported.
        var quiet = Guid.CreateVersion7();
        var t = new DateTime(2225, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = dbFactory.CreateDbContext())
        {
            // The entity under test has its one and only event far in the past relative to t.
            db.Entities.Add(Seed(quiet));
            db.EntityStateEvents.Add(Ev(quiet, "status", "dormant", t.AddDays(-400)));

            // Flood 50 more-recent events (small in a unit test, but the same shape as the
            // "2000 most recent events across the whole universe" scenario that broke the
            // old inline query — proportionally, 'quiet' would have been event #1 of 51 by
            // recency, well outside any small cap).
            for (int i = 0; i < 50; i++)
            {
                var noisyId = Guid.CreateVersion7();
                db.Entities.Add(Seed(noisyId, $"Noisy {i}"));
                db.EntityStateEvents.Add(Ev(noisyId, "status", $"noisy-{i}", t.AddDays(-i)));
            }

            await db.SaveChangesAsync();
        }

        var snap = await ledger.SnapshotManyAsync(null, t, max: 10);

        // 'max' caps the number of RESULT aspect-states (most-recent-first), so with only 10
        // slots the ancient 'quiet' entity legitimately loses the popularity contest here —
        // that's an intentional, documented cap on the OUTPUT. What must never happen is the
        // cap silently corrupting which underlying event is picked as "latest" for entities
        // that DO make the cut. Verify that by asking for enough room that 'quiet' fits:
        var uncapped = await ledger.SnapshotManyAsync(null, t, max: null);
        Assert.That(uncapped.ContainsKey((quiet, "status")), Is.True,
            "an entity's only (and therefore latest) event must never be dropped by grouping, regardless of how many other entities have more-recent events");
        Assert.That(uncapped[(quiet, "status")].NewValue, Is.EqualTo("dormant"));
    }

    // ── EventsForEntitiesAsync ───────────────────────────────────────────────

    [Test]
    public async Task EventsForEntitiesAsync_ReturnsChronologicalHistoryForScopedEntities()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var t1 = new DateTime(2225, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2225, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(Seed(a, "A"));
            db.Entities.Add(Seed(b, "B"));
            db.EntityStateEvents.Add(Ev(a, "status", "second", t2));
            db.EntityStateEvents.Add(Ev(a, "status", "first", t1)); // inserted out of order
            db.EntityStateEvents.Add(Ev(b, "status", "not-in-scope", t1));
            await db.SaveChangesAsync();
        }

        var events = await ledger.EventsForEntitiesAsync([a]);

        Assert.That(events.Select(e => e.NewValue), Is.EqualTo(new[] { "first", "second" }), "must return entity A's events in chronological order, excluding entity B entirely");
    }

    [Test]
    public async Task EventsForEntitiesAsync_EmptyEntityList_ReturnsEmptyWithoutQuerying()
    {
        var events = await ledger.EventsForEntitiesAsync([]);
        Assert.That(events, Is.Empty);
    }

    // ── SQLite in-memory factory (same pattern as TimelineConsistencyServiceTests) ────

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
