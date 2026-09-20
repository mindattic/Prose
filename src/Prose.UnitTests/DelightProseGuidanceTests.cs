using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Regression cover for the 2026-09-06 move-rotation fix to <see cref="DelightProseGuidance"/>.
///
/// The service used to map each <see cref="BeatMode"/> to one FIXED rule list, so every beat of a
/// mode received byte-identical guidance for the life of a book. BeatChecklistGateService's own
/// "move monotony" check (DELIGHT §14) measured the consequence on BCODA: §7 "end on the act"
/// landed on 251 of 500 move-landing beats (50%) and §3 "involuntary body-truth" on 249 (50%),
/// because §7 appeared in 4 of the 6 mode lists AND was hardcoded into the through-line appended
/// to every single beat. These tests pin the properties that prevent that recurring.
/// </summary>
[TestFixture]
public class DelightProseGuidanceTests
{
    private static readonly BeatMode[] AllModes =
    [
        BeatMode.Combat, BeatMode.EmotionalClimax, BeatMode.Dialogue,
        BeatMode.Revelation, BeatMode.Transition, BeatMode.Narrative,
    ];

    /// <summary>The actual monotony property: across a realistic run of same-mode beats, no single
    /// rule may dominate. The pre-fix implementation scored 100% here for every rule in the list.</summary>
    [Test]
    public void NoSingleRuleDominates_AcrossALongRunOfSameModeBeats()
    {
        foreach (var mode in AllModes)
        {
            var hits = new Dictionary<string, int>();
            const int beats = 120;
            for (int i = 0; i < beats; i++)
                foreach (var section in RuleSections(DelightProseGuidance.GetForMode(mode, i)))
                    hits[section] = hits.GetValueOrDefault(section) + 1;

            var worst = hits.OrderByDescending(kv => kv.Value).First();
            var share = (double)worst.Value / beats;
            // 40%, deliberately below the 50% that BCODA's §7/§3 actually scored — a threshold
            // above the observed defect would let the exact reported failure pass as healthy.
            Assert.That(share, Is.LessThan(0.40),
                $"{mode}: rule {worst.Key} lands on {share:P0} of beats — that is a stamp, not a palette (DELIGHT §14).");
        }
    }

    /// <summary>Rotation must actually reach the whole pool; a rotation that cycles only part of it
    /// would still starve the remaining moves.</summary>
    [Test]
    public void EveryModePoolIsFullyReachable()
    {
        foreach (var mode in AllModes)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < 60; i++)
                foreach (var section in RuleSections(DelightProseGuidance.GetForMode(mode, i)))
                    seen.Add(section);

            // 9 is the minimum pool width that keeps a 3-rule window under ~33% per rule.
            Assert.That(seen.Count, Is.GreaterThanOrEqualTo(9),
                $"{mode}: only {seen.Count} distinct rules ever selected — pool is too narrow to vary.");
        }
    }

    /// <summary>Consecutive beats of the same mode must not receive identical guidance — that
    /// byte-identical repetition was the pre-fix behaviour.</summary>
    [Test]
    public void ConsecutiveBeats_GetDifferentGuidance()
    {
        foreach (var mode in AllModes)
            Assert.That(DelightProseGuidance.GetForMode(mode, 0),
                Is.Not.EqualTo(DelightProseGuidance.GetForMode(mode, 1)),
                $"{mode}: beats 0 and 1 got identical DELIGHT guidance.");
    }

    /// <summary>§7 was pushed on 100% of beats because the through-line named it on top of the four
    /// modes that already did. The through-line must stay universal-only.</summary>
    [Test]
    public void ThroughLine_DoesNotHardcodeASingleNumberedMove()
    {
        var text = DelightProseGuidance.GetForMode(BeatMode.Combat, 0);
        var throughLine = text[text.IndexOf("Through-line:", StringComparison.Ordinal)..];

        Assert.That(throughLine, Does.Not.Contain("§7"),
            "Through-line must not re-push §7 — that is what drove it to a 50% landing rate.");
    }

    /// <summary>Guidance is still mode-targeted, not one generic blob.</summary>
    [Test]
    public void GuidanceIsStillModeSpecific()
    {
        Assert.That(DelightProseGuidance.GetForMode(BeatMode.Combat, 0),
            Is.Not.EqualTo(DelightProseGuidance.GetForMode(BeatMode.Dialogue, 0)));
    }

    /// <summary>Pulls the "§N" rule ids out of one rendered guidance string, ignoring the
    /// through-line so only the per-beat selection is measured.</summary>
    private static IEnumerable<string> RuleSections(string guidance)
    {
        var cut = guidance.IndexOf("Through-line:", StringComparison.Ordinal);
        var body = cut >= 0 ? guidance[..cut] : guidance;
        return System.Text.RegularExpressions.Regex.Matches(body, @"§\d+")
            .Select(m => m.Value)
            .Distinct();
    }
}
