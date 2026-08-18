using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Cli;

/// <summary>
/// prose --backfill-short-name-alias [--universe glmz|scry|...] [--dry-run]
///
/// Root-cause fix for a corpus-wide entity-detection miss: prose overwhelmingly refers to
/// characters by first name ("Wes", "Idris"), but SceneContextAssembler.ScanNames only matches
/// an entity's full Name or an already-registered CharacterAlias — it has no built-in
/// first-name fallback (by design; a bare first name is ambiguous across characters, so making
/// it a real alias row lets the existing OriginNodeId disambiguation in
/// EntityDisambiguationService/GetNameIndexAsync resolve collisions the same way it already does
/// for any other alias, rather than special-casing "first token of Name" in the scan itself).
///
/// Found 2026-08-10 while investigating why --backfill-entity-presence's no-LLM roster backfill
/// had only a 0.5% yield (17/3331) corpus-wide: measured only 123/2238 (5.5%) multi-word-named
/// GLMZ+SCRY characters had their first name registered as a CharacterAlias. An earlier session's
/// "missing short-name alias" fix (project_missing_shortname_alias_pattern_2026_08_10) covered
/// only ~105 hand-picked "well-connected" characters — this generalizes it to every character.
///
/// Skips: single-word names (nothing to shorten), first tokens that are bare titles
/// (Mr/Mrs/Ms/Dr/etc. — too generic/ambiguous to serve as a usable alias), and any character that
/// already has a case-insensitive alias match for that first token.
/// </summary>
public static class BackfillShortNameAliasCli
{
    private static readonly HashSet<string> TitleStopWords = new(StringComparer.OrdinalIgnoreCase)
        { "Mr", "Mrs", "Ms", "Mx", "Dr", "Miss", "Sir", "Madam", "Lord", "Lady", "Captain", "Sergeant", "Officer" };

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        string? universeSlug = null;
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--universe" && i + 1 < args.Length) universeSlug = args[++i];

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        Guid? universeId = null;
        if (universeSlug != null)
        {
            universeId = await db.Set<Universe>().AsNoTracking()
                .Where(u => u.Slug == universeSlug).Select(u => (Guid?)u.Id).FirstOrDefaultAsync();
            if (universeId == null)
            {
                Console.Error.WriteLine($"[backfill-short-name-alias] Unknown universe slug '{universeSlug}'.");
                return 1;
            }
        }

        var charactersQuery = db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.EntityType == "character");
        if (universeId != null)
            charactersQuery = charactersQuery.Where(e => e.UniverseId == universeId);

        // CharacterAliases.CharacterId FKs to Characters.Id, not Entities.Id directly — a handful
        // of "character"-typed Entity rows have no matching Characters row (stub/orphaned
        // entities never materialized via create_character) and would violate that FK.
        var characterTableIds = new HashSet<Guid>(await db.Characters.AsNoTracking().Select(c => c.Id).ToListAsync());
        var characters = (await charactersQuery.Select(e => new { e.Id, e.Name }).ToListAsync())
            .Where(e => characterTableIds.Contains(e.Id))
            .ToList();

        var existingAliasesByChar = (await db.CharacterAliases.AsNoTracking()
                .Where(a => characters.Select(c => c.Id).Contains(a.CharacterId))
                .Select(a => new { a.CharacterId, a.Value })
                .ToListAsync())
            .GroupBy(a => a.CharacterId)
            .ToDictionary(g => g.Key, g => new HashSet<string>(g.Select(a => a.Value), StringComparer.OrdinalIgnoreCase));

        var maxPositionByChar = (await db.CharacterAliases.AsNoTracking()
                .Where(a => characters.Select(c => c.Id).Contains(a.CharacterId))
                .GroupBy(a => a.CharacterId)
                .Select(g => new { CharacterId = g.Key, MaxPos = g.Max(a => a.Position) })
                .ToListAsync())
            .ToDictionary(x => x.CharacterId, x => x.MaxPos);

        int candidates = 0, inserted = 0, skippedTitle = 0, skippedExisting = 0;
        foreach (var c in characters)
        {
            var firstSpace = c.Name.IndexOf(' ');
            if (firstSpace <= 0) continue; // single-word name — nothing to shorten
            var firstToken = c.Name[..firstSpace].Trim();
            if (firstToken.Length < 2) continue;
            if (TitleStopWords.Contains(firstToken)) { skippedTitle++; continue; }

            candidates++;
            var already = existingAliasesByChar.TryGetValue(c.Id, out var set) && set.Contains(firstToken);
            if (already) { skippedExisting++; continue; }

            inserted++;
            if (dryRun) continue;

            var nextPos = maxPositionByChar.TryGetValue(c.Id, out var mp) ? mp + 1 : 0;
            maxPositionByChar[c.Id] = nextPos;
            db.CharacterAliases.Add(new CharacterAlias { CharacterId = c.Id, Position = nextPos, Value = firstToken });
        }

        if (!dryRun && inserted > 0)
            await db.SaveChangesAsync();

        Console.WriteLine($"[backfill-short-name-alias] multi-word candidates={candidates}  " +
                           $"already-aliased={skippedExisting}  title-skipped={skippedTitle}  " +
                           $"{(dryRun ? "would insert" : "inserted")}={inserted}");
        if (dryRun) Console.WriteLine("(DRY RUN — no changes written)");
        return 0;
    }
}
