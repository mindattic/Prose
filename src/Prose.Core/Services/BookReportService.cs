using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services.Audit;

namespace Prose.Core.Services;

public sealed record BookReportResult(
    Guid NodeId, string NodeSlug, string? NodeCode, string Title, string Markdown,
    int WordCount = 0, int BeatCount = 0, int OpenFindings = 0, bool Converged = false);

/// <summary>
/// Codifies the read-the-backlog-by-hand-then-report pass this engine's QA work has always done
/// ad hoc (most recently: the 2026-10-08 BLST session — read every open finding against the live
/// prose, verify or dismiss each by hand, fix what's real, then write up what happened). This
/// service does ONLY the deterministic half of that: pulling what's already on file (findings by
/// status/category, logged decisions, logic-sweep convergence, word/beat counts) into one
/// document. It does not itself judge anything — Law 7 (deterministic checks only, no LLM judges)
/// — the judging already happened, by hand, before the matching decisions were logged; this is the
/// report of that work, not a replacement for doing it.
/// </summary>
public class BookReportService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly FindingsService findings;
    private readonly LogicSweepService logicSweep;

    public BookReportService(
        IDbContextFactory<ProseDbContext> dbFactory,
        FindingsService findings,
        LogicSweepService logicSweep)
    {
        this.dbFactory  = dbFactory;
        this.findings   = findings;
        this.logicSweep = logicSweep;
    }

    public async Task<BookReportResult?> GenerateAsync(string nodeRef, bool complete = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var nodeId = await NodeRefResolver.ResolveAsync(db, nodeRef, ct);
        if (nodeId is null) return null;

        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct);
        if (node is null) return null;

        var beatTexts = await db.BeatNodes.AsNoTracking()
            .Where(bn => bn.NodeId == nodeId)
            .Join(db.Beats.AsNoTracking(), bn => bn.BeatId, b => b.Id, (bn, b) => b.Text)
            .ToListAsync(ct);
        var beatCount = beatTexts.Count;
        var wordCount = beatTexts.Sum(t => Regex.Matches(t ?? "", @"\S+").Count);

        var all = findings.List(status: null, limit: 20000, filePathPrefix: $"node:{node.Slug}");
        var open = all.Where(f => f.Status is FindingStatus.New or FindingStatus.Triaged).ToList();
        var resolved = all.Where(f => f.Status is FindingStatus.Applied or FindingStatus.Dismissed).ToList();
        var autoSuppressed = resolved.Where(f => f.SuppressedBy != null).ToList();
        var handTriaged = resolved.Where(f => f.SuppressedBy is null).ToList();

        var converged = await logicSweep.IsConvergedAsync(nodeId.Value, ct: ct);
        var convergenceState = await db.NodeConvergenceStates.AsNoTracking()
            .FirstOrDefaultAsync(s => s.NodeId == nodeId, ct);

        var codeOrTitle = node.NodeCode ?? node.Title;
        var decisions = await db.DecisionLedgerEntries.AsNoTracking()
            .Where(d => d.Summary.Contains(codeOrTitle) || d.Summary.Contains(node.Title)
                     || (d.Rationale != null && (d.Rationale.Contains(codeOrTitle) || d.Rationale.Contains(node.Title))))
            .OrderByDescending(d => d.At)
            .Take(10)
            .ToListAsync(ct);

        var md = Render(node.Title, node.NodeCode, node.Slug, complete, wordCount, beatCount,
            open, handTriaged, autoSuppressed, converged, convergenceState?.ConsecutiveDryRounds ?? 0,
            convergenceState?.TotalRoundsRun ?? 0, decisions);

        return new BookReportResult(nodeId.Value, node.Slug, node.NodeCode, node.Title, md,
            wordCount, beatCount, open.Count, converged);
    }

    private static string Render(
        string title, string? code, string slug, bool complete, int wordCount, int beatCount,
        IReadOnlyList<Finding> open, IReadOnlyList<Finding> handTriaged, IReadOnlyList<Finding> autoSuppressed,
        bool converged, int consecutiveDryRounds, int totalRounds,
        IReadOnlyList<Data.Entities.DecisionLedgerEntry> decisions)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {title}{(code is null ? "" : $" ({code})")}");
        sb.AppendLine();
        sb.AppendLine($"**Word count:** {wordCount:N0}  **Beats:** {beatCount:N0}  **Slug:** `{slug}`  " +
                       $"**Generated:** {DateTime.UtcNow:yyyy-MM-dd}  **Status:** {(complete ? "Complete" : "Draft")}");
        sb.AppendLine();

        sb.AppendLine("## Logic-sweep convergence");
        sb.AppendLine();
        sb.AppendLine(converged
            ? $"**Converged** — {consecutiveDryRounds} consecutive dry round(s) of {totalRounds} total, against the book's current text."
            : $"**Not converged** — {consecutiveDryRounds} consecutive dry round(s) of {totalRounds} total. Run `/logic-sweep {slug}` before treating this book as settled.");
        sb.AppendLine();

        sb.AppendLine("## Verified and closed this pass");
        sb.AppendLine();
        if (handTriaged.Count == 0)
            sb.AppendLine("_None yet — no finding on this book has been hand-verified and closed._");
        else
            foreach (var f in handTriaged.OrderByDescending(f => f.ResolvedAt))
                sb.AppendLine($"- **[{f.Category}/{f.Status}]** {Snip(f.Summary)}");
        sb.AppendLine();

        if (autoSuppressed.Count > 0)
        {
            sb.AppendLine("## Auto-suppressed (author exception on file)");
            sb.AppendLine();
            foreach (var f in autoSuppressed.OrderByDescending(f => f.ResolvedAt))
                sb.AppendLine($"- **[{f.SuppressedBy}]** {Snip(f.Summary)}");
            sb.AppendLine();
        }

        sb.AppendLine("## Still open");
        sb.AppendLine();
        if (open.Count == 0)
        {
            sb.AppendLine("_None — every finding on this book has been triaged._");
        }
        else
        {
            foreach (var g in open.GroupBy(f => f.Category).OrderByDescending(g => g.Count()))
                sb.AppendLine($"- **{g.Key}**: {g.Count()} open ({g.Count(f => f.Severity == FindingSeverity.High)} High, " +
                               $"{g.Count(f => f.Severity == FindingSeverity.Medium)} Medium, {g.Count(f => f.Severity == FindingSeverity.Low)} Low)");
        }
        sb.AppendLine();

        sb.AppendLine("## Logged decisions (most recent 10 mentioning this book)");
        sb.AppendLine();
        if (decisions.Count == 0)
            sb.AppendLine("_None found — nothing in the Decision Ledger mentions this book's title or code yet._");
        else
            foreach (var d in decisions)
                sb.AppendLine($"- **{d.At:yyyy-MM-dd}** — {d.Summary}");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string Snip(string s) => s.Length <= 220 ? s : s[..219] + "…";
}
