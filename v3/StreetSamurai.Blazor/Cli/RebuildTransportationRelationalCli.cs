using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --rebuild-transportation-relational</c> — backfill the Transportations
/// relational schema from Records.Json blobs. For every active transportation entity,
/// deserializes the blob → TransportationData → persists columns + bridge tables
/// (Aliases / StoryHooks) + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Transportations + bridge tables are already defined.
/// </summary>
public static class RebuildTransportationRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-transportation-relational] Backfilling Transportations relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await TransportationMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-transportation-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-transportation-relational] Wrote {count} transportation(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-transportation-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
