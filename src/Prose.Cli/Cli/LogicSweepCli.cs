using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Services.Audit;

namespace Prose.Cli;

/// <summary>
/// prose --logic-sweep --slug &lt;nodeSlug&gt; [--json]
/// prose --logic-sweep --slug &lt;nodeSlug&gt; --until-dry [--required-dry N] [--max-rounds N] [--json]
///
/// See LogicSweepService's class doc for the honest scope note: this is a single-pass
/// approximation, not a replacement for the full /logic-sweep skill on a large book.
///
/// --until-dry runs ONE round of the loop-until-dry convergence campaign (see
/// LogicSweepService.RunConvergenceRoundAsync) and reports whether to keep going (fix the
/// findings, run this again) or stop (converged, or the safety cap escalated as its own
/// finding). This is the replacement for "run the sweep N times regardless of what it found" —
/// call it again after each fix pass, across as many turns/sessions as it takes; the campaign
/// state persists in NodeConvergenceStates between calls.
/// </summary>
public static class LogicSweepCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool jsonMode = args.Contains("--json");
        bool untilDry = args.Contains("--until-dry");
        var requiredDry = LogicSweepService.DefaultRequiredDryRounds;
        var maxRounds   = LogicSweepService.DefaultMaxTotalRounds;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
            else if (args[i] == "--required-dry" && int.TryParse(args[i + 1], out var rd)) { requiredDry = rd; i++; }
            else if (args[i] == "--max-rounds" && int.TryParse(args[i + 1], out var mr)) { maxRounds = mr; i++; }
        }
        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --logic-sweep --slug <nodeSlug> [--until-dry] [--required-dry N] [--max-rounds N] [--json]");
            return 2;
        }

        var svc = services.GetRequiredService<LogicSweepService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        if (untilDry)
            return await RunConvergenceRoundAsync(svc, node.Id, slug, requiredDry, maxRounds, jsonMode);

        if (!jsonMode)
            Console.WriteLine($"Logic sweep: '{node.Title}' — 6 dimensions…\n");

        var report = await svc.RunAsync(node.Id);

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_id = report.NodeId,
                slug,
                title = report.NodeTitle,
                beat_count = report.BeatCount,
                clean = report.Clean,
                blocker_count = report.BlockerCount,
                moderate_count = report.ModerateCount,
                minor_count = report.MinorCount,
                findings = report.Findings.Select(f => new
                {
                    dimension = f.RuleKey,
                    severity = f.Severity,
                    evidence = f.Evidence,
                    fix = f.Fix,
                }),
            }, new JsonSerializerOptions { WriteIndented = true }));
            return report.BlockerCount > 0 ? 2 : report.Findings.Count > 0 ? 1 : 0;
        }

        if (report.Clean)
        {
            Console.WriteLine($"✓ Clean — {report.BeatCount} beats, no findings across all 6 dimensions.");
            return 0;
        }

        Console.WriteLine($"{report.Findings.Count} finding(s) — {report.BlockerCount} BLOCKER, " +
            $"{report.ModerateCount} MODERATE, {report.MinorCount} MINOR/DEVIATION\n");
        foreach (var f in report.Findings.OrderByDescending(f => f.Severity == "BLOCKER" ? 2 : f.Severity == "MODERATE" ? 1 : 0))
        {
            var icon = f.Severity switch { "BLOCKER" => "✗", "MODERATE" => "△", _ => "·" };
            Console.WriteLine($"  {icon} [{f.RuleKey}] {f.Severity}");
            Console.WriteLine($"      {f.Evidence}");
            if (!string.IsNullOrEmpty(f.Fix))
                Console.WriteLine($"      Fix: {f.Fix}");
            Console.WriteLine();
        }
        return report.BlockerCount > 0 ? 2 : 1;
    }

    private static async Task<int> RunConvergenceRoundAsync(
        LogicSweepService svc, Guid nodeId, string slug, int requiredDry, int maxRounds, bool jsonMode)
    {
        var round = await svc.RunConvergenceRoundAsync(nodeId, requiredDry, maxRounds);

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_id = round.NodeId,
                slug,
                skipped = round.Skipped,
                converged = round.Converged,
                hit_safety_cap = round.HitSafetyCap,
                consecutive_dry_rounds = round.ConsecutiveDryRounds,
                total_rounds_run = round.TotalRoundsRun,
                message = round.Message,
                findings_this_round = round.Report?.Findings.Select(f => new
                {
                    dimension = f.RuleKey,
                    severity = f.Severity,
                    evidence = f.Evidence,
                    fix = f.Fix,
                }),
            }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine(round.Message);
            if (round.Report != null && round.Report.Findings.Count > 0)
            {
                foreach (var f in round.Report.Findings.OrderByDescending(f => f.Severity == "BLOCKER" ? 2 : f.Severity == "MODERATE" ? 1 : 0))
                {
                    var icon = f.Severity switch { "BLOCKER" => "✗", "MODERATE" => "△", _ => "·" };
                    Console.WriteLine($"  {icon} [{f.RuleKey}] {f.Severity}");
                    Console.WriteLine($"      {f.Evidence}");
                    if (!string.IsNullOrEmpty(f.Fix))
                        Console.WriteLine($"      Fix: {f.Fix}");
                }
            }
        }

        // Exit code contract: 0 = converged (or already-converged skip) — nothing left to do.
        // 1 = keep going (fix the findings, run this again). 2 = safety cap hit — needs a
        // structural rewrite, not another fix pass, before retrying.
        return round.Converged ? 0 : round.HitSafetyCap ? 2 : 1;
    }
}
