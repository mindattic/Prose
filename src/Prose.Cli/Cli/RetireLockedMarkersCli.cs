using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --retire-locked-markers --dry-run [--slug &lt;slug&gt;]</c>
/// <c>prose --retire-locked-markers --apply [--slug &lt;slug&gt;]</c>
///
/// Bible→Outline refactor Phase 6a (author ruling 2026-08-29, decision #3): the LOCK concept is
/// retired entirely — no corner of the Outline⇄Book⇄Entities symbiosis has automatic authority,
/// so a "LOCKED" marker's implied "this cannot be overridden" no longer means anything real. Every
/// historical "LOCKED yyyy-mm-dd" annotation becomes a plain dated author note instead.
///
/// This is a DRY-RUN-FIRST tool by design (plan's own words: "not blind regex"). Two passes:
///
/// 1. SCAN (always runs): every book-level node's hand-authored <c>Nodes.NodeOutline</c> is
///    searched for the bare word LOCKED anywhere, with surrounding context, then bucketed:
///      - SAFE: the marker sits alone inside its own parenthetical — "(LOCKED)", "(LOCKED
///        yyyy-mm-dd)", "(LOCKED - never resolved)" — nothing else sharing those parens. The
///        rewrite is mechanical and cannot corrupt the surrounding sentence.
///      - MANUAL: everything else (a bare mid-sentence "LOCKED.", a "LOCKED SCENE" heading, a
///        compound parenthetical like "(AUTHORITATIVE - LOCKED yyyy-mm-dd; row 7 amended ...)",
///        an adjectival "the LOCKED LINE") — these need a human to rephrase the sentence around
///        them, not a regex guessing at grammar. Never touched by --apply.
///
/// 2. APPLY (only with --apply): rewrites ONLY the SAFE occurrences, via
///    <see cref="CanonDocumentService.SetNodeOutlineSectionAsync"/> (section "Full" — the single
///    choke point that also re-tags entities on save), then regenerates that node's doc
///    (<see cref="NodeDocService.GenerateAsync"/>) and finally re-syncs every markdown mirror
///    (<see cref="MarkdownFileService.SyncAllAsync"/>) once, after all books are done.
///
/// The author reviews the dry-run report before ever running --apply — see the report's own
/// MANUAL section for what still needs a hand edit afterward.
/// </summary>
public static class RetireLockedMarkersCli
{
    // Case-insensitive (fixed 2026-08-30): a same-day live corpus sweep found lowercase "locked"
    // section-heading qualifiers (e.g. "## Theme (locked - see brief §8)") in a book the original
    // ALL-CAPS-only pattern never surfaced — the doctrine word is retired regardless of case.
    private static readonly Regex ScanPattern = new(@"\bLOCKED\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Matches ONLY a parenthetical whose entire content, after "LOCKED", is nothing but an
    // optional dash + date or an optional dash + "never resolved" — never a compound parenthetical
    // that happens to mention LOCKED alongside other text (that stays MANUAL by simply not matching).
    private static readonly Regex SafeParenPattern = new(
        @"\(LOCKED\s*(?<inner>.*?)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DateOnly = new(@"^[-–—]?\s*(?<date>\d{4}-\d{2}-\d{2})$", RegexOptions.Compiled);
    private static readonly Regex NeverResolved = new(@"^[-–—]?\s*never resolved$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal sealed record BookScan(Guid NodeId, string Slug, string Title, List<string> SafeMatches, List<string> ManualMatches);

    /// <summary>Classifies every LOCKED occurrence in <paramref name="text"/> into SAFE (a bare
    /// parenthetical containing only LOCKED + optional date/"never resolved") vs MANUAL
    /// (everything else). Exposed internally so the classification logic — the part where a
    /// wrong call risks corrupting real narrative content — is directly unit-testable without a
    /// database.</summary>
    internal static BookScan ClassifyText(Guid nodeId, string slug, string title, string text)
    {
        var safe = new List<string>();
        var manual = new List<string>();

        var safeRanges = SafeParenPattern.Matches(text)
            .Where(m => IsSafeInner(m.Groups["inner"].Value))
            .Select(m => (m.Index, End: m.Index + m.Length))
            .ToList();

        foreach (Match m in ScanPattern.Matches(text))
        {
            var context = Context(text, m.Index, 70);
            if (safeRanges.Any(r => m.Index >= r.Index && m.Index < r.End))
                safe.Add(context);
            else
                manual.Add(context);
        }

        return new BookScan(nodeId, slug, title, safe, manual);
    }

    /// <summary>Applies the SAFE-only rewrite to <paramref name="text"/> — the same regex the live
    /// --apply path uses, exposed for direct testing.</summary>
    internal static string ApplySafeRewrite(string text) =>
        SafeParenPattern.Replace(text, m =>
        {
            var inner = m.Groups["inner"].Value;
            if (!IsSafeInner(inner)) return m.Value;
            if (string.IsNullOrWhiteSpace(inner)) return "(author decision)";
            var dateMatch = DateOnly.Match(inner);
            if (dateMatch.Success) return $"(author decision, {dateMatch.Groups["date"].Value})";
            if (NeverResolved.IsMatch(inner)) return "(author decision — never resolved)";
            return m.Value;
        });

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        var apply = args.Contains("--apply");
        var slug = Flag(args, "--slug");

        if (!dryRun && !apply)
        {
            Console.Error.WriteLine("Usage: prose --retire-locked-markers --dry-run [--slug <slug>]");
            Console.Error.WriteLine("       prose --retire-locked-markers --apply [--slug <slug>]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var query = db.Nodes.IgnoreQueryFilters().Where(n => n.Kind == "book" && n.NodeOutline != null);
        if (!string.IsNullOrWhiteSpace(slug)) query = query.Where(n => n.Slug == slug);
        var books = await query.Select(n => new { n.Id, n.Slug, n.Title, n.NodeOutline }).OrderBy(n => n.Title).ToListAsync();

        if (books.Count == 0)
        {
            Console.Error.WriteLine(string.IsNullOrWhiteSpace(slug)
                ? "[retire-locked-markers] No book-level nodes with a NodeOutline found."
                : $"[retire-locked-markers] No book-level node with slug '{slug}' (or it has no NodeOutline).");
            return 1;
        }

        var scans = new List<BookScan>();
        foreach (var b in books)
        {
            var text = b.NodeOutline!;
            if (!ScanPattern.IsMatch(text)) continue;
            var scan = ClassifyText(b.Id, b.Slug, b.Title, text);
            if (scan.SafeMatches.Count > 0 || scan.ManualMatches.Count > 0)
                scans.Add(scan);
        }

        var reportPath = WriteReport(scans);
        var totalSafe = scans.Sum(s => s.SafeMatches.Count);
        var totalManual = scans.Sum(s => s.ManualMatches.Count);
        Console.WriteLine($"[retire-locked-markers] {scans.Count} book(s) with LOCKED marker(s): " +
            $"{totalSafe} safe (auto-transformable), {totalManual} need manual author rewrite. Report: {reportPath}");

        if (!apply)
        {
            Console.WriteLine("[retire-locked-markers] Dry-run only — nothing written. Review the report, then re-run with --apply.");
            return 0;
        }

        if (totalSafe == 0)
        {
            Console.WriteLine("[retire-locked-markers] No SAFE occurrences to apply. MANUAL ones (if any) need a hand edit via set_book_outline.");
            return 0;
        }

        var canonDocs = services.GetRequiredService<CanonDocumentService>();
        var nodeDocs = services.GetRequiredService<NodeDocService>();
        var markdownSync = services.GetRequiredService<MarkdownFileService>();

        var touched = 0;
        foreach (var scan in scans.Where(s => s.SafeMatches.Count > 0))
        {
            var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == scan.NodeId);
            if (node?.NodeOutline == null) continue;

            var rewritten = ApplySafeRewrite(node.NodeOutline);
            if (rewritten == node.NodeOutline) continue;

            await canonDocs.SetNodeOutlineSectionAsync(scan.NodeId, "Full", rewritten);
            await nodeDocs.GenerateAsync(scan.NodeId);
            Console.WriteLine($"[retire-locked-markers] '{scan.Title}' ({scan.Slug}): {scan.SafeMatches.Count} marker(s) converted.");
            touched++;
        }

        if (touched > 0)
        {
            await markdownSync.SyncAllAsync();
            Console.WriteLine($"[retire-locked-markers] Done. {touched} book(s) updated, markdown mirrors re-synced.");
        }
        if (totalManual > 0)
            Console.WriteLine($"[retire-locked-markers] {totalManual} MANUAL occurrence(s) remain — see the report; hand-edit via set_book_outline.");

        return 0;
    }

    private static bool IsSafeInner(string inner) =>
        string.IsNullOrWhiteSpace(inner) || DateOnly.IsMatch(inner) || NeverResolved.IsMatch(inner);

    private static string Context(string text, int index, int radius)
    {
        var start = Math.Max(0, index - radius);
        var end = Math.Min(text.Length, index + radius);
        var snippet = text[start..end].Replace('\n', ' ').Replace('\r', ' ');
        return (start > 0 ? "…" : "") + snippet.Trim() + (end < text.Length ? "…" : "");
    }

    private static string WriteReport(List<BookScan> scans)
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), $"audit-outlines-{DateTime.UtcNow:yyyy-MM-dd}", "locked-markers");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "RETIRE-LOCKED-DRYRUN.md");

