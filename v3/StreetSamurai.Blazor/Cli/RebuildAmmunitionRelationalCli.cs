using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --rebuild-ammunition-relational</c> — backfill the Ammunitions relational
/// schema from Records.Json blobs. For every active ammunition entity, deserializes
/// the blob → AmmunitionData → persists columns + all 4 bridge tables (Aliases /
/// CompatibleWeapons / Variants / StoryHooks) + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Ammunitions + bridge tables are already defined.
/// </summary>
public static class RebuildAmmunitionRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-ammunition-relational] Backfilling Ammunitions relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await AmmunitionMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-ammunition-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-ammunition-relational] Wrote {count} ammunition entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-ammunition-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
