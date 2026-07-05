using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --rebuild-apparel-relational</c> — backfill the Apparels relational
/// schema from Records.Json blobs. For every apparel entity (active or inactive),
/// deserializes the blob → ApparelData → persists columns + all bridge rows +
/// syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// Schema migration required: relationalize_apparel_20260616.sql adds 5 missing scalar
/// columns (Functionality, WhatItSays, PriceRange, AugCompatible, GeneCompatible) and
/// creates 2 new bridge tables (ApparelMaterials, ApparelWornBy).
/// </summary>
public static class RebuildApparelRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-apparel-relational] Backfilling Apparels relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await ApparelMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-apparel-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-apparel-relational] Wrote {count} apparel entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-apparel-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
