using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Unit tests for <see cref="LocationContradictionService.ScanAsync"/>.
/// All tests run against an in-memory SQLite database — no SQL Server required.
/// </summary>
[TestFixture]
public class LocationContradictionServiceTests
{
    private SqliteConnection conn = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private string tempRoot = null!;
    private LocationContradictionService svc = null!;

    [SetUp]
    public void SetUp()
    {
        conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        dbFactory = new TestFactory(conn);
        using var ctx = dbFactory.CreateDbContext();
        ctx.Database.EnsureCreated();

        tempRoot = Path.Combine(Path.GetTempPath(), "ss-t39-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var findings = new FindingsService(dbFactory, new TestPathProviderWithRoot(tempRoot));
        svc = new LocationContradictionService(dbFactory, findings, NullLogger<LocationContradictionService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        conn.Close();
        conn.Dispose();
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best-effort cleanup */ }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static Guid AddCharacter(ProseDbContext db, string name)
    {
        var id = Guid.CreateVersion7();
        db.Entities.Add(new Entity
        {
            Id = id, EntityType = "character", Name = name,
            Slug = name.ToLowerInvariant().Replace(' ', '-'),
        });
        return id;
    }

    private static Guid AddPlace(ProseDbContext db, string name)
    {
        var id = Guid.CreateVersion7();
        db.Entities.Add(new Entity
        {
            Id = id, EntityType = "place", Name = name,
            Slug = name.ToLowerInvariant().Replace(' ', '-'),
        });
        return id;
    }

    private static void AddLocatedAt(ProseDbContext db, Guid characterId, Guid placeId, DateTime at)
    {
        db.Edges.Add(new Edge
        {
            SourceId = characterId, TargetId = placeId,
            RelationType = "located_at", StoryValidFrom = at,
        });
    }

    // ── empty DB ────────────────────────────────────────────────────────────

    [Test]
    public async Task ScanAsync_EmptyDatabase_ReturnsEmptyResult()
    {
        var result = await svc.ScanAsync();

        Assert.That(result.PresenceFacts, Is.Zero);
        Assert.That(result.CharactersExamined, Is.Zero);
        Assert.That(result.Conflicts, Is.Empty);
        Assert.That(result.StatusNote, Does.Contain("No presence facts found"));
    }

    // ── the core contradiction: one character, two places, too close in time ──

    [Test]
    public async Task ScanAsync_SameCharacterTwoPlacesCloseInTime_FlagsConflict()
    {
        await using var db = dbFactory.CreateDbContext();
        var kyle = AddCharacter(db, "Kyle");
        var chicago = AddPlace(db, "Chicago");
        var rotterdam = AddPlace(db, "Rotterdam");
        var t0 = new DateTime(2230, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        AddLocatedAt(db, kyle, chicago, t0);
        AddLocatedAt(db, kyle, rotterdam, t0.AddMinutes(2)); // well under the 5-min default
        await db.SaveChangesAsync();

        var result = await svc.ScanAsync();

        var conflict = result.Conflicts.Single();
        Assert.That(conflict.CharacterId, Is.EqualTo(kyle));
        Assert.That(conflict.PlaceA, Is.EqualTo("Chicago"));
        Assert.That(conflict.PlaceB, Is.EqualTo("Rotterdam"));
        Assert.That(conflict.Delta, Is.EqualTo(TimeSpan.FromMinutes(2)));
    }

    [Test]
    public async Task ScanAsync_SameCharacterTwoPlacesAtExactlyTheSameInstant_FlagsConflict()
    {
        await using var db = dbFactory.CreateDbContext();
        var kyle = AddCharacter(db, "Kyle");
        var chicago = AddPlace(db, "Chicago");
        var rotterdam = AddPlace(db, "Rotterdam");
        var t0 = new DateTime(2230, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        AddLocatedAt(db, kyle, chicago, t0);
        AddLocatedAt(db, kyle, rotterdam, t0); // identical instant — overlapping
        await db.SaveChangesAsync();

        var result = await svc.ScanAsync();

        Assert.That(result.Conflicts, Has.Count.EqualTo(1));
        Assert.That(result.Conflicts[0].Delta, Is.EqualTo(TimeSpan.Zero));
    }

    // ── non-flagged cases ───────────────────────────────────────────────────

    [Test]
    public async Task ScanAsync_SamePlaceTwice_DoesNotFlag()
    {
        await using var db = dbFactory.CreateDbContext();
        var kyle = AddCharacter(db, "Kyle");
        var chicago = AddPlace(db, "Chicago");
        var t0 = new DateTime(2230, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        AddLocatedAt(db, kyle, chicago, t0);
        AddLocatedAt(db, kyle, chicago, t0.AddMinutes(1)); // same place, no travel needed
        await db.SaveChangesAsync();

        var result = await svc.ScanAsync();

        Assert.That(result.Conflicts, Is.Empty);
        Assert.That(result.PresenceFacts, Is.EqualTo(2));
    }

    [Test]
    public async Task ScanAsync_DifferentCharactersDifferentPlacesSameTime_DoesNotFlag()
    {
        await using var db = dbFactory.CreateDbContext();
        var kyle = AddCharacter(db, "Kyle");
        var pixel = AddCharacter(db, "Pixel");
        var chicago = AddPlace(db, "Chicago");
        var rotterdam = AddPlace(db, "Rotterdam");
        var t0 = new DateTime(2230, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        AddLocatedAt(db, kyle, chicago, t0);
        AddLocatedAt(db, pixel, rotterdam, t0); // different character — not a contradiction
        await db.SaveChangesAsync();

        var result = await svc.ScanAsync();

        Assert.That(result.Conflicts, Is.Empty);
        Assert.That(result.CharactersExamined, Is.EqualTo(2));
    }

    [Test]
    public async Task ScanAsync_SameCharacterTwoPlacesFarApartInTime_DoesNotFlag()
    {
        await using var db = dbFactory.CreateDbContext();
        var kyle = AddCharacter(db, "Kyle");
        var chicago = AddPlace(db, "Chicago");
        var rotterdam = AddPlace(db, "Rotterdam");
        var t0 = new DateTime(2230, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        AddLocatedAt(db, kyle, chicago, t0);
        AddLocatedAt(db, kyle, rotterdam, t0.AddHours(6)); // ample travel time
        await db.SaveChangesAsync();

        var result = await svc.ScanAsync();

        Assert.That(result.Conflicts, Is.Empty);
    }

    // ── minTravelMinutes override ───────────────────────────────────────────

    [Test]
    public async Task ScanAsync_MinTravelMinutesOverride_FlagsAGapTheDefaultWouldAllow()
    {
        await using var db = dbFactory.CreateDbContext();
        var kyle = AddCharacter(db, "Kyle");
        var chicago = AddPlace(db, "Chicago");
        var rotterdam = AddPlace(db, "Rotterdam");
        var t0 = new DateTime(2230, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        AddLocatedAt(db, kyle, chicago, t0);
        AddLocatedAt(db, kyle, rotterdam, t0.AddMinutes(10)); // fine under the default 5-min floor
        await db.SaveChangesAsync();

        var withDefault = await svc.ScanAsync();
        var withOverride = await svc.ScanAsync(minTravelMinutes: 30); // now 10 min is too fast

        Assert.That(withDefault.Conflicts, Is.Empty, "10 minutes clears the default 5-minute floor");
        Assert.That(withOverride.Conflicts, Has.Count.EqualTo(1), "30-minute floor should catch the same 10-minute gap");
    }

    [Test]
    public async Task ScanAsync_MinTravelMinutesOverride_DoesNotMutateTheInstanceDefault()
    {
        await using var db = dbFactory.CreateDbContext();
        var kyle = AddCharacter(db, "Kyle");
        var chicago = AddPlace(db, "Chicago");
        var rotterdam = AddPlace(db, "Rotterdam");
        var t0 = new DateTime(2230, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        AddLocatedAt(db, kyle, chicago, t0);
        AddLocatedAt(db, kyle, rotterdam, t0.AddMinutes(10));
        await db.SaveChangesAsync();

        await svc.ScanAsync(minTravelMinutes: 30);
        var after = await svc.ScanAsync(); // should fall back to the unchanged default of 5

        Assert.That(after.Conflicts, Is.Empty);
        Assert.That(svc.MinTravelMinutes, Is.EqualTo(5));
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
