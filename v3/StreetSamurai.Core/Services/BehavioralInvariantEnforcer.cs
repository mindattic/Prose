using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

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
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
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
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILlmService llm)
    {
        this.dbFactory = dbFactory;
        this.llm = llm;
    }

    /// <summary>
    /// Checks <paramref name="beatText"/> against the behavioral rules for
    /// <paramref name="characterId"/>. Returns violations found by the LLM.
    /// Returns empty list when no behavioral rules are loaded for the character.
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

        List<BehaviorViolation> violations = [];
        try
        {
            var raw = await llm.GenerateAsync(SystemPrompt, userPrompt, temperature: 0.0, maxTokens: 800, ct: ct);
            violations = ParseViolations(raw, character.Name);
        }
        catch
        {
            // LLM unavailable or returned unparseable response — return empty rather than fail.
        }

        return violations;
    }

    private static List<BehaviorViolation> ParseViolations(string raw, string characterName)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];

        var result = new List<BehaviorViolation>();
        try
        {
            var trimmed = raw.Trim();
            // Find the JSON array bounds
            var start = trimmed.IndexOf('[');
            var end = trimmed.LastIndexOf(']');
            if (start < 0 || end <= start) return [];

            var json = trimmed[start..(end + 1)];
            using var doc = System.Text.Json.JsonDocument.Parse(json);

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
        }
        catch
        {
            // Malformed JSON from LLM — best effort, return whatever was parsed
        }

        return result;
    }
}
