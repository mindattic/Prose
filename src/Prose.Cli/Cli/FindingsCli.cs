using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI surface for the autonomous quality findings inbox.
///
///   prose --findings list [--status new|triaged|applied|dismissed] [--node &lt;slug-or-code&gt;]
///                         [--file-prefix &lt;raw FilePath prefix&gt;] [--limit N]
///                                                                    --node resolves a LIVE node
///                                                                    to "node:{slug}".
///                                                                    --file-prefix takes that
///                                                                    path prefix raw, so rows
///                                                                    belonging to a renamed or
///                                                                    deleted node stay reachable.
///                                                                    NB --prefix (bulk-dismiss)
///                                                                    matches the SUMMARY;
///                                                                    --file-prefix matches the
///                                                                    PATH.
///                                                                    List findings, optionally
///                                                                    scoped to one book. Without
///                                                                    --node, thousands of
///                                                                    High-severity findings from
///                                                                    other books can bury a
///                                                                    single book's own (esp.
///                                                                    Medium-severity) findings
///                                                                    past the default limit.
///   prose --findings stats [--by-instrument]                           Counts per status; --by-instrument
///                                                                     adds the value-audit rollup (findings
///                                                                     filed vs ever applied, per producer).
///   prose --findings show &lt;id&gt;                                       Full detail for one finding.
///   prose --findings apply &lt;id&gt;                                      Apply the suggested fix to the source file.
///   prose --findings dismiss &lt;id&gt;                                    Mark dismissed.
///   prose --findings triage &lt;id&gt;                                     Mark triaged.
///   prose --findings scan &lt;file-path&gt;                                Manually trigger a quality scan on a chapter file.
///   prose --findings bulk-dismiss [--category &lt;cat&gt;] [--prefix &lt;text&gt;] [--node &lt;slug-or-code&gt;]
///                                                                      Dismiss every open finding
///                                                                      matching the filter(s). At
///                                                                      least one filter is required.
/// </summary>
public static class FindingsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var idx = Array.FindIndex(args, a => a == "--findings");
        if (idx < 0 || idx + 1 >= args.Length) { PrintUsage(); return 1; }

        var sub  = args[idx + 1].ToLowerInvariant();
        var rest = args[(idx + 2)..];
        var store = services.GetRequiredService<FindingsService>();

        return sub switch
        {
            "list"         => await CmdList(rest, store, services),
            "stats"        => CmdStats(store, rest.Contains("--by-instrument")),
            "show"         => CmdShow(rest, store),
            "apply"        => await CmdApply(rest, services),
            "dismiss"      => CmdSetStatus(rest, store, FindingStatus.Dismissed),
            "triage"       => CmdSetStatus(rest, store, FindingStatus.Triaged),
            "scan"         => await CmdScan(rest, services),
            "bulk-dismiss" => await CmdBulkDismiss(rest, store, services),
            _              => Fail($"unknown subcommand: {sub}"),
        };
    }

    static int Fail(string msg) { Console.Error.WriteLine($"[findings] {msg}"); PrintUsage(); return 1; }

    /// <summary>Resolves a node ref (GUID, Slug, or NodeCode) to its Findings-table FilePath
    /// prefix ("node:{slug}") — same "accept id or slug" convention ContinuityCli's --node uses,
    /// extended to also accept NodeCode since that's the identifier most users actually know
    /// (see ExportNodeCli's documented Slug-only gotcha).</summary>
    static async Task<string?> ResolveNodeFilePathPrefixAsync(string nodeRef, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        // Guid.TryParse can't run inside an EF expression-tree lambda — resolve it first,
        // then branch, same pattern ContinuityCli's --node handling already uses.
        var node = Guid.TryParse(nodeRef, out var nodeId)
            ? await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId)
            : await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(n => n.Slug == nodeRef || n.NodeCode == nodeRef);
        if (node is null) return null;
        return $"node:{(string.IsNullOrEmpty(node.Slug) ? node.Id.ToString("N") : node.Slug)}";
    }

    static async Task<int> CmdList(string[] rest, FindingsService store, IServiceProvider services)
    {
        FindingStatus? filter = null;
        var sIdx = Array.IndexOf(rest, "--status");
        if (sIdx >= 0 && sIdx + 1 < rest.Length
            && Enum.TryParse<FindingStatus>(rest[sIdx + 1], ignoreCase: true, out var parsed))
            filter = parsed;

        string? filePathPrefix = null;
        var nIdx = Array.IndexOf(rest, "--node");
        if (nIdx >= 0 && nIdx + 1 < rest.Length)
        {
            var nodeRef = rest[nIdx + 1];
            filePathPrefix = await ResolveNodeFilePathPrefixAsync(nodeRef, services);
            if (filePathPrefix is null) return Fail($"node not found: {nodeRef}");
        }
        // --file-prefix: the raw FilePath prefix, for the case --node cannot serve — a node that
        // has been RENAMED or DELETED. Findings keep the FilePath they were written with, so a
        // book's own history accumulates under every slug it has ever had, and the rows belonging
        // to generations that no longer exist are unreachable by --node (it resolves live nodes
        // only). Added 2026-09-22 after "node:bushido-coda" was found to prefix-match both the
        // live book's rows and four deleted clones, making --node unusable as a dismiss filter.
        var fpIdx = Array.IndexOf(rest, "--file-prefix");
        if (fpIdx >= 0 && fpIdx + 1 < rest.Length) filePathPrefix = rest[fpIdx + 1];

        var limit = 200;
        var lIdx = Array.IndexOf(rest, "--limit");
        if (lIdx >= 0 && lIdx + 1 < rest.Length && int.TryParse(rest[lIdx + 1], out var parsedLimit))
            limit = parsedLimit;

        var items = store.List(filter, limit, filePathPrefix);
        if (items.Count == 0)
        {
            Console.WriteLine($"[findings] none{(filter is null ? "" : $" with status {filter}")}{(filePathPrefix is null ? "" : $" for {filePathPrefix}")}.");
            return 0;
        }
        foreach (var f in items)
        {
            Console.WriteLine($"#{f.Id,-5} {f.Severity,-6} {f.Category,-13} {f.Status,-9} {f.DetectedAt.ToLocalTime():MM-dd HH:mm}  {Truncate(f.Summary, 90)}");
        }
        return 0;
    }

    static int CmdStats(FindingsService store, bool byInstrument = false)
    {
        Console.WriteLine($"[findings] new:       {store.CountByStatus(FindingStatus.New)}");
        Console.WriteLine($"[findings] triaged:   {store.CountByStatus(FindingStatus.Triaged)}");
        Console.WriteLine($"[findings] applied:   {store.CountByStatus(FindingStatus.Applied)}");
        Console.WriteLine($"[findings] dismissed: {store.CountByStatus(FindingStatus.Dismissed)}");
        if (!byInstrument) return 0;

        // The value audit's table. Applied is the column that matters: an instrument with a large
        // New count and a zero Applied count has produced a landfill, not a backlog.
        Console.WriteLine();
        Console.WriteLine($"{"INSTRUMENT",-22}{"TOTAL",8}{"NEW",8}{"APPLIED",9}{"DISMISSED",11}  APPLY RATE");
        foreach (var (instrument, total, @new, applied, dismissed) in store.StatsByInstrument())
            Console.WriteLine($"{Trim(instrument, 21),-22}{total,8}{@new,8}{applied,9}{dismissed,11}  " +
                              $"{(total == 0 ? 0 : 100.0 * applied / total):0.000}%");
        return 0;

        static string Trim(string s, int n) => s.Length <= n ? s : s[..n];
    }

    static int CmdShow(string[] rest, FindingsService store)
    {
        if (rest.Length == 0 || !long.TryParse(rest[0], out var id)) return Fail("missing id");
        var f = store.Get(id);
        if (f is null) return Fail($"finding #{id} not found");

        Console.WriteLine($"#{f.Id}  [{f.Severity}] [{f.Category}] [{f.Status}]");
        Console.WriteLine($"file:        {f.FilePath}");
        if (!string.IsNullOrEmpty(f.ChapterId)) Console.WriteLine($"chapter id:  {f.ChapterId}");
        Console.WriteLine($"detected:    {f.DetectedAt.ToLocalTime():g}");
        if (f.ResolvedAt is not null) Console.WriteLine($"resolved:    {f.ResolvedAt.Value.ToLocalTime():g}");
        Console.WriteLine();
        Console.WriteLine($"summary:     {f.Summary}");
        if (!string.IsNullOrWhiteSpace(f.Snippet))      Console.WriteLine($"snippet:     {f.Snippet}");
        if (!string.IsNullOrWhiteSpace(f.SuggestedFix)) Console.WriteLine($"fix:         {f.SuggestedFix}");
        return 0;
    }

    static async Task<int> CmdApply(string[] rest, IServiceProvider services)
    {
        if (rest.Length == 0 || !long.TryParse(rest[0], out var id)) return Fail("missing id");
        var apply = services.GetRequiredService<FindingApplyService>();
        var result = await apply.ApplyAsync(id);
        Console.WriteLine($"[findings] {result.Outcome}{(result.Detail is null ? "" : $" — {result.Detail}")}");
        return result.Outcome == ApplyOutcome.Applied ? 0 : 1;
    }

    static int CmdSetStatus(string[] rest, FindingsService store, FindingStatus status)
    {
        if (rest.Length == 0 || !long.TryParse(rest[0], out var id)) return Fail("missing id");
        store.SetStatus(id, status);
        Console.WriteLine($"[findings] #{id} → {status}");
        return 0;
    }

    static async Task<int> CmdBulkDismiss(string[] rest, FindingsService store, IServiceProvider services)
    {
        string? categoryArg = null, prefix = null, nodeRef = null;
        var cIdx = Array.IndexOf(rest, "--category");
        if (cIdx >= 0 && cIdx + 1 < rest.Length) categoryArg = rest[cIdx + 1];
        var pIdx = Array.IndexOf(rest, "--prefix");
        if (pIdx >= 0 && pIdx + 1 < rest.Length) prefix = rest[pIdx + 1];
        var nIdx = Array.IndexOf(rest, "--node");
        if (nIdx >= 0 && nIdx + 1 < rest.Length) nodeRef = rest[nIdx + 1];

        FindingCategory? category = null;
        if (categoryArg != null)
        {
            if (!Enum.TryParse<FindingCategory>(categoryArg, ignoreCase: true, out var parsed))
                return Fail($"unknown category: {categoryArg}");
            category = parsed;
        }

        string? filePathPrefix = null;
        if (nodeRef != null)
        {
            filePathPrefix = await ResolveNodeFilePathPrefixAsync(nodeRef, services);
            if (filePathPrefix is null) return Fail($"node not found: {nodeRef}");
        }
        // See CmdList: --file-prefix reaches rows whose node has been renamed or deleted, which
        // --node cannot. Note --prefix matches the SUMMARY and --file-prefix matches the PATH;
        // they are not interchangeable, and the difference matters when sweeping dead rows.
        var fpIdx2 = Array.IndexOf(rest, "--file-prefix");
        if (fpIdx2 >= 0 && fpIdx2 + 1 < rest.Length) filePathPrefix = rest[fpIdx2 + 1];

        if (category is null && string.IsNullOrWhiteSpace(prefix) && filePathPrefix is null)
            return Fail("bulk-dismiss requires --category and/or --prefix and/or --node and/or --file-prefix");

        var n = await store.BulkSetStatusAsync(FindingStatus.Dismissed, category, prefix, filePathPrefix);
        Console.WriteLine($"[findings] dismissed {n} finding(s)"
            + (category is null ? "" : $" [category={category}]")
            + (string.IsNullOrWhiteSpace(prefix) ? "" : $" [prefix=\"{prefix}\"]")
            + (filePathPrefix is null ? "" : $" [node={filePathPrefix}]"));
        return 0;
    }

    static async Task<int> CmdScan(string[] rest, IServiceProvider services)
    {
        if (rest.Length == 0) return Fail("missing file path");
        var path = rest[0];
        if (!File.Exists(path)) return Fail($"file not found: {path}");
        var monitor = services.GetRequiredService<ContinuousQualityService>();
        Console.WriteLine($"[findings] scanning {path}…");
        await monitor.AnalyzeFileAsync(path);
        Console.WriteLine("[findings] scan complete; new findings (if any) are in the inbox.");
        return 0;
    }

    static string Truncate(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s : max <= 1 ? "…" : s.Substring(0, max - 1) + "…";

    static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  prose --findings list [--status new|triaged|applied|dismissed] [--node <slug-or-code>] [--limit <n>]");
        Console.WriteLine("  prose --findings stats [--by-instrument]");
        Console.WriteLine("  prose --findings show <id>");
        Console.WriteLine("  prose --findings apply <id>");
        Console.WriteLine("  prose --findings triage <id>");
        Console.WriteLine("  prose --findings dismiss <id>");
        Console.WriteLine("  prose --findings scan <file-path>");
        Console.WriteLine("  prose --findings bulk-dismiss [--category <cat>] [--prefix <text>] [--node <slug-or-code>]");
    }
}
