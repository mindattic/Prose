using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --archive-book (--id ... | --slug ...) [--reason "..."]
///
/// Snapshots a book's ENTIRE current live prose — walking the full book-&gt;chapter-&gt;...-&gt;beat
/// descendant tree via <see cref="NodeWorkbenchService.GetLeafDescendantIdsAsync"/> (proper
/// reading order, not just beats directly on the book node) — into one <c>ArchivedBooks</c> row,
/// as a pre-edit backup. Read-only against Beats/Nodes/BeatNodes: this command never deletes or
/// modifies any existing content, only adds a snapshot row. Beats/Nodes/BeatNodes are no longer
/// system-versioned (SS: DropBiTemporalAndMtld) and there is no soft-delete anymore
/// (NodeWorkbenchService.DeleteBeatAsync), so an explicit snapshot before any destructive editing
/// pass is the only way to recover prior content later.
/// </summary>
public static class ArchiveBookCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, reason = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":     if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":   if (i + 1 < args.Length) slug = args[++i]; break;
                case "--reason": if (i + 1 < args.Length) reason = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[archive-book] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: prose --archive-book (--id ... | --slug ...) [--reason \"...\"] --universe <u>");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = !string.IsNullOrWhiteSpace(slug)
            ? await db.Nodes.FirstOrDefaultAsync(n => n.Slug == slug)
            : Guid.TryParse(id, out var g)
                ? await db.Nodes.FirstOrDefaultAsync(n => n.Id == g)
                : null;

        if (node == null)
        {
            Console.Error.WriteLine("[archive-book] Target node not found.");
            return 1;
        }

        var archiveSvc = services.GetRequiredService<BookArchiveService>();
        BookArchiveResult result;
        try
        {
            result = await archiveSvc.ArchiveAsync(node.Id, string.IsNullOrWhiteSpace(reason) ? "manual-backup" : reason);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"[archive-book] {ex.Message}");
            return 1;
        }

        Console.WriteLine($"[archive-book] Archived '{node.Title}' ({node.Slug}) V{node.Version}");
        Console.WriteLine($"[archive-book]   leaf nodes walked: {result.LeafNodeCount}, beats: {result.BeatCount}, words: {result.WordCount:N0}");
        Console.WriteLine($"[archive-book]   ArchivedBooks.Id = {result.ArchivedBookId}");
        Console.WriteLine($"[archive-book]   VERIFIED: row present in DB.");
        return 0;
    }
}
