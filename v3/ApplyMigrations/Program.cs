using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Extensions;

// ── ApplyMigrations ────────────────────────────────────────────────────────
// Direct runner that applies a list of .sql migration files by splitting on
// GO and submitting each batch separately. SqlSeedService strips GO and
// runs the whole script as one batch, which breaks any script that
// references a newly-added column on the next statement (SQL Server hasn't
// committed the schema change within the same batch).
//
// Run:
//   dotnet run --project v3/ApplyMigrations
//
// Idempotent — each .sql file's IF NOT EXISTS / COL_LENGTH guards skip
// already-applied changes.

var services = new ServiceCollection();
services.AddLogging(b =>
{
    b.SetMinimumLevel(LogLevel.Warning);
    b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; });
});
services.AddStreetSamuraiServices();
using var sp = services.BuildServiceProvider();

var dbFactory = sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();

// Resolve the Sql folder via the same logic as SqlSeedService: walk up
// from this assembly to find …/StreetSamurai.Core/Data/Sql/.
string ResolveSqlDir()
{
    var dir = AppContext.BaseDirectory;
    for (int up = 0; up < 8 && !string.IsNullOrEmpty(dir); up++)
    {
        var probe = Path.Combine(dir, "..", "StreetSamurai.Core", "Data", "Sql");
        if (Directory.Exists(probe)) return Path.GetFullPath(probe);
        var inside = Path.Combine(dir, "StreetSamurai.Core", "Data", "Sql");
        if (Directory.Exists(inside)) return Path.GetFullPath(inside);
        dir = Path.GetDirectoryName(dir) ?? "";
    }
    var cwd = Path.Combine(Directory.GetCurrentDirectory(), "v3", "StreetSamurai.Core", "Data", "Sql");
    if (Directory.Exists(cwd)) return cwd;
    throw new InvalidOperationException("Could not locate Data/Sql migration folder.");
}

var sqlDir = ResolveSqlDir();
Console.WriteLine($"SQL folder: {sqlDir}");

// The list of migrations to apply, in order. New entries append.
var migrations = new[]
{
    "add_beat_number_20260522.sql",
    "add_gaps_table_20260522.sql",
};

await using var db = await dbFactory.CreateDbContextAsync();

foreach (var file in migrations)
{
    var path = Path.Combine(sqlDir, file);
    if (!File.Exists(path))
    {
        Console.WriteLine($"  ✗ {file}: not found at {path}");
        continue;
    }
    var script = await File.ReadAllTextAsync(path);
    var batches = SplitOnGo(script);
    Console.WriteLine($"  → {file}  ({batches.Count} batch{(batches.Count == 1 ? "" : "es")})");
    int batchIdx = 0;
    foreach (var batch in batches)
    {
        batchIdx++;
        var trimmed = batch.Trim();
        if (trimmed.Length == 0) continue;
        try
        {
            await db.Database.ExecuteSqlRawAsync(trimmed);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    ✗ batch {batchIdx} failed: {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }
    Console.WriteLine($"    ✓ {file} applied");
}

// Echo verification counts.
var beatCount      = await db.Beats.CountAsync();
var beatsWithNum   = await db.Beats.CountAsync(b => b.Number > 0);
var gapTableExists = await db.Database.SqlQueryRaw<int>(
        "SELECT CASE WHEN OBJECT_ID('dbo.Gaps','U') IS NOT NULL THEN 1 ELSE 0 END AS Value")
    .SingleAsync();
Console.WriteLine();
Console.WriteLine($"Beats total          : {beatCount}");
Console.WriteLine($"Beats with Number > 0: {beatsWithNum}");
Console.WriteLine($"Gaps table exists    : {(gapTableExists == 1 ? "yes" : "no")}");
return 0;

// Split a T-SQL script into batches on lines that contain only "GO" (case
// insensitive, optionally followed by a comment). Preserves blank lines
// inside batches.
static List<string> SplitOnGo(string script)
{
    var batches = new List<string>();
    var current = new System.Text.StringBuilder();
    foreach (var raw in script.Split('\n'))
    {
        var line = raw.TrimEnd('\r');
        var trimmed = line.Trim();
        if (trimmed.Equals("GO", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("GO ", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("GO\t", StringComparison.OrdinalIgnoreCase))
        {
            batches.Add(current.ToString());
            current.Clear();
            continue;
        }
        current.AppendLine(line);
    }
    if (current.Length > 0) batches.Add(current.ToString());
    return batches;
}
