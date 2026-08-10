using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-10 IxS CraftChecklist triage: the checklist's per-beat
/// DON'T-list evaluator had no way to distinguish a character's established, hand-authored voice
/// (e.g. Rook's filing/ledger/logistics framing, on file in Characters.SpeechVocabulary) from a
/// generic AI "cognitive-architecture tic," so it flagged the character's own consistent
/// characterization as a violation — 445 findings on IxS alone, the large majority under that one
/// category. <see cref="BeatChecklistGateService.BuildPovVoiceGuidance"/> is the pure, testable
/// piece of the fix: it builds the guidance block threaded into the evaluator prompt when a
/// beat's POV character has an on-file vocabulary hint.
/// </summary>
[TestFixture]
public class BeatChecklistGateServicePovVoiceTests
{
    [Test]
    public void NullHint_ProducesNoGuidance()
    {
        Assert.That(BeatChecklistGateService.BuildPovVoiceGuidance(null), Is.Empty);
    }

    [Test]
    public void EmptyOrWhitespaceHint_ProducesNoGuidance()
    {
        Assert.That(BeatChecklistGateService.BuildPovVoiceGuidance(""), Is.Empty);
        Assert.That(BeatChecklistGateService.BuildPovVoiceGuidance("   "), Is.Empty);
    }

    [Test]
    public void RealHint_IsIncludedVerbatimInGuidance()
    {
        var hint = "crew runner / heist planner — Nouns of logistics and terrain - seams, gaps, load, weight, records";
        var guidance = BeatChecklistGateService.BuildPovVoiceGuidance(hint);

        Assert.That(guidance, Does.Contain(hint));
        Assert.That(guidance, Does.Contain("NOT a violation"));
    }
}
