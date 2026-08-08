using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --rebuild-genemod-relational</c> — backfill the Genemods relational
/// schema from Records.Json blobs. For every active genemod entity, deserializes
/// the blob → GenemodData → persists columns + bridge tables (Aliases / StoryHooks)
/// + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Genemods + bridge tables are already defined.
/// </summary>
public static class RebuildGenemodRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine("[rebuild-genemod-relational] Backfilling Genemods relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await GenemodMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-genemod-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-genemod-relational] Wrote {count} genemod(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-genemod-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
