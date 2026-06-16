using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --rebuild-news-relational</c> — backfill the News relational schema from
/// Records.Json blobs. For every active news entity, deserializes the blob → NewsData
/// → persists scalar columns + bridge rows (EntitiesInvolved / Locations) + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — News, NewsEntitiesInvolved, NewsLocations tables are
/// already fully defined.
/// </summary>
public static class RebuildNewsRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-news-relational] Backfilling News relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await NewsMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-news-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-news-relational] Wrote {count} news item(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-news-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
