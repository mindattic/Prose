using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --rebuild-synthetic-relational</c> — backfill the SyntheticLives relational
/// schema from Records.Json blobs. For every synthetic entity (active or inactive),
/// deserializes the blob → SyntheticLifeData → persists columns + all bridge rows +
/// syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// Bridges: SyntheticLifeAliases, SyntheticLifeKnownAssociations, SyntheticLifeStoryHooks.
/// </summary>
public static class RebuildSyntheticRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-synthetic-relational] Backfilling SyntheticLives relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await SyntheticMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-synthetic-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-synthetic-relational] Wrote {count} synthetic entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-synthetic-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
