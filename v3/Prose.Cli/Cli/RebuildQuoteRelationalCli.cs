using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>ss --rebuild-quote-relational</c> — backfill the Quotes relational schema from
/// Records.Json blobs. For every active quote entity, deserializes the blob → QuoteData
/// → persists scalar columns + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Quotes table is already fully defined.
/// </summary>
public static class RebuildQuoteRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine("[rebuild-quote-relational] Backfilling Quotes relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await QuoteMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-quote-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-quote-relational] Wrote {count} quote(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-quote-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
