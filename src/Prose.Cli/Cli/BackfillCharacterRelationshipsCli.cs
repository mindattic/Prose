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
/// Matches ResolveEntityIdAny's own logic (exact Name, then Slug, then a registered alias —
/// EntityResolver.cs, shared with the live mapper path) but restricted to the SAME universe as
/// the character whose relationship it is (Universe division absolute — a same-named entity in a
/// different universe must never resolve as the target). Reports ambiguous/unresolved rows rather
/// than guessing.
///
/// Write-gate scope (2026-08-22 triage): accepted exception, same class as the
/// <c>Rebuild*RelationalCli</c> exemptions — a raw <c>ExecuteUpdateAsync</c>, but deterministic
/// re-derivation from already-seeded source-of-truth rows (Name/Slug/alias match, universe-scoped,
/// ambiguous cases reported and left untouched rather than guessed) that cannot produce a
/// narratively-wrong result. The plan's own audit noted no independent raw-write path existed for
/// <c>CharacterRelationships</c> at the time — this file is that path, found during Phase-3
/// triage; documenting it now so it isn't rediscovered as a gap.
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

        int resolved = 0, resolvedByAlias = 0, ambiguous = 0, notFound = 0;
        foreach (var row in unresolved)
        {
            if (string.IsNullOrWhiteSpace(row.TargetName)) { notFound++; continue; }

            var slug = UniverseGraphService.Slugify(row.TargetName);
            var candidates = await db.Entities.AsNoTracking().IgnoreQueryFilters()
                .Where(e => e.UniverseId == row.OwnerUniverseId && (e.Name == row.TargetName || e.Slug == slug))
                .Select(e => e.Id)
                .ToListAsync();

            var viaAlias = false;
            if (candidates.Count == 0)
            {
                candidates = await ResolveByAliasInUniverseAsync(db, row.TargetName, row.OwnerUniverseId);
                viaAlias = candidates.Count > 0;
            }

            if (candidates.Count == 1)
            {
                resolved++;
                if (viaAlias) resolvedByAlias++;
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

        Console.WriteLine($"[backfill-character-relationships] resolved={resolved} (of which {resolvedByAlias} via alias) ambiguous={ambiguous} not-found={notFound}" +
            (dryRun ? " (DRY RUN — no changes written)" : ""));
        return 0;
    }

    /// <summary>
    /// Alias fallback (Character/Place/Faction/Weapon), restricted to the owner's universe —
    /// the corpus-wide-scan counterpart of EntityResolver.ResolveEntityIdAny's ambient-scoped
    /// alias check, since this CLI IgnoreQueryFilters() to process every universe in one pass.
    /// </summary>
    private static async Task<List<Guid>> ResolveByAliasInUniverseAsync(ProseDbContext db, string name, Guid universeId)
    {
        var charHit = await db.Characters.AsNoTracking().IgnoreQueryFilters()
            .Where(c => c.Aliases.Any(a => a.Value == name))
            .Join(db.Entities.AsNoTracking().IgnoreQueryFilters().Where(e => e.UniverseId == universeId),
                c => c.Id, e => e.Id, (c, e) => e.Id)
            .ToListAsync();
        if (charHit.Count > 0) return charHit;

        var placeHit = await db.Places.AsNoTracking().IgnoreQueryFilters()
            .Where(p => p.Aliases.Any(a => a.Value == name))
            .Join(db.Entities.AsNoTracking().IgnoreQueryFilters().Where(e => e.UniverseId == universeId),
                p => p.Id, e => e.Id, (p, e) => e.Id)
            .ToListAsync();
        if (placeHit.Count > 0) return placeHit;

        var factionHit = await db.Factions.AsNoTracking().IgnoreQueryFilters()
            .Where(f => f.Aliases.Any(a => a.Value == name))
            .Join(db.Entities.AsNoTracking().IgnoreQueryFilters().Where(e => e.UniverseId == universeId),
                f => f.Id, e => e.Id, (f, e) => e.Id)
            .ToListAsync();
        if (factionHit.Count > 0) return factionHit;

        return await db.Weapons.AsNoTracking().IgnoreQueryFilters()
            .Where(w => w.Aliases.Any(a => a.Value == name))
            .Join(db.Entities.AsNoTracking().IgnoreQueryFilters().Where(e => e.UniverseId == universeId),
                w => w.Id, e => e.Id, (w, e) => e.Id)
            .ToListAsync();
    }
}
