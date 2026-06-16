using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --rebuild-archetype-relational</c> — backfill the Archetypes relational
/// schema from Records.Json blobs. For every active archetype entity, deserializes
/// the blob → ArchetypeData → persists columns + all 5 bridge tables (WillAlways /
/// WillNever / Unless / SimilarTo / OppositeOf) + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Archetypes + bridge tables are already defined.
/// </summary>
public static class RebuildArchetypeRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-archetype-relational] Backfilling Archetypes relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await ArchetypeMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-archetype-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-archetype-relational] Wrote {count} archetype(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-archetype-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
