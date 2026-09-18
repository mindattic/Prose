using Prose.V4.Core.Ledger;

namespace Prose.V4.Core.Contradiction;

/// <summary>One hand-planted calibration case for <see cref="NarrativeContradictionChecker"/>:
/// a fact set, a beat text, and the verdict a correct checker must reach. Same discipline as
/// GCTOC/GCSH/GCNEG/GCOBN elsewhere in this project — an instrument is measured against a
/// hand-built answer key before it's trusted on real content — scaled down to fact/text pairs
/// instead of whole books, because that's the actual unit this checker operates on.</summary>
public sealed record CalibrationCase(string Name, IReadOnlyList<OnScreenFact> Facts, string BeatText, bool ExpectedContradicts);

public static class CalibrationFixtures
{
    private static OnScreenFact Fact(string entity, string predicate, string value) =>
        new(entity, predicate, value, "fixture", null);

    /// <summary>3 hand-planted contradictions (must all be caught) + 3 clean cases (should not be
    /// flagged) — the v4 plan's own Phase 3 acceptance shape. Facts and phrasing are original, not
    /// copied from BCODA, so this fixture is reusable independent of any one book's canon.</summary>
    public static IReadOnlyList<CalibrationCase> All { get; } = new[]
    {
        new CalibrationCase(
            "dead-vs-alive",
            [Fact("Dana Okafor", "life_status", "active")],
            "Dana Okafor's body lay cold on the warehouse floor, three days dead before anyone found her.",
            ExpectedContradicts: true),

        new CalibrationCase(
            "stranger-vs-established-relationship",
            [Fact("Rys Vantage", "role", "Dana's neighbor and closest friend for six years")],
            "Rys Vantage, a stranger Dana had never met before, introduced himself at the door.",
            ExpectedContradicts: true),

        new CalibrationCase(
            "denies-established-possession",
            [Fact("Dana Okafor", "carries.primary", "a battered revolver she'd carried for a decade")],
            "Dana drew her twin blades — she had never once owned a gun in her life.",
            ExpectedContradicts: true),

        new CalibrationCase(
            "consistent-injury-mention",
            [Fact("Dana Okafor", "life_status", "active")],
            "Dana winced, favoring her bruised ribs as she crossed the room.",
            ExpectedContradicts: false),

        new CalibrationCase(
            "consistent-location-detail",
            [Fact("Rys Vantage", "location", "a cluttered workshop on the third floor")],
            "Rys waved Dana into the workshop, tools scattered across every surface, the third-floor window fogged with solder smoke.",
            ExpectedContradicts: false),

        new CalibrationCase(
            "consistent-use-of-established-item",
            [Fact("Dana Okafor", "carries.primary", "a battered revolver she'd carried for a decade")],
            "Dana set the revolver on the table, the worn grip catching the lamplight, and began to clean it.",
            ExpectedContradicts: false),

        // The single biggest false-positive source in the OLD continuity system (ContinuityService.cs's
        // VolatilePredicates exemption list, added after ~14 of 24 BCODA findings were exactly this):
        // a location/state fact is the character's LAST KNOWN position, not a permanent claim. Moving
        // on from it is the character having moved, not the prose contradicting itself. If this
        // checker flags it, that's a real finding about the checker, not the fixture.
        new CalibrationCase(
            "movement-is-not-contradiction",
            [Fact("Dana Okafor", "location", "her apartment on the fourth floor")],
            "Dana pushed through the door of the noodle shop three blocks over, the smell of broth hitting her before the bell finished ringing.",
            ExpectedContradicts: false),
    };
}
