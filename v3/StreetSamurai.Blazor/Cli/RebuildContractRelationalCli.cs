using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --rebuild-contract-relational</c> — backfill the Contracts relational schema
/// from Records.Json blobs. For every active contract entity, deserializes the blob →
/// ContractData → persists scalar columns (including CrewCapabilities flattened to 10
/// Capability* columns) + bridge rows (ContractBonuses / ContractComplications) +
/// syncs EntityTags.
///
/// ADDITIVE: Records.Json is never modified or deleted.
/// No new migration required — Contracts, ContractBonuses, ContractComplications tables
/// are already fully defined.
/// </summary>
public static class RebuildContractRelationalCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

        Console.WriteLine("[rebuild-contract-relational] Backfilling Contracts relational schema from Records.Json blobs…");
        var sw = Stopwatch.StartNew();

        await using var db = await dbFactory.CreateDbContextAsync();
        int count;
        try
        {
            count = await ContractMapper.RebuildAllAsync(db);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[rebuild-contract-relational] FAILED: {ex.Message}");
            return 1;
        }

        sw.Stop();
        Console.WriteLine($"[rebuild-contract-relational] Wrote {count} contract(s) in {sw.Elapsed.TotalSeconds:0.#}s.");
        Console.WriteLine("[rebuild-contract-relational] Records.Json blobs are untouched; retire them after verifying parity.");
        return 0;
    }
}
