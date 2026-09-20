using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// <c>prose --backfill-missing-subtype-rows [--dry-run] [--exclude-name "&lt;name&gt;"]...</c>
///
/// One-time data repair for a corpus-wide integrity gap found 2026-08-18: <c>Entities</c> rows
/// exist with no matching relational subtype row, across 11 types (character, place, faction,
/// quote, vocabulary, archetype, material, transportation, ammunition, contract, news).
/// Root-caused to raw SQL writes made directly against the DB, bypassing the app entirely —
/// every current write path already inserts the subtype row correctly, so there is no live code
/// bug to fix; only the debris left behind by writes that never went through any of them.
///
/// <c>prose --retire-records-blobs --rebuild</c> (RFC 0007) already covers most relational
/// types via each Mapper's <c>RebuildAllAsync</c> — but only for orphans that still have a
/// <c>Records.Json</c> blob to rebuild from. The 11 types here are exactly the ones whose
/// mapper has no "no blob, no relational row" minimal-stub fallback (unlike e.g.
/// <c>DocumentMapper.RebuildAllAsync</c>), and whose C# subtype class defaults every
/// non-nullable string column to <c>""</c> — the same convention
/// <c>FactInterpreterService.CreateStubAsync</c> already relies on for its own stub inserts —
/// so an Id+Name-only row is safe to insert for all eleven.
///
/// Additive and non-destructive; a row that already has a subtype row is left untouched.
/// Deliberately corpus-wide via <c>IgnoreQueryFilters()</c> — orphans span GLMZ/SCRY/NONFICTION,
/// and an ambient universe scope would silently fix only one.
/// </summary>
public static class BackfillMissingSubtypeRowsCli
{
    private sealed record TypeDef(
        string EntityType,
        Func<ProseDbContext, IQueryable<Guid>> ExistingIds,
        Action<ProseDbContext, Guid, string> Insert);

    private static readonly TypeDef[] Types =
    [
        new("character",      db => db.Characters.Select(x => x.Id),        (db, id, name) => db.Characters.Add(new Character { Id = id, Name = name })),
        new("place",          db => db.Places.Select(x => x.Id),            (db, id, name) => db.Places.Add(new Place { Id = id, Name = name })),
        new("faction",        db => db.Factions.Select(x => x.Id),          (db, id, name) => db.Factions.Add(new Faction { Id = id, Name = name })),
        new("quote",          db => db.Quotes.Select(x => x.Id),            (db, id, name) => db.Quotes.Add(new Quote { Id = id, Name = name })),
        new("vocabulary",     db => db.VocabularyEntries.Select(x => x.Id), (db, id, name) => db.VocabularyEntries.Add(new Vocabulary { Id = id, Name = name })),
        new("archetype",      db => db.Archetypes.Select(x => x.Id),        (db, id, name) => db.Archetypes.Add(new ArchetypeRow { Id = id, Name = name })),
        new("material",       db => db.Materials.Select(x => x.Id),         (db, id, name) => db.Materials.Add(new Material { Id = id, Name = name })),
        new("transportation", db => db.Transportations.Select(x => x.Id),   (db, id, name) => db.Transportations.Add(new Transportation { Id = id, Name = name })),
        new("ammunition",     db => db.Ammunitions.Select(x => x.Id),       (db, id, name) => db.Ammunitions.Add(new Ammunition { Id = id, Name = name })),
        new("contract",       db => db.Contracts.Select(x => x.Id),         (db, id, name) => db.Contracts.Add(new Contract { Id = id, Name = name })),
        new("news",           db => db.News.Select(x => x.Id),              (db, id, name) => db.News.Add(new News { Id = id, Name = name })),
    ];

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        var excludeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--exclude-name" && i + 1 < args.Length)
                excludeNames.Add(args[++i]);

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var skipped = new List<string>();

        foreach (var t in Types)
        {
            var existingIds = await t.ExistingIds(db).ToHashSetAsync();
            var orphans = await db.Entities.IgnoreQueryFilters()
                .Where(e => e.EntityType == t.EntityType && !existingIds.Contains(e.Id))
                .Select(e => new { e.Id, e.Name })
                .ToListAsync();

            var fixedCount = 0;
            foreach (var o in orphans)
            {
                if (excludeNames.Contains(o.Name)) { skipped.Add($"{t.EntityType}:{o.Name}"); continue; }
                if (!dryRun) t.Insert(db, o.Id, o.Name);
                fixedCount++;
            }

            if (orphans.Count > 0 || fixedCount > 0)
                Console.WriteLine($"[backfill-missing-subtype-rows] {t.EntityType,-16} {(dryRun ? "would insert" : "inserted")} {fixedCount}");
        }

        if (!dryRun) await db.SaveChangesAsync();

        if (skipped.Count > 0)
            Console.WriteLine($"[backfill-missing-subtype-rows] skipped (--exclude-name): {string.Join(", ", skipped)}");

        return 0;
    }
}
