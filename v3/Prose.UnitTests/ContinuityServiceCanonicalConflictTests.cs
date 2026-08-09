using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-09 fix: a new claim contradicting an already-CANONICAL
/// fact used to be silently inserted as plain "NEW" — CANONICAL was excluded from both the
/// conflict-detection query in Upsert and the "live" status arrays in
/// GetContradictionGroups/GetContradictionGroupsSince — so a post-resolution contradiction
/// was invisible everywhere. The winning CANONICAL claim must never be demoted by this;
/// the new, conflicting claim is the one flagged CONTRADICTED.
/// </summary>
[TestFixture]
public class ContinuityServiceCanonicalConflictTests
{
    private string tempRoot = "";
    private ContinuityService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-continuity-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var paths = new TestPathProviderWithRoot(tempRoot);
        var dbFactory = TestDbFactory.For(paths, "nodes");
        svc = new ContinuityService(dbFactory);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best effort */ }
    }

    static ContinuityClaim Claim(string entityId, string entityName, string predicate, string obj) => new()
    {
        EntityId = entityId,
        EntityName = entityName,
        EntityKind = "character",
        Predicate = predicate,
        Object = obj,
        SourceType = "test",
        ExtractedBy = ["test"],
    };

    [Test]
    public void NewClaim_ContradictingCanonical_IsFlaggedNotSilentlyInserted()
    {
        var entityId = Guid.NewGuid().ToString("N");

        var original = svc.Upsert(Claim(entityId, "Rook", "eye_color", "green"));
        Assert.That(original.Outcome, Is.EqualTo("NEW"));

        svc.MakeCanonical(original.Claim!.ClaimUid);
        var canonical = svc.GetByEntity(entityId).Single(c => c.ClaimUid == original.Claim.ClaimUid);
        Assert.That(canonical.Status, Is.EqualTo("CANONICAL"));

        // A later extraction asserts a DIFFERENT eye color for the same entity+predicate.
        var result = svc.Upsert(Claim(entityId, "Rook", "eye_color", "blue"));

        Assert.That(result.Outcome, Is.EqualTo("CONTRADICTED"),
            "a claim conflicting with an already-CANONICAL fact must be flagged, not inserted as plain NEW");
        Assert.That(result.Conflict, Is.Not.Null);
        Assert.That(result.Conflict!.Status, Is.EqualTo("CANONICAL"),
            "the canonical claim must NEVER be demoted by a new conflicting extraction — " +
            "only a human resolving it again should change its status");
    }

    [Test]
    public void GetContradictionGroups_SurfacesPostCanonicalConflict()
    {
        var entityId = Guid.NewGuid().ToString("N");

        var original = svc.Upsert(Claim(entityId, "Rook", "eye_color", "green"));
        svc.MakeCanonical(original.Claim!.ClaimUid);
        svc.Upsert(Claim(entityId, "Rook", "eye_color", "blue"));

        var groups = svc.GetContradictionGroups();
        var group = groups.FirstOrDefault(g => g.EntityId == entityId && g.Predicate == "eye_color");

        Assert.That(group, Is.Not.Null,
            "a post-canonical contradiction must surface in the contradiction-groups view, " +
            "not disappear because CANONICAL was excluded from the 'live' status set");
        Assert.That(group!.Claims.Select(c => c.Object), Does.Contain("green").And.Contain("blue"));
    }

    [Test]
    public void NewClaim_MatchingCanonical_IsConfirmedNotContradicted()
    {
        // Sanity check: re-asserting the SAME value as the canonical fact must not be
        // treated as a conflict — this exercises the "existing != null && IsActive" path,
        // not the conflict-query path, and must keep working after the fix.
        var entityId = Guid.NewGuid().ToString("N");
        var original = svc.Upsert(Claim(entityId, "Rook", "eye_color", "green"));
        svc.MakeCanonical(original.Claim!.ClaimUid);

        var result = svc.Upsert(Claim(entityId, "Rook", "eye_color", "green"));

        Assert.That(result.Outcome, Is.EqualTo("CONFIRMED"));
        Assert.That(result.Claim!.Status, Is.EqualTo("CANONICAL"),
            "re-confirming the same value must not demote the canonical status");
    }
}
