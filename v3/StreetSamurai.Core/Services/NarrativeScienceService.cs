using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

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
//   • AuditSceneEngagementAsync   — 6-point scene anatomy against the neural
//                                   engagement triggers (change, info-gap, cause-
//                                   effect, tribal emotion, specificity, show/tell)
//   • MapFiveActStructureAsync    — maps a node's beats to Storr's 5-act arc
//   • CheckAntiheroEmpathyAsync   — scores the 4 antihero empathy levers
// ─────────────────────────────────────────────────────────────────────────────

public class NarrativeScienceService(
    ILlmService llm,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── Sacred Flaw ───────────────────────────────────────────────────────────

    public async Task<SacredFlawAnalysis> AnalyzeSacredFlawAsync(
        Guid characterId, bool scaffold = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var c = await db.Characters
            .AsNoTracking()
            .Include(x => x.PsychologyTraits)
            .Include(x => x.BehavioralRules)
            .FirstOrDefaultAsync(x => x.Id == characterId, ct)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        var psychoBlob = BuildPsychologyBlob(c);

        var system = """
            You are a narrative-science analyst trained on Will Storr's "The Science of Storytelling".
            Your task: identify or scaffold a character's SACRED FLAW — their theory of control.

            DEFINITIONS
            • Theory of Control: the character's core false belief about what they must do to stay safe/powerful/loved. One sentence.
            • Origin Damage: the specific formative scene or wound that created the theory of control.
            • Secret Dread: what the character fears will happen if they act against their flaw.
            • Hero-Maker Narrative: how the character frames their flaw as a strength or virtue.
            • Material Gains: concrete career/status/money advantages the flaw currently provides (makes change terrifying).

            OUTPUT FORMAT: respond with a JSON object only, no prose wrapper.
            {
              "theory_of_control": "...",
              "origin_damage": "...",
              "secret_dread": "...",
              "hero_maker_narrative": "...",
              "material_gains": "...",
              "confidence": "high|medium|low",
              "diagnosis": "...(one paragraph: why this flaw is structurally interesting, what story arc it enables)"
            }
            """;

        var user = $"""
            CHARACTER: {c.Name}
            Role: {c.Role}
            Description: {c.Description}
            Narrative Function: {c.NarrationVoice}
            Psychology: {psychoBlob}
            Mode: {(scaffold ? "scaffold (generate plausible flaw from available info)" : "analyze (identify from existing data)")}
            """;

        var raw = await llm.GenerateAsync(system, user, temperature: 0.5, maxTokens: 800, ct: ct);
        return ParseJson<SacredFlawAnalysis>(raw) ?? new SacredFlawAnalysis
        {
            TheoryOfControl = "(parse error)",
            Confidence = "low",
            RawResponse = raw,
        };
    }

    // ── Dramatic Question ─────────────────────────────────────────────────────

    public async Task<DramaticQuestionResult> CheckDramaticQuestionAsync(
        string beatText, Guid? characterId = null, CancellationToken ct = default)
    {
        string charContext = "";
        if (characterId.HasValue)
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var c = await db.Characters.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == characterId.Value, ct);
            if (c != null)
                charContext = $"POV CHARACTER: {c.Name}\nRole: {c.Role}\nNarration Voice: {c.NarrationVoice}\n";
        }

        var system = """
            You are a narrative-science analyst. Your task: evaluate whether a story beat
            adequately poses or advances the DRAMATIC QUESTION per Will Storr's framework.

            THE DRAMATIC QUESTION: "Who is this person REALLY?" — operating on two levels:
              Surface level: what is happening externally in this beat?
              Subconscious level: what does this reveal or challenge about the character's
                                   core belief (theory of control)?

            Strong beats address BOTH levels. Weak beats address only the surface.

            OUTPUT FORMAT: JSON only, no prose wrapper.
            {
              "surface_score": 1-10,
              "subconscious_score": 1-10,
              "overall_score": 1-10,
              "surface_summary": "what the beat does at the plot level",
              "subconscious_summary": "what the beat reveals/challenges about the character's core belief (or 'none detected')",
              "dramatic_question_active": true|false,
              "improvement_hint": "one concrete suggestion to strengthen the subconscious layer"
            }
            """;

        var user = $"""
            {charContext}
            BEAT TEXT:
            {beatText}
            """;

        var raw = await llm.GenerateAsync(system, user, temperature: 0.4, maxTokens: 600, ct: ct);
        return ParseJson<DramaticQuestionResult>(raw) ?? new DramaticQuestionResult
        {
            OverallScore = 0,
            SurfaceSummary = "(parse error)",
            RawResponse = raw,
        };
    }

    // ── Scene Engagement Audit (6-point) ──────────────────────────────────────

    public async Task<SceneEngagementReport> AuditSceneEngagementAsync(
        string beatText, CancellationToken ct = default)
    {
        var system = """
            You are a narrative-science analyst trained on Will Storr's 6-point scene anatomy.
            Audit the provided beat against each of the six neural engagement mechanisms.

            THE SIX MECHANISMS:
            1. UNEXPECTED CHANGE — something the character didn't plan happens in this beat.
            2. INFORMATION GAP — a question the reader will want answered is opened (or closed).
            3. CAUSE-EFFECT CHAIN — this beat is visibly caused by what came before; it causes what follows.
            4. TRIBAL EMOTION — moral outrage, status play (underdog/humiliation), gossip, or altruistic punishment.
            5. SPECIFICITY (≥3 concrete details) — at least three precise, non-generic sensory or physical details.
            6. SHOW-NOT-TELL (≥60%) — action / dialogue / sensation dominates over summary / exposition.

            A beat PASSES overall if 4 of 6 mechanisms are present.

            OUTPUT FORMAT: JSON only, no prose wrapper.
            {
              "mechanisms": {
                "unexpected_change":  { "present": true|false, "evidence": "..." },
                "information_gap":    { "present": true|false, "evidence": "..." },
                "cause_effect":       { "present": true|false, "evidence": "..." },
                "tribal_emotion":     { "present": true|false, "kind": "moral_outrage|status_play|humiliation|gossip|altruistic_punishment|none", "evidence": "..." },
                "specificity":        { "present": true|false, "detail_count": 0, "examples": ["..."] },
                "show_not_tell":      { "present": true|false, "estimated_pct": 0 }
              },
              "mechanisms_passing": 0,
              "beat_passes": true|false,
              "top_weakness": "the single most damaging gap",
              "fix": "one concrete rewrite suggestion"
            }
            """;

        var user = $"""
            BEAT TEXT:
            {beatText}
            """;

        var raw = await llm.GenerateAsync(system, user, temperature: 0.3, maxTokens: 900, ct: ct);
        return ParseJson<SceneEngagementReport>(raw) ?? new SceneEngagementReport
        {
            BeatPasses = false,
            TopWeakness = "(parse error)",
            RawResponse = raw,
        };
    }

    // ── Five-Act Structure Map ────────────────────────────────────────────────

    public async Task<FiveActMap> MapFiveActStructureAsync(
        Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        // SS-A43: for book-mode nodes, beats live on chapter children.
        var childIds = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == nodeId)
            .Select(n => n.Id).ToListAsync(ct);
        var searchIds = childIds.Count > 0 ? childIds : new List<Guid> { nodeId };
        var beats = await (
            from sb in db.BeatNodes
            join b in db.Beats on sb.BeatId equals b.Id
            where searchIds.Contains(sb.NodeId) && sb.IsEnabled
            orderby sb.SortKey
            select new { b.Number, Title = b.Title ?? "", Description = b.Description ?? "", b.Text }
        ).ToListAsync(ct);

        if (beats.Count == 0)
            return new FiveActMap { NodeSlug = node.Slug ?? "", Error = "No beats found." };

        var beatList = string.Join("\n", beats.Select(b =>
            $"Beat {b.Number}: {b.Title} — {(string.IsNullOrWhiteSpace(b.Description) ? "(no description)" : b.Description)}"));

        var system = """
            You are a narrative-science analyst. Map the provided story beats to Will Storr's
            FIVE-ACT CHARACTER-CHANGE STRUCTURE.

            THE FIVE ACTS:
            I.   ESTABLISH + IGNITE  — Show the protagonist's flaw in action. The ignition event
                                        arrives: an unexpected change that pressures the flaw.
            II.  OLD THEORY TESTED   — Character applies their theory of control to the new situation.
                                        It partially works but at growing cost.
            III. TRANSFORMATION TRIGGER — The flaw fails catastrophically or succeeds at too high a
                                        cost. The protective shell cracks.
            IV.  DARK NIGHT          — All fears realized. Old theory of control stripped. Lowest point.
            V.   GOD MOMENT          — Dramatic question answered definitively. Character transforms
                                        (comic/happy) or doubles down (tragic).

            OUTPUT FORMAT: JSON only, no prose wrapper.
            {
              "node_title": "...",
              "acts": {
                "act_I":   { "beat_numbers": [], "ignition_beat": null|N, "assessment": "..." },
                "act_II":  { "beat_numbers": [], "assessment": "..." },
                "act_III": { "beat_numbers": [], "trigger_beat": null|N, "assessment": "..." },
                "act_IV":  { "beat_numbers": [], "assessment": "..." },
                "act_V":   { "beat_numbers": [], "god_moment_beat": null|N, "resolution": "comic|tragic|unclear", "assessment": "..." }
              },
              "structural_gaps": ["..."],
              "structural_strengths": ["..."],
              "overall_assessment": "..."
            }
            """;

        var user = $"""
            NODE: {node.Title ?? node.Slug}
            SEED: {node.Seed ?? "(none)"}

            BEATS ({beats.Count} total):
            {beatList}
            """;

        var raw = await llm.GenerateAsync(system, user, temperature: 0.4, maxTokens: 1200, ct: ct);
        var result = ParseJson<FiveActMap>(raw) ?? new FiveActMap
        {
            NodeSlug = node.Slug ?? "",
            Error = "(parse error)",
            RawResponse = raw,
        };
        result.NodeSlug = node.Slug ?? "";
        result.BeatCount = beats.Count;
        return result;
    }

    // ── Antihero Empathy Check ────────────────────────────────────────────────

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
        return ParseJson<AntiheroEmpathyResult>(raw) ?? new AntiheroEmpathyResult
        {
            EmpathyScore = 0,
            Diagnosis = "(parse error)",
            RawResponse = raw,
        };
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

public class SceneEngagementMechanism
{
    [JsonPropertyName("present")]   public bool   Present   { get; set; }
    [JsonPropertyName("evidence")]  public string Evidence  { get; set; } = "";
    [JsonPropertyName("kind")]      public string? Kind     { get; set; }
    [JsonPropertyName("detail_count")] public int DetailCount { get; set; }
    [JsonPropertyName("examples")]  public List<string> Examples { get; set; } = new();
    [JsonPropertyName("estimated_pct")] public int EstimatedPct { get; set; }
}

public class SceneEngagementReport
{
    [JsonPropertyName("mechanisms")]          public Dictionary<string, SceneEngagementMechanism> Mechanisms { get; set; } = new();
    [JsonPropertyName("mechanisms_passing")]  public int    MechanismsPassing { get; set; }
    [JsonPropertyName("beat_passes")]         public bool   BeatPasses        { get; set; }
    [JsonPropertyName("top_weakness")]        public string TopWeakness       { get; set; } = "";
    [JsonPropertyName("fix")]                 public string Fix               { get; set; } = "";
    [JsonIgnore]                              public string? RawResponse      { get; set; }
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
