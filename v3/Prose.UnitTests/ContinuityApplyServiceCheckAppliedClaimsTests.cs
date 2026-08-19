using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Phase D of the Bible/Book/Entities validation triangle (2026-08-18): verifies that an
/// already-applied continuity claim (ContinuityApplyService.ApplyAsync sets AppliedAt/
/// AppliedToField) still matches what the entity record currently says. Fully deterministic —
/// CheckAppliedClaimsAsync never touches LlmVotingService, so ContinuityApplyService is
/// constructed with a null voting dependency here (same as ApplyAsync's OTHER helper,
/// LocateRecordAsync, is tested via its static method directly in
/// ContinuityApplyServiceLocateRecordTests without needing a live service instance at all).
/// </summary>
[TestFixture]
public class ContinuityApplyServiceCheckAppliedClaimsTests
{
    private string tempRoot = "";
    private IDbContextFactory<ProseDbContext> dbFactory = null!;
    private ContinuityService store = null!;
    private ContinuityApplyService apply = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-applied-drift-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var paths = new TestPathProviderWithRoot(tempRoot);
        dbFactory = TestDbFactory.For(paths, "nodes");
        store = new ContinuityService(dbFactory);
        apply = new ContinuityApplyService(store, voting: null!, dbFactory, NullLogger<ContinuityApplyService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best effort */ }
    }

    static async Task<Guid> SeedEntityAsync(ProseDbContext db, string entityType, string name, string json)
    {
        var id = Guid.NewGuid();
        db.Entities.Add(new Entity
        {
            Id = id, EntityType = entityType, Name = name, Slug = name.ToLowerInvariant(),
            Status = "canon", CreatedAt = DateTime.UtcNow, ModifiedAt = DateTime.UtcNow,
        });
        db.Records.Add(new Record { EntityId = id, Json = json, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return id;
    }

    async Task<string> SeedAppliedClaimAsync(Guid entityId, string entityName, string predicate, string obj, string field, string bookSlug)
    {
        var claim = new ContinuityClaim
        {
            EntityId = entityId.ToString("N"), EntityName = entityName, EntityKind = "character",
            Predicate = predicate, Object = obj, SourceType = "prose", BookSlug = bookSlug,
            Snippet = "test snippet", Voice = "narrator", Confidence = "high",
            ExtractedBy = new List<string> { "test" },
        };
        var r = store.Upsert(claim);
        store.MarkApplied(r.Claim.ClaimUid, field);
        return r.Claim.ClaimUid;
    }

    [Test]
    public async Task CheckAppliedClaimsAsync_NoAppliedClaims_ReturnsEmpty()
    {
        var result = await apply.CheckAppliedClaimsAsync("some-book");
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task CheckAppliedClaimsAsync_FieldStillMatches_NoDrift()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = await SeedEntityAsync(db, "character", "Rook", """{"hair_color":"dark red"}""");
        await SeedAppliedClaimAsync(id, "Rook", "hair_color", "dark red", "hair_color", "TESTBOOK");

        var result = await apply.CheckAppliedClaimsAsync("TESTBOOK");

        Assert.That(result, Is.Empty, "field value unchanged since apply — no drift expected");
    }

    [Test]
    public async Task CheckAppliedClaimsAsync_ScalarFieldChanged_FlagsValueChanged()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = await SeedEntityAsync(db, "character", "Rook", """{"hair_color":"dark red"}""");
        await SeedAppliedClaimAsync(id, "Rook", "hair_color", "dark red", "hair_color", "TESTBOOK");

        await using (var db2 = await dbFactory.CreateDbContextAsync())
        {
            var rec = await db2.Records.FirstAsync(r => r.EntityId == id);
            rec.Json = """{"hair_color":"platinum blonde"}""";
            await db2.SaveChangesAsync();
        }

        var result = await apply.CheckAppliedClaimsAsync("TESTBOOK");

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Drifted, Is.True);
        Assert.That(result[0].Reason, Is.EqualTo("value_changed"));
    }

    [Test]
    public async Task CheckAppliedClaimsAsync_ArrayValueRemoved_FlagsValueRemovedFromArray()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = await SeedEntityAsync(db, "character", "Rook", """{"tags":["fixer","enforcer"]}""");
        await SeedAppliedClaimAsync(id, "Rook", "tags", "enforcer", "tags", "TESTBOOK");

        await using (var db2 = await dbFactory.CreateDbContextAsync())
        {
            var rec = await db2.Records.FirstAsync(r => r.EntityId == id);
            rec.Json = """{"tags":["fixer"]}""";
            await db2.SaveChangesAsync();
        }

        var result = await apply.CheckAppliedClaimsAsync("TESTBOOK");

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Reason, Is.EqualTo("value_removed_from_array"));
    }

    [Test]
    public async Task CheckAppliedClaimsAsync_FieldDeleted_FlagsFieldRemoved()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = await SeedEntityAsync(db, "character", "Rook", """{"hair_color":"dark red"}""");
        await SeedAppliedClaimAsync(id, "Rook", "hair_color", "dark red", "hair_color", "TESTBOOK");

        await using (var db2 = await dbFactory.CreateDbContextAsync())
        {
            var rec = await db2.Records.FirstAsync(r => r.EntityId == id);
            rec.Json = "{}";
            await db2.SaveChangesAsync();
        }

        var result = await apply.CheckAppliedClaimsAsync("TESTBOOK");

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Reason, Is.EqualTo("field_removed"));
    }

    [Test]
    public async Task CheckAppliedClaimsAsync_ContinuityFactsEntryRemoved_FlagsIt()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = await SeedEntityAsync(db, "character", "Rook", "{}");
        await SeedAppliedClaimAsync(id, "Rook", "weird_fact", "something", "continuity_facts", "TESTBOOK");

        // The record never actually got the continuity_facts[] entry written (this test isn't
        // exercising ApplyAsync itself) — so the "entry present" check correctly finds nothing,
        // exactly as if a real applied entry had later been edited away.
        var result = await apply.CheckAppliedClaimsAsync("TESTBOOK");

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Reason, Is.EqualTo("continuity_facts_entry_removed"));
    }

    [Test]
    public async Task CheckAppliedClaimsAsync_EntityRecordMissing_FlagsIt()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = await SeedEntityAsync(db, "character", "Rook", "{}");
        await SeedAppliedClaimAsync(id, "Rook", "hair_color", "dark red", "hair_color", "TESTBOOK");

        await using (var db2 = await dbFactory.CreateDbContextAsync())
        {
            var rec = await db2.Records.FirstAsync(r => r.EntityId == id);
            db2.Records.Remove(rec);
            await db2.SaveChangesAsync();
        }

        var result = await apply.CheckAppliedClaimsAsync("TESTBOOK");

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Reason, Is.EqualTo("entity_record_missing"));
    }

    [Test]
    public async Task CheckAppliedClaimsAsync_ScopesToBookSlug()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var id = await SeedEntityAsync(db, "character", "Rook", """{"hair_color":"platinum blonde"}""");
        // Claim asserts a value the record no longer has — but tagged to a DIFFERENT book.
        await SeedAppliedClaimAsync(id, "Rook", "hair_color", "dark red", "hair_color", "OTHERBOOK");

        var result = await apply.CheckAppliedClaimsAsync("TESTBOOK");

        Assert.That(result, Is.Empty, "a drifted claim scoped to a different book must not surface here");
    }
}
