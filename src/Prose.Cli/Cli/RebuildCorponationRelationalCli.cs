using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --rebuild-corponation-relational</c> — backfill the Corponations relational
/// schema from Records.Json blobs. For every corponation entity (active or inactive),
/// deserializes the blob → CorponationData → persists columns + CorponationCommonNames
/// bridge + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Corponations + bridge tables are already defined.
/// </summary>
public static class RebuildCorponationRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine("[rebuild-corponation-relational] Backfilling Corponations relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await CorponationMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-corponation-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-corponation-relational] Wrote {count} corponation entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-corponation-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
