using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --rebuild-consumer-good-relational</c> — backfill the ConsumerGoods relational
/// schema from Records.Json blobs. For every consumer_good entity (active or inactive),
/// deserializes the blob → ConsumerGoodData → persists columns + StoryHooks bridge +
/// syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// Schema migration required: relationalize_consumer_goods_20260616.sql adds 8 missing
/// columns (BrandName, ProductName, Subcategory, FlavorProfile, Price, PopularityRank,
/// Slogan, CulturalContext).
/// </summary>
public static class RebuildConsumerGoodRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine("[rebuild-consumer-good-relational] Backfilling ConsumerGoods relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await ConsumerGoodMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-consumer-good-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-consumer-good-relational] Wrote {count} consumer good entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-consumer-good-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
