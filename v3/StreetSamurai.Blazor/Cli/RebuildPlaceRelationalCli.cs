using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --rebuild-place-relational</c> — backfill the Places relational
/// schema from Records.Json blobs. For every place entity (active or inactive),
/// deserializes the blob → DistrictData → persists columns + all 10 bridge tables +
/// syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// Bridges: PlaceAliases, PlaceDangers, PlaceOpportunities, PlaceStoryHooks,
///          PlaceAtmosphereItems, PlaceAdjacencies, PlaceExits, PlaceFrequentedBy,
///          PlaceNotableLocations, PlaceRelatedEntities.
/// </summary>
public static class RebuildPlaceRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-place-relational] Backfilling Places relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await PlaceMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-place-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-place-relational] Wrote {count} place entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-place-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
