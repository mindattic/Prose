using Microsoft.Extensions.Logging;
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
public class ValidationService
{
    private readonly WorldGraphService graph;
    private readonly ILlmService llm;
    private readonly MultiLlmService multiLlm;
    private readonly ILogger<ValidationService> log;

    public ValidationService(WorldGraphService graph, ILlmService llm, MultiLlmService multiLlm, ILogger<ValidationService> log)
    {
        this.graph = graph;
        this.llm = llm;
        this.multiLlm = multiLlm;
        this.log = log;
    }

    /// <summary>
    /// Quick structural validation — checks generated text against graph facts
    /// without calling the LLM. Catches pronoun mismatches, dead characters
    /// being active, etc. Returns a list of issues found.
    /// </summary>
    public List<CanonIssue> ValidateQuick(string generatedText)
    {
        graph.EnsureLoaded();
        var issues = new List<CanonIssue>();
        var textLower = generatedText.ToLowerInvariant();

        foreach (var node in graph.AllNodes())
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
    /// Deep validation — fans out to ALL configured LLM providers in parallel.
    /// Each provider independently checks for canon contradictions. Results are
    /// merged and deduplicated so different models catch different issues.
    /// </summary>
    /// <summary>Fired during validation with progress updates (providerName, completedCount, totalCount).</summary>
    public event Action<string, int, int>? OnValidationProgress;

    public async Task<List<CanonIssue>> ValidateDeepAsync(string generatedText, string worldContext, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(generatedText) || string.IsNullOrWhiteSpace(worldContext))
            return [];

        var systemPrompt = $"""
            You are a canon validator for a near-future fiction world. Check the GENERATED TEXT
            against the WORLD CONTEXT (canonical truth). Find contradictions — facts
            that conflict with established canon.

            WORLD CONTEXT (this is TRUE):
            {worldContext}

            Find the SINGLE MOST IMPORTANT contradiction — the one that most damages
            canon integrity. Return exactly ONE issue (or empty array if none found).

            Return a JSON array with 0 or 1 objects:
            - "category": "CANON", "PRONOUN", "TIMELINE", or "RELATIONSHIP"
            - "entity": the entity name involved
            - "severity": "hard" or "soft"
            - "description": what's wrong
            - "story_text": the exact phrase/sentence from the generated text that contradicts canon
            - "canon_truth": what the canon actually says
            - "alternatives": array of 3 rewritten versions of the story_text that fix the contradiction while preserving narrative flow

            Return ONLY the JSON array ([] if clean, or [single object] if found). Nothing else.
            """;

        // Fan out to all configured providers in parallel
        var providers = multiLlm.GetConfiguredProviders();
        var allIssues = new List<CanonIssue>();

        if (providers.Count > 0)
        {
            var total = providers.Count;
            var completed = 0;
            log.LogInformation("Validation fanning out to {Count} providers: {Providers}",
                total, string.Join(", ", providers.Select(p => p.Name)));

            OnValidationProgress?.Invoke($"0/{total} providers", 0, total);

            // Fan out but track individual completions for progress
            var tasks = providers.Select(async provider =>
            {
                try
                {
                    OnValidationProgress?.Invoke($"Waiting on {provider.Name}...", completed, total);
                    var result = await multiLlm.CallProviderAsync(provider.Id, systemPrompt, generatedText, ct);
                    var issues = ParseValidationResponse(result, provider.Name);
                    Interlocked.Increment(ref completed);
                    var status = issues.Count > 0 ? $"found {issues.Count} issue(s)" : "clean";
                    OnValidationProgress?.Invoke($"{provider.Name}: {status} ({completed}/{total})", completed, total);
                    return issues;
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref completed);
                    OnValidationProgress?.Invoke($"{provider.Name}: error ({completed}/{total})", completed, total);
                    log.LogWarning(ex, "Validation request failed for provider {Provider}", provider.Name);
                    return new List<CanonIssue>();
                }
            });

            var results = await Task.WhenAll(tasks);
            allIssues.AddRange(results.SelectMany(r => r));
        }
        else
        {
            // Fallback: use the default LLM
            OnValidationProgress?.Invoke("Validating with default LLM...", 0, 1);
            var response = await llm.GenerateAsync(systemPrompt, generatedText, 0.1, 2048, ct: ct);
            allIssues.AddRange(ParseValidationResponse(response, "default"));
            OnValidationProgress?.Invoke("Done", 1, 1);
        }

        // Deduplicate by story_text similarity, take the single best issue
        var deduplicated = allIssues
            .GroupBy(i => i.StoryText.Trim().ToLowerInvariant())
            .Select(g =>
            {
                var best = g.OrderByDescending(i => i.Severity).ThenByDescending(i => i.StoryFixes.Count).First();
                var allAlts = g.SelectMany(i => i.StoryFixes).Distinct().ToList();
                return best with { StoryFixes = allAlts };
            })
            .OrderByDescending(i => i.Severity)
            .Take(1) // One issue at a time
            .ToList();

