using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// Rewrites a character's <c>image_prompt</c> (Midjourney) and
/// <c>dalle3_prompt</c> so the ethnicity-keyed visual descriptors — skin
/// tone, hair color/texture, eye color, facial bone structure — agree with
/// the character's current <c>genetic_ancestry</c> breakdown. Everything
/// else (clothing, posture, expression, accessories, weapons, scene,
/// lighting, camera, mood) is preserved verbatim.
///
/// <para><b>Cost-aware.</b> A SHA-256 hash of the genetic_ancestry that the
/// prompts were last keyed against is stored inline as the JSON property
/// <c>_image_prompts_genetics_hash</c>. The bulk mode skips characters whose
/// hash already matches the current ancestry, so re-runs after the genetics
/// walker only re-prompt the characters whose ancestry actually shifted.</para>
///
/// <para><b>LLM call.</b> Routed via <see cref="LlmRouter"/> so the active
/// provider (Claude / OpenAI) handles it. Single call per character returns
/// JSON with the two rewritten prompts. We parse, validate, persist.</para>
/// </summary>
public class ImagePromptRegenService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly LlmRouter                                  llm;
    private readonly ILogger<ImagePromptRegenService>           log;

    private const string HashProperty = "_image_prompts_genetics_hash";

    public ImagePromptRegenService(
        IDbContextFactory<ProseDbContext> dbFactory,
        LlmRouter                                  llm,
        ILogger<ImagePromptRegenService>           log)
    {
        this.dbFactory = dbFactory;
        this.llm       = llm;
        this.log       = log;
    }

    public sealed record RegenResult(bool Updated, string? Reason);
    public sealed record BulkReport(int Scanned, int Skipped, int Regenerated, int Failed);
    public sealed record BackfillReport(int Scanned, int Stamped, int AlreadyHashed, int NoAncestry);

    /// <summary>
    /// Stamp the current-ancestry hash on every character that doesn't have
    /// one. Lets the cost-aware bulk regen mode correctly skip characters
    /// whose prompts are already keyed against their current ancestry.
    /// Does NOT call the LLM — pure hash compute + Records.Json patch.
    /// </summary>
    public async Task<BackfillReport> BackfillHashesAsync(IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ids = await db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "character" && e.IsActive)
            .Select(e => e.Id)
            .ToListAsync(ct);

        int scanned = 0, stamped = 0, alreadyHashed = 0, noAncestry = 0;
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            scanned++;
            var rec = await db.Records.FirstOrDefaultAsync(r => r.EntityId == id, ct);
            if (rec == null) continue;
            using var doc = JsonDocument.Parse(rec.Json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("genetic_ancestry", out var ga) || ga.ValueKind != JsonValueKind.Object)
            {
                noAncestry++;
                progress?.Report((scanned, ids.Count));
                continue;
            }
            var hash = HashAncestry(ga);
            if (root.TryGetProperty(HashProperty, out var prev)
             && prev.ValueKind == JsonValueKind.String
             && prev.GetString() == hash)
            {
                alreadyHashed++;
                progress?.Report((scanned, ids.Count));
                continue;
            }
            var rewritten = StampHashOnly(rec.Json, hash);
            if (rewritten == null)
            {
                progress?.Report((scanned, ids.Count));
                continue;
            }
            rec.Json      = rewritten;
            rec.UpdatedAt = DateTime.UtcNow;
            stamped++;
            progress?.Report((scanned, ids.Count));
        }
        if (stamped > 0) await db.SaveChangesAsync(ct);
        log.LogInformation("BackfillHashes: scanned={S} stamped={St} already={A} no-ancestry={N}",
            scanned, stamped, alreadyHashed, noAncestry);
        return new BackfillReport(scanned, stamped, alreadyHashed, noAncestry);
    }

    private static string? StampHashOnly(string json, string ancestryHash)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            using var ms  = new MemoryStream();
            using (var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false }))
            {
                w.WriteStartObject();
                bool wroteHash = false;
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.NameEquals(HashProperty)) { w.WriteString(HashProperty, ancestryHash); wroteHash = true; }
                    else                                { prop.WriteTo(w); }
                }
                if (!wroteHash) w.WriteString(HashProperty, ancestryHash);
                w.WriteEndObject();
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }
        catch { return null; }
    }

    /// <summary>
    /// Regenerate one character's image prompts. Pass <paramref name="force"/>
    /// to bypass the hash check (useful for re-runs after the prompt template
    /// itself changes, not just the ancestry).
    /// </summary>
    public async Task<RegenResult> RegenForCharacterAsync(Guid characterId,
        bool force = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rec = await db.Records.FirstOrDefaultAsync(r => r.EntityId == characterId, ct);
        if (rec == null) return new RegenResult(false, "no Records row");

        using var doc = JsonDocument.Parse(rec.Json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("genetic_ancestry", out var ga) || ga.ValueKind != JsonValueKind.Object)
            return new RegenResult(false, "no genetic_ancestry");

        var ancestryHash = HashAncestry(ga);
        if (!force && root.TryGetProperty(HashProperty, out var prev)
                  && prev.ValueKind == JsonValueKind.String
                  && prev.GetString() == ancestryHash)
            return new RegenResult(false, "hash matches — already current");

        var name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "(unnamed)" : "(unnamed)";
        var midjourney = root.TryGetProperty("image_prompt",  out var mj) ? mj.GetString() ?? "" : "";
        var dalle3     = root.TryGetProperty("dalle3_prompt", out var d3) ? d3.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(midjourney) && string.IsNullOrWhiteSpace(dalle3))
            return new RegenResult(false, "no existing prompts to rewrite");

        var ancestryReadable = string.Join(", ", ga.EnumerateObject()
            .Where(p => p.Value.ValueKind == JsonValueKind.Number)
            .OrderByDescending(p => p.Value.GetDouble())
            .Select(p => $"{p.Name} {p.Value.GetDouble():F1}%"));

        var system = """
            You are a casting director updating a character's image prompts so visual descriptors match a new genetic ancestry breakdown.

            CHANGE ONLY:
              - Skin tone
              - Hair color, texture, hairline pattern (where ethnically marked)
              - Eye color (where the existing prompt names it)
              - Facial bone structure / jawline / cheekbones / nose shape (where ancestry-specific)

            PRESERVE EXACTLY (do not paraphrase, reorder, or alter):
              - Clothing, jewelry, accessories
              - Posture, expression, mood, demeanor
              - Weapons, gear, props
              - Scene, environment, lighting, weather, camera, lens, framing, render parameters
              - Age, build, height, scars, augmentations, cybernetics
              - Any explicit Midjourney parameters like --ar, --v

            Return STRICTLY valid JSON in this shape, nothing else, no markdown fences, no preamble:
              {"midjourney_prompt": "...", "dalle3_prompt": "..."}
            """;

        var user = $"""
            CHARACTER: {name}

            NEW GENETIC ANCESTRY: {ancestryReadable}

            CURRENT MIDJOURNEY PROMPT:
            {midjourney}

            CURRENT DALLE3 PROMPT:
            {dalle3}
            """;

        string raw;
        try
        {
            raw = await llm.GenerateAsync(system, user, temperature: 0.4, maxTokens: 4096, ct: ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Image-prompt regen LLM call failed for {Char}", characterId);
            return new RegenResult(false, $"LLM error: {ex.Message}");
        }

        if (!TryParseRewritten(raw, out var newMidjourney, out var newDalle3))
            return new RegenResult(false, "LLM returned non-JSON or missing fields");

        // Rewrite Records.Json: replace image_prompt + dalle3_prompt + stamp hash
        var rewritten = ReplacePromptFields(rec.Json, newMidjourney, newDalle3, ancestryHash);
        if (rewritten == null) return new RegenResult(false, "rewriter could not patch JSON");

        rec.Json      = rewritten;
        rec.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        log.LogInformation("Regenerated image prompts for {Char} ({Name})", characterId, name);
        return new RegenResult(true, null);
    }

    /// <summary>
    /// Sweep every active character. Skip those whose stored hash matches
    /// their current genetic_ancestry. The cost-aware wrapper around
    /// <see cref="RegenForCharacterAsync"/> for after a genetics walker run.
    /// </summary>
    public async Task<BulkReport> RegenAllChangedAsync(IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var ids = await db.Entities.AsNoTracking()
            .Where(e => e.EntityType == "character" && e.IsActive)
            .Select(e => e.Id)
            .ToListAsync(ct);

        int scanned = 0, skipped = 0, regen = 0, failed = 0;
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
            scanned++;
            var result = await RegenForCharacterAsync(id, force: false, ct);
            if (result.Updated) regen++;
            else if (result.Reason == "hash matches — already current"
                  || result.Reason == "no existing prompts to rewrite"
                  || result.Reason == "no genetic_ancestry") skipped++;
            else failed++;
            progress?.Report((scanned, ids.Count));
        }
        return new BulkReport(scanned, skipped, regen, failed);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private static string HashAncestry(JsonElement ancestry)
    {
        // Stable serialization: sort keys, fixed precision. Two ancestries
        // with the same content always hash the same regardless of write
        // order or rounding noise within tolerance.
        var entries = ancestry.EnumerateObject()
            .Where(p => p.Value.ValueKind == JsonValueKind.Number)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(p => $"{p.Name}:{Math.Round(p.Value.GetDouble(), 1)}");
        var canonical = string.Join("|", entries);
        var bytes     = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Parse the LLM response. Tolerates leading/trailing whitespace and a
    /// stray fenced code block, but rejects anything not yielding the two
    /// expected string fields.
    /// </summary>
    internal static bool TryParseRewritten(string raw, out string midjourney, out string dalle3)
    {
        midjourney = ""; dalle3 = "";
        var trimmed = raw.Trim();
        // Strip fenced code blocks if the model added any
        if (trimmed.StartsWith("```"))
        {
            var firstNL = trimmed.IndexOf('\n');
            if (firstNL >= 0) trimmed = trimmed.Substring(firstNL + 1);
            if (trimmed.EndsWith("```")) trimmed = trimmed[..^3];
            trimmed = trimmed.Trim();
        }
        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (!doc.RootElement.TryGetProperty("midjourney_prompt", out var mj)
             || !doc.RootElement.TryGetProperty("dalle3_prompt",     out var d3)) return false;
            midjourney = mj.GetString() ?? "";
            dalle3     = d3.GetString() ?? "";
            return !string.IsNullOrWhiteSpace(midjourney) && !string.IsNullOrWhiteSpace(dalle3);
        }
        catch { return false; }
    }

    /// <summary>
    /// Patch <c>image_prompt</c>, <c>dalle3_prompt</c>, and the inline hash
    /// marker on the Records.Json blob without disturbing other properties.
    /// </summary>
    private static string? ReplacePromptFields(string json, string midjourney, string dalle3, string ancestryHash)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            using var ms  = new MemoryStream();
            using (var w = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = false }))
            {
                w.WriteStartObject();
                bool wroteHash = false;
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if      (prop.NameEquals("image_prompt"))   { w.WriteString("image_prompt",   midjourney); }
                    else if (prop.NameEquals("dalle3_prompt"))  { w.WriteString("dalle3_prompt",  dalle3);     }
                    else if (prop.NameEquals(HashProperty))     { w.WriteString(HashProperty,     ancestryHash); wroteHash = true; }
                    else                                        { prop.WriteTo(w); }
                }
                if (!wroteHash) w.WriteString(HashProperty, ancestryHash);
                w.WriteEndObject();
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }
        catch { return null; }
    }
}
