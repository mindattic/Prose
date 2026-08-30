using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --reconcile-book-entities (--id &lt;guid&gt; | --slug &lt;slug&gt; | --all) --universe &lt;u&gt;
///
/// Phase 0 (repair) of the corpus-trust-recovery plan: the book is canon right now. Syncs each
/// already-linked live Entity's stored facts against what this book's CURRENT bible actually
/// says, and flags factual drift. Separately reports bible characters with no live entity at
/// all, and the corpus-wide count of entities with zero live connection to any book (not
/// attributed to a specific character — see BookEntityReconciliationService's remarks for why
/// that per-character matching was retired). Findings persist via FindingsService
/// (FindingCategory.EntityDrift) — report-only against the live Entities/Nodes tables, but not
/// ephemeral: nothing is merged/renamed automatically, but the finding itself is durable and
/// trackable, not console-only.
/// </summary>
public static class ReconcileBookEntitiesCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var id = Flag(args, "--id");
        var slug = Flag(args, "--slug");
        var all = args.Contains("--all");

        if (!all && string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --reconcile-book-entities (--id <guid> | --slug <slug> | --all) --universe <u>");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var svc = services.GetRequiredService<BookEntityReconciliationService>();

        List<(Guid Id, string Title, string Slug)> targets;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            if (all)
            {
                var rows = await db.Nodes.AsNoTracking()
                    .Where(n => n.Kind == "book" && n.NodeOutline != null)
                    .OrderBy(n => n.Title)
                    .Select(n => new { n.Id, n.Title, n.Slug })
                    .ToListAsync();
                targets = rows.Select(r => (r.Id, r.Title, r.Slug)).ToList();
            }
            else
            {
                var node = !string.IsNullOrWhiteSpace(slug)
                    // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
                    ? await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Slug == slug)
                    : Guid.TryParse(id, out var g)
                        // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
                        ? await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == g)
                        : null;
                if (node == null)
                {
                    Console.Error.WriteLine("[reconcile-book-entities] Target node not found.");
                    return 1;
                }
                targets = new() { (node.Id, node.Title, node.Slug) };
            }
        }

        if (targets.Count == 0)
        {
            Console.Error.WriteLine("[reconcile-book-entities] No book nodes with a NodeOutline found in this universe scope.");
            return 1;
        }

        Console.WriteLine($"[reconcile-book-entities] Processing {targets.Count} book(s)…");
        int ok = 0, fail = 0, totalDrift = 0, totalUnmatched = 0;
        foreach (var (nodeId, nodeTitle, nodeSlug) in targets)
        {
            try
            {
                var report = await svc.ReconcileAsync(nodeId);
                totalDrift += report.DriftFindings.Count;
                totalUnmatched += report.UnmatchedBibleCharacters.Count;

                var status = report.DriftFindings.Count == 0 && report.UnmatchedBibleCharacters.Count == 0
                    ? "clean" : $"{report.DriftFindings.Count} drift, {report.UnmatchedBibleCharacters.Count} unmatched";
                Console.WriteLine($"  ✓ {nodeTitle} ({nodeSlug}) — {status}" +
                    (report.BibleTruncated ? " [bible truncated]" : ""));
                ok++;
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"  ✗ {nodeTitle} ({nodeSlug}) — {ex.Message}");
                fail++;
            }
        }

        Console.WriteLine($"[reconcile-book-entities] Done: {ok} succeeded ({fail} skipped/failed). " +
            $"{totalDrift} drift finding(s), {totalUnmatched} unmatched-character finding(s) — see Findings table (category=EntityDrift).");
        return (totalDrift + totalUnmatched) > 0 ? 1 : 0;
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
