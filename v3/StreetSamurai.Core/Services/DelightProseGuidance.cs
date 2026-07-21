namespace StreetSamurai.Core.Services;

/// <summary>
/// Positive prose-craft guidance derived from docs/DELIGHT.md — the reverse-engineered moves that
/// drove GLMZ beats to a reader mean ≥ 4.75/5 (the 99 top-decile beats + 114 praise ballots).
///
/// CRAFT.md is the floor (the DON'Ts); DELIGHT.md is the ceiling (the DOs). The full doctrine is
/// pinned globally into DCM context like CRAFT.md; this class does the *targeting* — for the beat's
/// detected <see cref="BeatMode"/> it emphasizes the 2–3 rules that fit that beat's job, so the model
/// reaches for the loved moves instead of merely avoiding flagged ones. Mirrors the
/// <c>CombatProseGuidance</c> injection point in <see cref="ProseWriterRouter"/> (ComputeEnrichment).
/// </summary>
public static class DelightProseGuidance
{
    /// <summary>Through-line under all 13 rules — always appended. The four highest-yield moves.</summary>
    private const string ThroughLine =
        "Through-line: concrete over abstract; spend each load-bearing image ONCE then walk away " +
        "(repetition is the corpus's loudest complaint); end on the act/object/name, cut the " +
        "interpretive gloss; and write in THIS narrator's own cadence, never one house rhythm.";

    /// <summary>
    /// The focused DELIGHT pointer for a beat mode: names the rules to prioritize (full text in
    /// docs/DELIGHT.md, already in context). Kept short — it directs attention, it isn't the doctrine.
    /// </summary>
    public static string GetForMode(BeatMode mode)
    {
        var rules = mode switch
        {
            BeatMode.Combat =>
                "§8 put the competence in the body, not the monologue — verbs-first, each skill used " +
                "precisely, theme carried in the choreography; §6 one hard image, spent once.",
            BeatMode.EmotionalClimax =>
                "§3 one involuntary body-truth that fires against the character's will (the body under " +
                "oath beats any named emotion); §11 stay present and witness at cost; §7 end on the act.",
            BeatMode.Dialogue =>
                "§5 offer the hard truth flat and let it cost the speaker (gentle is how you let someone " +
                "keep not hearing you); §3 one involuntary body-tell; §7 end on the act, not the gloss.",
            BeatMode.Revelation =>
                "§9 a reversal that recontextualizes without contradicting anything established — let the " +
                "body feel the wrongness a half-beat before the prose names it; §13 anchor the uncanny in " +
                "bureaucracy (horror through correct bookkeeping); §7 end on the act.",
            BeatMode.Transition =>
                "§1 open on a sensory fact that is already a clue; §6 one image, once; §12 keep this " +
                "narrator's distinct rhythm even in connective tissue.",
            _ => // Narrative (default) — the forensic/competence spine that owns the top decile
                "§2 let a mind read a system and find the seam — show reasoning as physical evidence " +
                "(which way a buckle faces, what a pronoun costs), never announced deduction; §1 open on " +
                "a perceived clue; §4 if there's a cost, price it as a kept number; §7 end on the act.",
        };
        return "DELIGHT (write toward a loved beat — see docs/DELIGHT.md). For this " + mode + " beat, lean on: "
             + rules + " " + ThroughLine;
    }
}
