using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --rebuild-equipment-relational</c> — backfill the EquipmentItems relational
/// schema from Records.Json blobs. For every equipment entity (active or inactive),
/// deserializes the blob → EquipmentData → persists columns + all 5 bridge tables
/// (Aliases / BaseTechnologies / KnownUsers / Specifications / StoryHooks) + syncs
/// EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — EquipmentItems + bridge tables are already defined.
/// </summary>
public static class RebuildEquipmentRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine("[rebuild-equipment-relational] Backfilling EquipmentItems relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await EquipmentMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-equipment-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-equipment-relational] Wrote {count} equipment entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-equipment-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
