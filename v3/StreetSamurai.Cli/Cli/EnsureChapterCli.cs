using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --ensure-chapter --slug &lt;slug&gt; | --all
///
/// Enforces the "every story has at least one chapter" invariant. A flat
/// (chapterless) story — one whose beats hang directly on the story node — gets
/// its beats wrapped into a single <c>ChapterNode</c> child, re-pointed and in
/// reading order. Already-chaptered stories are left untouched. No LLM, no prose
/// changes. Renderers suppress the heading when a story resolves to one chapter,
/// so no "Chapter 1" label is ever printed.
/// </summary>
public static class EnsureChapterCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool all = args.Contains("--all");
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }

        if (slug == null && !all)
        {
            Console.Error.WriteLine("Usage: ss --ensure-chapter --slug <slug> | --all");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // Resolve the target story set. --all = every flat story (no chapter children,
        // has direct beats), across every universe — explicit via IgnoreQueryFilters()
        // rather than depending on no universe happening to be ambient in this process.
        var targets = new List<(Guid Id, string Slug, string Title)>();
        if (all)
        {
            var flat = await db.Nodes.OfType<BookNode>().AsNoTracking().IgnoreQueryFilters()
                .Where(n => !db.Nodes.IgnoreQueryFilters().Any(c => c.ParentNodeId == n.Id)
                    && db.BeatNodes.Any(b => b.NodeId == n.Id && b.IsEnabled))
                .Select(n => new { n.Id, n.Slug, n.Title }).ToListAsync();
            foreach (var f in flat) targets.Add((f.Id, f.Slug, f.Title));
        }
        else
        {
            var node = await db.Nodes.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
            if (node == null) { Console.Error.WriteLine($"Node '{slug}' not found."); return 2; }
            targets.Add((node.Id, node.Slug, node.Title));
        }

        if (targets.Count == 0)
        {
            Console.WriteLine("No flat stories found — every story already has at least one chapter.");
            return 0;
        }

        int wrapped = 0, skipped = 0;
        foreach (var t in targets)
        {
            try
            {
                var result = await workbench.WrapInSingleChapterAsync(t.Id);
                if (result is null)
                {
                    Console.WriteLine($"  skip  {t.Title} — already chaptered.");
                    skipped++;
                }
                else
                {
                    Console.WriteLine($"  wrap  {t.Title} → chapter '{result.Value.Slug}' ({result.Value.Beats} beats).");
                    wrapped++;
                }
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"  FAIL  {t.Title} — {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Wrapped {wrapped} flat stor{(wrapped == 1 ? "y" : "ies")}; {skipped} already chaptered.");
        return 0;
    }
}
