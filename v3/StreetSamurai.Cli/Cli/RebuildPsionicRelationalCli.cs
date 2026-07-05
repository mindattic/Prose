using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --rebuild-psionic-relational</c> — backfill the Psionics relational
/// schema from Records.Json blobs. For every active psionic entity, deserializes
/// the blob → PsionicData → persists columns + all 3 bridge tables (Aliases /
/// KnownPractitioners / StoryHooks) + syncs EntityTags. Also creates minimal
/// relational rows for any active psionic entities that have no blob.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Psionics + bridge tables are already defined.
/// </summary>
public static class RebuildPsionicRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-psionic-relational] Backfilling Psionics relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await PsionicMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-psionic-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-psionic-relational] Wrote {count} psionic entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-psionic-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
