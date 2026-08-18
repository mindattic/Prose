using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// <c>prose --backfill-missing-subtype-rows [--dry-run] [--exclude-name "&lt;name&gt;"]...</c>
///
/// One-time data repair for a corpus-wide integrity gap found 2026-08-18: 477 <c>Entities</c>
/// rows typed <c>character</c>/<c>place</c> exist with no matching <c>Characters</c>/<c>Places</c>
/// subtype row. Root-caused to raw SQL writes made directly against the DB, bypassing the app
/// entirely — every current write path (<see cref="Prose.Core.Services.Repositories"/>'s
/// CharacterRepository/PlaceRepository, <see cref="Prose.Core.Services.FactInterpreterService"/>'s
/// stub creator, the create_character/create_place MCP tools) already inserts the subtype row
/// correctly, so there is no live code bug to fix here — only the debris left behind by writes
/// that never went through any of them.
///
/// Inserts exactly the same minimal shape <c>FactInterpreterService.CreateStubAsync</c> already
/// uses for a legitimate stub (Id + Name only, nothing else) — additive and non-destructive; a
/// row that already has a subtype row is left untouched. Deliberately corpus-wide via
/// <c>IgnoreQueryFilters()</c> — the 477 orphans span GLMZ/SCRY/NONFICTION, and an ambient
/// universe scope would silently fix only one.
/// </summary>
public static class BackfillMissingSubtypeRowsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        var excludeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--exclude-name" && i + 1 < args.Length)
                excludeNames.Add(args[++i]);

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var charOrphans = await db.Entities.IgnoreQueryFilters()
            .Where(e => e.EntityType == "character" && !db.Characters.Any(c => c.Id == e.Id))
            .Select(e => new { e.Id, e.Name })
            .ToListAsync();
        var placeOrphans = await db.Entities.IgnoreQueryFilters()
            .Where(e => e.EntityType == "place" && !db.Places.Any(p => p.Id == e.Id))
            .Select(e => new { e.Id, e.Name })
            .ToListAsync();

        var skipped = new List<string>();
        var charFixed = 0;
        var placeFixed = 0;

        foreach (var o in charOrphans)
        {
            if (excludeNames.Contains(o.Name)) { skipped.Add($"character:{o.Name}"); continue; }
            if (!dryRun) db.Characters.Add(new Character { Id = o.Id, Name = o.Name });
            charFixed++;
        }
        foreach (var o in placeOrphans)
        {
            if (excludeNames.Contains(o.Name)) { skipped.Add($"place:{o.Name}"); continue; }
            if (!dryRun) db.Places.Add(new Place { Id = o.Id, Name = o.Name });
            placeFixed++;
        }

        if (!dryRun) await db.SaveChangesAsync();

        Console.WriteLine($"[backfill-missing-subtype-rows] {(dryRun ? "[dry-run] would insert" : "inserted")} " +
            $"{charFixed} Characters row(s), {placeFixed} Places row(s).");
        if (skipped.Count > 0)
            Console.WriteLine($"[backfill-missing-subtype-rows] skipped (--exclude-name): {string.Join(", ", skipped)}");

        return 0;
    }
}
