using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --set-narrative-mode --slug &lt;slug-or-code&gt; --mode original|retelling|historical</c>
///
/// Sets <see cref="Prose.Core.Data.Entities.Node.NarrativeMode"/> on a book node. This gates
/// whether personality/goal-drift checks apply (<c>BookHealthService.SacredFlawAsync</c> /
/// <c>NarrativeScienceService.AnalyzeSacredFlawAsync</c>): "original" fiction has author-invented
/// psychology that must stay internally consistent; "retelling" (a close/1:1 adaptation of a
/// pre-existing fixed narrative — e.g. Paradise Lost, the Gospels) and "historical" (nonfiction —
/// real people/events) both have motivations already fixed by an external source, so the
/// sacred-flaw "ground this character's flaw" nudge is a category error for them.
/// </summary>
public static class SetNarrativeModeCli
{
    private static readonly string[] ValidModes = ["original", "retelling", "historical"];

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, mode = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[++i];
            if (args[i] == "--mode" && i + 1 < args.Length) mode = args[++i];
        }

        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(mode))
        {
            Console.Error.WriteLine("Usage: prose --set-narrative-mode --slug <slug-or-code> --mode original|retelling|historical");
            return 2;
        }

        mode = mode.Trim().ToLowerInvariant();
        if (!ValidModes.Contains(mode))
        {
            Console.Error.WriteLine($"[set-narrative-mode] Invalid mode '{mode}'. Valid: {string.Join(", ", ValidModes)}");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.FirstOrDefaultAsync(n => n.Slug == slug || n.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"[set-narrative-mode] No node found with slug or code '{slug}'.");
            return 1;
        }

        var previous = node.NarrativeMode;
        node.NarrativeMode = mode;
        await db.SaveChangesAsync();

        Console.WriteLine($"[set-narrative-mode] {node.Title} ({node.NodeCode ?? node.Slug}): {previous} -> {mode}");
        return 0;
    }
}
