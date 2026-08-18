using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --archive-book (--id ... | --slug ... | --all) [--reason "..."]
///
/// Snapshots a book's ENTIRE current live prose — walking the full book-&gt;chapter-&gt;...-&gt;beat
/// descendant tree via <see cref="NodeWorkbenchService.GetLeafDescendantIdsAsync"/> (proper
/// reading order, not just beats directly on the book node) — into one <c>ArchivedBooks</c> row,
/// as a pre-edit backup. Read-only against Beats/Nodes/BeatNodes: this command never deletes or
/// modifies any existing content, only adds a snapshot row. Beats/Nodes/BeatNodes are once again
/// system-versioned (corpus-trust-recovery Phase -1a), so <c>Beats_History</c> already recovers any
/// single edit — this snapshot is a second, independent, human-readable safety net on top of that,
/// which matters most right before a corpus-wide mutation (e.g. Phase 1a's entity-tagging backfill)
/// where the volume of change makes "diff every History row by hand" impractical.
///
/// <c>--all</c> resolves every book-level node across every universe via <c>IgnoreQueryFilters()</c>
/// (this command explicitly targets rows by id/slug/kind, never an ambient universe default, so it
/// is exempt from the <c>--universe</c> requirement — see <c>UniverseAgnosticCommands</c> in
/// Program.cs) and archives each one in turn, continuing past any single book's failure so one bad
/// node can't abort the whole corpus-wide run.
/// </summary>
public static class ArchiveBookCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, reason = null;
        var all = args.Contains("--all");
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":     if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":   if (i + 1 < args.Length) slug = args[++i]; break;
                case "--reason": if (i + 1 < args.Length) reason = args[++i]; break;
            }
        }

        if (!all && string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[archive-book] One of --id, --slug, or --all is required.");
            Console.Error.WriteLine("Usage: prose --archive-book (--id ... | --slug ... | --all) [--reason \"...\"]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var archiveSvc = services.GetRequiredService<BookArchiveService>();
        var effectiveReason = string.IsNullOrWhiteSpace(reason) ? "manual-backup" : reason;

        await using var db = await dbFactory.CreateDbContextAsync();

        List<(Guid Id, string Title, string? Slug, int Version)> targets;
        if (all)
        {
            var rows = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                .Where(n => n.Kind == "book")
                .OrderBy(n => n.Title)
                .Select(n => new { n.Id, n.Title, n.Slug, n.Version })
                .ToListAsync();
            targets = rows.Select(r => (r.Id, r.Title, (string?)r.Slug, r.Version)).ToList();
        }
        else
        {
            var node = !string.IsNullOrWhiteSpace(slug)
                ? await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug)
                : Guid.TryParse(id, out var g)
                    ? await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == g)
                    : null;
            if (node == null)
            {
                Console.Error.WriteLine("[archive-book] Target node not found.");
                return 1;
            }
            targets = [(node.Id, node.Title, node.Slug, node.Version)];
        }

        if (targets.Count == 0)
        {
            Console.Error.WriteLine("[archive-book] No book-level nodes found.");
            return 1;
        }

        int ok = 0, fail = 0;
        foreach (var (nodeId, title, nodeSlug, version) in targets)
        {
            try
            {
                var result = await archiveSvc.ArchiveAsync(nodeId, effectiveReason);
                Console.WriteLine($"[archive-book] Archived '{title}' ({nodeSlug}) V{version} — " +
                    $"leaves={result.LeafNodeCount} beats={result.BeatCount} words={result.WordCount:N0} " +
                    $"ArchivedBooks.Id={result.ArchivedBookId}");
                ok++;
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"[archive-book] '{title}' ({nodeSlug}) — {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"[archive-book] Done: {ok} archived, {fail} failed, out of {targets.Count} target(s).");
        return fail > 0 && ok == 0 ? 1 : 0;
    }
}
