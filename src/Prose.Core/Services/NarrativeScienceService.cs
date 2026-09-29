using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

// ─────────────────────────────────────────────────────────────────────────────
// NarrativeScienceService
//
// Operationalizes Will Storr's "The Science of Storytelling" frameworks as
// LLM-backed analysis tools:
//
//   • CheckAntiheroEmpathyAsync   — scores the 4 antihero empathy levers
//
// AnalyzeSacredFlawAsync, CheckDramaticQuestionAsync and MapFiveActStructureAsync were
// cut in 1cb5a9489 ("Cut group A"); their result models went with them.
//
// AuditSceneEngagementAsync (6-point scene anatomy) was removed 2026-08-13 — its
// mechanisms (unexpected change, cause-effect, specificity, show-not-tell)
// substantially overlapped LogicSweepService's causality dimension, DELIGHT
// moves, and StoryScopeAuditService, none of which BookHealthService's own
// wiring ever ran it alongside (see the "Skipped" note that used to sit on
// DramaticQuestionAsync in BookHealthService.cs). It had no automated caller
// anywhere in the pipeline — only a manual CLI/MCP surface — so cutting it
// removes a real per-beat cost sink (1 LLM call/beat if ever run in bulk) with
// no loss of signal actually relied on. See docs/rfc/0009-cost-tiered-storytelling-engine.md.
// ─────────────────────────────────────────────────────────────────────────────

public class NarrativeScienceService(
    ILlmService llm,
    IDbContextFactory<ProseDbContext> dbFactory)
{
    // ── Antihero empathy ──────────────────────────────────────────────────────

    public async Task<AntiheroEmpathyResult> CheckAntiheroEmpathyAsync(
        Guid characterId, string beatText, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var c = await db.Characters.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == characterId, ct)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        var system = """
            You are a narrative-science analyst. Evaluate whether an antihero beat
            successfully activates the four EMPATHY LEVERS per Will Storr's framework.

            THE FOUR LEVERS:
            1. PRE-DEFLATION — A worse villain or more selfish character is visible in this
               beat or recent context, making the antihero look better by comparison.
            2. VULNERABILITY / PAIN — The beat shows the wound, fear, or cost beneath the
               antihero's surface. The reader sees where the damage came from.
            3. GENUINE VIRTUE — The antihero acts selflessly, protects someone, or shows
               genuine care — even briefly. Cash in enormous goodwill.
            4. ALTRUISTIC PUNISHMENT — The antihero punishes selfishness that the reader
               also wants punished. Reader and antihero want the same thing momentarily.

            OUTPUT FORMAT: JSON only, no prose wrapper.
            {
              "levers": {
                "pre_deflation":       { "active": true|false, "evidence": "..." },
                "vulnerability_pain":  { "active": true|false, "evidence": "..." },
                "genuine_virtue":      { "active": true|false, "evidence": "..." },
                "altruistic_punishment": { "active": true|false, "evidence": "..." }
              },
              "levers_active": 0,
              "empathy_score": 1-10,
              "diagnosis": "...",
              "improvement_hint": "..."
            }
            """;

        var user = $"""
            CHARACTER: {c.Name}
            Role: {c.Role}
            Description: {c.Description}

            BEAT TEXT:
            {beatText}
            """;

        var raw = await llm.GenerateAsync(system, user, temperature: 0.4, maxTokens: 700, ct: ct);
        // Throw rather than fabricate EmpathyScore=0 (2026-08-14 fix) — a real 0/10 antihero-
        // empathy verdict and an LLM parse failure were previously identical to any caller reading
        // empathy_score without also cross-checking Diagnosis for the literal string "(parse
        // error)". Same fix class as CheckDramaticQuestionAsync (2026-08-09).
        return ParseJson<AntiheroEmpathyResult>(raw)
            ?? throw new InvalidOperationException($"Could not parse antihero-empathy response: {raw[..Math.Min(200, raw.Length)]}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static T? ParseJson<T>(string raw)
    {
        try
        {
            var start = raw.IndexOf('{');
            var end = raw.LastIndexOf('}');
            if (start < 0 || end < start) return default;
            return JsonSerializer.Deserialize<T>(raw[start..(end + 1)], new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch { return default; }
    }
}

// ── Result models ─────────────────────────────────────────────────────────────

public class AntiheroEmpathyLever
{
    [JsonPropertyName("active")]   public bool   Active   { get; set; }
    [JsonPropertyName("evidence")] public string Evidence { get; set; } = "";
}

public class AntiheroEmpathyResult
{
    [JsonPropertyName("levers")]           public Dictionary<string, AntiheroEmpathyLever> Levers { get; set; } = new();
    [JsonPropertyName("levers_active")]    public int    LeversActive     { get; set; }
    [JsonPropertyName("empathy_score")]    public int    EmpathyScore     { get; set; }
    [JsonPropertyName("diagnosis")]        public string Diagnosis        { get; set; } = "";
    [JsonPropertyName("improvement_hint")] public string ImprovementHint { get; set; } = "";
    [JsonIgnore]                           public string? RawResponse     { get; set; }
}
