using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --rebuild-document-relational</c> — backfill the Documents relational
/// schema from Records.Json blobs. For every document entity (active or inactive),
/// deserializes the blob → WorldbuildingDocument → persists columns + DocumentHeadings
/// bridge + syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// Bridges: DocumentHeadings.
/// Tags route through the shared EntityTags table.
/// </summary>
public static class RebuildDocumentRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-document-relational] Backfilling Documents relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await DocumentMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-document-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-document-relational] Wrote {count} document entry(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-document-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
