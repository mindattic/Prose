using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI surface for <see cref="SqlSeedService"/>. Replaces the prior
/// "run sqlcmd against the .sql file by hand" workflow.
///
/// Usage:
///   ss --seed                     list known seeds + run state
///   ss --seed &lt;name&gt;              apply (idempotent — skip if already ran)
///   ss --seed &lt;name&gt; --force      re-run even if already applied
///   ss --seed --all               apply every known seed in order
/// </summary>
public static class SeedCli
{
    public static async Task<int> RunAsync(IReadOnlyList<string> args, IServiceProvider sp)
    {
        var name  = args.SkipWhile(a => a != "--seed").Skip(1).FirstOrDefault();
        var force = args.Contains("--force");
        var all   = args.Contains("--all");
        var svc   = sp.GetRequiredService<SqlSeedService>();

        if (string.IsNullOrWhiteSpace(name) && !all)
        {
            Console.WriteLine("ss --seed <name> [--force]   apply one named seed");
            Console.WriteLine("ss --seed --all              apply every known seed in order");
            Console.WriteLine();
            Console.WriteLine("Known seeds:");
            foreach (var (k, v) in SqlSeedService.Seeds)
                Console.WriteLine($"  {k,-32} → Data/Sql/{v}");
            return 0;
        }

        if (all)
        {
            var failures = 0;
            foreach (var (k, _) in SqlSeedService.Seeds)
            {
                var r = await svc.RunAsync(k, force);
                Console.WriteLine($"  {(r.Success ? '✓' : '✘')} {k,-32}  {r.Message}");
                if (!r.Success) failures++;
            }
            return failures > 0 ? 1 : 0;
        }

        var result = await svc.RunAsync(name!, force);
        Console.WriteLine($"{(result.Success ? "OK" : "FAIL")}  {result.Message}");
        return result.Success ? 0 : 1;
    }
}
