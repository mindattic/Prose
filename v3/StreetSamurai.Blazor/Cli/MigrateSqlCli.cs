using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// CLI entry for the SQL Server migration. Two phases (run --schema first):
///   ss --migrate-sql --schema       apply EF migrations
///   ss --migrate-sql --import all   import every supported entity type from JSON
///   ss --migrate-sql --import books also import books / chapters / beats
///   ss --migrate-sql --all          schema + every supported entity + books
///
/// Connection string: env <c>ConnectionStrings__StreetSamurai</c> →
/// appsettings <c>ConnectionStrings:StreetSamurai</c> → LocalDB.
/// </summary>
public static class MigrateSqlCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var schema = args.Contains("--schema") || args.Contains("--all");
        var importAll = args.Contains("--all") || (args.Contains("--import") && args.Contains("all"));
        var importBooks = args.Contains("--all") || (args.Contains("--import") && args.Contains("books"));
        var importPeople = !importAll && (args.Contains("--import") && args.Contains("people"));
        var importContinuity = args.Contains("--all") || (args.Contains("--import") && args.Contains("continuity"));
        var importArchives = args.Contains("--all") || (args.Contains("--import") && args.Contains("archives"));

        // Character relational migration (DataJson → columns + bridges).
        var charRelational  = args.Contains("--character-relational");
        var charDropLegacy  = args.Contains("--drop-legacy-json");
        var charNoBackfill  = args.Contains("--no-backfill");

        // Full rebuild: drop the entire database, recreate the full current schema,
        // re-import every entity type from the JSON source files.
        var rebuild = args.Contains("--rebuild");

        if (!schema && !importAll && !importBooks && !importPeople && !importContinuity && !importArchives
            && !charRelational && !charDropLegacy && !rebuild)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  ss --migrate-sql --schema                  apply EF migrations");
            Console.WriteLine("  ss --migrate-sql --import people           import character JSON");
            Console.WriteLine("  ss --migrate-sql --import all              import every supported entity type");
            Console.WriteLine("  ss --migrate-sql --import books            also import books / chapters / beats");
            Console.WriteLine("  ss --migrate-sql --all                     schema + import everything");
            Console.WriteLine();
            Console.WriteLine("  ss --migrate-sql --character-relational    add relational columns + bridges to Characters,");
            Console.WriteLine("                                             then backfill from Records.Json (--no-backfill skips Phase C)");
            Console.WriteLine("  ss --migrate-sql --drop-legacy-json        DROP the *Json columns from Characters (run AFTER verifying backfill)");
            Console.WriteLine();
            Console.WriteLine("  ss --migrate-sql --rebuild                 ⚠️  DROP entire database, recreate schema from current entity model,");
            Console.WriteLine("                                             then re-import everything (people / all / books / continuity / archives)");
            Console.WriteLine("                                             from JSON. Every Entity row gets Id (guid7) + Name + Slug.");
            return 0;
        }

        if (rebuild)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();
            var importer = scope.ServiceProvider.GetRequiredService<JsonImportService>();
            var books = scope.ServiceProvider.GetRequiredService<IBookRepository>();
            var chapters = scope.ServiceProvider.GetRequiredService<IChapterRepository>();

            Console.WriteLine();
            Console.WriteLine("[rebuild] dropping the StreetSamurai database…");

            // SYSTEM_VERSIONING blocks DROP TABLE; turn it off everywhere first.
            // EnsureDeletedAsync drops the entire database (history tables go with it).
            try
            {
                if (db.Database.IsSqlServer())
                {
                    foreach (var t in StreetSamuraiDbContext.SystemVersionedTables)
                    {
                        var sql = $"""
                            IF EXISTS (SELECT 1 FROM sys.tables t WHERE t.name = N'{t}' AND t.temporal_type = 2)
                            BEGIN
                                ALTER TABLE [dbo].[{t}] SET (SYSTEM_VERSIONING = OFF);
                            END;
                            """;
                        try { await db.Database.ExecuteSqlRawAsync(sql); }
                        catch (Exception ex) { Console.WriteLine($"  · warn: could not disable versioning on {t}: {ex.Message}"); }
                    }
                }
                await db.Database.EnsureDeletedAsync();
                Console.WriteLine("  ✔ database dropped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ drop failed: {ex.Message}");
                return 2;
            }

            Console.WriteLine("[rebuild] recreating schema from current entity model…");
            try
            {
                await db.Database.EnsureCreatedAsync();
                await db.EnableSystemVersioningAsync();
                Console.WriteLine("  ✔ schema created (every table from current EF model).");
                Console.WriteLine("  ✔ system versioning enabled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ recreate failed: {ex.Message}");
                return 3;
            }

            Console.WriteLine("[rebuild] importing every entity type from JSON…");
            var all = await importer.ImportAllAsync();
            int totalSrc = 0, totalImp = 0, totalErr = 0;
            var logRoot = Path.Combine(Path.GetTempPath(), "streetsamurai_import");
            Directory.CreateDirectory(logRoot);
            var errorPath = Path.Combine(logRoot, $"rebuild-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            using (var ew = new StreamWriter(errorPath))
            {
                foreach (var (kind, r) in all)
                {
                    Console.WriteLine($"  {kind,-16}  src={r.SourceCount,5}  imp={r.Imported,5}  err={r.Errors.Count,3}");
                    totalSrc += r.SourceCount; totalImp += r.Imported; totalErr += r.Errors.Count;
                    if (r.Errors.Count > 0)
                    {
                        ew.WriteLine($"=== {kind} ({r.Errors.Count} errors) ===");
                        foreach (var e in r.Errors) ew.WriteLine("  " + e);
                        ew.WriteLine();
                    }
                }
            }
            Console.WriteLine($"  {"TOTAL",-16}  src={totalSrc,5}  imp={totalImp,5}  err={totalErr,3}");
            if (totalErr > 0) Console.WriteLine($"  full error log → {errorPath}");

            Console.WriteLine("[rebuild] importing books / chapters / beats…");
            var rb = await importer.ImportBooksAndChaptersAsync(books, chapters);
            PrintImportResult(rb);

            Console.WriteLine("[rebuild] importing tone / story / literary / character bibles…");
            var rbb = await importer.ImportBiblesAsync();
            PrintImportResult(rbb);

            Console.WriteLine("[rebuild] importing continuity…");
            var rc = await importer.ImportContinuityFromSqliteAsync();
            if (rc.Skipped) Console.WriteLine("  (no continuity.db — skipped)");
            else
            {
                Console.WriteLine($"  claims          : {rc.Claims}");
                Console.WriteLine($"  contradictions  : {rc.Contradictions}");
                Console.WriteLine($"  confirmations   : {rc.Confirmations}");
            }

            Console.WriteLine("[rebuild] importing archives…");
            var ra = await importer.ImportArchivesAsync();
            PrintImportResult(ra);

            // Sanity check: every Entity row should have Id + Name + Slug.
            using var verify = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();
            var totalEntities = await verify.Entities.CountAsync();
            var missingName   = await verify.Entities.CountAsync(e => string.IsNullOrEmpty(e.Name));
            var missingSlug   = await verify.Entities.CountAsync(e => string.IsNullOrEmpty(e.Slug));
            Console.WriteLine();
            Console.WriteLine($"[rebuild] verification:");
            Console.WriteLine($"  entities total       : {totalEntities}");
            Console.WriteLine($"  missing Name         : {missingName}");
            Console.WriteLine($"  missing Slug         : {missingSlug}");
            if (missingName == 0 && missingSlug == 0)
                Console.WriteLine("  ✔ every entity has Id (guid7) + Name + Slug.");
            else
                Console.WriteLine("  ✘ some entities are missing Name or Slug — investigate before relying on this build.");

            return totalErr > 0 || missingName > 0 || missingSlug > 0 ? 1 : 0;
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

        if (importPeople)
        {
            using var scope = sp.CreateScope();
            var importer = scope.ServiceProvider.GetRequiredService<JsonImportService>();
            Console.WriteLine();
            Console.WriteLine("[import: characters]");
            var r = await importer.ImportCharactersAsync();
            PrintImportResult(r);
            failures += r.Errors.Count > 0 ? 1 : 0;
        }

        if (importAll)
        {
            using var scope = sp.CreateScope();
            var importer = scope.ServiceProvider.GetRequiredService<JsonImportService>();
            Console.WriteLine();
            Console.WriteLine("[import: every entity type]");
            var all = await importer.ImportAllAsync();
            int totalSrc = 0, totalImp = 0, totalErr = 0;
            // Dump every error to a per-run log file so they can be reviewed
            // without re-running the import. Streams to AppData so it survives
            // working-directory changes.
            var logRoot = Path.Combine(Path.GetTempPath(), "streetsamurai_import");
            Directory.CreateDirectory(logRoot);
            var errorPath = Path.Combine(logRoot, $"errors-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            using var ew = new StreamWriter(errorPath);
            foreach (var (kind, r) in all)
            {
                Console.WriteLine($"  {kind,-16}  src={r.SourceCount,5}  imp={r.Imported,5}  err={r.Errors.Count,3}");
                totalSrc += r.SourceCount; totalImp += r.Imported; totalErr += r.Errors.Count;
                if (r.Errors.Count > 0)
                {
                    ew.WriteLine($"=== {kind} ({r.Errors.Count} errors) ===");
                    foreach (var e in r.Errors) ew.WriteLine("  " + e);
                    ew.WriteLine();
                }
            }
            Console.WriteLine($"  {"TOTAL",-16}  src={totalSrc,5}  imp={totalImp,5}  err={totalErr,3}");
            if (totalErr > 0)
            {
                Console.WriteLine($"  full error log → {errorPath}");
            }
            failures += totalErr > 0 ? 1 : 0;
        }

        if (importBooks)
        {
            using var scope = sp.CreateScope();
            var importer = scope.ServiceProvider.GetRequiredService<JsonImportService>();
            var books = scope.ServiceProvider.GetRequiredService<IBookRepository>();
            var chapters = scope.ServiceProvider.GetRequiredService<IChapterRepository>();
            Console.WriteLine();
            Console.WriteLine("[import: books / chapters / beats]");
            var r = await importer.ImportBooksAndChaptersAsync(books, chapters);
            PrintImportResult(r);
            failures += r.Errors.Count > 0 ? 1 : 0;
        }

        if (importContinuity)
        {
            using var scope = sp.CreateScope();
            var importer = scope.ServiceProvider.GetRequiredService<JsonImportService>();
            Console.WriteLine();
            Console.WriteLine("[import: continuity SQLite → SQL Server]");
            var r = await importer.ImportContinuityFromSqliteAsync();
            if (r.Skipped) Console.WriteLine("  (no continuity.db found — skipped)");
            else
            {
                Console.WriteLine($"  claims          : {r.Claims}");
                Console.WriteLine($"  contradictions  : {r.Contradictions}");
                Console.WriteLine($"  confirmations   : {r.Confirmations}");
                Console.WriteLine($"  extraction runs : {r.Runs}");
            }
            failures += r.Errors.Count > 0 ? 1 : 0;
        }

        if (importArchives)
        {
            using var scope = sp.CreateScope();
            var importer = scope.ServiceProvider.GetRequiredService<JsonImportService>();
            Console.WriteLine();
            Console.WriteLine("[import: archives → IsActive=false]");
            var r = await importer.ImportArchivesAsync();
            PrintImportResult(r);
            failures += r.Errors.Count > 0 ? 1 : 0;
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
                    var logRoot = Path.Combine(Path.GetTempPath(), "streetsamurai_import");
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

    private static void PrintImportResult(JsonImportResult r)
    {
        Console.WriteLine($"  source count : {r.SourceCount}");
        Console.WriteLine($"  imported     : {r.Imported}");
        if (r.Errors.Count > 0)
        {
            Console.WriteLine($"  errors       : {r.Errors.Count}");
            foreach (var e in r.Errors.Take(10)) Console.WriteLine($"    - {e}");
        }
    }
}
