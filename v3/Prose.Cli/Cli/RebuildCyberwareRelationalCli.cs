using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>ss --rebuild-cyberware-relational</c> — backfill the CyberwareItems relational
/// schema from Records.Json blobs. For every cyberware entity (active or inactive),
/// deserializes the blob → CyberwareData → persists columns + all 4 bridge tables
/// (Aliases / SideEffects / KnownUsers / StoryHooks) + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — CyberwareItems + bridge tables are already defined.
/// </summary>
public static class RebuildCyberwareRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine("[rebuild-cyberware-relational] Backfilling CyberwareItems relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await CyberwareMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-cyberware-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-cyberware-relational] Wrote {count} cyberware entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-cyberware-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
