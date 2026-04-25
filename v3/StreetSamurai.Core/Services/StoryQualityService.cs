using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Multi-LLM quality evaluation and iterative improvement loop.
///
/// After a story is written, sends it to multiple LLMs (Claude, GPT-4, Gemini, etc.)
/// for structured critique against a rubric built from what actually makes neo-noir
/// literary fiction compelling. Scores are aggregated, saved to disk, and accumulated
/// across stories to identify systemic failure patterns.
///
/// The feedback loop:
/// 1. Story generated → 2. Quality evaluated → 3. Patterns extracted
/// 4. Patterns fed back into OutlineReviewService system prompt
/// 5. Next story benefits from previous failures
///
/// Rubric dimensions (each scored 1-10):
/// VOICE — Distinct, consistent neo-noir narration without purple prose
/// MORAL_COMPLEXITY — Genuine ethical dilemmas with no clean answers
/// PACING — Tension builds and releases organically, climax is earned
/// CHARACTER_AUTHENTICITY — Characters act from their psychology, not plot need
/// WORLD_SPECIFICITY — GLMZ mechanics visible and textured, not generic sci-fi
/// CLICHE_AVOIDANCE — Avoids the ten most common genre failure modes
/// DIALOGUE_QUALITY — Subtext present, voices distinct, dialogue earns its space
///
/// ── LEARNING MECHANISM ──
/// Low-scoring dimensions (< 5) with specific pattern text get saved to
/// engine/stories/quality_patterns.json as known failure patterns.
/// OutlineReviewService reads this file and injects the patterns into its prompt.
/// This creates a feedback loop without requiring code changes.
/// </summary>
public class StoryQualityService
{
    private readonly LLMVotingService llmVoting;
    private readonly IPathProvider paths;
    private readonly ILogger<StoryQualityService> log;

    private static readonly List<string> RubricDimensions =
    [
        "VOICE",
        "MORAL_COMPLEXITY",
        "PACING",
        "CHARACTER_AUTHENTICITY",
        "WORLD_SPECIFICITY",
        "CLICHE_AVOIDANCE",
        "DIALOGUE_QUALITY"
    ];

    // Threshold below which a pattern becomes a "known failure" for future stories
    private const int FailureThreshold = 5;

    public StoryQualityService(
        LLMVotingService llmVoting, IPathProvider paths,
        ILogger<StoryQualityService> log)
    {
        this.llmVoting = llmVoting;
        this.paths = paths;
        this.log = log;
    }

