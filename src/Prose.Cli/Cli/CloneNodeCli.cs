using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --clone-book (--id &lt;guid&gt; | --slug &lt;slug&gt;) [--title "New Title"] [--book-code "SM1"] [--draft] [--status &lt;status&gt;]</c>
/// — deep-clone a node: creates a new Node row plus independent copies of every
/// beat in its full subtree (new IDs, new Numbers). Audio, scores, and review history are NOT
/// cloned — the clone starts fresh so review scores are independent.
///
/// Write-gate Phase 1 (2026-08-22): this used to hand-roll its own raw write, cloning ONLY the
/// beats directly attached to the source node itself — a silent no-op for any book with chapters
/// (per the Book→Chapter→Beat hierarchy, a book's beats normally live on its ChapterNode children,
/// not the book node), since a real multi-chapter book has no directly-attached beats to copy. Now
/// a thin wrapper around <see cref="NodeWorkbenchService.DuplicateNodeAsync"/>, which recurses the
/// whole subtree — the same fix already applied to the near-identical MCP <c>CloneBookImpl</c>.
/// </summary>
public static class CloneNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, title = null, nodeCode = null;
        string status = "ready";
        bool isDraft = false, statusExplicit = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":           if (i + 1 < args.Length) id       = args[++i]; break;
                case "--slug":         if (i + 1 < args.Length) slug     = args[++i]; break;
                case "--title":        if (i + 1 < args.Length) title    = args[++i]; break;
                case "--book-code":    if (i + 1 < args.Length) nodeCode = args[++i]; break;
                case "--status":       if (i + 1 < args.Length) { status = args[++i]; statusExplicit = true; } break;
                case "--draft":        isDraft = true; break;
            }
        }

        if (isDraft && !statusExplicit) status = "draft";

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[clone-book] One of --id or --slug is required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();

        Node? source;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            // NodeRefResolver takes slug, NodeCode, GUID or a unique GUID prefix across universes.
            // The hand-rolled prefix branch it replaces was ambient-universe-scoped (a prefix of a
            // node in another universe came back "not found") unlike its full-GUID sibling.
            source = await NodeRefResolver.ResolveNodeAsync(db, !string.IsNullOrWhiteSpace(slug) ? slug : id);
        }

        if (source == null)
        {
            Console.Error.WriteLine($"[clone-book] {NodeRefResolver.NotFoundMessage(!string.IsNullOrWhiteSpace(slug) ? slug : id)}");
            return 1;
        }

        var newTitle = string.IsNullOrWhiteSpace(title) ? $"{source.Title} (Clone)" : title.Trim();
        Console.WriteLine($"[clone-book] Source: '{source.Title}' ({source.Slug})");

        try
        {
            var (newId, newSlug) = await workbench.DuplicateNodeAsync(source.Id, newTitle, nodeCode, status);
            Console.WriteLine($"[clone-book] Created '{newTitle}'");
            Console.WriteLine($"[clone-book] id:   {newId}");
            Console.WriteLine($"[clone-book] slug: {newSlug}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[clone-book] Failed: {ex.Message}");
            return 1;
        }
    }
}
