using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Distills the craft science from Stephen King's "On Writing" (2000) and Will Storr's
/// "The Science of Storytelling" (2019) into active prompt companions injected at beat-write time.
///
/// This is NOT a scorer or reviewer. It is a prevention layer — it injects precise guidance
/// into the generation prompt so the LLM avoids the failure modes both authors diagnose:
///   - Characters who act outside their psychometric profile (Storr: "sacred flaw" consistency)
///   - "And then, and then" plotting (Storr: neural narrative / because-chains)
///   - Status stagnation across a scene (Storr: status games are universal and load-bearing)
///   - Abstract adjectives over specific sensory detail (Storr: the hallucination model)
///   - Adverbs in attribution, passive voice, wardrobe inventory (King: the anti-pattern list)
///   - Theme imposed instead of emerging (King: theme is second-draft work)
///   - Characters who are convenient functions, not people with wrong beliefs (Storr: sacred flaw)
///   - Dialogue that tells instead of revealing (King + Storr: dialogue as character excavation)
///   - A plot that drives characters instead of characters who drive the story (King: situation vs plot)
///   - The curiosity gap closing too early or opening without resolution (Storr: curiosity is dopamine)
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

    // ── Anti-patterns (King + Storr cross-referenced) ─────────────────────────

    private static readonly string[] KingAntiPatterns =
    [
        "adverbs in dialogue attribution — use 'said' only; no 'whispered urgently', no 'grated', no 'gasped'",
        "passive voice — write 'Kyle pulled the trigger', not 'the trigger was pulled'",
        "physical-trait shortcuts — no 'sharply intelligent blue eyes' or 'arrogant cheekbones' as character shorthand",
        "wardrobe inventory — no cataloguing clothing unless the clothing IS the beat",
        "the Zen simile — a comparison that illuminates nothing ('patient as a man waiting for a turkey sandwich')",
        "clichéd similes — 'ran like a madman', 'pretty as a summer day'; freshness or nothing",
        "announcing character state — 'Annie was angry' is a loss; show the jaw, the stillness, the word she didn't say",
        "research as foreground — canon detail enriches; it does not lecture",
        "back story in the front — earn exposition; backstory belongs in the back",
        "starting with theme — theme is found in revision, not imposed in the first beat",
        "artificial profundity — symbolism adorns; it does not substitute for story",
    ];

    private static readonly string[] StorrAntiPatterns =
    [
        "bolt-on quirks instead of a sacred flaw — a character trait that doesn't generate behavior is decoration",
        "'and then' plotting — every scene must connect with 'because', not 'and then'",
        "expository scene that doesn't advance the plot AND stand alone dramatically — it is superfluous or wrongly written",
        "over-explaining origin damage — hints and behavioral residue are more profound than direct statement",
        "passive protagonist — the character who merely endures is not a protagonist; characters must want, act, choose",
        "clichéd metaphors — worn metaphors no longer fire neural networks; they convey nothing",
        "abstract adjectives ('terrible', 'delightful') — describe so the reader feels it, do not name the feeling",
        "milieu as substitute for character — compelling world + generic occupant = hollow spectacle",
        "literal final battle — the real climax is internal; the surface fight is its symbolic expression",
        "goodness as heroism — virtue alone generates no drama; heroism requires a flaw being overcome",
        "single-layer story — surface action without subconscious transformation is 'light and sound'",
        "vague sacred flaw — 'he is controlling' tells nothing; flaw must generate a specific suite of behaviors",
    ];

    // ── StoryScope anti-patterns (University of Maryland / Google DeepMind, 2025) ─
    // From a 61,608-story study: narrative-structure classifiers distinguish human from AI
    // fiction at 93.2% accuracy without reading a single word of prose. These are the
    // structural decisions that give AI-written stories away.

    private static readonly string[] StoryScopeAntiPatterns =
    [
        "narratorial moral gloss — the beat that ends with the narrator labeling what it 'meant'; let the scene deliver its verdict in images and action; the reader produces the theme, the narrator does not announce it",
        "philosophy-seminar dialogue — characters debating abstract positions at length; dialogue is a status battle between two people who want different things, not a symposium on ideas",
        "all-embodied emotion — routing every emotional cue through body sensations (tightening chest, cold sweat, caught breath) with no explicit labels ever; use 'she was afraid' or 'he was furious' once per scene — the direct label earns authority precisely because the body work surrounds it",
        "clean internal resolution exit — a beat that closes with the protagonist achieving internal understanding or peace; internal states may shift, but the external situation must stay open, worsen, or complicate",
        "description-first character entry — a new character who arrives as a physical inventory before doing or saying anything; introduce through action in the scene or through how another character responds to their presence",
        "front-loaded revelation — disclosing information in the order events happened rather than the order they land hardest; the fact that recontextualizes everything belongs near the end of the beat, not the opening",
        "flat event escalation — each beat registering at the same emotional intensity as the last; every beat must feel larger, more costly, or more irreversible than what came before; a plateau of events at equal weight is the single strongest AI fiction signal in structural classifier studies",
        "event monoculture — writing three confrontations in a row, or three discoveries; vary the event type: confession → chase → ceremony → negotiation → ambush → loss; sameness of event type is a measurable AI fingerprint",
        "reflexive epilogue — closing with a retrospective narration of what the story 'meant' from an outside-time vantage; the story ends on the last event, not a narrator's commentary on its significance",
    ];

    // ── Curiosity gap by arc stage (Storr: curiosity is pleasantly unpleasant dopamine) ──

    private static string GetCuriosityInstruction(ChangeArcStage stage, BeatMode mode) => stage switch
    {
        ChangeArcStage.FlawEnthroned =>
            "CURIOSITY GAP — OPEN: Establish the protagonist's world and flaw as seemingly adequate. Plant the question 'what could possibly crack this?' without answering it. The gap is just beginning to open.",

        ChangeArcStage.IgnitionPoint =>
            "CURIOSITY GAP — WIDEN: The unexpected change has struck. The dramatic question ('Who is this person really?') is now fully open. Do NOT answer it. Let the protagonist overreact in a way that precisely fits their sacred flaw — that overreaction IS the gap widening.",

        ChangeArcStage.FlawTested =>
            "CURIOSITY GAP — TEASE: The flaw is being tested but not broken. Something is working — and something is subtly wrong. Give the reader enough to form a theory about what will crack, then deny them confirmation. Anticipation is more powerful than the event.",

        ChangeArcStage.MidpointCommitment =>
            "CURIOSITY GAP — PIVOT: The flaw has failed. Commitment to change (or refusal) is now locked in. A new question opens: 'Can the protagonist survive what the flaw was protecting them from?' The gap transforms — do not close the original one, evolve it.",

        ChangeArcStage.WorstCaseRealised =>
            "CURIOSITY GAP — MAXIMUM TENSION: Everything the flaw protected against is now happening. This beat must be the most intense thing in the story so far — flat escalation (each beat at the same intensity) is the single strongest AI fiction signal in classifier studies; do not plateau here. The reader knows the dramatic question must be answered soon — use that expectation as pressure. Do NOT resolve it here; let the reader squirm.",

        ChangeArcStage.GodMoment =>
            "CURIOSITY GAP — CLOSE: The dramatic question is being answered. The closing must have sufficient force that the reader feels it as permanent. Do not soft-pedal the answer. The God Moment delivers control — internal or external — definitively. End on the event itself or the last image of it. Do NOT follow with a retrospective narration explaining what the story meant — that is the narrator's vanity, not the story's truth. Avalanche endings over quiet ones.",

        _ => ""
    };

    // ── Status dynamics (Storr: status is computed in 1/10th of a second; universal) ──

    private static string GetStatusInstruction(BeatMode mode) => mode switch
    {
        BeatMode.Dialogue =>
            "STATUS DYNAMICS: Every line of dialogue is a status move. Who is dominant shifts sentence by sentence. Track it. The character who is losing status reaches for a maneuver — humor, deflection, an unexpected truth, silence. Show the move.",

        BeatMode.Combat =>
            "STATUS DYNAMICS: Combat is a status annihilation machine. Track who holds status at each exchange — it shifts with every blow. Humiliation (removal of the ability to claim status) is more dangerous than injury. The loser's response to humiliation drives the scene's aftermath.",

        BeatMode.EmotionalClimax =>
            "STATUS DYNAMICS: Emotional confrontations are status battles conducted with revelation instead of weapons. Who holds the most damaging truth holds status. Watch for the moment the power inverts — it is the beat's climax.",

        _ =>
            "STATUS DYNAMICS: Every scene has a status hierarchy. Mark who holds status at the scene's open and who holds it at the close. If status does not shift, the scene does not move the story. A rise in one character's status requires a fall in another's."
    };

    // ── Theory of Mind reminder (Storr: characters confabulate; their model of others is wrong) ──

    private static string GetTheoryOfMindInstruction(int charactersOnScreen) =>
        charactersOnScreen <= 1 ? "" :
        """
        THEORY OF MIND: Every character has a model of every other character's inner life — and those models are WRONG about 65-80% of the time. This wrongness is the engine of interpersonal drama.
        • Show what Character A THINKS Character B is thinking — and let the prose subtly reveal that A is mistaken.
        • Characters confabulate: their stated reasons for their actions are post-hoc rationalizations. The narrator does not have to expose this — the behavior does.
        • Dialogue is two monologues clashing. Each speaker is primarily advancing their own model of the world.
        • The most revealing moments are involuntary: a gesture, a word that escapes before they mean it, a silence in the wrong place.
        """;

    // ── King's prose mechanics (pure craft, both books converge) ─────────────

    private static readonly string ProseCoreMechanics = """
        PROSE MECHANICS (King's toolbox — these are non-negotiable):
        • Vocabulary: use the first word that comes to mind if it is accurate. Do not dress up or reach for synonyms.
        • Attribution: "said" only. No adverbs on said. No "grated / gasped / jerked out."
        • Active voice: 'The door slammed' not 'The door was slammed by the wind.'
        • Description: 3-5 well-chosen specific details stand for everything. First-visualized details are almost always the truest.
        • Sensory specificity: at least one non-visual sense per scene (sound, smell, texture, temperature). The brain simulates what the senses provide.
        • Paragraph rhythm: short paragraphs = fast, nervous. Dense paragraphs = weight, inevitability. Match to the beat's emotional register.
        • Metaphor must illuminate: if a comparison does not clarify, remove it. Clichéd metaphors no longer fire neural networks — they are dead noise.
        • Body before mind by default: 'Her hand found the door frame before she knew she was moving.' But one explicit emotion label per scene used deliberately — 'she was afraid', 'he was furious' — earns authority precisely because the body work surrounds it. The failure mode is exclusive reliance on either: all body-language with no label is AI-distinctive; all label with no body is lazy.
        """;

    // ── Situation vs Plot (King's core doctrine) ──────────────────────────────

    private static readonly string SituationNotPlot = """
        CHARACTER DRIVES STORY — NOT PLOT:
        • Characters are not plot functions. They are people with wrong beliefs who are trying to get what they want.
        • What happens in this beat should emerge from who the characters ARE under this pressure — not from where the outline says the story must go.
        • If a character acts in a way that serves the plot but contradicts who they've been shown to be, the beat is false.
        • Ask: given this person's sacred flaw, given what they want and fear, given who else is in this room — what would THEY actually do?
        """;

    // ── StoryScope: human narrative markers (what makes fiction read as human) ──
    // These are the positive structural patterns that human fiction uses and AI defaults away from.
    // Derived from University of Maryland / Google DeepMind StoryScope study, 2025.

    private static readonly string HumanNarrativeMarkers = """
        HUMAN NARRATIVE MARKERS — structural choices that distinguish human fiction:
        • Escalate: this beat must feel larger, more costly, or more irreversible than the last. Ask: what is the single highest-stakes thing that could happen right now given what has been established?
        • Vary event type: identify what the last two beats were (confrontation, discovery, chase, confession, ceremony, ambush, negotiation, loss, betrayal). This beat should be a different type.
        • Back-load the revelation: if this beat contains a fact that recontextualizes what came before, hold it until the end of the beat — do not open with it.
        • Leave the moral question open: do not resolve who was right. The protagonist's choice should have a genuine cost on the path not taken. Ambivalence is not weakness; it is human.
        • Introduce new characters through what they do and say — not through a physical description given before they act or speak.
        """;

    // ── Neural narrative: because-chains (Storr's brain-science finding) ──────

    private static readonly string NeuralNarrativeRules = """
        NEURAL NARRATIVE — CAUSAL CHAIN:
        • Every scene must connect to the next with a 'because', not an 'and then.'
        • The brain is a cause-and-effect machine. Scenes that don't cause the next scene feel abandoned.
        • This beat must be a consequence of what preceded it AND a cause of what follows.
        • The dramatic question must shift — slightly closer to an answer, or reopened in a new form.
        • If you cannot state the causal link ('this happened because...'), the beat is a free-floating event, not a story beat.
        """;

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

    // ── Specificity mandate (Storr: the brain hallucinates from specific material) ──

    private static string GetSpecificityInstruction(BeatMode mode) => mode switch
    {
        BeatMode.Combat =>
            "SPECIFICITY: In combat, geometry is voice. Name the hand, the angle, the surface. 'He hit her' tells nothing. 'The heel of his palm caught the underside of her jaw' tells everything. Three specific physical qualities per key contact.",

        BeatMode.Transition or BeatMode.Narrative =>
            "SPECIFICITY: Immersive beats live on sensory texture. Three non-visual details minimum (smell, temperature, sound, texture). The brain hallucinates the scene from specific material — abstract language ('it was dark', 'the room was tense') gives it nothing to work with.",

        _ =>
            "SPECIFICITY: Abstract adjectives are thin gruel. 'Terrible', 'delightful', 'impressive' — these name the feeling instead of creating it. Describe so the reader produces the adjective themselves. Specific physical detail (three qualities minimum) triggers the neural model-building mechanism."
    };

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
    /// Returns the full StoryScienceService guidance block for injection into the beat prompt.
    /// Contains: psychometric consistency, status dynamics, curiosity gap, causal chain,
    /// theory of mind, sensory specificity, prose mechanics, anti-patterns.
    /// </summary>
    public string GetBeatGuidance(
        BeatContext context,
        int beatIndex,
        int totalBeats,
        BeatMode mode)
    {
        var stage = ClassifyArcStage(beatIndex, totalBeats);
        var characters = context.CharactersInScene?.ToList() ?? new List<string>();
        var hasXRay = !string.IsNullOrEmpty(context.XRayContext);

        var parts = new List<string>
        {
            "## STORY SCIENCE — craft laws in force for this beat (non-negotiable):",
            "",
            NeuralNarrativeRules,
            "",
            GetStatusInstruction(mode),
            "",
            GetCuriosityInstruction(stage, mode),
            "",
            GetSpecificityInstruction(mode),
            "",
        };

        if (hasXRay && characters.Count > 0)
        {
            parts.Add(GetSacredFlawReminder(characters, context.XRayContext));
            parts.Add("");
        }

        var tomGuidance = GetTheoryOfMindInstruction(characters.Count);
        if (tomGuidance.Length > 0)
        {
            parts.Add(tomGuidance);
            parts.Add("");
        }

        if (mode == BeatMode.Dialogue || mode == BeatMode.EmotionalClimax)
        {
            parts.Add(DialogueHonestyRules);
            parts.Add("");
        }

        parts.Add(ProseCoreMechanics);
        parts.Add("");
        parts.Add(SituationNotPlot);
        parts.Add("");
        parts.Add(HumanNarrativeMarkers);
        parts.Add("");
        parts.Add(GetAntiPatternBlock(mode));

        return string.Join("\n", parts).Trim();
    }

    private static string GetAntiPatternBlock(BeatMode mode)
    {
        List<string> selected;

        if (mode == BeatMode.Combat)
        {
            selected =
            [
                KingAntiPatterns[0],        // adverbs in attribution
                KingAntiPatterns[1],        // passive voice
                KingAntiPatterns[6],        // announcing character state
                StorrAntiPatterns[6],       // abstract adjectives
                StorrAntiPatterns[1],       // and-then plotting
                StorrAntiPatterns[7],       // milieu as substitute
                StoryScopeAntiPatterns[6],  // flat event escalation
                StoryScopeAntiPatterns[7],  // event monoculture
            ];
        }
        else
        {
            selected =
            [
                ..KingAntiPatterns.Take(5),
                ..StorrAntiPatterns.Take(4),
                StoryScopeAntiPatterns[0],  // narratorial moral gloss
                StoryScopeAntiPatterns[2],  // all-embodied emotion
                StoryScopeAntiPatterns[6],  // flat event escalation
            ];
        }

        return "ANTI-PATTERNS — these are failures, not choices:\n" +
               string.Join("\n", selected.Select(p => $"• {p}"));
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
