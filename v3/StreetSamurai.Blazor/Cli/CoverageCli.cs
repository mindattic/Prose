using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --coverage</c> — per-entity-type reachability matrix: how many entities
/// exist vs. how many are embedded (and therefore pullable into prose by the
/// universal canon retrieval). The standing gap-finder: 0%-embedded types are
/// dead inventory; 100% means fully wired into the engine.
/// </summary>
public static class CoverageCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var coverage = services.GetRequiredService<CoverageService>();
        var rows = await coverage.ReportAsync();
        if (rows.Count == 0) { Console.WriteLine("[coverage] No active entities."); return 0; }

        Console.WriteLine($"{"TYPE",-18} {"TOTAL",7} {"EMBEDDED",9} {"REACH%",7}  STATUS");
        Console.WriteLine(new string('-', 64));
        int total = 0, embedded = 0;
        foreach (var r in rows)
        {
            total += r.Total; embedded += r.Embedded;
            var status = r.EmbeddedPct >= 99 ? "fully reachable"
                       : r.EmbeddedPct <= 1  ? "DEAD INVENTORY"
                       : "partial";
            Console.WriteLine($"{Trunc(r.EntityType, 18),-18} {r.Total,7} {r.Embedded,9} {r.EmbeddedPct,6:F0}%  {status}");
        }
        Console.WriteLine(new string('-', 64));
        var pct = total > 0 ? 100.0 * embedded / total : 0;
        Console.WriteLine($"{"ALL",-18} {total,7} {embedded,9} {pct,6:F0}%");
        Console.WriteLine("\n[coverage] Embedded = reachable by the universal canon retrieval (all types).");
        return 0;
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
