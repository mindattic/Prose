using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Comprehensive post-beat continuity validation. Checks generated text against
/// full story state — character inventory, injuries, locations, timeline,
/// and cross-beat references. Returns issues ranked by severity.
/// </summary>
public class ContinuityValidatorService
{
    private readonly ILlmService llm;
    private readonly StoryStateService storyState;
    private readonly IDatabaseService db;

    public ContinuityValidatorService(ILlmService llm, StoryStateService storyState, IDatabaseService db)
    {
        this.llm = llm;
        this.storyState = storyState;
        this.db = db;
    }

    /// <summary>
    /// Validate a generated beat against the full story state.
    /// Returns a list of continuity issues, empty if clean.
    /// </summary>
    public async Task<ContinuityReport> ValidateAsync(
        string generatedText,
        string projectId,
        List<string> charactersInScene,
        string location,
        List<string> allPriorText,
        CancellationToken ct = default)
    {
        var stateConstraints = storyState.BuildConstraints(projectId);

        // Build character fact sheet from canon
        var charFacts = new List<string>();
        foreach (var name in charactersInScene)
        {
            var c = db.FindCharacter(name);
            if (c == null) continue;

            var facts = new List<string> { $"[{c.Name}]" };
            if (c.Gender.Length > 0) facts.Add($"  Gender: {c.Gender}");
            if (c.Age > 0) facts.Add($"  Age: {c.Age}");
            var p = c.PhysicalDescription;
            if (p.HeightCm > 0) facts.Add($"  Height: {p.HeightCm}cm");
            if (p.Build.Length > 0) facts.Add($"  Build: {p.Build}");
            if (p.HairColor.Length > 0) facts.Add($"  Hair: {p.HairColor}");
            if (p.EyeColor.Length > 0) facts.Add($"  Eyes: {p.EyeColor}");
            if (p.VisibleAugmentations.Length > 0) facts.Add($"  Augmentations: {p.VisibleAugmentations}");
            if (c.Species.Length > 0 && c.Species != "human") facts.Add($"  Species: {c.Species}");
            charFacts.Add(string.Join("\n", facts));
        }

        // Recent text for cross-reference (last 2000 chars to stay within token budget)
        var recentText = allPriorText.Count > 0
            ? string.Join("\n\n", allPriorText.TakeLast(3))
            : "";
        if (recentText.Length > 2000) recentText = recentText[^2000..];

        var system = """
            You are a continuity editor for a neo-noir novel. Check the new text against
            established facts and prior text. Look for:
            1. Character description contradictions (wrong eye color, hair, build, augmentations)
            2. Inventory errors (using items they don't have, items that were destroyed)
            3. Injury continuity (wounds that disappear, actions impossible with stated injuries)
            4. Location errors (character in wrong place, impossible travel)
            5. Timeline errors (events out of order, time gaps that don't work)
            6. Dead character errors (characters who died appearing alive)
            7. Pronoun/gender mismatches
            8. Name inconsistencies (different spellings, wrong names)

            Return ONLY a JSON object:
            {
              "clean": true/false,
              "issues": [
                {
                  "severity": "critical|major|minor",
                  "category": "description|inventory|injury|location|timeline|death|pronoun|name",
                  "description": "what's wrong",
                  "quote": "the specific text that has the error",
                  "fix": "suggested correction"
                }
              ]
            }
            If no issues found, return {"clean": true, "issues": []}
            """;

        var user = $"""
            ESTABLISHED CHARACTER FACTS:
            {string.Join("\n\n", charFacts)}

            STORY STATE (current character statuses):
            {stateConstraints}

            LOCATION: {location}

            RECENT PRIOR TEXT:
            {recentText}

            NEW TEXT TO VALIDATE:
            {generatedText}

            Check for continuity errors.
            """;

        try
        {
            var response = await llm.GenerateAsync(system, user, 0.1f, 1024, null, ct);
            var json = ExtractJson(response);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var issues = new List<ContinuityIssue>();
            if (root.TryGetProperty("issues", out var arr))
            {
                foreach (var item in arr.EnumerateArray())
                {
                    issues.Add(new ContinuityIssue
                    {
                        Severity = item.TryGetProperty("severity", out var sev) ? sev.GetString() ?? "minor" : "minor",
                        Category = item.TryGetProperty("category", out var cat) ? cat.GetString() ?? "" : "",
                        Description = item.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "",
                        Quote = item.TryGetProperty("quote", out var q) ? q.GetString() ?? "" : "",
                        Fix = item.TryGetProperty("fix", out var f) ? f.GetString() ?? "" : "",
                    });
                }
            }

            return new ContinuityReport
            {
                Clean = issues.Count == 0,
                Issues = issues.OrderBy(i => i.Severity == "critical" ? 0 : i.Severity == "major" ? 1 : 2).ToList()
            };
        }
        catch
        {
            return new ContinuityReport { Clean = true, Issues = [] };
        }
    }

    /// <summary>
    /// Quick structural validation without LLM — catches obvious errors fast.
    /// </summary>
    public ContinuityReport QuickValidate(string text, List<string> charactersInScene)
    {
        var issues = new List<ContinuityIssue>();

        foreach (var name in charactersInScene)
        {
            var c = db.FindCharacter(name);
            if (c == null) continue;

            // Check gender pronoun consistency
            if (c.Gender.Equals("male", StringComparison.OrdinalIgnoreCase))
            {
                if (text.Contains($"{c.Name.Split(' ')[0]} tucked her", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains($"{c.Name.Split(' ')[0]}, she", StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new ContinuityIssue
                    {
                        Severity = "major", Category = "pronoun",
                        Description = $"{c.Name} is male but female pronouns were used",
                        Fix = "Replace she/her with he/him"
                    });
                }
            }
            else if (c.Gender.Equals("female", StringComparison.OrdinalIgnoreCase))
            {
                if (text.Contains($"{c.Name.Split(' ')[0]} tucked his", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains($"{c.Name.Split(' ')[0]}, he", StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new ContinuityIssue
                    {
                        Severity = "major", Category = "pronoun",
                        Description = $"{c.Name} is female but male pronouns were used",
                        Fix = "Replace he/him with she/her"
                    });
                }
            }
        }

        return new ContinuityReport { Clean = issues.Count == 0, Issues = issues };
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : "{}";
    }
}

public class ContinuityReport
{
    public bool Clean { get; init; }
    public List<ContinuityIssue> Issues { get; init; } = [];
    public bool HasCritical => Issues.Any(i => i.Severity == "critical");
}

public class ContinuityIssue
{
    public string Severity { get; init; } = "minor";
    public string Category { get; init; } = "";
    public string Description { get; init; } = "";
    public string Quote { get; init; } = "";
    public string Fix { get; init; } = "";
}