    /// <summary>
    /// Evaluate a completed story using all configured LLM providers.
    /// Returns a quality report with scores, feedback, and identified failure patterns.
    /// Saves the report to disk alongside the story.
    /// Optionally feeds failure patterns back into the quality_patterns.json accumulator.
    /// </summary>
    public async Task<StoryQualityReport> EvaluateAsync(
        AutonomousStory story, bool updatePatternAccumulator = true,
        CancellationToken ct = default)
    {
        log.LogInformation("StoryQuality evaluation starting: projectId={ProjectId}, title={Title}, textLen={Len}",
            story.ProjectId, story.Title, story.FullText.Length);

        var activeProviders = llmVoting.GetActiveProviderIds();
        if (activeProviders.Count == 0)
        {
            log.LogWarning("No LLM providers configured — skipping quality evaluation");
            return new StoryQualityReport
            {
                ProjectId = story.ProjectId,
                Title = story.Title,
                Error = "No LLM providers configured"
            };
        }

        // Truncate story text if very long — LLMs have context limits
        var storyText = story.FullText.Length > 12000
            ? story.FullText[..12000] + "\n\n[...story truncated for evaluation...]"
            : story.FullText;

        var outlineSummary = BuildOutlineSummary(story.Outline);

        var context = $"""
            STORY TITLE: {story.Title}
            PROTAGONIST: {story.Protagonist}
            LOCATION: {story.Location}
            BEAT COUNT: {story.Beats.Count}

            OUTLINE SUMMARY:
            {outlineSummary}

            FULL STORY TEXT:
            {storyText}
            """;

        var request = new ScoredVoteRequest
        {
            Question            = "Evaluate this neo-noir short story.",
            Context             = context,
            Dimensions          = RubricDimensions,
            FailureThreshold    = FailureThreshold,
            EvaluatorContext    = BuildEvaluatorContext(),
            SynthesizeNarrative = true,
            MaxTokens           = 2048,
        };

        ScoredVotingResult scored;
        try
        {
            scored = await llmVoting.ScoreAsync(request, ct);
            log.LogInformation("Quality evaluation: {Count} voters responded, overall={Overall:F1}/10",
                scored.SuccessfulVoters, scored.AggregateScores.GetValueOrDefault("OVERALL"));
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Quality evaluation failed");
            return new StoryQualityReport
            {
                ProjectId = story.ProjectId,
                Title = story.Title,
                Error = ex.Message
            };
        }

        // Map LLMVoting result → StoryQualityReport
        var individualReports = scored.IndividualVotes
            .Where(v => !v.IsError)
            .Select(v => new LlmQualityVote
            {
                ProviderName        = v.VoterName,
                Scores              = v.Scores,
                OverallScore        = v.Confidence,
                Strengths           = v.Flags.Where(f => scored.ConsensusStrengths.Contains(f)).ToList(),
                Failures            = scored.ConsensusFailures.Where(f => v.Flags.Contains(f)).ToList(),
                ClichesFound        = [],
                BestMoment          = v.BestMoment,
                WorstMoment         = v.WorstMoment,
                ImprovementDirective = v.Flags.LastOrDefault() ?? "",
            }).ToList();

        var report = new StoryQualityReport
        {
            ProjectId            = story.ProjectId,
            Title                = story.Title,
            EvaluatedAt          = DateTime.UtcNow,
            AggregateScores      = scored.AggregateScores.ToDictionary(kv => kv.Key, kv => kv.Value),
            WeakestDimension     = scored.WeakestDimension,
            ImprovementDirectives = scored.ImprovementDirectives,
            ConsensusStrengths   = scored.ConsensusStrengths,
            AllFailures          = scored.ConsensusFailures,
            AllClichesFound      = [],
            IndividualVotes      = individualReports,
        };

        // Save to disk
        Save(story.ProjectId, report);

        // Feed failures back into the pattern accumulator
        if (updatePatternAccumulator)
            UpdatePatternAccumulator(report);

        log.LogInformation("Quality evaluation complete: overall={Overall}/10, voice={Voice}, moral={Moral}, pacing={Pacing}",
            report.AggregateScores.GetValueOrDefault("OVERALL"),
            report.AggregateScores.GetValueOrDefault("VOICE"),
            report.AggregateScores.GetValueOrDefault("MORAL_COMPLEXITY"),
            report.AggregateScores.GetValueOrDefault("PACING"));

        return report;
    }

    /// <summary>
    /// Get the improvement directives derived from accumulated failure patterns.
    /// These can be injected into generation prompts to avoid known weaknesses.
    /// </summary>
    public string GetImprovementDirectives()
    {
        var path = Path.Combine(paths.StoriesDir, "quality_patterns.json");
        if (!File.Exists(path)) return "";

        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(path));

            var directives = new List<string>();

            if (doc.RootElement.TryGetProperty("failure_patterns", out var patterns))
            {
                var items = patterns.EnumerateArray()
                    .Select(p => p.GetString() ?? "")
                    .Where(s => s.Length > 0)
                    .Take(10)
                    .ToList();
                if (items.Count > 0)
                    directives.Add("AVOID THESE KNOWN WEAKNESSES:\n" + string.Join("\n", items.Select(i => $"  - {i}")));
            }

            if (doc.RootElement.TryGetProperty("dimension_scores", out var dimScores))
            {
                var lowDims = dimScores.EnumerateObject()
                    .Where(kv => kv.Value.TryGetDouble(out var score) && score < 6.0)
                    .Select(kv => kv.Name)
                    .ToList();

                if (lowDims.Contains("WORLD_SPECIFICITY"))
                    directives.Add("WORLD SPECIFICITY IS LOW: Name specific corponation brands, tier levels, locations, and QUANTA prices. Make GLMZ feel textured, not generic.");
                if (lowDims.Contains("MORAL_COMPLEXITY"))
                    directives.Add("MORAL COMPLEXITY IS LOW: Give the antagonist a coherent worldview. Make the protagonist's 'win' cost something real. Avoid clean resolutions.");
                if (lowDims.Contains("DIALOGUE_QUALITY"))
                    directives.Add("DIALOGUE IS WEAK: Each character must have a distinct voice. Dialogue should advance conflict OR reveal psychology — never just convey information.");
                if (lowDims.Contains("CHARACTER_AUTHENTICITY"))
                    directives.Add("CHARACTER AUTHENTICITY IS LOW: Characters must act from their established psychology, not from plot necessity. Check decision rules before writing choices.");
                if (lowDims.Contains("CLICHE_AVOIDANCE"))
                    directives.Add("CLICHÉS ARE PRESENT: Review the beat goals and avoid: villain explains plan, convenient rescue, unearned redemption, protagonist is 'special'.");
            }

