using System.Collections.Concurrent;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Tracks beat-mode history per node and warns the prose pipeline when consecutive
/// non-escalating beats have been written. Prevents tension stagnation without forcing
/// every beat to be a climax — only fires when the window shows genuine drift.
///
/// Purely in-memory (no DB) — resets on restart. This is intentional: the pattern
/// of tension drift is a per-session authorial signal, not a permanent canon fact.
/// ProseWriterRouter calls RecordBeat after every write and BuildGuidanceBlock before.
/// </summary>
public class TensionEscalationService
{
    private const int WindowSize = 5;
    private const int StagnationThreshold = 3;

    private static readonly HashSet<BeatMode> EscalatingModes =
    [
        BeatMode.Combat,
        BeatMode.EmotionalClimax,
        BeatMode.Revelation,
    ];

    private readonly ConcurrentDictionary<Guid, LinkedList<BeatMode>> recentModes = new();

    /// <summary>
    /// Record the mode of a completed beat. Called fire-and-forget from ProseWriterRouter
    /// after every successful write. Thread-safe.
    /// </summary>
    public void RecordBeat(Guid nodeId, BeatMode mode)
    {
        if (nodeId == Guid.Empty) return;

        var window = recentModes.GetOrAdd(nodeId, _ => new LinkedList<BeatMode>());
        lock (window)
        {
            window.AddLast(mode);
            while (window.Count > WindowSize)
                window.RemoveFirst();
        }
    }

    /// <summary>
    /// Return a non-empty guidance string when the node's recent beat history shows tension
    /// stagnation. Empty string = no guidance needed (no injection into the prompt).
    /// </summary>
    /// <param name="nodeId">Node being written.</param>
    /// <param name="incomingMode">Mode of the beat about to be written (don't warn if already escalating).</param>
    public string BuildGuidanceBlock(Guid nodeId, BeatMode incomingMode)
    {
        if (nodeId == Guid.Empty) return "";
        if (EscalatingModes.Contains(incomingMode)) return "";
        if (!recentModes.TryGetValue(nodeId, out var window)) return "";

        BeatMode[] recent;
        lock (window) { recent = [.. window]; }

        if (recent.Length < StagnationThreshold) return "";

        var nonEscalatingCount = recent.Count(m => !EscalatingModes.Contains(m));
        if (nonEscalatingCount < StagnationThreshold) return "";

        return $"""
            TENSION ESCALATION — the last {nonEscalatingCount} consecutive beats have been low-intensity ({string.Join(", ", recent.TakeLast(nonEscalatingCount).Select(m => m.ToString()))}).
            This beat must raise the stakes. Do not write another quiet beat.
            Options (choose what fits the scene): introduce new information that recontextualises what came before,
            deepen or surface an existing conflict, apply a consequence the POV character cannot ignore,
            or force a choice they cannot defer. The emotional temperature must be measurably higher at the
            end of this beat than it was at the start of the previous one.
            """;
    }

    /// <summary>Returns the recent mode window for a node (used by tests and the workflow monitor).</summary>
    public IReadOnlyList<BeatMode> GetRecentModes(Guid nodeId)
    {
        if (!recentModes.TryGetValue(nodeId, out var window)) return Array.Empty<BeatMode>();
        lock (window) { return [.. window]; }
    }

    /// <summary>Clear the history for a node (call when beginning a new writing session on a node).</summary>
    public void Reset(Guid nodeId) => recentModes.TryRemove(nodeId, out _);
}
