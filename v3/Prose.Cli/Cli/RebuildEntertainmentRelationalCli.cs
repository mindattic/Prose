using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --rebuild-entertainment-relational</c> — backfill the EntertainmentItems relational
/// schema from Records.Json blobs. For every entertainment entity (active or inactive),
/// deserializes the blob → EntertainmentData → persists columns + all bridge rows +
/// syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// Bridges: EntertainmentAliases, EntertainmentKnownFans, EntertainmentStoryHooks.
/// </summary>
public static class RebuildEntertainmentRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine("[rebuild-entertainment-relational] Backfilling EntertainmentItems relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await EntertainmentMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-entertainment-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-entertainment-relational] Wrote {count} entertainment entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-entertainment-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
