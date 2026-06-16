using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --rebuild-vocabulary-relational</c> — backfill the VocabularyEntries relational
/// schema from Records.Json blobs. For every active vocabulary entity, deserializes the
/// blob → VocabularyData → persists scalar columns + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — VocabularyEntries table is already fully defined.
/// </summary>
public static class RebuildVocabularyRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-vocabulary-relational] Backfilling VocabularyEntries relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await VocabularyMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-vocabulary-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-vocabulary-relational] Wrote {count} vocabulary entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-vocabulary-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
