using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using System.Text;
using System.Text.RegularExpressions;

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
    /// <summary>What this snapshot looked at — so an empty result is never mistaken for
    /// "nothing is true", and a full result is never mistaken for "the whole universe".</summary>
    public string? Scope { get; set; }

    public string FormatAsContextBlock()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## World state at this beat");
        if (Scope != null) sb.AppendLine($"_({Scope})_");

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
    private readonly WorldStateLedger ledger;

    private const int MaxAspects = 400;
    private static readonly Regex GuidAttrRx = new(@"guid=""([0-9a-fA-F-]{36})""", RegexOptions.Compiled);

    public WorldStateAtBeatService(IDbContextFactory<ProseDbContext> dbFactory, WorldStateLedger ledger)
    {
        this.dbFactory = dbFactory;
        this.ledger = ledger;
    }

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

        // 2026-08-22 fix: EntityStateEvents are only extracted post-hoc, AFTER a chapter's prose
        // is already written (BeatStateExtractor.OnChapterSaved) — so during ordinary FORWARD
        // drafting, the beat about to be generated has no events of its own yet, and the query
        // above always returned null. This "Always"-documented service was therefore silently a
        // no-op for the primary use case (drafting a new beat), only ever populating when
        // REGENERATING an already-extracted beat.
        //
        // 2026-09-04: that fix took "the single most recent story-time fact across the whole
        // universe" as a proxy for "where is the story clock right now". It is only that for the
        // beat currently at the front of the draft. For any EARLIER beat — regenerating chapter 3
        // of a finished book, or any audit that walks a book in order — it returned state from the
        // END of the story, silently, as a confident answer. The cause was that a beat had no
        // position on any timeline, so the service could not ask "at or before THIS beat".
        //
        // Beat.StoryPosition (author ruling: beats are the authoritative time axis) gives it one.
        // Take the newest story time among events at or before this beat's own position, which is
        // exact instead of a proxy. The universe-wide guess is kept ONLY for a beat with no
        // stamped position, since a null position means unknown, not "the beginning".
        if (effectiveTime == null)
        {
            var scopeIdsForFallback = entityIds?.ToHashSet();
            var position = await db.Beats.AsNoTracking()
                .Where(b => b.Id == beatId)
                .Select(b => b.StoryPosition)
                .FirstOrDefaultAsync(ct);

            if (position != null)
            {
                var priorQ = from e in db.EntityStateEvents.AsNoTracking()
                             join b in db.Beats.AsNoTracking() on e.BeatGuid equals b.Id
                             where b.StoryPosition != null && b.StoryPosition <= position
                             select new { e.AtStoryTime, e.EntityId };
                if (scopeIdsForFallback is { Count: > 0 })
                    priorQ = priorQ.Where(x => scopeIdsForFallback.Contains(x.EntityId));
                effectiveTime = await priorQ
                    .OrderByDescending(x => x.AtStoryTime)
                    .Select(x => (DateTime?)x.AtStoryTime)
                    .FirstOrDefaultAsync(ct);
            }
            else
            {
                var fallbackQ = db.EntityStateEvents.AsNoTracking().Where(e => e.BeatGuid != beatId);
                if (scopeIdsForFallback is { Count: > 0 })
                    fallbackQ = fallbackQ.Where(e => scopeIdsForFallback.Contains(e.EntityId));
                effectiveTime = await fallbackQ
                    .OrderByDescending(e => e.AtStoryTime)
                    .Select(e => (DateTime?)e.AtStoryTime)
                    .FirstOrDefaultAsync(ct);
            }
        }

        var snapshot = new WorldStateSnapshot { BeatId = beatId, StoryTime = effectiveTime };
        if (effectiveTime == null)
        {
            snapshot.Scope = "COULD NOT LOOK — no story time could be inferred for this beat (no state events at or before its position).";
            return snapshot;
        }

        var t = effectiveTime.Value;
        var scopeIds = entityIds?.ToHashSet();

        // 2026-09-05: with no entityIds this used to ask the ledger for EVERY entity's latest
        // aspect in the universe (max: null) — on BCODA the MCP tool returned a 1M-character,
        // 8,929-line alphabetical roster of the corpus's background NPCs as "what is true right
        // now for this beat". A snapshot the reader cannot read is a fail-open. Default the scope
        // to the entities actually tagged in the beat's own chapter, and say so.
        if (scopeIds == null)
        {
            var nodeIds = await db.BeatNodes.AsNoTracking()
                .Where(bn => bn.BeatId == beatId)
                .Select(bn => bn.NodeId)
                .ToListAsync(ct);
            var texts = await (
                from bn in db.BeatNodes.AsNoTracking()
                join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
                where nodeIds.Contains(bn.NodeId) && b.Text != null
                select b.Text!).ToListAsync(ct);
            scopeIds = texts
                .SelectMany(txt => GuidAttrRx.Matches(txt).Cast<Match>()
                    .Select(m => Guid.TryParse(m.Groups[1].Value, out var g) ? g : Guid.Empty))
                .Where(g => g != Guid.Empty)
                .ToHashSet();
            if (scopeIds.Count == 0)
            {
                snapshot.Scope = $"COULD NOT LOOK — no entity tags in the beat's chapter ({texts.Count} beat(s) examined); pass entityIds to scope explicitly.";
                return snapshot;
            }
            snapshot.Scope = $"scoped to the {scopeIds.Count} entities tagged in this beat's chapter ({texts.Count} beat(s)); pass entityIds to override";
        }
        else
        {
            snapshot.Scope = $"scoped to {scopeIds.Count} caller-supplied entities";
        }

        // Latest EntityStateEvent per (EntityId, AspectKey) at or before this story time.
        // Delegates to WorldStateLedger.SnapshotManyAsync (2026-09-01) rather than querying
        // EntityStateEvents directly — that method groups before capping (this used to cap the
        // raw 2000-most-recent events BEFORE grouping, which could silently drop the correct
        // "latest" value for an aspect belonging to a less-recently-touched entity) and applies
        // a deterministic Id tie-break this call site didn't have before. The cap is a hard
        // ceiling on output size; the scope above is what keeps it from being hit in practice.
        var latestByKey = (await ledger.SnapshotManyAsync(scopeIds, t, max: MaxAspects, ct))
            .Values
            .ToList();
        if (latestByKey.Count >= MaxAspects)
            snapshot.Scope += $"; aspect list capped at {MaxAspects}";

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

        // Deterministic ordering before the cap: edges with a real StoryValidFrom (i.e. actual
        // temporal facts, not the always-on "timeless" majority) sort first, most recent first.
        // Without this, SQL Server's unordered TOP silently drops whichever rows its scan happens
        // to reach last — including newly-inserted temporal edges — once the table exceeds 500
        // matching rows (confirmed 2026-08-15: the motorcycle ownership edges were invisible to
        // every query until this ordering was added, despite being correctly written to Edges).
        var edges = await edgesQ
            .OrderByDescending(e => e.StoryValidFrom != null)
            .ThenByDescending(e => e.StoryValidFrom)
            .Take(500)
            .ToListAsync(ct);

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
