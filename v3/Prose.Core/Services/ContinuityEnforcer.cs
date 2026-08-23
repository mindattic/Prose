using Prose.Core.Interfaces;

namespace Prose.Core.Services;

public class ContinuityViolation
{
    public string EntityName { get; set; } = "";
    public string Predicate { get; set; } = "";
    public string EstablishedFact { get; set; } = "";
    public string Explanation { get; set; } = "";
}

/// <summary>
/// LLM-based post-generation check, same pattern and posture as BehavioralInvariantEnforcer/
/// GearCarryEnforcer. Loads the same CANONICAL/CONFIRMED ContinuityClaims ProseWriterRouter's
/// "## ESTABLISHED CANON — do not contradict these facts" prompt block is built from (same
/// name-matching filter, so this checks EXACTLY what the LLM was shown) and asks whether the
/// freshly-generated beat contradicts any of them.
///
/// Closes a real gap (2026-08-22): ContinuityService's canon block was pure prompt-side hope
/// with zero verification that the LLM actually obeyed it — the only downstream check was
/// Trinity Reconciliation's asynchronous, indirect fact re-extraction sweep, which can lag by
/// sessions and never checks the SPECIFIC beat against the SPECIFIC constraints it was shown.
/// </summary>
public class ContinuityEnforcer
{
    private readonly ContinuityService continuity;
    private readonly ILlmService llm;

    private const string SystemPrompt = """
        You are a continuity editor checking prose against established canon facts.
        Given a list of established facts about characters and a prose passage, identify any
        line where the prose contradicts one of the facts.

        Respond with a JSON array of violations, or an empty array [] if none exist.
        Each violation: {"entity":"...", "predicate":"...", "fact":"...", "explanation":"..."}
        Be strict: flag only clear, factual contradictions — not interpretive differences, and
        not facts the passage simply doesn't mention.
        Return only the JSON array, no other text.
        """;

    public ContinuityEnforcer(ContinuityService continuity, ILlmService llm)
    {
        this.continuity = continuity;
        this.llm = llm;
    }

    /// <summary>
    /// Checks <paramref name="beatText"/> against the CANONICAL/CONFIRMED continuity claims for
    /// the named characters in scene. Returns an empty list when no matching claims exist (a
    /// real, distinguishable "nothing to check" case, not an evaluation failure). Throws if the
    /// LLM call itself fails or returns an unparseable response — deliberately, so callers cannot
    /// mistake "could not evaluate" for "evaluated, no violations found" (same fail-open bug class
    /// BehavioralInvariantEnforcer.EnforceAsync's own remarks document and fix).
    /// </summary>
    public async Task<List<ContinuityViolation>> EnforceAsync(
        string beatText, IReadOnlyList<string> characterNamesInScene, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(beatText) || characterNamesInScene.Count == 0) return [];

        var sceneNames = characterNamesInScene
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (sceneNames.Count == 0) return [];

        // Same match rule as ProseWriterRouter's canon block (Fix 3): startswith either
        // direction, so "Yemina" (scene name) matches claim EntityName "Yemina Fola" and
        // vice versa.
        var claims = continuity.GetByStatus("CANONICAL")
            .Concat(continuity.GetByStatus("CONFIRMED"))
            .Where(c => sceneNames.Any(n =>
                c.EntityName.StartsWith(n, StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith(c.EntityName, StringComparison.OrdinalIgnoreCase)))
            // 2026-08-23: exclude point-in-time state. A stored location_current /
            // appearance_in_story from an earlier book is not something a later book can
            // "contradict" — the character moved. Including them made this check report a
            // violation on essentially every beat of a sequel (see
            // ContinuityService.VolatilePredicates for the full reasoning).
            .Where(c => !ContinuityService.IsVolatilePredicate(c.Predicate))
            .Take(40)
            .ToList();
        if (claims.Count == 0) return [];

        var factBlock = string.Join("\n", claims.Select(c => $"- {c.EntityName}: {c.Predicate} = {c.Object}"));
        var userPrompt = $"""
            ESTABLISHED FACTS:
            {factBlock}

            PROSE TO CHECK:
            {beatText}
            """;

        var raw = await llm.GenerateAsync(SystemPrompt, userPrompt, temperature: 0.0, maxTokens: 800, ct: ct);
        return ParseViolations(raw);
    }

    /// <summary>Throws (InvalidOperationException / JsonException) on an empty response, a
    /// response with no JSON array at all, or malformed JSON — see EnforceAsync's remarks.</summary>
    private static List<ContinuityViolation> ParseViolations(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Empty LLM response.");

        var trimmed = raw.Trim();
        var start = trimmed.IndexOf('[');
        var end = trimmed.LastIndexOf(']');
        if (start < 0 || end <= start)
            throw new InvalidOperationException($"No JSON array in response: {trimmed[..Math.Min(80, trimmed.Length)]}");

        var json = trimmed[start..(end + 1)];
        using var doc = System.Text.Json.JsonDocument.Parse(json); // throws JsonException on malformed input — let it propagate

        var result = new List<ContinuityViolation>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var entity = item.TryGetProperty("entity", out var e) ? e.GetString() ?? "" : "";
            var predicate = item.TryGetProperty("predicate", out var p) ? p.GetString() ?? "" : "";
            var fact = item.TryGetProperty("fact", out var f) ? f.GetString() ?? "" : "";
            var explanation = item.TryGetProperty("explanation", out var x) ? x.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(explanation)) continue;

            result.Add(new ContinuityViolation
            {
                EntityName = entity,
                Predicate = predicate,
                EstablishedFact = fact,
                Explanation = explanation,
            });
        }
        return result;
    }
}
