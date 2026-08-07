using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// Pre-command cost gate: queries <see cref="CommandCostEstimatorService"/> for an estimate,
/// prompts y/n when the estimate exceeds <see cref="DefaultThreshold"/>, and records actual
/// cost after the command finishes so estimates self-calibrate over time.
///
/// Usage pattern in Program.cs:
/// <code>
///   var sp = BuildCoreServices(args);
///   var (proceed, estimate) = await CostGateCli.ConfirmAsync("--write-story", args, sp);
///   if (!proceed) return;
///   var costBefore = CostGateCli.SnapshotCost(sp);
///   Environment.ExitCode = await TheCommandCli.RunAsync(args, sp);
///   await CostGateCli.RecordActualAsync("--write-story", estimate, costBefore, sp);
/// </code>
///
/// Pass <c>--no-confirm</c> to bypass the y/n prompt without skipping cost recording.
/// </summary>
public static class CostGateCli
{
    private const double DefaultThreshold = 0.10;

    public static async Task<(bool Proceed, CommandCostEstimatorService.CommandCostEstimate? Estimate)> ConfirmAsync(
        string commandName, string[] args, IServiceProvider sp,
        double threshold = DefaultThreshold)
    {
        var estimator = sp.GetRequiredService<CommandCostEstimatorService>();
        var estimate  = await estimator.EstimateAsync(commandName);

        if (args.Contains("--no-confirm") || estimate.Estimated < threshold)
            return (true, estimate);

        Console.WriteLine();
        Console.WriteLine($"  Command  : {commandName}");
        Console.WriteLine($"  Est cost : ${estimate.Estimated:F3}  ({estimate.Confidence})");
        Console.Write("  Proceed? [y/n]: ");
        var key = Console.ReadKey(intercept: false);
        Console.WriteLine();

        return (key.KeyChar is 'y' or 'Y', estimate);
    }

    public static double SnapshotCost(IServiceProvider sp)
        => sp.GetRequiredService<TokenLedger>().GetSummary().TotalCost;

    public static async Task RecordActualAsync(
        string commandName,
        CommandCostEstimatorService.CommandCostEstimate? estimate,
        double costBefore,
        IServiceProvider sp)
    {
        try
        {
            var ledger     = sp.GetRequiredService<TokenLedger>();
            var actualCost = ledger.GetSummary().TotalCost - costBefore;
            if (actualCost <= 0) return;

            var estimator = sp.GetRequiredService<CommandCostEstimatorService>();
            var settings  = sp.GetRequiredService<SettingsService>();
            await estimator.RecordActualAsync(
                commandName,
                estimate?.Estimated ?? 0,
                actualCost,
                settings.ActiveLlmProvider ?? "claude-api");

            if (estimate is { Estimated: > 0 })
            {
                var delta = actualCost - estimate.Estimated;
                var pct   = delta / estimate.Estimated * 100;
                Console.WriteLine($"  Cost: ${actualCost:F4} actual  (est ${estimate.Estimated:F3}, {(delta >= 0 ? "+" : "")}{pct:F0}%)");
            }
        }
        catch (Exception ex)
        {
            // [SS-CostGate-001] RecordActualAsync failed — check CommandCostEstimatorService and DB connectivity.
            Console.Error.WriteLine($"[CostGate] Warning: failed to record actual cost — {ex.Message}");
        }
    }
}
