using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --rebuild-material-relational</c> — backfill the Materials relational
/// schema from Records.Json blobs. For every active material entity, deserializes
/// the blob → MaterialData → persists columns + bridge tables (Aliases / StoryHooks)
/// + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Materials + bridge tables are already defined.
/// </summary>
public static class RebuildMaterialRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine("[rebuild-material-relational] Backfilling Materials relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await MaterialMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-material-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-material-relational] Wrote {count} material(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-material-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
