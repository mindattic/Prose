using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

// ── Emotional Ledger Service ───────────────────────────────────────────────────
//
// Parses Want / Need / Wound / Flaw / VoiceRegister for each named character
// from the strand's StrandBible field, caches the result per (StrandId, Character),
// and cache-busts when the bible content changes (SourceBibleHash).
//
// Fallback: when no bible is present, or a character is not mentioned in the bible,
// a single LLM extraction call infers the fields from the assembled prose, flagged
// Inferred=true so callers can treat it with appropriate skepticism.
//
// Usage:
//   ss --examine-emotion (via EmotionalDepthService)
//   MCP: examine_emotional_depth (via EmotionalDepthService)

/// <summary>
/// Per-character emotional profile (Want/Need/Wound/Flaw) extracted from the strand
/// bible or inferred from prose. Injected into dimension prompts as character context.
/// </summary>
public record CharacterLedgerEntry(
    string Character,
    string Want,
    string Need,
    string Wound,
    string Flaw,
    string VoiceRegister,
    bool Inferred);

/// <summary>
/// Parses and caches Want/Need/Wound/Flaw from strand bibles. Cache-busted on bible
/// content hash. Falls back to LLM prose inference when no bible is available.
/// </summary>
public class EmotionalLedgerService
{
    private readonly ILlmService llm;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<EmotionalLedgerService> log;

