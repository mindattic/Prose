using System.Collections.Concurrent;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Per-node LRU entity working-memory. Singleton service keyed by NodeId.
///
/// Every beat pushed to the stack adds direct entities (depth 0) and their semantic
/// neighbors (depth 1, 2). Entities not mentioned for EvictAfterBeats beats are
/// automatically evicted to make room for more recent references. Depth-0 entries
/// (directly named in the current beat goal or prose) are never evicted mid-beat.
/// </summary>
public sealed class EntityContextStack
{
    public const int StackCapacity = 20;
    private const int EvictAfterBeats = 4;

    public sealed record StackEntry(
        Guid EntityId,
        string Name,
        string EntityType,
        string Description,
        double Score,
        int PushedAtBeat,
        int LastMentionedBeat,
        int Depth);

    private sealed class NodeState
    {
        public readonly ConcurrentDictionary<Guid, StackEntry> Entries = new();
        public int BeatCounter;
    }

    private readonly ConcurrentDictionary<Guid, NodeState> nodes = new();

    private NodeState GetOrCreate(Guid nodeId) =>
        nodes.GetOrAdd(nodeId, _ => new NodeState());

    /// <summary>Call at the start of each beat. Increments LRU counter and evicts stale depth>0 entries.</summary>
    public void BeginBeat(Guid nodeId)
    {
        var state = GetOrCreate(nodeId);
        Interlocked.Increment(ref state.BeatCounter);
        EvictStale(state);
    }

    /// <summary>Push an entity onto the stack. If already present, refreshes its LRU timestamp and upgrades to lower depth.</summary>
    public void Push(Guid nodeId, Guid entityId, string name, string entityType, string description, double score, int depth = 0)
    {
        if (entityId == Guid.Empty) return;
        var state = GetOrCreate(nodeId);

        state.Entries.AddOrUpdate(entityId,
            _ => new StackEntry(entityId, name, entityType, description, score,
                state.BeatCounter, state.BeatCounter, depth),
            (_, existing) => existing with
            {
                LastMentionedBeat = state.BeatCounter,
                Depth = Math.Min(existing.Depth, depth),
                Description = string.IsNullOrWhiteSpace(existing.Description) && !string.IsNullOrWhiteSpace(description)
                    ? description : existing.Description,
            });

        if (state.Entries.Count > StackCapacity)
            EvictLru(state);
    }

    /// <summary>Record that the given entities were mentioned in generated prose, refreshing their LRU timestamps.</summary>
    public void RecordMentions(Guid nodeId, IEnumerable<Guid> entityIds)
    {
        if (!nodes.TryGetValue(nodeId, out var state)) return;
        foreach (var id in entityIds)
            if (state.Entries.TryGetValue(id, out var e))
                state.Entries[id] = e with { LastMentionedBeat = state.BeatCounter };
    }

    /// <summary>Returns active entities ordered by most-recently-mentioned, then by depth.</summary>
    public IReadOnlyList<StackEntry> GetActive(Guid nodeId)
    {
        if (!nodes.TryGetValue(nodeId, out var state)) return [];
        return [.. state.Entries.Values
            .OrderByDescending(e => e.LastMentionedBeat)
            .ThenBy(e => e.Depth)];
    }

    /// <summary>Clears the stack for a node (use when starting a new node session).</summary>
    public void Clear(Guid nodeId) => nodes.TryRemove(nodeId, out _);

    private static void EvictStale(NodeState state)
    {
        foreach (var e in state.Entries.Values.ToList())
            if (e.Depth > 0 && state.BeatCounter - e.LastMentionedBeat >= EvictAfterBeats)
                state.Entries.TryRemove(e.EntityId, out _);
    }

    private static void EvictLru(NodeState state)
    {
        // Prefer evicting low-priority (depth > 0) entries first; fall back to
        // any LRU entry when all entries are depth-0 to keep count within capacity.
        var lru = state.Entries.Values
                      .Where(e => e.Depth > 0)
                      .MinBy(e => e.LastMentionedBeat)
                  ?? state.Entries.Values.MinBy(e => e.LastMentionedBeat);
        if (lru != null)
            state.Entries.TryRemove(lru.EntityId, out _);
    }
}
