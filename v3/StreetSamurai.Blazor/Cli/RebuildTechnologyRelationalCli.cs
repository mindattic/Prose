using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --rebuild-technology-relational</c> — backfill the Technologies relational
/// schema from Records.Json blobs. For every technology entity (active or inactive),
/// deserializes the blob → TechnologyData → persists columns + all 5 bridge tables
/// (Aliases / Developers / BaseTechnologies / Enables / StoryHooks) + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Technologies + bridge tables are already defined.
/// </summary>
public static class RebuildTechnologyRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-technology-relational] Backfilling Technologies relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await TechnologyMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-technology-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-technology-relational] Wrote {count} technology entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-technology-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
