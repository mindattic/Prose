using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Reads chapter prose and emits <see cref="EntityStateEvent"/> rows describing
/// every concrete state change the prose narrates: a character moves to a new
/// place, fires a shotgun shell, picks up an item, joins or leaves another
/// character, decides on a plan. Each event is timestamped to the beat's
/// <see cref="ChapterBeat.InWorldDate"/> when set, falling back to the
/// chapter's <see cref="Chapter.InWorldDate"/> or the global story-now cursor.
///
/// Wired to <see cref="IChapterRepository.OnChapterSaved"/> — the moment the
/// writer hits Save, the ledger updates. The same path is also driven by the
/// CLI (<c>ss --repair --extract-state</c>) for one-shot historical backfill.
/// </summary>
public class BeatStateExtractor
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly WorldStateLedger ledger;
    private readonly WorldClockService clock;
    private readonly ILlmService llm;
    private readonly ILogger<BeatStateExtractor> log;

    public BeatStateExtractor(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        WorldStateLedger ledger,
        WorldClockService clock,
        ILlmService llm,
        IChapterRepository chapters,
        ILogger<BeatStateExtractor> log)
    {
        this.dbFactory = dbFactory;
        this.ledger    = ledger;
        this.clock     = clock;
        this.llm       = llm;
        this.log       = log;

        // Subscribe to every chapter save so the ledger stays current.
        chapters.OnChapterSaved += OnChapterSaved;
    }

    /// <summary>
    /// Set to false in test environments to avoid spamming an LLM on every
    /// SaveChapter; the explicit ExtractAsync API still runs the extraction.
    /// </summary>
    public bool AutoOnChapterSaved { get; set; } = true;

    private void OnChapterSaved(Models.Chapter chapter)
    {
        if (!AutoOnChapterSaved) return;
        // Fire-and-forget — keep SaveChapter snappy. Errors are swallowed and
        // logged so the writer's save flow never blocks on extraction.
        _ = Task.Run(async () =>
        {
            try { await ExtractAsync(chapter); }
            catch (Exception ex) { log.LogWarning(ex, "Beat state extraction failed for chapter {Id}", chapter.Id); }
        });
    }

    public sealed class ExtractionResult
    {
        public int BeatsScanned { get; set; }
        public int EventsRecorded { get; set; }
        public int Skipped       { get; set; }
        public List<string> Errors { get; } = new();
    }

    /// <summary>
    /// Run the extractor on one chapter. Walks each beat (in order), prompts
    /// the LLM for a strict-JSON event list, resolves entity names to canon
    /// guids, and records via <see cref="WorldStateLedger.RecordManyAsync"/>.
    /// </summary>
    public async Task<ExtractionResult> ExtractAsync(Models.Chapter chapter, CancellationToken ct = default)
    {
        var result = new ExtractionResult();
        if (chapter == null || chapter.Beats == null || chapter.Beats.Count == 0)
            return result;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var chapterGuid = ParseGuid(chapter.Id);
        var chapterRow = await db.Chapters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == chapterGuid, ct);
        var chapterDate = chapterRow?.InWorldDate;

        // Cache entity-name → Guid lookups so each beat doesn't re-query for
        // the same characters.
        var nameLookup = new Dictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);

        foreach (var beat in chapter.Beats.OrderBy(b => b.Index))
        {
            if (ct.IsCancellationRequested) break;
            result.BeatsScanned++;
            try
            {
                var beatGuid = ParseGuid(beat.Id);
                var beatRow = await db.ChapterBeats.AsNoTracking().FirstOrDefaultAsync(b => b.BeatGuid == beatGuid, ct);
                var beatDate = beatRow?.InWorldDate ?? chapterDate ?? clock.GetNow();

                var raw = await CallExtractionLlmAsync(chapter, beat, ct);
                var parsed = ParseEventArray(raw);
                if (parsed.Count == 0) { result.Skipped++; continue; }

                var rows = new List<EntityStateEvent>(parsed.Count);
                foreach (var p in parsed)
                {
                    var id = await ResolveEntityIdAsync(db, p.EntityName, nameLookup, ct);
                    if (id == null) continue;
                    rows.Add(new EntityStateEvent
                    {
                        EntityId    = id.Value,
                        AspectKey   = Truncate(p.Aspect, 200),
                        Verb        = NormalizeVerb(p.Verb),
                        OldValue    = p.OldValue,
                        NewValue    = p.NewValue,
                        Delta       = p.Delta,
                        AtStoryTime = beatDate,
                        ChapterId   = chapterGuid,
                        BeatGuid    = beatGuid,
                        Source      = "extracted:beat",
                        Confidence  = MapConfidence(p.Confidence),
                        Snippet     = TruncateNullable(p.Snippet, 500),
                    });
                }
                if (rows.Count > 0)
                {
                    await ledger.RecordManyAsync(rows, ct);
                    result.EventsRecorded += rows.Count;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"beat {beat.Index} ({beat.Title}): {ex.Message}");
                log.LogWarning(ex, "Extraction failed for beat {Index} of chapter {ChId}", beat.Index, chapter.Id);
            }
        }
        return result;
    }

    // ── LLM prompt + parsing ───────────────────────────────────────────────────

    private async Task<string> CallExtractionLlmAsync(Models.Chapter chapter, Models.ChapterBeat beat, CancellationToken ct)
    {
        var system =
            "You extract concrete world-state changes from a single beat of fiction. Read the prose. " +
            "For every CONCRETE physical or possessed-state change a named character undergoes — " +
            "moved to a place, fired a round, gained/lost an item, joined/left someone, formed an intent, " +
            "took a wound — emit one JSON object. Skip emotion, atmosphere, internal speculation. " +
            "Output ONLY a JSON array (possibly empty) on the FINAL line. Each object has these fields: " +
            "{\"entity\": \"<exact character name>\", \"aspect\": \"<dotted key, see examples>\", " +
            "\"verb\": \"set|inc|dec|enter|leave|add|remove\", " +
            "\"old\": \"<prior value, or null>\", \"new\": \"<resulting value>\", \"delta\": <number or null>, " +
            "\"snippet\": \"<≤200-char exact quote that supports it>\", \"confidence\": \"low|medium|high\"}. " +
            "Aspect examples: \"location\", \"ammo:chorus.shells\", \"inventory.bracelet\", " +
            "\"companion.with\", \"intent\", \"condition.shoulder.severity\". " +
            "Use snake_case slugs in aspect keys. Quote names verbatim from the prose.";

        var sb = new StringBuilder();
        sb.AppendLine($"CHAPTER: {chapter.Title}  (Ch {chapter.Number?.ToString() ?? "?"})");
        sb.AppendLine($"BEAT {beat.Index}: {beat.Title}");
        if (!string.IsNullOrWhiteSpace(beat.Synopsis)) sb.AppendLine($"SYNOPSIS: {beat.Synopsis}");
        sb.AppendLine("PROSE:");
        sb.AppendLine(Truncate(beat.Text ?? "", 4000));

        try
        {
            return await llm.GenerateAsync(system, sb.ToString(),
                temperature: 0.1, maxTokens: 1500, ct: ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "LLM call failed for beat {Index}", beat.Index);
            return "[]";
        }
    }

    private sealed record ParsedEvent(
        string EntityName, string Aspect, string Verb,
        string? OldValue, string? NewValue, double? Delta,
        string? Snippet, string? Confidence);

    private static List<ParsedEvent> ParseEventArray(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new();
        var start = raw.IndexOf('[');
        var end = raw.LastIndexOf(']');
        if (start < 0 || end <= start) return new();
        var slice = raw[start..(end + 1)];

        try
        {
            using var doc = JsonDocument.Parse(slice);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return new();

            var list = new List<ParsedEvent>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                var entity = StringOrNull(el, "entity");
                var aspect = StringOrNull(el, "aspect");
                var verb   = StringOrNull(el, "verb");
                if (string.IsNullOrWhiteSpace(entity)
                    || string.IsNullOrWhiteSpace(aspect)
                    || string.IsNullOrWhiteSpace(verb)) continue;

                list.Add(new ParsedEvent(
                    EntityName: entity!,
                    Aspect:     aspect!,
                    Verb:       verb!,
                    OldValue:   StringOrNull(el, "old"),
                    NewValue:   StringOrNull(el, "new"),
                    Delta:      el.TryGetProperty("delta", out var d) && d.ValueKind == JsonValueKind.Number
                                ? d.GetDouble() : (double?)null,
                    Snippet:    StringOrNull(el, "snippet"),
                    Confidence: StringOrNull(el, "confidence")));
            }
            return list;
        }
        catch { return new(); }
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static async Task<Guid?> ResolveEntityIdAsync(StreetSamuraiDbContext db, string name,
        Dictionary<string, Guid?> cache, CancellationToken ct)
    {
        if (cache.TryGetValue(name, out var hit)) return hit;
        var slug = WorldGraphService.Slugify(name);
        var id = await db.Entities.AsNoTracking()
            .Where(e => e.IsActive && (e.Name == name || e.Slug == slug))
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(ct);
        cache[name] = id;
        return id;
    }

    private static string NormalizeVerb(string verb) => verb.Trim().ToLowerInvariant() switch
    {
        "set" or "inc" or "dec" or "enter" or "leave" or "add" or "remove" => verb.Trim().ToLowerInvariant(),
        "increase" or "gain"   => "inc",
        "decrease" or "lose"   => "dec",
        "join"                 => "enter",
        "depart"               => "leave",
        _                      => "set",
    };

    private static double? MapConfidence(string? c) => (c ?? "").Trim().ToLowerInvariant() switch
    {
        "high"   => 0.9,
        "medium" => 0.6,
        "low"    => 0.3,
        _        => null,
    };

    private static string? StringOrNull(JsonElement obj, string key) =>
        obj.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString() : null;

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n];
    private static string? TruncateNullable(string? s, int n) =>
        string.IsNullOrEmpty(s) ? s : (s.Length <= n ? s : s[..n]);

    private static Guid ParseGuid(string s)
    {
        if (Guid.TryParse(s, out var g)) return g;
        if (s.Length == 32 && Guid.TryParseExact(s, "N", out g)) return g;
        var hash = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(s));
        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);
        return new Guid(bytes);
    }
}
