using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --list-archives (--id &lt;guid&gt; | --slug &lt;slug&gt;)</c>
///
/// Lists every <c>ArchivedBook</c> snapshot for a node, newest first, so a human can find the
/// right one before calling <c>prose --restore-node-field</c>. Read-only. "Current" is never
/// ambiguous here: this command only ever shows history: the live values always live on the
/// Node row itself, never in this list.
/// </summary>
public static class ListArchivesCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":   if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[list-archives] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: prose --list-archives (--id <guid> | --slug <slug>) --universe <u>");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = !string.IsNullOrWhiteSpace(slug)
            // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
            ? await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug)
            : Guid.TryParse(id, out var g)
                // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
                ? await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == g)
                : null;

        if (node == null)
        {
            Console.Error.WriteLine("[list-archives] Target node not found.");
            return 1;
        }

        var archives = await db.ArchivedBooks.AsNoTracking()
            .Where(a => a.NodeId == node.Id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        if (archives.Count == 0)
        {
            Console.WriteLine($"[list-archives] No archives found for '{node.Title}' ({node.Slug}). Run prose --archive-book first.");
            return 0;
        }

        Console.WriteLine($"[list-archives] {archives.Count} archive(s) for '{node.Title}' ({node.Slug}):");
        foreach (var a in archives)
        {
            var fields = new List<string>();
            if (!string.IsNullOrWhiteSpace(a.Description)) fields.Add("description");
            if (!string.IsNullOrWhiteSpace(a.NodeOutline)) fields.Add("nodeoutline");
            if (!string.IsNullOrWhiteSpace(a.Summary)) fields.Add("summary");
            if (!string.IsNullOrWhiteSpace(a.Seed)) fields.Add("seed");
            if (!string.IsNullOrWhiteSpace(a.Subtitle)) fields.Add("subtitle");
            var fieldList = fields.Count > 0 ? string.Join(",", fields) : "(none captured)";

            Console.WriteLine($"  {a.Id}  {a.CreatedAt:yyyy-MM-dd HH:mm:ss}Z  reason={a.Reason,-18} V{a.Version}  beats={a.BeatCount} words={a.WordCount:N0}  fields=[{fieldList}]");
        }

        return 0;
    }
}
