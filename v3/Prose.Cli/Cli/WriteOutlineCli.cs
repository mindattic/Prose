using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// ss --write-outline --slug &lt;nodeSlug&gt; [--json]
///
/// Generates a beat-by-beat narrative outline of a node. For a real logic check
/// (causality/knowledge-states/timeline/plant-payoff/orphan-refs/bible-agreement),
/// use ss --logic-sweep instead — this used to bundle a logic audit here too, but that
/// audit predated LOGIC.md's current six-dimension doctrine and never matched it.
/// </summary>
public static class WriteOutlineCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug   = null;
        bool jsonMode  = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
        }

        if (slug == null)
        {
            Console.Error.WriteLine("Usage: ss --write-outline --slug <nodeSlug> [--json]");
            return 2;
        }

        var outlineSvc = services.GetRequiredService<NodeOutlineService>();
        var dbFactory  = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        if (!jsonMode)
            Console.WriteLine($"Writing outline for '{node.Title}'…\n");

        var result = await outlineSvc.GenerateAsync(node.Id);

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_id    = result.NodeId,
                slug,
                title      = result.Title,
                beat_count = result.BeatCount,
                outline    = result.Outline,
            }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine(result.Outline);
        return 0;
    }
}
