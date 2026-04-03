using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models.Graph;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Validates generated text against the world graph DURING generation.
/// This is a fast, targeted check — not the full Sync analysis.
/// It catches hard contradictions: wrong pronouns, dead characters acting,
/// wrong affiliations, impossible locations. Returns a list of issues
/// the generation pipeline can use to either flag or auto-correct.
/// </summary>
public class CanonValidationService
{
    private readonly WorldGraphService _graph;
    private readonly ILlmService _llm;

    public CanonValidationService(WorldGraphService graph, ILlmService llm)
    {
        _graph = graph;
        _llm = llm;
    }

    /// <summary>
    /// Quick structural validation — checks generated text against graph facts
    /// without calling the LLM. Catches pronoun mismatches, dead characters
    /// being active, etc. Returns a list of issues found.
    /// </summary>
    public List<CanonIssue> ValidateQuick(string generatedText)
    {
        _graph.EnsureLoaded();
        var issues = new List<CanonIssue>();
        var textLower = generatedText.ToLowerInvariant();

        foreach (var node in _graph.AllNodes())
        {
            if (node.NodeType != EntityTypes.Character) continue;
            if (!textLower.Contains(node.Name.ToLowerInvariant())) continue;

            // Check pronouns
            if (node.Properties.TryGetValue("pronouns", out var pronouns) && pronouns.Length > 0)
            {
                var wrongPronouns = GetWrongPronouns(pronouns);
                foreach (var wrong in wrongPronouns)
                {
                    // Look for "CharacterName ... wrong_pronoun" within ~200 chars
                    var nameIdx = textLower.IndexOf(node.Name.ToLowerInvariant());
                    while (nameIdx >= 0)
                    {
                        var window = textLower.Substring(nameIdx, Math.Min(200, textLower.Length - nameIdx));
                        if (window.Contains($" {wrong} ") || window.Contains($" {wrong}.") || window.Contains($" {wrong},"))
                        {
                            issues.Add(new CanonIssue
                            {
                                Category = "PRONOUN",
                                EntityName = node.Name,
                                Description = $"{node.Name} uses {pronouns} but text uses '{wrong}'",
                                Severity = IssueSeverity.Hard,
                            });
                            break;
                        }
                        nameIdx = textLower.IndexOf(node.Name.ToLowerInvariant(), nameIdx + 1);
                    }
                }
            }

            // Check if dead character is doing things
            if (node.Properties.TryGetValue("status", out var status) &&
                status.Equals("dead", StringComparison.OrdinalIgnoreCase))
            {
                // Look for active verbs near the character's name
                var nameIdx = textLower.IndexOf(node.Name.ToLowerInvariant());
                if (nameIdx >= 0)
                {
                    var window = generatedText.Substring(nameIdx, Math.Min(100, generatedText.Length - nameIdx));
                    var activeVerbs = new[] { " said", " walked", " ran", " grabbed", " looked", " smiled", " laughed", " nodded", " turned", " spoke" };
                    foreach (var verb in activeVerbs)
                    {
                        if (window.Contains(verb, StringComparison.OrdinalIgnoreCase))
                        {
                            issues.Add(new CanonIssue
                            {
                                Category = "CANON",
                                EntityName = node.Name,
                                Description = $"{node.Name} is dead but appears to be acting in the scene",
                                Severity = IssueSeverity.Hard,
                            });
                            break;
                        }
                    }
                }
            }
        }

        return issues;
    }

    /// <summary>
    /// Deep validation — uses the LLM to check generated text against graph context.
    /// More thorough but costs an API call. Use sparingly (e.g. after full scene generation).
    /// </summary>
    public async Task<List<CanonIssue>> ValidateDeepAsync(string generatedText, string worldContext, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(generatedText) || string.IsNullOrWhiteSpace(worldContext))
            return [];

        var system = $"""
            You are a canon validator for a cyberpunk fiction world. Check the GENERATED TEXT
            against the WORLD CONTEXT (canonical truth). Find ONLY hard contradictions — facts
            that directly conflict with established canon.

            WORLD CONTEXT (this is TRUE):
            {worldContext}

            Report each issue as a JSON array. If no issues, return empty array [].
            Each object has: "category" (CANON, PRONOUN, or TIMELINE), "entity" (name),
            "description" (what's wrong), "severity" (hard or soft).
            Return ONLY the JSON array, nothing else.
            """;

        var response = await _llm.GenerateAsync(system, generatedText, 0.1, 1024, ct: ct);

        try
        {
            var json = response.Trim();
            if (json.StartsWith("```")) json = json[(json.IndexOf('\n') + 1)..];
            if (json.EndsWith("```")) json = json[..^3];
            json = json.Trim();

            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<RawCanonIssue>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parsed?.Select(r => new CanonIssue
            {
                Category = r.Category ?? "CANON",
                EntityName = r.Entity ?? "",
                Description = r.Description ?? "",
                Severity = r.Severity?.ToLowerInvariant() == "hard" ? IssueSeverity.Hard : IssueSeverity.Soft,
            }).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static List<string> GetWrongPronouns(string correctPronouns)
    {
        var correct = correctPronouns.ToLowerInvariant();
        var wrong = new List<string>();

        if (correct.Contains("she"))
        {
            wrong.AddRange(["he", "him", "his", "himself"]);
        }
        else if (correct.Contains("he"))
        {
            wrong.AddRange(["she", "her", "hers", "herself"]);
        }
        else if (correct.Contains("they"))
        {
            wrong.AddRange(["he", "him", "his", "himself", "she", "her", "hers", "herself"]);
        }

        return wrong;
    }

    private record RawCanonIssue
    {
        public string? Category { get; init; }
        public string? Entity { get; init; }
        public string? Description { get; init; }
        public string? Severity { get; init; }
    }
}

public record CanonIssue
{
    public string Category { get; init; } = "";
    public string EntityName { get; init; } = "";
    public string Description { get; init; } = "";
    public IssueSeverity Severity { get; init; } = IssueSeverity.Soft;
}

public enum IssueSeverity
{
    Soft,
    Hard,
}
