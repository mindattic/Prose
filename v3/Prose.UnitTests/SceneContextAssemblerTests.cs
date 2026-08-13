using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>Throws if ever actually called — <see cref="SceneContextAssembler.FilterToBeatUniverseAsync"/>
/// never touches embeddings, so a real HTTP client is never needed to construct the assembler.</summary>
file sealed class NeverCalledHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => throw new InvalidOperationException("Should not be called by this test.");
}

/// <summary>
/// Regression cover for the 2026-08-10 fix: <c>BeatEntities</c> has a PRIMARY KEY on
/// (BeatId, EntityId), but a character can legitimately match a beat's text twice in one
/// <see cref="SceneContextAssembler.AssembleAsync"/> call — once via their canonical Name,
/// once via a registered alias both present in the same passage. A corpus-wide
/// <c>--backfill-entity-presence</c> run crashed on exactly this the day first-name
/// aliases were bulk-added for 105 characters. Fixed via
/// <see cref="SceneContextAssembler.DedupeByEntityId"/>. Tested directly (not through
/// PersistRosterAsync, which uses SQL Server-only DDL the SQLite test fixture can't run).
/// </summary>
[TestFixture]
public class SceneContextAssemblerTests
{
    [Test]
    public void DedupeByEntityId_CollapsesDuplicateEntityMatches_KeepingHighestScore()
    {
        var id = Guid.NewGuid();
        var roster = new List<SceneEntityRef>
        {
            new(id, "Yemina Fola", "character", "name", 3.0),
            new(id, "Yemina", "character", "name", 1.5),
        };

        var deduped = SceneContextAssembler.DedupeByEntityId(roster);

        Assert.That(deduped, Has.Count.EqualTo(1));
        Assert.That(deduped[0].EntityId, Is.EqualTo(id));
        Assert.That(deduped[0].Score, Is.EqualTo(3.0), "the higher-scoring match must survive");
    }

    [Test]
    public void DedupeByEntityId_PreservesDistinctEntities()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var roster = new List<SceneEntityRef>
        {
            new(a, "Idris Kovac", "character", "name", 3.0),
            new(b, "Bishop Alaoui", "character", "name", 3.0),
        };

        var deduped = SceneContextAssembler.DedupeByEntityId(roster);

        Assert.That(deduped, Has.Count.EqualTo(2));
        Assert.That(deduped.Select(r => r.EntityId), Is.EquivalentTo(new[] { a, b }));
    }

    [Test]
    public void DedupeByEntityId_EmptyRoster_ReturnsEmpty()
    {
        var deduped = SceneContextAssembler.DedupeByEntityId(new List<SceneEntityRef>());
        Assert.That(deduped, Is.Empty);
    }
}

