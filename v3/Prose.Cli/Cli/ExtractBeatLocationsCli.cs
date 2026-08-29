using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --extract-beat-locations --slug &lt;slug&gt; [--force] [--limit N] [--dry-run]
///
/// Backfills the per-beat scene location (Beat.PlaceName + resolved Beat.PlaceEntityId) for one
/// book — batched Haiku extraction in reading order, hash-gated on Beat.PlaceExtractedFromHash
/// vs TextHash so unchanged beats cost nothing on re-run. New beats get this automatically via
/// BeatExtractionService's consolidated post-write call; this CLI exists for the existing corpus.
/// </summary>
public static class ExtractBeatLocationsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null; int? limit = null;
        bool dryRun = args.Contains("--dry-run");
        bool force = args.Contains("--force");
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
            if (args[i] == "--limit" && int.TryParse(args[i + 1], out var l)) { limit = l; i++; }
        }
        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --extract-beat-locations --slug <slug> [--force] [--limit N] [--dry-run]");
            return 2;
        }

        var svc = services.GetRequiredService<BeatPlaceService>();
        Console.WriteLine($"Beat scene-location {(force ? "re-extract ALL" : "fill changed/missing")} for {slug}{(dryRun ? " (dry run)" : "")}...");
        var r = await svc.ExtractAsync(slug, limit, dryRun, force, Console.WriteLine);

        Console.WriteLine();
        Console.WriteLine($"Node               : {r.NodeCode}");
        Console.WriteLine($"Candidates         : {r.Candidates}");
        Console.WriteLine($"Extracted          : {r.Extracted}{(dryRun ? " (dry run — not saved)" : "")}");
        Console.WriteLine($"Resolved to canon  : {r.Resolved}");
        Console.WriteLine($"Failed             : {r.Failed}");
        Console.WriteLine($"Skipped (cached)   : {r.SkippedFromCache}");
        return 0;
    }
}
