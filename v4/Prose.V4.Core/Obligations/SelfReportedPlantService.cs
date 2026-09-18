using Prose.Core.Interfaces;

namespace Prose.V4.Core.Obligations;

/// <summary>One plant the writer reported immediately after generating the beat that made it.</summary>
public sealed record SelfReportedPlant(string Description);

/// <summary>
/// v4 plan Phase 4, the "self-reported" half of explicit obligations. This is a deliberately
/// different task shape from <c>NarrativeObligationExtractor</c> (v3's blanket LLM-mined obligation
/// miner, measured THIS SAME SESSION at a ~96% false-positive rate on GCOBN — see RFC 0013 §8,
/// 2026-09-17): that extractor reads arbitrary already-written prose and guesses what it "sets up,"
/// with no operating notion of salience. This service asks ONE narrow question scoped to a single
/// beat, immediately after it's generated: "did THIS beat plant something, yes or no, name it or
/// say none" — closer to asking the author what they just did than mining a stranger's paragraph
/// for hidden meaning. Whether that difference in task shape actually produces a different
/// false-positive rate is a hypothesis, not a fact — it must clear the same GCTOC/GCSH/GCNEG/GCOBN
/// calibration before being trusted, exactly like every other instrument in this project.
/// </summary>
public sealed class SelfReportedPlantService
{
    private readonly ILlmService llm;

    public SelfReportedPlantService(ILlmService llm)
    {
        this.llm = llm;
    }

    public async Task<IReadOnlyList<SelfReportedPlant>> ExtractAsync(string beatText, CancellationToken ct = default)
    {
        // Tightened 2026-09-17 after GCTOC/GCSH calibration measured real, repeated over-triggering
        // (~3.1-3.2 plants/beat, vs. GCOBN's ~0.05) on classic/mystery-register prose — the false
        // positives read were consistently broad thematic/allegorical/backdrop commentary with no
        // single concrete, nameable referent (e.g. "the unresolved political tension between England
        // and France," "the Woodman and Farmer as Fate and Death"), never a texture-vs-plant error of
        // the GCOBN kind. The two-part HARD RULE + contrastive examples below target that specific
        // failure shape rather than re-deriving the whole prompt from scratch.
        const string system = "You identify narrative promises a piece of prose makes to its reader — nothing else. Most beats promise nothing; say so plainly when that's true.";
        var user = $"""
            BEAT TEXT (one scene from a story):
            {beatText}

            Question: did THIS beat introduce something a reader will actively expect to see
            explained, used, or resolved LATER in the story?

            HARD RULE, both parts must hold: (1) the plant must be a SPECIFIC, CONCRETE thing — one
            named or nameable object, person, place, or an explicit question the text itself poses in
            those terms ("what did it mean?", "who was she?") — never a theme, mood, historical
            backdrop, or authorial commentary with no single referent; and (2) the text must mark that
            specific thing as unresolved or mysterious RIGHT NOW, not merely mention it while telling
            the story.

            EXAMPLE — a real plant: "A message arrives reading only 'RECALLED TO LIFE.' The messenger
            frowns — he doesn't understand it either." → PLANT (one concrete message, explicitly
            marked as not understood).

            EXAMPLE — NOT a plant: "The chapter opens on how both England and France were ruled by
            kings convinced their nations were untroubled, while want and revolution gathered beneath
            them." → NOT a plant (a thematic/historical backdrop with no single object, person, or
            posed question to track and resolve).

            EXAMPLE — NOT a plant: "A room being described, a character doing something mundane, or
            an object being used and set back down" is texture, not a promise, even if described at
            length.

            List each real plant on its own line, in the form:
            PLANT: <the one concrete referent> — <the specific unresolved question about it>

            If this beat plants nothing meeting BOTH parts of the rule, output exactly:
            NONE

            Do not pad the list to find something. Most beats — including most beats in classic or
            densely-plotted fiction — plant nothing by this stricter standard.
            """;

        var raw = await llm.GenerateAsync(system, user, temperature: 0.0, maxTokens: 400, ct: ct);
        return raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.StartsWith("PLANT:", StringComparison.OrdinalIgnoreCase))
            .Select(l => new SelfReportedPlant(l[6..].Trim()))
            .ToList();
    }
}
