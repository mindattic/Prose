using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix: ContinuityApplyService.KindToEntityType only
/// mapped 8 of 33+ live entity types (character/person, place, faction, corponation, weapon,
/// equipment, technology, cyberware). For every OTHER kind — apparel, automaton, pharmaceutical,
/// genemod, material, subsidiary, transportation, psionic, etc. — ApplyAsync's LocateRecordAsync
/// bailed out immediately with "No Records.Json blob," even though ExtractContinuityFromEntityRecord's
/// InferKindFromEntityType passes those kinds through unchanged (so claim.EntityKind already
/// equals the real EntityType verbatim for them). Fix: fall back to using claim.EntityKind
/// directly when it's not in the dictionary, instead of returning null.
/// </summary>
[TestFixture]
public class ContinuityApplyServiceLocateRecordTests
{
    private string tempRoot = "";
    private IDbContextFactory<ProseDbContext> dbFactory = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-apply-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best effort */ }
    }

    static async Task<Guid> SeedEntityAsync(ProseDbContext db, string entityType, string name)
    {
        var id = Guid.NewGuid();
        db.Entities.Add(new Entity
        {
            Id = id, EntityType = entityType, Name = name, Slug = name.ToLowerInvariant(),
            Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow, IsActive = true,
        });
        db.Records.Add(new Record { EntityId = id, Json = "{}", UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return id;
    }

    [Test]
    public async Task LocateRecordAsync_UnmappedEntityKind_FindsRecordByIdFallback()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = await SeedEntityAsync(db, "apparel", "Reinforced Field Jacket");

        // "apparel" is NOT in KindToEntityType — before the fix this always returned null.
        var claim = new ContinuityClaim { EntityId = id.ToString("N"), EntityKind = "apparel", EntityName = "Reinforced Field Jacket" };

        var record = await ContinuityApplyService.LocateRecordAsync(db, claim, CancellationToken.None);

        Assert.That(record, Is.Not.Null, "an unmapped EntityKind must fall back to using the kind as the raw EntityType, not bail out");
        Assert.That(record!.EntityId, Is.EqualTo(id));
    }

    [Test]
    public async Task LocateRecordAsync_UnmappedEntityKind_FindsRecordByNameFallback()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await SeedEntityAsync(db, "psionic", "Null Static");

        // No parseable EntityId — must fall back to the name route, still using the raw kind.
        var claim = new ContinuityClaim { EntityId = "not-a-guid", EntityKind = "psionic", EntityName = "Null Static" };

        var record = await ContinuityApplyService.LocateRecordAsync(db, claim, CancellationToken.None);

        Assert.That(record, Is.Not.Null);
    }

    [Test]
    public async Task LocateRecordAsync_MappedKind_PersonAliasesToCharacterEntityType()
    {
        // Sanity check the pre-existing mapped path still works: "person" -> "character".
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = await SeedEntityAsync(db, "character", "Rook");

        var claim = new ContinuityClaim { EntityId = id.ToString("N"), EntityKind = "person", EntityName = "Rook" };

        var record = await ContinuityApplyService.LocateRecordAsync(db, claim, CancellationToken.None);

        Assert.That(record, Is.Not.Null);
        Assert.That(record!.EntityId, Is.EqualTo(id));
    }

    [Test]
    public async Task LocateRecordAsync_NoMatchingEntity_ReturnsNull()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var claim = new ContinuityClaim { EntityId = Guid.NewGuid().ToString("N"), EntityKind = "automaton", EntityName = "Nonexistent" };

        var record = await ContinuityApplyService.LocateRecordAsync(db, claim, CancellationToken.None);

        Assert.That(record, Is.Null);
    }
}
