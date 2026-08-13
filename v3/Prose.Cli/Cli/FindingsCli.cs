using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI surface for the autonomous quality findings inbox.
///
///   prose --findings list [--status new|triaged|applied|dismissed]   List findings.
///   prose --findings stats                                            Counts per status.
///   prose --findings show &lt;id&gt;                                       Full detail for one finding.
///   prose --findings apply &lt;id&gt;                                      Apply the suggested fix to the source file.
///   prose --findings dismiss &lt;id&gt;                                    Mark dismissed.
///   prose --findings triage &lt;id&gt;                                     Mark triaged.
///   prose --findings scan &lt;file-path&gt;                                Manually trigger a quality scan on a chapter file.
///   prose --findings bulk-dismiss [--category &lt;cat&gt;] [--prefix &lt;text&gt;]  Dismiss every open finding
///                                                                      matching the filter(s). At least
///                                                                      one filter is required.
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
            "list"         => CmdList(rest, store),
            "stats"        => CmdStats(store),
            "show"         => CmdShow(rest, store),
            "apply"        => await CmdApply(rest, services),
            "dismiss"      => CmdSetStatus(rest, store, FindingStatus.Dismissed),
            "triage"       => CmdSetStatus(rest, store, FindingStatus.Triaged),
            "scan"         => await CmdScan(rest, services),
            "bulk-dismiss" => await CmdBulkDismiss(rest, store),
            _              => Fail($"unknown subcommand: {sub}"),
        };
    }

    static int Fail(string msg) { Console.Error.WriteLine($"[findings] {msg}"); PrintUsage(); return 1; }

    static int CmdList(string[] rest, FindingsService store)
    {
        FindingStatus? filter = null;
        var sIdx = Array.IndexOf(rest, "--status");
        if (sIdx >= 0 && sIdx + 1 < rest.Length
            && Enum.TryParse<FindingStatus>(rest[sIdx + 1], ignoreCase: true, out var parsed))
            filter = parsed;

        var items = store.List(filter, limit: 200);
        if (items.Count == 0)
        {
            Console.WriteLine($"[findings] none{(filter is null ? "" : $" with status {filter}")}.");
            return 0;
        }
        foreach (var f in items)
        {
            Console.WriteLine($"#{f.Id,-5} {f.Severity,-6} {f.Category,-13} {f.Status,-9} {f.DetectedAt.ToLocalTime():MM-dd HH:mm}  {Truncate(f.Summary, 90)}");
        }
        return 0;
    }

    static int CmdStats(FindingsService store)
    {
        Console.WriteLine($"[findings] new:       {store.CountByStatus(FindingStatus.New)}");
        Console.WriteLine($"[findings] triaged:   {store.CountByStatus(FindingStatus.Triaged)}");
        Console.WriteLine($"[findings] applied:   {store.CountByStatus(FindingStatus.Applied)}");
        Console.WriteLine($"[findings] dismissed: {store.CountByStatus(FindingStatus.Dismissed)}");
        return 0;
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

    static async Task<int> CmdBulkDismiss(string[] rest, FindingsService store)
    {
        string? categoryArg = null, prefix = null;
        var cIdx = Array.IndexOf(rest, "--category");
        if (cIdx >= 0 && cIdx + 1 < rest.Length) categoryArg = rest[cIdx + 1];
        var pIdx = Array.IndexOf(rest, "--prefix");
        if (pIdx >= 0 && pIdx + 1 < rest.Length) prefix = rest[pIdx + 1];

        FindingCategory? category = null;
        if (categoryArg != null)
        {
            if (!Enum.TryParse<FindingCategory>(categoryArg, ignoreCase: true, out var parsed))
                return Fail($"unknown category: {categoryArg}");
            category = parsed;
        }

        if (category is null && string.IsNullOrWhiteSpace(prefix))
            return Fail("bulk-dismiss requires --category and/or --prefix");

        var n = await store.BulkSetStatusAsync(FindingStatus.Dismissed, category, prefix);
        Console.WriteLine($"[findings] dismissed {n} finding(s)"
            + (category is null ? "" : $" [category={category}]")
            + (string.IsNullOrWhiteSpace(prefix) ? "" : $" [prefix=\"{prefix}\"]"));
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
        Console.WriteLine("  prose --findings list [--status new|triaged|applied|dismissed]");
        Console.WriteLine("  prose --findings stats");
        Console.WriteLine("  prose --findings show <id>");
        Console.WriteLine("  prose --findings apply <id>");
        Console.WriteLine("  prose --findings triage <id>");
        Console.WriteLine("  prose --findings dismiss <id>");
        Console.WriteLine("  prose --findings scan <file-path>");
        Console.WriteLine("  prose --findings bulk-dismiss [--category <cat>] [--prefix <text>]");
    }
}
