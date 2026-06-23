using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

// ── Emotional Depth Service (SS-A15) ──────────────────────────────────────────
//
// Scores prose against an 8-dimension, 0–4 emotional rubric — per beat,
// character-aware (Want/Need/Wound/Flaw via EmotionalLedgerService), and
// register-adaptive (CODA vs JOY/SORROW/Fantasy anchors).
//
// Two-pass examination honouring effort tier:
//   Draft    — Pass 1 only (8 parallel dimension calls, cheap models).
//   Standard — Pass 1 + Pass 2 (per-beat emotional curve).
//   Deep     — Pass 1 + Pass 2 + ledger refresh + weakest-moment fix writes.
//
// Advisory cap: blocking dimensions (WantNeedDivergence, CostFeltNotAsserted)
// file Findings via FindingsService. Does NOT alter Strand.Score or the 82/85
// reader-panel gate.
//
// Usage:
//   ss --examine-emotion --slug <slug> [--effort draft|standard|deep] [--json]
//   MCP: examine_emotional_depth(strandIdOrSlug, effort, maxChars)

public enum EmotionalDimension
{
    WantNeedDivergence        = 0,
    TheUnsaid                 = 1,
    ObjectsAndGestures        = 2,
    RegisterShiftAsInstrument = 3,
    EarnedInteriority         = 4,
    RelationalSubtext         = 5,
    CostFeltNotAsserted       = 6,
    ContradictionAndAmbivalence = 7,
}

public record DimensionResult(
    EmotionalDimension Dimension,
    string Name,
    string Description,
    int Score,
    string StrongestEvidence,
    string WeakestEvidence,
    int? WeakestBeatNumber,
    string Fix,
    string CraftLaw,
    bool IsBlocking);

public record BeatEmotionalScore(int BeatNumber, int Depth, string? Note);

public record EmotionalExaminationResult(
    Guid StrandId,
    string Slug,
    string Title,
    double EmotionalDepthScore,
    string Register,
    IReadOnlyList<DimensionResult> Dimensions,
    IReadOnlyList<BeatEmotionalScore> BeatCurve,
    IReadOnlyList<CharacterLedgerEntry> Ledgers,
    int BlockingCount,
    string Recommendation);

/// <summary>
/// Scores prose against an 8-dimension, 0–4 emotional rubric — per beat,
/// character-aware, and register-adaptive. Run before the Legion panel to
/// surface subtext weaknesses with craft-specific fixes.
/// </summary>
public class EmotionalDepthService
{
    private readonly ILlmService llm;
    private readonly FindingsService findings;
    private readonly EmotionalLedgerService ledger;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<EmotionalDepthService> log;

    // Dimension table — name, description, craft law, is-blocking
    private static readonly (string Name, string Desc, string CraftLaw, bool Blocking)[] DimensionDefs =
    [
        ("WantNeedDivergence",        "Gap between on-page Want and arc-revealed Need dramatized vs. collapsed/absent.", "bible: Will-vs-Flaw, Want/Need",                                           true),
        ("TheUnsaid",                 "Meaning carried by the withheld / silence / white space, or every feeling named.", "CODA: 'white space does the mourning'",                                   false),
        ("ObjectsAndGestures",        "Tenderness/cost arriving as objects and physical acts vs. stated feeling.",        "CODA: 'tenderness arrives as objects, never statements'",                 false),
        ("RegisterShiftAsInstrument", "Temperature shifts on cue vs. one flat tone throughout.",                          "CODA: 'the register SHIFT is the instrument'",                            false),
        ("EarnedInteriority",         "Rare, load-bearing interior lines vs. spammed or subtext-explaining interiority.", "CODA: 'one flat interior line'; RULE ZERO: 'narrator is never wise'",   false),
        ("RelationalSubtext",         "Power/evasion/approach-and-retreat in dialogue vs. pure information exchange.",   "CODA: Kyle↔Pixel; bible relationships",                                   false),
        ("CostFeltNotAsserted",       "Price of wins felt (calories, years, wound ledger) vs. asserted.",                "CODA: 'every win PRICED'",                                                 true),
        ("ContradictionAndAmbivalence","Gap between what a character feels and what they show, vs. emotionally one-note.","bible: Flaw; contradiction",                                              false),
    ];

    private const string ScaleAnchor = """
0 = Absent      (flat/stated — no dimension present)
1 = Asserted    (told, reads as a label)
2 = Mixed       (inconsistent — sometimes present, sometimes not)
3 = Embodied    (working through behaviour/object/silence)
4 = Instrument  (the dimension IS doing the emotional work, at Full Freight / One Shoe exemplar grade)
""";

