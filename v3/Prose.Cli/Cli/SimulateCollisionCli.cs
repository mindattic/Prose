using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --simulate-collision --slug &lt;nodeSlug|nodeCode&gt; --beat-number &lt;n&gt; [--json]
///
/// Manual test harness for SceneCollisionService (2026-08-10): runs the collision computation
/// against one real beat without needing a full ProseWriterRouter pass. Useful for validating
/// the engine on existing books before it's proven on its intended first target (a new
/// standalone book — see memory: project_causal_collision_engine_vision).
///
/// Exit codes: 0 = collision computed, 1 = skipped (gate not met), 2 = beat/node not found.
/// </summary>
public static class SimulateCollisionCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        int? beatNumber = null;
        bool json = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
            if (args[i] == "--beat-number" && int.TryParse(args[i + 1], out var bn)) { beatNumber = bn; i++; }
        }

        if (slug == null || beatNumber == null)
        {
            Console.Error.WriteLine("Usage: prose --simulate-collision --slug <nodeSlug|nodeCode> --beat-number <n> [--json]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Number == beatNumber.Value);
        if (beat == null)
        {
            Console.Error.WriteLine($"Beat #{beatNumber} not found.");
            return 2;
        }

        // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        var assembler = services.GetRequiredService<SceneContextAssembler>();
        var collisionSvc = services.GetRequiredService<SceneCollisionService>();

        var scene = await assembler.AssembleForBeatAsync(beat.Id, tokenBudget: 2000);
        if (scene == null || string.IsNullOrWhiteSpace(scene.ContextBlock))
        {
            Console.Error.WriteLine($"Beat #{beatNumber}: no scene roster could be assembled — nothing to compute from.");
            return 1;
        }

        var characterNames = scene.Roster
            .Where(r => r.EntityType.Equals("character", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Name)
            .Distinct()
            .ToList();

        if (!json)
        {
            Console.WriteLine($"Beat #{beat.Number} ('{node.Title}'): {characterNames.Count} character(s) on page — {string.Join(", ", characterNames)}");
        }

        if (characterNames.Count < 2)
        {
            Console.Error.WriteLine($"Beat #{beatNumber}: fewer than 2 characters on page ({characterNames.Count}) — collision engine requires at least 2. Skipped.");
            return 1;
        }

        var beatGoal = beat.Description ?? beat.Title ?? "";
        if (string.IsNullOrWhiteSpace(beatGoal))
        {
            Console.Error.WriteLine($"Beat #{beatNumber}: no Description/Title to use as the beat goal. Skipped.");
            return 1;
        }

        var collision = await collisionSvc.ComputeAsync(
            characterNames, scene.ContextBlock,
            worldStateContext: "", consequenceContext: "",
            beatGoal, locationContext: "");

        if (collision == null)
        {
            Console.Error.WriteLine($"Beat #{beatNumber}: collision computation failed or returned nothing. See logs.");
            return 1;
        }

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                beat_number = beat.Number,
                node_title  = node.Title,
                characters  = characterNames,
                mechanics   = collision.Mechanics,
                reactions   = collision.Reactions.Select(r => new { r.Name, r.Reaction }),
                new_consequence = collision.NewConsequence,
                rationale   = collision.Rationale,
            }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine();
        Console.WriteLine("MECHANICS:");
        Console.WriteLine($"  {collision.Mechanics}");
        if (collision.Reactions.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("REACTIONS:");
            foreach (var r in collision.Reactions)
                Console.WriteLine($"  {r.Name}: {r.Reaction}");
        }
        if (!string.IsNullOrWhiteSpace(collision.NewConsequence))
        {
            Console.WriteLine();
            Console.WriteLine($"NEW CONSEQUENCE: {collision.NewConsequence}");
        }
        Console.WriteLine();
        Console.WriteLine($"RATIONALE: {collision.Rationale}");

        return 0;
    }
}
