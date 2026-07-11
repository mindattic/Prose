using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --compute-metrics [--slug &lt;slug&gt; | --all]
///
/// Computes and upserts per-beat prose quality metrics (sentence stats, TTR,
/// MTLD, Flesch-Kincaid, dialogue proportion) for one story or every story.
/// CPU-only — no LLM or API calls. Safe to re-run; results are upserted.
/// Exit 0 = success.
/// </summary>
public static class BeatProseMetricsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var service = sp.GetRequiredService<BeatProseMetricsService>();
        var slug    = args.SkipWhile(a => a != "--slug").Skip(1).FirstOrDefault();
        var all     = args.Contains("--all");

        if (!all && slug == null)
        {
            Console.Error.WriteLine("Usage: ss --compute-metrics --slug <slug>   compute metrics for one story");
            Console.Error.WriteLine("       ss --compute-metrics --all           compute metrics for all enabled beats");
            return 1;
        }

        BeatProseMetricsReport report;
        if (slug != null)
        {
            Console.WriteLine($"[metrics] Computing prose metrics for: {slug}");
            report = await service.ComputeSlugAsync(slug);
        }
        else
        {
            Console.WriteLine("[metrics] Computing prose metrics for all enabled beats...");
            report = await service.ComputeAllAsync();
        }

        Console.WriteLine();
        Console.WriteLine($"  Beats processed   : {report.BeatCount}");
        Console.WriteLine($"  Mean TTR          : {report.MeanTtr:F3}");
        Console.WriteLine($"  Mean Flesch Ease  : {report.MeanFleschReadingEase:F1}");
        Console.WriteLine($"  Mean FK Grade     : {report.MeanFleschKincaidGrade:F1}");
        Console.WriteLine($"  Mean Words/Sent   : {report.MeanAvgWordsPerSentence:F1}");
        Console.WriteLine($"  Outliers          : {report.Outliers.Count}");

        if (report.Outliers.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Outlier beats (low TTR or low readability):");
            foreach (var o in report.Outliers.Take(20))
            {
                var flags = new List<string>();
                if (o.LowTtr)        flags.Add($"TTR={o.TypeTokenRatio:F3}");
                if (o.LowReadability) flags.Add($"Flesch={o.FleschReadingEase:F1}");
                Console.WriteLine($"  beat:{o.BeatId}  {string.Join(", ", flags)}");
            }
            if (report.Outliers.Count > 20)
                Console.WriteLine($"  ... and {report.Outliers.Count - 20} more.");
        }

        return 0;
    }
}
