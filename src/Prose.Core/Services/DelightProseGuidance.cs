namespace Prose.Core.Services;

/// <summary>
/// Positive prose-craft guidance derived from docs/DELIGHT.md — the reverse-engineered moves that
/// drove GLMZ beats to a reader mean ≥ 4.75/5 (the 99 top-decile beats + 114 praise ballots).
///
/// CRAFT.md is the floor (the DON'Ts); DELIGHT.md is the ceiling (the DOs). The full doctrine is
/// pinned globally into DCM context like CRAFT.md; this class does the *targeting* — for the beat's
/// detected <see cref="BeatMode"/> it emphasizes 2–3 rules that fit that beat's job, so the model
/// reaches for the loved moves instead of merely avoiding flagged ones. Mirrors the
/// <c>CombatProseGuidance</c> injection point in <see cref="ProseWriterRouter"/> (ComputeEnrichment).
///
/// <para>2026-09-06 — ROTATION. Until now each mode mapped to one FIXED rule list, so every beat of
/// a given mode received identical guidance for the life of a book. BeatChecklistGateService's own
/// "move monotony" check caught the result on BCODA: §7 "end on the act" landed 251/500 beats (50%)
/// and §3 "involuntary body-truth" 249/500 (50%) — because §7 sat in 4 of the 6 mode lists AND was
/// hardcoded into the through-line appended to every beat, while §3 sat in 2 more. The engine had a
/// tic, and it was this table. That directly violates DELIGHT §14 ("Vary the moves — a palette, not
/// a stamp"), which the doctrine states and the code contradicted. Each mode now carries a POOL of
/// mode-appropriate rules and the beat index selects a rotating window over it, so a long run of
/// same-mode beats cycles the whole palette instead of stamping one move. §7 is also no longer
/// duplicated in the through-line — it earns its place from the pools like everything else.</para>
/// </summary>
public static class DelightProseGuidance
{
    /// <summary>Through-line under all 14 rules — always appended. Deliberately holds only the
    /// genuinely universal floor (concreteness, the repetition complaint, per-narrator cadence) plus
    /// §14 itself. It used to also carry §7's "end on the act, cut the gloss", which meant §7 was
    /// pushed on 100% of beats on top of the 4 modes that named it — half the monotony problem.</summary>
    private const string ThroughLine =
        "Through-line: concrete over abstract; spend each load-bearing image ONCE then walk away " +
        "(repetition is the corpus's loudest complaint); write in THIS narrator's own cadence, never " +
        "one house rhythm; and per §14 vary which move does the work — a palette, not a stamp.";

    /// <summary>How many rules to name per beat. With a stride of 1 and a pool of N, each rule lands
    /// on RulesPerBeat/N of that mode's beats — so pools are kept at 9+ to hold every rule near 33%.
    /// A pool of 6 with a window of 3 would put every rule on 50% of beats, which is precisely the
    /// rate the move-monotony check flagged; the pool size IS the fix, not the rotation alone.</summary>
    private const int RulesPerBeat = 3;

    private static readonly string[] Combat =
    [
        "§8 put the competence in the body, not the monologue — verbs-first, each skill used precisely, theme carried in the choreography",
        "§6 one hard image, spent once",
        "§3 one involuntary body-truth that fires against the character's will",
        "§4 if there's a cost, price it as a kept number",
        "§11 stay present and witness at cost",
        "§7 end on the act, not the gloss",
        "§1 open on a sensory fact that is already a clue",
        "§10 let mundane heroism win — someone just does the work and holds",
        "§12 keep this narrator's distinct rhythm",
    ];

    private static readonly string[] EmotionalClimax =
    [
        "§3 one involuntary body-truth that fires against the character's will (the body under oath beats any named emotion)",
        "§11 stay present and witness at cost",
        "§7 end on the act, not the gloss",
        "§5 offer the hard truth flat and let it cost the speaker",
        "§10 let mundane heroism win — someone just does the work and holds",
        "§6 one image, spent once",
        "§1 open on a sensory fact that is already a clue",
        "§4 price the cost as a kept number",
        "§12 keep this narrator's distinct rhythm",
    ];

