using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using System.Text;

namespace Prose.Core.Services;

public class EntityAspectState
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = "";
    public string AspectKey { get; set; } = "";
    public string? Value { get; set; }
    public string Verb { get; set; } = "";
    public DateTime AtStoryTime { get; set; }
}

public class ActiveRelationship
{
    public Guid SourceId { get; set; }
    public string SourceName { get; set; } = "";
    public Guid TargetId { get; set; }
    public string TargetName { get; set; } = "";
    public string RelationType { get; set; } = "";
    public string Sentiment { get; set; } = "neutral";
}

public class WorldStateSnapshot
{
    public Guid BeatId { get; set; }
    public DateTime? StoryTime { get; set; }
    public List<EntityAspectState> EntityStates { get; set; } = [];
    public List<ActiveRelationship> ActiveEdges { get; set; } = [];

    public string FormatAsContextBlock()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## World state at this beat");

        if (EntityStates.Count > 0)
        {
            sb.AppendLine("### Entity aspects");
            foreach (var s in EntityStates.OrderBy(s => s.EntityName).ThenBy(s => s.AspectKey))
                sb.AppendLine($"- {s.EntityName} / {s.AspectKey}: {s.Value} (verb={s.Verb})");
        }

        if (ActiveEdges.Count > 0)
        {
            sb.AppendLine("### Active relationships");
            foreach (var r in ActiveEdges.OrderBy(r => r.SourceName))
            {
                var sent = r.Sentiment != "neutral" ? $" ({r.Sentiment})" : "";
                sb.AppendLine($"- {r.SourceName} —{r.RelationType}→ {r.TargetName}{sent}");
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// Builds a point-in-time world-state snapshot from EntityStateEvents and Edges.
/// Use before generation to inject consistent "what is true right now" context.
/// </summary>
public class WorldStateAtBeatService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public WorldStateAtBeatService(IDbContextFactory<ProseDbContext> dbFactory)
        => this.dbFactory = dbFactory;

    /// <summary>
    /// Returns a snapshot of world state at the given beat.
    /// If <paramref name="storyTime"/> is null, the service infers the story time from
    /// the most recent EntityStateEvent that references this beatId.
    /// Pass <paramref name="entityIds"/> to scope the snapshot to specific entities;
    /// omit for the full universe state (expensive on large DBs — scope in production).
    /// </summary>
    public async Task<WorldStateSnapshot> SnapshotAsync(
        Guid beatId,
        DateTime? storyTime = null,
        IEnumerable<Guid>? entityIds = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Infer story time from beat events when not provided
        var effectiveTime = storyTime;
        if (effectiveTime == null)
        {
            effectiveTime = await db.EntityStateEvents.AsNoTracking()
                .Where(e => e.BeatGuid == beatId)
                .OrderByDescending(e => e.AtStoryTime)
                .Select(e => (DateTime?)e.AtStoryTime)
                .FirstOrDefaultAsync(ct);
        }

        var snapshot = new WorldStateSnapshot { BeatId = beatId, StoryTime = effectiveTime };
        if (effectiveTime == null) return snapshot;

        var t = effectiveTime.Value;
        var scopeIds = entityIds?.ToHashSet();

        // Latest EntityStateEvent per (EntityId, AspectKey) at or before this story time.
        // Group in memory after a bounded query — avoids complex SQL GROUP BY on temporal tables.
        var eventsQ = db.EntityStateEvents.AsNoTracking()
            .Where(e => e.AtStoryTime <= t);

        if (scopeIds is { Count: > 0 })
            eventsQ = eventsQ.Where(e => scopeIds.Contains(e.EntityId));

        var events = await eventsQ
            .OrderByDescending(e => e.AtStoryTime)
            .Take(2000)
            .ToListAsync(ct);

        // Latest value per (EntityId, AspectKey)
        var latestByKey = events
            .GroupBy(e => (e.EntityId, e.AspectKey))
            .Select(g => g.OrderByDescending(e => e.AtStoryTime).First())
            .ToList();

        // Resolve entity names
        var entityIdSet = latestByKey.Select(e => e.EntityId).Distinct().ToList();
        var entityNames = await db.Entities.AsNoTracking()
            .Where(e => entityIdSet.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, ct);

        snapshot.EntityStates = latestByKey
            .Select(e => new EntityAspectState
            {
                EntityId = e.EntityId,
                EntityName = entityNames.GetValueOrDefault(e.EntityId, e.EntityId.ToString()),
                AspectKey = e.AspectKey,
                Value = e.NewValue,
                Verb = e.Verb,
                AtStoryTime = e.AtStoryTime,
            })
            .OrderBy(s => s.EntityName).ThenBy(s => s.AspectKey)
            .ToList();

        // Active edges at this story time
        var edgesQ = db.Edges.AsNoTracking()
            .Where(e => e.InvalidatedAt == null
                     && (e.StoryValidFrom == null || e.StoryValidFrom <= t)
                     && (e.StoryValidUntil == null || e.StoryValidUntil > t));

        if (scopeIds is { Count: > 0 })
            edgesQ = edgesQ.Where(e => scopeIds.Contains(e.SourceId) || scopeIds.Contains(e.TargetId));

        var edges = await edgesQ.Take(500).ToListAsync(ct);

        // Resolve names for edge endpoints
        var edgeEntityIds = edges.SelectMany(e => new[] { e.SourceId, e.TargetId }).Distinct().ToList();
        var edgeNames = await db.Entities.AsNoTracking()
            .Where(e => edgeEntityIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, ct);

        snapshot.ActiveEdges = edges
            .Select(e => new ActiveRelationship
            {
                SourceId = e.SourceId,
                SourceName = edgeNames.GetValueOrDefault(e.SourceId, e.SourceId.ToString()),
                TargetId = e.TargetId,
                TargetName = edgeNames.GetValueOrDefault(e.TargetId, e.TargetId.ToString()),
                RelationType = e.RelationType,
                Sentiment = e.Sentiment,
            })
            .OrderBy(r => r.SourceName)
            .ToList();

        return snapshot;
    }
}
