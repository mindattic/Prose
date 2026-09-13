using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// prose --set-node-version (--slug &lt;slug&gt; | --id &lt;id&gt;) --version &lt;N&gt;
/// — sets Node.Version directly (the counter DocxExportService reads as nextVersion = Version + 1
/// when naming the next exported file "&lt;Title&gt; V{nextVersion}.docx/.epub/...").
///
/// Exists for the case where a book's local export-folder version lineage was reset (a fresh
/// folder/regen under a renamed node) while the SAME Amazon KDP listing's real version history
/// continues from an earlier number — e.g. BCODA: archived up to V69 under its pre-rename node,
/// then restarted at V1 after the BCODA3-&gt;BCODA rename (2026-09-09), even though the live KDP
/// listing (ASIN recorded in the restored .publish marker) still expects the next uploaded
/// manuscript to read as a continuation, not a regression. No prior CLI/MCP path set this field.
/// </summary>
public static class SetNodeVersionCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null;
        int? version = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":      if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":    if (i + 1 < args.Length) slug = args[++i]; break;
                case "--version": if (i + 1 < args.Length && int.TryParse(args[++i], out var v)) version = v; break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[set-node-version] --id or --slug required to identify the node.");
            return 1;
        }
        if (version is null)
        {
            Console.Error.WriteLine("[set-node-version] --version <N> required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // IgnoreQueryFilters(): explicit id/slug lookup, not ambient scope.
        var q = db.Nodes.IgnoreQueryFilters().AsQueryable();
        var node = !string.IsNullOrWhiteSpace(slug)
            ? await q.FirstOrDefaultAsync(s => s.Slug == slug)
            : Guid.TryParse(id, out var g)
                ? await q.FirstOrDefaultAsync(s => s.Id == g)
                : await q.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync()
                    is { Count: 1 } m ? m[0] : null;

        if (node == null) { Console.Error.WriteLine("[set-node-version] Node not found."); return 1; }

        var previous = node.Version;
        node.Version = version.Value;
        await db.SaveChangesAsync();

        Console.WriteLine($"[set-node-version] \"{node.Title}\" Version {previous} -> {node.Version}. " +
                          $"Next --export-node will write V{node.Version + 1}.");
        return 0;
    }
}
