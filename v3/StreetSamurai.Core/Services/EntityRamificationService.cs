using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// When a canon entity is updated, finds every beat that mentions it
/// (via <see cref="BeatEntityMention"/>) and marks those beats
/// <see cref="Beat.EntityStale"/>. Then scans downstream beats in the
/// same strand with an LLM ramification check, flagging only those whose
/// content actually conflicts with or depends on the changed entity.
///
/// The index side (<see cref="IndexBeatMentionsAsync"/>) is called by
/// <see cref="StrandWorkbenchService"/> after every beat write.
/// </summary>
public class EntityRamificationService(
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    ILlmService llm,
    ILogger<EntityRamificationService> log)
{
    const string RamificationSystem =
        "You are a continuity checker for a fiction project. " +
        "Answer only YES or NO (one word, uppercase), followed by \" — \" and a reason of at most one sentence.";

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Called when entity <paramref name="entityId"/> is saved.
    /// Marks direct-mention beats EntityStale, then queues an async
    /// downstream ramification scan.
    /// </summary>
    public async Task ProcessEntityUpdateAsync(Guid entityId, string entityName, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var directBeatIds = await db.BeatEntityMentions
            .Where(m => m.EntityId == entityId)
            .Select(m => m.BeatId)
            .ToListAsync(ct);

        if (directBeatIds.Count == 0) return;

        await db.Beats
            .Where(b => directBeatIds.Contains(b.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.EntityStale, true), ct);

        log.LogInformation(
            "EntityRamification: {Count} direct beat(s) flagged for entity {Name} ({Id})",
            directBeatIds.Count, entityName, entityId);

        var entityDesc = await db.Entities
            .Where(e => e.Id == entityId)
            .Select(e => e.Description)
            .FirstOrDefaultAsync(ct) ?? "";

        // Downstream scan is fire-and-forget; don't block the save path.
        _ = Task.Run(() => ScanDownstreamAsync(directBeatIds, entityName, entityDesc), CancellationToken.None);
    }

    /// <summary>
    /// Extracts entity name matches from <paramref name="beatText"/> and
    /// upserts <see cref="BeatEntityMention"/> rows for <paramref name="beatId"/>.
    /// Called after every beat write.
    /// </summary>
    public async Task IndexBeatMentionsAsync(Guid beatId, string beatText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(beatText)) return;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var allEntities = await db.Entities
            .Where(e => e.IsActive && e.Name != "")
            .Select(e => new { e.Id, e.Name, e.EntityType })
            .ToListAsync(ct);

        var mentioned = allEntities
            .Where(e => beatText.Contains(e.Name, StringComparison.OrdinalIgnoreCase))
            .Select(e => new BeatEntityMention
            {
                BeatId     = beatId,
                EntityId   = e.Id,
                EntityName = e.Name,
                EntityType = e.EntityType,
                CreatedAt  = DateTime.UtcNow,
            })
            .ToList();

        // Replace all existing mentions for this beat atomically.
        await db.BeatEntityMentions.Where(m => m.BeatId == beatId).ExecuteDeleteAsync(ct);
        if (mentioned.Count > 0)
        {
            db.BeatEntityMentions.AddRange(mentioned);
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Bulk backfill: indexes mentions for every beat in the DB.
    /// Used by the <c>--scan-entity-mentions</c> CLI.
    /// </summary>
    public async Task BackfillAllBeatsAsync(IProgress<(int done, int total)>? progress = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var beatIds = await db.Beats
            .Select(b => new { b.Id, b.Text })
            .ToListAsync(ct);

        int done = 0;
        foreach (var beat in beatIds)
        {
            if (ct.IsCancellationRequested) break;
            await IndexBeatMentionsAsync(beat.Id, beat.Text, ct);
            progress?.Report((++done, beatIds.Count));
        }
    }

    // ── Entity-stale review helpers ─────────────────────────────────────────

    /// <summary>Returns all beats with <see cref="Beat.EntityStale"/> = true.</summary>
    public async Task<List<EntityStaleBeatDto>> GetEntityStaleBeatsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await db.StrandBeats
            .Where(sb => sb.Beat!.EntityStale && sb.IsEnabled)
            .Select(sb => new EntityStaleBeatDto
            {
                BeatId      = sb.BeatId,
                BeatNumber  = sb.Beat!.Number,
                StrandId    = sb.StrandId,
                StrandTitle = sb.Strand!.Title,
                SortKey     = sb.SortKey,
                TextPreview = string.IsNullOrEmpty(sb.Beat.Text) ? "" : sb.Beat.Text.Length > 120 ? sb.Beat.Text.Substring(0, 120) + "…" : sb.Beat.Text,
                Entities    = db.BeatEntityMentions
                    .Where(m => m.BeatId == sb.BeatId)
                    .Select(m => m.EntityName)
                    .ToList(),
            })
            .OrderBy(x => x.StrandTitle).ThenBy(x => x.SortKey)
            .ToListAsync(ct);
    }

    /// <summary>Clears <see cref="Beat.EntityStale"/> on a beat after author review.</summary>
    public async Task ClearEntityStaleAsync(Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.Beats
            .Where(b => b.Id == beatId)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.EntityStale, false), ct);
    }

    // ── Private ─────────────────────────────────────────────────────────────

    private async Task ScanDownstreamAsync(List<Guid> directBeatIds, string entityName, string entityDesc)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();

            foreach (var directBeatId in directBeatIds)
            {
                var strandPositions = await db.StrandBeats
                    .Where(sb => sb.BeatId == directBeatId && sb.IsEnabled)
                    .Select(sb => new { sb.StrandId, sb.SortKey })
                    .ToListAsync();

                foreach (var pos in strandPositions)
                {
                    var downstream = await db.StrandBeats
                        .Where(sb => sb.StrandId == pos.StrandId
                                  && sb.SortKey > pos.SortKey
                                  && sb.IsEnabled)
                        .OrderBy(sb => sb.SortKey)
                        .Select(sb => new { sb.BeatId, sb.Beat!.Text })
                        .ToListAsync();

                    foreach (var d in downstream)
                    {
                        if (string.IsNullOrWhiteSpace(d.Text)) continue;
                        var flagged = await CheckRamificationAsync(entityName, entityDesc, d.Text);
                        if (flagged)
                        {
                            await db.Beats
                                .Where(b => b.Id == d.BeatId)
                                .ExecuteUpdateAsync(s => s.SetProperty(b => b.EntityStale, true));
                            log.LogInformation(
                                "EntityRamification: downstream beat {BeatId} flagged (entity: {Name})",
                                d.BeatId, entityName);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "EntityRamification: downstream scan failed for entity {Name}", entityName);
        }
    }

    private async Task<bool> CheckRamificationAsync(string entityName, string entityDesc, string beatText)
    {
        var user =
            $"Entity \"{entityName}\" was just updated. Its current canonical description:\n" +
            $"{(entityDesc.Length > 400 ? entityDesc[..400] + "…" : entityDesc)}\n\n" +
            $"Beat text:\n{beatText}\n\n" +
            "Does this beat's content conflict with or contradict the current entity description?";

        try
        {
            var raw = await llm.GenerateAsync(RamificationSystem, user, temperature: 0.0f, maxTokens: 80);
            return raw.TrimStart().StartsWith("YES", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "EntityRamification: LLM check skipped for entity {Name}", entityName);
            return false;
        }
    }
}

public class EntityStaleBeatDto
{
    public Guid   BeatId      { get; set; }
    public int    BeatNumber  { get; set; }
    public Guid   StrandId    { get; set; }
    public string StrandTitle { get; set; } = "";
    public double SortKey     { get; set; }
    public string TextPreview { get; set; } = "";
    public List<string> Entities { get; set; } = [];
}
