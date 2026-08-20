using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-19 Trinity Reconciliation full-corpus run: 19 of 88
/// contradiction groups had a losing claim whose underlying prose/bible edit was refused (snippet
/// not found verbatim, or the safety guard rejected the rewrite), but MakeCanonical's original
/// blanket "reject every live sibling" call still marked those losers REJECTED — permanently
/// hiding a still-uncorrected fact from ever resurfacing, since GetContradictionGroups only
/// re-forms a group from claims in the "live" status set. MakeCanonical's new
/// onlyRejectClaimUids parameter lets a caller reject only the claims whose edit actually landed,
/// leaving the rest at their current live status.
/// </summary>
[TestFixture]
public class ContinuityServicePartialRejectTests
{
    private string tempRoot = "";
    private ContinuityService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-continuity-partial-tests-" + Guid.NewGuid().ToString("N"));
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
    public void MakeCanonical_WithOnlyRejectClaimUids_LeavesUnlistedSiblingLive()
    {
        var entityId = Guid.NewGuid().ToString("N");
        var winner = svc.Upsert(Claim(entityId, "Sable", "role", "fixer")).Claim!;
        var editedLoser = svc.Upsert(Claim(entityId, "Sable", "role", "contractor")).Claim!;
        var unresolvedLoser = svc.Upsert(Claim(entityId, "Sable", "role", "broker")).Claim!;
        var unresolvedLoserStatusBefore = svc.GetByEntity(entityId).First(c => c.ClaimUid == unresolvedLoser.ClaimUid).Status;

        svc.MakeCanonical(winner.ClaimUid, "test", onlyRejectClaimUids: new HashSet<string> { editedLoser.ClaimUid });

        var byUid = svc.GetByEntity(entityId).ToDictionary(c => c.ClaimUid);
        Assert.That(byUid[winner.ClaimUid].Status, Is.EqualTo("CANONICAL"));
        Assert.That(byUid[editedLoser.ClaimUid].Status, Is.EqualTo("REJECTED"),
            "the claim whose edit actually landed should be rejected");
        Assert.That(byUid[unresolvedLoser.ClaimUid].Status, Is.EqualTo(unresolvedLoserStatusBefore),
            "a claim whose edit was refused must stay at its current live status, not be marked resolved");
        Assert.That(byUid[unresolvedLoser.ClaimUid].Status, Is.Not.EqualTo("REJECTED"));
    }

    [Test]
    public void MakeCanonical_WithOnlyRejectClaimUids_UnresolvedSiblingStillFormsContradictionGroup()
    {
        var entityId = Guid.NewGuid().ToString("N");
        var winner = svc.Upsert(Claim(entityId, "Sable", "role", "fixer")).Claim!;
        var unresolvedLoser = svc.Upsert(Claim(entityId, "Sable", "role", "broker")).Claim!;

        svc.MakeCanonical(winner.ClaimUid, "test", onlyRejectClaimUids: new HashSet<string>());

        var group = svc.GetContradictionGroups().FirstOrDefault(g => g.EntityId == entityId && g.Predicate == "role");
        Assert.That(group, Is.Not.Null,
            "the unresolved loser must keep contradicting the now-CANONICAL winner so the next " +
            "reconciliation pass retries the edit, instead of silently vanishing");
        Assert.That(group!.Claims.Select(c => c.ClaimUid), Does.Contain(unresolvedLoser.ClaimUid));
    }

    [Test]
    public void MakeCanonical_WithNullRejectSet_KeepsOriginalBlanketBehavior()
    {
        var entityId = Guid.NewGuid().ToString("N");
        var winner = svc.Upsert(Claim(entityId, "Sable", "role", "fixer")).Claim!;
        var loser = svc.Upsert(Claim(entityId, "Sable", "role", "broker")).Claim!;

        svc.MakeCanonical(winner.ClaimUid, "test");

        Assert.That(svc.GetByEntity(entityId).First(c => c.ClaimUid == loser.ClaimUid).Status, Is.EqualTo("REJECTED"));
    }

