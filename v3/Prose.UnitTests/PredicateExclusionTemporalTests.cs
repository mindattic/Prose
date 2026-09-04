using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Audit;

namespace Prose.UnitTests;

/// <summary>
/// The temporal exclusion axioms — "a dead character does not later act".
///
/// <para><b>Why the constraint exists.</b> The Story Ledger plan declared three built-in axiom
/// families and only one shipped, because only one is a statement about two predicates alone.
/// This one is a statement about two predicates <i>in an order</i>: without the ordering it reads
/// "the character is dead and also acts", which is true of every character who dies on the page.
/// A corpus dry run on 2026-09-03 measured the cost of the gap — 32 books, 13 active axioms, zero
/// candidates anywhere.</para>
///
/// <para>These tests pin the two ways this could go wrong. Too loose, and every on-page death
/// becomes a paid LLM adjudication and a false finding. Too tight (or evaluated with no ordering
/// available), and the axiom silently matches nothing, which is indistinguishable from never
/// having written it.</para>
/// </summary>
[TestFixture]
public class PredicateExclusionTemporalTests
{
    private static readonly Guid BeatEarly = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BeatLate = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BeatUnknown = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly Dictionary<Guid, int> Order = new()
    {
        [BeatEarly] = 10,
        [BeatLate] = 300,
    };

    private static PredicateExclusion DeadThenActs() => new()
    {
        Id = 42,
        UniverseId = Guid.Empty,
        PredicateA = "death_status*|life_status*|fate*",
        ObjectPatternA = "dead|deceased|killed",
        PredicateB = "action*|speaks*",
        ObjectPatternB = null,
        Symmetric = false,
        TemporalOrder = PredicateExclusion.TemporalBAfterA,
        Source = "builtin",
        Status = "active",
        Rationale = "A character established dead does not act afterwards.",
    };

    private static ContinuityClaim Claim(string uid, string predicate, string obj, Guid? beat) => new()
    {
        ClaimUid = uid, EntityId = "entity-1", EntityName = "Sift",
        Predicate = predicate, Object = obj, Status = "NEW", SourceBeatId = beat,
    };

    [Test]
    public void IsTemporal_RecognizesTheSeededConstraintValue()
    {
        Assert.That(PredicateExclusionService.IsTemporal(DeadThenActs()), Is.True);
        Assert.That(PredicateExclusionService.IsTemporal(new PredicateExclusion { TemporalOrder = null }), Is.False);
    }

    [Test]
    public void ActionAfterDeath_IsACandidate()
    {
        var claims = new[]
        {
            Claim("c-death", "death_status", "dead", BeatEarly),
            Claim("c-act", "action_taken", "walks into the Carrion ward and signs the manifest", BeatLate),
        };

        var candidates = PredicateExclusionService.GenerateCandidates(claims, [DeadThenActs()], Order);

        Assert.That(candidates, Has.Count.EqualTo(1));
        // Direction matters: A must be the death, B the later action, or the adjudication prompt
        // describes the pair backwards.
        Assert.That(candidates[0].A.ClaimUid, Is.EqualTo("c-death"));
        Assert.That(candidates[0].B.ClaimUid, Is.EqualTo("c-act"));
    }

    [Test]
    public void ActionBeforeDeath_IsNotACandidate()
    {
        // The normal shape of a story: the character does things, then dies. If this produced a
        // candidate the axiom would bill for — and file against — every death in the corpus.
        var claims = new[]
        {
            Claim("c-act", "action_taken", "signs the manifest", BeatEarly),
            Claim("c-death", "death_status", "dead", BeatLate),
        };

        Assert.That(PredicateExclusionService.GenerateCandidates(claims, [DeadThenActs()], Order),
            Is.Empty);
    }

    [Test]
    public void ActionInTheSameBeatAsTheDeath_IsNotACandidate()
    {
        // The death scene itself. Strictly-later, not later-or-equal.
        var claims = new[]
        {
            Claim("c-death", "death_status", "dead", BeatEarly),
            Claim("c-act", "action_taken", "raises the revolver one last time", BeatEarly),
        };

        Assert.That(PredicateExclusionService.GenerateCandidates(claims, [DeadThenActs()], Order),
            Is.Empty);
    }

