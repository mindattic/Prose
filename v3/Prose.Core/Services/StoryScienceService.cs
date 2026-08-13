using Prose.Core.Models.Canon;

namespace Prose.Core.Services;

/// <summary>
/// Distills the craft science from Stephen King's "On Writing" (2000) and Will Storr's
/// "The Science of Storytelling" (2019) into active prompt companions injected at beat-write time.
///
/// Shrunk 2026-08-13 (plan "Separating rigor from fluidity"): this used to inject ~9 always-on
/// sub-blocks into every beat's prompt regardless of whether that beat had the problem being
/// pre-empted — one of only five context mechanisms firing on 100% of beats (confirmed via
/// <c>BeatServiceLog</c> telemetry). Now injects only what a wrong first draft would force a
/// rewrite on:
///   - Characters who act outside their psychometric profile (Storr: "sacred flaw" consistency)
///   - Dialogue that tells instead of revealing (King + Storr: dialogue as character excavation)
///   - Adverbs in attribution, passive voice, missing sensory grounding (King's prose mechanics)
///   - A one-line causal-chain reminder (Storr: neural narrative / because-chains)
///
/// Everything else this class used to inject preemptively — curiosity-gap engineering, status
/// dynamics, specificity mandates, theory-of-mind prompts, "character drives plot" doctrine,
/// escalation/event-variety markers, and the full anti-pattern checklist — are scene-shape
/// judgments that need the *finished* beat read in context to make, not a prediction in advance.
/// They moved to <see cref="CraftQualityService"/> (<c>BeatLensServices.cs</c>), a post-write
/// lens wired into <see cref="BeatAuditService"/>'s chapter-close self-repair loop — the same
/// treatment <c>CausalityService</c> already gives causal-chain quality. Mirrors
/// <see cref="DelightProseGuidance.GetForMode"/>'s existing DO-side pattern (2-3 targeted rules
/// per beat mode, not all 13 moves every beat) applied to the DON'T side.
///
/// WIRED INTO: ProseWriterRouter.WriteAsync() — fires every beat, zero LLM cost.
/// </summary>
public class StoryScienceService
{
    // ── Change Arc Stages (Storr: 5-act sacred-flaw arc) ─────────────────────

    public enum ChangeArcStage
    {
        /// <summary>Flaw established + comfortable; protagonist "succeeds" by their flawed model.</summary>
        FlawEnthroned,
        /// <summary>Ignition point — unexpected change strikes; flaw overreacts. "Who is this person?" opens.</summary>
        IgnitionPoint,
        /// <summary>Flaw tested; small wins, but something is wrong. Protagonist doubles down or experiments.</summary>
        FlawTested,
        /// <summary>Midpoint commitment — the flaw fails catastrophically; protagonist commits to change (or refuses).</summary>
        MidpointCommitment,
        /// <summary>Everything the flaw protected against now actually happens. Lowest point. Internal collapse.</summary>
        WorstCaseRealised,
        /// <summary>God Moment — protagonist chooses new self or final tragic refusal. Dramatic question answered.</summary>
        GodMoment,
    }

    // ── King's prose mechanics (pure craft, both books converge) ─────────────

    /// <summary>Trimmed 2026-08-13 (plan "Separating rigor from fluidity") to the highest-yield,
    /// sentence-level mechanics that are cheap to get right at write-time and expensive to
    /// retrofit — dropped vocabulary/description-count/paragraph-rhythm/metaphor guidance, which
    /// are scene-shape judgments a repair-time read can catch just as well (see
    /// <see cref="CraftQualityService"/>). Kept the four rules where a wrong first pass forces a
    /// full sentence-level rewrite rather than a targeted patch.</summary>
    private static readonly string ProseCoreMechanics = """
        PROSE MECHANICS (King's toolbox — these are non-negotiable):
        • Attribution: "said" only. No adverbs on said. No "grated / gasped / jerked out."
        • Active voice: 'The door slammed' not 'The door was slammed by the wind.'
        • Sensory specificity: at least one non-visual sense per scene (sound, smell, texture, temperature). The brain simulates what the senses provide.
        • Body before mind, always: 'Her hand found the door frame before she knew she was moving.' Nobody names a feeling (CRAFT.md §4) — the feeling arrives as an object or gesture, never a stated label ('she was afraid', 'he was furious'). All body-language with no label is the goal, not a risk to hedge against.
        """;

    // ── Neural narrative: because-chains (Storr's brain-science finding) ──────