    // ── Re-resolution demotes a previously-CANONICAL sibling ─────────────────
    // Regression cover for a second bug found the same day, during the first real
    // --reconcile-trinity --all run: a non-deterministic panel re-vote on the same
    // (EntityId, Predicate) can pick a DIFFERENT winner than a prior run did. Before this fix,
    // MakeCanonical's sibling-demotion query excluded CANONICAL from its "live" status list, so the
    // OLD winner was never touched — leaving two simultaneously-CANONICAL claims with different
    // Object values for the same key, forever re-surfacing as a false contradiction on every future
    // sweep since nothing ever resolves it. Live instance: Breckenridge.background flipped between
    // "ex-Arcturus" and "ex-Arcturus Defense Solutions" across two runs this session.

    [Test]
    public void MakeCanonical_CalledAgainWithNewWinner_DemotesThePreviousCanonicalSibling()
    {
        var entityId = Guid.NewGuid().ToString("N");
        var firstWinner = svc.Upsert(Claim(entityId, "Breckenridge", "background", "ex-Arcturus Defense Solutions")).Claim!;
        svc.MakeCanonical(firstWinner.ClaimUid, "first resolution");
        Assert.That(svc.GetByEntity(entityId).First(c => c.ClaimUid == firstWinner.ClaimUid).Status, Is.EqualTo("CANONICAL"));

        // A later re-vote (e.g. a second Trinity Reconciliation pass) picks the OTHER phrasing.
        var secondWinner = svc.Upsert(Claim(entityId, "Breckenridge", "background", "ex-Arcturus")).Claim!;
        svc.MakeCanonical(secondWinner.ClaimUid, "second resolution — panel flipped");

        var byUid = svc.GetByEntity(entityId).ToDictionary(c => c.ClaimUid);
        Assert.That(byUid[secondWinner.ClaimUid].Status, Is.EqualTo("CANONICAL"));
        Assert.That(byUid[firstWinner.ClaimUid].Status, Is.EqualTo("REJECTED"),
            "the OLD canonical winner must be demoted when a later explicit re-resolution picks a different winner — " +
            "otherwise two claims are simultaneously CANONICAL for the same (EntityId, Predicate) forever");
        Assert.That(byUid[firstWinner.ClaimUid].SupersededBy, Is.EqualTo(secondWinner.ClaimUid));
    }

    [Test]
    public void MakeCanonical_CalledAgain_NeverLeavesTwoSimultaneouslyCanonicalClaims()
    {
        var entityId = Guid.NewGuid().ToString("N");
        var a = svc.Upsert(Claim(entityId, "Breckenridge", "background", "ex-Arcturus Defense Solutions")).Claim!;
        svc.MakeCanonical(a.ClaimUid);
        var b = svc.Upsert(Claim(entityId, "Breckenridge", "background", "ex-Arcturus")).Claim!;
        svc.MakeCanonical(b.ClaimUid);

        var canonicalCount = svc.GetByEntity(entityId).Count(c => c.Predicate == "background" && c.Status == "CANONICAL");
        Assert.That(canonicalCount, Is.EqualTo(1));
    }

    [Test]
    public void MakeCanonical_WithOnlyRejectClaimUids_DemotesPreviousCanonicalOnlyWhenListed()
    {
        // Trinity's actual call shape: a previously-CANONICAL sibling should only be demoted if
        // its underlying source was verifiably re-patched this pass (i.e. it's IN the resolved set)
        // — mirroring the exact same rule already applied to NEW/CONFIRMED/CONTRADICTED siblings.
        var entityId = Guid.NewGuid().ToString("N");
        var oldWinner = svc.Upsert(Claim(entityId, "Breckenridge", "background", "ex-Arcturus Defense Solutions")).Claim!;
        svc.MakeCanonical(oldWinner.ClaimUid);
        var newWinner = svc.Upsert(Claim(entityId, "Breckenridge", "background", "ex-Arcturus")).Claim!;

        // Simulate the old winner's edit being refused this pass — NOT in onlyRejectClaimUids.
        svc.MakeCanonical(newWinner.ClaimUid, "test", onlyRejectClaimUids: new HashSet<string>());

        Assert.That(svc.GetByEntity(entityId).First(c => c.ClaimUid == oldWinner.ClaimUid).Status, Is.EqualTo("CANONICAL"),
            "an unresolved previously-canonical sibling must stay CANONICAL (and thus keep forming a contradiction " +
            "group) until its own source is actually verified to have changed, same as any other losing claim");
    }
}