    private static readonly string[] Dialogue =
    [
        "§5 offer the hard truth flat and let it cost the speaker (gentle is how you let someone keep not hearing you)",
        "§3 one involuntary body-tell",
        "§7 end on the act, not the gloss",
        "§12 keep this narrator's distinct rhythm — sameness is the wall",
        "§2 let a mind read a system and find the seam",
        "§10 let mundane heroism win — someone just does the work and holds",
        "§11 stay present to witness the thing no one else will",
        "§6 spend the load-bearing image once, then walk away",
        "§4 price the cost as a kept number",
    ];

    private static readonly string[] Revelation =
    [
        "§9 a reversal that recontextualizes without contradicting anything established — let the body feel the wrongness a half-beat before the prose names it",
        "§13 anchor the uncanny in bureaucracy (horror through correct bookkeeping)",
        "§7 end on the act, not the gloss",
        "§2 let a mind read a system and find the seam",
        "§1 open on a sensory fact that is already a clue",
        "§6 one image, spent once",
        "§4 price the cost as a kept number",
        "§11 stay present to witness the thing no one else will",
        "§12 keep this narrator's distinct rhythm",
    ];

    private static readonly string[] Transition =
    [
        "§1 open on a sensory fact that is already a clue",
        "§6 one image, once",
        "§12 keep this narrator's distinct rhythm even in connective tissue",
        "§10 let mundane heroism win — someone just does the work and holds",
        "§4 if there's a cost, price it as a kept number",
        "§13 anchor the uncanny in bureaucracy",
        "§2 let a mind read a system and find the seam",
        "§11 stay present to witness the thing no one else will",
        "§7 end on the act, not the gloss",
    ];

    /// <summary>Narrative is the DEFAULT mode, so it takes the most beats in almost every book and
    /// its pool is deliberately the widest — a narrow default pool is what concentrated §2/§1/§4/§7.</summary>
    private static readonly string[] Narrative =
    [
        "§2 let a mind read a system and find the seam — show reasoning as physical evidence (which way a buckle faces, what a pronoun costs), never announced deduction",
        "§1 open on a perceived clue",
        "§4 if there's a cost, price it as a kept number",
        "§7 end on the act, not the gloss",
        "§10 let mundane heroism win — someone just does the work and holds",
        "§8 put the competence in the body, not the monologue",
        "§12 give this narrator a distinct rhythm — sameness is the wall",
        "§6 spend the load-bearing image once, hard, then walk away",
        "§11 stay present to witness the thing no one else will",
        "§13 anchor the uncanny in the bureaucratic",
    ];

    private static string[] PoolFor(BeatMode mode) => mode switch
    {
        BeatMode.Combat          => Combat,
        BeatMode.EmotionalClimax => EmotionalClimax,
        BeatMode.Dialogue        => Dialogue,
        BeatMode.Revelation      => Revelation,
        BeatMode.Transition      => Transition,
        _                        => Narrative,
    };

    /// <summary>
    /// The focused DELIGHT pointer for a beat mode: names the rules to prioritize (full text in
    /// docs/DELIGHT.md, already in context). Kept short — it directs attention, it isn't the doctrine.
    /// <paramref name="beatIndex"/> rotates the selection through the mode's pool so the book gets a
    /// palette; pass the beat's position in the book. It defaults to 0 only so ad hoc/preview callers
    /// that have no index still get valid guidance — a real generation path should always pass one,
    /// since a constant index reintroduces exactly the monotony this rotation exists to prevent.
    /// </summary>
    public static string GetForMode(BeatMode mode, int beatIndex = 0)
    {
        var pool = PoolFor(mode);
        // Stride 1, not RulesPerBeat: a stride sharing a factor with the pool length collapses the
        // rotation into a handful of tiled windows (stride 3 over a pool of 6 yields just TWO, so
        // every rule still lands on 50% of that mode's beats). Stride 1 visits every offset.
        var start = Math.Abs(beatIndex) % pool.Length;
        var picked = new string[Math.Min(RulesPerBeat, pool.Length)];
        for (int i = 0; i < picked.Length; i++)
            picked[i] = pool[(start + i) % pool.Length];

        return "DELIGHT (write toward a loved beat — see docs/DELIGHT.md). For this " + mode + " beat, lean on: "
             + string.Join("; ", picked) + ". " + ThroughLine;
    }
}