    /// <summary>Collapsed 2026-08-13 (plan "Separating rigor from fluidity") from a five-line
    /// lecture to one reminder: whether a beat actually connected via "because" vs. "and then"
    /// is exactly what <c>CausalityService</c>'s existing post-write lens already audits over
    /// the whole node in context — a one-line write-time reminder plus a real after-the-fact
    /// check beats a paragraph on every beat regardless of need.</summary>
    private const string NeuralNarrativeOneLine =
        "CAUSAL CHAIN: this beat must follow from what came before with \"because,\" not \"and then\" — a consequence of what preceded it, a cause of what follows.";

    // ── Psychometric profile / sacred flaw enforcement (Storr's core thesis) ──

    private static string GetSacredFlawReminder(List<string> charactersInScene, string xRayContext)
    {
        if (charactersInScene.Count == 0 || string.IsNullOrEmpty(xRayContext)) return "";

        return $"""
            PSYCHOMETRIC CONSISTENCY — SACRED FLAW:
            Every character on screen has a documented psychology (see SCENE X-RAY above). Their sacred flaw is:
            • A specific wrong belief about how to safely navigate the world — not a quirk, a theory of control.
            • The filter through which they interpret every event in this scene.
            • The thing they are most irrational about, even when they believe they are most reasonable.

            ENFORCE: Characters({string.Join(", ", charactersInScene)}) may only act in ways consistent with their documented:
              core_fears / core_desires / coping_mechanisms / blind_spots / speech_patterns

            Any character action that a reader would call "out of character" means the sacred flaw has been ignored.
            The flaw is INVISIBLE to its owner — they do not experience it as a flaw; they experience it as self-evident truth.
            Do not have characters recognize their own flaw unless this is the God Moment.
            """;
    }

    // ── Dialogue rules (King + Storr convergence) ─────────────────────────────

    private static readonly string DialogueHonestyRules = """
        DIALOGUE RULES (King + Storr):
        • Every line reveals character — what they want, what they're hiding, how they see the world, their status in the room.
        • Dialogue is NOT exposition delivery. Characters do not explain themselves or their history.
        • What is left unsaid is as important as what is said. Silences, deflections, non-sequiturs are character.
        • Honesty rule (King): if a character would say 'shit', write 'shit'. Softening dialogue breaks the contract with the reader.
        • Two monologues clashing (Davies): both speakers are advancing their own model of the world. Real conversation rarely achieves mutual understanding.
        • Subtext is load-bearing: what is said on the surface and what is meant beneath it should rarely be the same thing.
        """;

    // ── Main entry point ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the (shrunk 2026-08-13, plan "Separating rigor from fluidity") StoryScienceService
    /// guidance block for injection into the beat prompt. Contains only what a wrong first draft
    /// would force a rewrite on: sacred-flaw psychology (when characters are on page), dialogue
    /// subtext (when the beat has dialogue), sentence mechanics, and a one-line causal-chain
    /// reminder. Status dynamics, curiosity-gap engineering, specificity mandates, theory-of-mind
    /// prompts, "character drives plot" doctrine, escalation/event-variety markers, and the
    /// anti-pattern checklist moved to <see cref="CraftQualityService"/> — a post-write lens that
    /// reads the finished beat in context, the same way <c>CausalityService</c> already does for
    /// causal-chain quality. These are scene-shape judgments that need the finished beat to
    /// evaluate; a prompt instruction can only guess in advance. This was one of only five
    /// context blocks firing on 100% of beats (confirmed via <c>BeatServiceLog</c> telemetry) —
    /// unconditional density on every beat regardless of whether that beat had the problem being
    /// pre-empted. Mirrors <see cref="DelightProseGuidance.GetForMode"/>'s existing DO-side
    /// pattern (2-3 targeted rules per mode, not all 13 moves every beat) applied to the DON'T side.
    /// </summary>
    public string GetBeatGuidance(
        BeatContext context,
        int beatIndex,
        int totalBeats,
        BeatMode mode)
    {
        var characters = context.CharactersInScene?.ToList() ?? new List<string>();
        var hasXRay = !string.IsNullOrEmpty(context.XRayContext);

        var parts = new List<string>
        {
            "## STORY SCIENCE — craft laws in force for this beat (non-negotiable):",
            "",
            NeuralNarrativeOneLine,
            "",
        };

        if (hasXRay && characters.Count > 0)
        {
            parts.Add(GetSacredFlawReminder(characters, context.XRayContext));
            parts.Add("");
        }

        if (mode == BeatMode.Dialogue || mode == BeatMode.EmotionalClimax)
        {
            parts.Add(DialogueHonestyRules);
            parts.Add("");
        }

        parts.Add(ProseCoreMechanics);

        return string.Join("\n", parts).Trim();
    }

