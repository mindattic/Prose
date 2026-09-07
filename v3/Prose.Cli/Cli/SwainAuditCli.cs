using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI entry for the Swain Scene/Sequel doctrine audit. Report only — the --repair splice mode was deleted 2026-09-06 (RFC 0009).
///
///   prose --swain-audit --slug &lt;slug&gt;               classify all beats; print BLOCKER + MODERATE findings
///   prose --swain-audit --code &lt;code&gt;               same, by NodeCode (e.g. BCODA)
///   prose --swain-audit --all                       audit every non-draft story; print summary table
///
/// Append --blockers to suppress MODERATE findings (show only BLOCKERs).
///
/// Classification:
///   Scene    — goal / conflict / disaster turn all present         → pass
///   Sequel   — reaction / dilemma / decision all present           → pass
///   Ambiguous — one element weak or underwritten                   → MODERATE
///   Deficient — does not execute either pattern; element missing   → BLOCKER
///
/// Haiku classifies; --opus upgrades the classifier to claude-opus-4-8. Nothing here writes prose.
/// for beats that resist multiple Sonnet passes.
/// </summary>
public static class SwainAuditCli
{
    private const string OpusModel = "claude-opus-4-8";

    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var slugArg      = ArgValue(args, "--slug");
        var codeArg      = ArgValue(args, "--code");
        var doAll        = args.Contains("--all");
        var blockersOnly = args.Contains("--blockers");
        var useOpus      = args.Contains("--opus");

        string? classifyModel = useOpus ? OpusModel : null;

        if (slugArg == null && codeArg == null && !doAll)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  prose --swain-audit --slug <slug>               audit one story");
            Console.WriteLine("  prose --swain-audit --code <code>               audit one story by NodeCode");
            Console.WriteLine("  prose --swain-audit --all                       audit all non-draft stories");
            Console.WriteLine("  prose --swain-audit --all    --opus             Opus classify");
            Console.WriteLine();
            Console.WriteLine("  --blockers   suppress MODERATE findings (show BLOCKER only)");
            return 0;
        }

        var svc = sp.GetRequiredService<SwainAuditService>();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        if (useOpus)
            Console.WriteLine($"[Opus mode — classify using {OpusModel}]");

        var failures = 0;
        try
        {
            if (doAll)
            {
                Console.WriteLine("=== Swain audit — all stories ===");
                Console.WriteLine();
                var reports = await svc.AuditAllAsync(classifyModel, cts.Token);
                PrintSummaryTable(reports);

            }
            else
            {
                var key = slugArg ?? codeArg!;
                Console.WriteLine($"=== Swain audit — {key} ===");
                Console.WriteLine();
                SwainAuditReport report;
                try   { report = await svc.AuditAsync(key, classifyModel, cts.Token); }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"✘ {ex.Message}");
                    return 2;
                }
                PrintReport(report, blockersOnly);

            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine();
            Console.WriteLine("Interrupted.");
            return 130;
        }

        return failures > 0 ? 1 : 0;
    }

    // ── Print helpers ─────────────────────────────────────────────────────────

    private static void PrintReport(SwainAuditReport report, bool blockersOnly)
    {
        Console.WriteLine($"{report.Title}  [{report.NodeCode}]");
        Console.WriteLine($"  {report.TotalBeats} beats · {report.PassCount} pass · " +
                          $"{report.ModerateCount} MODERATE · {report.BlockerCount} BLOCKER · " +
                          $"{report.ComplianceRate:P0} compliant");
        Console.WriteLine();

        var findings = blockersOnly
            ? report.Results.Where(r => r.Severity == "BLOCKER")
            : report.Results.Where(r => !r.IsPass);

        foreach (var r in findings)
        {
            var icon = r.Severity == "BLOCKER" ? "✘" : "△";
            Console.WriteLine($"  {icon} Beat {r.Position,4}  [{r.Classification,-9}]  missing: {r.MissingElement}");
            Console.WriteLine($"          {Trunc(r.Title, 70)}");
            Console.WriteLine($"          {r.Note}");
            Console.WriteLine();
        }

        if (!report.Results.Any(r => !r.IsPass))
            Console.WriteLine("  ✔ All beats are Swain-compliant.");
        Console.WriteLine();
    }

    private static void PrintSummaryTable(List<SwainAuditReport> reports)
    {
        const int W = 32;
        Console.WriteLine($"{"Story",-W} {"Code",-8} {"Beats",6} {"Pass",6} {"MOD",5} {"BLK",5} {"Rate",7}");
        Console.WriteLine(new string('─', W + 8 + 6 + 6 + 5 + 5 + 7 + 6));
        foreach (var r in reports.OrderBy(r => r.NodeCode))
        {
            var rate = r.ComplianceRate;
            var rateStr = $"{rate:P0}";
            var flag = r.BlockerCount > 0 ? " ✘" : r.ModerateCount > 0 ? " △" : " ✔";
            Console.WriteLine($"{Trunc(r.Title, W),-W} {r.NodeCode,-8} {r.TotalBeats,6} {r.PassCount,6} {r.ModerateCount,5} {r.BlockerCount,5} {rateStr,7}{flag}");
        }
        Console.WriteLine(new string('─', W + 8 + 6 + 6 + 5 + 5 + 7 + 6));
        var total    = reports.Sum(r => r.TotalBeats);
        var totPass  = reports.Sum(r => r.PassCount);
        var totMod   = reports.Sum(r => r.ModerateCount);
        var totBlk   = reports.Sum(r => r.BlockerCount);
        var totRate  = total > 0 ? $"{(double)totPass / total:P0}" : "—";
        Console.WriteLine($"{"TOTAL",-W} {"",8} {total,6} {totPass,6} {totMod,5} {totBlk,5} {totRate,7}");
        Console.WriteLine();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static string Trunc(string s, int max) =>
        s.Length <= max ? s : string.Concat(s.AsSpan(0, max - 1), "…");
}
