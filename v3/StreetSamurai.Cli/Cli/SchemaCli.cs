using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// CLI surface for <see cref="SchemaRebuildService"/>. Two commands:
///
///   ss --schema snapshot --table NAME [--out path.sql]
///       Emit a complete reconstitution script for one table — DDL + indexes
///       + outgoing FKs + inbound FKs + every row as INSERT. Lands under
///       engine/data/schema-snapshots/{table}-{timestamp}.sql by default.
///
///   ss --schema rebuild --table NAME --order "col1,col2,col3,…"
///       Snapshot first, then drop and rebuild the table with the columns in
///       the requested order. Any unmentioned columns append in their original
///       relative order. Runs inside one transaction with row-count + per-column
///       checksum verification before commit. Snapshot remains as the
///       manual-recovery artifact.
/// </summary>
public static class SchemaCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var idx = Array.FindIndex(args, a => a == "--schema");
        if (idx < 0 || idx + 1 >= args.Length) { PrintUsage(); return 1; }

        var sub = args[idx + 1].ToLowerInvariant();
        var table = ArgValue(args, "--table");
        if (string.IsNullOrEmpty(table)) { Console.Error.WriteLine("--table required"); return 1; }

        var svc = sp.GetRequiredService<SchemaRebuildService>();

        try
        {
            switch (sub)
            {
                case "snapshot":
                {
                    var outPath = ArgValue(args, "--out");
                    var path = await svc.SnapshotTableAsync(table, outPath);
                    Console.WriteLine($"[schema snapshot] {table} → {path}");
                    return 0;
                }

                case "rebuild":
                {
                    var order = ArgValue(args, "--order");
                    if (string.IsNullOrEmpty(order))
                    {
                        Console.Error.WriteLine("--order \"col1,col2,…\" required");
                        return 1;
                    }
                    var cols = order
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        .ToList();
                    Console.WriteLine($"[schema rebuild] {table}  ({cols.Count} columns specified, rest appended)");
                    var progress = new Progress<string>(Console.WriteLine);
                    var rs = await svc.RebuildTableAsync(table, cols, progress);
                    Console.WriteLine();
                    Console.WriteLine($"  snapshot         : {rs.SnapshotPath}");
                    Console.WriteLine($"  rows copied      : {rs.RowsCopied}");
                    Console.WriteLine($"  indexes recreated: {rs.IndexesRecreated}");
                    Console.WriteLine($"  inbound FKs      : {rs.InboundFksRecreated}");
                    Console.WriteLine($"  outbound FKs     : {rs.OutboundFksRecreated}");
                    Console.WriteLine($"  row count match  : {rs.RowCountVerified}");
                    Console.WriteLine($"  checksum match   : {rs.ChecksumsVerified}");
                    Console.WriteLine($"  duration         : {rs.Duration.TotalSeconds:F1}s");
                    foreach (var w in rs.Warnings) Console.WriteLine($"  ⚠  {w}");
                    return 0;
                }

                default:
                    PrintUsage();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[schema] FAILED: {ex.Message}");
            Console.Error.WriteLine("Snapshot remains on disk for manual recovery (see path above).");
            return 2;
        }
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  ss --schema snapshot --table NAME [--out path.sql]");
        Console.WriteLine("  ss --schema rebuild  --table NAME --order \"col1,col2,col3,…\"");
    }
}