            return string.Join("\n\n", directives);
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// List all quality reports sorted by overall score (best first).
    /// Used by the UI to browse story quality over time.
    /// </summary>
    public List<StoryQualityReport> ListReports()
    {
        var dir = paths.StoriesDir;
        if (!Directory.Exists(dir)) return [];

        return Directory.GetDirectories(dir)
            .Select(d => Path.Combine(d, "quality_report.json"))
            .Where(File.Exists)
            .Select(f =>
            {
                try { return JsonSerializer.Deserialize<StoryQualityReport>(File.ReadAllText(f)); }
                catch { return null; }
            })
            .Where(r => r != null)
            .OrderByDescending(r => r!.AggregateScores.GetValueOrDefault("OVERALL"))
            .ToList()!;
    }

    // ── Private ──

    /// <summary>
    /// Domain-specific evaluator context injected into LLMVoting's ScoredVoteRequest.
    /// Provides the rubric framing; LLMVoting enforces the JSON output schema.
    /// </summary>
    private static string BuildEvaluatorContext() => """
        You are a literary critic evaluating a neo-noir short story set in GLMZ
        (also called Meridian 88 by informed characters, or The Glooms by Gray Zone residents).
        The city is consumed by corporate sovereignty, aug-culture, and institutional collapse.
        The Gray Zone between corponation territories is a structural DMZ — no police,
        no government, designed to absorb border friction, which is why it has the most violence.
        There is no city police force (Meridian PD dissolved 2208). Arcturus Civil Security
        holds private contracts but serves corponations, not citizens.
        Currency is Φ (QUANTA). Outside the GLMZ is called The Gap — a thousand little towns
        the Pulse passes through at Mach 6 without stopping.

        Score the story on these 7 dimensions, each 1-10:

        VOICE (1-10): Is the narrative voice distinct, noir, and consistent?
          10 = unmistakable voice with controlled darkness and precision
           1 = generic prose, no atmosphere, could be any genre

        MORAL_COMPLEXITY (1-10): Are ethical dilemmas genuine with no clean answers?
          10 = every choice has a real moral cost, antagonist has coherent worldview
           1 = clear hero, clear villain, clear right answer

        PACING (1-10): Does tension build and release organically?
          10 = Act structure earned, climax inevitable in retrospect, no flat zones
           1 = events happen in random order with no tension curve

        CHARACTER_AUTHENTICITY (1-10): Do characters act from their psychology?
          10 = every decision traceable to who they are, surprises feel inevitable
           1 = characters make choices based on plot need, not who they are

        WORLD_SPECIFICITY (1-10): Is GLMZ textured and specific?
          10 = specific corponations, tier levels, Φ prices, place names, Gray Zone slang,
               The Gap references, thrumline, The Spine, Arcturus presence or absence noted
           1 = generic cyberpunk backdrop with no world-specific texture
          Flag as bad: invoking city police that don't exist; treating Gray Zone violence
          as surprising; calling the currency phi instead of Φ/QUANTA; adjacent corponation
          zones with no Gray Zone buffer; treating Behemoths as alive or sentient.

        CLICHE_AVOIDANCE (1-10): Does the story avoid the genre failure modes?
          10 = no clichés, every trope is subverted or avoided entirely
           1 = villain explains plan, chosen one narrative, unearned redemption

        DIALOGUE_QUALITY (1-10): Is dialogue earning its space?
          10 = each line advances conflict OR reveals psychology, voices distinct, subtext present;
               name choice (GLMZ vs Meridian 88 vs The Glooms) is used as characterization
           1 = dialogue conveys only information, all characters sound the same

        In flags_bad, include specific genre clichés found with quotes from the text.
        In flags_good, include specific strengths with quotes from the text.
        """;


    private void Save(string projectId, StoryQualityReport report)
    {
        try
        {
            var path = StoryFolderHelper.GetFilePath(paths.StoriesDir, projectId, "quality_report.json");
            File.WriteAllText(path, JsonSerializer.Serialize(report, JsonDefaults.Indented));
            log.LogDebug("Quality report saved to {Path}", path);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to save quality report for {ProjectId}", projectId);
        }
    }

