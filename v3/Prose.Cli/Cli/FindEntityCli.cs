using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --find-entity --name "&lt;text&gt;" [--type character] [--universe &lt;slug&gt;] [--limit N]</c>
/// — search seeded entities by name or registered alias, case-insensitive substring.
///
/// Built 2026-08-23 while seeding a new book's cast. There was no read-side command anywhere on
/// the CLI for "does this character already exist?" — only write-side (<c>--add-character</c>) and
/// repair-side (<c>--duplicate-entity-scan</c>, <c>--merge-entity</c>) tooling. That asymmetry is
/// the direct cause of the duplicate-entity problem those repair tools exist to clean up: with no
/// cheap way to check first, the path of least resistance during authoring is to seed a second
/// "Kyle" and discover the collision later. Searching aliases as well as canonical names matters
/// for the same reason — a character seeded under a street handle won't be found by their legal
/// name, and vice versa.
/// </summary>
public static class FindEntityCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var name = args.SkipWhile(a => a != "--name").Skip(1).FirstOrDefault()
                   ?? args.SkipWhile(a => a != "--query").Skip(1).FirstOrDefault();
        var type = args.SkipWhile(a => a != "--type").Skip(1).FirstOrDefault();
        var limitStr = args.SkipWhile(a => a != "--limit").Skip(1).FirstOrDefault();
        var limit = int.TryParse(limitStr, out var l) ? Math.Clamp(l, 1, 200) : 40;

        if (string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("Usage: prose --find-entity --name \"<text>\" [--type character] [--universe <slug>] [--limit N]");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var needle = name.Trim().ToLowerInvariant();

        // Ambient universe scope is respected here (no IgnoreQueryFilters): unlike an explicit
        // id/slug lookup, this is a browse — and Universe division is absolute, so surfacing a
        // same-named entity from another universe as a "match" would invite exactly the
        // cross-universe mis-link this command exists to prevent.
        var q = db.Entities.AsNoTracking().Where(e => e.Name.ToLower().Contains(needle));
        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(e => e.EntityType == type);

        // Rank exact match first, then prefix, then substring — NOT alphabetical. Caught
        // 2026-08-23 within minutes of writing this command: searching "Able" sorted
        // alphabetically and truncated at --limit, so "Mr. Able" (an exact-substring hit, and a
        // major existing character) fell off the end behind AquaPure/AwakeAll/Axiom, and the
        // command reported what looked like "not seeded." For a tool whose entire job is
        // answering "does this already exist?", a false negative is the one unacceptable
        // failure — it produces the duplicate entity the command exists to prevent.
        var byName = await q
            .OrderBy(e => e.Name.ToLower() == needle ? 0 : e.Name.ToLower().StartsWith(needle) ? 1 : 2)
            .ThenBy(e => e.Name.Length)
            .ThenBy(e => e.Name)
            .Take(limit)
            .Select(e => new { e.Id, e.Name, e.EntityType, e.Slug, e.OriginNodeId })
            .ToListAsync();

        var aliasHits = await db.CharacterAliases.AsNoTracking()
            .Where(a => a.Value.ToLower().Contains(needle))
            .Join(db.Entities.AsNoTracking(), a => a.CharacterId, e => e.Id,
                  (a, e) => new { e.Id, e.Name, e.EntityType, e.Slug, e.OriginNodeId, Alias = a.Value })
            .Take(limit)
            .ToListAsync();

        var nameIds = byName.Select(x => x.Id).ToHashSet();
        var extraAlias = aliasHits.Where(a => !nameIds.Contains(a.Id)).ToList();

        if (byName.Count == 0 && extraAlias.Count == 0)
        {
            Console.WriteLine($"[find-entity] No entity matching \"{name}\"" +
                              (string.IsNullOrWhiteSpace(type) ? "" : $" of type '{type}'") + ".");
            return 0;
        }

        // Resolve origin book codes in one round-trip rather than per row.
        var originIds = byName.Select(x => x.OriginNodeId).Concat(extraAlias.Select(x => x.OriginNodeId))
            .Where(g => g != null).Select(g => g!.Value).Distinct().ToList();
        var originCodes = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => originIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n.NodeCode ?? n.Slug);

        string Origin(Guid? id) => id != null && originCodes.TryGetValue(id.Value, out var c) ? c : "—";

        Console.WriteLine($"\n{"TYPE",-14} {"NAME",-32} {"ORIGIN",-10} SLUG / ID");
        Console.WriteLine(new string('-', 100));
        foreach (var e in byName)
            Console.WriteLine($"{e.EntityType,-14} {Trunc(e.Name, 32),-32} {Origin(e.OriginNodeId),-10} {e.Slug}");
        foreach (var a in extraAlias)
            Console.WriteLine($"{a.EntityType,-14} {Trunc(a.Name, 32),-32} {Origin(a.OriginNodeId),-10} {a.Slug}   (alias: \"{a.Alias}\")");

        Console.WriteLine();
        Console.WriteLine($"[find-entity] {byName.Count + extraAlias.Count} match(es) for \"{name}\".");
        return 0;
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