/// <summary>
/// Regression cover for the 2026-08-13 defense-in-depth fix (plan "Prose, objectively...").
/// A corpus-wide scan earlier this project found 788 rows in one book (VIGL) where the roster/POV
/// write path had persisted an entity from a DIFFERENT universe than the beat's own book — the
/// specific historical bug (a backfill CLI silently ignoring `--universe` scope) was already
/// fixed, and the live matching pipeline (embedding search, name-index scan) is already correctly
/// scoped by the ambient EF query filter. This is the second, independent check added at the exact
/// write path that produced the historical bad data (<see cref="SceneContextAssembler.PersistRosterAsync"/>/
/// <see cref="SceneContextAssembler.PersistPovAsync"/>): even if the ambient scope were ever wrong
/// again, a mismatched entity gets dropped and logged, not silently written.
/// </summary>
[TestFixture]
public class SceneContextAssemblerUniverseGuardTests
{
    private string tempRoot = "";
    private TestPathProviderWithRoot paths = null!;
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private SceneContextAssembler assembler = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-scenecontext-universe-guard-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "universe-guard");

        var findings = new FindingsService(dbFactory, paths);
        var wounds = new WoundLedgerService(dbFactory, NullLogger<WoundLedgerService>.Instance);
        var disambiguation = new EntityDisambiguationService(dbFactory, NullLogger<EntityDisambiguationService>.Instance);
        var embedding = new EmbeddingService(dbFactory, new SettingsService(tempRoot), new NeverCalledHttpClientFactory(), NullLogger<EmbeddingService>.Instance);
        assembler = new SceneContextAssembler(dbFactory, embedding, new ThrowingLlmService(), findings, wounds, disambiguation, NullLogger<SceneContextAssembler>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        TestDbFactory.Reset(paths);
        try { Directory.Delete(tempRoot, recursive: true); } catch { }
    }

    private async Task<(Guid BeatId, Guid MatchingEntityId, Guid MismatchedEntityId)> SeedCrossUniverseScenarioAsync()
    {
        var beatUniverse = Guid.CreateVersion7();
        var otherUniverse = Guid.CreateVersion7();

        await using var db = await dbFactory.CreateDbContextAsync();

        var node = NodeFactory.Create("book");
        node.Slug = "b-" + Guid.NewGuid().ToString("N")[..8];
        node.Title = "T"; node.Status = "draft"; node.SortKey = 100; node.UniverseId = beatUniverse;
        db.Nodes.Add(node);

        var beat = new Beat { Id = Guid.CreateVersion7(), Number = 1, Text = "Some prose." };
        db.Beats.Add(beat);
        db.BeatNodes.Add(new BeatNode { NodeId = node.Id, BeatId = beat.Id, SortKey = 1 });

        var matching = new Entity { Id = Guid.CreateVersion7(), UniverseId = beatUniverse, EntityType = "character", Name = "Same-Universe Character", Slug = "s-" + Guid.NewGuid().ToString("N")[..8] };
        var mismatched = new Entity { Id = Guid.CreateVersion7(), UniverseId = otherUniverse, EntityType = "character", Name = "Other-Universe Character", Slug = "o-" + Guid.NewGuid().ToString("N")[..8] };
        db.Entities.AddRange(matching, mismatched);

        await db.SaveChangesAsync();
        return (beat.Id, matching.Id, mismatched.Id);
    }

    [Test]
    public async Task FilterToBeatUniverseAsync_DropsEntityFromDifferentUniverse()
    {
        var (beatId, matchingId, mismatchedId) = await SeedCrossUniverseScenarioAsync();
        var candidates = new List<SceneEntityRef>
        {
            new(matchingId, "Same-Universe Character", "character", "name", 3.0),
            new(mismatchedId, "Other-Universe Character", "character", "name", 5.0),
        };

        await using var db = await dbFactory.CreateDbContextAsync();
        var kept = await assembler.FilterToBeatUniverseAsync(db, beatId, candidates, CancellationToken.None);

        Assert.That(kept.Select(r => r.EntityId), Is.EquivalentTo(new[] { matchingId }),
            "the cross-universe entity must be dropped even though it scored higher");
    }

    [Test]
    public async Task FilterToBeatUniverseAsync_NoMismatch_KeepsEverything()
    {
        var (beatId, matchingId, _) = await SeedCrossUniverseScenarioAsync();
        var candidates = new List<SceneEntityRef> { new(matchingId, "Same-Universe Character", "character", "name", 3.0) };

        await using var db = await dbFactory.CreateDbContextAsync();
        var kept = await assembler.FilterToBeatUniverseAsync(db, beatId, candidates, CancellationToken.None);

        Assert.That(kept, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task FilterToBeatUniverseAsync_EmptyCandidates_ReturnsEmpty()
    {
        var (beatId, _, _) = await SeedCrossUniverseScenarioAsync();
        await using var db = await dbFactory.CreateDbContextAsync();
        var kept = await assembler.FilterToBeatUniverseAsync(db, beatId, new List<SceneEntityRef>(), CancellationToken.None);
        Assert.That(kept, Is.Empty);
    }
}