        log.LogInformation("Validation found {Total} issues from {Providers} providers, {Deduped} after dedup",
            allIssues.Count, providers.Count > 0 ? providers.Count : 1, deduplicated.Count);

        // Generate canon fixes + vote on both directions for each issue
        if (deduplicated.Count > 0)
        {
            for (int i = 0; i < deduplicated.Count; i++)
            {
                var issue = deduplicated[i];

                // Generate canon-side fixes (what would canon need to say to match the story?)
                OnValidationProgress?.Invoke($"Generating canon fixes {i + 1}/{deduplicated.Count}...", i, deduplicated.Count);
                issue.CanonFixes = await GenerateCanonFixesAsync(issue, ct);

                // Vote on story fixes
                if (issue.StoryFixes.Count > 0)
                {
                    OnValidationProgress?.Invoke($"Voting on story fixes {i + 1}/{deduplicated.Count}...", i, deduplicated.Count);
                    issue.StoryFixScores = await VoteOnFixesAsync(issue.StoryText, issue.CanonTruth, issue.StoryFixes, "story rewrite", ct);
                }

                // Vote on canon fixes
                if (issue.CanonFixes.Count > 0)
                {
                    OnValidationProgress?.Invoke($"Voting on canon fixes {i + 1}/{deduplicated.Count}...", i, deduplicated.Count);
                    issue.CanonFixScores = await VoteOnFixesAsync(issue.StoryText, issue.CanonTruth, issue.CanonFixes, "canon update", ct);
                }
            }
            OnValidationProgress?.Invoke("Voting complete", deduplicated.Count, deduplicated.Count);
        }

