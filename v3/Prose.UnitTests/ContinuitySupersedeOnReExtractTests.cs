using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-09-06 fix to extraction being purely ADDITIVE.
///
/// <para>Re-extracting a source used to leave the previous pass's claims live, so the same fact
/// accumulated paraphrase copies and the same-predicate/different-object detector counted every
/// pile as a contradiction. Measured on BCODA: <c>[The Roost] acquisition_effective_date</c> held
/// three phrasings of one date and <c>acquisition_status</c> four phrasings of one status, all from
/// ch.15; <c>[Kyle] age</c> held three mutually-consistent variants. The publish gate was therefore
/// counting extraction duplication rather than story defects — and moved the WRONG WAY when the
/// book was fixed, going 47 → 86 after five prose splices, because editing a beat re-extracts its
/// chapter. <c>reassess</c> could not clear them: its paraphrase rule requires no peer, and
/// duplicates have peers by definition.</para>
/// </summary>
[TestFixture]
public class ContinuitySupersedeOnReExtractTests
{
    private string tempRoot = "";
    private ContinuityService svc = null!;

    [SetUp]
    public void SetUp()
    {
        tempRoot = Path.Combine(Path.GetTempPath(), "ss-continuity-supersede-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        var paths = new TestPathProviderWithRoot(tempRoot);
        svc = new ContinuityService(TestDbFactory.For(paths, "nodes"));
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(tempRoot, recursive: true); } catch { /* best effort */ }
    }

    static ContinuityClaim Claim(
        string entityId, string predicate, string obj,
        string sourceChapterId, string sourceType = "prose") => new()
    {
        EntityId = entityId,
        EntityName = "The Roost",
        EntityKind = "place",
        Predicate = predicate,
        Object = obj,
        SourceType = sourceType,
        SourceChapterId = sourceChapterId,
        ExtractedBy = ["test"],
    };

    private ContinuityClaim Status(string entityId, string uid) =>
        svc.GetByEntity(entityId).First(c => c.ClaimUid == uid);

    /// <summary>The core behaviour: a second pass over the same chapter replaces its own prior
    /// output rather than stacking a duplicate on it.</summary>
    [Test]
    public void ReExtractingASource_SupersedesThatSourcesPriorClaims()
    {
        var id = Guid.NewGuid().ToString("N");
        var firstPass = svc.Upsert(Claim(id, "acquisition_effective_date", "one month from notice date", "ch15")).Claim!;

        var n = svc.SupersedeLiveClaimsForSource("ch15", "prose", "re-extraction");
        var secondPass = svc.Upsert(Claim(id, "acquisition_effective_date", "end of fiscal quarter, one month out", "ch15")).Claim!;

        Assert.Multiple(() =>
        {
            Assert.That(n, Is.EqualTo(1), "the first pass's claim should have been superseded");
            Assert.That(Status(id, firstPass.ClaimUid).Status, Is.EqualTo("SUPERSEDED"));
            Assert.That(Status(id, secondPass.ClaimUid).Status, Is.Not.EqualTo("SUPERSEDED"));
        });
    }

    /// <summary>Scope discipline: re-extracting one chapter must not disturb another chapter's
    /// claims about the same entity and predicate — those are separate evidence, and collapsing
    /// them would destroy exactly the cross-chapter contradictions the ledger exists to find.</summary>
    [Test]
    public void SupersedingOneSource_LeavesOtherSourcesUntouched()
    {
        var id = Guid.NewGuid().ToString("N");
        var ch15 = svc.Upsert(Claim(id, "acquisition_status", "scheduled for district acquisition", "ch15")).Claim!;
        var ch22 = svc.Upsert(Claim(id, "acquisition_status", "already acquired", "ch22")).Claim!;

        svc.SupersedeLiveClaimsForSource("ch15", "prose", "re-extraction");

        Assert.Multiple(() =>
        {
            Assert.That(Status(id, ch15.ClaimUid).Status, Is.EqualTo("SUPERSEDED"));
            Assert.That(Status(id, ch22.ClaimUid).Status, Is.Not.EqualTo("SUPERSEDED"),
                "a different chapter's claim is separate evidence and must survive.");
        });
    }

    /// <summary>A prose re-extraction must not wipe entity-record or outline claims that happen to
    /// carry the same source id — those have their own refresh paths.</summary>
    [Test]
    public void SupersedingProse_LeavesOtherSourceTypesUntouched()
    {
        var id = Guid.NewGuid().ToString("N");
        var prose = svc.Upsert(Claim(id, "affiliation", "Lotus Syndicate", "ch12")).Claim!;
        var record = svc.Upsert(Claim(id, "affiliation", "Lotus Syndicate — south-arm cell captain", "ch12", "entity_record")).Claim!;

        svc.SupersedeLiveClaimsForSource("ch12", "prose", "re-extraction");

        Assert.Multiple(() =>
        {
            Assert.That(Status(id, prose.ClaimUid).Status, Is.EqualTo("SUPERSEDED"));
            Assert.That(Status(id, record.ClaimUid).Status, Is.Not.EqualTo("SUPERSEDED"),
                "entity-record claims are refreshed by their own path, not by a prose pass.");
        });
    }

    /// <summary>A claim already written into canon is a decision somebody made. Re-extraction is
    /// not evidence against it.</summary>
    [Test]
    public void AppliedClaims_AreNeverSuperseded()
    {
        var id = Guid.NewGuid().ToString("N");
        var applied = svc.Upsert(Claim(id, "owner", "Mrs. Chen", "ch15")).Claim!;
        svc.MarkApplied(applied.ClaimUid, "place.owner");

        var n = svc.SupersedeLiveClaimsForSource("ch15", "prose", "re-extraction");

        Assert.Multiple(() =>
        {
            Assert.That(n, Is.EqualTo(0));
            Assert.That(Status(id, applied.ClaimUid).Status, Is.Not.EqualTo("SUPERSEDED"));
        });
    }

    /// <summary>Fail-safe. The supersede is gated on real candidates surviving validation, so an
    /// LLM outage or a truncated JSON array cannot blank a source's ledger and leave nothing —
    /// the same fail-open trap already fixed in BehavioralInvariantEnforcer and SwainAuditService.
    /// This pins the guarantee at the service level: superseding a source with nothing to replace
    /// it is never something the extraction path asks for.</summary>
    [Test]
    public void SupersedeIsANoOp_WhenTheSourceHasNoLiveClaims()
    {
        Assert.That(svc.SupersedeLiveClaimsForSource("ch-never-extracted", "prose", "re-extraction"),
            Is.EqualTo(0));
        Assert.That(svc.SupersedeLiveClaimsForSource("", "prose", "re-extraction"),
            Is.EqualTo(0), "an empty source id must not match every claim with a null source.");
    }
}
