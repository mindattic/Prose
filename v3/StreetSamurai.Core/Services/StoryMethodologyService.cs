namespace StreetSamurai.Core.Services;

/// <summary>
/// Encodes the science of storytelling — structural beat roles, scene-sequel mechanics,
/// and protagonist Want vs Need — as prompt context injected into the generation pipeline.
///
/// Based on Blake Snyder's Save the Cat beat sheet, Dwight Swain's scene-sequel model,
/// and Shawn Coyne's Story Grid genre obligations.
///
/// ── WHY ──
/// An outline with "3 acts" and tension scores is necessary but not sufficient. Without
/// named structural positions the LLM places the inciting incident wherever it feels right
/// (usually too late), midpoints are soft, and "all is lost" moments are skipped entirely.
/// Encoding these positions turns vague arc guidance into structural contracts.
///
/// ── SCENE-SEQUEL ──
/// Every beat is one of two types:
/// - Scene: Character pursues a Goal → faces Conflict → ends in Disaster (yes-but / no-and)
/// - Sequel: Character Reacts to disaster → weighs Dilemma → makes Decision → new Goal
/// This alternation is what prevents "and then, and then, and then" plotting.
///
/// ── WANT vs NEED ──
/// The protagonist consciously wants something external (the job, the money, the truth).
/// Unconsciously they need something internal (trust, acceptance, to grieve, to forgive).
/// The story tests whether they can sacrifice the Want to gain the Need.
/// </summary>
public class StoryMethodologyService
{
    /// <summary>Named structural roles at specific proportional positions.</summary>
    public record BeatRole(
        string Name,
        string Description,
        string SceneType,   // "scene" or "sequel"
        float PositionMin,
        float PositionMax);

    private static readonly BeatRole[] Roles =
    [
        new("Opening Image",      "Establish the status quo — the world before change arrives. Ground the reader in place, character, and tone.", "scene",   0.00f, 0.08f),
        new("Theme Stated",       "Someone — not the protagonist — states the story's thematic question, usually obliquely. The protagonist doesn't understand it yet.", "scene",   0.06f, 0.15f),
        new("Set-Up",             "Introduce all story elements that will pay off later: characters, flaws, world details, relationships. Everything planted here must bloom.", "scene",   0.10f, 0.18f),
        new("Catalyst",           "The inciting incident — the event that disrupts the status quo and forces the story's central question. The protagonist cannot ignore it.", "scene",   0.10f, 0.20f),
        new("Debate",             "The protagonist debates whether to engage. Fear, doubt, or obligation. They need a push — internal or external — to commit.", "sequel",  0.15f, 0.25f),
        new("Break Into Two",     "The protagonist commits. They cross the threshold into the story's new world. There is no going back. Act Two begins.", "scene",   0.20f, 0.28f),
        new("B Story",            "A secondary relationship or subplot enters — often the character who will deliver the theme. This story runs parallel and converges at the climax.", "scene",   0.22f, 0.32f),
        new("Fun and Games",      "The promise of the premise — the protagonist exploring (and often succeeding in) the new world. The reader is getting what the logline promised.", "scene",   0.30f, 0.50f),
        new("Midpoint",           "A false peak or false valley at the exact center. A reversal — victory that sets up the fall, or defeat that reveals what really matters. Raises stakes.", "scene",   0.45f, 0.55f),
        new("Bad Guys Close In",  "The forces of opposition regroup and push back harder. The protagonist's team falls apart. Internal and external pressure intensifies.", "scene",   0.55f, 0.70f),
        new("All Is Lost",        "The lowest point. The protagonist loses what they valued most — a person, a belief, a mission. All their plans have failed. The whiff of death.", "scene",   0.70f, 0.78f),
        new("Dark Night of the Soul", "The protagonist sits with the loss. No action — pure reaction. What does it mean? Was it worth it? This silence before the final push is essential.", "sequel",  0.76f, 0.84f),
        new("Break Into Three",   "The protagonist synthesizes the A and B stories. They find the answer — usually something they already had but couldn't see. Act Three begins.", "sequel",  0.84f, 0.88f),
        new("Finale",             "The protagonist executes the final plan. The climax. Every thread pays off. The antagonist force is defeated or the question is answered, at a cost.", "scene",   0.85f, 0.97f),
        new("Final Image",        "Mirror of the Opening Image — but changed. Show, don't tell, that transformation occurred. The world is different because the protagonist changed.", "scene",   0.95f, 1.00f),
    ];

    /// <summary>
    /// Returns the structural role for a beat at position beatIndex / totalBeats.
    /// Falls back to position-based label if no named role covers this slot.
    /// </summary>
    public BeatRole GetBeatRole(int beatIndex, int totalBeats)
    {
        if (totalBeats <= 1) return Roles.First(r => r.Name == "Catalyst");
        var position = (float)beatIndex / (totalBeats - 1);

        // Find the best-fitting role (prefer roles whose midpoint is closest to position)
        return Roles
            .Where(r => position >= r.PositionMin && position <= r.PositionMax)
            .OrderBy(r => Math.Abs(position - (r.PositionMin + r.PositionMax) / 2f))
            .FirstOrDefault()
            ?? FallbackRole(position);
    }

