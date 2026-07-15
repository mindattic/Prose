using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --migrate-node-bibles [--slug &lt;slug&gt;] [--dry-run]
///
/// Step A2 (NodeBible side): migrates Nodes.NodeBible text blobs into structured
/// NodeBibleSection rows. One "Full" section per node — the complete NodeBible
/// content is preserved intact. Typed sections (ArcSummary, Characters, etc.)
/// are created via set_story_bible_section MCP over time.
///
/// After migration, generate_node_doc reads from NodeBibleSections WHERE
/// SectionType='Full' (or assembles from typed sections when they exist).
/// The legacy Nodes.NodeBible column is kept but marked stale via NodeBibleGeneratedAt.
///
/// Idempotent: skips nodes that already have a NodeBibleSection row.
/// </summary>
public static class MigrateNodeBiblesCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        bool dryRun = args.Contains("--dry-run");
        string? filterSlug = null;
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { filterSlug = args[i + 1]; i++; }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var query = db.Nodes
            .Where(n => n.NodeBible != null && n.NodeBible.Length > 10);

        if (filterSlug != null)
            query = query.Where(n => n.Slug == filterSlug || n.NodeCode == filterSlug);

        var nodes = await query
            .Select(n => new { n.Id, n.Slug, n.NodeCode, n.Title, n.NodeBible })
            .OrderBy(n => n.Slug)
            .ToListAsync();

        // Get nodes that already have at least one section
        var alreadyMigrated = await db.NodeBibleSections
            .Select(s => s.NodeId)
            .Distinct()
            .ToHashSetAsync();

        int created = 0, skipped = 0;

        foreach (var node in nodes)
        {
            if (alreadyMigrated.Contains(node.Id))
            {
                Console.WriteLine($"  SKIP  [{node.NodeCode ?? node.Slug}] {node.Title} — already has sections");
                skipped++;
                continue;
            }

            Console.WriteLine($"  {(dryRun ? "DRY " : "")}CREATE  [{node.NodeCode ?? node.Slug}] {node.Title}");

            if (!dryRun)
            {
                db.NodeBibleSections.Add(new NodeBibleSection
                {
                    NodeId      = node.Id,
                    SectionType = "Full",
                    Content     = node.NodeBible!,
                    UpdatedAt   = DateTime.UtcNow,
                });
                created++;
            }
        }

        if (!dryRun && created > 0)
            await db.SaveChangesAsync();

        Console.WriteLine();
        Console.WriteLine($"Done. created={created} skipped={skipped}{(dryRun ? " (dry run)" : "")}");
        return 0;
    }
}
