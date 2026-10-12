using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --set-node-layout --slug &lt;slug-or-code&gt; --layout prose|lyrics|screenplay</c>
///
/// Sets <see cref="Prose.Core.Data.Entities.Node.Layout"/>: how the book's beats print in every
/// export format (<see cref="BeatLayout"/>). The beats themselves are untouched.
/// </summary>
public static class SetNodeLayoutCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, layout = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[++i];
            if (args[i] == "--layout" && i + 1 < args.Length) layout = args[++i];
        }

        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(layout))
        {
            Console.Error.WriteLine("Usage: prose --set-node-layout --slug <slug-or-code> --layout prose|lyrics|screenplay");
            return 2;
        }

        layout = layout.Trim().ToLowerInvariant();
        if (!BeatLayout.Valid.Contains(layout))
        {
            Console.Error.WriteLine($"[set-node-layout] Invalid layout '{layout}'. Valid: {string.Join(", ", BeatLayout.Valid)}");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // NodeRefResolver, then a TRACKED load by id (this command writes the node).
        var slugId = await NodeRefResolver.ResolveAsync(db, slug) ?? Guid.Empty;
        var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == slugId);
        if (node == null)
        {
            Console.Error.WriteLine($"[set-node-layout] No node found with slug or code '{slug}'.");
            return 1;
        }

        var previous = node.Layout;
        node.Layout = layout;
        node.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        Console.WriteLine($"[set-node-layout] {node.Title} ({node.NodeCode ?? node.Slug}): {previous} -> {layout}");
        return 0;
    }
}
