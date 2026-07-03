using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Self-correcting canon guard. For a node's prose it pulls the relevant canon
/// across <em>all</em> entity types (<see cref="CanonRetrievalService"/>) and has
/// an LLM flag every place the prose CONTRADICTS that canon — a wrong attribute,
/// something impossible per an entity's record, a retired thing used as current,
/// an entity behaving against its documented nature. Each contradiction is queued
/// as a <c>CANON-CONTRADICTION</c> finding with a proposed fix.
///
/// This is the "no admin constantly diff-checking" piece: the system does the
/// detection AND drafts the correction across every type (not just characters,
/// which is all the legacy continuity extractor covered). Application stays
/// approval-gated — findings are surfaced for the writer to accept, never
/// silently rewritten into the prose.
/// </summary>
public class CanonContradictionService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly NodeWorkbenchService workbench;
    private readonly CanonRetrievalService retrieval;
    private readonly ILlmService llm;
    private readonly FindingsService findings;
    private readonly ILogger<CanonContradictionService> log;

    private const int ChunkChars = 6000;

    public CanonContradictionService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        NodeWorkbenchService workbench,
        CanonRetrievalService retrieval,
        ILlmService llm,
        FindingsService findings,
        ILogger<CanonContradictionService> log)
    {
        this.dbFactory = dbFactory;
        this.workbench = workbench;
        this.retrieval = retrieval;
        this.llm = llm;
        this.findings = findings;
        this.log = log;
    }

    public sealed record Contradiction(string Entity, string Issue, string? Snippet, string? SuggestedFix, string Severity);
    public sealed record CheckResult(string Slug, int ChunksChecked, List<Contradiction> Contradictions);

    /// <summary>Sweep one node. Each contradiction is written as an
    /// approval-gated CANON-CONTRADICTION finding (nothing rewrites the prose).
    /// When <paramref name="proposeFixes"/> is set, high-severity contradictions
    /// get a concrete canon-honoring rewrite of the offending span generated and
    /// attached to the finding (the bounded self-correction loop — still
    /// approval-gated; the writer applies it).</summary>
    public async Task<CheckResult> CheckNodeAsync(Guid nodeId, bool proposeFixes = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);
        var prose = string.Join("\n\n", ordered.Select(o => (o.Beat.Text ?? "").Trim()).Where(t => t.Length > 0));
        if (prose.Length == 0) return new CheckResult(node.Slug, 0, []);

        var all = new List<Contradiction>();
        int chunks = 0;
        foreach (var chunk in Chunk(prose, ChunkChars))
        {
            ct.ThrowIfCancellationRequested();
            chunks++;
            // Canon relevant to THIS chunk, across every type.
            var canon = await retrieval.RetrieveContextBlockAsync(chunk, k: 24, charBudget: 3000, ct: ct);
            if (canon.Length == 0) continue;
            var found = await DetectAsync(canon, chunk, ct);

            // Bounded self-correction: for high-severity hits with a located
            // snippet, generate a canon-honoring rewrite of just that span and
            // fold it into the suggested fix (using the canon in scope for this
            // chunk). Nothing is auto-applied — the writer approves it.
            if (proposeFixes)
            {
                var corrected = new List<Contradiction>(found.Count);
                foreach (var c in found)
                {
                    if (ParseSeverity(c.Severity) == FindingSeverity.High && !string.IsNullOrWhiteSpace(c.Snippet))
                    {
                        var rewrite = await ProposeCorrectedSpanAsync(c.Snippet!, canon, c.Issue, ct);
                        corrected.Add(string.IsNullOrWhiteSpace(rewrite)
                            ? c
                            : c with { SuggestedFix = $"REWRITE the span to: \"{rewrite!.Trim()}\"" + (string.IsNullOrWhiteSpace(c.SuggestedFix) ? "" : $"  (why: {c.SuggestedFix})") });
                    }
                    else corrected.Add(c);
                }
                found = corrected;
            }

            all.AddRange(found);
        }

        // Queue each as an approval-gated finding.
        foreach (var c in all)
        {
            try
            {
                findings.Upsert(
                    filePath:     $"node:{node.Slug}",
                    chapterId:    null,
                    category:     FindingCategory.Contradiction,
                    severity:     ParseSeverity(c.Severity),
                    summary:      $"CANON-CONTRADICTION [{c.Entity}]: {c.Issue}",
                    snippet:      c.Snippet,
                    suggestedFix: c.SuggestedFix);
            }
            catch (Exception ex) { log.LogWarning(ex, "Failed to queue contradiction finding"); }
        }

        log.LogInformation("Canon check {Slug}: {Chunks} chunks → {N} contradictions.", node.Slug, chunks, all.Count);
        return new CheckResult(node.Slug, chunks, all);
    }

    private async Task<List<Contradiction>> DetectAsync(string canon, string prose, CancellationToken ct)
    {
        var system =
            "You are a canon-continuity auditor. You are given DOCUMENTED CANON (entity facts pulled from a world " +
            "database, across all entity types) and a passage of PROSE. Report ONLY places where the prose CONTRADICTS " +
            "the canon: a wrong attribute, an action impossible per an entity's record, a retired/destroyed thing used " +
            "as current, an entity acting against its documented nature. Do NOT report things merely absent from canon, " +
            "and do NOT invent canon. If there are no contradictions, return []. " +
            "Return ONLY a JSON array; each item: {\"entity\": string, \"issue\": string, \"snippet\": string (the " +
            "contradicting prose, verbatim, short), \"suggested_fix\": string, \"severity\": \"low\"|\"medium\"|\"high\"}. " +
            "No prose, no markdown fences.";
        var user = new StringBuilder()
            .AppendLine(canon).AppendLine()
            .AppendLine("PROSE:").AppendLine(prose).ToString();

        string raw;
        try { raw = await llm.GenerateAsync(system, user, temperature: 0.1, maxTokens: 1500, ct: ct); }
        catch (Exception ex) { log.LogWarning(ex, "Contradiction LLM call failed"); return []; }

        return Parse(raw);
    }

    /// <summary>Rewrite just the offending span so it no longer contradicts canon,
    /// changing as little as possible and keeping the prose voice. Returns the
    /// corrected span text (no JSON), or null on failure. Approval-gated upstream.</summary>
    internal async Task<string?> ProposeCorrectedSpanAsync(string snippet, string canon, string issue, CancellationToken ct)
    {
        var system =
            "You are a careful line editor. Rewrite ONLY the given prose span so it no longer contradicts the canon, " +
            "changing as little as possible and preserving the author's voice, tense, and rhythm. Do not add new facts. " +
            "Return ONLY the rewritten span as plain prose — no quotes, no explanation, no markdown.";
        var user = new StringBuilder()
            .AppendLine(canon).AppendLine()
            .AppendLine($"CONTRADICTION: {issue}").AppendLine()
            .AppendLine("SPAN TO REWRITE:").AppendLine(snippet).ToString();
        try
        {
            var rewrite = await llm.GenerateAsync(system, user, temperature: 0.3, maxTokens: 400, ct: ct);
            rewrite = rewrite?.Trim().Trim('"');
            return string.IsNullOrWhiteSpace(rewrite) ? null : rewrite;
        }
        catch (Exception ex) { log.LogWarning(ex, "Span rewrite failed"); return null; }
    }

    internal static List<Contradiction> Parse(string raw)
    {
        var json = ExtractJsonArray(raw);
        if (json == null) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];
            var list = new List<Contradiction>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                string S(string n) => el.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : "";
                string? SN(string n) => el.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
                var issue = S("issue");
                if (string.IsNullOrWhiteSpace(issue)) continue;
                list.Add(new Contradiction(
                    string.IsNullOrWhiteSpace(S("entity")) ? "(unnamed)" : S("entity"),
                    issue, SN("snippet"), SN("suggested_fix"),
                    string.IsNullOrWhiteSpace(S("severity")) ? "medium" : S("severity")));
            }
            return list;
        }
        catch { return []; }
    }

    private static string? ExtractJsonArray(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        int start = raw.IndexOf('[');
        int end = raw.LastIndexOf(']');
        return start >= 0 && end > start ? raw[start..(end + 1)] : null;
    }

    internal static FindingSeverity ParseSeverity(string s) => s.Trim().ToLowerInvariant() switch
    {
        "high" => FindingSeverity.High,
        "low"  => FindingSeverity.Low,
        _      => FindingSeverity.Medium,
    };

    /// <summary>Split prose into chunks on paragraph boundaries up to a char budget.</summary>
    internal static IEnumerable<string> Chunk(string text, int max)
    {
        var paras = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();
        foreach (var p in paras)
        {
            if (sb.Length > 0 && sb.Length + p.Length + 2 > max)
            {
                yield return sb.ToString();
                sb.Clear();
            }
            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append(p);
        }
        if (sb.Length > 0) yield return sb.ToString();
    }
}
