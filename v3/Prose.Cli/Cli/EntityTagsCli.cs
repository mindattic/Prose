using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --entity-tags --entity &lt;guid-or-name&gt; [--json]
/// prose --entity-tags --entity &lt;guid-or-name&gt; --remove "tag1,tag2"
/// prose --entity-tags --entity &lt;guid-or-name&gt; --add "tag1,tag2"
///
/// The missing half of entity tagging: tags could be ADDED (via <c>create_character</c>'s
/// <c>tags</c>) and never taken away, because that path merges rather than replaces. A wrong tag
/// was therefore permanent — and a stale book tag is not cosmetic, it can pull a character into
/// that book's context loads. See <see cref="EntityTagService"/> for the full rationale.
///
/// <para>Not to be confused with <c>--tag-entities</c>, which rewrites inline
/// <c>&lt;entity guid="…"&gt;</c> markup inside beat TEXT. Different table, different job.</para>
/// </summary>
public static class EntityTagsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var tags = services.GetRequiredService<EntityTagService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        var who = Flag(args, "--entity") ?? Flag(args, "--id") ?? Flag(args, "--name");
        if (string.IsNullOrWhiteSpace(who))
        {
            Console.Error.WriteLine(
                "Usage: prose --entity-tags --entity <guid-or-name> [--json]\n" +
                "       prose --entity-tags --entity <guid-or-name> --remove \"tag1,tag2\"\n" +
                "       prose --entity-tags --entity <guid-or-name> --add \"tag1,tag2\"");
            return 2;
        }

        var resolved = await ResolveAsync(dbFactory, who);
        if (resolved == null)
        {
            Console.Error.WriteLine($"[entity-tags] No entity matched '{who}'.");
            return 2;
        }
        var (id, name) = resolved.Value;

        if (Flag(args, "--remove") is { } removeList && !string.IsNullOrWhiteSpace(removeList))
        {
            var removed = await tags.RemoveAsync(id, Split(removeList));
            Console.WriteLine(removed.Count == 0
                ? $"[entity-tags] '{name}' carried none of those tags — nothing removed."
                : $"[entity-tags] Removed {removed.Count} tag(s) from '{name}': {string.Join(", ", removed)}");
        }

        if (Flag(args, "--add") is { } addList && !string.IsNullOrWhiteSpace(addList))
        {
            var added = await tags.AddAsync(id, Split(addList));
            Console.WriteLine(added.Count == 0
                ? $"[entity-tags] '{name}' already carried those tags — nothing added."
                : $"[entity-tags] Added {added.Count} tag(s) to '{name}': {string.Join(", ", added)}");
        }

        var current = await tags.ListAsync(id);
        if (args.Contains("--json"))
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                new { entity = name, entityId = id, tags = current },
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"[entity-tags] '{name}' ({id:N}) — {current.Count} tag(s): " +
                          (current.Count == 0 ? "(none)" : string.Join(", ", current)));
        return 0;
    }

    /// <summary>Resolve by Guid (any format) or exact Name. IgnoreQueryFilters on the id path so an
    /// explicitly-named id resolves regardless of the ambient universe.</summary>
    private static async Task<(Guid Id, string Name)?> ResolveAsync(
        IDbContextFactory<ProseDbContext> dbFactory, string who)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(who, out var parsed))
        {
            var byId = await db.Entities.IgnoreQueryFilters().AsNoTracking()
                .Where(e => e.Id == parsed).Select(e => new { e.Id, e.Name }).FirstOrDefaultAsync();
            if (byId != null) return (byId.Id, byId.Name);
        }
        var byName = await db.Entities.AsNoTracking()
            .Where(e => e.Name == who).Select(e => new { e.Id, e.Name }).FirstOrDefaultAsync();
        return byName == null ? null : (byName.Id, byName.Name);
    }

    private static List<string> Split(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string? Flag(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
