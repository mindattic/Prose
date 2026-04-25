using System.Text.Json;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Core.Services.Operator.Tools;

/// <summary>
/// Runs the same multi-LLM rubric the autonomous pipeline uses (VOICE,
/// MORAL_COMPLEXITY, PACING, CHARACTER_AUTHENTICITY, WORLD_SPECIFICITY,
/// CLICHE_AVOIDANCE, DIALOGUE_QUALITY) on a draft. Slow — calls multiple
/// providers — but gives a grounded "is this good?" answer when the writer
/// wants a second opinion.
/// </summary>
public class ScoreStoryQualityTool : IWriterTool
{
    private readonly StoryQualityService quality;
    public ScoreStoryQualityTool(StoryQualityService quality) { this.quality = quality; }

    public string Name => "score_story_quality";

    public string Description =>
        "Evaluate a draft against the multi-LLM quality rubric. Returns aggregate " +
        "scores (0–100) on voice, moral complexity, pacing, character authenticity, " +
        "world specificity, cliche avoidance, dialogue quality — plus consensus " +
        "strengths, failures, and improvement directives. Slow (multi-provider " +
        "vote). Use only when the writer asks for an evaluation; don't auto-call " +
        "after every draft.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "text": {
          "type": "string",
          "description": "Story prose to evaluate. Defaults to the active document if omitted."
        },
        "title": { "type": "string", "description": "Title for the report. Defaults to the active project title." }
      }
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, OperatorContext ctx, CancellationToken ct)
    {
        var text = args.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(text)) text = ctx.StoryText;
        var title = args.TryGetProperty("title", out var tt) ? tt.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(title)) title = ctx.StoryTitle ?? "(untitled)";

        if (string.IsNullOrWhiteSpace(text))
            return JsonSerializer.Serialize(new { error = "No text to evaluate (active story is empty and no text passed)." });

        var story = new AutonomousStory
        {
            ProjectId = ctx.ProjectId,
            Title = title,
            FullText = text,
        };
        var report = await quality.EvaluateAsync(story, updatePatternAccumulator: false, ct);
        return JsonSerializer.Serialize(new
        {
            error = report.Error,
            aggregate_scores = report.AggregateScores,
            weakest_dimension = report.WeakestDimension,
            consensus_strengths = report.ConsensusStrengths,
            failures = report.AllFailures,
            cliches_found = report.AllClichesFound,
            improvement_directives = report.ImprovementDirectives,
        });
    }
}
