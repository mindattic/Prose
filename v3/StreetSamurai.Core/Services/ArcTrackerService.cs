using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Tracks story arc progress at runtime — validates that generated prose
/// hits planned turning points, emotional arcs, and act transitions.
/// Flags drift when the story deviates from the outline.
/// </summary>
public class ArcTrackerService
{
    private readonly ILlmService llm;

    public ArcTrackerService(ILlmService llm) => this.llm = llm;

    /// <summary>
    /// Validate whether the generated beat text actually achieves the outline's intended goal.
    /// Returns an ArcValidation with pass/fail and suggestions.
    /// </summary>
    public async Task<ArcValidation> ValidateBeatAsync(
        string generatedText,
        OutlineBeat beat,
        StoryOutline outline,
        int beatIndex,
        CancellationToken ct = default)
    {
        var totalBeats = outline.Acts.SelectMany(a => a.Beats).Count();
        // FindIndex returns -1 when `beat` isn't reference-equal to any beat the
        // outline holds (e.g. a reloaded/cloned copy). Guard so we don't index
        // Acts[-1]; fall back to the caller-supplied beatIndex for position.
        var actIndex = outline.Acts.FindIndex(a => a.Beats.Contains(beat));
        var actNumber = actIndex >= 0 ? actIndex + 1 : 1;
        var positionInAct = actIndex >= 0 ? outline.Acts[actIndex].Beats.IndexOf(beat) + 1 : beatIndex + 1;

        // Find which character arcs should be progressing
        var relevantArcs = outline.CharacterArcs
            .Where(a => beat.CharactersPresent.Contains(a.Character, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var arcContext = relevantArcs.Count > 0
            ? string.Join("\n", relevantArcs.Select(a =>
                $"  {a.Character}: {a.StartState} → {a.EndState} (turning point: {a.TurningPoint})"))
            : "  No specific character arcs tracked in this beat.";

        // Seeds that should be planted in this beat
        var seedsToPlant = beat.Seeds.Count > 0
            ? string.Join(", ", beat.Seeds)
            : "none";

        // Payoffs that should land in this beat
        var payoffsToResolve = beat.Payoffs.Count > 0
            ? string.Join(", ", beat.Payoffs)
            : "none";

        var system = """
            You are a story structure analyst. Given a generated beat and its intended outline goals,
            evaluate whether the prose achieves what the outline planned. Be specific and actionable.
            Return ONLY a JSON object with these fields:
            {
              "achieved_goal": true/false,
              "goal_score": 1-10 (how well the beat goal was achieved),
              "seeds_planted": ["list of seeds that were successfully planted"],
              "seeds_missed": ["list of seeds that should have been planted but weren't"],
              "payoffs_resolved": ["list of payoffs that landed"],
              "payoffs_missed": ["list of payoffs that should have resolved but didn't"],
              "arc_progress": "description of character arc movement in this beat",
              "tension_actual": 1-10 (actual tension level of the prose),
              "drift_warning": "specific description of how the prose diverged from plan, or empty string",
              "suggestions": ["actionable suggestions to realign, or empty if on track"]
            }
            """;

        var user = $"""
            OUTLINE BEAT #{beatIndex + 1} of {totalBeats} (Act {actNumber}, Beat {positionInAct}):
              Goal: {beat.Goal}
              Intended tension: {beat.Tension}/10
              Emotional arc: {beat.EmotionalArc}
              Seeds to plant: {seedsToPlant}
              Payoffs to resolve: {payoffsToResolve}

            CHARACTER ARCS IN PLAY:
            {arcContext}

            GENERATED PROSE:
            {generatedText}

            Evaluate this beat against the plan.
            """;

        try
        {
            var response = await llm.GenerateAsync(system, user, 0.2f, 1024, null, ct);
            var json = ExtractJson(response);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new ArcValidation
            {
                AchievedGoal = root.TryGetProperty("achieved_goal", out var ag) && ag.GetBoolean(),
                GoalScore = root.TryGetProperty("goal_score", out var gs) ? gs.GetInt32() : 5,
                TensionActual = root.TryGetProperty("tension_actual", out var ta) ? ta.GetInt32() : beat.Tension,
                DriftWarning = root.TryGetProperty("drift_warning", out var dw) ? dw.GetString() ?? "" : "",
                ArcProgress = root.TryGetProperty("arc_progress", out var ap) ? ap.GetString() ?? "" : "",
                Suggestions = root.TryGetProperty("suggestions", out var sg)
                    ? sg.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList()
                    : [],
                SeedsPlanted = root.TryGetProperty("seeds_planted", out var sp)
                    ? sp.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList()
                    : [],
                SeedsMissed = root.TryGetProperty("seeds_missed", out var sm)
                    ? sm.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList()
                    : [],
            };
        }
        catch
        {
            return new ArcValidation { AchievedGoal = true, GoalScore = 5, DriftWarning = "Arc validation failed — continuing without validation" };
        }
    }

    /// <summary>
    /// Build a cumulative arc progress summary for injection into subsequent beats.
    /// Helps the generator stay on track with the planned arcs.
    /// </summary>
    public string BuildArcGuidance(StoryOutline outline, int currentBeatIndex, List<ArcValidation> priorValidations)
    {
        if (priorValidations.Count == 0) return "";

        var lines = new List<string> { "ARC PROGRESS SO FAR:" };

        // Summarize drift warnings
        var drifts = priorValidations.Where(v => v.DriftWarning.Length > 0).ToList();
        if (drifts.Count > 0)
        {
            lines.Add("  DRIFT CORRECTIONS NEEDED:");
            foreach (var d in drifts.TakeLast(3))
                lines.Add($"    - {d.DriftWarning}");
        }

        // Summarize missed seeds that still need planting
        var allMissed = priorValidations.SelectMany(v => v.SeedsMissed).Distinct().ToList();
        var allPlanted = priorValidations.SelectMany(v => v.SeedsPlanted).Distinct().ToList();
        var stillMissing = allMissed.Except(allPlanted).ToList();
        if (stillMissing.Count > 0)
            lines.Add($"  SEEDS STILL NEEDING PLANTING: {string.Join(", ", stillMissing)}");

        // Overall arc progress
        var lastArc = priorValidations.LastOrDefault(v => v.ArcProgress.Length > 0);
        if (lastArc != null)
            lines.Add($"  LATEST ARC STATUS: {lastArc.ArcProgress}");

        // Average goal achievement
        var avgScore = priorValidations.Average(v => v.GoalScore);
        if (avgScore < 6)
            lines.Add($"  WARNING: Average goal achievement is {avgScore:F1}/10 — story may be drifting from outline. Refocus on beat goals.");

        return lines.Count <= 1 ? "" : string.Join("\n", lines);
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : "{}";
    }
}

public class ArcValidation
{
    public bool AchievedGoal { get; init; }
    public int GoalScore { get; init; }
    public int TensionActual { get; init; }
    public string DriftWarning { get; init; } = "";
    public string ArcProgress { get; init; } = "";
    public List<string> Suggestions { get; init; } = [];
    public List<string> SeedsPlanted { get; init; } = [];
    public List<string> SeedsMissed { get; init; } = [];
}
