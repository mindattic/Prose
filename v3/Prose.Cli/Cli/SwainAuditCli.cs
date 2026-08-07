using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI entry for Swain Scene/Sequel doctrine audit and repair.
///
///   ss --swain-audit --slug &lt;slug&gt;               classify all beats; print BLOCKER + MODERATE findings
///   ss --swain-audit --code &lt;code&gt;               same, by NodeCode (e.g. BCODA)
///   ss --swain-audit --all                       audit every non-draft story; print summary table
///   ss --swain-audit --slug &lt;slug&gt; --repair      auto-splice missing elements for all BLOCKER beats
///   ss --swain-audit --all    --repair           bulk repair across all stories
///   ss --swain-audit --all    --repair --opus    use Opus for both classify and splice (stubborn beats)
///
/// Append --blockers to suppress MODERATE findings (show only BLOCKERs).
///
/// Classification:
///   Scene    — goal / conflict / disaster turn all present         → pass
///   Sequel   — reaction / dilemma / decision all present           → pass
///   Ambiguous — one element weak or underwritten                   → MODERATE
///   Deficient — does not execute either pattern; element missing   → BLOCKER
///
/// Repair: Haiku classifies → Sonnet splices. --opus upgrades both to claude-opus-4-8
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
        var doRepair     = args.Contains("--repair");
        var blockersOnly = args.Contains("--blockers");
        var useOpus      = args.Contains("--opus");

        string? classifyModel = useOpus ? OpusModel : null;
        string? spliceModel   = useOpus ? OpusModel : null;

        if (slugArg == null && codeArg == null && !doAll)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  ss --swain-audit --slug <slug>               audit one story");
            Console.WriteLine("  ss --swain-audit --code <code>               audit one story by NodeCode");
            Console.WriteLine("  ss --swain-audit --all                       audit all non-draft stories");
            Console.WriteLine("  ss --swain-audit --slug <slug> --repair      audit + splice BLOCKERs");
            Console.WriteLine("  ss --swain-audit --all    --repair           bulk repair all stories");
            Console.WriteLine("  ss --swain-audit --all    --repair --opus    Opus classify + splice");
            Console.WriteLine();
            Console.WriteLine("  --blockers   suppress MODERATE findings (show BLOCKER only)");
            return 0;
        }

        var svc = sp.GetRequiredService<SwainAuditService>();
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        if (useOpus)
            Console.WriteLine($"[Opus mode — classify + splice using {OpusModel}]");

        var failures = 0;
        try
        {
            if (doAll)
            {
                Console.WriteLine("=== Swain audit — all stories ===");
                Console.WriteLine();
                var reports = await svc.AuditAllAsync(classifyModel, cts.Token);
                PrintSummaryTable(reports);

                if (doRepair)
                {
                    var withBlockers = reports.Where(r => r.BlockerCount > 0).ToList();
                    if (withBlockers.Count == 0)
                    {
                        Console.WriteLine("No BLOCKER findings — nothing to repair.");
                    }
                    else
                    {
                        var totalBlockers = withBlockers.Sum(r => r.BlockerCount);
                        Console.WriteLine($"Repairing {totalBlockers} BLOCKER(s) across {withBlockers.Count} story/stories.");
                        Console.WriteLine();
                        foreach (var report in withBlockers)
                            failures += await RepairAsync(svc, report, spliceModel, cts.Token);
                    }
                }
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

                if (doRepair)
                {
                    if (report.BlockerCount == 0)
                        Console.WriteLine("No BLOCKER findings — nothing to repair.");
                    else
                        failures += await RepairAsync(svc, report, spliceModel, cts.Token);
                }
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

    // ── Repair ────────────────────────────────────────────────────────────────

    private static async Task<int> RepairAsync(
        SwainAuditService svc, SwainAuditReport report, string? spliceModel, CancellationToken ct)
    {
        var blockers = report.Results.Where(r => r.Severity == "BLOCKER").ToList();
        if (blockers.Count == 0) return 0;

        Console.WriteLine($"── Repairing {blockers.Count} BLOCKER(s) in {report.NodeCode} ──");
        var failures = 0;

        foreach (var finding in blockers)
        {
            ct.ThrowIfCancellationRequested();
            Console.Write($"  Beat {finding.Position,4}: {Trunc(finding.Title, 50)}");
            Console.Write($"  (missing: {finding.MissingElement}) … ");

            var beatText = await svc.LoadBeatTextAsync(finding.BeatId, ct);
            if (beatText == null)
            {
                Console.WriteLine("✘ load failed");
                failures++;
                continue;
            }

            var before = beatText.Length;
            var spliced = await svc.SpliceAsync(finding, beatText, spliceModel, ct);
            if (spliced == null)
            {
                Console.WriteLine("✘ splice failed");
                failures++;
                continue;
            }

            var ok = await svc.ApplySpliceAsync(finding, spliced, ct);
            if (ok)
                Console.WriteLine($"✔ +{spliced.Length - before} chars");
            else
            {
                Console.WriteLine("✘ apply failed");
                failures++;
            }
        }

        Console.WriteLine($"  Done: {blockers.Count - failures} repaired, {failures} failed.");
        Console.WriteLine();
        return failures;
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