    /// <summary>
    /// Classify the beat's position on the 5-act sacred-flaw change arc.
    /// </summary>
    public static ChangeArcStage ClassifyArcStage(int beatIndex, int totalBeats)
    {
        if (totalBeats <= 1) return ChangeArcStage.FlawTested;
        var pos = (float)beatIndex / (totalBeats - 1);

        return pos switch
        {
            < 0.10f => ChangeArcStage.FlawEnthroned,
            < 0.22f => ChangeArcStage.IgnitionPoint,
            < 0.50f => ChangeArcStage.FlawTested,
            < 0.58f => ChangeArcStage.MidpointCommitment,
            < 0.80f => ChangeArcStage.WorstCaseRealised,
            _       => ChangeArcStage.GodMoment,
        };
    }

    /// <summary>
    /// Returns a standalone psychometric audit prompt — used by the logic-sweep
    /// harness to test whether a written beat respects each character's psychology.
    /// NOT injected into the generation prompt; consumed by audit tools.
    /// </summary>
    public static string GetPsychometricAuditPrompt(string beatProse, string characterName, CharacterData character)
    {
        var psy = character.Psychology;

        var flawProfile = new List<string>();
        if (psy.CoreFears.Count > 0)
            flawProfile.Add($"Core fears: {string.Join("; ", psy.CoreFears)}");
        if (psy.CoreDesires.Count > 0)
            flawProfile.Add($"Core desires: {string.Join("; ", psy.CoreDesires)}");
        if (psy.CopingMechanisms.Count > 0)
            flawProfile.Add($"Coping mechanisms: {string.Join("; ", psy.CopingMechanisms)}");
        if (psy.BlindSpots.Count > 0)
            flawProfile.Add($"Blind spots: {string.Join("; ", psy.BlindSpots)}");
        if (psy.Secret.Length > 0)
            flawProfile.Add($"Hidden secret (never disclosed on-page): {psy.Secret}");

        var sp = character.SpeechPatterns;
        var speechProfile = new List<string>();
        if (sp.Cadence.Length > 0) speechProfile.Add($"Cadence: {sp.Cadence}");
        if (sp.VerbalTics.Count > 0) speechProfile.Add($"Verbal tics: {string.Join("; ", sp.VerbalTics)}");
        if (sp.Vocabulary.Length > 0) speechProfile.Add($"Vocabulary markers: {sp.Vocabulary}");

        return $"""
            PSYCHOMETRIC AUDIT — {characterName.ToUpperInvariant()}

            CANON PSYCHOLOGY:
            {string.Join("\n", flawProfile)}

            SPEECH PATTERNS:
            {string.Join("\n", speechProfile)}

            BEAT PROSE TO AUDIT:
            {beatProse}

            AUDIT QUESTIONS:
            1. Does every action {characterName} takes in this beat follow logically from their documented fears, desires, and coping mechanisms?
            2. Is there any moment where {characterName} acts in a way that contradicts their sacred flaw — i.e., has access to the truth about themselves that they canonically lack?
            3. Does the dialogue sound like {characterName}'s documented cadence and vocabulary, or is it genre-generic?
            4. Are there any emotional states named directly (e.g. "felt afraid") that should instead be rendered as physical behavior?
            5. Does {characterName}'s theory of mind about other characters reflect documented blind spots — do they make wrong assumptions they would predictably make?

            Return: PASS | MINOR drift | MODERATE drift | MAJOR violation — then one sentence per finding.
            """;
    }

    /// <summary>
    /// Returns a narrative-chart-ready arc description for a story, describing
    /// which beats map to which ChangeArcStage. Used by NarrativeChartService.
    /// </summary>
    public static List<(int BeatIndex, ChangeArcStage Stage, string Label)> GetArcMap(int totalBeats)
    {
        return Enumerable.Range(0, totalBeats)
            .Select(i =>
            {
                var stage = ClassifyArcStage(i, totalBeats);
                var label = stage switch
                {
                    ChangeArcStage.FlawEnthroned    => "Flaw Enthroned",
                    ChangeArcStage.IgnitionPoint     => "Ignition",
                    ChangeArcStage.FlawTested        => "Flaw Tested",
                    ChangeArcStage.MidpointCommitment => "Midpoint",
                    ChangeArcStage.WorstCaseRealised => "Worst Case",
                    ChangeArcStage.GodMoment         => "God Moment",
                    _                                => "Unknown",
                };
                return (i, stage, label);
            })
            .ToList();
    }
}