    /// <summary>
    /// Update the global quality_patterns.json accumulator.
    /// Failure patterns from low-scoring dimensions are added to the list
    /// so future OutlineReview prompts know what to avoid.
    /// Uses a sliding window of the most recent 30 patterns.
    /// </summary>
    private void UpdatePatternAccumulator(StoryQualityReport report)
    {
        var path = Path.Combine(paths.StoriesDir, "quality_patterns.json");

        // Load existing patterns
        var existing = new QualityPatternAccumulator();
        if (File.Exists(path))
        {
            try { existing = JsonSerializer.Deserialize<QualityPatternAccumulator>(File.ReadAllText(path)) ?? new(); }
            catch { existing = new(); }
        }

        // Update rolling dimension scores
        foreach (var (dim, score) in report.AggregateScores)
        {
            if (!existing.DimensionScores.ContainsKey(dim))
                existing.DimensionScores[dim] = score;
            else
                existing.DimensionScores[dim] = Math.Round((existing.DimensionScores[dim] * 0.7) + (score * 0.3), 1);
        }

        // Add clichés as failure patterns
        foreach (var cliche in report.AllClichesFound)
        {
            if (!existing.FailurePatterns.Contains(cliche))
                existing.FailurePatterns.Add(cliche);
        }

        // Add improvement directives from very low scoring evaluations
        foreach (var directive in report.ImprovementDirectives)
        {
            if (!existing.FailurePatterns.Contains(directive))
                existing.FailurePatterns.Add(directive);
        }

        // Rolling window: keep only the most recent 30 patterns
        if (existing.FailurePatterns.Count > 30)
            existing.FailurePatterns = existing.FailurePatterns.TakeLast(30).ToList();

        existing.StoriesEvaluated++;
        existing.LastUpdated = DateTime.UtcNow;

        try
        {
            Directory.CreateDirectory(paths.StoriesDir);
            File.WriteAllText(path, JsonSerializer.Serialize(existing, JsonDefaults.Indented));
            log.LogDebug("Quality pattern accumulator updated: {Count} patterns, {Stories} stories",
                existing.FailurePatterns.Count, existing.StoriesEvaluated);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Failed to update quality pattern accumulator");
        }
    }

    private static string BuildOutlineSummary(StoryOutline? outline)
    {
        if (outline == null) return "No outline available";
        var beats = outline.Acts.SelectMany(a => a.Beats).ToList();
        var lines = new List<string>
        {
            $"Title: {outline.Title}",
            $"Theme: {outline.Theme}",
            $"Logline: {outline.Logline}",
        };
        foreach (var beat in beats)
            lines.Add($"  Beat {beat.BeatIndex + 1}: {beat.Title} — {beat.Goal}");
        return string.Join("\n", lines);
    }
}

public class StoryQualityReport
{
    [JsonPropertyName("project_id")] public string ProjectId { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("evaluated_at")] public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("aggregate_scores")] public Dictionary<string, double> AggregateScores { get; set; } = new();
    [JsonPropertyName("consensus_strengths")] public List<string> ConsensusStrengths { get; set; } = [];
    [JsonPropertyName("all_failures")] public List<string> AllFailures { get; set; } = [];
    [JsonPropertyName("all_cliches_found")] public List<string> AllClichesFound { get; set; } = [];
    [JsonPropertyName("improvement_directives")] public List<string> ImprovementDirectives { get; set; } = [];
    [JsonPropertyName("weakest_dimension")] public string WeakestDimension { get; set; } = "";
    [JsonPropertyName("individual_votes")] public List<LlmQualityVote> IndividualVotes { get; set; } = [];
    [JsonPropertyName("error")] public string? Error { get; set; }
}

public class LlmQualityVote
{
    [JsonPropertyName("provider")] public string ProviderName { get; set; } = "";
    [JsonPropertyName("scores")] public Dictionary<string, int> Scores { get; set; } = new();
    [JsonPropertyName("overall_score")] public int OverallScore { get; set; }
    [JsonPropertyName("strengths")] public List<string> Strengths { get; set; } = [];
    [JsonPropertyName("failures")] public List<string> Failures { get; set; } = [];
    [JsonPropertyName("cliches_found")] public List<string> ClichesFound { get; set; } = [];
    [JsonPropertyName("best_moment")] public string BestMoment { get; set; } = "";
    [JsonPropertyName("worst_moment")] public string WorstMoment { get; set; } = "";
    [JsonPropertyName("improvement_directive")] public string ImprovementDirective { get; set; } = "";
}

/// <summary>
/// Persisted accumulator for quality patterns across all evaluated stories.
/// Lives at engine/stories/quality_patterns.json.
/// </summary>
public class QualityPatternAccumulator
{
    [JsonPropertyName("stories_evaluated")] public int StoriesEvaluated { get; set; }
    [JsonPropertyName("last_updated")] public DateTime LastUpdated { get; set; }
    [JsonPropertyName("dimension_scores")] public Dictionary<string, double> DimensionScores { get; set; } = new();
    [JsonPropertyName("failure_patterns")] public List<string> FailurePatterns { get; set; } = [];
}
