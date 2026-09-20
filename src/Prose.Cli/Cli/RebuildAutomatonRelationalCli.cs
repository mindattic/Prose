using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Cli;

/// <summary>
/// <c>prose --rebuild-automaton-relational</c> — backfill the Automata relational
/// schema from Records.Json blobs. For every active automaton entity, deserializes
/// the blob → AutomatonData → persists columns + all 5 bridge tables (Aliases /
/// Armament / Sensors / KnownDeployments / StoryHooks) + syncs EntityTags.
/// Also creates minimal relational rows for any active automaton entities that have no blob.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Automata + bridge tables are already defined.
/// </summary>
public static class RebuildAutomatonRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        Console.WriteLine("[rebuild-automaton-relational] Backfilling Automata relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await AutomatonMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-automaton-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-automaton-relational] Wrote {count} automaton entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-automaton-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
