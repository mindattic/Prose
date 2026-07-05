using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --rebuild-subsidiary-relational</c> — backfill the Subsidiaries relational
/// schema from Records.Json blobs. For every subsidiary entity (active or inactive),
/// deserializes the blob → SubsidiaryData → persists columns + SubsidiaryProducts
/// bridge + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Subsidiaries + SubsidiaryProducts are already defined.
/// </summary>
public static class RebuildSubsidiaryRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-subsidiary-relational] Backfilling Subsidiaries relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await SubsidiaryMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-subsidiary-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-subsidiary-relational] Wrote {count} subsidiary entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-subsidiary-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
