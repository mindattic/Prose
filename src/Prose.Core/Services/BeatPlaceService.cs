using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

public record BeatPlaceReport(string NodeCode, int Candidates, int Extracted, int Resolved, int Failed, int SkippedFromCache);

/// <summary>
/// Owns Beat.PlaceName / Beat.PlaceEntityId / Beat.PlaceExtractedFromHash — the per-beat scene
/// location added 2026-08-28 (the corpus previously had NO per-beat location signal at all:
/// BeatContext.Location was only ever a single book-wide DefaultLocation string, set on a
/// minority of books, so ambient sensory grounding was absent or scene-blind everywhere).
///
/// Three jobs:
///  1. Backfill/extraction (<see cref="ExtractAsync"/>): batched Haiku pass over a book's beats
///     in reading order, hash-gated on PlaceExtractedFromHash vs TextHash — same idiom, batching
///     and clip strategy as BeatEventSummaryService. Reading order matters here: the model
///     carries a scene's location forward across beats until the prose moves the scene.
///  2. Single-beat persist (<see cref="PersistAsync"/>): used by BeatExtractionService's
///     consolidated post-write call for the beat just written.
///  3. Router read (<see cref="GetPriorPlaceAsync"/>): the nearest prior beat's PlaceName within
///     the same chapter — ProseWriterRouter's scene-continuity default for BeatContext.Location.
///
/// Writes the Place* columns directly, never through UpdateBeatTextAsync/UpdateBeatMetadataAsync
/// (wrong semantics: Version bump, Stale, audio invalidation) — same rule as
/// BeatEventSummaryService documents for EventSummary.
/// </summary>
public class BeatPlaceService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly ILogger<BeatPlaceService> log;

    private const int PerBeatClipChars = 6_000;
    private const int BatchCharBudget = 60_000;
    private const int MaxBatchBeats = 12;

    public BeatPlaceService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILlmService llm,
        ILogger<BeatPlaceService> log)
    {
        this.dbFactory = dbFactory;
        this.llm = llm;
        this.log = log;
    }

    // ── Router read ──────────────────────────────────────────────────────────

    /// <summary>
    /// The nearest prior beat's PlaceName in the same chapter node, walking backwards from
    /// <paramref name="beatId"/> in BeatNodes.SortKey order; the beat's own PlaceName wins if
    /// already set (regeneration of an extracted beat). Null when no beat in the chapter has a
    /// location yet. Deliberately does not cross the chapter boundary — a new chapter is where
    /// the scene-continuity assumption is weakest.
    /// </summary>
    public async Task<string?> GetPriorPlaceAsync(Guid chapterNodeId, Guid beatId, CancellationToken ct = default)
    {
        if (chapterNodeId == Guid.Empty || beatId == Guid.Empty) return null;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var rows = await (
                from bn in db.BeatNodes.AsNoTracking()
                join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
                where bn.NodeId == chapterNodeId
                orderby bn.SortKey
                select new { b.Id, b.PlaceName, bn.SortKey }
            ).ToListAsync(ct);

            var self = rows.FirstOrDefault(r => r.Id == beatId);
            if (self == null) return rows.LastOrDefault(r => !string.IsNullOrWhiteSpace(r.PlaceName))?.PlaceName;
            if (!string.IsNullOrWhiteSpace(self.PlaceName)) return self.PlaceName;

            return rows
                .Where(r => r.SortKey < self.SortKey && !string.IsNullOrWhiteSpace(r.PlaceName))
                .OrderByDescending(r => r.SortKey)
                .FirstOrDefault()?.PlaceName;
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "[BeatPlaceService] prior-place lookup skipped for beat {BeatId}", beatId);
            return null;
        }
    }

    // ── Single-beat persist (post-write consolidated extraction) ────────────

    /// <summary>Persist an extracted scene location for one beat. No LLM call. Resolves
    /// PlaceEntityId against canon places in the beat's universe; stamps the hash gate.</summary>
    public async Task PersistAsync(Guid beatId, string placeName, CancellationToken ct = default)
    {
        if (beatId == Guid.Empty || string.IsNullOrWhiteSpace(placeName)) return;
        placeName = placeName.Trim();
        if (placeName.Length > 300) placeName = placeName[..300];

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat == null) return;

        var universeId = await (
            from bn in db.BeatNodes.AsNoTracking().IgnoreQueryFilters()
            join n in db.Nodes.AsNoTracking().IgnoreQueryFilters() on bn.NodeId equals n.Id
            where bn.BeatId == beatId
            select n.UniverseId
        ).FirstOrDefaultAsync(ct);

        beat.PlaceName = placeName;
        beat.PlaceEntityId = await ResolvePlaceEntityIdAsync(db, placeName, universeId, ct);
        beat.PlaceExtractedFromHash = beat.TextHash;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Resolve a free-text scene location to a canon place Entity in the given universe —
    /// exact (case-insensitive) match of the full string, then of each comma-separated segment,
    /// against Places.Name and PlaceAliases.Value. Ground-truth reads use IgnoreQueryFilters +
    /// an explicit universe check, same defense-in-depth rationale as
    /// SceneContextAssembler.FilterToBeatUniverseAsync.
    /// </summary>
    internal static async Task<Guid?> ResolvePlaceEntityIdAsync(
        ProseDbContext db, string placeName, Guid universeId, CancellationToken ct)
    {
        var segments = new List<string> { placeName };
        segments.AddRange(placeName.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        foreach (var segment in segments.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (segment.Length < 3) continue;

            var byName = await (
                from p in db.Set<Data.Entities.Place>().AsNoTracking().IgnoreQueryFilters()
                join e in db.Set<Data.Entities.Entity>().AsNoTracking().IgnoreQueryFilters() on p.Id equals e.Id
                where p.Name == segment && (universeId == Guid.Empty || e.UniverseId == universeId)
                select (Guid?)p.Id
            ).FirstOrDefaultAsync(ct);
            if (byName != null) return byName;

            var byAlias = await (
                from a in db.Set<Data.Entities.PlaceAlias>().AsNoTracking().IgnoreQueryFilters()
                join e in db.Set<Data.Entities.Entity>().AsNoTracking().IgnoreQueryFilters() on a.PlaceId equals e.Id
                where a.Value == segment && (universeId == Guid.Empty || e.UniverseId == universeId)
                select (Guid?)a.PlaceId
            ).FirstOrDefaultAsync(ct);
            if (byAlias != null) return byAlias;
        }
        return null;
    }

    // ── Backfill extraction ──────────────────────────────────────────────────

    private const string System = """
You extract WHERE each beat's scene takes place — the concrete physical location as the prose
establishes it (venue plus district/region when stated, e.g. "Doc Stash's clinic, The Shelf" or
"the archive under Vigil Keep"). Beats are given in reading order: when a beat clearly continues
the previous scene without moving, repeat the previous location. When the location is genuinely
indeterminate (pure abstraction, dream, montage across places), output "UNKNOWN".

Rules:
- Max ~10 words per location. Name places as the prose names them — do not invent geography.
- Output STRICT JSON, no fences, no commentary:
{"items":[{"ref":N,"place":"..."}]}
""";

    /// <summary>
    /// Hash-gated batched extraction over one book's beats (recursive descendant walk, reading
    /// order). Unchanged beats cost nothing on re-run.
    /// </summary>
    public async Task<BeatPlaceReport> ExtractAsync(
        string slugOrCode, int? limit = null, bool dryRun = false, bool force = false,
        Action<string>? progress = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(
            n => n.Slug == slugOrCode || (n.NodeCode != null && n.NodeCode.ToUpper() == slugOrCode.ToUpper()), ct)
            ?? throw new InvalidOperationException($"Node not found: {slugOrCode}");
        var nodeCode = node.NodeCode?.ToUpperInvariant() ?? node.Slug.ToUpperInvariant();

        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id, ct);
        var all = await (
            from bn in db.BeatNodes.AsNoTracking()
            join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
            join c in db.Nodes.AsNoTracking() on bn.NodeId equals c.Id
            where searchIds.Contains(bn.NodeId) && b.Text != null && b.Text != ""
            orderby c.SortKey, bn.SortKey
            select new { b.Id, b.Text, b.TextHash, b.PlaceName, b.PlaceExtractedFromHash, Chapter = c.Title }
        ).ToListAsync(ct);

        var candidates = all;
        var skippedFromCache = 0;
        if (!force)
        {
            // Same null-safe hash-gate shape as BeatEventSummaryService, but keyed on
            // PlaceExtractedFromHash (the actual "processed" marker) rather than PlaceName:
            // an honestly-UNKNOWN beat stamps the hash while deliberately leaving PlaceName
            // null (see the UNKNOWN branch below), so gating on PlaceName == null would make
            // that beat re-qualify on every single run forever — the exact "doesn't re-qualify
            // forever" guarantee the UNKNOWN branch's own comment promises but this filter used
            // to break.
            candidates = all.Where(a =>
                a.PlaceExtractedFromHash == null ||
                (a.TextHash != null &&
                 !string.Equals(a.PlaceExtractedFromHash, a.TextHash, StringComparison.OrdinalIgnoreCase))
            ).ToList();
            skippedFromCache = all.Count - candidates.Count;
        }
        if (limit.HasValue) candidates = candidates.Take(limit.Value).ToList();
        if (candidates.Count == 0)
            return new BeatPlaceReport(nodeCode, 0, 0, 0, 0, skippedFromCache);

        int extracted = 0, resolved = 0, failed = 0, done = 0;
        var universeId = node.UniverseId;

        foreach (var batch in BuildBatches(candidates.Select(c => (c.Id, c.Text!, c.Chapter)).ToList()))
        {
            ct.ThrowIfCancellationRequested();
            var refMap = new Dictionary<int, Guid>();
            var sb = new StringBuilder();
            for (int i = 0; i < batch.Count; i++)
            {
                refMap[i] = batch[i].Id;
                sb.AppendLine($"[ref {i} · {batch[i].Chapter}]");
                sb.AppendLine(Clip(batch[i].Text));
                sb.AppendLine();
            }

            try
            {
                var raw = await llm.GenerateAsync(System, sb.ToString(), temperature: 0.1,
                    maxTokens: 1200, model: LlmModels.Haiku, ct: ct);
                var places = ParsePlaceBatch(raw, refMap)
                    .GroupBy(p => p.BeatId).Select(g => g.Last()).ToList();
                if (places.Count == 0) { failed += batch.Count; }
                else
                {
                    var ids = batch.Select(b => b.Id).ToList();
                    var tracked = await db.Beats.Where(b => ids.Contains(b.Id)).ToListAsync(ct);
                    var trackedById = tracked.ToDictionary(b => b.Id);

                    var writtenIds = new HashSet<Guid>();
                    foreach (var (beatId, place) in places)
                    {
                        if (!trackedById.TryGetValue(beatId, out var beat)) continue;
                        if (place.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase))
                        {
                            // Honest indeterminate: stamp the hash so the beat doesn't
                            // re-qualify forever, but leave PlaceName/PlaceEntityId null.
                            if (!dryRun) beat.PlaceExtractedFromHash = beat.TextHash;
                            writtenIds.Add(beatId);
                            continue;
                        }
                        if (!dryRun)
                        {
                            beat.PlaceName = place.Length > 300 ? place[..300] : place;
                            beat.PlaceEntityId = await ResolvePlaceEntityIdAsync(db, place, universeId, ct);
                            beat.PlaceExtractedFromHash = beat.TextHash;
                            if (beat.PlaceEntityId != null) resolved++;
                        }
                        writtenIds.Add(beatId);
                        extracted++;
                    }
                    if (!dryRun) await db.SaveChangesAsync(ct);
                    failed += batch.Count - writtenIds.Count;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "BeatPlace batch failed ({Count} beats)", batch.Count);
                failed += batch.Count;
            }

            done += batch.Count;
            progress?.Invoke($"  {done}/{candidates.Count} processed ({extracted} extracted, {resolved} resolved to canon places, {failed} failed)");
        }

        return new BeatPlaceReport(nodeCode, candidates.Count, extracted, resolved, failed, skippedFromCache);
    }

    private static List<List<(Guid Id, string Text, string Chapter)>> BuildBatches(
        List<(Guid Id, string Text, string Chapter)> items)
    {
        var batches = new List<List<(Guid, string, string)>>();
        var current = new List<(Guid, string, string)>();
        int currentChars = 0;
        foreach (var item in items)
        {
            var clippedLen = Math.Min(item.Text.Length, PerBeatClipChars);
            if (current.Count > 0 &&
                (current.Count >= MaxBatchBeats || currentChars + clippedLen > BatchCharBudget))
            {
                batches.Add(current);
                current = new List<(Guid, string, string)>();
                currentChars = 0;
            }
            current.Add(item);
            currentChars += clippedLen;
        }
        if (current.Count > 0) batches.Add(current);
        return batches;
    }

    /// <summary>Head-weighted clip — scene location is almost always established at the TOP of
    /// a beat (establishing lines), unlike plot events which land at the end.</summary>
    private static string Clip(string text, int cap = PerBeatClipChars)
    {
        if (text.Length <= cap) return text;
        int head = cap * 3 / 4;
        int tail = cap - head;
        return text[..head] + "\n\n[...clipped for length...]\n\n" + text[^tail..];
    }

    internal static List<(Guid BeatId, string Place)> ParsePlaceBatch(
        string raw, IReadOnlyDictionary<int, Guid> refMap)
    {
        var results = new List<(Guid, string)>();
        try
        {
            using var doc = JsonDocument.Parse(JsonDefaults.StripCodeFences(raw));
            if (!doc.RootElement.TryGetProperty("items", out var arr)) return results;
            foreach (var el in arr.EnumerateArray())
            {
                try
                {
                    if (!el.TryGetProperty("ref", out var refEl) || refEl.ValueKind != JsonValueKind.Number) continue;
                    var place = el.TryGetProperty("place", out var m) ? m.GetString() : null;
                    if (string.IsNullOrWhiteSpace(place)) continue;
                    if (!refMap.TryGetValue(refEl.GetInt32(), out var beatId)) continue;
                    results.Add((beatId, place.Trim()));
                }
                catch { /* skip just this malformed entry */ }
            }
        }
        catch { /* malformed JSON entirely — return what parsed */ }
        return results;
    }
}