        var sb = new StringBuilder();
        sb.AppendLine($"# LOCKED marker retirement — dry-run report ({DateTime.UtcNow:yyyy-MM-dd})");
        sb.AppendLine();
        sb.AppendLine("Bible→Outline refactor Phase 6a. SAFE occurrences are auto-transformable by " +
            "`--apply` (a bare parenthetical containing only LOCKED + optional date/\"never resolved\"). " +
            "MANUAL occurrences need a human to rephrase the surrounding sentence and are never touched " +
            "by `--apply` — fix them directly via `set_book_outline`.");
        sb.AppendLine();
        foreach (var scan in scans.OrderByDescending(s => s.ManualMatches.Count + s.SafeMatches.Count))
        {
            sb.AppendLine($"## {scan.Title} (`{scan.Slug}`)");
            if (scan.SafeMatches.Count > 0)
            {
                sb.AppendLine($"**SAFE ({scan.SafeMatches.Count}):**");
                foreach (var m in scan.SafeMatches) sb.AppendLine($"- {m}");
            }
            if (scan.ManualMatches.Count > 0)
            {
                sb.AppendLine($"**MANUAL ({scan.ManualMatches.Count}):**");
                foreach (var m in scan.ManualMatches) sb.AppendLine($"- {m}");
            }
            sb.AppendLine();
        }
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
