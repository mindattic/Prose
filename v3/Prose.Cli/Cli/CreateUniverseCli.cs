using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --create-universe --slug &lt;slug&gt; --name &lt;name&gt; [--theme &lt;theme&gt;] [--description &lt;text&gt;]</c>
///
/// Inserts a new <see cref="Universe"/> row. Confirmed gap as of 2026-08-30: no prior tool could
/// create a universe at all — only list/switch (<see cref="UniverseCli"/>) and rename-in-place
/// (<see cref="RenameUniverseCli"/>) existed. A fresh universe starts with no books; use
/// <c>prose --create-book --universe &lt;slug&gt;</c> or <c>prose --move-node-universe</c> to
/// populate it.
/// </summary>
public static class CreateUniverseCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, name = null, theme = null, description = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":        if (i + 1 < args.Length) slug = args[++i]; break;
                case "--name":        if (i + 1 < args.Length) name = args[++i]; break;
                case "--theme":       if (i + 1 < args.Length) theme = args[++i]; break;
                case "--description": if (i + 1 < args.Length) description = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("[create-universe] --slug and --name are both required.");
            Console.Error.WriteLine("Usage: prose --create-universe --slug <slug> --name <name> [--theme <theme>] [--description <text>]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var clash = await db.Universes.AnyAsync(u => u.Slug == slug);
        if (clash)
        {
            Console.Error.WriteLine($"[create-universe] Slug '{slug}' is already in use. Run 'prose --universe list' to see existing universes.");
            return 1;
        }

        var maxSortKey = await db.Universes.MaxAsync(u => (double?)u.SortKey) ?? 0;

        var universe = new Universe
        {
            Slug = slug,
            Name = name,
            Theme = theme,
            Description = description,
            SortKey = maxSortKey + 100,
        };
        db.Universes.Add(universe);
        await db.SaveChangesAsync();

        // IUniverseContext caches the universe catalog in memory — without this, the Hub won't
        // resolve the new slug until its next restart (same reload RenameUniverseCli does).
        services.GetRequiredService<IUniverseContext>().Refresh();

        Console.WriteLine($"[create-universe] Created universe '{universe.Slug}' ({universe.Name}), Id={universe.Id}.");
        return 0;
    }
}
