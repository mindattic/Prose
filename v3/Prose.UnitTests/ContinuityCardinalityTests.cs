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
    [TestCase("occupation")]
    [TestCase("weapon_type")]
    public void SetValued_Leaves_Single_Valued_Predicates_Alone(string predicate) =>
        Assert.That(ContinuityService.IsSetValuedPredicate(predicate), Is.False, predicate);

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
}
