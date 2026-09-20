using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI surface for <see cref="GeneticsInheritanceService"/>.
///
///   prose --genetics propagate                     walk the family graph, blend genetics root→leaf
///   prose --genetics propagate --id &lt;id|slug&gt;       single character only
///   prose --genetics propagate --seed 42           seeded RNG for reproducible noise
/// </summary>
public static class GeneticsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var idx = Array.IndexOf(args, "--genetics");
        var sub = idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
        if (sub != "propagate")
        {
            Console.Error.WriteLine("Usage: prose --genetics propagate [--id <id|slug>] [--seed N]");
            return 2;
        }

        var svc    = sp.GetRequiredService<GeneticsInheritanceService>();
        var export = sp.GetRequiredService<CanonExportService>();

        var seedArg = GetArg(args, "--seed");
        var rng     = seedArg != null && int.TryParse(seedArg, out var s) ? new Random(s) : null;

        var idArg = GetArg(args, "--id");
        if (idArg != null)
        {
            var id = await export.ResolveEntityIdAsync(idArg);
            if (id == null) { Console.Error.WriteLine("could not resolve id/slug"); return 1; }
            var changed = await svc.PropagateForAsync(id.Value, rng);
            Console.WriteLine(changed
                ? $"propagated genetics for {id}"
                : $"no propagation: {id} is a root (no parents in family graph)");
            return 0;
        }

        Console.WriteLine("[genetics] propagating across family graph...");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var report = await svc.PropagateAllAsync(rng);
        sw.Stop();
        Console.WriteLine($"=== Propagation done in {sw.Elapsed:mm\\:ss} ===");
        Console.WriteLine($"  characters processed:        {report.Processed}");
        Console.WriteLine($"  rooted (no parents in graph): {report.Roots}");
        Console.WriteLine($"  blended + written:           {report.Updated}");
        Console.WriteLine($"  skipped (no parent records):  {report.Skipped}");
        return 0;
    }

    private static string? GetArg(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
