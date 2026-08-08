using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --backfill-entity-docs --slug &lt;slug&gt; [--text]</c>
///
/// Materializes per-entity <c>MarkdownFiles</c> rows (category="entity-doc") for every
/// entity referenced in a book's beat goals. Entity docs are normally created on-demand
/// during prose generation (via EntityDocService.InferFromTextAsync inside PrepareForNodeAsync),
/// but books written before the entity-doc system was active have zero entries.
///
/// This command replays the inference pass over every enabled beat's goal text so future
/// prose generation — and the DCM viz — see the character docs they should have had.
///
/// With <c>--text</c>: also scans beat prose text (much broader; use once to catch
/// entities not mentioned in goals).
///
/// Exit codes: 0 = ran, 1 = bad args / node not found.
/// </summary>
public static class BackfillEntityDocsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var slug = ArgValue(args, "--slug");
        bool includeText = args.Contains("--text");

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --backfill-entity-docs --slug <slug> [--text]");
            return 1;
        }

        var dbFactory  = sp.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var entityDocs = sp.GetRequiredService<EntityDocService>();

        // Resolve node (book or chapter)
        Guid nodeId;
        string nodeTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var node = await db.Nodes.AsNoTracking()
                .Where(n => n.Slug == slug)
                .Select(n => new { n.Id, n.Title })
                .FirstOrDefaultAsync();
            if (node == null)
            {
                Console.Error.WriteLine($"[backfill-entity-docs] node not found: {slug}");
                return 1;
            }
            nodeId    = node.Id;
            nodeTitle = node.Title ?? slug;
        }

        Console.WriteLine($"[backfill-entity-docs] Book: \"{nodeTitle}\" ({slug})");

        // Collect all enabled beats across this node and its chapter children.
        var beats = await CollectBeatsAsync(nodeId, dbFactory, includeText);
        Console.WriteLine($"[backfill-entity-docs] {beats.Count} beat(s) to scan…");

        int total = 0, created = 0;
        foreach (var (beatIndex, goal, text) in beats)
        {
            // Goal text first — fast, contains character names from outline.
            if (!string.IsNullOrWhiteSpace(goal))
            {
                var n = await entityDocs.InferFromTextAsync(goal);
                created += n;
                total++;
            }

            // Prose text (optional — broader but slower).
            if (includeText && !string.IsNullOrWhiteSpace(text))
            {
                var n = await entityDocs.InferFromTextAsync(text);
                created += n;
                total++;
            }

            if ((beatIndex + 1) % 10 == 0)
                Console.Write($"\r[backfill-entity-docs]   beat {beatIndex + 1}/{beats.Count}…   ");
        }
        Console.WriteLine();

        // Report final MarkdownFiles entity-doc count for this project.
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var docCount = await db.MarkdownFiles.CountAsync(m => m.Category == "entity-doc");
            Console.WriteLine($"[backfill-entity-docs] Total entity-doc rows in MarkdownFiles: {docCount}");
        }

        Console.WriteLine($"[backfill-entity-docs] Scanned {total} text block(s); {created} entity doc(s) created or updated.");
        if (created > 0)
            Console.WriteLine("[backfill-entity-docs] Next: prose --sync-markdown  (pushes content into embedding scope)");
        return 0;
    }

    private static async Task<IReadOnlyList<(int Index, string? Goal, string? Text)>> CollectBeatsAsync(
        Guid nodeId, IDbContextFactory<ProseDbContext> dbFactory, bool includeText)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var chapters = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == nodeId)
            .OrderBy(n => n.SortKey)
            .Select(n => n.Id)
            .ToListAsync();

        var sourceIds = chapters.Count > 0 ? chapters : new List<Guid> { nodeId };

        var rows = await db.BeatNodes.AsNoTracking()
            .Where(bn => sourceIds.Contains(bn.NodeId) && bn.IsEnabled)
            .OrderBy(bn => bn.SortKey)
            .Select(bn => new { bn.Beat!.Description, bn.Beat.Text })
            .ToListAsync();

        return rows.Select((r, i) => (i, r.Description, includeText ? r.Text : null)).ToList();
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
