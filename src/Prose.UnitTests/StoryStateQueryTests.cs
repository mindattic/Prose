using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Composition.Ledger;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Unit tests for <see cref="StoryStateQuery"/> — the v4 read path that bounds what the writer
/// believes is true to the entities actually on screen in one beat (see the class doc comment
/// and the v4 plan, §2). Covers the four behaviors the ticket calls out: scoping to beat
/// entities + place, the StoryPosition as-of cutoff, latest-value-per-aspect resolution, and
/// obligation scoping — plus the unknown-beat and empty-input edge cases.
/// </summary>
[TestFixture]
public class StoryStateQueryTests
{
    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private StoryStateQuery svc = null!;

    [SetUp]
    public void SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        using var ctx = dbFactory.CreateDbContext();
        ctx.Database.EnsureCreated();
        svc = new StoryStateQuery(dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        conn.Close();
        conn.Dispose();
    }

    // ── fixture helpers ──────────────────────────────────────────────────────

    private static Guid AddBook(ProseDbContext db, Guid id)
    {
        db.Nodes.Add(new BookNode { Id = id, UniverseId = Universe.GlmzId, Slug = $"book-{id:N}", Title = "Test Book" });
        return id;
    }

    private static Guid AddEntity(ProseDbContext db, string name, string entityType = "character")
    {
        var id = Guid.CreateVersion7();
        db.Entities.Add(new Entity
        {
            Id = id, Name = name, Slug = id.ToString("N"), EntityType = entityType, UniverseId = Universe.GlmzId,
        });
        return id;
    }

    private static Guid AddBeat(ProseDbContext db, int number, string text, int? storyPosition, Guid? placeEntityId = null)
    {
        var beat = new Beat
        {
            Id = Guid.CreateVersion7(), Number = number, Text = text, TextHash = Beat.ComputeHash(text),
            Kind = "prose", SceneType = "scene", StoryPosition = storyPosition, PlaceEntityId = placeEntityId,
        };
        db.Beats.Add(beat);
        return beat.Id;
    }

    private static void AddEvent(
        ProseDbContext db, Guid entityId, string aspect, string value, Guid? beatGuid, DateTime atStoryTime)
    {
        db.EntityStateEvents.Add(new EntityStateEvent
        {
            EntityId = entityId, AspectKey = aspect, Verb = "set", NewValue = value,
            BeatGuid = beatGuid, AtStoryTime = atStoryTime, Source = "extracted", UniverseId = Universe.GlmzId,
        });
    }

    private static void AddClaim(
        ProseDbContext db, string entityId, string entityName, string predicate, string value, string status)
    {
        db.ContinuityClaims.Add(new ContinuityClaim
        {
            ClaimUid = Guid.NewGuid().ToString("N"), EntityId = entityId, EntityName = entityName,
            Predicate = predicate, Object = value, Status = status, SourceType = "test",
        });
    }

    private static Guid AddObligation(
        ProseDbContext db, Guid nodeId, Guid? entityId, string state, string description = "pays off later")
    {
        var id = Guid.CreateVersion7();
        db.NarrativeObligations.Add(new NarrativeObligation
        {
            Id = id, NodeId = nodeId, EntityId = entityId, State = state, Description = description,
            DedupKey = id.ToString("N"),
        });
        return id;
    }

