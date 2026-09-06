using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// The two 2026-09-04 corrections to same-predicate contradiction detection: predicate
/// CARDINALITY (a set-valued predicate has many correct values at once) and PARAPHRASE (one
/// assertion recorded twice in different words).
///
/// <para><b>Why these are pinned hard in both directions.</b> The asymmetry matters more here than
/// almost anywhere else in the engine: a rule that is too LOOSE hides a real contradiction, which
/// is the exact failure the Story Ledger exists to prevent; a rule that is too TIGHT merely costs
/// someone a triage decision. So every test below has a matching negative — the thing that must
/// still be flagged.</para>
///
/// <para>Measured cause: a survey of the 1,316 live contradiction groups across BCODA/DWIACE/VATD
/// found 250 groups (950 rows) that were purely set-valued predicates and ~300 more that were pure
/// paraphrase — noise burying the real disagreements.</para>
/// </summary>
[TestFixture]
public class ContinuityCardinalityTests
{
    // ── set-valued predicates ────────────────────────────────────────────────

    [TestCase("ability")]
    [TestCase("ability_neuretics")]
    [TestCase("action")]
    [TestCase("action_taken")]
    [TestCase("action_during_dark_period")]
    [TestCase("knowledge_of_kyle")]
    [TestCase("possession_credstick")]
    [TestCase("skill")]
    [TestCase("relationship_to_kyle")]
    public void SetValued_Recognizes_The_Family_And_Its_Members(string predicate) =>
        Assert.That(ContinuityService.IsSetValuedPredicate(predicate), Is.True, predicate);

    [TestCase("age")]
    [TestCase("origin")]
    [TestCase("father_name")]
    [TestCase("life_status")]
    [TestCase("birthplace")]
    [TestCase("weapon_type")]
    public void SetValued_Leaves_Single_Valued_Predicates_Alone(string predicate) =>
        Assert.That(ContinuityService.IsSetValuedPredicate(predicate), Is.False, predicate);

    // ── the "what someone does" cluster (author ruling 2026-09-06) ───────────
    //
    // `occupation` was pinned False by this fixture as a deliberately-excluded example. BCODA
    // overturned it with measurement: `occupation` alone is SIX of the book's 43 live contradiction
    // groups, because a freelancer's occupation is a list. Kyle is security AND contractor AND
    // freelancer AND muscle in the same week; Pixel carries nine values; Sable two.

    [TestCase("occupation")]
    [TestCase("profession")]
    [TestCase("job_type")]
    [TestCase("job_title")]
    [TestCase("employment_status")]
    [TestCase("role")]
    [TestCase("crew_role")]
    [TestCase("role_in_crew")]
    public void SetValued_Covers_The_What_Someone_Does_Cluster(string predicate) =>
        Assert.That(ContinuityService.IsSetValuedPredicate(predicate), Is.True, predicate);

    [TestCase("occupation_duration")]
    [TestCase("job_duration")]
    [TestCase("employment_duration")]
    [TestCase("occupation_start_year")]
    [TestCase("role_established_year")]
    public void SetValued_Cluster_Is_Exact_So_Its_Durations_Still_Contradict(string predicate) =>
        // The whole safety margin of matching these EXACTLY rather than as a prefix family: how
        // long someone has held a job is one number, and "nine years" against "eleven years" is a
        // real defect the ledger must keep catching.
        Assert.That(ContinuityService.IsSetValuedPredicate(predicate), Is.False, predicate);

    // ── volatile: word-order variants and wound scope (2026-09-06) ───────────

    [Test]
    public void Volatile_Matches_A_Listed_Predicate_In_Either_Word_Order()
    {
        // `shoulder_injury` was listed and exempt; `injury_shoulder` — the same predicate from a
        // different extraction pass — was not, and contradicted. A matching bug, not a judgement.
        Assert.That(ContinuityService.IsVolatilePredicate("shoulder_injury"), Is.True);
        Assert.That(ContinuityService.IsVolatilePredicate("injury_shoulder"), Is.True);
        Assert.That(ContinuityService.IsVolatilePredicate("current_location"), Is.True);
        Assert.That(ContinuityService.IsVolatilePredicate("location_current"), Is.True);
    }

