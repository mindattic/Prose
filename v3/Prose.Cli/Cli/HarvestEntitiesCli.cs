using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --harvest-entities</c> — turn open text (design notes, canon briefs, worldbuilding
/// passages) into canon: extract every load-bearing noun, resolve against the entity corpus,
/// create the missing ones as stubs in their proper EntityType, and wire Edges between
/// related pairs.
///
///   ss --harvest-entities --file &lt;path&gt; [--universe glmz] [--dry-run]
///
/// Exit codes: 0 = harvest ran (see counts), 1 = bad args / file missing / extraction empty.
/// After a non-dry run, run <c>ss --reembed</c> so new stubs join the semantic index.
/// </summary>
public static class HarvestEntitiesCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? file = null;
        bool dryRun = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--file":    if (i + 1 < args.Length) file = args[++i]; break;
                case "--dry-run": dryRun = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            Console.Error.WriteLine("[harvest] --file <path> is required and must exist.");
            Console.Error.WriteLine("Usage: ss --harvest-entities --file <path> [--universe glmz] [--dry-run]");
            return 1;
        }

        var universeId = UniverseScope.EffectiveId;
        if (universeId == Guid.Empty)
        {
            Console.Error.WriteLine("[harvest] No universe scope. Pass --universe glmz (or set PROSE_UNIVERSE).");
            return 1;
        }

        var text = await File.ReadAllTextAsync(file);
        Console.WriteLine($"[harvest] {Path.GetFileName(file)}: {text.Length} chars, universe {universeId}{(dryRun ? " (dry-run)" : "")}");

        var svc = services.GetRequiredService<EntityHarvestService>();
        var result = await svc.HarvestAsync(text, universeId, dryRun);

        foreach (var w in result.Warnings)
            Console.WriteLine($"[harvest]   ! {w}");

        foreach (var r in result.Entities.OrderBy(r => r.Outcome).ThenBy(r => r.Name))
        {
            var note = r.Outcome switch
            {
                "existing" => "= existing",
                "similar"  => $"≈ merged into '{r.MatchedName}'",
                "created"  => dryRun ? "+ WOULD CREATE (stub)" : "+ created (stub)",
                _          => r.Outcome,
            };
            Console.WriteLine($"[harvest]   {r.Name,-42} {r.EntityType,-12} {note}");
        }

        var created = result.Entities.Count(r => r.Outcome == "created");
        Console.WriteLine($"[harvest] {result.Entities.Count} nouns → {created} new, " +
                          $"{result.Entities.Count - created} resolved; {result.EdgesCreated} edge(s){(dryRun ? " (dry-run, nothing written)" : "")}.");
        if (!dryRun && created > 0)
            Console.WriteLine("[harvest] Next: ss --reembed  (adds the new stubs to the semantic index); promote stubs to canon via the entity queue.");
        return 0;
    }
}
