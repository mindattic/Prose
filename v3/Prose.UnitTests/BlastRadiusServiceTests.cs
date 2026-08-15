using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Operationalizes docs/LOGIC.md's "blast radius" concept (2026-08-14) — previously prose with
/// zero code behind it. Verifies both halves: same-chapter windowing by SortKey, and the
/// cross-book entity-sharing self-join over BeatEntityPresence (a raw-SQL-only table with no EF
/// mapping — this test creates it ad hoc in the SQLite fixture since EnsureCreated() only builds
/// EF-mapped tables).
/// </summary>
[TestFixture]
public class BlastRadiusServiceTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private BlastRadiusService svc = null!;

    [SetUp]
    public async Task SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-blastradius-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        svc = new BlastRadiusService(dbFactory);

        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS BeatEntityPresence (BeatId TEXT NOT NULL, EntityId TEXT NOT NULL, EntityName TEXT NOT NULL, PresenceType TEXT NOT NULL)");
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
    }

    private static Beat MakeBeat(int number, string text = "text") => new()
    {
        Id = Guid.NewGuid(), Number = number, Text = text,
    };

    [Test]
    public async Task GetBlastRadiusBeatIdsAsync_IncludesSameChapterWindow()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var chapter = new ChapterNode { Id = Guid.NewGuid(), Slug = "ch1-" + Guid.NewGuid().ToString("N")[..8], Title = "Chapter 1" };
        db.Nodes.Add(chapter);

        // 7 beats in reading order; edit beat[3] (middle). Default window is +/-3, so all 7
        // should be included via the chapter-window half alone.
        var beats = Enumerable.Range(0, 7).Select(i => MakeBeat(i)).ToList();
        db.Beats.AddRange(beats);
        for (int i = 0; i < beats.Count; i++)
            db.BeatNodes.Add(new BeatNode { NodeId = chapter.Id, BeatId = beats[i].Id, SortKey = i * 100 });
        await db.SaveChangesAsync();

        var result = await svc.GetBlastRadiusBeatIdsAsync(beats[3].Id);

        Assert.That(result, Is.EquivalentTo(beats.Select(b => b.Id)));
    }

    [Test]
    public async Task GetBlastRadiusBeatIdsAsync_RespectsNarrowerWindow()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var chapter = new ChapterNode { Id = Guid.NewGuid(), Slug = "ch2-" + Guid.NewGuid().ToString("N")[..8], Title = "Chapter 2" };
        db.Nodes.Add(chapter);

        var beats = Enumerable.Range(0, 9).Select(i => MakeBeat(i)).ToList();
        db.Beats.AddRange(beats);
        for (int i = 0; i < beats.Count; i++)
            db.BeatNodes.Add(new BeatNode { NodeId = chapter.Id, BeatId = beats[i].Id, SortKey = i * 100 });
        await db.SaveChangesAsync();

        // Edit the middle beat (index 4) with a window of 1 — expect indices 3,4,5 only.
        var result = await svc.GetBlastRadiusBeatIdsAsync(beats[4].Id, chapterWindow: 1);

        Assert.That(result, Is.EquivalentTo(new[] { beats[3].Id, beats[4].Id, beats[5].Id }));
    }

    [Test]
    public async Task GetBlastRadiusBeatIdsAsync_IncludesBeatsSharingAnEntity_AcrossChapters()
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var chapterA = new ChapterNode { Id = Guid.NewGuid(), Slug = "cha-" + Guid.NewGuid().ToString("N")[..8], Title = "Chapter A" };
        var chapterB = new ChapterNode { Id = Guid.NewGuid(), Slug = "chb-" + Guid.NewGuid().ToString("N")[..8], Title = "Chapter B" };
        db.Nodes.AddRange(chapterA, chapterB);

        var editedBeat  = MakeBeat(1);
        var farBeat     = MakeBeat(2);   // shares the entity, but in a DIFFERENT chapter — window alone would miss it
        var unrelatedBeat = MakeBeat(3); // no shared entity, different chapter — must be excluded
        db.Beats.AddRange(editedBeat, farBeat, unrelatedBeat);
        db.BeatNodes.Add(new BeatNode { NodeId = chapterA.Id, BeatId = editedBeat.Id, SortKey = 100 });
        db.BeatNodes.Add(new BeatNode { NodeId = chapterB.Id, BeatId = farBeat.Id, SortKey = 100 });
        db.BeatNodes.Add(new BeatNode { NodeId = chapterB.Id, BeatId = unrelatedBeat.Id, SortKey = 200 });
        await db.SaveChangesAsync();

        // Insert with the Guid CLR value itself (not pre-stringified) so EF binds it through the
        // SAME provider-specific conversion the service's own SqlQueryRaw({0}, guid) uses to read
        // it back — a manual .ToString() here previously mismatched Sqlite's own Guid parameter
        // representation and made the join silently match zero rows.
        var sharedEntityId = Guid.NewGuid();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO BeatEntityPresence (BeatId, EntityId, EntityName, PresenceType) VALUES ({editedBeat.Id}, {sharedEntityId}, 'Orim Zebulun', 'pov')");
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO BeatEntityPresence (BeatId, EntityId, EntityName, PresenceType) VALUES ({farBeat.Id}, {sharedEntityId}, 'Orim Zebulun', 'present-active')");

        var result = await svc.GetBlastRadiusBeatIdsAsync(editedBeat.Id, chapterWindow: 0);

        Assert.That(result, Does.Contain(editedBeat.Id));
        Assert.That(result, Does.Contain(farBeat.Id), "a beat sharing an entity in a different chapter must be included");
        Assert.That(result, Does.Not.Contain(unrelatedBeat.Id));
    }

    [Test]
    public async Task GetBlastRadiusBeatIdsAsync_UnknownBeat_ReturnsEmpty()
    {
        var result = await svc.GetBlastRadiusBeatIdsAsync(Guid.NewGuid());
        Assert.That(result, Is.Empty);
    }
}
