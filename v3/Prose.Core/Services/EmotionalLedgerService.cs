using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

// ── Emotional Ledger Service ───────────────────────────────────────────────────
//
// Parses Want / Need / Wound / Flaw / VoiceRegister for each named character
// from the node's NodeOutline field, caches the result per (NodeId, Character),
// and cache-busts when the bible content changes (SourceOutlineHash).
//
// Fallback: when no bible is present, or a character is not mentioned in the bible,
// a single LLM extraction call infers the fields from the assembled prose, flagged
// Inferred=true so callers can treat it with appropriate skepticism.
//
// Usage:
//   prose --examine-emotion (via EmotionalDepthService)
//   MCP: examine_emotional_depth (via EmotionalDepthService)

/// <summary>
/// Per-character emotional profile (Want/Need/Wound/Flaw) extracted from the node
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
/// Parses and caches Want/Need/Wound/Flaw from node bibles. Cache-busted on bible
/// content hash. Falls back to LLM prose inference when no bible is available.
/// </summary>
public class EmotionalLedgerService
{
    private readonly ILlmService llm;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<EmotionalLedgerService> log;

    public EmotionalLedgerService(
        ILlmService llm,
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<EmotionalLedgerService> log)
    {
        this.llm       = llm;
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    // ── Public entry points ───────────────────────────────────────────────────

    /// <summary>
    /// Returns the current ledger for all characters in the node, refreshing
    /// stale cache entries. Force=true always re-extracts even if the hash matches.
    /// </summary>
    public async Task<IReadOnlyList<CharacterLedgerEntry>> GetLedgerAsync(
        Guid nodeId, string? bible, string assembledText,
        bool force = false, CancellationToken ct = default)
    {
        var bibleHash = bible is { Length: > 0 } b ? Hash(b) : null;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var cached = await db.CharacterEmotionalLedgers.AsNoTracking()
            .Where(x => x.NodeId == nodeId)
            .ToListAsync(ct);

        // If bible changed or forced, clear and re-extract
        bool stale = force
            || cached.Count == 0
            || cached.Any(c => c.SourceOutlineHash != bibleHash);

        if (!stale)
            return cached.Select(ToEntry).ToList();

        // Delete stale entries
        var toDelete = await db.CharacterEmotionalLedgers
            .Where(x => x.NodeId == nodeId)
            .ToListAsync(ct);
        db.CharacterEmotionalLedgers.RemoveRange(toDelete);

        // Extract from bible if available, otherwise from prose
        var entries = bible is { Length: > 10 }
            ? await ExtractFromOutlineAsync(nodeId, bible, bibleHash!, ct)
            : await InferFromProseAsync(nodeId, assembledText, ct);

        db.CharacterEmotionalLedgers.AddRange(entries);
        await db.SaveChangesAsync(ct);

        return entries.Select(ToEntry).ToList();
    }

    // ── Extraction from bible ─────────────────────────────────────────────────

    private async Task<List<CharacterEmotionalLedger>> ExtractFromOutlineAsync(
        Guid nodeId, string bible, string bibleHash, CancellationToken ct)
    {
        const string system =
            "You are a story analyst. Extract character emotional profiles from a node bible. " +
            "Return ONLY the JSON array requested. No prose, no markdown fences, no explanation.";

        var bibleText = Truncate(bible, 8000);
        var prompt = $$"""
You are reading a node bible. Identify every named major character and extract their:
- want: the on-page goal (what they consciously pursue)
- need: the arc-level gap they must grow into (what they actually need)
- wound: the past event or damage that shapes their flaw
- flaw: the behaviour pattern or belief that blocks their need
- voice_register: their emotional/tonal default (e.g. "dry sardonic", "warm direct", "guarded formal")

If a field is genuinely not stated in the bible, use "" (empty string). Do NOT invent.

Return ONLY a JSON array of objects:
[{"character":"<name>","want":"...","need":"...","wound":"...","flaw":"...","voice_register":"..."}]

NODE BIBLE:
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
                    NodeId        = nodeId,
                    Character       = name,
                    Want            = el.TryGetProperty("want",            out var w) ? w.GetString() : null,
                    Need            = el.TryGetProperty("need",            out var n) ? n.GetString() : null,
                    Wound           = el.TryGetProperty("wound",           out var wo) ? wo.GetString() : null,
                    Flaw            = el.TryGetProperty("flaw",            out var f) ? f.GetString() : null,
                    VoiceRegister   = el.TryGetProperty("voice_register",  out var vr) ? vr.GetString() : null,
                    Inferred        = false,
                    SourceOutlineHash = bibleHash,
                    UpdatedAt       = DateTime.UtcNow,
                });
            }
            return results;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Bible extraction failed for node {NodeId}; falling back to prose inference", nodeId);
            return await InferFromProseAsync(nodeId, "", ct);
        }
    }

    // ── Inference from prose ──────────────────────────────────────────────────

    private async Task<List<CharacterEmotionalLedger>> InferFromProseAsync(
        Guid nodeId, string prose, CancellationToken ct)
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
                        NodeId      = nodeId,
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
            log.LogWarning(ex, "Prose inference failed for node {NodeId}", nodeId);
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
