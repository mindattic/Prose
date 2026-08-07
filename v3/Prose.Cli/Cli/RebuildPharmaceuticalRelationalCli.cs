using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>ss --rebuild-pharmaceutical-relational</c> — backfill the Pharmaceuticals relational
/// schema from Records.Json blobs. For every pharmaceutical entity (active or inactive),
/// deserializes the blob → PharmaceuticalData → persists columns + all 4 bridge tables
/// (Aliases / Effects / SideEffects / StoryHooks) + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Pharmaceuticals + bridge tables are already defined.
/// </summary>
public static class RebuildPharmaceuticalRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine("[rebuild-pharmaceutical-relational] Backfilling Pharmaceuticals relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await PharmaceuticalMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-pharmaceutical-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-pharmaceutical-relational] Wrote {count} pharmaceutical entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-pharmaceutical-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
