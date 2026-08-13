using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-08-13 shrink (plan "Separating rigor from fluidity").
/// <see cref="StoryScienceService.GetBeatGuidance"/> used to concatenate ~9 always-on craft-law
/// sub-blocks into every beat's prompt regardless of need — one of only five context mechanisms
/// firing on 100% of beats per <c>BeatServiceLog</c> telemetry (baseline measured 2026-08-13:
/// mean 6,428 chars, min 5,155, max 7,466 across 404 logged instances). The scene-shape/
/// structural judgments moved to <see cref="CraftQualityService"/>, a post-write lens; this
/// class now injects only sacred-flaw psychology, dialogue subtext, sentence mechanics, and a
/// one-line causal-chain reminder. This test guards against that shrink silently regressing back
/// toward the old size as new guidance gets added over time.
/// </summary>
[TestFixture]
public class StoryScienceServiceTests
{
    private readonly StoryScienceService svc = new();

    [Test]
    public void GetBeatGuidance_NarrativeModeNoCharacters_WellUnderOldBaseline()
    {
        var context = new BeatContext();
        var guidance = svc.GetBeatGuidance(context, beatIndex: 5, totalBeats: 50, BeatMode.Narrative);

        // Old mean was 6,428 chars (min 5,155). Post-shrink, a beat with no characters/dialogue
        // on screen carries only the causal-chain one-liner + prose mechanics.
        Assert.That(guidance.Length, Is.LessThan(1200),
            "a no-character, non-dialogue beat should carry only the causal-chain reminder + prose mechanics");
    }

    [Test]
    public void GetBeatGuidance_WorstCase_StillWellUnderOldBaseline()
    {
        // Worst case: XRay + characters present (sacred-flaw fires) AND dialogue mode (dialogue
        // honesty rules fire) — every conditional block active at once.
        var context = new BeatContext
        {
            XRayContext = "## Some Character\nVOICE — vocabulary: plain.",
            CharactersInScene = ["Some Character", "Other Character"],
        };
        var guidance = svc.GetBeatGuidance(context, beatIndex: 25, totalBeats: 50, BeatMode.Dialogue);

        // Measured 2,538 chars for this worst-case combination when this test was written —
        // still ~60% below the old 6,428-char mean and under the old 5,155-char minimum.
        Assert.That(guidance.Length, Is.LessThan(3000),
            "even with every conditional block active, the shrunk guidance should be well under the old ~6,428-char mean (and under the old ~5,155-char minimum)");
    }

    [Test]
    public void GetBeatGuidance_AlwaysIncludesCausalChainReminder()
    {
        var guidance = svc.GetBeatGuidance(new BeatContext(), beatIndex: 0, totalBeats: 10, BeatMode.Narrative);
        Assert.That(guidance, Does.Contain("CAUSAL CHAIN"));
    }

    [Test]
    public void GetBeatGuidance_NoCharacters_OmitsSacredFlawBlock()
    {
        var guidance = svc.GetBeatGuidance(new BeatContext(), beatIndex: 0, totalBeats: 10, BeatMode.Narrative);
        Assert.That(guidance, Does.Not.Contain("SACRED FLAW"));
    }

    [Test]
    public void GetBeatGuidance_WithXRayAndCharacters_IncludesSacredFlawBlock()
    {
        var context = new BeatContext
        {
            XRayContext = "## Some Character\nVOICE — vocabulary: plain.",
            CharactersInScene = ["Some Character"],
        };
        var guidance = svc.GetBeatGuidance(context, beatIndex: 0, totalBeats: 10, BeatMode.Narrative);
        Assert.That(guidance, Does.Contain("SACRED FLAW"));
    }
}
