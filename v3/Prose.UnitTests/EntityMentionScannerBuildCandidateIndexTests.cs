using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-19 fix: <see cref="EntityMentionScanner.BuildCandidateIndexAsync"/>
/// only ever read <c>CharacterAliases</c> — every other entity type's own alias bridge table
/// (<c>PlaceAliases</c>, <c>FactionAliases</c>, <c>CorponationCommonNames</c>, ...) was silently
/// never consulted. Found live: "ArcSec" was already registered as a Corponation CommonName on
/// "Arcturus Defense Solutions," yet three independent tagging passes across three different books
/// still reported it unmatched, because this method had no code path to ever see it.
/// </summary>
[TestFixture]
public class EntityMentionScannerBuildCandidateIndexTests
{
    private string tempRoot = "";
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private Guid universeId;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-scanner-candidate-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        universeId = Guid.NewGuid();
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best effort */ }
    }

    [Test]
    public async Task BuildCandidateIndexAsync_PlaceAlias_IsIncludedAsCandidate()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = id, UniverseId = universeId, EntityType = "place", Name = "Arcturus Defense Solutions HQ", Slug = "adshq", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Places.Add(new Place { Id = id, Name = "Arcturus Defense Solutions HQ" });
        db.PlaceAliases.Add(new PlaceAlias { PlaceId = id, Position = 0, Value = "The Spire" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        Assert.That(candidates.Any(c => c.Text == "The Spire" && c.EntityId == id && c.EntityType == "place"), Is.True,
            "a registered PlaceAlias must appear as a taggable candidate");
    }

    [Test]
    public async Task BuildCandidateIndexAsync_FactionAlias_IsIncludedAsCandidate()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = id, UniverseId = universeId, EntityType = "faction", Name = "Neuretic Crime Investigation Division", Slug = "ncid", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Factions.Add(new Faction { Id = id, Name = "Neuretic Crime Investigation Division" });
        db.FactionAliases.Add(new FactionAlias { FactionId = id, Position = 0, Value = "NCID" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        Assert.That(candidates.Any(c => c.Text == "NCID" && c.EntityId == id && c.EntityType == "faction"), Is.True,
            "a registered FactionAlias must appear as a taggable candidate");
    }

    [Test]
    public async Task BuildCandidateIndexAsync_CorponationCommonName_IsIncludedAsCandidate()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = id, UniverseId = universeId, EntityType = "corponation", Name = "Arcturus Defense Solutions", Slug = "arcturus-defense", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Corponations.Add(new Corponation { Id = id, Name = "Arcturus Defense Solutions" });
        db.CorponationCommonNames.Add(new CorponationCommonName { CorponationId = id, Position = 0, Value = "ArcSec" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        Assert.That(candidates.Any(c => c.Text == "ArcSec" && c.EntityId == id && c.EntityType == "corponation"), Is.True,
            "a registered CorponationCommonName must appear as a taggable candidate — this is the exact live case that surfaced the bug");
    }

    [Test]
    public async Task BuildCandidateIndexAsync_ShortAlias_IsExcludedAcrossAllTypes()
    {
        // The >=3-char guard must apply uniformly to the new alias sources too, not just Character.
        await using var db = await dbFactory.CreateDbContextAsync();
        var placeId = Guid.NewGuid();
        db.Entities.Add(new Entity { Id = placeId, UniverseId = universeId, EntityType = "place", Name = "Some Place", Slug = "some-place", Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow });
        db.Places.Add(new Place { Id = placeId, Name = "Some Place" });
        db.PlaceAliases.Add(new PlaceAlias { PlaceId = placeId, Position = 0, Value = "Sp" });
        await db.SaveChangesAsync();

        var candidates = await EntityMentionScanner.BuildCandidateIndexAsync(db, universeId, bookNodeId: null);

        Assert.That(candidates.Any(c => c.Text == "Sp"), Is.False);
    }
}