    private static BeatRole FallbackRole(float position) => position switch
    {
        < 0.25f => Roles.First(r => r.Name == "Set-Up"),
        < 0.50f => Roles.First(r => r.Name == "Fun and Games"),
        < 0.75f => Roles.First(r => r.Name == "Bad Guys Close In"),
        _       => Roles.First(r => r.Name == "Finale"),
    };

    /// <summary>
    /// Returns per-beat structural guidance to inject into the prose generation prompt.
    /// Tells the LLM the beat's structural role and what scene type to write.
    /// </summary>
    public string GetBeatGenerationGuidance(int beatIndex, int totalBeats)
    {
        var role = GetBeatRole(beatIndex, totalBeats);
        var sceneInstruction = role.SceneType == "sequel"
            ? "SCENE TYPE: SEQUEL — This beat is a Reaction beat. The protagonist responds emotionally to the previous disaster, weighs their options (Dilemma), and commits to a new course of action (Decision). The reader should feel the weight of what happened before action resumes."
            : "SCENE TYPE: SCENE — This beat is an Action beat. The protagonist pursues a clear Goal, encounters Conflict that resists them, and ends in a Disaster (yes-but, no-and — never a clean yes). Leave something unresolved.";

        return $"""
            STRUCTURAL ROLE: {role.Name.ToUpperInvariant()}
            {role.Description}

            {sceneInstruction}
            """;
    }

    /// <summary>
    /// Builds the full methodology context block for injection into the OutlineService prompt.
    /// Tells the LLM exactly what beats need to be at what positions.
    /// </summary>
    public string GetOutlineMethodologyPrompt(int targetBeats)
    {
        var beatAssignments = Enumerable.Range(0, targetBeats)
            .Select(i =>
            {
                var role = GetBeatRole(i, targetBeats);
                return $"  Beat {i + 1}/{targetBeats}: {role.Name} — {role.Description}";
            })
            .ToList();

        return $"""
            STORYTELLING METHODOLOGY — STRUCTURAL REQUIREMENTS:

            This story must follow the proven structure of emotionally satisfying narrative.
            Each beat listed below has a specific structural role. Honor these roles exactly.

            BEAT ASSIGNMENTS:
            {string.Join("\n", beatAssignments)}

            SCENE-SEQUEL RULE: Every beat is either a Scene or a Sequel.
            - Scene: Character has Goal → faces Conflict → ends in Disaster (yes-but or no-and). Never "yes."
            - Sequel: Character Reacts emotionally → weighs Dilemma → makes Decision. No action until the decision is made.
            The alternation of Scene and Sequel creates the tension-release rhythm that keeps readers engaged.

            WANT vs NEED: The protagonist must have:
            - WANT: A concrete external goal they're consciously pursuing (the contract, the truth, the escape)
            - NEED: An internal truth they are unconsciously avoiding (trust, forgiveness, accepting loss, belonging)
            The story tests whether the protagonist can sacrifice their Want to achieve their Need.
            State both in the character_arcs. The arc is the journey from Want-driven to Need-earned.

            TENSION CURVE: Tension should follow a dramatic curve — not monotonically rising.
            - Act 1 (setup): moderate rise, 3-5/10
            - Catalyst: spike to 6-7/10
            - Fun and Games: varies 4-7/10 with peaks
            - Midpoint: either a false peak (7-8) or false valley (2-3)
            - Bad Guys Close In: rising 6-8/10
            - All Is Lost: 9/10
            - Dark Night: low, 2-3/10 (quiet before the storm)
            - Finale: 8-10/10, climax then resolution falls to 3-4/10

            THEMATIC ARGUMENT: The theme is not a topic (loyalty, corruption). It is an argument:
            "Loyalty without judgment destroys the people you love." The story must TEST this argument —
            let the antagonist embody the counter-argument — and reach a conclusion.
            """;
    }

    /// <summary>
    /// Returns ideal tension target for a beat based on the dramatic curve.
    /// Use as a suggestion, not a mandate — the outline beat's own tension field takes precedence.
    /// </summary>
    public int GetIdealTension(int beatIndex, int totalBeats)
    {
        if (totalBeats <= 1) return 5;
        var pos = (float)beatIndex / (totalBeats - 1);

        // Dramatic curve: gentle rise → spike at catalyst → fun/games variation →
        // midpoint peak or valley → escalation → all-is-lost spike → dark night dip →
        // finale escalation → resolution fall
        return pos switch
        {
            < 0.10f => 3,   // Opening — low, grounded
            < 0.18f => 5,   // Catalyst — first spike
            < 0.30f => 4,   // Debate / Break Into Two — settling into new world
            < 0.50f => 6,   // Fun and Games — escalating, active
            < 0.55f => 7,   // Midpoint — peak or false valley
            < 0.72f => 7,   // Bad Guys Close In — sustained high
            < 0.80f => 9,   // All Is Lost — maximum pain
            < 0.85f => 2,   // Dark Night — quiet, still
            < 0.92f => 8,   // Break Into Three + Finale begins
            < 0.98f => 10,  // Climax
            _       => 3,   // Resolution — exhale
        };
    }
}
