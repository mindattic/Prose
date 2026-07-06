using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Deterministic (zero LLM calls) timeline-consistency validator.
///
/// Two detection classes:
///
/// DETECTION 1 — "dead-character-acting"
///   Finds entities whose <c>status</c> aspect (EntityStateEvent) is "dead" /
///   "deceased" at some AtStoryTime T, then checks whether that entity is
///   mentioned in any Beat in the node whose EntityStateEvent story-time is
///   later than T. Severity: high.
///
/// DETECTION 2 — "wound-regression"
///   For each entity with <c>condition.*.severity</c> events, walks them in
///   story-time order and flags any case where a "healed"/"none"/0-severity
///   row has an AtStoryTime EARLIER than the corresponding injury event.
///   (Conservative: only clear ordering violations.) Severity: medium.
///
/// Both detectors are no-ops when the event ledger is unpopulated — they
/// return an empty list rather than throw.
/// </summary>
public class TimelineConsistencyService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<TimelineConsistencyService> log;

    /// <summary>Values (case-insensitive) that indicate an entity is dead.</summary>
    private static readonly HashSet<string> DeathValues =
        new(StringComparer.OrdinalIgnoreCase) { "dead", "deceased", "killed", "dead (confirmed)" };

    /// <summary>Values (case-insensitive) that indicate a wound is healed/resolved.</summary>
    private static readonly HashSet<string> HealedValues =
        new(StringComparer.OrdinalIgnoreCase) { "healed", "none", "resolved", "0", "clear" };

    public TimelineConsistencyService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<TimelineConsistencyService> log)
    {
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    /// <summary>
    /// Result type returned by <see cref="CheckNodeAsync"/>.
    /// </summary>
    public sealed record TimelineFinding(
        string   Kind,
        Guid?    EntityId,
        string?  EntityName,
        int?     BeatNumber,
        string   Detail,
        string   Severity);

    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Runs both detectors over the given node and returns any findings.
    /// Returns an empty list when the event ledger has no relevant data —
    /// never throws.
    /// </summary>
    public async Task<List<TimelineFinding>> CheckNodeAsync(
        Guid nodeId,
        CancellationToken ct = default)
    {
        var findings = new List<TimelineFinding>();
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            // Collect the set of entity IDs and beat IDs for this node.
            // We join via BeatEntityMention (new unified-schema path).
            var BeatNodeQuery = await (
                from sb in db.BeatNodes.AsNoTracking()
                join b  in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
                where sb.NodeId == nodeId && sb.IsEnabled
                orderby sb.SortKey
                select new { BeatId = b.Id, b.Number }
            ).ToListAsync(ct);

            if (BeatNodeQuery.Count == 0)
            {
                log.LogDebug("TimelineConsistencyService: node {NodeId} has no beats — skipping", nodeId);
                return findings;
            }

            var beatIds = BeatNodeQuery.Select(x => x.BeatId).ToHashSet();
            var beatNumberById = BeatNodeQuery.ToDictionary(x => x.BeatId, x => x.Number);

            // Entity mentions per beat in this node.
            var mentionRows = await db.BeatEntityMentions.AsNoTracking()
                .Where(m => beatIds.Contains(m.BeatId))
                .ToListAsync(ct);

            if (mentionRows.Count == 0)
            {
                log.LogDebug("TimelineConsistencyService: node {NodeId} has no entity mentions — skipping", nodeId);
                return findings;
            }

            // Unique entity IDs that appear in this node.
            var entityIds = mentionRows.Select(m => m.EntityId).Distinct().ToList();

            // All EntityStateEvents for these entities (unbounded to story time —
            // we need the full history to find the earliest death event).
            var events = await db.EntityStateEvents.AsNoTracking()
                .Where(e => entityIds.Contains(e.EntityId))
                .OrderBy(e => e.EntityId)
                .ThenBy(e => e.AtStoryTime)
                .ToListAsync(ct);

            if (events.Count == 0)
            {
                log.LogDebug("TimelineConsistencyService: no EntityStateEvents for node {NodeId} entities", nodeId);
                return findings;
            }

            // Resolve entity names (from event data first; fall back to DB).
            var entityNameById = new Dictionary<Guid, string>();
            foreach (var m in mentionRows)
                entityNameById.TryAdd(m.EntityId, m.EntityName);

            // ── DETECTION 1: dead-character-acting ──────────────────────
            // For each entity, find the earliest AtStoryTime at which they
            // become "dead"/"deceased" (status or condition.*.severity).
            // Then check if any beat in this node that mentions that entity
            // has a story-time reference AFTER the death time.
            //
            // Since Beat doesn't carry InWorldDate, we infer story time for a
            // beat-entity-mention by finding the latest EntityStateEvent whose
            // BeatGuid matches the beat, or the latest event AtStoryTime for
            // that entity at or before that beat's position.
            //
            // Simpler and more defensive: find the EARLIEST death-time for an
            // entity (across the entire ledger). Then check if the entity
            // appears in any event that was recorded AFTER that time.
            // For beats, we use the EntityStateEvent rows whose BeatGuid maps
            // to beats in this node — if an event for a "dead" entity is
            // recorded in a beat that comes after the death, that's a violation.

            var eventsByEntity = events.GroupBy(e => e.EntityId)
                .ToDictionary(g => g.Key, g => g.OrderBy(e => e.AtStoryTime).ToList());

            // Build a map of ChapterBeat.BeatGuid → Beat.Id for events that
            // reference beats by the legacy BeatGuid path.
            // EntityStateEvent.BeatGuid stores the ChapterBeat.BeatGuid (legacy)
            // OR null. For new beats we look at BeatEntityMention instead.

            // Strategy: for each entity with a death record, find the death time.
            // Then, for each beat in this node that mentions that entity,
            // try to infer the beat's story time. A beat's story time can be
            // inferred from any EntityStateEvent whose BeatGuid matches the beat.

            // Build beat story-time lookup (from ChapterBeat.BeatGuid on events).
            // This covers the legacy path. For the new path, we approximate
            // beat order with SortKey (ordinal position as a proxy).

            var beatStoryTime = new Dictionary<Guid, DateTime>();
            foreach (var ev in events)
            {
                if (ev.BeatGuid.HasValue && beatIds.Contains(ev.BeatGuid.Value))
                    if (!beatStoryTime.ContainsKey(ev.BeatGuid.Value))
                        beatStoryTime[ev.BeatGuid.Value] = ev.AtStoryTime;
                    else if (ev.AtStoryTime < beatStoryTime[ev.BeatGuid.Value])
                        beatStoryTime[ev.BeatGuid.Value] = ev.AtStoryTime;
            }

            foreach (var entityId in entityIds)
            {
                if (!eventsByEntity.TryGetValue(entityId, out var entityEvents)) continue;
                var entityName = entityNameById.GetValueOrDefault(entityId, entityId.ToString("N"));

                // Find the earliest death-status event.
                var deathEvent = entityEvents.FirstOrDefault(e =>
                    (e.AspectKey.Equals("status", StringComparison.OrdinalIgnoreCase)
                     && e.NewValue != null
                     && DeathValues.Contains(e.NewValue))
                    ||
                    (e.AspectKey.StartsWith("condition.", StringComparison.OrdinalIgnoreCase)
                     && e.AspectKey.EndsWith(".severity", StringComparison.OrdinalIgnoreCase)
                     && e.NewValue != null
                     && DeathValues.Contains(e.NewValue)));

                if (deathEvent == null) continue;

                var deathTime = deathEvent.AtStoryTime;

                // Find beats in this node that mention this entity AND
                // have a story time AFTER the death time.
                var mentionedBeats = mentionRows
                    .Where(m => m.EntityId == entityId)
                    .Select(m => m.BeatId)
                    .ToHashSet();

                foreach (var beatId in mentionedBeats)
                {
                    // Try to get an actual story time for this beat.
                    if (!beatStoryTime.TryGetValue(beatId, out var beatTime)) continue;
                    if (beatTime <= deathTime) continue;

                    var beatNum = beatNumberById.GetValueOrDefault(beatId);
                    findings.Add(new TimelineFinding(
                        Kind:       "dead-character-acting",
                        EntityId:   entityId,
                        EntityName: entityName,
                        BeatNumber: beatNum,
                        Detail:
                            $"{entityName} was marked dead/deceased at story-time {deathTime:yyyy-MM-dd HH:mm} " +
                            $"but appears in beat #{beatNum} at story-time {beatTime:yyyy-MM-dd HH:mm} " +
                            $"(aspect '{deathEvent.AspectKey}' = '{deathEvent.NewValue}').",
                        Severity: "high"));
                }
            }

            // ── DETECTION 2: wound-regression ───────────────────────────
            // For each entity, walk its condition.*.severity events in
            // story-time order. Flag any "healed"/"none"/0 event that has
            // an AtStoryTime EARLIER than the injury-onset event for the
            // same condition name.
            foreach (var entityId in entityIds)
            {
                if (!eventsByEntity.TryGetValue(entityId, out var entityEvents)) continue;
                var entityName = entityNameById.GetValueOrDefault(entityId, entityId.ToString("N"));

                // Group by condition name (the middle segment of condition.{name}.severity).
                var conditionEvents = entityEvents
                    .Where(e =>
                        e.AspectKey.StartsWith("condition.", StringComparison.OrdinalIgnoreCase)
                        && e.AspectKey.EndsWith(".severity", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(e => e.AspectKey, StringComparer.OrdinalIgnoreCase);

                foreach (var condGroup in conditionEvents)
                {
                    var condEvents = condGroup.OrderBy(e => e.AtStoryTime).ToList();
                    if (condEvents.Count < 2) continue;

                    // Find the first non-healed event (the "injury onset").
                    var injuryEvent = condEvents.FirstOrDefault(e =>
                        e.NewValue != null && !HealedValues.Contains(e.NewValue));
                    if (injuryEvent == null) continue;

                    // Find any healed event that predates the injury event.
                    var prematureHeal = condEvents.FirstOrDefault(e =>
                        e.NewValue != null
                        && HealedValues.Contains(e.NewValue)
                        && e.AtStoryTime < injuryEvent.AtStoryTime);
                    if (prematureHeal == null) continue;

                    findings.Add(new TimelineFinding(
                        Kind:       "wound-regression",
                        EntityId:   entityId,
                        EntityName: entityName,
                        BeatNumber: null,
                        Detail:
                            $"{entityName} has a '{prematureHeal.NewValue}' event for {condGroup.Key} " +
                            $"at {prematureHeal.AtStoryTime:yyyy-MM-dd HH:mm}, but the injury was first " +
                            $"recorded at {injuryEvent.AtStoryTime:yyyy-MM-dd HH:mm} " +
                            $"(value '{injuryEvent.NewValue}'). The healed event predates the injury.",
                        Severity: "medium"));
                }
            }

            log.LogInformation(
                "TimelineConsistencyService: node {NodeId} — {Count} finding(s) ({Beats} beats, {Entities} entities, {Events} events)",
                nodeId, findings.Count, beatIds.Count, entityIds.Count, events.Count);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "TimelineConsistencyService: unexpected error for node {NodeId} — returning empty findings", nodeId);
        }

        return findings;
    }
}
