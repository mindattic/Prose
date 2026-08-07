using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>ss --rebuild-faction-relational</c> — backfill the Factions relational
/// schema from Records.Json blobs. For every active faction entity, deserializes
/// the blob → FactionData → persists columns + all 8 bridge tables (including
/// the new FactionRelationshipTags) + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// Run once after applying add_faction_relationship_tags_20260615.sql.
/// </summary>
public static class RebuildFactionRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine("[rebuild-faction-relational] Backfilling Factions relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await FactionMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-faction-relational] FAILED: {ex.Message}");
            Console.Error.WriteLine("[rebuild-faction-relational] Did you run the migration?  ss --migrate-sql  (add_faction_relationship_tags_20260615.sql)");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-faction-relational] Wrote {count} faction(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-faction-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
