using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Pre-command cost estimator. Queries <see cref="CommandCostHistory"/> for recent runs
/// of the same command and returns a calibrated estimate (≥3 runs: average × 1.1 buffer)
/// or a static fallback when history is thin.
///
/// Callers record actual cost via <see cref="RecordActualAsync"/> after each command so
/// estimates improve over time. Both methods are non-throwing: failures are logged as
/// warnings and the command proceeds normally.
/// </summary>
public class CommandCostEstimatorService
{
    private static readonly Dictionary<string, double> FallbackEstimates =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["--write-story"]         = 0.12,
            ["--refine-story"]        = 0.08,
            ["--review-node"]         = 0.50,
            ["--review-story"]        = 0.50,
            ["--run-panel"]           = 0.50,
            ["--auto-run"]            = 0.30,
            ["--harvest-voice"]       = 0.06,
            ["--examine-emotion"]     = 0.04,
            ["--causality-check"]     = 0.05,
            ["--affect-check"]        = 0.05,
            ["--interpersonal-check"] = 0.05,
            ["--harvest-entities"]    = 0.08,
            ["--generate-blueprint"]  = 0.04,
            ["--storyscope-audit"]    = 0.05,
        };

    public record CommandCostEstimate(double Estimated, string Confidence, int BasisRuns);

    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<CommandCostEstimatorService> log;

    public CommandCostEstimatorService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<CommandCostEstimatorService> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    public async Task<CommandCostEstimate> EstimateAsync(string commandName, CancellationToken ct = default)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var history = await db.CommandCostHistories
                .AsNoTracking()
                .Where(h => h.CommandName == commandName)
                .OrderByDescending(h => h.RunAt)
                .Take(20)
                .Select(h => h.ActualCost)
                .ToListAsync(ct);

            if (history.Count >= 3)
            {
                var avg = history.Average();
                return new(avg * 1.1, $"historical ({history.Count} runs)", history.Count);
            }
        }
        catch (Exception ex)
        {
            // [SS-CostEst-001] EstimateAsync failed — DB connectivity or CommandCostHistories table missing; run migration.
            log.LogWarning(ex, "[CommandCostEstimatorService] EstimateAsync failed for {Command}, using static fallback", commandName);
        }

        var fallback = FallbackEstimates.TryGetValue(commandName, out var f) ? f : 0.05;
        return new(fallback, "estimated", 0);
    }

    public async Task RecordActualAsync(
        string commandName, double estimatedCost, double actualCost,
        string provider, CancellationToken ct = default)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            db.CommandCostHistories.Add(new CommandCostHistory
            {
                CommandName   = commandName,
                EstimatedCost = estimatedCost,
                ActualCost    = actualCost,
                AccuracyRatio = estimatedCost > 0 ? actualCost / estimatedCost : 0,
                RunAt         = DateTime.UtcNow,
                Provider      = provider,
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // [SS-CostEst-002] RecordActualAsync failed — check DB write permissions and CommandCostHistories table.
            log.LogWarning(ex, "[CommandCostEstimatorService] RecordActualAsync failed for {Command}", commandName);
        }
    }
}
