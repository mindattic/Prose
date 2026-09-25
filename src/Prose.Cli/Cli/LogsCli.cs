using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --logs</c> — the engine-error triage queue.
///
/// <para>The Hub's Serilog files already hold every error with 14 days of history, but reading
/// them gives you volume, not a work list: one broken code path writes hundreds of identical
/// lines. This groups them into distinct faults, lets you queue the ones worth fixing, and
/// re-checks "fixed" against the logs instead of trusting the flag.</para>
///
/// <list type="table">
/// <item><term>--logs</term><description>distinct faults in the last 24h, newest first</description></item>
/// <item><term>--logs --since 2h|3d</term><description>widen the window (default 24h, max useful 14d)</description></item>
/// <item><term>--logs --level Warning</term><description>default is Error; warnings are noise for a fix queue</description></item>
/// <item><term>--logs --search "circuit"</term><description>free text over message and exception</description></item>
/// <item><term>--logs --detail &lt;sig&gt;</term><description>full message + stack for one fault</description></item>
/// <item><term>--logs --raw</term><description>ungrouped lines, for when grouping hides what you need</description></item>
/// <item><term>--logs --track &lt;sig&gt; [--note "..."]</term><description>queue it to fix</description></item>
/// <item><term>--logs --issues [--all]</term><description>the queue; --all includes resolved/ignored</description></item>
/// <item><term>--logs --resolve &lt;id&gt; [--note "..."]</term><description>call it fixed (the logs get a vote)</description></item>
/// <item><term>--logs --ignore &lt;id&gt;</term><description>stop reporting it without claiming a fix</description></item>
/// <item><term>--logs --reopen &lt;id&gt;</term><description>put it back on the queue</description></item>
/// </list>
///
/// <para>Free: reads log files and one small table. No LLM call.</para>
/// </summary>
public static class LogsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? since = null, level = null, search = null, note = null, track = null, detail = null;
        long? resolve = null, ignore = null, reopen = null;
        var showIssues = args.Contains("--issues");
        var includeClosed = args.Contains("--all");
        var raw = args.Contains("--raw");
        var limit = 40;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--since":   if (i + 1 < args.Length) since = args[++i]; break;
                case "--level":   if (i + 1 < args.Length) level = args[++i]; break;
                case "--search":  if (i + 1 < args.Length) search = args[++i]; break;
                case "--note":    if (i + 1 < args.Length) note = args[++i]; break;
                case "--track":   if (i + 1 < args.Length) track = args[++i]; break;
                case "--detail":  if (i + 1 < args.Length) detail = args[++i]; break;
                case "--limit":   if (i + 1 < args.Length && int.TryParse(args[++i], out var l)) limit = l; break;
                case "--resolve": if (i + 1 < args.Length && long.TryParse(args[++i], out var r)) resolve = r; break;
                case "--ignore":  if (i + 1 < args.Length && long.TryParse(args[++i], out var g)) ignore = g; break;
                case "--reopen":  if (i + 1 < args.Length && long.TryParse(args[++i], out var o)) reopen = o; break;
            }
        }

        var svc = services.GetRequiredService<LogIssueService>();

        // ── State changes ───────────────────────────────────────────────────
        if (resolve != null) return await ChangeAsync(svc.ResolveAsync(resolve.Value, note), "resolved", resolve.Value);
        if (ignore  != null) return await ChangeAsync(svc.IgnoreAsync(ignore.Value, note),   "ignored",  ignore.Value);
        if (reopen  != null) return await ChangeAsync(svc.ReopenAsync(reopen.Value, note),   "reopened", reopen.Value);

        // ── The queue ───────────────────────────────────────────────────────
        if (showIssues)
        {
            var views = await svc.ListAsync(includeClosed);
            if (views.Count == 0)
            {
                Console.WriteLine(includeClosed
                    ? "[logs] Nothing queued yet. Run `prose --logs` to see what is failing, then --track <sig>."
                    : "[logs] Nothing open. (`--all` to include resolved and ignored.)");
                return 0;
            }

            // The count is deliberately labelled with its window. This view re-groups over the
            // whole retention period and includes warnings, so that a fault which comes back at a
            // different level still trips regression detection — which means its N legitimately
            // differs from the N in `prose --logs`, where you chose the window and level. Two
            // numbers for the same signature with no explanation reads as a bug.
            Console.WriteLine($"{"ID",-5} {"STATE",-10} {"LAST SEEN",-17} {$"N/{LogIssueService.RetentionDays}d",-6} TITLE");
            Console.WriteLine(new string('-', 110));
            foreach (var v in views)
            {
                var last = v.Fault == null ? "-" : v.Fault.LastSeen.ToString("MM-dd HH:mm:ss");
                var n    = v.Fault == null ? "-" : v.Fault.Count.ToString();
                Console.WriteLine($"{v.Issue.Id,-5} {StateLabel(v.State),-10} {last,-17} {n,-6} {Clip(v.Issue.Title, 60)}");

                if (v.State == IssueState.Regressed)
                    Console.WriteLine($"      ^ marked fixed {v.Issue.ResolvedAt:MM-dd HH:mm} but logged again since — the fix did not hold.");
                else if (v.State == IssueState.Resolved && !v.Verifiable)
                    Console.WriteLine($"      ^ resolved {v.Issue.ResolvedAt:yyyy-MM-dd}, older than the {LogIssueService.RetentionDays}-day log window — " +
                                       "no evidence either way now.");
                else if (v.State == IssueState.Open && v.Fault == null)
                    Console.WriteLine($"      ^ nothing in the last {LogIssueService.RetentionDays} days — may already be fixed; --resolve {v.Issue.Id} to close it.");

                if (!string.IsNullOrWhiteSpace(v.Issue.Note))
                    Console.WriteLine($"      note: {Clip(v.Issue.Note!, 90)}");
            }

            var regressed = views.Count(v => v.State == IssueState.Regressed);
            Console.WriteLine();
            Console.WriteLine($"[logs] {views.Count} issue(s)" + (regressed > 0 ? $" — {regressed} REGRESSED" : "") + ". " +
                              $"Counts span the full {LogIssueService.RetentionDays}-day log window, warnings included.");
            return 0;
        }

        // ── Faults from the log files ───────────────────────────────────────
        var window = ParseSince(since) ?? DateTime.Now.AddDays(-1);
        var faults = svc.GroupFaults(window, level ?? "Error", search);

        // --detail / --track both address a fault by signature prefix.
        if (detail != null || track != null)
        {
            var wanted = (detail ?? track)!;
            var match = faults.FirstOrDefault(f => f.Signature.StartsWith(wanted, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                Console.Error.WriteLine($"[logs] No fault matching '{wanted}' in this window. " +
                    "Widen it with --since 7d, or run `prose --logs` to list what is there.");
                return 1;
            }

            if (track != null)
            {
                var issue = await svc.TrackAsync(match, note);
                Console.WriteLine($"[logs] Queued as issue #{issue.Id}: {Clip(issue.Title, 80)}");
                Console.WriteLine($"       Mark it fixed later with: prose --logs --resolve {issue.Id} --note \"what you changed\"");
                return 0;
            }

            Console.WriteLine($"[logs] {match.Signature}  {match.Level}  x{match.Count}");
            Console.WriteLine($"       first {match.FirstSeen:yyyy-MM-dd HH:mm:ss}   last {match.LastSeen:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
            Console.WriteLine(match.Sample.Message);
            if (!string.IsNullOrWhiteSpace(match.Sample.Exception))
            {
                Console.WriteLine();
                Console.WriteLine(match.Sample.Exception);
            }
            Console.WriteLine();
            Console.WriteLine($"       Queue it:  prose --logs --track {match.Signature}");
            return 0;
        }

        if (raw)
        {
            var svcLogs = services.GetRequiredService<LoggingService>();
            var entries = svcLogs.Search(new LogSearchRequest
            {
                Since = window, MinSeverity = level ?? "Error", SearchText = search, MaxResults = limit,
            });
            foreach (var e in entries.OrderByDescending(e => e.Timestamp).Take(limit))
                Console.WriteLine($"{e.Timestamp:MM-dd HH:mm:ss} [{e.Level,-11}] {Clip(e.Message, 140)}");
            Console.WriteLine($"\n[logs] {entries.Count} line(s) since {window:yyyy-MM-dd HH:mm}.");
            return 0;
        }

        if (faults.Count == 0)
        {
            Console.WriteLine($"[logs] No {(level ?? "Error").ToLowerInvariant()}-level entries since {window:yyyy-MM-dd HH:mm}. " +
                              "Widen with --since 7d, or lower with --level Warning.");
            return 0;
        }

        // Which of these are already known, so a triaged fault is not re-triaged.
        var known = (await svc.ListAsync(includeClosed: true))
            .ToDictionary(v => v.Issue.Signature, v => v);

        Console.WriteLine($"{"SIG",-12} {"LEVEL",-11} {"N",-6} {"LAST SEEN",-17} TITLE");
        Console.WriteLine(new string('-', 110));
        foreach (var f in faults.Take(limit))
        {
            var mark = known.TryGetValue(f.Signature, out var v) ? $"#{v.Issue.Id} {StateLabel(v.State)}" : "";
            Console.WriteLine($"{f.Signature,-12} {f.Level,-11} {f.Count,-6} {f.LastSeen:MM-dd HH:mm:ss}   {Clip(f.Title, 52)}");
            if (mark.Length > 0) Console.WriteLine($"             ^ already queued as {mark}");
        }

        Console.WriteLine();
        Console.WriteLine($"[logs] {faults.Count} distinct fault(s) since {window:yyyy-MM-dd HH:mm}, " +
                          $"{faults.Sum(f => f.Count)} line(s) in total.");
        Console.WriteLine($"       Inspect:  prose --logs --detail {faults[0].Signature}");
        Console.WriteLine($"       Queue:    prose --logs --track  {faults[0].Signature}");
        Console.WriteLine($"       Queue so far: prose --logs --issues");
        return 0;
    }

    private static async Task<int> ChangeAsync(Task<Prose.Core.Data.Entities.LogIssue?> op, string verb, long id)
    {
        var issue = await op;
        if (issue == null)
        {
            Console.Error.WriteLine($"[logs] No issue #{id}. List them with: prose --logs --issues --all");
            return 1;
        }
        Console.WriteLine($"[logs] Issue #{issue.Id} {verb}: {Clip(issue.Title, 80)}");
        if (verb == "resolved")
            Console.WriteLine("       If it is logged again, it comes back as REGRESSED — you do not have to remember to check.");
        return 0;
    }

    /// <summary>Accepts 90m / 6h / 3d, or anything DateTime.TryParse understands.</summary>
    private static DateTime? ParseSince(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;

        var trimmed = s.Trim();
        var unit = char.ToLowerInvariant(trimmed[^1]);
        if (unit is 'm' or 'h' or 'd' && double.TryParse(trimmed[..^1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var qty))
            return unit switch
            {
                'm' => DateTime.Now.AddMinutes(-qty),
                'h' => DateTime.Now.AddHours(-qty),
                _   => DateTime.Now.AddDays(-qty),
            };

        return DateTime.TryParse(trimmed, out var abs) ? abs : null;
    }

    private static string StateLabel(IssueState s) => s switch
    {
        IssueState.Regressed => "REGRESSED",
        IssueState.Open      => "open",
        IssueState.Resolved  => "resolved",
        _                    => "ignored",
    };

    private static string Clip(string s, int max) =>
        string.IsNullOrEmpty(s) ? "" :
        s.Length <= max ? s.ReplaceLineEndings(" ") : s.ReplaceLineEndings(" ")[..max] + "…";
}
