using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

public class BehaviorViolation
{
    public string CharacterName { get; set; } = "";
    public string RuleBucket { get; set; } = "";
    public string RuleText { get; set; } = "";
    public string Explanation { get; set; } = "";
}

/// <summary>
/// LLM-based post-generation check. Loads a character's behavioral rules
/// (CharacterBehavioralRules) and asks the LLM whether the beat prose violates any of them.
/// One LLM call per character in the beat. Call after generation, not inline.
/// </summary>
public class BehavioralInvariantEnforcer
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILlmService llm;

    private const string SystemPrompt = """
        You are a continuity editor checking character behavioral consistency.
        Given a character's behavioral rules and a prose passage, identify any line where
        the character acts in clear contradiction to their established rules.

        Respond with a JSON array of violations, or an empty array [] if none exist.
        Each violation: {"bucket":"...", "rule":"...", "explanation":"..."}
        Be strict: flag only clear, specific contradictions — not interpretive differences.
        Return only the JSON array, no other text.
        """;

    public BehavioralInvariantEnforcer(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILlmService llm)
    {
        this.dbFactory = dbFactory;
        this.llm = llm;
    }

    /// <summary>
    /// Checks <paramref name="beatText"/> against the behavioral rules for
    /// <paramref name="characterId"/>. Returns violations found by the LLM.
    /// Returns empty list when no behavioral rules are loaded for the character (a real,
    /// distinguishable "nothing to check" case, not an evaluation failure).
    /// Throws if the LLM call itself fails or returns an unparseable response — deliberately,
    /// so callers cannot mistake "could not evaluate" for "evaluated, no violations found."
    /// 2026-08-09: previously swallowed every failure into an empty list, which
    /// BookHealthService.BehaviorCheckAsync's purge-then-refile cycle could not distinguish
    /// from a real clean pass — an LLM outage silently deleted real prior BEHAVIOR findings and
    /// never re-added them (the same fail-open bug class fixed 6 other times this session).
    /// Callers that need best-effort semantics (e.g. PostBeatValidationService's fire-and-forget
    /// beat-save hook) already wrap this call in their own try/catch.
    /// </summary>
    public async Task<List<BehaviorViolation>> EnforceAsync(
        string beatText,
        Guid characterId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(beatText)) return [];

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var character = await db.Entities.AsNoTracking()
            .Where(e => e.Id == characterId)
            .Select(e => new { e.Id, e.Name })
            .FirstOrDefaultAsync(ct);

        if (character == null) return [];

        var rules = await db.CharacterBehavioralRules.AsNoTracking()
            .Where(r => r.CharacterId == characterId)
            .OrderBy(r => r.Bucket).ThenBy(r => r.Position)
            .ToListAsync(ct);

        if (rules.Count == 0) return [];

        var ruleBlock = string.Join("\n",
            rules.GroupBy(r => r.Bucket)
                 .Select(g => $"[{g.Key}]\n" + string.Join("\n", g.Select(r => $"- {r.Rule}"))));

        var userPrompt = $"""
            Character: {character.Name}

            Behavioral rules:
            {ruleBlock}

            Prose to check:
            {beatText}
            """;

        var raw = await llm.GenerateAsync(SystemPrompt, userPrompt, temperature: 0.0, maxTokens: 800, ct: ct);
        return ParseViolations(raw, character.Name);
    }

    /// <summary>Throws (InvalidOperationException / JsonException) on an empty response, a
    /// response with no JSON array at all, or malformed JSON — these are evaluation failures,
    /// not "the model found zero violations," and must not be silently swallowed into an empty
    /// list (see EnforceAsync's remarks: this was previously a second, deeper instance of the
    /// same fail-open bug that method itself had).</summary>
    private static List<BehaviorViolation> ParseViolations(string raw, string characterName)
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

        var result = new List<BehaviorViolation>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var bucket = item.TryGetProperty("bucket", out var b) ? b.GetString() ?? "" : "";
            var rule = item.TryGetProperty("rule", out var r) ? r.GetString() ?? "" : "";
            var explanation = item.TryGetProperty("explanation", out var e) ? e.GetString() ?? "" : "";

            if (string.IsNullOrWhiteSpace(rule) && string.IsNullOrWhiteSpace(explanation)) continue;

            result.Add(new BehaviorViolation
            {
                CharacterName = characterName,
                RuleBucket = bucket,
                RuleText = rule,
                Explanation = explanation,
            });
        }
        return result;
    }
}
