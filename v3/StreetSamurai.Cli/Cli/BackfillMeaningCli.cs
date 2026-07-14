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
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
            if (args[i] == "--limit" && int.TryParse(args[i + 1], out var l)) { limit = l; i++; }
        }
        if (slug == null)
        {
            Console.Error.WriteLine("Usage: ss --backfill-meaning --slug <slug> [--limit N] [--dry-run]");
            return 2;
        }

        var svc = services.GetRequiredService<MeaningBackfillService>();
        Console.WriteLine($"Backfilling meaning for {slug}{(dryRun ? " (dry run)" : "")}...");
        var r = await svc.BackfillAsync(slug, limit, dryRun, Console.WriteLine);

        Console.WriteLine();
        Console.WriteLine($"Node   : {r.NodeCode}");
        Console.WriteLine($"Missing: {r.Missing}");
        Console.WriteLine($"Filled : {r.Filled}{(dryRun ? " (dry run — not saved)" : "")}");
        Console.WriteLine($"Failed : {r.Failed}");
        return 0;
    }
}