    [TestCase("injury_forearm")]
    [TestCase("forearm_injury")]
    [TestCase("burn_location")]
    [TestCase("hand_injury")]
    [TestCase("physical_injury")]
    [TestCase("wound_status")]
    [TestCase("scar_location")]
    public void Volatile_Covers_Every_Wound_Scoped_Predicate(string predicate) =>
        // A wound predicate names a body part or nothing — never WHICH wound — so two wound claims
        // on one entity answer questions the ledger cannot tell apart.
        Assert.That(ContinuityService.IsVolatilePredicate(predicate), Is.True, predicate);

    [TestCase("injurious_reputation")]  // stem is a whole token, never a prefix
    [TestCase("burned_bridges_with")]   // a relationship fact wearing a participle
    [TestCase("wounded_by")]            // names an assailant — an invariant, and must stay one
    [TestCase("scarring_event")]
    public void Volatile_Wound_Stems_Do_Not_Overreach(string predicate) =>
        Assert.That(ContinuityService.IsVolatilePredicate(predicate), Is.False, predicate);

    [TestCase("arrival_time")]
    [TestCase("location_at_time")]
    [TestCase("job_location")]
    public void Volatile_Covers_Scene_Scoped_Facts(string predicate) =>
        Assert.That(ContinuityService.IsVolatilePredicate(predicate), Is.True, predicate);

    [TestCase("birth_time")]
    [TestCase("birthplace")]
    [TestCase("father_name")]
    [TestCase("age")]
    [TestCase("origin")]
    public void Volatile_Leaves_Invariants_Alone(string predicate) =>
        Assert.That(ContinuityService.IsVolatilePredicate(predicate), Is.False, predicate);

    [TestCase("reaction")]      // contains "action" but is not in the family
    [TestCase("abilityish")]
    [TestCase("transaction")]
    public void SetValued_Is_An_Anchored_Family_Not_A_Substring_Match(string predicate) =>
        Assert.That(ContinuityService.IsSetValuedPredicate(predicate), Is.False, predicate);

    [Test]
    public void SetValued_Normalizes_Separators_Like_Every_Other_Predicate_Test()
    {
        Assert.That(ContinuityService.IsSetValuedPredicate("Action-Taken"), Is.True);
        Assert.That(ContinuityService.IsSetValuedPredicate("action taken"), Is.True);
    }

    // ── paraphrase detection ─────────────────────────────────────────────────

