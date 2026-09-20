using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --retire-bible-title-header --dry-run [--slug &lt;slug&gt;]</c>
/// <c>prose --retire-bible-title-header --apply [--slug &lt;slug&gt;]</c>
///
/// Bible→Outline terminology cleanup: every book whose hand-authored <c>Nodes.NodeOutline</c>
/// was generated before <see cref="NodeOutlineService.BuildBibleSystemPrompt"/> was corrected
/// still opens with the stale header <c>"# NODE BIBLE: [Title]"</c> — the LLM prompt template
/// literally instructed the model to emit that line, so it was baked into the persisted outline
/// text itself, not just a display artifact. <see cref="NodeDocService.ExtractHandAuthored"/> and
/// <see cref="NodeDocService.StripFrontmatter"/> only strip the generated-sections marker and the
/// `related:` frontmatter block respectively — neither touches a leading title line — so the
/// stale header survives every regenerate untouched.
///
/// Unlike LOCKED-marker retirement, a title header has no ambiguous case: it is always the very
/// first line of the hand-authored text, always matches <c>^#\s*NODE BIBLE\s*:\s*(.*)$</c>, and
/// rewriting it to <c>"# Book Context: {title}"</c> (the terminology already used everywhere else
/// — see <see cref="NodeDocService.GenerateAsync"/>'s own placeholder text) can never corrupt
/// surrounding content. There is no MANUAL bucket.
///
/// SCAN (always runs): every book-level node's <c>Nodes.NodeOutline</c> is checked for a leading
/// stale header. APPLY (only with --apply): rewrites it via
/// <see cref="CanonDocumentService.SetNodeOutlineSectionAsync"/> (section "Full" — the single
/// choke point that also re-tags entities on save), regenerates that node's doc
/// (<see cref="NodeDocService.GenerateAsync"/>), then re-syncs every markdown mirror
/// (<see cref="MarkdownFileService.SyncAllAsync"/>) once, after all books are done.
/// </summary>
public static class RetireBibleTitleHeaderCli
{
    // Case-insensitive by design — see RetireLockedMarkersCli's own comment on the same lesson
    // (a same-day corpus sweep found a lowercase "locked" variant the original ALL-CAPS-only
    // pattern missed). Anchored to the very start of the text: GenerateAndSaveAsync persists
    // bibleText.Trim() with no frontmatter, so the header is always line 1 when present.
    // [ \t]* (not \s*) around/after the colon — \s also matches newlines, which would let an
    // empty title greedily swallow the blank line and the start of the next section header.
    private static readonly Regex HeaderPattern = new(
        @"\A#[ \t]*NODE BIBLE[ \t]*:[ \t]*(?<title>[^\r\n]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    internal sealed record BookScan(Guid NodeId, string Slug, string Title, string Match);

    /// <summary>Exposed internally so the classification/rewrite logic is directly
    /// unit-testable without a database.</summary>
    internal static bool TryRewrite(string text, out string rewritten, out string matchedTitle)
    {
        var m = HeaderPattern.Match(text);
        if (!m.Success)
        {
            rewritten = text;
            matchedTitle = "";
            return false;
        }

        matchedTitle = m.Groups["title"].Value.Trim();
        rewritten = HeaderPattern.Replace(text, $"# Book Context: {matchedTitle}", 1);
        return true;
    }

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        var apply = args.Contains("--apply");
        var slug = Flag(args, "--slug");

        if (!dryRun && !apply)
        {
            Console.Error.WriteLine("Usage: prose --retire-bible-title-header --dry-run [--slug <slug>]");
            Console.Error.WriteLine("       prose --retire-bible-title-header --apply [--slug <slug>]");
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
                ? "[retire-bible-title-header] No book-level nodes with a NodeOutline found."
                : $"[retire-bible-title-header] No book-level node with slug '{slug}' (or it has no NodeOutline).");
            return 1;
        }

        var scans = new List<BookScan>();
        foreach (var b in books)
        {
            if (TryRewrite(b.NodeOutline!, out _, out var matchedTitle))
                scans.Add(new BookScan(b.Id, b.Slug, b.Title, matchedTitle));
        }

        var reportPath = WriteReport(scans);
        Console.WriteLine($"[retire-bible-title-header] {scans.Count} book(s) with a stale \"# NODE BIBLE:\" header. Report: {reportPath}");

        if (!apply)
        {
            Console.WriteLine("[retire-bible-title-header] Dry-run only — nothing written. Review the report, then re-run with --apply.");
            return 0;
        }

        if (scans.Count == 0)
        {
            Console.WriteLine("[retire-bible-title-header] Nothing to apply.");
            return 0;
        }

        var canonDocs = services.GetRequiredService<CanonDocumentService>();
        var nodeDocs = services.GetRequiredService<NodeDocService>();
        var markdownSync = services.GetRequiredService<MarkdownFileService>();

        var touched = 0;
        foreach (var scan in scans)
        {
            var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == scan.NodeId);
            if (node?.NodeOutline == null) continue;

            if (!TryRewrite(node.NodeOutline, out var rewritten, out _)) continue;

            await canonDocs.SetNodeOutlineSectionAsync(scan.NodeId, "Full", rewritten);
            await nodeDocs.GenerateAsync(scan.NodeId);
            Console.WriteLine($"[retire-bible-title-header] '{scan.Title}' ({scan.Slug}): header rewritten.");
            touched++;
        }

        if (touched > 0)
        {
            await markdownSync.SyncAllAsync();
            Console.WriteLine($"[retire-bible-title-header] Done. {touched} book(s) updated, markdown mirrors re-synced.");
        }

        return 0;
    }

    private static string WriteReport(List<BookScan> scans)
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), $"audit-outlines-{DateTime.UtcNow:yyyy-MM-dd}", "bible-title-header");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "RETIRE-BIBLE-HEADER-DRYRUN.md");

        var sb = new StringBuilder();
        sb.AppendLine($"# Stale \"NODE BIBLE\" title header retirement — dry-run report ({DateTime.UtcNow:yyyy-MM-dd})");
        sb.AppendLine();
        sb.AppendLine("Every occurrence below is auto-transformable by `--apply` — a title header has no " +
            "ambiguous case, unlike LOCKED-marker retirement. `\"# NODE BIBLE: <title>\"` becomes " +
            "`\"# Book Context: <title>\"`.");
        sb.AppendLine();
        foreach (var scan in scans.OrderBy(s => s.Title))
            sb.AppendLine($"- **{scan.Title}** (`{scan.Slug}`) — matched title: \"{scan.Match}\"");
        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
