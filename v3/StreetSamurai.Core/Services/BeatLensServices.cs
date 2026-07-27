using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

// ── Beat-Lens analysis services ───────────────────────────────────────────────
//
// Three sibling "behave like people" lenses, each a single-LLM-call read over a
// node's ordered, numbered beats. They write advisory Findings (no new DB
// tables, no migration) and return a per-lens score + issue list.
//
//   CausalityService          — events follow by therefore/but, not "and then".
//   AffectBehaviorService     — emotion plausibly DRIVES action.
//   InterpersonalDynamicsService — verbal + non-verbal exchange does real work.
//
// Usage:
//   ss --causality-check     --slug <slug> [--json]
//   ss --affect-check        --slug <slug> [--json]
//   ss --interpersonal-check --slug <slug> [--json]
//   MCP: causality_check / affect_check / interpersonal_check (nodeIdOrSlug)

public record LensIssue(int? Beat, string Kind, string Evidence, string Fix, string Severity);

public record LensResult(
    Guid NodeId, string Slug, string Title,
    string Lens, double Score,
    IReadOnlyList<LensIssue> Issues, string Recommendation);

/// <summary>
/// Base for the single-call, beat-numbered story lenses. Subclasses supply the
/// lens title and rubric; the base loads beats, calls the LLM, parses the JSON
/// contract, writes Findings, and returns a <see cref="LensResult"/>.
/// </summary>
public abstract class BeatLensService
{
    protected readonly ILlmService Llm;
    protected readonly FindingsService Findings;
    protected readonly IDbContextFactory<StreetSamuraiDbContext> DbFactory;
    protected readonly ILogger Log;

    protected BeatLensService(
        ILlmService llm, FindingsService findings,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory, ILogger log)
    {
        Llm = llm; Findings = findings; DbFactory = dbFactory; Log = log;
    }

    /// <summary>ALL-CAPS finding prefix, e.g. "CAUSALITY".</summary>
    protected abstract string Tag { get; }
    /// <summary>Human lens name for the result, e.g. "Causality".</summary>
    protected abstract string LensName { get; }
    /// <summary>One-line statement of what this lens judges.</summary>
    protected abstract string LensTitle { get; }
    /// <summary>The lens-specific rubric: what to reward, what to flag.</summary>
    protected abstract string Rubric { get; }

