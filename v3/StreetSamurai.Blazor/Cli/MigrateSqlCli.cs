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

        // Beat soft-delete: add IsEnabled to StrandBeats (and its history table).
        var strandBeatSoftDelete = args.Contains("--strand-beat-soft-delete");

        // Strand + Beat version counter: add Version INT to Beats, Strands (and history tables).
        var strandBeatVersion = args.Contains("--strand-beat-version");

        // Entity grammar notes: add GrammarNote NVARCHAR(MAX) to Entities (and history table).
        var entityGrammarNote = args.Contains("--entity-grammar-note");

        // Strand short reference code: add StrandCode NVARCHAR(20) to Strands (+ history) with a
        // unique filtered index (enforced for non-null values only).
        var strandCode = args.Contains("--strand-code");

        if (!schema && !charRelational && !charDropLegacy && !strandBeatSoftDelete && !strandBeatVersion && !entityGrammarNote && !strandCode)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  ss --migrate-sql --schema                    apply EF migrations + enable SYSTEM_VERSIONING");
            Console.WriteLine("  ss --migrate-sql --strand-beat-soft-delete   add IsEnabled column to StrandBeats/StrandBeats_History");
            Console.WriteLine("  ss --migrate-sql --strand-beat-version       add Version INT counter to Beats+Strands (and history tables)");
            Console.WriteLine("  ss --migrate-sql --entity-grammar-note       add GrammarNote column to Entities (and history table)");
            Console.WriteLine("  ss --migrate-sql --strand-code               add StrandCode NVARCHAR(20) to Strands (unique per non-null value)");
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

        if (strandBeatSoftDelete)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();

            Console.WriteLine();
            Console.WriteLine("[strand-beat-soft-delete]");
            try
            {
                // StrandBeats and StrandBeats_History are temporal tables.
                // To add a column we must briefly disable system versioning,
                // alter both tables, then re-enable it.
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID('StrandBeats') AND name = 'IsEnabled')
                    BEGIN
                        ALTER TABLE [dbo].[StrandBeats] SET (SYSTEM_VERSIONING = OFF);
                        ALTER TABLE [dbo].[StrandBeats]         ADD [IsEnabled] bit NOT NULL DEFAULT 1;
                        ALTER TABLE [dbo].[StrandBeats_History] ADD [IsEnabled] bit NOT NULL DEFAULT 1;
                        ALTER TABLE [dbo].[StrandBeats]
                            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[StrandBeats_History],
                                                         DATA_CONSISTENCY_CHECK = OFF));
                    END;
                    """);
                Console.WriteLine("  ✔ IsEnabled column added to StrandBeats (+ history table).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ failed: {ex.Message}");
                failures++;
            }
        }

        if (strandBeatVersion)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();

            Console.WriteLine();
            Console.WriteLine("[strand-beat-version]");
            foreach (var (table, hist) in new[] { ("Beats", "Beats_History"), ("Strands", "Strands_History") })
            {
                try
                {
                    await db.Database.ExecuteSqlRawAsync($"""
                        IF NOT EXISTS (SELECT 1 FROM sys.columns
                                       WHERE object_id = OBJECT_ID('{table}') AND name = 'Version')
                        BEGIN
                            ALTER TABLE [dbo].[{table}] SET (SYSTEM_VERSIONING = OFF);
                            ALTER TABLE [dbo].[{table}]         ADD [Version] INT NOT NULL DEFAULT 0;
                            ALTER TABLE [dbo].[{hist}] ADD [Version] INT NOT NULL DEFAULT 0;
                            ALTER TABLE [dbo].[{table}]
                                SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[{hist}],
                                                             DATA_CONSISTENCY_CHECK = OFF));
                        END;
                        """);
                    Console.WriteLine($"  ✔ Version column added to {table} (+ {hist}).");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✘ {table} failed: {ex.Message}");
                    failures++;
                }
            }
        }

        if (entityGrammarNote)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();

            Console.WriteLine();
            Console.WriteLine("[entity-grammar-note]");
            try
            {
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID('Entities') AND name = 'GrammarNote')
                    BEGIN
                        ALTER TABLE [dbo].[Entities] SET (SYSTEM_VERSIONING = OFF);
                        ALTER TABLE [dbo].[Entities]         ADD [GrammarNote] NVARCHAR(MAX) NULL;
                        ALTER TABLE [dbo].[Entities_History] ADD [GrammarNote] NVARCHAR(MAX) NULL;
                        ALTER TABLE [dbo].[Entities]
                            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Entities_History],
                                                         DATA_CONSISTENCY_CHECK = OFF));
                    END;
                    """);
                Console.WriteLine("  ✔ GrammarNote column added to Entities (+ Entities_History).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ Entities failed: {ex.Message}");
                failures++;
            }
        }

        if (strandCode)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();

            Console.WriteLine();
            Console.WriteLine("[strand-code]");
            try
            {
                // Add the column to the temporal table + its history shadow.
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID('Strands') AND name = 'StrandCode')
                    BEGIN
                        ALTER TABLE [dbo].[Strands] SET (SYSTEM_VERSIONING = OFF);
                        ALTER TABLE [dbo].[Strands]         ADD [StrandCode] NVARCHAR(20) NULL;
                        ALTER TABLE [dbo].[Strands_History] ADD [StrandCode] NVARCHAR(20) NULL;
                        ALTER TABLE [dbo].[Strands]
                            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Strands_History],
                                                         DATA_CONSISTENCY_CHECK = OFF));
                    END;
                    """);
                Console.WriteLine("  ✔ StrandCode column added to Strands (+ Strands_History).");

                // Unique filtered index: enforces no two non-null codes can match.
                // Filtered indexes are not temporal-table-gated, so no SYSTEM_VERSIONING dance needed.
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                                   WHERE object_id = OBJECT_ID('Strands') AND name = 'IX_Strands_StrandCode')
                    BEGIN
                        CREATE UNIQUE INDEX [IX_Strands_StrandCode]
                            ON [dbo].[Strands] ([StrandCode])
                            WHERE [StrandCode] IS NOT NULL;
                    END;
                    """);
                Console.WriteLine("  ✔ Unique filtered index IX_Strands_StrandCode created.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ StrandCode migration failed: {ex.Message}");
                failures++;
            }
        }

        return failures > 0 ? 1 : 0;
    }
}
