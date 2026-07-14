using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --backfill-meaning --slug &lt;slug&gt; [--limit N] [--dry-run]
///
/// Fills the MEANING coordinate (Beat.Description) for beats that have prose but no
/// recorded meaning — the gap the coordination pass surfaces. Sonnet, batched.
/// </summary>
public static class BackfillMeaningCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null; int? limit = null; bool dryRun = args.Contains("--dry-run");
        bool overwrite = args.Contains("--overwrite");
        HashSet<int>? beats = null;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
            if (args[i] == "--limit" && int.TryParse(args[i + 1], out var l)) { limit = l; i++; }
            if (args[i] == "--beats")
            {
                beats = args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(s => int.TryParse(s, out var n) ? n : -1).Where(n => n >= 0).ToHashSet();
                i++;
            }
        }
        if (slug == null)
        {
            Console.Error.WriteLine("Usage: ss --backfill-meaning --slug <slug> [--limit N] [--dry-run] [--overwrite] [--beats 4308,4309,...]");
            return 2;
        }

        var svc = services.GetRequiredService<MeaningBackfillService>();
        var mode = overwrite ? (beats != null ? $"refresh {beats.Count} beat(s)" : "refresh ALL") : "backfill empty";
        Console.WriteLine($"Meaning {mode} for {slug}{(dryRun ? " (dry run)" : "")}...");
        var r = await svc.BackfillAsync(slug, limit, dryRun, overwrite, beats, Console.WriteLine);

        Console.WriteLine();
        Console.WriteLine($"Node   : {r.NodeCode}");
        Console.WriteLine($"Missing: {r.Missing}");
        Console.WriteLine($"Filled : {r.Filled}{(dryRun ? " (dry run — not saved)" : "")}");
        Console.WriteLine($"Failed : {r.Failed}");
        return 0;
    }
}
