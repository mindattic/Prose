using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --check-temporal-hygiene [--json]
///
/// Enforces, rather than just documents, the two rules that make SQL Server system-versioned
/// temporal tables safe in this project (Phase -1 of the corpus-trust-recovery plan). The
/// original Beats/Nodes/BeatNodes incident was never "temporal tables are unsafe" — it was an
/// IsEnabled-style flag competing with row-existence to decide "what's current," combined with
/// query code that JOINed the _History shadow table back into live results to fill in disabled
/// rows. Both are now hard rules for every table in ProseDbContext.SystemVersionedTables:
///
///   1. No IsEnabled/IsActive/IsDeleted-style status-flag column on any versioned table.
///   2. No application query ever joins a live table to its own {Table}_History shadow.
///
/// Check 1 is a live schema query (sys.columns). Check 2 is a source-tree grep for lines that
/// mention both a "_History" table name and a JOIN — the exact shape of the original bug. Both
/// checks are advisory-with-teeth: this is meant to be run in CI / after any schema change
/// touching a versioned table, not just once.
/// </summary>
public static class TemporalHygieneCli
{
    // Case-insensitive column-name denylist. Anything shaped like an app-level soft-delete/
    // enabled flag is banned outright on a versioned table — presence in the live table is the
    // only signal of "current" that's allowed to exist.
    private static readonly string[] BannedColumnNames =
    {
        "isenabled", "isactive", "isdeleted", "issoftdeleted", "isremoved", "ishidden",
        "enabled", "active", "deleted", "softdeleted",
    };

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var json = args.Contains("--json");
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();

        var schemaFindings = await CheckSchemaAsync(dbFactory);
        var grepFindings = CheckSourceForHistoryJoins();

        var clean = schemaFindings.Count == 0 && grepFindings.Count == 0;

        if (json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                clean,
                schemaFindings,
                grepFindings,
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return clean ? 0 : 1;
        }

        Console.WriteLine("=== Temporal hygiene check ===");
        Console.WriteLine();
        Console.WriteLine($"[1] Status-flag columns on versioned tables ({ProseDbContext.SystemVersionedTables.Length} tables checked)");
        if (schemaFindings.Count == 0)
        {
            Console.WriteLine("  ✔ none found.");
        }
        else
        {
            foreach (var f in schemaFindings)
                Console.WriteLine($"  ✘ {f.Table}.{f.Column} — status-flag column on a versioned table. " +
                    "Row existence in the live table must be the only signal of \"current.\"");
        }

        Console.WriteLine();
        Console.WriteLine("[2] Application queries joining a live table to its own _History shadow");
        if (grepFindings.Count == 0)
        {
            Console.WriteLine("  ✔ none found.");
        }
        else
        {
            foreach (var f in grepFindings)
                Console.WriteLine($"  ✘ {f.File}:{f.Line} — {f.Text.Trim()}");
        }

        Console.WriteLine();
        Console.WriteLine(clean
            ? "RESULT: clean — the failure mode that broke Beats/Nodes/BeatNodes before cannot recur right now."
            : "RESULT: VIOLATIONS FOUND — fix before trusting any table's temporal history.");

        return clean ? 0 : 1;
    }

    public sealed record SchemaFinding(string Table, string Column);
    public sealed record GrepFinding(string File, int Line, string Text);

    private static async Task<List<SchemaFinding>> CheckSchemaAsync(IDbContextFactory<ProseDbContext> dbFactory)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (!db.Database.IsSqlServer()) return [];

        var tables = ProseDbContext.SystemVersionedTables;
        var findings = new List<SchemaFinding>();

        var rows = await db.Database.SqlQueryRaw<ColumnRow>("""
            SELECT t.name AS TableName, c.name AS ColumnName
            FROM sys.columns c
            JOIN sys.tables t ON c.object_id = t.object_id
            WHERE t.temporal_type <> 1
            """).ToListAsync();

        var tableSet = new HashSet<string>(tables, StringComparer.OrdinalIgnoreCase);
        var bannedSet = new HashSet<string>(BannedColumnNames, StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            if (!tableSet.Contains(row.TableName)) continue;
            if (bannedSet.Contains(row.ColumnName))
                findings.Add(new SchemaFinding(row.TableName, row.ColumnName));
        }

        return findings;
    }

    private sealed class ColumnRow
    {
        public string TableName { get; set; } = "";
        public string ColumnName { get; set; } = "";
    }

    // Directories that are either generated (Migrations snapshots), not source (bin/obj), or
    // the DDL-flip code itself (ProseDbContext.cs, MigrateSqlCli.cs) — those legitimately
    // reference "{table} SET (SYSTEM_VERSIONING = ...)" and "{table}_History" in the same
    // ALTER TABLE statement; that's schema management, not a live-data query joining the two.
    private static readonly string[] ExcludedPathSegments =
    {
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}",
    };

    private static readonly string[] ExcludedFiles = { "ProseDbContext.cs", "MigrateSqlCli.cs", "TemporalHygieneCli.cs" };

    private static List<GrepFinding> CheckSourceForHistoryJoins()
    {
        var repoRoot = KdpManifestService.FindRepoRoot();
        var v3Root = Path.Combine(repoRoot, "v3");
        if (!Directory.Exists(v3Root)) return [];

        var findings = new List<GrepFinding>();
        foreach (var file in Directory.EnumerateFiles(v3Root, "*.cs", SearchOption.AllDirectories))
        {
            if (ExcludedPathSegments.Any(seg => file.Contains(seg, StringComparison.OrdinalIgnoreCase))) continue;
            if (ExcludedFiles.Contains(Path.GetFileName(file))) continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("*") || trimmed.StartsWith("\\ ")) continue;

                if (line.Contains("_History", StringComparison.Ordinal) &&
                    line.Contains("join", StringComparison.OrdinalIgnoreCase))
                {
                    findings.Add(new GrepFinding(Path.GetRelativePath(repoRoot, file), i + 1, line));
                }
            }
        }
        return findings;
    }
}
