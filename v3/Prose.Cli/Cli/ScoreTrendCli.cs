using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// prose --score-trend [--batches N] [--universe &lt;slug&gt;]
///
/// Prints the rolling mean reader score across chronological batches of nodes
/// so the flywheel's direction is visible from the CLI (SS-US-J6 / SS-US-F10).
///
/// Batches: all nodes with at least one score-history record are ordered
/// by their first RecordedAt, split into N equal groups, then the mean score
/// per group is printed. A positive Δ from batch to batch confirms the voice-
/// harvest flywheel is spinning forward.
///
/// Exit codes: 0 = positive trend, 1 = flat/negative, 2 = not enough data.
/// </summary>
public static class ScoreTrendCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        int batches = 2;
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--batches" && int.TryParse(args[i + 1], out var n) && n >= 2)
                batches = n;

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // Aggregate: for each node, take its latest score-history record.
        // Then order all nodes by that RecordedAt (earliest = oldest work).
        // Joining through db.Nodes (not just db.NodeScoreHistories) matters: Nodes carries the
        // ambient-universe EF query filter and NodeScoreHistory does not, so starting from the
        // history table alone silently ignored --universe / PROSE_UNIVERSE entirely — every
        // universe returned the identical unfiltered set.
        var rows = await db.Nodes.AsNoTracking()
            .Join(db.NodeScoreHistories.AsNoTracking(), n => n.Id, h => h.NodeId, (n, h) => h)
            .GroupBy(h => h.NodeId)
            .Select(g => new
            {
                NodeId    = g.Key,
                EarliestAt  = g.Min(h => h.RecordedAt),
                LatestScore = g.OrderByDescending(h => h.RecordedAt).Select(h => h.MeanScore).FirstOrDefault(),
            })
            .OrderBy(r => r.EarliestAt)
            .ToListAsync();

        if (rows.Count < batches)
        {
            Console.WriteLine($"Not enough scored nodes ({rows.Count}) to split into {batches} batches.");
            Console.WriteLine("Run more review panels, then retry.");
            return 2;
        }

        int batchSize = (int)Math.Ceiling(rows.Count / (double)batches);
        Console.WriteLine($"Score trend — {rows.Count} scored node(s), {batches} batch(es)\n");

        var header = $"{"Batch",-7} {"Nodes",-9} {"Mean Score",-12} {"Δ vs prior"}";
        Console.WriteLine(header);
        Console.WriteLine(new string('─', header.Length));

        double? prevMean = null;
        int exitCode = 0;

        for (int b = 0; b < batches; b++)
        {
            var slice = rows.Skip(b * batchSize).Take(batchSize).ToList();
            if (slice.Count == 0) break;

            var mean = slice.Average(r => r.LatestScore);
            var earliest = slice[0].EarliestAt.ToString("yyyy-MM-dd");
            var latest   = slice[^1].EarliestAt.ToString("yyyy-MM-dd");

            string delta;
            if (prevMean == null)
            {
                delta = "—";
            }
            else
            {
                var d = mean - prevMean.Value;
                delta = d >= 0 ? $"+{d:F1}" : $"{d:F1}";
                if (d < 0) exitCode = 1;
            }

            string label = b == 0 ? $"{earliest}+" : $"{earliest}…{latest}";
            Console.WriteLine($"  {b + 1,-5}  {slice.Count,-8}  {mean,9:F1}    {delta}");
            prevMean = mean;
        }

        Console.WriteLine();
        if (exitCode == 0 && prevMean.HasValue)
            Console.WriteLine("Trend: ▲ flywheel is spinning forward.");
        else if (exitCode == 1)
            Console.WriteLine("Trend: ▼ scores declining — check recent harvests for bad directives.");
        else
            Console.WriteLine("Trend: → flat or single-batch data.");

        return exitCode;
    }
}
