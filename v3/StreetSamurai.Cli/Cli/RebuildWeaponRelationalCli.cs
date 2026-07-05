using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --rebuild-weapon-relational</c> — backfill the Weapons relational
/// schema from Records.Json blobs. For every weapon entity (active or inactive),
/// deserializes the blob → WeaponryData → persists columns + all bridge rows +
/// syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// Bridges: WeaponAliases, WeaponBaseTechnologies, WeaponKnownUsers,
///          WeaponAmmunitionTypes, WeaponStoryHooks.
/// </summary>
public static class RebuildWeaponRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-weapon-relational] Backfilling Weapons relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await WeaponMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-weapon-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-weapon-relational] Wrote {count} weapon entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-weapon-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
