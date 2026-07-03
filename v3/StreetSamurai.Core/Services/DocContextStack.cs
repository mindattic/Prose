using System.Collections.Concurrent;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Per-context LRU working memory for canon <c>.md</c> documents — the document analog of
/// <see cref="EntityContextStack"/>. Keyed by a context id (NodeId for the prose engine;
/// a session key for the Claude Code hook). This is the "rotating cast" of pertinent docs:
/// the few that matter right now are resident; the rest stay in the DB.
///
/// Tier rules:
///   <c>always</c> — pinned; never evicted (the small universal core).
///   <c>node</c> — pinned while its scope matches the active context; never decays.
///   <c>topic</c>  — LRU: evicted after <see cref="EvictAfterActions"/> actions without a
///                   refresh, and capped at <see cref="TopicCapacity"/> (oldest topic dropped
///                   when over). This is the load-when-relevant / unload-when-not behaviour.
/// </summary>
public sealed class DocContextStack
{
    public const int TopicCapacity = 8;
    private const int EvictAfterActions = 4;

    public sealed record StackEntry(
        Guid DocId,
        string RelativePath,
        string Tier,
        string Scope,
        string Triggers,
        string Reason,            // provenance, e.g. "always" | "node:BCODA" | "keyword:schism" | "embedding 0.71"
        double Score,
        int PushedAtAction,
        int LastTouchedAction);

    private sealed class ContextState
    {
        public readonly ConcurrentDictionary<Guid, StackEntry> Entries = new();
        public int ActionCounter;
    }

    private readonly ConcurrentDictionary<Guid, ContextState> contexts = new();
    private ContextState GetOrCreate(Guid contextId) => contexts.GetOrAdd(contextId, _ => new ContextState());

    private static bool IsPinned(StackEntry e) => e.Tier is "always" or "node";
    private static int TierRank(string tier) => tier switch { "always" => 0, "node" => 1, _ => 2 };

    /// <summary>Call at the start of each action/beat/turn. Advances the LRU clock and evicts stale topic docs.</summary>
    public void BeginAction(Guid contextId)
    {
        var state = GetOrCreate(contextId);
        Interlocked.Increment(ref state.ActionCounter);
        EvictStale(state);
    }

    /// <summary>Push a doc. If already present, refresh its LRU timestamp and keep the strongest reason/score.</summary>
    public void Push(Guid contextId, StackEntry entry)
    {
        if (entry.DocId == Guid.Empty) return;
        var state = GetOrCreate(contextId);

        state.Entries.AddOrUpdate(entry.DocId,
            _ => entry with { PushedAtAction = state.ActionCounter, LastTouchedAction = state.ActionCounter },
            (_, existing) => existing with
            {
                LastTouchedAction = state.ActionCounter,
                Score  = Math.Max(existing.Score, entry.Score),
                Reason = entry.Score > existing.Score ? entry.Reason : existing.Reason,
            });

        // Capacity applies to topic docs only — pinned tiers never count against it.
        if (state.Entries.Values.Count(e => !IsPinned(e)) > TopicCapacity)
            EvictLruTopic(state);
    }

    /// <summary>Record that the given docs were referenced this action, refreshing their LRU timestamps.</summary>
    public void RecordMentions(Guid contextId, IEnumerable<Guid> docIds)
    {
        if (!contexts.TryGetValue(contextId, out var state)) return;
        foreach (var id in docIds)
            if (state.Entries.TryGetValue(id, out var e))
                state.Entries[id] = e with { LastTouchedAction = state.ActionCounter };
    }

    /// <summary>Active docs ordered by tier (always → node → topic), then most-recently-touched, then score.</summary>
    public IReadOnlyList<StackEntry> GetActive(Guid contextId)
    {
        if (!contexts.TryGetValue(contextId, out var state)) return [];
        return [.. state.Entries.Values
            .OrderBy(e => TierRank(e.Tier))
            .ThenByDescending(e => e.LastTouchedAction)
            .ThenByDescending(e => e.Score)];
    }

    public void Clear(Guid contextId) => contexts.TryRemove(contextId, out _);

    private static void EvictStale(ContextState state)
    {
        foreach (var e in state.Entries.Values.ToList())
            if (!IsPinned(e) && state.ActionCounter - e.LastTouchedAction >= EvictAfterActions)
                state.Entries.TryRemove(e.DocId, out _);
    }

    private static void EvictLruTopic(ContextState state)
    {
        var lru = state.Entries.Values.Where(e => !IsPinned(e)).MinBy(e => e.LastTouchedAction);
        if (lru != null) state.Entries.TryRemove(lru.DocId, out _);
    }
}
