using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --beat-granularity [--slug &lt;slug&gt; | --code &lt;code&gt; | --all]
///
/// Analyses beat-size distribution against the 4,000–7,500 char (~800–1,500 word)
/// optimal dramatic-scene range. Labels each beat as OK / SPLIT / MERGE and prints
/// per-story stats. Use results to plan which beats to split before the next review cycle.
/// </summary>
public static class BeatGranularityCli
{
    private const int ColW = 58;   // console table row width

    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var service = sp.GetRequiredService<BeatGranularityService>();
        var slug = args.SkipWhile(a => a != "--slug").Skip(1).FirstOrDefault()
                ?? args.SkipWhile(a => a != "--code").Skip(1).FirstOrDefault();
        var all  = args.Contains("--all");
        var full = args.Contains("--beats");   // show per-beat table even for --all

        if (!all && slug is null)
        {
            Console.Error.WriteLine("Usage: ss --beat-granularity --slug <slug>    one story by slug");
            Console.Error.WriteLine("       ss --beat-granularity --code <code>    one story by NodeCode");
            Console.Error.WriteLine("       ss --beat-granularity --all            all stories (summary table)");
            Console.Error.WriteLine("       ss --beat-granularity --all --beats    all stories + per-beat detail");
            return 1;
        }

        if (slug is not null)
        {
            var report = await service.AnalyzeAsync(slug);
            if (report is null) { Console.Error.WriteLine($"Story not found: {slug}"); return 1; }
            PrintReport(report, showBeats: true);
        }
        else
        {
            var reports = await service.AnalyzeAllAsync();
            PrintAllSummary(reports, showBeats: full);
        }

        return 0;
    }

    // ── Renderers ─────────────────────────────────────────────────────────────

    private static void PrintReport(BeatGranularityReport r, bool showBeats)
    {
        Console.WriteLine(new string('━', ColW));
        Console.WriteLine($"  BEAT GRANULARITY — {r.NodeCode} · {r.Title}");
        Console.WriteLine($"  Beats: {r.Beats.Count}  ·  Avg: {r.AvgChars:N0} chars  ·  StdDev: {r.StdDevChars:N0}");
        Console.WriteLine($"  Optimal range: {BeatGranularityService.OptimalMinChars:N0}–{BeatGranularityService.OptimalMaxChars:N0} chars");
        Console.WriteLine($"  Target beat count: {r.EstimatedOptimalCount}");
        Console.WriteLine(new string('─', ColW));
        Console.WriteLine($"  {"Status",-8} {"OK",4} {"SPLIT",6} {"MERGE",6}");
        Console.WriteLine($"  {"Count",-8} {r.OkCount,4} {r.SplitCount,6} {r.MergeCount,6}");

        if (r.ScoreStats is { } ss)
        {
            Console.WriteLine(new string('─', ColW));
            Console.WriteLine($"  Review signal/noise ({ss.TotalBallots} total scores)");
            Console.WriteLine($"    InterBeatSD : {ss.InterBeatSd:F3}  (beat-to-beat score spread)");
            Console.WriteLine($"    BallotSD    : {ss.BallotSd:F3}  (per-voter noise)");
            Console.WriteLine($"    F           : {ss.F:F3}  (>1 = signal dominates noise)");
            Console.WriteLine($"    SNR@100     : {ss.Snr100:F1}  (>3 = reliable weak-beat detection)");
        }

        if (!showBeats || r.Beats.Count == 0) { Console.WriteLine(new string('━', ColW)); return; }

        Console.WriteLine(new string('─', ColW));
        Console.WriteLine($"  {"#",4}  {"Label",-6}  {"Chars",7}  {"Words",5}  Title");
        Console.WriteLine(new string('─', ColW));

        foreach (var e in r.Beats)
        {
            var label = e.Label switch
            {
                BeatSizeLabel.Split => "SPLIT",
                BeatSizeLabel.Merge => "MERGE",
                _                  => "ok",
            };
            var titleTrunc = (e.Title ?? "—").Length > 34 ? (e.Title ?? "—")[..31] + "…" : (e.Title ?? "—");
            Console.WriteLine($"  {e.Position,4}  {label,-6}  {e.CharCount,7:N0}  {e.WordCount,5:N0}  {titleTrunc}");
        }

        Console.WriteLine(new string('━', ColW));
    }

    private static void PrintAllSummary(List<BeatGranularityReport> reports, bool showBeats)
    {
        Console.WriteLine(new string('━', ColW));
        Console.WriteLine("  BEAT GRANULARITY — ALL STORIES");
        Console.WriteLine($"  Optimal: {BeatGranularityService.OptimalMinChars:N0}–{BeatGranularityService.OptimalMaxChars:N0} chars  " +
                          $"(target midpoint {(BeatGranularityService.OptimalMinChars + BeatGranularityService.OptimalMaxChars) / 2:N0})");
        Console.WriteLine(new string('─', ColW));
        Console.WriteLine($"  {"CODE",-6}  {"Beats",5}  {"Avg",7}  {"OK",4}  {"SPLIT",5}  {"MERGE",5}  {"→Target",7}");
        Console.WriteLine(new string('─', ColW));

        int totalSplit = 0, totalMerge = 0;
        foreach (var r in reports)
        {
            totalSplit += r.SplitCount;
            totalMerge += r.MergeCount;
            var arrow = r.EstimatedOptimalCount != r.Beats.Count
                ? $"{r.Beats.Count}→{r.EstimatedOptimalCount}"
                : $"{r.Beats.Count}";
            Console.WriteLine($"  {r.NodeCode,-6}  {r.Beats.Count,5}  {r.AvgChars,7:N0}  " +
                              $"{r.OkCount,4}  {r.SplitCount,5}  {r.MergeCount,5}  {arrow,7}");

            if (showBeats && r.Beats.Count > 0)
                PrintReport(r, showBeats: true);
        }

        Console.WriteLine(new string('─', ColW));
        Console.WriteLine($"  Totals: {totalSplit} beats need splitting · {totalMerge} beats need merging");
        Console.WriteLine(new string('━', ColW));
    }
}
