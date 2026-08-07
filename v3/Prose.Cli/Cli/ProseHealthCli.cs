using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

public static class ProseHealthCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var slug = ArgValue(args, "--slug");
        var json = args.Contains("--json");
        var outDir = ArgValue(args, "--out");

        var health = services.GetRequiredService<NightlyHealthService>();

        Console.WriteLine(slug != null
            ? $"Prose health: analysing '{slug}'…"
            : "Prose health: analysing all non-WIP stories…");

        var report = await health.RunAsync(slug);

        // ── Console summary ───────────────────────────────────────────────
        if (!json)
        {
            Console.WriteLine();
            Console.WriteLine($"Stories: {report.BooksAnalyzed}  |  Beats: {report.BeatsAnalyzed}  |  API calls: 0");
            Console.WriteLine();

            PrintTier("RISK TIER 1 — fix before next review", report.Tier1, ConsoleColor.Red);
            PrintTier("RISK TIER 2 — worth a read",          report.Tier2, ConsoleColor.Yellow);
            PrintTier("RISK TIER 3 — low signal",            report.Tier3, ConsoleColor.DarkGray);

            if (report.BookMeanPredictedScore.Count > 0)
            {
                Console.WriteLine("kNN Predictions (unscored stories):");
                foreach (var (s, mean) in report.BookMeanPredictedScore.OrderBy(kv => kv.Value))
                    Console.WriteLine($"  {s,-20} mean predicted {mean:F1}");
                Console.WriteLine();
            }

            if (report.Warnings.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                foreach (var w in report.Warnings) Console.WriteLine($"⚠ {w}");
                Console.ResetColor();
            }
        }

        // ── Markdown report ───────────────────────────────────────────────
        var md = NightlyHealthService.FormatReportMarkdown(report);
        var dir = outDir
            ?? Path.Combine("audit-outlines-" + report.RunAt.ToString("yyyyMMdd"), "health");
        Directory.CreateDirectory(dir);
        var fileName = slug != null ? $"{slug}-prose-health.md" : "prose-health.md";
        var filePath = Path.Combine(dir, fileName);
        await File.WriteAllTextAsync(filePath, md);
        Console.WriteLine($"Report written: {filePath}");

        return report.Tier1.Count > 0 ? 1 : 0;
    }

    private static void PrintTier(string header, IReadOnlyList<BeatHealthRecord> tier, ConsoleColor colour)
    {
        if (tier.Count == 0) return;
        Console.ForegroundColor = colour;
        Console.WriteLine($"{header} ({tier.Count})");
        Console.ResetColor();
        foreach (var r in tier)
        {
            var title = r.Title is { Length: > 0 } t ? ("\"" + t + "\"") : ("#" + r.BeatNumber);
            var signals = BuildSignalLine(r);
            Console.WriteLine($"  {r.NodeCode ?? r.NodeSlug,-6} #{r.BeatNumber,-4} {title,-35}  {signals}");
        }
        Console.WriteLine();
    }

    private static string BuildSignalLine(BeatHealthRecord r)
    {
        var parts = new List<string>();
        if (r.PredictedScore.HasValue)                       parts.Add($"kNN={r.PredictedScore:F0}");
        if (r.OutlierSigmas is > 1.5)                        parts.Add($"outlier={r.OutlierSigmas:+0.0}σ");
        if (r.AdverbDensity > 0.05)                          parts.Add($"adv={r.AdverbDensity:P0}");
        if (r.PassiveCount > 3)                              parts.Add($"pass={r.PassiveCount}");
        if (r.TellingCount > 3)                              parts.Add($"tell={r.TellingCount}");
        if (r.AdjacentMonotonous)                            parts.Add("mono");
        if (r.AdjacentJarring)                               parts.Add("jarring");
        return string.Join("  ", parts);
    }

    private static string? ArgValue(string[] args, string flag)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == flag) return args[i + 1];
        return null;
    }
}