    public async Task<LensResult> RunAsync(Guid nodeId, int maxChars = 45000, CancellationToken ct = default)
    {
        await using var db = await DbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var hasChildren = await db.Nodes.AsNoTracking()
            .AnyAsync(s => s.ParentNodeId == nodeId && s is ChapterNode, ct);

        List<(int Num, string Text)> beats;
        if (hasChildren)
        {
            var rows = await (
                from s in db.Nodes.AsNoTracking()
                join sb in db.BeatNodes.AsNoTracking() on s.Id equals sb.NodeId
                join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
                where s.ParentNodeId == nodeId && s is ChapterNode && sb.IsEnabled
                orderby s.SortKey, sb.SortKey
                select new { b.Text, b.Number }
            ).ToListAsync(ct);
            beats = rows.Where(r => !string.IsNullOrWhiteSpace(r.Text))
                        .Select(r => (r.Number, r.Text)).ToList();
            maxChars = Math.Max(maxChars, 100000);
        }
        else
        {
            var rows = await (
                from sb in db.BeatNodes.AsNoTracking()
                join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
                where sb.NodeId == nodeId && sb.IsEnabled
                orderby sb.SortKey
                select new { b.Text, b.Number }
            ).ToListAsync(ct);
            beats = rows.Where(r => !string.IsNullOrWhiteSpace(r.Text))
                        .Select(r => (r.Number, r.Text)).ToList();
        }

        if (beats.Count == 0)
            return new LensResult(nodeId, node.Slug, node.Title, LensName, 0,
                Array.Empty<LensIssue>(), "No prose to analyse.");

        var sb2 = new StringBuilder();
        foreach (var (num, text) in beats)
            sb2.Append("### Beat ").Append(num).Append('\n').Append(text).Append("\n\n");
        var numbered = Truncate(sb2.ToString(), maxChars);

        const string system =
            "You are an expert developmental story editor. " +
            "Return ONLY the JSON object requested — no markdown fences, no prose, no explanation.";

        var prompt = $$"""
{{LensTitle}}

{{Rubric}}

The prose below is one book node. Beats are separated and labelled "### Beat N".

Return ONLY a JSON object with these exact keys:
{
  "score": <int 0-100 — overall quality of the node ON THIS LENS ONLY>,
  "issues": [
    {
      "beat": <int beat number, or null if node-wide>,
      "kind": "<short-kebab kind, e.g. and-then / unmotivated / dead-exchange>",
      "evidence": "<a direct quote of the weakest moment>",
      "fix": "<one concrete, beat-scoped directive to repair it>",
      "severity": "Low|Medium|High"
    }
  ]
}

Report only real, high-value issues (max 12), worst first. If the node is strong on this lens,
return few or no issues and a high score. Be specific and quote the text.

PROSE:
{{numbered}}
""";

        double score = 75; var issues = new List<LensIssue>();
        try
        {
            var raw = await Llm.GenerateAsync(system, prompt, 0.1, 2600, null, ct);
            using var doc = JsonDocument.Parse(ExtractJson(raw));
            var root = doc.RootElement;
            if (root.TryGetProperty("score", out var sp) && sp.ValueKind == JsonValueKind.Number)
                score = Math.Clamp(sp.GetDouble(), 0, 100);
            if (root.TryGetProperty("issues", out var ip) && ip.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in ip.EnumerateArray())
                {
                    int? beat = el.TryGetProperty("beat", out var bp) && bp.ValueKind == JsonValueKind.Number
                        ? bp.GetInt32() : null;
                    var kind = Str(el, "kind", "issue");
                    var ev   = Str(el, "evidence", "");
                    var fix  = Str(el, "fix", "");
                    var sev  = Str(el, "severity", "Medium");
                    issues.Add(new LensIssue(beat, kind, ev, fix, NormSeverity(sev)));
                }
            }
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "{Tag} lens failed for {Slug}", Tag, node.Slug);
            return new LensResult(nodeId, node.Slug, node.Title, LensName, score,
                issues, "Lens call failed — re-run.");
        }

        // Refresh findings for this lens on this node
        Findings.DeleteBySummaryPrefix($"node:{node.Slug}", Tag + " ");
        foreach (var iss in issues)
        {
            var sev = iss.Severity == "High" ? FindingSeverity.High
                    : iss.Severity == "Low"  ? FindingSeverity.Low
                    : FindingSeverity.Medium;
            Findings.Upsert(
                filePath:     $"node:{node.Slug}",
                chapterId:    null,
                category:     FindingCategory.Other,
                severity:     sev,
                summary:      $"{Tag} [{iss.Kind}]{(iss.Beat.HasValue ? $" beat {iss.Beat}" : "")}: {Trunc(iss.Fix, 240)}",
                snippet:      iss.Evidence,
                suggestedFix: iss.Fix);
        }

        var rec = issues.Count == 0
            ? $"{LensName}: clean ({score:F0}/100)."
            : $"{LensName}: {score:F0}/100; {issues.Count(i => i.Severity == "High")} high / {issues.Count} total issues filed.";

        return new LensResult(nodeId, node.Slug, node.Title, LensName, score, issues, rec);
    }

    // ── helpers ───────────────────────────────────────────────────────────────
    private static string Str(JsonElement el, string name, string fallback) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? (p.GetString() ?? fallback) : fallback;

    private static string NormSeverity(string s) =>
        s.StartsWith("h", StringComparison.OrdinalIgnoreCase) ? "High"
        : s.StartsWith("l", StringComparison.OrdinalIgnoreCase) ? "Low" : "Medium";

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n];

    private static string Truncate(string text, int max) =>
        text.Length <= max ? text : text[..max];

    private static string ExtractJson(string raw)
    {
        int a = raw.IndexOf('{'), b = raw.LastIndexOf('}');
        return (a >= 0 && b > a) ? raw[a..(b + 1)] : raw;
    }
}