    public EmotionalDepthService(
        ILlmService llm,
        FindingsService findings,
        EmotionalLedgerService ledger,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<EmotionalDepthService> log)
    {
        this.llm       = llm;
        this.findings  = findings;
        this.ledger    = ledger;
        this.dbFactory = dbFactory;
        this.log       = log;
    }

    // ── Public entry points ───────────────────────────────────────────────────

    public async Task<EmotionalExaminationResult> ExamineStrandAsync(
        Guid strandId, string effort = "standard", int maxChars = 40000, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var strand = await db.Strands.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == strandId, ct)
            ?? throw new InvalidOperationException($"Strand {strandId} not found.");

        var beatRows = await (
            from sb in db.StrandBeats.AsNoTracking()
            join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
            where sb.StrandId == strandId
            orderby sb.SortKey
            select new { b.Text, b.Number }
        ).ToListAsync(ct);

        var beats     = beatRows.Select(x => x.Text).ToList();
        var beatNums  = beatRows.Select(x => x.Number).ToList();
        var assembled = string.Join("\n\n---\n\n", beats.Where(t => !string.IsNullOrWhiteSpace(t)));

        return await ExamineTextAsync(
            strandId, strand.Slug, strand.Title, strand.StrandBible,
            assembled, beatNums, effort, maxChars, ct);
    }

    public async Task<EmotionalExaminationResult> ExamineTextAsync(
        Guid strandId, string slug, string title, string? bible,
        string text, IReadOnlyList<int> beatNumbers,
        string effort = "standard", int maxChars = 40000, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return EmptyResult(strandId, slug, title);

        var truncated = Truncate(text, maxChars);
        var register  = DetectRegister(bible);
        var forceRefresh = effort == "deep";

        // Ledger extraction (all tiers — cheap, cached)
        var ledgers = await this.ledger.GetLedgerAsync(strandId, bible, truncated, forceRefresh, ct);
        var whoBlock = BuildWhoBlock(ledgers);

        // Pass 1 — 8 parallel dimension calls (all tiers)
        var dimTasks = DimensionDefs.Select((d, i) =>
            RunDimensionAsync((EmotionalDimension)i, d.Name, d.Desc, d.CraftLaw, d.Blocking,
                truncated, register, whoBlock, ct)).ToArray();

        var dimensions = (await Task.WhenAll(dimTasks)).ToList();

        // Pass 2 — per-beat curve (Standard / Deep only)
        var beatCurve = new List<BeatEmotionalScore>();
        if (effort is "standard" or "deep" && beatNumbers.Count > 0)
            beatCurve = await RunBeatCurveAsync(truncated, beatNumbers, ct);

        // Score = mean(dim/4)*100
        var depthScore = dimensions.Count > 0
            ? dimensions.Average(d => d.Score / 4.0) * 100.0
            : 0.0;

        int blockingCount = dimensions.Count(d => d.IsBlocking && d.Score <= 1);

        // File blocking findings
        foreach (var dim in dimensions.Where(d => d.IsBlocking && d.Score <= 1))
        {
            var sev = dim.Score == 0 ? FindingSeverity.High : FindingSeverity.Medium;
            findings.Upsert(
                filePath: $"strand:{slug}",
                chapterId: null,
                category: FindingCategory.Other,
                severity: sev,
                summary: $"EMOTIONAL-DEPTH [{dim.Name}]{(dim.WeakestBeatNumber.HasValue ? $" beat {dim.WeakestBeatNumber}" : "")}: {dim.Fix}",
                snippet: dim.WeakestEvidence,
                suggestedFix: dim.Fix);
        }

        // Persist examination + children
        var hash = Hash(text);
        await PersistAsync(strandId, effort, depthScore, register, hash,
            beatNumbers.Count, blockingCount, dimensions, beatCurve, ct);

        // Write Beat.EmotionalScore for Pass 2 results
        if (beatCurve.Count > 0)
            await UpdateBeatEmotionalScoresAsync(strandId, beatCurve, ct);

        var recommendation = blockingCount > 0
            ? $"⛔ {blockingCount} blocking dimension(s) open — resolve WantNeedDivergence / CostFeltNotAsserted before marking publish-ready."
            : depthScore < 50
                ? "Multiple dimensions scoring ≤ 2 — prioritise the weakest-beat fixes before the next revision pass."
                : depthScore >= 75
                    ? "Strong emotional architecture — targeted spot fixes at weakest beats will lift to Instrument grade."
                    : "Solid foundation — address the weakest dimensions' beat-scoped fixes to push toward Embodied/Instrument.";

        return new EmotionalExaminationResult(
            strandId, slug, title,
            Math.Round(depthScore, 1),
            register,
            dimensions,
            beatCurve,
            ledgers,
            blockingCount,
            recommendation);
    }

    // ── Dimension runner ──────────────────────────────────────────────────────

    private async Task<DimensionResult> RunDimensionAsync(
        EmotionalDimension dimension, string name, string desc, string craftLaw, bool isBlocking,
        string text, string register, string whoBlock, CancellationToken ct)
    {
        const string systemBase =
            "You are an expert story editor specialising in emotional subtext. " +
            "Return ONLY the JSON object requested. No prose, no markdown fences, no explanation.";

        var registerNote = dimension == EmotionalDimension.RegisterShiftAsInstrument
            ? register == "CODA"
                ? "Apply CODA-specific anchors: warm→cold temperature shifts on cue. Score 4 only if the shift IS the instrument."
                : "Soften the anchor: look for purposeful tonal modulation (not necessarily CODA warm→cold). Score 4 for sustained purposeful modulation."
            : "";

        var registerLine = registerNote.Length > 0 ? $"Register note ({register}): {registerNote}\n" : "";
        var prompt = $$"""
You are scoring prose on the EMOTIONAL DIMENSION: {{name}}
Definition: {{desc}}
Craft law: {{craftLaw}}
{{registerLine}}
CHARACTER CONTEXT:
{{whoBlock}}

SCORING SCALE (0–4):
{{ScaleAnchor}}

Read the prose and return a JSON object with these exact keys:
{
  "score": <int 0-4>,
  "strongest_evidence": "<direct quote from the text that best exemplifies this dimension>",
  "weakest_evidence": "<direct quote from the flattest moment>",
  "weakest_beat_number": <beat number int, or null if not determinable>,
  "fix": "<one beat-scoped, character-aware directive: Beat N: [Character] [want/behaviour]. Replace '[quote]' with [craft-law move] so [need/cost/subtext] lands without being named.>"
}

PROSE:
{{text}}
""";

        try
        {
            var raw  = await llm.GenerateAsync(systemBase, prompt, 0.1, 600, null, ct);
            var json = ExtractJson(raw);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            int score    = root.TryGetProperty("score",              out var sp) ? sp.GetInt32()     : 2;
            var strongest = root.TryGetProperty("strongest_evidence", out var sep) ? sep.GetString() ?? "" : "";
            var weakest   = root.TryGetProperty("weakest_evidence",   out var wep) ? wep.GetString() ?? "" : "";
            int? weakBeat = root.TryGetProperty("weakest_beat_number", out var wbp) && wbp.ValueKind == JsonValueKind.Number
                ? wbp.GetInt32() : null;
            var fix       = root.TryGetProperty("fix", out var fp) ? fp.GetString() ?? "" : "";

            return new DimensionResult(dimension, name, desc,
                Math.Clamp(score, 0, 4), strongest, weakest, weakBeat, fix, craftLaw, isBlocking);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Dimension {Name} failed; defaulting to Mixed (2)", name);
            return new DimensionResult(dimension, name, desc, 2, "", "", null,
                "Re-run the examination to get a valid fix.", craftLaw, isBlocking);
        }
    }

    // ── Per-beat curve (Pass 2) ───────────────────────────────────────────────

    private async Task<List<BeatEmotionalScore>> RunBeatCurveAsync(
        string text, IReadOnlyList<int> beatNumbers, CancellationToken ct)
    {
        int beatCount  = beatNumbers.Count;
        int maxTok     = 900 + beatCount * 6;

        const string system =
            "You are a story editor scoring emotional depth beat-by-beat. " +
            "Return ONLY the JSON array requested. No prose, no markdown fences, no explanation.";

        var prompt = $$"""
Score every beat of this prose for EMOTIONAL DEPTH on the 0–4 scale:
{{ScaleAnchor}}

The prose below has {{beatCount}} beats separated by "---". Score each in order.

Return a JSON array with one entry per beat in order:
[{"beat_number": <int>, "depth": <int 0-4>, "note": "<one short phrase naming the key strength or weakness>"}]

PROSE:
{{text}}
""";

        try
        {
            var raw  = await llm.GenerateAsync(system, prompt, 0.1, maxTok, null, ct);
            var json = ExtractJsonArray(raw);
            using var doc = JsonDocument.Parse(json);

            var results = new List<BeatEmotionalScore>();
            int idx = 0;
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                int beatNum = el.TryGetProperty("beat_number", out var bnp) ? bnp.GetInt32()
                    : idx < beatNumbers.Count ? beatNumbers[idx] : idx + 1;
                int depth   = el.TryGetProperty("depth", out var dp) ? Math.Clamp(dp.GetInt32(), 0, 4) : 2;
                var note    = el.TryGetProperty("note",  out var np) ? np.GetString() : null;
                results.Add(new BeatEmotionalScore(beatNum, depth, note));
                idx++;
            }
            return results;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Beat curve failed");
            return new List<BeatEmotionalScore>();
        }
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    private async Task PersistAsync(
        Guid strandId, string effort, double score, string register,
        string hash, int beatCount, int blockingCount,
        IReadOnlyList<DimensionResult> dims,
        IReadOnlyList<BeatEmotionalScore> curve,
        CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var exam = new EmotionalExamination
        {
            Id                 = Guid.NewGuid(),
            StrandId           = strandId,
            EffortTier         = effort,
            EmotionalDepthScore = score,
            Register           = register,
            ContentHash        = hash,
            BeatCount          = beatCount,
            BlockingCount      = blockingCount,
            ExaminedAt         = DateTime.UtcNow,
            CreatedAt          = DateTime.UtcNow,
        };

        exam.DimensionResults = dims.Select(d => new EmotionalDimensionResult
        {
            ExaminationId    = exam.Id,
            Dimension        = (int)d.Dimension,
            Score            = d.Score,
            StrongestEvidence = d.StrongestEvidence,
            WeakestEvidence  = d.WeakestEvidence,
            WeakestBeatNumber = d.WeakestBeatNumber,
            Fix              = d.Fix,
            CraftLaw         = d.CraftLaw,
            IsBlocking       = d.IsBlocking,
        }).ToList();

        exam.BeatScores = curve.Select(b => new EmotionalBeatScore
        {
            ExaminationId = exam.Id,
            BeatNumber    = b.BeatNumber,
            Depth         = b.Depth,
            Note          = b.Note,
        }).ToList();

        db.EmotionalExaminations.Add(exam);
        await db.SaveChangesAsync(ct);
    }

    private async Task UpdateBeatEmotionalScoresAsync(
        Guid strandId, IReadOnlyList<BeatEmotionalScore> curve, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beatNumbers = curve.Select(c => c.BeatNumber).ToHashSet();

        var beats = await (
            from sb in db.StrandBeats
            join b in db.Beats on sb.BeatId equals b.Id
            where sb.StrandId == strandId && beatNumbers.Contains(b.Number)
            select b
        ).ToListAsync(ct);

        foreach (var beat in beats)
        {
            var entry = curve.FirstOrDefault(c => c.BeatNumber == beat.Number);
            if (entry is not null)
                beat.EmotionalScore = entry.Depth;
        }

        await db.SaveChangesAsync(ct);
    }

    // ── Register detection ────────────────────────────────────────────────────

    private static string DetectRegister(string? bible)
    {
        if (bible is null or { Length: 0 }) return "";
        if (bible.Contains("CODA",    StringComparison.OrdinalIgnoreCase)) return "CODA";
        if (bible.Contains("JOY",     StringComparison.OrdinalIgnoreCase)) return "JOY";
        if (bible.Contains("SORROW",  StringComparison.OrdinalIgnoreCase)) return "SORROW";
        if (bible.Contains("Fantasy", StringComparison.OrdinalIgnoreCase)) return "Fantasy";
        return "";
    }

    // ── Character context block ───────────────────────────────────────────────

    private static string BuildWhoBlock(IReadOnlyList<CharacterLedgerEntry> ledgers)
    {
        if (ledgers.Count == 0) return "(No character ledger available — score based on prose evidence alone.)";

        var sb = new StringBuilder();
        foreach (var e in ledgers)
        {
            sb.AppendLine($"  {e.Character}:");
            if (!string.IsNullOrWhiteSpace(e.Want))           sb.AppendLine($"    Want: {e.Want}");
            if (!string.IsNullOrWhiteSpace(e.Need))           sb.AppendLine($"    Need: {e.Need}");
            if (!string.IsNullOrWhiteSpace(e.Wound))          sb.AppendLine($"    Wound: {e.Wound}");
            if (!string.IsNullOrWhiteSpace(e.Flaw))           sb.AppendLine($"    Flaw: {e.Flaw}");
            if (!string.IsNullOrWhiteSpace(e.VoiceRegister))  sb.AppendLine($"    Register: {e.VoiceRegister}");
            if (e.Inferred)                                    sb.AppendLine($"    (inferred from prose — treat with caution)");
        }
        return sb.ToString().TrimEnd();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Hash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max] + "\n[... truncated ...]";

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end   = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw[start..(end + 1)] : raw;
    }

    private static string ExtractJsonArray(string raw)
    {
        var start = raw.IndexOf('[');
        var end   = raw.LastIndexOf(']');
        return start >= 0 && end > start ? raw[start..(end + 1)] : "[]";
    }

    private static EmotionalExaminationResult EmptyResult(Guid id, string slug, string title) =>
        new(id, slug, title, 0, "", [], [], [], 0, "No prose found — nothing to examine.");
}