    [Test]
    public void AnUnanchoredClaim_IsNotACandidate()
    {
        // A claim with no beat cannot be placed on the book's clock. Pairing it anyway would turn
        // the temporal axiom back into the timeless one it must never be.
        var claims = new[]
        {
            Claim("c-death", "death_status", "dead", null),
            Claim("c-act", "action_taken", "signs the manifest", BeatLate),
        };

        Assert.That(PredicateExclusionService.GenerateCandidates(claims, [DeadThenActs()], Order),
            Is.Empty);
    }

    [Test]
    public void AnAnchorMissingFromTheOrdering_IsNotACandidate()
    {
        // A beat that is not in this book's reading order (disabled, moved, deleted) has no
        // position to compare, so the pair fails closed rather than being guessed at.
        var claims = new[]
        {
            Claim("c-death", "death_status", "dead", BeatUnknown),
            Claim("c-act", "action_taken", "signs the manifest", BeatLate),
        };

        Assert.That(PredicateExclusionService.GenerateCandidates(claims, [DeadThenActs()], Order),
            Is.Empty);
    }

    [Test]
    public void WithNoOrderingSupplied_TemporalRulesAreSkippedEntirely()
    {
        // Not "evaluated as timeless" — that is a different, far broader axiom than the one the
        // author approved, and silently widening a rule is the failure this table is written
        // to avoid.
        var claims = new[]
        {
            Claim("c-death", "death_status", "dead", BeatEarly),
            Claim("c-act", "action_taken", "signs the manifest", BeatLate),
        };

        Assert.That(PredicateExclusionService.GenerateCandidates(claims, [DeadThenActs()]), Is.Empty);
    }

    [Test]
    public void ATemporalRuleNeverMatchesWithItsSidesSwapped()
    {
        // Symmetric is ignored for a temporal rule even when a row carries it set, because
        // swapping the sides asserts the opposite ordering — the opposite axiom.
        var rule = DeadThenActs();
        rule.Symmetric = true;

        var death = Claim("c-death", "death_status", "dead", BeatLate);
        var act = Claim("c-act", "action_taken", "signs the manifest", BeatEarly);

        // act(early) filling the DEATH slot and death(late) filling the ACTION slot is the only
        // way a swap could produce a pair here, and it must not.
        Assert.That(PredicateExclusionService.Matches(rule, act, death), Is.False);
        Assert.That(PredicateExclusionService.GenerateCandidates([death, act], [rule], Order), Is.Empty);
    }

    [Test]
    public void ANonDeathObject_IsNotACandidateEvenWithTheRightPredicate()
    {
        // life_status is in the death family, but "alive" is not a death.
        var claims = new[]
        {
            Claim("c-alive", "life_status", "alive on a loaner heart, 72-hour limit", BeatEarly),
            Claim("c-act", "action_taken", "signs the manifest", BeatLate),
        };

        Assert.That(PredicateExclusionService.GenerateCandidates(claims, [DeadThenActs()], Order),
            Is.Empty);
    }

    [Test]
    public void TimelessRulesStillWorkWhenAnOrderingIsSupplied()
    {
        // The ordering map must not change the behaviour of a non-temporal axiom.
        var timeless = new PredicateExclusion
        {
            Id = 1, UniverseId = Guid.Empty,
            PredicateA = "origin*", ObjectPatternA = "constructed",
            PredicateB = "father*", ObjectPatternB = null,
            Symmetric = true, TemporalOrder = null, Status = "active", Source = "builtin",
            Rationale = "A constructed being has no biological father.",
        };
        var claims = new[]
        {
            Claim("c-origin", "origin", "constructed, no prior life", BeatLate),
            Claim("c-father", "father_name", "Dae-jung Seo", BeatEarly),
        };

        // Father anchored EARLIER than the origin claim — a temporal rule would reject this pair;
        // a timeless one must not care.
        Assert.That(PredicateExclusionService.GenerateCandidates(claims, [timeless], Order),
            Has.Count.EqualTo(1));
    }
}