        return deduplicated;
    }

    private List<CanonIssue> ParseValidationResponse(string response, string providerName)
    {
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
                Description = $"[{providerName}] {r.Description ?? ""}",
                Severity = r.Severity?.ToLowerInvariant() == "hard" ? IssueSeverity.Hard : IssueSeverity.Soft,
                StoryText = r.StoryText ?? "",
                CanonTruth = r.CanonTruth ?? "",
                Alternatives = r.Alternatives ?? [],
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Validation parse failed for provider {Provider}", providerName);
            return [];
        }
    }

    /// <summary>
    /// Generate additional alternatives for a specific contradiction.
    /// </summary>
    public async Task<List<string>> GenerateAlternativesAsync(string storyText, string canonTruth, string fullContext, CancellationToken ct = default)
    {
        var system = $"""
            You are rewriting a sentence from a neo-noir story to fix a canon contradiction.
            The canon truth is: {canonTruth}
            The full story context: {fullContext}

            Generate 3 alternative rewrites of the problematic text that:
            1. Fix the contradiction so it aligns with canon
            2. Preserve the narrative voice and flow
            3. Each offers a different approach (subtle fix, expanded, creative reinterpretation)

            Return ONLY a JSON array of 3 strings. Nothing else.
            """;

        var response = await llm.GenerateAsync(system, $"Rewrite this: {storyText}", 0.7, 512, ct: ct);
        try
        {
            var json = response.Trim();
            if (json.StartsWith("```")) json = json[(json.IndexOf('\n') + 1)..];
            if (json.EndsWith("```")) json = json[..^3];
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json.Trim()) ?? [];
        }
        catch (Exception ex) { log.LogWarning(ex, "Validation extraction failed"); return []; }
    }

    /// <summary>
    /// Generate canon-side fixes — what would the canon entity need to say to match the story?
    /// </summary>
    public async Task<List<string>> GenerateCanonFixesAsync(CanonIssue issue, CancellationToken ct = default)
    {
        var system = $"""
            A neo-noir story says: {issue.StoryText}
            But canon says: {issue.CanonTruth}
            Entity involved: {issue.EntityName}

            The author wants to keep the story as-is and update the canon instead.
            Generate 3 alternative canon updates — how should the entity's canon entry
            be rewritten so the story text is no longer a contradiction?

            Each should:
            1. Make the story text canonically correct
            2. Be internally consistent with the world
            3. Offer a different approach (minor tweak, expanded lore, creative reinterpretation)

            Return ONLY a JSON array of 3 strings. Nothing else.
            """;

        var response = await llm.GenerateAsync(system, "Generate canon fixes.", 0.7, 512, ct: ct);
        try
        {
            var json = response.Trim();
            if (json.StartsWith("```")) json = json[(json.IndexOf('\n') + 1)..];
            if (json.EndsWith("```")) json = json[..^3];
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json.Trim()) ?? [];
        }
        catch { return []; }
    }

    /// <summary>
    /// Have all configured LLMs vote on a set of fixes. Returns scores 0-100 averaged across voters.
    /// Returns empty scores if no votes come back (providers erroring/slow).
    /// </summary>
    public async Task<List<int>> VoteOnFixesAsync(string storyText, string canonTruth, List<string> fixes, string fixType, CancellationToken ct = default)
    {
        if (fixes.Count == 0) return [];
        var empty = Enumerable.Repeat(0, fixes.Count).ToList();

        var numberedFixes = string.Join("\n", fixes.Select((f, i) => $"  {i + 1}. {f}"));

        var system = $"""
            You are judging {fixType} options for a canon contradiction in a neo-noir story.

            STORY TEXT: {storyText}
            CANON TRUTH: {canonTruth}

            Score each {fixType} from 0 to 100 based on:
            - Does it resolve the contradiction?
            - Is it internally consistent with the world?
            - Does it preserve narrative quality and voice?

            OPTIONS:
            {numberedFixes}

            Return ONLY a JSON array of integers (scores 0-100), one per option.
            Example for 3 options: [85, 42, 71]
            """;

        try
        {
            var providers = multiLlm.GetConfiguredProviders();
            var allVotes = new List<List<int>>();

            if (providers.Count > 0)
            {
                // Each provider gets 30s, collect whatever comes back
                var tasks = providers.Select(async provider =>
                {
                    try
                    {
                        using var perProviderCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        perProviderCts.CancelAfter(TimeSpan.FromSeconds(30));
                        var response = await multiLlm.CallProviderAsync(provider.Id, system, "Score the options.", perProviderCts.Token);
                        return ParseScores(response, fixes.Count);
                    }
                    catch { return null; }
                });

                var results = await Task.WhenAll(tasks);
                allVotes.AddRange(results.Where(r => r != null)!);
            }
            else
            {
                using var fallbackCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                fallbackCts.CancelAfter(TimeSpan.FromSeconds(30));
                var response = await llm.GenerateAsync(system, "Score the options.", 0.1, 256, ct: fallbackCts.Token);
                var scores = ParseScores(response, fixes.Count);
                if (scores != null) allVotes.Add(scores);
            }

            if (allVotes.Count == 0)
            {
                log.LogWarning("Voting returned no valid scores — skipping");
                return empty;
            }

            var averaged = new List<int>();
            for (int i = 0; i < fixes.Count; i++)
            {
                var votesForFix = allVotes.Where(v => i < v.Count).Select(v => v[i]).ToList();
                averaged.Add(votesForFix.Count > 0 ? (int)votesForFix.Average() : 0);
            }

            return averaged;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Voting failed entirely — returning no scores");
            return empty;
        }
    }

    private static List<int>? ParseScores(string response, int expectedCount)
    {
        try
        {
            var json = response.Trim();
            if (json.StartsWith("```")) json = json[(json.IndexOf('\n') + 1)..];
            if (json.EndsWith("```")) json = json[..^3];
            var scores = System.Text.Json.JsonSerializer.Deserialize<List<int>>(json.Trim());
            if (scores != null && scores.Count == expectedCount) return scores;
            return null;
        }
        catch { return null; }
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
        [System.Text.Json.Serialization.JsonPropertyName("story_text")]
        public string? StoryText { get; init; }
        [System.Text.Json.Serialization.JsonPropertyName("canon_truth")]
        public string? CanonTruth { get; init; }
        public List<string>? Alternatives { get; init; }
    }
}

public record CanonIssue
{
    public string Category { get; init; } = "";
    public string EntityName { get; init; } = "";
    public string Description { get; init; } = "";
    public IssueSeverity Severity { get; init; } = IssueSeverity.Soft;
    public string StoryText { get; init; } = "";
    public string CanonTruth { get; init; } = "";

    /// <summary>Story rewrites that fix the contradiction (sync story → canon). Voted 0-100.</summary>
    public List<string> StoryFixes { get; set; } = [];
    public List<int> StoryFixScores { get; set; } = [];

    /// <summary>Canon rewrites that update canon to match the story (sync canon → story). Voted 0-100.</summary>
    public List<string> CanonFixes { get; set; } = [];
    public List<int> CanonFixScores { get; set; } = [];

    // Legacy compat
    [System.Text.Json.Serialization.JsonPropertyName("alternatives")]
    public List<string> Alternatives { get => StoryFixes; set => StoryFixes = value; }
    [System.Text.Json.Serialization.JsonPropertyName("scores")]
    public List<int> Scores { get => StoryFixScores; set => StoryFixScores = value; }
}

public enum IssueSeverity
{
    Soft,
    Hard,
}
