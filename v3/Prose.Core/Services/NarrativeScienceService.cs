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
//   • AnalyzeSacredFlawAsync      — character's theory of control, origin damage,
//                                   secret dread, hero-maker narrative
//   • CheckDramaticQuestionAsync  — scores whether a beat poses/answers "who is
//                                   this person really?" at surface + subconscious
//   • MapFiveActStructureAsync    — maps a node's beats to Storr's 5-act arc
//   • CheckAntiheroEmpathyAsync   — scores the 4 antihero empathy levers
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
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── Sacred Flaw ───────────────────────────────────────────────────────────

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

    static string BuildPsychologyBlob(Data.Entities.Character c)
    {
        var sb = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(c.PsychologySecret))
            sb.AppendLine($"Secret: {c.PsychologySecret}");
        var buckets = new[] { "core_fears", "core_desires", "coping_mechanisms", "blind_spots" };
        foreach (var b in buckets)
        {
            var items = c.PsychologyTraits.Where(t => t.Bucket == b).OrderBy(t => t.Position)
                .Select(t => t.Trait).ToList();
            if (items.Count > 0)
                sb.AppendLine($"{b.Replace('_', ' ')}: {string.Join("; ", items)}");
        }
        foreach (var b in new[] { "decision_rules", "breaking_points", "contradictions" })
        {
            var items = c.BehavioralRules.Where(r => r.Bucket == b).OrderBy(r => r.Position)
                .Select(r => r.Rule).ToList();
            if (items.Count > 0)
                sb.AppendLine($"{b.Replace('_', ' ')}: {string.Join("; ", items)}");
        }
        return sb.ToString();
    }

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

public class SacredFlawAnalysis
{
    [JsonPropertyName("theory_of_control")]   public string TheoryOfControl    { get; set; } = "";
    [JsonPropertyName("origin_damage")]       public string OriginDamage       { get; set; } = "";
    [JsonPropertyName("secret_dread")]        public string SecretDread        { get; set; } = "";
    [JsonPropertyName("hero_maker_narrative")]public string HeroMakerNarrative  { get; set; } = "";
    [JsonPropertyName("material_gains")]      public string MaterialGains       { get; set; } = "";
    [JsonPropertyName("confidence")]          public string Confidence          { get; set; } = "";
    [JsonPropertyName("diagnosis")]           public string Diagnosis           { get; set; } = "";
    [JsonIgnore]                              public string? RawResponse        { get; set; }
}

public class DramaticQuestionResult
{
    [JsonPropertyName("surface_score")]          public int    SurfaceScore         { get; set; }
    [JsonPropertyName("subconscious_score")]     public int    SubconsciousScore    { get; set; }
    [JsonPropertyName("overall_score")]          public int    OverallScore         { get; set; }
    [JsonPropertyName("surface_summary")]        public string SurfaceSummary       { get; set; } = "";
    [JsonPropertyName("subconscious_summary")]   public string SubconsciousSummary  { get; set; } = "";
    [JsonPropertyName("dramatic_question_active")]public bool  DramaticQuestionActive{ get; set; }
    [JsonPropertyName("improvement_hint")]       public string ImprovementHint      { get; set; } = "";
    [JsonIgnore]                                 public string? RawResponse         { get; set; }
}

public class FiveActEntry
{
    [JsonPropertyName("beat_numbers")]   public List<int> BeatNumbers  { get; set; } = new();
    [JsonPropertyName("ignition_beat")]  public int?  IgnitionBeat     { get; set; }
    [JsonPropertyName("trigger_beat")]   public int?  TriggerBeat      { get; set; }
    [JsonPropertyName("god_moment_beat")]public int?  GodMomentBeat    { get; set; }
    [JsonPropertyName("resolution")]     public string? Resolution     { get; set; }
    [JsonPropertyName("assessment")]     public string Assessment      { get; set; } = "";
}

public class FiveActMap
{
    [JsonPropertyName("node_title")]         public string NodeTitle        { get; set; } = "";
    [JsonPropertyName("acts")]                 public Dictionary<string, FiveActEntry> Acts { get; set; } = new();
    [JsonPropertyName("structural_gaps")]      public List<string> StructuralGaps    { get; set; } = new();
    [JsonPropertyName("structural_strengths")] public List<string> StructuralStrengths { get; set; } = new();
    [JsonPropertyName("overall_assessment")]   public string OverallAssessment   { get; set; } = "";
    [JsonIgnore] public string  NodeSlug  { get; set; } = "";
    [JsonIgnore] public int     BeatCount   { get; set; }
    [JsonIgnore] public string? Error       { get; set; }
    [JsonIgnore] public string? RawResponse { get; set; }
}

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
