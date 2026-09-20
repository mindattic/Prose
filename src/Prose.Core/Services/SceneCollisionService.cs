using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Computes what specifically happens when two or more on-page characters' psychology and
/// circumstances collide, BEFORE prose is written — the first concrete step toward the
/// author's 2026-08-10 architecture vision (see memory: project_causal_collision_engine_vision):
/// the enrichment pipeline has ~20 services that each independently produce advisory text
/// (pacing, tension, world state, consequences...), and ProseWriterRouter just concatenates
/// all of it into one prompt for the prose-writing LLM call to reconcile in the same pass it
/// writes prose. There was no step where two characters' states actually got COMBINED into a
/// specific outcome first. This service is that step.
///
/// Deliberately a REFINEMENT layer, not a replacement for the hand-authored beat spine (author's
/// explicit scope decision): it does not decide WHETHER an event happens — BeatGoal still says
/// that — it computes HOW the specific people already on page, with their specific documented
/// psychology, would actually behave when that event happens to them. Its output is advisory
/// guidance injected into the prose prompt (BeatContext.SceneCollisionGuidance), not a hard
/// constraint — same non-blocking posture as every other ProseWriterRouter enrichment service.
///
/// Deliberately reuses XRayContext/WorldStateContext/ConsequenceContext (already assembled by
/// SceneContextAssembler/WorldStateAtBeatService/ConsequenceService) rather
/// than re-querying character psychology from scratch — avoids both a duplicate DB round trip
/// and a fragile new dependency on CharacterEmotionalLedger, which is only populated for books
/// that have run EmotionalLedgerService and would otherwise make this a silent no-op on most
/// of the corpus.
/// </summary>
public class SceneCollisionService(ILlmService llm, ILogger<SceneCollisionService> log)
{
    public sealed record CharacterReaction(string Name, string Reaction);

    public sealed record SceneCollision(
        string Mechanics,
        IReadOnlyList<CharacterReaction> Reactions,
        string? NewConsequence,
        string Rationale);

    /// <summary>
    /// Computes the collision outcome for a beat with 2+ characters on page. Returns null
    /// (never throws) when there isn't enough to compute from, or the LLM call/parse fails —
    /// callers should treat this exactly like every other optional enrichment service and
    /// continue generation without it.
    /// </summary>
    public async Task<SceneCollision?> ComputeAsync(
        IReadOnlyList<string> charactersInScene,
        string xRayContext,
        string worldStateContext,
        string consequenceContext,
        string beatGoal,
        string locationContext,
        CancellationToken ct = default)
    {
        if (charactersInScene.Count < 2) return null;
        if (string.IsNullOrWhiteSpace(xRayContext)) return null;
        if (string.IsNullOrWhiteSpace(beatGoal)) return null;

        const string system =
            "You are a narrative causality engine, not a prose writer. Given specific people's " +
            "documented psychology and their specific circumstance, compute what SPECIFICALLY " +
            "happens when they collide — grounded in the exact Want/Need/Wound/secret/wound " +
            "details given, never generic. Return ONLY the JSON object requested. No prose, no " +
            "markdown fences, no explanation outside the JSON.";

        var worldBlock = string.IsNullOrWhiteSpace(worldStateContext) ? "" : $"\n\nCURRENT WORLD STATE:\n{worldStateContext}";
        var consequenceBlock = string.IsNullOrWhiteSpace(consequenceContext) ? "" : $"\n\nACTIVE CONSTRAINTS:\n{consequenceContext}";
        var locationBlock = string.IsNullOrWhiteSpace(locationContext) ? "" : $"\n\nLOCATION:\n{locationContext}";

        var prompt = $$"""
            PEOPLE ON PAGE (their documented voice, psychology, wounds — the only truth about who
            they are; do not invent traits not implied here):
            {{xRayContext}}{{worldBlock}}{{consequenceBlock}}{{locationBlock}}

            WHAT THIS BEAT IS SUPPOSED TO ACCOMPLISH (already decided — do not change it, compute
            HOW it plays out given exactly these people):
            {{beatGoal}}

            Return a JSON object with these exact keys:
            {
              "mechanics": "<2-4 sentences: the SPECIFIC friction, spark, or consequence that
                results when these particular people's properties meet under this particular
                circumstance — not a generic version of the beat goal, the version that could
                ONLY happen with these specific people>",
              "reactions": [
                {"name": "<character name from the roster above>", "reaction": "<one sentence:
                  their specific, non-generic reaction, grounded in their documented want/need/
                  wound — what they'd actually do, not what a stock character would do>"}
              ],
              "new_consequence": "<one sentence: any new fact/constraint this collision
                establishes going forward, or null if nothing new is established>",
              "rationale": "<one sentence: why this is the specific outcome, tying back to the
                documented psychology above — not restating the mechanics>"
            }
            """;

        try
        {
            var raw = await llm.GenerateAsync(system, prompt, 0.4, 700, null, ct);
            var result = ParseCollisionResponse(raw);
            if (result == null)
                log.LogWarning("SceneCollisionService: LLM returned empty/unparseable mechanics, skipping");
            return result;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "SceneCollisionService: computation failed, continuing without collision guidance");
            return null;
        }
    }

    /// <summary>
    /// Parses the LLM's raw JSON response into a <see cref="SceneCollision"/>. Extracted from
    /// <see cref="ComputeAsync"/> so the parsing logic is directly unit-testable without an LLM
    /// dependency — same pattern as EmotionalDepthService.ParseBeatCurve. Returns null (never
    /// throws to the caller — callers wrap this in their own try/catch) when "mechanics" is
    /// missing/empty, since that's the one field this service cannot function without.
    /// </summary>
    internal static SceneCollision? ParseCollisionResponse(string raw)
    {
        var json = ExtractJson(raw);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var mechanics = root.TryGetProperty("mechanics", out var mp) ? mp.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(mechanics)) return null;

        var reactions = new List<CharacterReaction>();
        if (root.TryGetProperty("reactions", out var rp) && rp.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in rp.EnumerateArray())
            {
                var name = item.TryGetProperty("name", out var np) ? np.GetString() ?? "" : "";
                var reaction = item.TryGetProperty("reaction", out var rxp) ? rxp.GetString() ?? "" : "";
                if (name.Length > 0 && reaction.Length > 0)
                    reactions.Add(new CharacterReaction(name, reaction));
            }
        }

        string? newConsequence = root.TryGetProperty("new_consequence", out var ncp)
            && ncp.ValueKind == JsonValueKind.String
            ? ncp.GetString()
            : null;

        var rationale = root.TryGetProperty("rationale", out var rap) ? rap.GetString() ?? "" : "";

        return new SceneCollision(mechanics, reactions, newConsequence, rationale);
    }

    /// <summary>Formats a computed collision as the context block injected into the prose prompt.</summary>
    public static string FormatForPrompt(SceneCollision collision)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SCENE MECHANICS — computed from these specific people's psychology + circumstance " +
                       "(refines HOW the beat goal plays out; does not change WHAT the beat goal is):");
        sb.AppendLine(collision.Mechanics);
        if (collision.Reactions.Count > 0)
        {
            sb.AppendLine("Per-character reaction (grounded, not generic):");
            foreach (var r in collision.Reactions)
                sb.AppendLine($"- {r.Name}: {r.Reaction}");
        }
        if (!string.IsNullOrWhiteSpace(collision.NewConsequence))
            sb.AppendLine($"New consequence this collision establishes: {collision.NewConsequence}");
        return sb.ToString().TrimEnd();
    }

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw[start..(end + 1)] : raw;
    }
}
