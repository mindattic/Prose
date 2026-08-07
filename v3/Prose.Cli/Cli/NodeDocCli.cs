using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --generate-node-doc</c> — assemble the unified Story Context Document for a node.
///
/// Merges hand-authored NodeBible content with the Structural Blueprint and Beat Spine
/// from the DB, writes the result to both <c>Nodes.NodeBible</c> and
/// <c>docs/nodes/{CODE}.md</c>.  The disk file is a generated read-only mirror.
///
/// Args:
///   --slug &lt;slug&gt;   Target node by slug or NodeCode. Required unless --all is set.
///   --all            Process every root node that has a NodeCode.
/// </summary>
public static class NodeDocCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool all = args.Contains("--all");

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[++i];
        }

        if (!all && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[generate-node-doc] --slug <slug> or --all is required.");
            Console.Error.WriteLine("Usage: ss --generate-node-doc --slug <slug>");
            Console.Error.WriteLine("       ss --generate-node-doc --all");
            return 2;
        }

        var dbFactory  = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var docService = services.GetRequiredService<NodeDocService>();

        if (all)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            // Root nodes (ParentNodeId == null) with a NodeCode — SeriesNodes and BookNodes
            var nodes = await db.Nodes
                .Where(n => n.NodeCode != null && n.ParentNodeId == null)
                .OrderBy(n => n.NodeCode)
                .Select(n => new { n.Id, n.NodeCode, n.Title })
                .ToListAsync();

            if (nodes.Count == 0)
            {
                Console.Error.WriteLine("[generate-node-doc] No nodes with NodeCode found.");
                return 1;
            }

            Console.WriteLine($"[generate-node-doc] Processing {nodes.Count} nodes…");
            int ok = 0, fail = 0;
            foreach (var n in nodes)
            {
                try
                {
                    var result = await docService.GenerateAsync(n.Id);
                    Console.WriteLine($"  ✓ {n.NodeCode,-8} {n.Title} — {result.BeatCount} beats, blueprint={result.HasBlueprint}");
                    ok++;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"  ✗ {n.NodeCode,-8} {n.Title} — {ex.Message}");
                    fail++;
                }
            }

            Console.WriteLine($"[generate-node-doc] Done: {ok} succeeded, {fail} failed.");
            return fail > 0 ? 1 : 0;
        }
        else
        {
            // Single-node mode
            await using var db = await dbFactory.CreateDbContextAsync();
            var node = await db.Nodes.FirstOrDefaultAsync(
                n => n.Slug == slug || n.NodeCode == slug);

            if (node == null)
            {
                Console.Error.WriteLine($"[generate-node-doc] No node found with slug or code '{slug}'.");
                return 1;
            }

            Console.WriteLine($"[generate-node-doc] Node: {node.Title} ({node.NodeCode ?? node.Slug})");

            try
            {
                var result = await docService.GenerateAsync(node.Id);
                Console.WriteLine($"[generate-node-doc] Done.");
                Console.WriteLine($"  Beats   : {result.BeatCount}");
                Console.WriteLine($"  Blueprint: {result.HasBlueprint}");
                Console.WriteLine($"  File    : {result.Path}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[generate-node-doc] Failed: {ex.Message}");
                return 1;
            }
        }
    }
}
