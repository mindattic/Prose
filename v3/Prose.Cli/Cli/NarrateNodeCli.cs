using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --narrate-book</c> — (re)run TTS narration on an EXISTING node,
/// resolved by id (full or prefix) or slug. The complement to
/// <c>--write-story --narrate</c> (which only narrates a node it just
/// generated). Runs the same <see cref="NodeWorkbenchService.NarrateAsync"/>
/// path the Record button uses, then prints the per-run tally.
///
/// Args (one of --id / --slug required):
///   --id &lt;guid|prefix&gt;  Node id; a unique prefix is enough.
///   --slug &lt;slug&gt;       Node slug.
///
/// Exit codes:
///   0 — node finished with status "ready" (every beat rendered).
///   1 — bad args / node not found / finished "failed" (some beats failed).
/// </summary>
public static class NarrateNodeCli
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
            Console.Error.WriteLine("[narrate-book] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: ss --narrate-book (--id <guid|prefix> | --slug <slug>)");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();

        Guid nodeId;
        string nodeSlug, nodeTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var query = db.Nodes.AsNoTracking();
            Node? node;
            if (!string.IsNullOrWhiteSpace(slug))
            {
                node = await query.FirstOrDefaultAsync(s => s.Slug == slug);
            }
            else if (Guid.TryParse(id, out var exact))
            {
                node = await query.FirstOrDefaultAsync(s => s.Id == exact);
            }
            else
            {
                // Prefix match on the id's string form (e.g. "019e609c").
                var prefix = id!.ToLowerInvariant();
                var matches = await query
                    .Where(s => s.Id.ToString().StartsWith(prefix))
                    .Take(2)
                    .ToListAsync();
                if (matches.Count > 1)
                {
                    Console.Error.WriteLine($"[narrate-book] Id prefix '{id}' is ambiguous — matches multiple nodes. Use a longer prefix or the full id.");
                    return 1;
                }
                node = matches.FirstOrDefault();
            }

            if (node == null)
            {
                Console.Error.WriteLine($"[narrate-book] No node found for {(slug != null ? $"slug '{slug}'" : $"id '{id}'")}.");
                return 1;
            }
            nodeId    = node.Id;
            nodeSlug  = node.Slug;
            nodeTitle = node.Title;
        }

        Console.WriteLine($"[narrate-book] Narrating node:");
        Console.WriteLine($"   Id:    {nodeId}");
        Console.WriteLine($"   Slug:  {nodeSlug}");
        Console.WriteLine($"   Title: {nodeTitle}");
        Console.WriteLine($"[narrate-book] Running TTS — this may take a while…");

        try
        {
            await workbench.NarrateAsync(nodeId);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[narrate-book] Narration crashed: {ex.Message}");
            return 1;
        }

        // NarrateAsync swallows per-beat failures and records the outcome on the
        // node row — re-read it to report the tally and pick the exit code.
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var done = await db.Nodes.AsNoTracking().FirstAsync(s => s.Id == nodeId);
            Console.WriteLine($"[narrate-book] Status: {done.Status}  ({done.NarratedBeatCount}/{done.TotalBeatsToNarrate} beats narrated)");
            if (!string.IsNullOrEmpty(done.Error))
                Console.WriteLine($"[narrate-book] Error: {done.Error}");
            return done.Status == "ready" ? 0 : 1;
        }
    }
}
