using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI handler for <c>prose --cost</c>.
///
/// In long-running processes (MCP server, Blazor host) the <see cref="TokenLedger"/>
/// accumulates every LLM call. In a one-shot CLI invocation the ledger captures only
/// the calls made during that run — use <c>--cost</c> as a suffix on any command to
/// see how much that operation spent, e.g.:
///   <code>prose --write-node --slug foo --cost</code>
///
/// Usage:
///   prose --cost             print the session cost table
///   prose --cost --json      emit the summary as JSON
///   prose --cost --reset     clear the ledger (no output)
///   prose --cost --history [--command &lt;name&gt;] [--take N]
///                            print the DURABLE CommandCostHistories calibration data
///
/// <para><b>Why <c>--history</c> exists (added 2026-09-04).</b> Every cost-gated command estimates
/// itself from <see cref="CommandCostEstimatorService"/>, which averages the last 20 recorded
/// actuals for that command name — and nothing in the engine could read that table. So when a
/// gate's estimate for <c>--ledger-adjudicate</c> moved from $7.234 to $0.050 between two
/// consecutive runs, there was no way to see whether the history had 2 rows or 20, or what was in
/// them. A calibration input no instrument can inspect is one nobody can tell is wrong; the whole
/// point of the gate is that it warns before an $8 command, and a silently-collapsed estimate
/// stops it doing that.</para>
/// </summary>
public static class CostCli
{
    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var ledger = services.GetRequiredService<TokenLedger>();

        if (args.Contains("--history"))
            return HistoryAsync(args, services);

        if (args.Contains("--reset"))
        {
            ledger.Clear();
            Console.WriteLine("Token ledger cleared.");
            return Task.FromResult(0);
        }

        if (args.Contains("--json"))
        {
            var summary = ledger.GetSummary();
            var json = JsonSerializer.Serialize(new
            {
                sessionStart  = summary.SessionStart,
                callCount     = summary.CallCount,
                inputTokens   = summary.InputTokens,
                outputTokens  = summary.OutputTokens,
                totalCost     = Math.Round(summary.TotalCost, 6),
                byModel = summary.ByModel.Values
                    .OrderByDescending(m => m.TotalCost)
                    .Select(m => new
                    {
                        model        = m.Model,
                        label        = m.Label,
                        callCount    = m.CallCount,
                        inputTokens  = m.InputTokens,
                        outputTokens = m.OutputTokens,
                        totalCost    = Math.Round(m.TotalCost, 6),
                    }),
            }, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(json);
            return Task.FromResult(0);
        }

        Console.WriteLine(ledger.RenderReport());
        return Task.FromResult(0);
    }

    /// <summary>
    /// The durable per-command calibration data — what the cost gate actually estimates from,
    /// grouped the same way <see cref="CommandCostEstimatorService.EstimateAsync"/> reads it
    /// (last 20 rows per command name, averaged ×1.1, and the static fallback below 3 rows).
    /// </summary>
    private static async Task<int> HistoryAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var command = Flag(args, "--command");
        var take = int.TryParse(Flag(args, "--take"), out var t) && t > 0 ? t : 20;

        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.CommandCostHistories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(command))
            q = q.Where(h => h.CommandName.Contains(command));

        var rows = await q.OrderByDescending(h => h.RunAt).Take(500).ToListAsync();

        if (rows.Count == 0)
        {
            Console.WriteLine("  No command cost history recorded"
                + (string.IsNullOrWhiteSpace(command) ? "." : $" for a command matching \"{command}\"."));
            return 0;
        }

        if (args.Contains("--json"))
        {
            Console.WriteLine(JsonSerializer.Serialize(rows.Take(take),
                new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        // Grouped: the estimator is per command name, so per command name is the useful view.
        foreach (var g in rows.GroupBy(r => r.CommandName).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var recent = g.OrderByDescending(r => r.RunAt).Take(20).ToList();
            var basis = recent.Count >= 3
                ? $"historical ({recent.Count} runs) → est ${recent.Average(r => r.ActualCost) * 1.1:F3}"
                : $"only {recent.Count} run(s) — the gate uses its STATIC fallback, not this data";
            Console.WriteLine();
            Console.WriteLine($"  {g.Key}");
            Console.WriteLine($"    {basis}");
            Console.WriteLine($"    actual: min ${recent.Min(r => r.ActualCost):F4}  " +
                              $"max ${recent.Max(r => r.ActualCost):F4}  " +
                              $"median ${Median(recent.Select(r => r.ActualCost)):F4}");
            foreach (var r in recent.Take(take))
                Console.WriteLine($"      {r.RunAt:MM-dd HH:mm}  actual ${r.ActualCost,9:F4}   " +
                                  $"est ${r.EstimatedCost,8:F3}   {r.Provider}");
        }

        Console.WriteLine();
        Console.WriteLine($"  {rows.Count} row(s) across {rows.Select(r => r.CommandName).Distinct().Count()} command(s).");
        return 0;
    }

    private static double Median(IEnumerable<double> values)
    {
        var s = values.OrderBy(v => v).ToList();
        if (s.Count == 0) return 0;
        return s.Count % 2 == 1 ? s[s.Count / 2] : (s[s.Count / 2 - 1] + s[s.Count / 2]) / 2;
    }

    private static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    /// <summary>
    /// Convenience method: print the cost report if <c>--cost</c> is present in args.
    /// Call this at the end of any CLI handler that performs LLM calls.
    /// </summary>
    public static void PrintIfRequested(string[] args, IServiceProvider services)
    {
        if (!args.Contains("--cost")) return;
        var ledger = services.GetRequiredService<TokenLedger>();
        Console.WriteLine();
        Console.WriteLine(ledger.RenderReport());
    }
}
