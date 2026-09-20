using NUnit.Framework;
using Prose.Core.Services.Contradiction;

namespace Prose.UnitTests;

/// <summary>
/// The parse behind the one judgement-based check the save gate is willing to block on.
///
/// <para><b>Why the parse and not the prompt.</b> The prompt's accuracy is measured by
/// <see cref="CalibrationFixtures"/> against a real model — seven hand-planted cases, billed, run
/// by <c>prose-v4 calibrate-gate</c>. What cannot be measured that way, because it never reaches
/// the model, is what happens to a reply that comes back in an unexpected shape. That path decides
/// whether the author gets blocked, so it is tested here.</para>
///
/// <para>The asymmetry that shapes it: this check sits in front of the author's own typing, so a
/// reply it cannot read must NOT block them. It fails open, deliberately, and the opposite of
/// <c>ContinuityEnforcer</c>'s fail-closed policy for generation.</para>
/// </summary>
[TestFixture]
public class NarrativeContradictionCheckerTests
{
    [Test]
    public void Reads_a_consistent_verdict()
    {
        var v = NarrativeContradictionChecker.Parse(
            "Dana is alive in the facts and the beat shows her walking. Nothing conflicts.\n"
            + "VERDICT: CONSISTENT");

        Assert.That(v.Contradicts, Is.False);
        Assert.That(v.Reasoning, Does.Contain("Nothing conflicts"));
    }

    [Test]
    public void Reads_a_contradiction_and_keeps_the_fact_it_names()
    {
        var v = NarrativeContradictionChecker.Parse(
            "The facts say Dana is active. The beat says she has been dead three days.\n"
            + "VERDICT: CONTRADICTS Dana Okafor — life_status: active");

        Assert.That(v.Contradicts, Is.True);
        Assert.That(v.ViolatedFact, Is.EqualTo("Dana Okafor — life_status: active"));
        Assert.That(v.Reasoning, Does.Not.Contain("VERDICT"),
            "the verdict line is protocol and must not be shown to the author as reasoning");
    }

    [Test]
    public void The_reasoning_is_everything_before_the_verdict()
    {
        // Reasoning-first ordering is the whole point of the prompt: a verdict emitted before its
        // reasoning lets the model argue against its own boolean about one time in ten. The parse
        // has to honour that ordering or the measurement it was built on means nothing.
        var v = NarrativeContradictionChecker.Parse(
            "First sentence.\nSecond sentence.\nVERDICT: CONSISTENT");

        Assert.That(v.Reasoning, Is.EqualTo("First sentence.\nSecond sentence."));
    }

    [Test]
    public void Takes_the_LAST_verdict_line_when_the_model_writes_two()
    {
        // A model that explains the format before using it writes "VERDICT:" twice. The operative
        // one is its conclusion, not its worked example.
        var v = NarrativeContradictionChecker.Parse(
            "I will answer with VERDICT: CONTRADICTS if they conflict.\n"
            + "They do not conflict.\n"
            + "VERDICT: CONSISTENT");

        Assert.That(v.Contradicts, Is.False);
    }

    [Test]
    public void A_reply_with_no_verdict_line_does_not_block_the_author()
    {
        // Fails OPEN, and says so in the reasoning. This check stands in front of someone's own
        // typing; a parse failure must cost them nothing.
        var v = NarrativeContradictionChecker.Parse("I am not sure what to make of this.");

        Assert.That(v.Contradicts, Is.False);
        Assert.That(v.Reasoning, Does.Contain("fail-open"));
    }

    [Test]
    public void A_bare_contradiction_verdict_carries_no_invented_fact()
    {
        var v = NarrativeContradictionChecker.Parse("It conflicts.\nVERDICT: CONTRADICTS");

        Assert.That(v.Contradicts, Is.True);
        Assert.That(v.ViolatedFact, Is.Null,
            "better to name nothing than to name something the model did not say");
    }

    [Test]
    public void Is_not_thrown_by_case_or_leading_whitespace()
    {
        Assert.That(NarrativeContradictionChecker.Parse("ok\n   verdict: consistent").Contradicts,
                    Is.False);
        Assert.That(NarrativeContradictionChecker.Parse("no\n  VERDICT: Contradicts the fact").Contradicts,
                    Is.True);
    }

    [Test]
    public void The_calibration_fixtures_are_still_a_real_answer_key()
    {
        // Not a test of the model — a test of the fixture set, which is the thing an unmeasured
        // instrument hides behind. A set that had drifted to all-negative would let a checker that
        // never fires score 100%.
        Assert.That(CalibrationFixtures.All, Has.Count.GreaterThanOrEqualTo(6));
        Assert.That(CalibrationFixtures.All.Any(c => c.ExpectedContradicts), Is.True);
        Assert.That(CalibrationFixtures.All.Any(c => !c.ExpectedContradicts), Is.True);
        Assert.That(CalibrationFixtures.All.All(c => c.Facts.Count > 0), Is.True,
            "a case with no facts short-circuits before the model and measures nothing");
    }
}
