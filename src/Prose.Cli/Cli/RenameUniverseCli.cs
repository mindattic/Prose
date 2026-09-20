using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --rename-universe --slug &lt;oldSlug&gt; --new-slug &lt;newSlug&gt; --new-name &lt;newName&gt; [--new-theme &lt;newTheme&gt;]</c>
///
/// Renames a Universe row IN PLACE (same Id, same UniverseId FK on every Node/Entity/Book
/// already scoped to it) — a seamless cutover, not a copy-and-migrate. Only Slug/Name/Theme
/// change; every book, chapter, beat, and entity already stamped with this universe's Id keeps
/// working unmodified. Added 2026-08-27 — no prior tool could rename a universe's own identity,
/// only list/switch/create-adjacent operations existed.
/// </summary>
public static class RenameUniverseCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? oldSlug = null, newSlug = null, newName = null, newTheme = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":      if (i + 1 < args.Length) oldSlug  = args[++i]; break;
                case "--new-slug":  if (i + 1 < args.Length) newSlug  = args[++i]; break;
                case "--new-name":  if (i + 1 < args.Length) newName  = args[++i]; break;
                case "--new-theme": if (i + 1 < args.Length) newTheme = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(oldSlug) || string.IsNullOrWhiteSpace(newSlug) || string.IsNullOrWhiteSpace(newName))
        {
            Console.Error.WriteLine("[rename-universe] --slug, --new-slug, and --new-name are all required.");
            Console.Error.WriteLine("Usage: prose --rename-universe --slug <oldSlug> --new-slug <newSlug> --new-name <newName> [--new-theme <newTheme>]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var universe = await db.Universes.FirstOrDefaultAsync(u => u.Slug == oldSlug);
        if (universe == null)
        {
            Console.Error.WriteLine($"[rename-universe] No universe found with slug '{oldSlug}'. Run 'prose --universe list' to see available slugs.");
            return 1;
        }

        if (!string.Equals(oldSlug, newSlug, StringComparison.OrdinalIgnoreCase))
        {
            var clash = await db.Universes.AnyAsync(u => u.Slug == newSlug && u.Id != universe.Id);
            if (clash)
            {
                Console.Error.WriteLine($"[rename-universe] Slug '{newSlug}' is already in use by another universe.");
                return 1;
            }
        }

        var prevSlug = universe.Slug;
        var prevName = universe.Name;
        var prevTheme = universe.Theme;

        universe.Slug = newSlug;
        universe.Name = newName;
        if (newTheme != null) universe.Theme = newTheme;

        await db.SaveChangesAsync();

        // IUniverseContext caches the universe catalog in memory (ambient singleton, read on every
        // --universe <slug> resolution) — without this, the Hub keeps resolving the OLD slug until
        // its next restart, even though the DB row is already renamed.
        services.GetRequiredService<IUniverseContext>().Refresh();

        Console.WriteLine($"[rename-universe] Id {universe.Id}: slug '{prevSlug}' -> '{universe.Slug}', name '{prevName}' -> '{universe.Name}'" +
            (newTheme != null ? $", theme '{prevTheme}' -> '{universe.Theme}'" : ""));
        Console.WriteLine("[rename-universe] Every Node/Entity/Book already scoped to this universe's Id follows automatically — no data was moved.");
        return 0;
    }
}
