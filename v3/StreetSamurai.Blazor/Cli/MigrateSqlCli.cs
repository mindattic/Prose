using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// CLI entry for SQL Server schema operations. JSON imports retired — every
/// canon entity now lives in SQL with its full <c>Records.Json</c> blob, and
/// the legacy file-based importer (JsonImportService) was removed alongside
/// the JSON archival sweep. What remains here is schema management and
/// in-place column migrations.
///
///   ss --migrate-sql --schema                  apply EF migrations + enable SYSTEM_VERSIONING
///   ss --migrate-sql --character-relational    add relational columns + bridges to Characters
///                                              and backfill from Records.Json (--no-backfill skips Phase C)
///   ss --migrate-sql --drop-legacy-json        DROP the *Json columns from Characters
///                                              (run AFTER verifying the column-based read path)
///
/// Connection string: env <c>ConnectionStrings__StreetSamurai</c> →
/// appsettings <c>ConnectionStrings:StreetSamurai</c> → LocalDB.
/// </summary>
public static class MigrateSqlCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var schema = args.Contains("--schema");

        // Character relational migration (Records.Json → typed columns + bridges).
        var charRelational  = args.Contains("--character-relational");
        var charDropLegacy  = args.Contains("--drop-legacy-json");
        var charNoBackfill  = args.Contains("--no-backfill");

        if (!schema && !charRelational && !charDropLegacy)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  ss --migrate-sql --schema                  apply EF migrations + enable SYSTEM_VERSIONING");
            Console.WriteLine();
            Console.WriteLine("  ss --migrate-sql --character-relational    add relational columns + bridges to Characters,");
            Console.WriteLine("                                             then backfill from Records.Json (--no-backfill skips Phase C)");
            Console.WriteLine("  ss --migrate-sql --drop-legacy-json        DROP the *Json columns from Characters (run AFTER verifying backfill)");
            return 0;
        }

        Console.WriteLine("=== StreetSamurai SQL migration ===");
        var failures = 0;

        if (schema)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();
            Console.WriteLine();
            Console.WriteLine("[schema]");
            try
            {
                if (db.Database.GetMigrations().Any())
                {
                    await db.Database.MigrateAsync();
                    Console.WriteLine("  ✔ migrations applied.");
                }
                else
                {
                    var created = await db.Database.EnsureCreatedAsync();
                    Console.WriteLine(created
                        ? "  ✔ schema created (EnsureCreated)."
                        : "  ✔ schema already exists.");
                }

                // Enable SYSTEM_VERSIONING on every table in the temporal set.
                // Idempotent — skips tables that are already temporal. No-op on SQLite.
                Console.WriteLine("  · enabling system versioning…");
                await db.EnableSystemVersioningAsync();
                Console.WriteLine("  ✔ temporal: system versioning is on.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ schema failed: {ex.Message}");
                return 2;
            }
        }

        if (charRelational)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();
            var migration = new CharacterRelationalMigration(db);

            Console.WriteLine();
            Console.WriteLine("[character relational migration — Phase A: add columns]");
            var rA = await migration.ApplyPhaseAAsync();
            foreach (var s in rA.SchemaActions) Console.WriteLine($"  {s}");
            foreach (var e in rA.Errors) Console.WriteLine($"  ✘ {e}");
            failures += rA.Errors.Count > 0 ? 1 : 0;

            Console.WriteLine();
            Console.WriteLine("[character relational migration — Phase B: create bridge tables]");
            var rB = await migration.ApplyPhaseBAsync();
            foreach (var s in rB.SchemaActions) Console.WriteLine($"  {s}");
            foreach (var e in rB.Errors) Console.WriteLine($"  ✘ {e}");
            failures += rB.Errors.Count > 0 ? 1 : 0;

            if (!charNoBackfill)
            {
                Console.WriteLine();
                Console.WriteLine("[character relational migration — Phase C: backfill from Records.Json]");
                var progress = new Progress<int>(n => Console.Write($"\r  · processed {n}…   "));
                var rC = await migration.ApplyPhaseCAsync(progress: progress);
                Console.WriteLine();
                Console.WriteLine($"  backfilled : {rC.CharactersBackfilled}");
                Console.WriteLine($"  failed     : {rC.CharactersFailed}");
                if (rC.Errors.Count > 0)
                {
                    var logRoot = Path.Combine(Path.GetTempPath(), "streetsamurai_migrate");
                    Directory.CreateDirectory(logRoot);
                    var errorPath = Path.Combine(logRoot, $"char-relational-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
                    File.WriteAllLines(errorPath, rC.Errors);
                    Console.WriteLine($"  full error log → {errorPath}");
                }
                failures += rC.Errors.Count > 0 ? 1 : 0;
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("[character relational migration — Phase C SKIPPED (--no-backfill)]");
            }
        }

        if (charDropLegacy)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();
            var migration = new CharacterRelationalMigration(db);

            Console.WriteLine();
            Console.WriteLine("[character relational migration — Phase D: drop legacy *Json columns]");
            Console.WriteLine("  (verify the column-based read path before running this — drops are irreversible)");
            var rD = await migration.ApplyPhaseDAsync();
            foreach (var s in rD.SchemaActions) Console.WriteLine($"  {s}");
            foreach (var e in rD.Errors) Console.WriteLine($"  ✘ {e}");
            failures += rD.Errors.Count > 0 ? 1 : 0;
        }

        return failures > 0 ? 1 : 0;
    }
}
