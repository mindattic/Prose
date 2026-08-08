using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI surface for <see cref="DriftAuditService"/>. Reports every Character
/// row whose denormalised dynamic column disagrees with the latest matching
/// EntityStateEvents row. Output is human-readable; use --json for a
/// machine-parseable dump.
///
///   prose --audit-drift           pretty-printed report
///   prose --audit-drift --json    JSON to stdout
/// </summary>
public static class AuditDriftCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var asJson = args.Contains("--json");
        var svc = sp.GetRequiredService<DriftAuditService>();

        if (!asJson) Console.WriteLine("[audit-drift] scanning Characters…");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var report = await svc.RunAsync();
        sw.Stop();

        if (asJson)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return report.Total > 0 ? 1 : 0;
        }

        Console.WriteLine($"=== Drift audit done in {sw.Elapsed:mm\\:ss} ===");
        Console.WriteLine($"  drift rows : {report.Total}");
        if (report.Total == 0)
        {
            Console.WriteLine("  ✓ no drift detected — column values match the ledger.");
            return 0;
        }
        Console.WriteLine();
        Console.WriteLine("  per aspect:");
        foreach (var kv in report.PerAspect.OrderByDescending(x => x.Value))
            Console.WriteLine($"    {kv.Key,-22} {kv.Value}");

        Console.WriteLine();
        Console.WriteLine("  first 30 drifts:");
        foreach (var d in report.Drifts.Take(30))
        {
            Console.WriteLine($"  {d.EntityName} · aspect:{d.AspectKey}");
            Console.WriteLine($"    column: {Truncate(d.ColumnValue, 90)}");
            Console.WriteLine($"    ledger: {Truncate(d.LedgerValue, 90)}  (at {d.LedgerAtStoryTime:O})");
        }
        if (report.Drifts.Count > 30)
            Console.WriteLine($"  …and {report.Drifts.Count - 30} more (rerun with --json for full list)");
        return 1;
    }

    private static string Truncate(string? s, int n)
    {
        s ??= "(null)";
        return s.Length <= n ? s : s[..n] + "…";
    }
}
