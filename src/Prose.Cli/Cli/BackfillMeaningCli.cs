using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --backfill-meaning --slug &lt;slug&gt; [--limit N] [--dry-run]
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
            if (args[i] == "--limit")
            {
                // "1O" used to mean "no limit" — every missing beat went to the LLM.
                if (!int.TryParse(args[i + 1], out var l)) { Console.Error.WriteLine($"--limit must be a number, got '{args[i + 1]}'."); return 2; }
                limit = l; i++;
            }
            if (args[i] == "--beats")
            {
                // A bad token used to be dropped; all bad → empty set → the service refreshed EVERY beat.
                beats = [];
                foreach (var tok in args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (!int.TryParse(tok, out var n) || n < 0) { Console.Error.WriteLine($"--beats: '{tok}' is not a beat number."); return 2; }
                    beats.Add(n);
                }
                if (beats.Count == 0) { Console.Error.WriteLine("--beats: no beat numbers given."); return 2; }
                i++;
            }
        }
        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --backfill-meaning --slug <slug> [--limit N] [--dry-run] [--overwrite] [--beats 4308,4309,...]");
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
        return r.Failed > 0 ? 1 : 0;
    }
}
