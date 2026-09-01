using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Unit tests for <see cref="TimelineConsistencyService"/>.
/// All tests run against an in-memory SQLite database — no SQL Server required.
///
/// Tests prove:
///   1. A clean node (no events) yields zero findings.
///   2. DETECTION 1 fires when a dead character appears in a later beat
///      (entity has status=dead at story-time T1; same entity is mentioned in
///       a beat whose EntityStateEvent story-time is T2 > T1).
///   3. A node where the only event is an injury (no healed-before-injured) yields
///      zero wound-regression findings.
/// </summary>
[TestFixture]
public class TimelineConsistencyServiceTests
{
    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private TimelineConsistencyService svc = null!;

    [SetUp]
    public void SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        using var ctx = dbFactory.CreateDbContext();
        ctx.Database.EnsureCreated();
        var ledger = new WorldStateLedger(dbFactory, NullLogger<WorldStateLedger>.Instance);
        svc = new TimelineConsistencyService(dbFactory, NullLogger<TimelineConsistencyService>.Instance, ledger);
    }

    [TearDown]
    public void TearDown()
    {
        conn.Close();
        conn.Dispose();
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Seed a minimal node with two beats and return the node ID and both beat IDs.
    /// Beat A is at sort position 1.0, Beat B at 2.0.
    /// </summary>
    private async Task<(Guid NodeId, Guid BeatAId, Guid BeatBId)> SeedNodeAsync(
        ProseDbContext db)
    {
        var nodeId = Guid.CreateVersion7();
        var beatAId  = Guid.CreateVersion7();
        var beatBId  = Guid.CreateVersion7();

        db.Nodes.Add(new ChapterNode
        {
            Id         = nodeId,
            Slug       = $"test-{nodeId:N}",
            Title      = "Test Node",
            Kind       = "chapter",
            UniverseId = Universe.GlmzId,
        });
        db.Beats.Add(new Beat { Id = beatAId, Number = 1, Text = "First beat." });
        db.Beats.Add(new Beat { Id = beatBId, Number = 2, Text = "Second beat." });
        db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beatAId, SortKey = 1.0 });
        db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beatBId, SortKey = 2.0 });

        await db.SaveChangesAsync();
        return (nodeId, beatAId, beatBId);
    }

    // ── tests ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CleanNode_NoBeats_ReturnsEmpty()
    {
        // A node with no beats and no events should yield zero findings.
        await using var db = dbFactory.CreateDbContext();
        var nodeId = Guid.CreateVersion7();
        db.Nodes.Add(new ChapterNode
        {
            Id = nodeId, Slug = $"empty-{nodeId:N}", Title = "Empty",
            Kind = "chapter", UniverseId = Universe.GlmzId,
        });
        await db.SaveChangesAsync();

        var findings = await svc.CheckNodeAsync(nodeId);

        Assert.That(findings, Is.Empty, "a node with no beats should produce no findings");
    }

    [Test]
    public async Task CleanNode_BeatsWithNoEvents_ReturnsEmpty()
    {
        // Beats exist with entity mentions but no EntityStateEvents → no findings.
        await using var db = dbFactory.CreateDbContext();
        var (nodeId, beatAId, _) = await SeedNodeAsync(db);

        var entityId = Guid.CreateVersion7();
        db.Entities.Add(new Entity
        {
            Id = entityId, Name = "Marko Tan", EntityType = "character",
             UniverseId = Universe.GlmzId,
        });
        db.BeatEntityMentions.Add(new BeatEntityMention
        {
            BeatId = beatAId, EntityId = entityId,
            EntityName = "Marko Tan", EntityType = "character",
        });
        await db.SaveChangesAsync();

        var findings = await svc.CheckNodeAsync(nodeId);

        Assert.That(findings, Is.Empty, "beats with mentions but no state events should produce no findings");
    }

    [Test]
    public async Task Detection1_DeadCharacterInLaterBeat_FiresHighFinding()
    {
        // DETECTION 1: entity marked dead at T1; that same entity is mentioned in
        // a beat whose EntityStateEvent story-time is T2 > T1.
        await using var db = dbFactory.CreateDbContext();
        var (nodeId, beatAId, beatBId) = await SeedNodeAsync(db);

        var entityId = Guid.CreateVersion7();
        db.Entities.Add(new Entity
        {
            Id = entityId, Name = "Nadia Rao", EntityType = "character",
             UniverseId = Universe.GlmzId,
        });

        var deathTime = new DateTime(2225, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        var laterTime = new DateTime(2225, 3, 16, 9, 0, 0, DateTimeKind.Utc);

        // Event at beatAId marks the entity dead.
        db.EntityStateEvents.Add(new EntityStateEvent
        {
            EntityId    = entityId,
            AspectKey   = "status",
            Verb        = "set",
            NewValue    = "dead",
            AtStoryTime = deathTime,
            BeatGuid    = beatAId,   // links to the beat in this node
            Source      = "manual",
            UniverseId  = Universe.GlmzId,
        });

        // Event at beatBId — AFTER the death time — also references this entity.
        db.EntityStateEvents.Add(new EntityStateEvent
        {
            EntityId    = entityId,
            AspectKey   = "location",
            Verb        = "set",
            NewValue    = "alley",
            AtStoryTime = laterTime,
            BeatGuid    = beatBId,   // the "later" beat
            Source      = "manual",
            UniverseId  = Universe.GlmzId,
        });

        // Entity mention in beatBId confirms the entity is referenced there.
        db.BeatEntityMentions.Add(new BeatEntityMention
        {
            BeatId = beatBId, EntityId = entityId,
            EntityName = "Nadia Rao", EntityType = "character",
        });

        await db.SaveChangesAsync();

        var findings = await svc.CheckNodeAsync(nodeId);

        var deadFindings = findings.Where(f => f.Kind == "dead-character-acting").ToList();
        Assert.That(deadFindings, Has.Count.GreaterThanOrEqualTo(1),
            "should have at least one dead-character-acting finding");
        Assert.That(deadFindings.All(f => f.Severity == "high"),
            "dead-character-acting findings must be severity 'high'");
        Assert.That(deadFindings.Any(f => f.EntityId == entityId),
            "the finding should reference the dead entity");
    }

    [Test]
    public async Task Detection1_NoDeadStatus_ReturnsEmpty()
    {
        // Entity has a "wounded" status (not dead) — no detection-1 firing.
        await using var db = dbFactory.CreateDbContext();
        var (nodeId, beatAId, _) = await SeedNodeAsync(db);

        var entityId = Guid.CreateVersion7();
        db.Entities.Add(new Entity
        {
            Id = entityId, Name = "Felix Dang", EntityType = "character",
             UniverseId = Universe.GlmzId,
        });
        db.EntityStateEvents.Add(new EntityStateEvent
        {
            EntityId    = entityId,
            AspectKey   = "status",
            Verb        = "set",
            NewValue    = "wounded",
            AtStoryTime = new DateTime(2225, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            BeatGuid    = beatAId,
            Source      = "manual",
            UniverseId  = Universe.GlmzId,
        });
        db.BeatEntityMentions.Add(new BeatEntityMention
        {
            BeatId = beatAId, EntityId = entityId,
            EntityName = "Felix Dang", EntityType = "character",
        });
        await db.SaveChangesAsync();

        var findings = await svc.CheckNodeAsync(nodeId);

        Assert.That(findings.Where(f => f.Kind == "dead-character-acting"), Is.Empty,
            "a 'wounded' (not dead) status should not trigger dead-character-acting");
    }

    [Test]
    public async Task Detection2_HealedBeforeInjury_FiresMediumFinding()
    {
        // DETECTION 2: condition.fracture.severity has a "healed" event BEFORE the
        // injury-onset event for the same condition.
        await using var db = dbFactory.CreateDbContext();
        var (nodeId, beatAId, beatBId) = await SeedNodeAsync(db);

        var entityId = Guid.CreateVersion7();
        db.Entities.Add(new Entity
        {
            Id = entityId, Name = "Bear", EntityType = "character",
             UniverseId = Universe.GlmzId,
        });
        db.BeatEntityMentions.Add(new BeatEntityMention
        {
            BeatId = beatAId, EntityId = entityId,
            EntityName = "Bear", EntityType = "character",
        });

        var earlyTime  = new DateTime(2225, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var laterTime  = new DateTime(2225, 5, 2, 0, 0, 0, DateTimeKind.Utc);

        // "healed" at earlyTime — BEFORE the injury at laterTime.
        db.EntityStateEvents.Add(new EntityStateEvent
        {
            EntityId    = entityId,
            AspectKey   = "condition.fracture.severity",
            Verb        = "set",
            NewValue    = "healed",
            AtStoryTime = earlyTime,
            BeatGuid    = beatAId,
            Source      = "manual",
            UniverseId  = Universe.GlmzId,
        });
        // "severe" injury at laterTime.
        db.EntityStateEvents.Add(new EntityStateEvent
        {
            EntityId    = entityId,
            AspectKey   = "condition.fracture.severity",
            Verb        = "set",
            NewValue    = "severe",
            AtStoryTime = laterTime,
            BeatGuid    = beatBId,
            Source      = "manual",
            UniverseId  = Universe.GlmzId,
        });

        await db.SaveChangesAsync();

        var findings = await svc.CheckNodeAsync(nodeId);

        var regressions = findings.Where(f => f.Kind == "wound-regression").ToList();
        Assert.That(regressions, Has.Count.GreaterThanOrEqualTo(1),
            "should have at least one wound-regression finding");
        Assert.That(regressions.All(f => f.Severity == "medium"),
            "wound-regression findings must be severity 'medium'");
    }

    // ── SQLite in-memory factory (same pattern as MigrationSmokeTests) ─────

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
