namespace StreetSamurai.Core.Services;

/// <summary>
/// Captures, for a single refactor/generation "run", exactly which canon docs (Doc Context Stack)
/// and entities (Entity Context Stack) were pulled into working memory for each beat, with
/// timestamps and durations. The resulting <see cref="Run"/> is the machine-readable record behind
/// every instrumented export/review — serialized to JSON for the self-feedback loop, rendered to a
/// per-second .log, and to an interactive HTML timeline.
///
/// Singleton with a single "current run" — the refactor runner calls <see cref="BeginRun"/> before
/// the beat loop, ProseWriterRouter calls <see cref="RecordBeat"/> per beat while a run is active,
/// and the runner calls <see cref="EndRun"/> with the before/after scores.
/// </summary>
public sealed class ContextTelemetryService
{
    public sealed record DocLoad(string Path, string Tier, string Reason, double Score, int Chars);
    public sealed record EntityLoad(string Name, string Type, string MatchSource, double Score, int Depth);
    /// <summary>Full DCM working-set entry at a beat — not budget-clipped. Used for Gantt visualization.</summary>
    public sealed record StackDocEntry(string Path, string Tier, string Reason, double Score);

    public sealed record BeatRecord(
        int BeatIndex,
        string BeatId,
        string BeatTitle,
        DateTime StartedAt,
        double DurationMs,
        int ProseChars,
        IReadOnlyList<DocLoad> Docs,
        IReadOnlyList<EntityLoad> Entities,
        IReadOnlyList<StackDocEntry>? FullActiveSet = null);

    public sealed class Run
    {
        public Guid RunId { get; set; }
        public Guid NodeId { get; set; }
        public string NodeSlug { get; set; } = "";
        public string Label { get; set; } = "";
        public bool DocContextEnabled { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public double BaselineScore { get; set; }
        public double BaselineFlow { get; set; }
        public double FinalScore { get; set; }
        public double FinalFlow { get; set; }
        public List<BeatRecord> Beats { get; } = new();
    }

    private Run? current;
    private readonly object gate = new();

    public bool IsActive { get { lock (gate) return current != null; } }
    public Run? Current { get { lock (gate) return current; } }

    public void BeginRun(Guid runId, Guid nodeId, string nodeSlug, string label, bool docContextEnabled, DateTime startedAt, double baselineScore, double baselineFlow)
    {
        lock (gate)
        {
            current = new Run
            {
                RunId = runId,
                NodeId = nodeId,
                NodeSlug = nodeSlug,
                Label = label,
                DocContextEnabled = docContextEnabled,
                StartedAt = startedAt,
                BaselineScore = baselineScore,
                BaselineFlow = baselineFlow,
            };
        }
    }

    public void RecordBeat(BeatRecord record)
    {
        lock (gate) { current?.Beats.Add(record); }
    }

    public Run? EndRun(DateTime endedAt, double finalScore, double finalFlow)
    {
        lock (gate)
        {
            if (current == null) return null;
            current.EndedAt = endedAt;
            current.FinalScore = finalScore;
            current.FinalFlow = finalFlow;
            var done = current;
            current = null;
            return done;
        }
    }
}
