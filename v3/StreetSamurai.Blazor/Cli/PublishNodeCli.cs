using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --publish-story</c> — stitch an existing node's recorded beats into
/// one combined file (WAV → final MP3), drop a friendly copy in the configured
/// publish output directory (Desktop fallback), and record the 1:M publication run
/// plus its process-event ledger. Resolves the node by id (full or prefix)
/// or slug. Headless equivalent of the in-app Publish button.
///
/// Args (one of --id / --slug required):
///   --id &lt;guid|prefix&gt;  Node id; a unique prefix is enough.
///   --slug &lt;slug&gt;       Node slug.
///
/// Exit codes: 0 — published; 1 — bad args / not found / nothing to publish.
/// </summary>
public static class PublishNodeCli
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
            Console.Error.WriteLine("[publish-story] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: ss --publish-story (--id <guid|prefix> | --slug <slug>)");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();

        Guid nodeId; string nodeSlug, nodeTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var query = db.Nodes.AsNoTracking();
            Node? node;
            if (!string.IsNullOrWhiteSpace(slug))
                node = await query.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var exact))
                node = await query.FirstOrDefaultAsync(s => s.Id == exact);
            else
            {
                var prefix = id!.ToLowerInvariant();
                var matches = await query.Where(s => s.Id.ToString().StartsWith(prefix)).Take(2).ToListAsync();
                if (matches.Count > 1)
                {
                    Console.Error.WriteLine($"[publish-story] Id prefix '{id}' is ambiguous. Use a longer prefix or the full id.");
                    return 1;
                }
                node = matches.FirstOrDefault();
            }

            if (node == null)
            {
                Console.Error.WriteLine($"[publish-story] No node found for {(slug != null ? $"slug '{slug}'" : $"id '{id}'")}.");
                return 1;
            }
            nodeId = node.Id; nodeSlug = node.Slug; nodeTitle = node.Title;
        }

        Console.WriteLine($"[publish-story] Publishing:");
        Console.WriteLine($"   Id:    {nodeId}");
        Console.WriteLine($"   Slug:  {nodeSlug}");
        Console.WriteLine($"   Title: {nodeTitle}");

        string? rel;
        try
        {
            rel = await workbench.ExportCombinedAsync(nodeId);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[publish-story] Publish failed: {ex.Message}");
            return 1;
        }

        if (rel == null)
        {
            Console.Error.WriteLine("[publish-story] Nothing to publish — record beats first (or beats are mixed-format).");
            return 1;
        }

        // Report the recorded publication run + ledger so the CLI run is self-verifying.
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var pub = await db.NodePublications.AsNoTracking()
                .Where(p => p.NodeId == nodeId)
                .OrderByDescending(p => p.StartedAt)
                .FirstOrDefaultAsync();
            var eventCount = await db.NodeAudioEvents.CountAsync(e => e.NodeId == nodeId);
            Console.WriteLine($"[publish-story] Combined internal path: {rel}");
            if (pub != null)
            {
                Console.WriteLine($"[publish-story] Publication: {pub.Status}, {pub.Format}, {pub.BeatCount} beats, {pub.ByteSize:N0} bytes");
                Console.WriteLine($"[publish-story] Exported to: {pub.Path}");
            }
            Console.WriteLine($"[publish-story] Audio-event ledger rows for this node: {eventCount}");
        }
        Console.WriteLine("[publish-story] Done.");
        return 0;
    }
}