    public EmotionalLedgerService(
        ILlmService llm,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<EmotionalLedgerService> log)
    {
        this.llm       = llm;
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    // ── Public entry points ───────────────────────────────────────────────────

    /// <summary>
    /// Returns the current ledger for all characters in the strand, refreshing
    /// stale cache entries. Force=true always re-extracts even if the hash matches.
    /// </summary>
    public async Task<IReadOnlyList<CharacterLedgerEntry>> GetLedgerAsync(
        Guid strandId, string? bible, string assembledText,
        bool force = false, CancellationToken ct = default)
    {
        var bibleHash = bible is { Length: > 0 } b ? Hash(b) : null;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var cached = await db.CharacterEmotionalLedgers.AsNoTracking()
            .Where(x => x.StrandId == strandId)
            .ToListAsync(ct);

        // If bible changed or forced, clear and re-extract
        bool stale = force
            || cached.Count == 0
            || cached.Any(c => c.SourceBibleHash != bibleHash);

        if (!stale)
            return cached.Select(ToEntry).ToList();

        // Delete stale entries
        var toDelete = await db.CharacterEmotionalLedgers
            .Where(x => x.StrandId == strandId)
            .ToListAsync(ct);
        db.CharacterEmotionalLedgers.RemoveRange(toDelete);

        // Extract from bible if available, otherwise from prose
        var entries = bible is { Length: > 10 }
            ? await ExtractFromBibleAsync(strandId, bible, bibleHash!, ct)
            : await InferFromProseAsync(strandId, assembledText, ct);

        db.CharacterEmotionalLedgers.AddRange(entries);
        await db.SaveChangesAsync(ct);

        return entries.Select(ToEntry).ToList();
    }

    // ── Extraction from bible ─────────────────────────────────────────────────

    private async Task<List<CharacterEmotionalLedger>> ExtractFromBibleAsync(
        Guid strandId, string bible, string bibleHash, CancellationToken ct)
    {
        const string system =
            "You are a story analyst. Extract character emotional profiles from a strand bible. " +
            "Return ONLY the JSON array requested. No prose, no markdown fences, no explanation.";

        var bibleText = Truncate(bible, 8000);
        var prompt = $$"""
You are reading a strand bible. Identify every named major character and extract their:
- want: the on-page goal (what they consciously pursue)
- need: the arc-level gap they must grow into (what they actually need)
- wound: the past event or damage that shapes their flaw
- flaw: the behaviour pattern or belief that blocks their need
- voice_register: their emotional/tonal default (e.g. "dry sardonic", "warm direct", "guarded formal")

If a field is genuinely not stated in the bible, use "" (empty string). Do NOT invent.

Return ONLY a JSON array of objects:
[{"character":"<name>","want":"...","need":"...","wound":"...","flaw":"...","voice_register":"..."}]

STRAND BIBLE:
{{bibleText}}
""";

        try
        {
            var raw  = await llm.GenerateAsync(system, prompt, 0.1, 800, null, ct);
            var json = ExtractJsonArray(raw);
            using var doc = JsonDocument.Parse(json);

            var results = new List<CharacterEmotionalLedger>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                var name = el.TryGetProperty("character", out var np) ? np.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(name)) continue;

                results.Add(new CharacterEmotionalLedger
                {
                    Id              = Guid.NewGuid(),
                    StrandId        = strandId,
                    Character       = name,
                    Want            = el.TryGetProperty("want",            out var w) ? w.GetString() : null,
                    Need            = el.TryGetProperty("need",            out var n) ? n.GetString() : null,
                    Wound           = el.TryGetProperty("wound",           out var wo) ? wo.GetString() : null,
                    Flaw            = el.TryGetProperty("flaw",            out var f) ? f.GetString() : null,
                    VoiceRegister   = el.TryGetProperty("voice_register",  out var vr) ? vr.GetString() : null,
                    Inferred        = false,
                    SourceBibleHash = bibleHash,
                    UpdatedAt       = DateTime.UtcNow,
                });
            }
            return results;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Bible extraction failed for strand {StrandId}; falling back to prose inference", strandId);
            return await InferFromProseAsync(strandId, "", ct);
        }
    }

    // ── Inference from prose ──────────────────────────────────────────────────

    private async Task<List<CharacterEmotionalLedger>> InferFromProseAsync(
        Guid strandId, string prose, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(prose))
            return new List<CharacterEmotionalLedger>();

        const string system =
            "You are a story analyst. Infer character emotional profiles from prose. " +
            "Return ONLY the JSON array requested. No prose, no markdown fences, no explanation.";

        var proseText = Truncate(prose, 6000);
        var prompt = $$"""
Read this prose passage and identify up to 4 major named characters. For each, infer their
most likely want, need, wound, flaw, and voice register from behavioural evidence in the text.
Mark all entries as inferred (they are not from a bible).

Return ONLY a JSON array:
[{"character":"<name>","want":"...","need":"...","wound":"...","flaw":"...","voice_register":"..."}]

PROSE:
{{proseText}}
""";

        try
        {
            var raw  = await llm.GenerateAsync(system, prompt, 0.1, 600, null, ct);
            var json = ExtractJsonArray(raw);
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.EnumerateArray()
                .Select(el =>
                {
                    var name = el.TryGetProperty("character", out var np) ? np.GetString() ?? "" : "";
                    return new CharacterEmotionalLedger
                    {
                        Id            = Guid.NewGuid(),
                        StrandId      = strandId,
                        Character     = name,
                        Want          = el.TryGetProperty("want",           out var w)  ? w.GetString()  : null,
                        Need          = el.TryGetProperty("need",           out var n)  ? n.GetString()  : null,
                        Wound         = el.TryGetProperty("wound",          out var wo) ? wo.GetString() : null,
                        Flaw          = el.TryGetProperty("flaw",           out var f)  ? f.GetString()  : null,
                        VoiceRegister = el.TryGetProperty("voice_register", out var vr) ? vr.GetString() : null,
                        Inferred      = true,
                        UpdatedAt     = DateTime.UtcNow,
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Character))
                .ToList();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Prose inference failed for strand {StrandId}", strandId);
            return new List<CharacterEmotionalLedger>();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CharacterLedgerEntry ToEntry(CharacterEmotionalLedger e) =>
        new(e.Character, e.Want ?? "", e.Need ?? "", e.Wound ?? "", e.Flaw ?? "",
            e.VoiceRegister ?? "", e.Inferred);

    private static string Hash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "\n[truncated]";

    private static string ExtractJsonArray(string raw)
    {
        var start = raw.IndexOf('[');
        var end   = raw.LastIndexOf(']');
        return start >= 0 && end > start ? raw[start..(end + 1)] : "[]";
    }
}