    private static readonly DateTime T0 = new(2230, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── unknown beat / empty input ───────────────────────────────────────────

    [Test]
    public async Task GetOnScreenFactsAsync_UnknownBeat_ReturnsEmptySnapshot()
    {
        var snap = await svc.GetOnScreenFactsAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.That(snap.IsEmpty, Is.True);
        Assert.That(snap.EntityIds, Is.Empty);
        Assert.That(snap.EntityStateFacts, Is.Empty);
        Assert.That(snap.ContinuityFacts, Is.Empty);
        Assert.That(snap.OpenObligations, Is.Empty);
    }

    [Test]
    public async Task GetOnScreenFactsAsync_BeatWithNoTagsNoPlaceNoMentions_ReturnsEmptySnapshot()
    {
        Guid beatId;
        await using (var db = dbFactory.CreateDbContext())
        {
            beatId = AddBeat(db, 1, "Plain prose with no entity tags at all.", storyPosition: 1);
            await db.SaveChangesAsync();
        }

        var snap = await svc.GetOnScreenFactsAsync(Guid.NewGuid(), beatId);

        Assert.That(snap.IsEmpty, Is.True);
    }

    [Test]
    public async Task GetOnScreenFactsForEntitiesAsync_EmptyEntityList_ReturnsEmptySnapshotWithoutQuerying()
    {
        var snap = await svc.GetOnScreenFactsForEntitiesAsync(Guid.NewGuid(), [], asOfStoryPosition: 100);

        Assert.That(snap.IsEmpty, Is.True);
    }

    // ── scoping to beat entities + place ─────────────────────────────────────

    [Test]
    public async Task GetOnScreenFactsAsync_ScopesToTaggedEntitiesPlusPlace_ExcludesEveryoneElse()
    {
        var nodeId = Guid.NewGuid();
        Guid a, place, bystander, beatId;

        await using (var db = dbFactory.CreateDbContext())
        {
            AddBook(db, nodeId);
            a = AddEntity(db, "Declan Doyle");
            place = AddEntity(db, "The Shelf", entityType: "place");
            bystander = AddEntity(db, "Someone Else Entirely");
            beatId = AddBeat(db, 1, $"""<entity guid="{a}">Declan Doyle</entity> walked in.""", storyPosition: 5, placeEntityId: place);

            AddEvent(db, a, "status", "on screen", beatId, T0);
            AddEvent(db, place, "ambience", "quiet", beatId, T0);
            AddEvent(db, bystander, "status", "never mentioned here", beatId, T0);
            await db.SaveChangesAsync();
        }

        var snap = await svc.GetOnScreenFactsAsync(nodeId, beatId);

        Assert.That(snap.EntityIds, Is.EquivalentTo(new[] { a, place }));
        Assert.That(snap.EntityStateFacts.Select(f => f.EntityName), Is.EquivalentTo(new[] { "Declan Doyle", "The Shelf" }));
    }

    [Test]
    public async Task GetOnScreenFactsAsync_PlaceAlsoTaggedInText_IsNotDuplicatedInEntityIds()
    {
        var nodeId = Guid.NewGuid();
        Guid a, beatId;

        await using (var db = dbFactory.CreateDbContext())
        {
            AddBook(db, nodeId);
            a = AddEntity(db, "Declan Doyle");
            beatId = AddBeat(db, 1, $"""<entity guid="{a}">Declan Doyle</entity> walked in.""", storyPosition: 1, placeEntityId: a);
            await db.SaveChangesAsync();
        }

        var snap = await svc.GetOnScreenFactsAsync(nodeId, beatId);

        Assert.That(snap.EntityIds, Is.EqualTo(new[] { a }));
    }

    [Test]
    public async Task GetOnScreenFactsAsync_FallsBackToBeatEntityMentions_WhenTextHasNoTags()
    {
        var nodeId = Guid.NewGuid();
        Guid a, beatId;

        await using (var db = dbFactory.CreateDbContext())
        {
            AddBook(db, nodeId);
            a = AddEntity(db, "Declan Doyle");
            beatId = AddBeat(db, 1, "Declan Doyle walked in, untagged.", storyPosition: 1);
            db.BeatEntityMentions.Add(new BeatEntityMention { BeatId = beatId, EntityId = a, EntityName = "Declan Doyle", EntityType = "character" });
            await db.SaveChangesAsync();
        }

        var snap = await svc.GetOnScreenFactsAsync(nodeId, beatId);

        Assert.That(snap.EntityIds, Is.EqualTo(new[] { a }));
    }

    // ── StoryPosition as-of cutoff ────────────────────────────────────────────

    [Test]
    public async Task GetOnScreenFactsAsync_ExcludesEventsFromBeatsAfterTheTargetStoryPosition()
    {
        var nodeId = Guid.NewGuid();
        Guid a, earlyBeat, targetBeat, lateBeat;

        await using (var db = dbFactory.CreateDbContext())
        {
            AddBook(db, nodeId);
            a = AddEntity(db, "Declan Doyle");
            earlyBeat = AddBeat(db, 1, "setup", storyPosition: 1);
            targetBeat = AddBeat(db, 2, $"""<entity guid="{a}">Declan Doyle</entity> is here.""", storyPosition: 3);
            lateBeat = AddBeat(db, 3, "later", storyPosition: 5);

            AddEvent(db, a, "status", "wounded", earlyBeat, T0);
            AddEvent(db, a, "status", "recovered", lateBeat, T0.AddHours(1));
            await db.SaveChangesAsync();
        }

        var snap = await svc.GetOnScreenFactsAsync(nodeId, targetBeat);

        var status = snap.EntityStateFacts.Single(f => f.Predicate == "status");
        Assert.That(status.Value, Is.EqualTo("wounded"),
            "the event tied to beat #3 (StoryPosition 5) is after the target beat's StoryPosition 3 and must not be visible");
    }

    [Test]
    public async Task GetOnScreenFactsAsync_IncludesEventExactlyAtTheTargetStoryPosition()
    {
        var nodeId = Guid.NewGuid();
        Guid a, targetBeat;

        await using (var db = dbFactory.CreateDbContext())
        {
            AddBook(db, nodeId);
            a = AddEntity(db, "Declan Doyle");
            targetBeat = AddBeat(db, 1, $"""<entity guid="{a}">Declan Doyle</entity> is here.""", storyPosition: 3);

            AddEvent(db, a, "status", "current", targetBeat, T0);
            await db.SaveChangesAsync();
        }

        var snap = await svc.GetOnScreenFactsAsync(nodeId, targetBeat);

        Assert.That(snap.EntityStateFacts.Single(f => f.Predicate == "status").Value, Is.EqualTo("current"));
    }

    [Test]
    public async Task GetOnScreenFactsAsync_EventWithNoBeatGuid_IsAlwaysVisible()
    {
        var nodeId = Guid.NewGuid();
        Guid a, targetBeat;

        await using (var db = dbFactory.CreateDbContext())
        {
            AddBook(db, nodeId);
            a = AddEntity(db, "Declan Doyle");
            targetBeat = AddBeat(db, 1, $"""<entity guid="{a}">Declan Doyle</entity> is here.""", storyPosition: 1);

            AddEvent(db, a, "manual_note", "hand-entered, no originating beat", beatGuid: null, atStoryTime: T0.AddYears(50));
            await db.SaveChangesAsync();
        }

        var snap = await svc.GetOnScreenFactsAsync(nodeId, targetBeat);

        Assert.That(snap.EntityStateFacts.Single(f => f.Predicate == "manual_note").Value,
            Is.EqualTo("hand-entered, no originating beat"));
    }

    // ── latest value per aspect wins ──────────────────────────────────────────

    [Test]
    public async Task GetOnScreenFactsForEntitiesAsync_LatestByStoryPosition_NotByWallClockAtStoryTime_Wins()
    {
        var nodeId = Guid.NewGuid();
        var a = Guid.CreateVersion7();
        Guid earlyBeat, laterBeat;

        await using (var db = dbFactory.CreateDbContext())
        {
            AddBook(db, nodeId);
            db.Entities.Add(new Entity { Id = a, Name = "Declan Doyle", Slug = a.ToString("N"), EntityType = "character", UniverseId = Universe.GlmzId });
            earlyBeat = AddBeat(db, 1, "early", storyPosition: 2);
            laterBeat = AddBeat(db, 2, "later", storyPosition: 4);

            // A backfill: the early beat's fact is written to the ledger LATER (later AtStoryTime,
            // the system wall clock), but it still describes the EARLIER point in the story.
            AddEvent(db, a, "location", "the docks", earlyBeat, atStoryTime: T0.AddDays(10));
            AddEvent(db, a, "location", "the clinic", laterBeat, atStoryTime: T0);
            await db.SaveChangesAsync();
        }

        var snap = await svc.GetOnScreenFactsForEntitiesAsync(nodeId, [a], asOfStoryPosition: 10);

        Assert.That(snap.EntityStateFacts.Single(f => f.Predicate == "location").Value, Is.EqualTo("the clinic"),
            "StoryPosition 4 is the later point in the story even though its AtStoryTime (wall-clock backfill) is earlier");
    }

    [Test]
    public async Task GetOnScreenFactsForEntitiesAsync_MultipleAspectsPerEntity_EachResolvesIndependently()
    {
        var nodeId = Guid.NewGuid();
        var a = Guid.CreateVersion7();
        Guid beat1, beat2;

        await using (var db = dbFactory.CreateDbContext())
        {
            AddBook(db, nodeId);
            db.Entities.Add(new Entity { Id = a, Name = "Declan Doyle", Slug = a.ToString("N"), EntityType = "character", UniverseId = Universe.GlmzId });
            beat1 = AddBeat(db, 1, "one", storyPosition: 1);
            beat2 = AddBeat(db, 2, "two", storyPosition: 2);

            AddEvent(db, a, "location", "the docks", beat1, T0);
            AddEvent(db, a, "intent", "find the ledger", beat2, T0);
            await db.SaveChangesAsync();
        }

        var snap = await svc.GetOnScreenFactsForEntitiesAsync(nodeId, [a], asOfStoryPosition: 2);

        Assert.That(snap.EntityStateFacts, Has.Count.EqualTo(2));
        Assert.That(snap.EntityStateFacts.Single(f => f.Predicate == "location").Value, Is.EqualTo("the docks"));
        Assert.That(snap.EntityStateFacts.Single(f => f.Predicate == "intent").Value, Is.EqualTo("find the ledger"));
    }

    // ── continuity facts ───────────────────────────────────────────────────

    [Test]
    public async Task GetOnScreenFactsForEntitiesAsync_ContinuityFacts_OnlyCanonicalAndConfirmed_ScopedToOnScreen()
    {
        var nodeId = Guid.NewGuid();
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();

        await using (var db = dbFactory.CreateDbContext())
        {
            AddBook(db, nodeId);
            AddClaim(db, a.ToString(), "Declan Doyle", "carries", "a katana", "CANONICAL");
            AddClaim(db, a.ToString("N"), "Declan Doyle", "wears", "a long coat", "CONFIRMED");
            AddClaim(db, a.ToString(), "Declan Doyle", "might-carry", "a spare clip", "NEW");
            AddClaim(db, a.ToString(), "Declan Doyle", "rejected-claim", "a cape", "REJECTED");
            AddClaim(db, b.ToString(), "Bystander", "carries", "a torch", "CANONICAL");
            await db.SaveChangesAsync();
        }

        var snap = await svc.GetOnScreenFactsForEntitiesAsync(nodeId, [a], asOfStoryPosition: 1);

        Assert.That(snap.ContinuityFacts.Select(f => f.Predicate), Is.EquivalentTo(new[] { "carries", "wears" }),
            "only CANONICAL/CONFIRMED claims for on-screen entities; the N-form EntityId must match too");
    }

    // ── obligations ────────────────────────────────────────────────────────

    [Test]
    public async Task GetOnScreenFactsForEntitiesAsync_Obligations_OnlyOpenOrAdvanced_ScopedToNodeAndOnScreenEntity()
    {
        var nodeId = Guid.NewGuid();
        var otherNodeId = Guid.NewGuid();
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();

        await using (var db = dbFactory.CreateDbContext())
        {
            AddBook(db, nodeId);
            AddBook(db, otherNodeId);
            db.Entities.Add(new Entity { Id = a, Name = "Declan Doyle", Slug = a.ToString("N"), EntityType = "character", UniverseId = Universe.GlmzId });
            db.Entities.Add(new Entity { Id = b, Name = "Bystander", Slug = b.ToString("N"), EntityType = "character", UniverseId = Universe.GlmzId });
            await db.SaveChangesAsync();

            AddObligation(db, nodeId, a, ObligationState.Open, "the gun will be found");
            AddObligation(db, nodeId, a, ObligationState.Advanced, "the wound keeps bleeding");
            AddObligation(db, nodeId, a, ObligationState.Closed, "already paid off");
            AddObligation(db, nodeId, a, ObligationState.Dropped, "author dropped this");
            AddObligation(db, otherNodeId, a, ObligationState.Open, "a different book's promise");
            AddObligation(db, nodeId, b, ObligationState.Open, "not on screen");
            AddObligation(db, nodeId, null, ObligationState.Open, "no entity to scope to");
            await db.SaveChangesAsync();
        }

        var snap = await svc.GetOnScreenFactsForEntitiesAsync(nodeId, [a], asOfStoryPosition: 1);

        Assert.That(snap.OpenObligations.Select(o => o.Description),
            Is.EquivalentTo(new[] { "the gun will be found", "the wound keeps bleeding" }));
    }

    // ── SQLite in-memory factory (same pattern as WorldStateAtBeatServiceTests) ────

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
