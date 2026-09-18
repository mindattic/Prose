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
        const string system = "You identify narrative promises a piece of prose makes to its reader — nothing else. Most beats promise nothing; say so plainly when that's true.";
        var user = $"""
            BEAT TEXT (one scene from a story):
            {beatText}

            Question: did THIS beat introduce something — an object, an unnamed person, an unresolved
            question, a foreshadowed event — that a reader will actively expect to see explained,
            used, or resolved LATER in the story? This is about SALIENCE, not description. A room
            being described, a character doing something mundane, or an object being used and set
            back down is NOT a plant. A plant is something the text itself marks as mattering: an
            unanswered question stated outright, an object given unusual narrative weight, a person
            introduced in a way that signals they matter beyond this scene.

            List each one on its own line, in the form:
            PLANT: <one sentence describing what was planted and why it reads as a promise, not texture>

            If this beat plants nothing, output exactly:
            NONE

            Do not pad the list to find something. Most ordinary beats plant nothing at all.
            """;

        var raw = await llm.GenerateAsync(system, user, temperature: 0.0, maxTokens: 400, ct: ct);
        return raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.StartsWith("PLANT:", StringComparison.OrdinalIgnoreCase))
            .Select(l => new SelfReportedPlant(l[6..].Trim()))
            .ToList();
    }
}
