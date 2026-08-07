using System.Text.Json;
using Prose.Core.Models;
using Prose.Core.Services;

namespace Prose.Core.Services.Operator.Tools;

/// <summary>
/// Beat-by-beat refinement notes: identifies impactful moments, cluttered
/// prose, underdeveloped tensions, context gaps, and pacing mismatches.
/// Returns suggestions the writer can accept or skip — the operator surfaces
/// them but doesn't apply without consent.
/// </summary>
public class RefineStoryTool : IWriterTool
{
    private readonly StoryRefinementService refine;
    public RefineStoryTool(StoryRefinementService refine) { this.refine = refine; }

    public string Name => "refine_story";

    public string Description =>
        "Run beat-by-beat refinement analysis on the active story. Returns notes " +
        "tagged Impactful / Cluttered / Underdeveloped / ContextGap / PacingMismatch, " +
        "each with a quote, rationale, and suggested edit. Use when the writer asks " +
        "for editorial feedback. Slow — multi-LLM vote. Does NOT modify the document; " +
        "report findings and let the writer decide.";

    public string ParametersJsonSchema => """
    {
      "type": "object",
      "properties": {
        "load_existing": {
          "type": "boolean",
          "default": false,
          "description": "If true, load the most recent saved report instead of regenerating."
        }
      }
    }
    """;

    public async Task<string> InvokeAsync(JsonElement args, OperatorContext ctx, CancellationToken ct)
    {
        var loadExisting = args.TryGetProperty("load_existing", out var le) && le.ValueKind == JsonValueKind.True;
        RefinementReport? report;

        if (loadExisting)
        {
            report = refine.LoadReport(ctx.ProjectId);
            if (report == null)
                return JsonSerializer.Serialize(new { error = "No saved refinement report for this project." });
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ctx.StoryText))
                return JsonSerializer.Serialize(new { error = "Active story is empty — nothing to refine." });

            var story = new AutonomousStory
            {
                ProjectId = ctx.ProjectId,
                Title = ctx.StoryTitle ?? "(untitled)",
                FullText = ctx.StoryText,
            };
            report = await refine.AnalyzeAsync(story, ct);
        }

        return JsonSerializer.Serialize(new
        {
            project_id = report.ProjectId,
            beats_analyzed = report.BeatsAnalyzed,
            error = report.Error,
            note_count = report.Notes.Count,
            notes = report.Notes.Select(n => new
            {
                kind = n.Kind.ToString(),
                beat_index = n.BeatIndex,
                quote = n.Quote,
                rationale = n.Rationale,
                suggestion = n.Suggestion,
                canon_fact = n.CanonFact,
            }),
        });
    }
}
