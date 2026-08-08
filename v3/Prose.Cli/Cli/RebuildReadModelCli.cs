using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --rebuild-readmodel [--archived]</c> — rebuild the materialized
/// character read-model projection (CharacterReadModels) from the relational
/// source of truth. This is the one-time slow path (the 25-Include fan-out over
/// every character) that lets every subsequent full read be a single column
/// read. Backfills missing / stale-version rows and prunes orphans.
///
/// Run after a bulk import, the JSON→relational migration, or a
/// <see cref="CharacterMapper.ReadModelVersion"/> bump. The steady-state read
/// path self-heals, so day-to-day you never need this.
/// </summary>
public static class RebuildReadModelCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        bool includeArchived = args.Contains("--archived");
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine($"[rebuild-readmodel] Rebuilding character read-models (v{CharacterMapper.ReadModelVersion}{(includeArchived ? ", incl. archived" : "")})…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await CharacterMapper.RebuildAllReadModelsAsync(db, includeArchived);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-readmodel] FAILED: {ex.Message}");
            Console.Error.WriteLine("[rebuild-readmodel] Did you run the migration? prose --migrate-sql (create_character_readmodel_20260606.sql)");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-readmodel] Wrote {count} read-models in {sw.Elapsed.TotalSeconds:0.#}s. Full reads now serve from the projection.");
        return 0;
    }
}
