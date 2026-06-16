using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --rebuild-flyover-entity-relational</c> — backfill the FlyoverEntities relational
/// schema from Records.Json blobs. For every active flyover_entity entity, deserializes
/// the blob → FlyoverEntityData → persists columns + all 3 bridge tables (Aliases /
/// KnownLocations / StoryHooks) + syncs EntityTags. Also creates minimal relational
/// rows for any active flyover_entity entities that have no blob.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — FlyoverEntities + bridge tables are already defined.
/// </summary>
public static class RebuildFlyoverEntityRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-flyover-entity-relational] Backfilling FlyoverEntities relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await FlyoverEntityMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-flyover-entity-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-flyover-entity-relational] Wrote {count} flyover entity entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-flyover-entity-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
