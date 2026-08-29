using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --write-synopsis --slug &lt;nodeSlug&gt; [--json]
///
/// Generates a beat-by-beat narrative synopsis of a node — a post-hoc description of the
/// written prose (renamed from --write-outline 2026-08-29; "Outline" now names the per-book
/// pre-writing plan formerly called the Node Bible). For a real logic check
/// (causality/knowledge-states/timeline/plant-payoff/orphan-refs/outline-agreement),
/// use prose --logic-sweep instead.
/// </summary>
public static class WriteSynopsisCli
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
            Console.Error.WriteLine("Usage: prose --write-synopsis --slug <nodeSlug> [--json]");
            return 2;
        }

        var synopsisSvc = services.GetRequiredService<NarrativeSynopsisService>();
        var dbFactory   = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        if (!jsonMode)
            Console.WriteLine($"Writing synopsis for '{node.Title}'…\n");

        var result = await synopsisSvc.GenerateAsync(node.Id);

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_id    = result.NodeId,
                slug,
                title      = result.Title,
                beat_count = result.BeatCount,
                synopsis   = result.Synopsis,
            }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine(result.Synopsis);
        return 0;
    }
}
