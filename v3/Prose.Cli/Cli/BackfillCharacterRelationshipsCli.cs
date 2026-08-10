using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --backfill-character-relationships [--universe glmz|scry|...] [--dry-run]
///
/// One-time repair: CharacterMapper.FillBridges never called ResolveEntityId when building
/// CharacterRelationshipRow, unlike every other bridge in the same file (and unlike
/// FactionMapper's equivalent for FactionRelationships) — found 2026-08-10 while seeding a new
/// book's cast and confirming corpus-wide that ALL 493 existing CharacterRelationships rows had
/// a null TargetEntityId. The code fix (CharacterMapper.cs) only takes effect for future saves;
/// this backfills what already exists.
///
/// Matches ResolveEntityIdAny's own logic exactly (exact Name, then Slug) but restricted to the
/// SAME universe as the character whose relationship it is (Universe division absolute — a
/// same-named entity in a different universe must never resolve as the target). Reports
/// ambiguous/unresolved rows rather than guessing.
/// </summary>
public static class BackfillCharacterRelationshipsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var unresolved = await (
            from cr in db.CharacterRelationships.IgnoreQueryFilters()
            where cr.TargetEntityId == null
            join owner in db.Entities.IgnoreQueryFilters() on cr.CharacterId equals owner.Id
            select new { cr.Id, cr.CharacterId, cr.TargetName, OwnerUniverseId = owner.UniverseId }
        ).ToListAsync();

        Console.WriteLine($"[backfill-character-relationships] {unresolved.Count} unresolved row(s) found.");
        if (unresolved.Count == 0) return 0;

        int resolved = 0, ambiguous = 0, notFound = 0;
        foreach (var row in unresolved)
        {
            if (string.IsNullOrWhiteSpace(row.TargetName)) { notFound++; continue; }

            var slug = WorldGraphService.Slugify(row.TargetName);
            var candidates = await db.Entities.AsNoTracking().IgnoreQueryFilters()
                .Where(e => e.UniverseId == row.OwnerUniverseId && e.IsActive
                    && (e.Name == row.TargetName || e.Slug == slug))
                .Select(e => e.Id)
                .ToListAsync();

            if (candidates.Count == 1)
            {
                resolved++;
                if (!dryRun)
                {
                    var target = candidates[0];
                    await db.CharacterRelationships
                        .Where(x => x.Id == row.Id)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.TargetEntityId, target));
                }
            }
            else if (candidates.Count > 1)
            {
                ambiguous++;
                Console.WriteLine($"  AMBIGUOUS: \"{row.TargetName}\" matches {candidates.Count} entities in the same universe — left unresolved");
            }
            else
            {
                notFound++;
            }
        }

        Console.WriteLine($"[backfill-character-relationships] resolved={resolved} ambiguous={ambiguous} not-found={notFound}" +
            (dryRun ? " (DRY RUN — no changes written)" : ""));
        return 0;
    }
}
