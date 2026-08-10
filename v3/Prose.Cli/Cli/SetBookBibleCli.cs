using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --set-book-bible --slug &lt;slug&gt; --file &lt;path&gt;
///
/// CLI mirror of the MCP tool SetBookBible (Tools.Nodes.cs) — hand-write the node bible text
/// verbatim instead of generating it, then cascade regeneration (docs/nodes/{CODE}.md +
/// MarkdownFiles sync) exactly as the MCP path does. Added 2026-08-10 because MCP tools are not
/// reachable in every session; Stage 4 of the locked New Story Workflow (CLAUDE.md) requires a
/// hand-authored bible write regardless of which surface is available.
/// </summary>
public static class SetBookBibleCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, filePath = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
                case "--file": if (i + 1 < args.Length) filePath = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(filePath))
        {
            Console.Error.WriteLine("[set-book-bible] --slug and --file are both required.");
            Console.Error.WriteLine("Usage: prose --set-book-bible --slug <slug> --file <path-to-bible.md>");
            return 2;
        }

        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"[set-book-bible] File not found: {filePath}");
            return 1;
        }
        var bibleText = await File.ReadAllTextAsync(filePath);

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var nodeDoc = services.GetRequiredService<NodeDocService>();
        var markdownFiles = services.GetRequiredService<MarkdownFileService>();

        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.FirstOrDefaultAsync(n => n.Slug == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"[set-book-bible] No node found with slug '{slug}'.");
            return 1;
        }

        node.NodeBible = string.IsNullOrEmpty(bibleText) ? null : bibleText;
        node.NodeBibleGeneratedAt = DateTime.UtcNow;
        node.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var genResult = await nodeDoc.GenerateAsync(node.Id);
        var syncResult = await markdownFiles.SyncAllAsync();

        Console.WriteLine($"[set-book-bible] Saved {bibleText.Length} chars to Nodes.NodeBible for '{node.Title}' ({node.Slug}).");
        Console.WriteLine($"[set-book-bible] Regenerated: {genResult.Path}");
        Console.WriteLine($"[set-book-bible] Synced: inserted={syncResult.Inserted} updated={syncResult.Updated} unchanged={syncResult.Unchanged} errors={syncResult.Errors.Count}");
        foreach (var err in syncResult.Errors) Console.WriteLine($"  SYNC ERROR: {err}");
        return 0;
    }
}
