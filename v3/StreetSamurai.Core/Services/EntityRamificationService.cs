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
/// same node with an LLM ramification check, flagging only those whose
/// content actually conflicts with or depends on the changed entity.
///
/// The index side (<see cref="IndexBeatMentionsAsync"/>) is called by
/// <see cref="NodeWorkbenchService"/> after every beat write.
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

        var index = await GetNameIndexAsync(ct);

        var hits = new Dictionary<Guid, NameEntry>();
        foreach (var entry in index)
        {
            if (hits.ContainsKey(entry.EntityId)) continue;
            if (ContainsWholeWord(beatText, entry.MatchText, entry.CaseSensitive))
                hits[entry.EntityId] = entry;
        }

        var mentioned = hits.Values
            .Select(e => new BeatEntityMention
            {
                BeatId     = beatId,
                EntityId   = e.EntityId,
                EntityName = e.CanonicalName,
                EntityType = e.EntityType,
                CreatedAt  = DateTime.UtcNow,
            })
            .ToList();

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Replace all existing mentions for this beat atomically.
        await db.BeatEntityMentions.Where(m => m.BeatId == beatId).ExecuteDeleteAsync(ct);
        if (mentioned.Count > 0)
        {
            db.BeatEntityMentions.AddRange(mentioned);
            await db.SaveChangesAsync(ct);
        }
    }

    // ── Name index (names + character aliases, whole-word matching) ─────────

    private sealed record NameEntry(Guid EntityId, string MatchText, string CanonicalName, string EntityType, bool CaseSensitive);

    /// <summary>Alias values that are ordinary English words (number words etc.) — too
    /// ambiguous to index even case-sensitively ("Eight seconds later…").</summary>
    private static readonly HashSet<string> AliasStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "one","two","three","four","five","six","seven","eight","nine","ten",
        "eleven","twelve","thirteen","fourteen","fifteen","sixteen","seventeen","eighteen","nineteen","twenty",
        "thirty","forty","fifty","sixty","seventy","eighty","ninety","hundred","thousand",
        "north","south","east","west","left","right",
    };

    private static List<NameEntry>? nameIndexCache;
    private static DateTime nameIndexBuiltAt = DateTime.MinValue;

    /// <summary>
    /// Match texts for every active entity: the entity Name plus, for characters,
    /// every CharacterAliases value (this is what lets prose that says just "Kyle"
    /// index against "Kyle Ellen Corbin"). Cached for 60s so the bulk backfill
    /// builds it once instead of once per beat.
    /// </summary>
    private async Task<List<NameEntry>> GetNameIndexAsync(CancellationToken ct)
    {
        if (nameIndexCache is { } cached && (DateTime.UtcNow - nameIndexBuiltAt) < TimeSpan.FromSeconds(60))
            return cached;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var entities = await db.Entities
            .Where(e => e.IsActive && e.Name != "")
            .Select(e => new { e.Id, e.Name, e.EntityType })
            .ToListAsync(ct);

        var aliasRows = await db.Characters
            .SelectMany(c => c.Aliases.Select(a => new { c.Id, a.Value }))
            .ToListAsync(ct);

        var byId = entities.ToDictionary(e => e.Id);
        var index = new List<NameEntry>(entities.Count + aliasRows.Count);

        // Longer match texts first so a hit on "Kyle Ellen Corbin" short-circuits "Kyle".
        foreach (var e in entities)
            if (e.Name.Length >= 3)
                index.Add(new NameEntry(e.Id, e.Name, e.Name, e.EntityType, CaseSensitive: false));

        // Aliases are proper-noun handles: match case-SENSITIVELY ("Bear said" but not
        // "couldn't bear it"), skip lowercase-initial epithets ("the wall" would match
        // that literal phrase in any beat) and ordinary-word aliases.
        foreach (var a in aliasRows)
            if (!string.IsNullOrWhiteSpace(a.Value) && a.Value.Length >= 3
                && char.IsUpper(a.Value[0])
                && !AliasStopWords.Contains(a.Value)
                && byId.TryGetValue(a.Id, out var owner))
                index.Add(new NameEntry(owner.Id, a.Value, owner.Name, owner.EntityType, CaseSensitive: true));

        var built = index.OrderByDescending(e => e.MatchText.Length).ToList();
        nameIndexCache = built;
        nameIndexBuiltAt = DateTime.UtcNow;
        return built;
    }

    /// <summary>Case-insensitive whole-word containment: the match may not be
    /// flanked by letters or digits ("held" no longer matches "Eld").</summary>
    private static bool ContainsWholeWord(string text, string word, bool caseSensitive = false)
    {
        var cmp = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        int i = 0;
        while ((i = text.IndexOf(word, i, cmp)) >= 0)
        {
            bool leftOk  = i == 0 || !char.IsLetterOrDigit(text[i - 1]);
            int end = i + word.Length;
            bool rightOk = end >= text.Length || !char.IsLetterOrDigit(text[end]);
            if (leftOk && rightOk) return true;
            i += 1;
        }
        return false;
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

        return await db.BeatNodes
            .Where(sb => sb.Beat!.EntityStale && sb.IsEnabled)
            .Select(sb => new EntityStaleBeatDto
            {
                BeatId      = sb.BeatId,
                BeatNumber  = sb.Beat!.Number,
                NodeId    = sb.NodeId,
                NodeTitle = sb.Node!.Title,
                SortKey     = sb.SortKey,
                TextPreview = string.IsNullOrEmpty(sb.Beat.Text) ? "" : sb.Beat.Text.Length > 120 ? sb.Beat.Text.Substring(0, 120) + "…" : sb.Beat.Text,
                Entities    = db.BeatEntityMentions
                    .Where(m => m.BeatId == sb.BeatId)
                    .Select(m => m.EntityName)
                    .ToList(),
            })
            .OrderBy(x => x.NodeTitle).ThenBy(x => x.SortKey)
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
                var nodePositions = await db.BeatNodes
                    .Where(sb => sb.BeatId == directBeatId && sb.IsEnabled)
                    .Select(sb => new { sb.NodeId, sb.SortKey })
                    .ToListAsync();

                foreach (var pos in nodePositions)
                {
                    var downstream = await db.BeatNodes
                        .Where(sb => sb.NodeId == pos.NodeId
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
    public Guid   NodeId    { get; set; }
    public string NodeTitle { get; set; } = "";
    public double SortKey     { get; set; }
    public string TextPreview { get; set; } = "";
    public List<string> Entities { get; set; } = [];
}
