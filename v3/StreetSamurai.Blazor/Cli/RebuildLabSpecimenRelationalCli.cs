using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --rebuild-lab-specimen-relational</c> — backfill the LabSpecimens relational
/// schema from Records.Json blobs. For every active lab_specimen entity, deserializes
/// the blob → LabSpecimenData → persists columns + all 3 bridge tables (Aliases /
/// KnownLocations / StoryHooks) + syncs EntityTags. Also creates minimal relational
/// rows for any active lab_specimen entities that have no blob.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — LabSpecimens + bridge tables are already defined.
/// </summary>
public static class RebuildLabSpecimenRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-lab-specimen-relational] Backfilling LabSpecimens relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await LabSpecimenMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-lab-specimen-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-lab-specimen-relational] Wrote {count} lab specimen entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-lab-specimen-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