    [Test]
    public void Paraphrase_Catches_Subsumption()
    {
        // Measured live: the same ability recorded twice, once with an extra clause.
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "can read events ahead of time",
            "can read events ahead of time, provides tactical advantage"), Is.True);
    }

    [Test]
    public void Paraphrase_Catches_Near_Identical_Wording()
    {
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "locked counter and walked north with go-bag",
            "locked counter and walked north with prepared go-bag"), Is.True);
    }

    [Test]
    public void Paraphrase_Ignores_Case_And_Punctuation()
    {
        Assert.That(ContinuityService.ObjectsSayTheSameThing("Rebuilds bike.", "rebuilds  bike"), Is.True);
    }

    [Test]
    public void Paraphrase_Leaves_Complementary_Facets_For_A_Human()
    {
        // Two partial descriptions of one thing. Deciding whether they agree is an author's call
        // about the story, not a string comparison — this MUST stay on the triage pile.
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "red hair in loose braid", "dark red hair"), Is.False);
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "faded Kimodo Dragon stencil on tank",
            "faded Kimodo Dragon stencil, badges stripped"), Is.False);
    }

    [Test]
    public void Paraphrase_Never_Masks_A_Real_Disagreement()
    {
        Assert.That(ContinuityService.ObjectsSayTheSameThing("thirty-four", "thirty-six"), Is.False);
        Assert.That(ContinuityService.ObjectsSayTheSameThing("alive", "dead"), Is.False);
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "under Axiom cable gantry", "garage on Ashland, south side of Circuit"), Is.False);
        // The defect that started the programme, in object form.
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "constructed, no prior life", "son of a swordsmith"), Is.False);
    }

    [Test]
    public void Paraphrase_Does_Not_Swallow_A_Short_Object_Inside_A_Longer_One()
    {
        // "live" appears inside "delivered to the lab", and a bare substring rule would call a
        // beacon being live the same assertion as a delivery. The length floor prevents it.
        Assert.That(ContinuityService.ObjectsSayTheSameThing("live", "delivered to the lab"), Is.False);
        Assert.That(ContinuityService.ObjectsSayTheSameThing("red", "prepared"), Is.False);
    }

    [Test]
    public void Paraphrase_Requires_Two_Real_Objects()
    {
        Assert.That(ContinuityService.ObjectsSayTheSameThing(null, "anything"), Is.False);
        Assert.That(ContinuityService.ObjectsSayTheSameThing("", "anything"), Is.False);
        Assert.That(ContinuityService.ObjectsSayTheSameThing("   ", "anything"), Is.False);
    }

    // ── token subsumption + negation guard (2026-09-06) ──────────────────────

    [Test]
    public void Paraphrase_Catches_Detail_Inserted_Rather_Than_Appended()
    {
        // The live BCODA pair. Neither string contains the other and their overlap is 0.67, so the
        // substring rule and the 0.75 threshold both missed it — one implant, two chapters, on
        // record as a contradiction.
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "Atlas NeoCortex", "Atlas-grade NeoCortex"), Is.True);
    }

    [Test]
    public void Paraphrase_Never_Merges_A_Fact_With_Its_Own_Denial()
    {
        // Both rules would otherwise have said yes: "has neuretics" is a substring of "has no
        // neuretics", and {atlas, neocortex} is a token subset of the longer object.
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "has neuretics implanted", "has no neuretics implanted"), Is.False);
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "Atlas NeoCortex", "Atlas NeoCortex with no governor"), Is.False);
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "carries the blade", "carries the blade, never drawn"), Is.False);
    }

    [Test]
    public void Paraphrase_Token_Subsumption_Still_Leaves_Complementary_Facets_Alone()
    {
        // "dark" appears in neither direction, so neither side is a subset of the other.
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "red hair in loose braid", "dark red hair"), Is.False);
        // Two different places, sharing a word, must not merge.
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "noodle stall", "operates a noodle cart"), Is.False);
    }

    [Test]
    public void Paraphrase_Subsumes_A_OneToken_Designator_But_Never_A_Bare_Name()
    {
        // Pixel's address, recorded twice at different granularity.
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "2E", "2E, second floor of The Pivot"), Is.True);
        Assert.That(ContinuityService.ObjectsSayTheSameThing(
            "room 2E, across the hall from Kyle", "2E"), Is.True);

        // The reason the floor cannot simply drop to one token for everything: this pair is the
        // founding defect of the whole Story Ledger programme, and it must stay on the pile.
        Assert.That(ContinuityService.ObjectsSayTheSameThing("Seito", "Seito's apprentice"), Is.False);
        Assert.That(ContinuityService.ObjectsSayTheSameThing("live", "delivered to the lab"), Is.False);
        Assert.That(ContinuityService.ObjectsSayTheSameThing("fixer", "fixer who routes contracts"), Is.False);
    }

    // ── boolean polarity (2026-09-06) ────────────────────────────────────────

    private static ContinuityClaim BoolClaim(string predicate, string obj) => new()
    {
        EntityId = "e1", EntityName = "Kyle", Predicate = predicate, Object = obj,
    };

    [Test]
    public void BooleanPredicate_SameAnswer_DifferentElaboration_IsOneAssertion()
    {
        // The live BCODA pair: has_neuretics "true" (ch.5) vs "yes, with overlay display" (ch.19).
        // The two objects share no words at all, so no string rule could ever have seen it.
        Assert.That(ContinuityService.IsSameAssertion(
            BoolClaim("has_neuretics", "true"),
            BoolClaim("has_neuretics", "yes, with overlay display")), Is.True);
    }

    [Test]
    public void BooleanPredicate_OppositeAnswers_Still_Contradict()
    {
        Assert.That(ContinuityService.IsSameAssertion(
            BoolClaim("has_neuretics", "true"),
            BoolClaim("has_neuretics", "false")), Is.False);
        Assert.That(ContinuityService.IsSameAssertion(
            BoolClaim("has_neuretics", "yes, with overlay display"),
            BoolClaim("has_neuretics", "no")), Is.False);
    }

    [Test]
    public void BooleanPolarity_Never_Applies_To_A_NonBoolean_Predicate_Or_A_NonBoolean_Object()
    {
        // `occupation` is not a yes/no question, and "alive"/"dead" are not yes/no answers.
        Assert.That(ContinuityService.IsSameAssertion(
            BoolClaim("occupation", "true"),
            BoolClaim("occupation", "yes, mostly")), Is.False);
        Assert.That(ContinuityService.IsSameAssertion(
            BoolClaim("is_alive", "alive"),
            BoolClaim("is_alive", "dead")), Is.False);
    }
}