/// <summary>Cause-and-effect / common-sense lens: therefore/but, never "and then".</summary>
public sealed class CausalityService : BeatLensService
{
    public CausalityService(ILlmService llm, FindingsService findings,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory, ILogger<CausalityService> log)
        : base(llm, findings, dbFactory, log) { }

    protected override string Tag => "CAUSALITY";
    protected override string LensName => "Causality";
    protected override string LensTitle => "You judge CAUSE-AND-EFFECT and common-sense plausibility.";
    protected override string Rubric => """
        Each beat should follow from what came before by THEREFORE or BUT — not "and then".
        FLAG: episodic "and then" transitions (events that don't cause the next); effects with no
        setup; coincidences that solve problems; a character acting against their established motive,
        knowledge, or capability; reactions that defy common sense or world rules; problems that
        evaporate without cost. REWARD: tight causal chains where each beat's outcome forces the next,
        and where setups pay off. An info-dump or a scene that could be deleted without breaking the
        chain is a failure.
        """;
}

/// <summary>Affect→behaviour lens: emotion plausibly drives action.</summary>
public sealed class AffectBehaviorService : BeatLensService
{
    public AffectBehaviorService(ILlmService llm, FindingsService findings,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory, ILogger<AffectBehaviorService> log)
        : base(llm, findings, dbFactory, log) { }

    protected override string Tag => "AFFECT-BEHAVIOR";
    protected override string LensName => "Affect→Behavior";
    protected override string LensTitle => "You judge whether each character's EMOTION believably DRIVES their ACTION.";
    protected override string Rubric => """
        A character's choices must follow from their established emotional state — fear, anger, grief,
        shame, love, exhaustion. FLAG: actions that ignore what just happened to the character;
        unmotivated calm under threat; an emotional event with no behavioural consequence; a feeling
        named but not enacted; whiplash mood with no cause. REWARD: behaviour that reads as the
        inevitable output of what the character feels right now — body before mind, the controlled
        character re-armouring, the consequence carried into the next beat.
        """;
}

/// <summary>Interpersonal-dynamics lens: the 90+ relational layer, verbal + non-verbal.</summary>
public sealed class InterpersonalDynamicsService : BeatLensService
{
    public InterpersonalDynamicsService(ILlmService llm, FindingsService findings,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory, ILogger<InterpersonalDynamicsService> log)
        : base(llm, findings, dbFactory, log) { }

    protected override string Tag => "INTERPERSONAL";
    protected override string LensName => "Interpersonal Dynamics";
    protected override string LensTitle =>
        "You judge INTERPERSONAL DYNAMICS — the relational layer that lifts a node to 90+.";
    protected override string Rubric => """
        Every scene with two or more people is a relationship under pressure, carried on TWO channels:
        VERBAL (what's said, what's pointedly NOT said, the answer to the question under the question,
        deflection, teasing, status moves, repair attempts) and NON-VERBAL (body, proximity, eye
        contact and its refusal, gesture, who touches/steps back, what the body does that the mouth
        contradicts). FLAG: info-only "dead" exchanges that do no relational work; scenes missing the
        non-verbal channel entirely; on-the-nose emotion-naming instead of subtext; relationships that
        don't change across the exchange. REWARD: exchanges that shift power, deepen or fracture a
        bond, expose or conceal, forgive or wound — and leave the relationship changed going forward.
        Genuine human interaction (good, bad, or indifferent) is the secret sauce; render it.
        """;
}
