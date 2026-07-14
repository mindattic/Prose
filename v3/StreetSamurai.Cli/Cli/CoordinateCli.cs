using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --coordinate --slug &lt;slug&gt; [--json &lt;path&gt;] [--no-stamp]
///
/// Full-coverage bible↔blueprint↔beat coordination. Correlates every enabled beat's
/// meaning (bible), construction (blueprint), and prose (DB); emits a JSON report and
/// stamps a regenerable "## Beat Coordination Index" into docs/nodes/&lt;CODE&gt;.md.
/// </summary>
public static class CoordinateCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, jsonPath = null;
        bool stamp = !args.Contains("--no-stamp");
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
            if (args[i] == "--json") { jsonPath = args[i + 1]; i++; }
        }

        if (slug == null)
        {
            Console.Error.WriteLine("Usage: ss --coordinate --slug <slug> [--json <path>] [--no-stamp]");
            return 2;
        }

        var svc = services.GetRequiredService<BeatCoordinationService>();
        Console.WriteLine($"Coordinating bible↔blueprint↔beat for {slug}...");

        var r = await svc.CoordinateAsync(slug, jsonPath, stamp);

        Console.WriteLine();
        Console.WriteLine($"Node        : {r.NodeCode}");
        Console.WriteLine($"Beats       : {r.TotalBeats}");
        Console.WriteLine($"Covered     : {r.Covered}/{r.TotalBeats} "
            + $"({100.0 * r.Covered / Math.Max(1, r.TotalBeats):F1}%)");
        Console.WriteLine($"Blueprint   : {(r.StoryScope.HasBlueprint ? $"yes (granularity={r.StoryScope.Granularity})" : "MISSING")}");
        Console.WriteLine();

        if (r.FlagCounts.Count > 0)
        {
            Console.WriteLine("Coverage gaps:");
            foreach (var kv in r.FlagCounts.OrderByDescending(kv => kv.Value))
                Console.WriteLine($"  {kv.Key,-16} {kv.Value}");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("No coverage gaps — every beat carries all three coordinates.");
            Console.WriteLine();
        }

        Console.WriteLine($"JSON        : {r.JsonPath}");
        Console.WriteLine($"Index       : {(r.StampedTo ?? "(not stamped)")}");
        return 0;
    }
}
