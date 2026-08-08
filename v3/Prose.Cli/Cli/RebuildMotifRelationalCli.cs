using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --rebuild-motif-relational</c> — backfill the Motifs relational
/// schema from Records.Json blobs. For every active motif entity, deserializes
/// the blob → MotifData → persists columns + MotifAppearances bridge.
/// Also creates minimal relational rows for any active motif entities that have no blob.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Motifs + MotifAppearances are already defined.
/// </summary>
public static class RebuildMotifRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine("[rebuild-motif-relational] Backfilling Motifs relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await MotifMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-motif-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-motif-relational] Wrote {count} motif entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-motif-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
