using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --fix-self-aliases [--dry-run]</c> — finds and removes every <c>CharacterAlias</c>/
/// <c>PlaceAlias</c>/<c>FactionAlias</c>/<c>WeaponAlias</c> row whose <c>Value</c> matches
/// (case-insensitively) its own owning entity's canonical <c>Name</c> — a redundant, meaningless
/// self-alias. Built 2026-08-22 as the corpus-wide repair companion to the root-cause fix in
/// <see cref="DuplicateEntityScanService.MergeAsync"/> (which now prevents NEW self-aliases from
/// a merge, but doesn't retroactively clean up ones a merge already created before that fix
/// shipped — confirmed live: two real ones, Femi and Mrs. Chen, both from the same day's BCODA
/// entity merges). See <c>WorldValidationTests.NoSelfAliases</c> for the detection query this
/// mirrors. All four alias tables are system-versioned (temporal) — a delete here is recoverable
/// via SQL Server's own history, same safety net as every other entity write in this codebase.
/// </summary>
public static class FixSelfAliasesCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        bool dryRun = args.Contains("--dry-run");
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var characterHits = await (from a in db.CharacterAliases
                                    join c in db.Characters on a.CharacterId equals c.Id
                                    where a.Value.ToLower() == c.Name.ToLower()
                                    select new { Table = "CharacterAliases", a.Id, c.Name, Value = a.Value })
            .ToListAsync();

        var placeHits = await (from a in db.PlaceAliases
                                join e in db.Entities on a.PlaceId equals e.Id
                                where a.Value.ToLower() == e.Name.ToLower()
                                select new { Table = "PlaceAliases", a.Id, e.Name, Value = a.Value })
            .ToListAsync();

        var factionHits = await (from a in db.FactionAliases
                                  join e in db.Entities on a.FactionId equals e.Id
                                  where a.Value.ToLower() == e.Name.ToLower()
                                  select new { Table = "FactionAliases", a.Id, e.Name, Value = a.Value })
            .ToListAsync();

        var weaponHits = await (from a in db.WeaponAliases
                                 join e in db.Entities on a.WeaponId equals e.Id
                                 where a.Value.ToLower() == e.Name.ToLower()
                                 select new { Table = "WeaponAliases", a.Id, e.Name, Value = a.Value })
            .ToListAsync();

        var all = characterHits.Select(h => (h.Table, h.Id, h.Name, h.Value))
            .Concat(placeHits.Select(h => (h.Table, h.Id, h.Name, h.Value)))
            .Concat(factionHits.Select(h => (h.Table, h.Id, h.Name, h.Value)))
            .Concat(weaponHits.Select(h => (h.Table, h.Id, h.Name, h.Value)))
            .ToList();

        if (all.Count == 0)
        {
            Console.WriteLine("[fix-self-aliases] No self-alias violations found.");
            return 0;
        }

        foreach (var (table, id, name, value) in all)
            Console.WriteLine($"  {table} row {id}: \"{value}\" on \"{name}\"{(dryRun ? " (dry-run, not removed)" : " — removed")}");

        if (dryRun)
        {
            Console.WriteLine($"[fix-self-aliases] {all.Count} violation(s) found, dry-run — nothing changed.");
            return 1;
        }

        var charIds = characterHits.Select(h => h.Id).ToList();
        var placeIds = placeHits.Select(h => h.Id).ToList();
        var factionIds = factionHits.Select(h => h.Id).ToList();
        var weaponIds = weaponHits.Select(h => h.Id).ToList();

        await db.CharacterAliases.Where(a => charIds.Contains(a.Id)).ExecuteDeleteAsync();
        await db.PlaceAliases.Where(a => placeIds.Contains(a.Id)).ExecuteDeleteAsync();
        await db.FactionAliases.Where(a => factionIds.Contains(a.Id)).ExecuteDeleteAsync();
        await db.WeaponAliases.Where(a => weaponIds.Contains(a.Id)).ExecuteDeleteAsync();

        Console.WriteLine($"[fix-self-aliases] Removed {all.Count} self-alias violation(s).");
        return 0;
    }
}
