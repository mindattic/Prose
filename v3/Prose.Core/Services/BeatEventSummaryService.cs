using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

public record BeatEventListReport(string NodeCode, int Candidates, int Generated, int Failed, int SkippedFromCache);

public record BeatEventListEntry(int SortKey, string? Title, string? Pov, string? EventSummary);

/// <summary>Row shape for the raw SqlQueryRaw POV lookup — BeatEntityPresence has no EF
/// entity mapping in this codebase (see ProseWriterRouter's identical pattern).</summary>
internal class PovRow
{
    public Guid BeatId { get; set; }
    public string? EntityName { get; set; }
}

/// <summary>
/// Generates the per-beat plot-EVENT one-liner (Beat.EventSummary) — "what happened", in the
/// terse, present-tense, name-anchored register the author reads to check a book's flow
/// without reading its prose. Distinct from MeaningBackfillService's Beat.Description, which
/// records authorial PURPOSE ("why this beat exists"), not plot events.
///
/// Hash-gated on Beat.EventSummaryHash vs Beat.TextHash (not a null-check): an edited beat's
/// TextHash moves, so it automatically re-qualifies next run; an untouched beat costs nothing
/// on re-run, same idiom as BeatChecklistGateService. Batches by a character budget rather
/// than a fixed beat count, since beat length varies enormously across books (VIGL averages
/// ~20k chars/beat; other books average ~1.6k) — a fixed "10 beats/call" would blow context
/// on some books and waste calls on others. Clips long beats head+tail rather than head-only,
/// since plot twists/reveals often land at the END of a beat.
///
/// Deliberately never touches NodeDocService, Node.NodeBible, or any DCM/MarkdownFiles
/// ingestion path — this is a human-readable QA artifact, not prose-generation context.
/// Writes EventSummary/EventSummaryHash directly; never routes through
/// NodeWorkbenchService.UpdateBeatTextAsync (wrong semantics: bumps Version, sets Stale,
/// invalidates audio) or the all-or-nothing UpdateBeatMetadataAsync (would clobber other
/// metadata fields).
/// </summary>
public class BeatEventSummaryService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly SettingsService settings;
    private readonly ILogger<BeatEventSummaryService> log;

    private const int PerBeatClipChars = 12_000;
    private const int BatchCharBudget = 70_000;
    private const int MaxBatchBeats = 10;

    public BeatEventSummaryService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILlmService llm,
        SettingsService settings,
        ILogger<BeatEventSummaryService> log)
    {
        this.dbFactory = dbFactory;
        this.llm = llm;
        this.settings = settings;
        this.log = log;
    }

    public async Task<BeatEventListReport> GenerateAsync(
        string slugOrCode, int? limit = null, bool dryRun = false, bool force = false,
        string? model = null, HashSet<int>? onlyNumbers = null,
        Action<string>? progress = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(
            n => n.Slug == slugOrCode || (n.NodeCode != null && n.NodeCode.ToUpper() == slugOrCode.ToUpper()), ct)
            ?? throw new InvalidOperationException($"Node not found: {slugOrCode}");
        var nodeCode = node.NodeCode?.ToUpperInvariant() ?? node.Slug.ToUpperInvariant();

        // Recurses past any nested Collection (2026-08-09 fix).
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id, ct);
        var all = await (
            from bn in db.BeatNodes.AsNoTracking()
            join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
            join c in db.Nodes.AsNoTracking() on bn.NodeId equals c.Id
            where true && searchIds.Contains(bn.NodeId)
                  && b.Text != null && b.Text != ""
            orderby c.SortKey, bn.SortKey
            select new { b.Id, b.Number, b.Text, b.TextHash, b.EventSummary, b.EventSummaryHash, Chapter = c.Title }
        ).ToListAsync(ct);

        var skippedFromCache = 0;
        var candidates = all;
        if (!force)
        {
            // Never generated -> candidate. Otherwise, only re-qualify when TextHash is known
            // AND doesn't match EventSummaryHash — a beat whose own TextHash is null (a rare,
            // pre-existing data-quality gap unrelated to this feature) must not infinitely
            // re-qualify just because EventSummaryHash mirrors that same null.
            candidates = all.Where(a =>
                a.EventSummary == null ||
                (a.TextHash != null &&
                 !string.Equals(a.EventSummaryHash, a.TextHash, StringComparison.OrdinalIgnoreCase))
            ).ToList();
            skippedFromCache = all.Count - candidates.Count;
        }

        // Targeted retry (e.g. remediating a batch where the model shifted refs and wrote a
        // summary onto the wrong beat) — bypasses the hash-gate for exactly these beat numbers,
        // same idiom as MeaningBackfillService's onlyNumbers.
        if (onlyNumbers is { Count: > 0 })
            candidates = all.Where(a => onlyNumbers.Contains(a.Number)).ToList();

        if (limit.HasValue) candidates = candidates.Take(limit.Value).ToList();

        if (candidates.Count == 0)
            return new BeatEventListReport(nodeCode, 0, 0, 0, skippedFromCache);

        const string system = """
You extract WHAT HAPPENED in each beat — concrete plot events, not themes, mood, or
authorial purpose. Write ONE line per beat (max ~15 words), present tense, third
person, naming the actors and the concrete action/outcome. Telegraphic — a plot
synopsis line, not a sentence of prose.

Examples of the register you must match:
- "Thieves steal Relic."
- "Lyra is dispatched to hunt them down."
- "Lyra arrives at the dock and investigates."
- "Kade lies about the ambush to protect Esvane."
- "The archive door seals, trapping both of them inside."

If a beat is pure connective tissue, scene-setting, or internal reflection with NO
new plot event, say so plainly and briefly — e.g. "No new event - transitional beat"
or "Description only; no plot advance." Do NOT invent a plot event to avoid saying
this. Honest flat/repeated lines across several beats are useful diagnostic signal
to the author about pacing, not a failure on your part.

Output STRICT JSON, no fences, no commentary:
{"items":[{"ref":N,"event":"..."}]}
""";

        int generated = 0, failed = 0;
        // Targeted retries run one beat per call — no batch means no ref-to-beat ambiguity,
        // which is the whole point of a remediation pass.
        var batches = onlyNumbers is { Count: > 0 }
            ? candidates.Select(c => new List<(Guid Id, string Text, string Chapter)> { (c.Id, c.Text!, c.Chapter) }).ToList()
            : BuildBatches(candidates.Select(c => (c.Id, c.Text!, c.Chapter)).ToList());

        int done = 0;
        foreach (var batch in batches)
        {
            ct.ThrowIfCancellationRequested();
            var refMap = new Dictionary<int, Guid>();
            var sb = new StringBuilder();
            for (int i = 0; i < batch.Count; i++)
            {
                refMap[i] = batch[i].Id;
                sb.AppendLine($"[ref {i} · {batch[i].Chapter}]");
                sb.AppendLine(ClipForEvent(batch[i].Text));
                sb.AppendLine();
            }

            try
            {
                var raw = await llm.GenerateAsync(system, sb.ToString(), temperature: 0.2,
                    maxTokens: 1800, model: model ?? LlmModels.Haiku, ct: ct);
                // Dedupe by beatId — a hallucinated duplicate "ref" in the LLM's JSON must not
                // double-count a beat as "generated twice" (last value wins on write either way).
                var events = ParseEventBatch(raw, refMap)
                    .GroupBy(e => e.BeatId).Select(g => g.Last()).ToList();
                if (events.Count == 0) { failed += batch.Count; }
                else
                {
                    var ids = batch.Select(b => b.Id).ToList();
                    var tracked = await db.Beats.Where(b => ids.Contains(b.Id)).ToListAsync(ct);
                    var trackedById = tracked.ToDictionary(b => b.Id);

                    var writtenIds = new HashSet<Guid>();
                    foreach (var (beatId, evt) in events)
                    {
                        if (trackedById.TryGetValue(beatId, out var beat))
                        {
                            if (!dryRun)
                            {
                                beat.EventSummary = evt;
                                beat.EventSummaryHash = beat.TextHash;
                            }
                            writtenIds.Add(beatId);
                        }
                    }
                    generated += writtenIds.Count;
                    if (!dryRun) await db.SaveChangesAsync(ct);
                    failed += batch.Count - writtenIds.Count;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "BeatEventSummary batch failed ({Count} beats)", batch.Count);
                failed += batch.Count;
            }

            done += batch.Count;
            progress?.Invoke($"  {done}/{candidates.Count} processed ({generated} generated, {failed} failed)");
        }

        return new BeatEventListReport(nodeCode, candidates.Count, generated, failed, skippedFromCache);
    }

    /// <summary>Reads the current DB state (no LLM call) for export/display — every enabled
    /// beat, SK-ordered, with its POV (if tagged) and EventSummary.</summary>
    public async Task<(string NodeCode, string Title, List<BeatEventListEntry> Entries)> GetEventListAsync(
        string slugOrCode, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(
            n => n.Slug == slugOrCode || (n.NodeCode != null && n.NodeCode.ToUpper() == slugOrCode.ToUpper()), ct)
            ?? throw new InvalidOperationException($"Node not found: {slugOrCode}");
        var nodeCode = node.NodeCode?.ToUpperInvariant() ?? node.Slug.ToUpperInvariant();

        // Recurses past any nested Collection (2026-08-09 fix).
        var eventListSearchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id, ct);
        var rows = await (
            from bn in db.BeatNodes.AsNoTracking()
            join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
            join c in db.Nodes.AsNoTracking() on bn.NodeId equals c.Id
            where true && eventListSearchIds.Contains(bn.NodeId)
            orderby c.SortKey, bn.SortKey
            select new { SortKey = bn.SortKey, b.Id, b.Title, b.EventSummary }
        ).ToListAsync(ct);

        // BeatEntityPresence has no EF entity mapping in this codebase — queried via raw SQL
        // everywhere it's used (see ProseWriterRouter's POV lookup). Mirror that here.
        var povRows = await db.Database.SqlQueryRaw<PovRow>(
            "SELECT BeatId, EntityName FROM BeatEntityPresence WHERE PresenceType = 'pov'")
            .ToListAsync(ct);
        var povByBeat = povRows.ToDictionary(p => p.BeatId, p => p.EntityName);

        var entries = rows.Select(r => new BeatEventListEntry(
            (int)r.SortKey, r.Title, povByBeat.GetValueOrDefault(r.Id), r.EventSummary)).ToList();

        return (nodeCode, node.Title, entries);
    }

    /// <summary>Writes the current DB state to the book's own publish folder (same
    /// &lt;universe export dir&gt;/&lt;Series…&gt;/&lt;Title&gt; layout as description.txt and
    /// {CODE}-dcm-viz.htm — see ExportPathResolver/SynopsisExportService.NodePublishDirAsync),
    /// not docs/nodes — this artifact is a reader-facing QA export, not book-bible/DCM
    /// material, so it belongs beside the manuscript exports it's meant to accompany.
    /// Read-only, no LLM call.</summary>
    public async Task<string> ExportTxtAsync(string slugOrCode, CancellationToken ct = default)
    {
        var (nodeCode, title, entries) = await GetEventListAsync(slugOrCode, ct);

        var sb = new StringBuilder();
        sb.AppendLine(title.ToUpperInvariant() + " — Event List");
        sb.AppendLine($"Generated {DateTime.UtcNow:yyyy-MM-dd} · {entries.Count} beats · node {nodeCode}");
        sb.AppendLine(new string('=', 70));
        sb.AppendLine();
        foreach (var e in entries)
        {
            var pov = PovFirstName(e.Pov);
            var label = $"[SK{e.SortKey,-6}{pov,-10}]";
            sb.AppendLine($"{label}  {e.EventSummary ?? "(not yet generated)"}");
        }

        var dir = await NodePublishDirAsync(slugOrCode, ct);
        var path = Path.Combine(dir, $"{nodeCode}-Events.txt");
        await GeneratedFileWriter.WriteReadOnlyAsync(path, sb.ToString(), ct);
        return path;
    }

    /// <summary>Same publish-folder resolution as SynopsisExportService.NodePublishDirAsync /
    /// DcmVizCli — &lt;universe export dir&gt;/&lt;Series…&gt;/&lt;Title&gt;, byte-for-byte the
    /// exporters' layout (description.txt, {CODE}-dcm-viz.htm, story-synopsis.txt all live
    /// here).</summary>
    private async Task<string> NodePublishDirAsync(string slugOrCode, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(
            n => n.Slug == slugOrCode || (n.NodeCode != null && n.NodeCode.ToUpper() == slugOrCode.ToUpper()), ct)
            ?? throw new InvalidOperationException($"Node not found: {slugOrCode}");
        var universeSlug = await db.Universes.AsNoTracking()
            .Where(u => u.Id == node.UniverseId).Select(u => u.Slug).FirstOrDefaultAsync(ct);
        var baseDir = settings.GetExportDirectory(universeSlug);
        var (nodeDir, _) = await ExportPathResolver.ResolveAsync(db, node, baseDir, ct);
        return nodeDir;
    }

    private static readonly HashSet<string> PovTitlePrefixes = new(StringComparer.OrdinalIgnoreCase)
        { "Dame", "Sir", "Lord", "Lady", "Canon", "Sergeant", "Sgt", "Sgt.", "Captain",
          "King", "Queen", "Knight", "Doctor", "Dr", "Dr.", "Warrior" };

    /// <summary>Display-only: the export's POV column shows the narrator's given name alone
    /// (e.g. "Lyra", not "Dame Lyra Ocipheus-Athen-Moor") — this never touches the underlying
    /// BeatEntityPresence.EntityName data, which keeps the full form for every other consumer.</summary>
    private static string PovFirstName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "";
        var tokens = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var idx = 0;
        if (tokens.Length > 1 && PovTitlePrefixes.Contains(tokens[0])) idx = 1;
        return tokens.Length > idx ? tokens[idx] : fullName;
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

    /// <summary>Head+tail clip — plot twists/reveals in this codebase's beats often land at
    /// the END, so a head-only clip (as MeaningBackfillService uses for purpose-extraction)
    /// would systematically miss them for event-extraction.</summary>
    private static string ClipForEvent(string text, int cap = PerBeatClipChars)
    {
        if (text.Length <= cap) return text;
        int head = cap / 4;
        int tail = cap - head;
        return text[..head] + "\n\n[...clipped for length...]\n\n" + text[^tail..];
    }

    /// <summary>Same defensive per-item try/catch shape as MeaningBackfillService.ParseMeaningBatch
    /// — one hallucinated/malformed "items" entry must not discard the whole batch.</summary>
    internal static List<(Guid BeatId, string Event)> ParseEventBatch(
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
                    var evt = el.TryGetProperty("event", out var m) ? m.GetString() : null;
                    if (string.IsNullOrWhiteSpace(evt)) continue;
                    if (!refMap.TryGetValue(refEl.GetInt32(), out var beatId)) continue;
                    results.Add((beatId, evt.Trim()));
                }
                catch
                {
                    // Skip just this malformed "items" entry — not the whole batch.
                }
            }
        }
        catch
        {
            // Malformed JSON entirely — return whatever (nothing) was parsed so far.
        }
        return results;
    }
}
