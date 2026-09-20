using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Unit tests for <see cref="WorldStateAtBeatService"/> — added 2026-09-01. This service had
/// zero test coverage before this file. Focused on the two real behavioral changes from
/// delegating its EntityStateEvents query to the new <see cref="WorldStateLedger.SnapshotManyAsync"/>
/// (2026-09-01 consolidation): a deterministic tie-break on AtStoryTime, and no more silent
/// truncation of an older entity's state behind a flood of more-recent unrelated events.
/// </summary>
[TestFixture]
public class WorldStateAtBeatServiceTests
{
    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private WorldStateAtBeatService svc = null!;

    [SetUp]
    public void SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        using var ctx = dbFactory.CreateDbContext();
        ctx.Database.EnsureCreated();
        var ledger = new WorldStateLedger(dbFactory, NullLogger<WorldStateLedger>.Instance);
        svc = new WorldStateAtBeatService(dbFactory, ledger);
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

    // Slug must be distinct per row: (UniverseId, EntityType, Slug) is a unique index and Slug
    // defaults to "", so two entities in the same test would otherwise collide on insert.
    private static Entity Person(Guid id, string name) => new()
    {
        Id = id, Name = name, Slug = id.ToString("N"), EntityType = "character", UniverseId = Universe.GlmzId,
    };

    [Test]
    public async Task SnapshotAsync_ExplicitStoryTime_ReturnsLatestStatePerEntity()
    {
        var kyle = Guid.CreateVersion7();
        var t = new DateTime(2225, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(Person(kyle, "Kyle"));
            db.EntityStateEvents.Add(Ev(kyle, "status", "wounded", t.AddDays(-2)));
            db.EntityStateEvents.Add(Ev(kyle, "status", "recovering", t.AddDays(-1)));
            await db.SaveChangesAsync();
        }

        // Scoped explicitly: this test is about picking the latest value per aspect, not about
        // how an unscoped call chooses its scope (see CouldNotLook_* below for that contract).
        var snap = await svc.SnapshotAsync(Guid.CreateVersion7(), storyTime: t, entityIds: [kyle]);

        var kyleStatus = snap.EntityStates.Single(s => s.EntityId == kyle && s.AspectKey == "status");
        Assert.That(kyleStatus.Value, Is.EqualTo("recovering"));
        Assert.That(kyleStatus.EntityName, Is.EqualTo("Kyle"));
    }

    [Test]
    public async Task SnapshotAsync_TieOnStoryTime_PicksHighestIdDeterministically()
    {
        var kyle = Guid.CreateVersion7();
        var t = new DateTime(2225, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(Person(kyle, "Kyle"));
            db.EntityStateEvents.Add(Ev(kyle, "status", "first", t));
            db.EntityStateEvents.Add(Ev(kyle, "status", "second", t)); // same AtStoryTime, later Id
            await db.SaveChangesAsync();
        }

        var snap = await svc.SnapshotAsync(Guid.CreateVersion7(), storyTime: t, entityIds: [kyle]);

        var kyleStatus = snap.EntityStates.Single(s => s.EntityId == kyle && s.AspectKey == "status");
        Assert.That(kyleStatus.Value, Is.EqualTo("second"),
            "before the 2026-09-01 consolidation this service's own inline query had no Id tie-break — a tied AtStoryTime resolved non-deterministically instead of picking the later-inserted row");
    }

    [Test]
    public async Task SnapshotAsync_DoesNotDropOlderEntityBehindManyRecentUnrelatedEvents()
    {
        // Regresses the silent-truncation bug fixed by delegating to
        // WorldStateLedger.SnapshotManyAsync: this service used to cap the most-recent events by
        // AtStoryTime BEFORE grouping by (EntityId, AspectKey). An entity whose only/latest event
        // was older than the cutoff vanished from the snapshot entirely, even though it has a
        // perfectly well-defined "latest known state."
        var quiet = Guid.CreateVersion7();
        var t = new DateTime(2225, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var everyone = new List<Guid> { quiet };

        await using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(Person(quiet, "Quiet One"));
            db.EntityStateEvents.Add(Ev(quiet, "status", "dormant", t.AddDays(-400)));

            for (int i = 0; i < 50; i++)
            {
                var noisyId = Guid.CreateVersion7();
                everyone.Add(noisyId);
                db.Entities.Add(Person(noisyId, $"Noisy {i}"));
                db.EntityStateEvents.Add(Ev(noisyId, "status", $"noisy-{i}", t.AddDays(-i)));
            }

            await db.SaveChangesAsync();
        }

        // The whole cast in scope: the quiet one is the OLDEST event of the 51, so if the cap is
        // ever re-applied before grouping it is the first row to disappear.
        var snap = await svc.SnapshotAsync(Guid.CreateVersion7(), storyTime: t, entityIds: everyone);

        var quietState = snap.EntityStates.SingleOrDefault(s => s.EntityId == quiet);
        Assert.That(quietState, Is.Not.Null, "an entity's latest known state must survive regardless of how many other entities have more-recent events");
        Assert.That(quietState!.Value, Is.EqualTo("dormant"));
    }

    // ── the scope contract itself (2026-09-05) ───────────────────────────────

    [Test]
    public async Task SnapshotAsync_Unscoped_WithNoEntityTagsInTheChapter_SaysItCouldNotLook()
    {
        // The load-bearing half of the 2026-09-05 change. Omitting entityIds no longer means
        // "the whole universe" — it derives scope from the entities tagged in the beat's own
        // chapter. When there is nothing to derive from, the snapshot must SAY it could not
        // look, because an empty result that never got to look is not the same answer as an
        // empty result that looked and found nothing. Nothing else in the suite pins this.
        var kyle = Guid.CreateVersion7();
        var t = new DateTime(2225, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(Person(kyle, "Kyle"));
            db.EntityStateEvents.Add(Ev(kyle, "status", "wounded", t.AddDays(-1)));
            await db.SaveChangesAsync();
        }

        var snap = await svc.SnapshotAsync(Guid.CreateVersion7(), storyTime: t);

        Assert.That(snap.EntityStates, Is.Empty);
        Assert.That(snap.Scope, Does.Contain("COULD NOT LOOK"),
            "an unscoped snapshot with nothing to scope to must announce that it could not look, "
            + "not quietly return zero facts that read like 'nothing is true here'");
    }

    [Test]
    public async Task SnapshotAsync_ScopedToEntityIds_ExcludesOthers()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var t = new DateTime(2225, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = dbFactory.CreateDbContext())
        {
            db.Entities.Add(Person(a, "A"));
            db.Entities.Add(Person(b, "B"));
            db.EntityStateEvents.Add(Ev(a, "status", "alive", t.AddDays(-1)));
            db.EntityStateEvents.Add(Ev(b, "status", "alive", t.AddDays(-1)));
            await db.SaveChangesAsync();
        }

        var snap = await svc.SnapshotAsync(Guid.CreateVersion7(), storyTime: t, entityIds: [a]);

        Assert.That(snap.EntityStates.Any(s => s.EntityId == a), Is.True);
        Assert.That(snap.EntityStates.Any(s => s.EntityId == b), Is.False);
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
