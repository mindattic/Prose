using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Cli;

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

        // Beat soft-delete: add IsEnabled to NodeBeats (and its history table).
        var nodeBeatSoftDelete = args.Contains("--node-beat-soft-delete");

        // Node + Beat version counter: add Version INT to Beats, Nodes (and history tables).
        var nodeBeatVersion = args.Contains("--node-beat-version");

        // Entity grammar notes: add GrammarNote NVARCHAR(MAX) to Entities (and history table).
        var entityGrammarNote = args.Contains("--entity-grammar-note");

        // Node short reference code: add NodeCode NVARCHAR(20) to Nodes (+ history) with a
        // unique filtered index (enforced for non-null values only).
        var nodeCode = args.Contains("--story-code");

        // Entity quality reviews: create EntityReviews + EntityReviewSummaries tables.
        var entityReviews = args.Contains("--entity-reviews");

        // Node Bible: add NodeBible + NodeBibleGeneratedAt to Nodes (+ history table).
        var nodeBible = args.Contains("--node-bible");

        // MarkdownFiles: create the MarkdownFiles table so .md files (project rules,
        // Codex docs, Claude Code memory) can be backed up + restored by timestamp.
        var markdownFiles = args.Contains("--markdown-files");

        // Node spine: add NodeUserStories columns to Nodes + create
        // NodeAmendments and NodeSpineVersions tables.
        var nodeSpine = args.Contains("--node-spine");

        // Emotional examination (SS-A15): 4 new tables + Beat.EmotionalScore column.
        var emotionalExamination = args.Contains("--emotional-examination");

        // Node draft flag: add IsDraft BIT to Nodes (+ history). Draft nodes
        // (and their whole subtree) are ignored by the tools.
        var nodeDraftFlag = args.Contains("--node-draft-flag");

        // Review contradictions: add Contradictions NVARCHAR(MAX) to EntityReviews,
        // NodeReviews, and Gripes+Contradictions to NodeReviewBeatScores.
        var reviewContradictions = args.Contains("--review-contradictions");

        // Distributed work queue: create DistributedWorkQueue table for multi-machine
        // entity-review / node-review / beat-review / beat-write.
        var distributedQueue = args.Contains("--distributed-queue");

        if (!schema && !charRelational && !charDropLegacy && !nodeBeatSoftDelete && !nodeBeatVersion && !entityGrammarNote && !nodeCode && !entityReviews && !nodeBible && !markdownFiles && !nodeSpine && !emotionalExamination && !nodeDraftFlag && !reviewContradictions && !distributedQueue)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  ss --migrate-sql --schema                    apply EF migrations + enable SYSTEM_VERSIONING");
            Console.WriteLine("  ss --migrate-sql --node-beat-soft-delete   add IsEnabled column to NodeBeats/NodeBeats_History");
            Console.WriteLine("  ss --migrate-sql --node-beat-version       add Version INT counter to Beats+Nodes (and history tables)");
            Console.WriteLine("  ss --migrate-sql --entity-grammar-note       add GrammarNote column to Entities (and history table)");
            Console.WriteLine("  ss --migrate-sql --story-code               add NodeCode NVARCHAR(20) to Nodes (unique per non-null value)");
            Console.WriteLine("  ss --migrate-sql --entity-reviews            create EntityReviews + EntityReviewSummaries tables");
            Console.WriteLine("  ss --migrate-sql --node-bible              add NodeBible + NodeBibleGeneratedAt to Nodes (+ history)");
            Console.WriteLine("  ss --migrate-sql --markdown-files            create MarkdownFiles table (project-rules, Codex, memory backup)");
            Console.WriteLine("  ss --migrate-sql --node-spine              add NodeUserStories to Nodes; create NodeAmendments + NodeSpineVersions");
            Console.WriteLine("  ss --migrate-sql --emotional-examination     create EmotionalExaminations/DimensionResults/BeatScores/CharacterEmotionalLedgers + Beat.EmotionalScore (SS-A15)");
            Console.WriteLine("  ss --migrate-sql --node-draft-flag         add IsDraft BIT to Nodes (+ history); draft subtrees are ignored by the tools");
            Console.WriteLine("  ss --migrate-sql --review-contradictions     add Contradictions to EntityReviews + NodeReviews; Gripes+Contradictions to NodeReviewBeatScores");
            Console.WriteLine("  ss --migrate-sql --distributed-queue         create DistributedWorkQueue table (multi-machine entity/node/beat review + prose writing)");
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

        if (nodeBeatSoftDelete)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();

            Console.WriteLine();
            Console.WriteLine("[node-beat-soft-delete]");
            try
            {
                // NodeBeats and NodeBeats_History are temporal tables.
                // To add a column we must briefly disable system versioning,
                // alter both tables, then re-enable it.
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID('NodeBeats') AND name = 'IsEnabled')
                    BEGIN
                        ALTER TABLE [dbo].[NodeBeats] SET (SYSTEM_VERSIONING = OFF);
                        ALTER TABLE [dbo].[NodeBeats]         ADD [IsEnabled] bit NOT NULL DEFAULT 1;
                        ALTER TABLE [dbo].[NodeBeats_History] ADD [IsEnabled] bit NOT NULL DEFAULT 1;
                        ALTER TABLE [dbo].[NodeBeats]
                            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[NodeBeats_History],
                                                         DATA_CONSISTENCY_CHECK = OFF));
                    END;
                    """);
                Console.WriteLine("  ✔ IsEnabled column added to NodeBeats (+ history table).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ failed: {ex.Message}");
                failures++;
            }
        }

        if (nodeBeatVersion)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();

            Console.WriteLine();
            Console.WriteLine("[node-beat-version]");
            foreach (var (table, hist) in new[] { ("Beats", "Beats_History"), ("Nodes", "Nodes_History") })
            {
                try
                {
#pragma warning disable EF1002 // table/hist are hardcoded strings — not user input
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
#pragma warning restore EF1002
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

        if (nodeCode)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();

            Console.WriteLine();
            Console.WriteLine("[story-code]");
            try
            {
                // Add the column to the temporal table + its history shadow.
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID('Nodes') AND name = 'NodeCode')
                    BEGIN
                        ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);
                        ALTER TABLE [dbo].[Nodes]         ADD [NodeCode] NVARCHAR(20) NULL;
                        ALTER TABLE [dbo].[Nodes_History] ADD [NodeCode] NVARCHAR(20) NULL;
                        ALTER TABLE [dbo].[Nodes]
                            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History],
                                                         DATA_CONSISTENCY_CHECK = OFF));
                    END;
                    """);
                Console.WriteLine("  ✔ NodeCode column added to Nodes (+ Nodes_History).");

                // Unique filtered index: enforces no two non-null codes can match.
                // Filtered indexes are not temporal-table-gated, so no SYSTEM_VERSIONING dance needed.
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                                   WHERE object_id = OBJECT_ID('Nodes') AND name = 'IX_Nodes_NodeCode')
                    BEGIN
                        CREATE UNIQUE INDEX [IX_Nodes_NodeCode]
                            ON [dbo].[Nodes] ([NodeCode])
                            WHERE [NodeCode] IS NOT NULL;
                    END;
                    """);
                Console.WriteLine("  ✔ Unique filtered index IX_Nodes_NodeCode created.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ NodeCode migration failed: {ex.Message}");
                failures++;
            }
        }

        if (entityReviews)
        {
            using var erScope = sp.CreateScope();
            var erDb = erScope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();
            Console.WriteLine();
            Console.WriteLine("[entity-reviews]");
            try
            {
                await erDb.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EntityReviews')
                    BEGIN
                        CREATE TABLE [dbo].[EntityReviews] (
                            [Id]           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
                            [EntityId]     NVARCHAR(200)    NOT NULL,
                            [EntityType]   NVARCHAR(40)     NOT NULL,
                            [EntityName]   NVARCHAR(400)    NOT NULL DEFAULT '',
                            [PersonaId]    NVARCHAR(40)     NOT NULL DEFAULT '',
                            [PersonaName]  NVARCHAR(80)     NOT NULL DEFAULT '',
                            [PersonaBlurb] NVARCHAR(400)    NULL,
                            [ProviderId]   NVARCHAR(40)     NOT NULL DEFAULT '',
                            [Model]        NVARCHAR(80)     NULL,
                            [Score]        INT              NOT NULL DEFAULT 0,
                            [ReviewText]   NVARCHAR(MAX)    NOT NULL DEFAULT '',
                            [Improvements] NVARCHAR(MAX)    NULL,
                            [ContentHash]  NVARCHAR(64)     NOT NULL DEFAULT '',
                            [ReviewedAt]   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
                            [CreatedAt]    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
                            [UpdatedAt]    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
                        );
                        CREATE INDEX [IX_EntityReviews_EntityId_Type_ReviewedAt]
                            ON [dbo].[EntityReviews] ([EntityId], [EntityType], [ReviewedAt] DESC);
                        CREATE INDEX [IX_EntityReviews_EntityType_ReviewedAt]
                            ON [dbo].[EntityReviews] ([EntityType], [ReviewedAt] DESC);
                    END;
                    """);
                Console.WriteLine("  ✔ EntityReviews table created (or already exists).");

                await erDb.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EntityReviewSummaries')
                    BEGIN
                        CREATE TABLE [dbo].[EntityReviewSummaries] (
                            [Id]                    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
                            [EntityId]              NVARCHAR(200)    NOT NULL,
                            [EntityType]            NVARCHAR(40)     NOT NULL,
                            [EntityName]            NVARCHAR(400)    NOT NULL DEFAULT '',
                            [ReviewCount]           INT              NOT NULL DEFAULT 0,
                            [AvgScore]              FLOAT            NOT NULL DEFAULT 0,
                            [ScoreDistributionJson] NVARCHAR(MAX)    NULL,
                            [SummaryMarkdown]       NVARCHAR(MAX)    NULL,
                            [ContentHash]           NVARCHAR(64)     NOT NULL DEFAULT '',
                            [GeneratedAt]           DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
                        );
                        CREATE UNIQUE INDEX [IX_EntityReviewSummaries_EntityId_Type]
                            ON [dbo].[EntityReviewSummaries] ([EntityId], [EntityType]);
                    END;
                    """);
                Console.WriteLine("  ✔ EntityReviewSummaries table created (or already exists).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ entity-reviews migration failed: {ex.Message}");
                failures++;
            }
        }

        if (nodeBible)
        {
            using var sbScope = sp.CreateScope();
            var sbDb = sbScope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();
            Console.WriteLine();
            Console.WriteLine("[node-bible]");
            try
            {
                await sbDb.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID('Nodes') AND name = 'NodeBible')
                    BEGIN
                        ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);
                        ALTER TABLE [dbo].[Nodes]         ADD [NodeBible]             NVARCHAR(MAX) NULL;
                        ALTER TABLE [dbo].[Nodes_History] ADD [NodeBible]             NVARCHAR(MAX) NULL;
                        ALTER TABLE [dbo].[Nodes]         ADD [NodeBibleGeneratedAt]  DATETIME2     NULL;
                        ALTER TABLE [dbo].[Nodes_History] ADD [NodeBibleGeneratedAt]  DATETIME2     NULL;
                        ALTER TABLE [dbo].[Nodes]
                            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History],
                                                         DATA_CONSISTENCY_CHECK = OFF));
                    END;
                    """);
                Console.WriteLine("  ✔ NodeBible + NodeBibleGeneratedAt columns added to Nodes (+ Nodes_History).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ node-bible migration failed: {ex.Message}");
                failures++;
            }
        }

        if (nodeSpine)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();

            Console.WriteLine();
            Console.WriteLine("[node-spine]");
            try
            {
                // Phase A: add NodeUserStories + NodeUserStoriesUpdatedAt to Nodes
                // (temporal table — must turn versioning off, alter both tables, then re-enable).
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID('Nodes') AND name = 'NodeUserStories')
                    BEGIN
                        ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);
                        ALTER TABLE [dbo].[Nodes]         ADD [NodeUserStories]           NVARCHAR(MAX)  NULL;
                        ALTER TABLE [dbo].[Nodes_History] ADD [NodeUserStories]           NVARCHAR(MAX)  NULL;
                        ALTER TABLE [dbo].[Nodes]         ADD [NodeUserStoriesUpdatedAt]  DATETIME2      NULL;
                        ALTER TABLE [dbo].[Nodes_History] ADD [NodeUserStoriesUpdatedAt]  DATETIME2      NULL;
                        ALTER TABLE [dbo].[Nodes]
                            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History],
                                                         DATA_CONSISTENCY_CHECK = OFF));
                    END;
                    """);
                Console.WriteLine("  ✔ NodeUserStories columns added to Nodes (+ Nodes_History).");

                // Phase B: create NodeAmendments table.
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NodeAmendments]') AND type = N'U')
                    BEGIN
                        CREATE TABLE [dbo].[NodeAmendments] (
                            [Id]         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                            [NodeId]   UNIQUEIDENTIFIER NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                            [SequenceNo] INT              NOT NULL DEFAULT 0,
                            [Code]       NVARCHAR(20)     NOT NULL DEFAULT '',
                            [Summary]    NVARCHAR(500)    NOT NULL DEFAULT '',
                            [Body]       NVARCHAR(MAX)    NOT NULL DEFAULT '',
                            [CreatedAt]  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
                            [CreatedBy]  NVARCHAR(200)    NOT NULL DEFAULT '',
                            CONSTRAINT [PK_NodeAmendments] PRIMARY KEY ([Id])
                        );
                        CREATE        INDEX [IX_NodeAmendments_NodeId]            ON [dbo].[NodeAmendments] ([NodeId]);
                        CREATE UNIQUE INDEX [IX_NodeAmendments_NodeId_SequenceNo] ON [dbo].[NodeAmendments] ([NodeId], [SequenceNo]);
                    END;
                    """);
                Console.WriteLine("  ✔ NodeAmendments table created (or already exists).");

                // Phase C: create NodeSpineVersions table (bridge).
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[NodeSpineVersions]') AND type = N'U')
                    BEGIN
                        CREATE TABLE [dbo].[NodeSpineVersions] (
                            [Id]               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                            [NodeId]         UNIQUEIDENTIFIER NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                            [NodeVersion]    INT              NOT NULL DEFAULT 0,
                            [BibleHash]        NVARCHAR(64)     NOT NULL DEFAULT '',
                            [UserStoriesHash]  NVARCHAR(64)     NOT NULL DEFAULT '',
                            [AmendmentCount]   INT              NOT NULL DEFAULT 0,
                            [PinnedAt]         DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
                            [PinnedBy]         NVARCHAR(100)    NOT NULL DEFAULT '',
                            [Notes]            NVARCHAR(1000)   NOT NULL DEFAULT '',
                            CONSTRAINT [PK_NodeSpineVersions] PRIMARY KEY ([Id])
                        );
                        CREATE        INDEX [IX_NodeSpineVersions_NodeId]              ON [dbo].[NodeSpineVersions] ([NodeId]);
                        CREATE UNIQUE INDEX [IX_NodeSpineVersions_NodeId_NodeVersion] ON [dbo].[NodeSpineVersions] ([NodeId], [NodeVersion]);
                    END;
                    """);
                Console.WriteLine("  ✔ NodeSpineVersions table created (or already exists).");

                // Phase D: enable system versioning on the two new tables.
                Console.WriteLine("  · enabling system versioning on NodeAmendments + NodeSpineVersions…");
                await db.EnableSystemVersioningAsync(onError: (t, ex) =>
                    Console.WriteLine($"  ✘ system versioning failed for {t}: {ex.Message}"));
                Console.WriteLine("  ✔ both tables are temporal.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ node-spine migration failed: {ex.Message}");
                failures++;
            }
        }

        if (markdownFiles)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();

            Console.WriteLine();
            Console.WriteLine("[markdown-files]");
            try
            {
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MarkdownFiles]') AND type = N'U')
                    BEGIN
                        CREATE TABLE [dbo].[MarkdownFiles] (
                            [Id]            UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                            [FilePath]      NVARCHAR(2000)   NOT NULL DEFAULT '',
                            [FileRoot]      NVARCHAR(100)    NOT NULL DEFAULT '',
                            [RelativePath]  NVARCHAR(2000)   NOT NULL DEFAULT '',
                            [FileName]      NVARCHAR(500)    NOT NULL DEFAULT '',
                            [Category]      NVARCHAR(100)    NOT NULL DEFAULT '',
                            [Content]       NVARCHAR(MAX)    NOT NULL DEFAULT '',
                            [ContentHash]   NVARCHAR(64)     NOT NULL DEFAULT '',
                            [LastSyncedAt]  DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
                            [SyncedBy]      NVARCHAR(100)    NOT NULL DEFAULT '',
                            CONSTRAINT [PK_MarkdownFiles] PRIMARY KEY ([Id])
                        );
                        CREATE UNIQUE INDEX [IX_MarkdownFiles_FileRoot_RelativePath] ON [dbo].[MarkdownFiles] ([FileRoot], [RelativePath]);
                        CREATE        INDEX [IX_MarkdownFiles_Category]     ON [dbo].[MarkdownFiles] ([Category]);
                        CREATE        INDEX [IX_MarkdownFiles_LastSyncedAt] ON [dbo].[MarkdownFiles] ([LastSyncedAt]);
                    END;
                    """);
                Console.WriteLine("  ✔ MarkdownFiles table created (or already exists).");

                // Swap the legacy single-column unique index for a composite (FileRoot, RelativePath)
                // unique index. RelativePath alone is NOT unique: project + global CLAUDE.md both have
                // RelativePath "CLAUDE.md" and differ only by FileRoot. Idempotent.
                await db.Database.ExecuteSqlRawAsync("""
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MarkdownFiles_RelativePath' AND object_id = OBJECT_ID(N'[dbo].[MarkdownFiles]'))
                        DROP INDEX [IX_MarkdownFiles_RelativePath] ON [dbo].[MarkdownFiles];
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MarkdownFiles_FileRoot_RelativePath' AND object_id = OBJECT_ID(N'[dbo].[MarkdownFiles]'))
                        CREATE UNIQUE INDEX [IX_MarkdownFiles_FileRoot_RelativePath] ON [dbo].[MarkdownFiles] ([FileRoot], [RelativePath]);
                    """);
                Console.WriteLine("  ✔ Unique index is composite (FileRoot, RelativePath).");

                // Doc Context Stack columns (dynamic .md working-set engine). Idempotent ADD;
                // guarded per-column so re-runs are no-ops. Temporal table tolerates ADD with DEFAULT.
                await db.Database.ExecuteSqlRawAsync("""
                    IF COL_LENGTH('dbo.MarkdownFiles','Tier') IS NULL
                        ALTER TABLE [dbo].[MarkdownFiles] ADD [Tier] NVARCHAR(20) NOT NULL CONSTRAINT [DF_MarkdownFiles_Tier] DEFAULT 'topic';
                    IF COL_LENGTH('dbo.MarkdownFiles','Scope') IS NULL
                        ALTER TABLE [dbo].[MarkdownFiles] ADD [Scope] NVARCHAR(1000) NOT NULL CONSTRAINT [DF_MarkdownFiles_Scope] DEFAULT '';
                    IF COL_LENGTH('dbo.MarkdownFiles','Triggers') IS NULL
                        ALTER TABLE [dbo].[MarkdownFiles] ADD [Triggers] NVARCHAR(2000) NOT NULL CONSTRAINT [DF_MarkdownFiles_Triggers] DEFAULT '';
                    IF COL_LENGTH('dbo.MarkdownFiles','AutoTier') IS NULL
                        ALTER TABLE [dbo].[MarkdownFiles] ADD [AutoTier] BIT NOT NULL CONSTRAINT [DF_MarkdownFiles_AutoTier] DEFAULT 1;
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MarkdownFiles_Tier' AND object_id = OBJECT_ID(N'[dbo].[MarkdownFiles]'))
                        CREATE INDEX [IX_MarkdownFiles_Tier] ON [dbo].[MarkdownFiles] ([Tier]);
                    """);
                Console.WriteLine("  ✔ Doc-context columns present (Tier, Scope, Triggers, AutoTier).");

                // Enable system versioning via the same idempotent EnableSystemVersioningAsync path.
                Console.WriteLine("  · enabling system versioning on MarkdownFiles…");
                await db.EnableSystemVersioningAsync(onError: (t, ex) =>
                    Console.WriteLine($"  ✘ system versioning failed for {t}: {ex.Message}"));
                Console.WriteLine("  ✔ MarkdownFiles is temporal (MarkdownFiles_History).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ markdown-files migration failed: {ex.Message}");
                failures++;
            }
        }

        // ── Emotional examination (SS-A15) ────────────────────────────────────
        if (emotionalExamination)
        {
            using var eeScope = sp.CreateScope();
            var eeDb = eeScope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();
            Console.WriteLine();
            Console.WriteLine("[emotional-examination]");
            try
            {
                // 1. Beat.EmotionalScore — temporal table dance (mirrors --node-beat-version)
                await eeDb.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID('Beats') AND name = 'EmotionalScore')
                    BEGIN
                        ALTER TABLE [dbo].[Beats] SET (SYSTEM_VERSIONING = OFF);
                        ALTER TABLE [dbo].[Beats]         ADD [EmotionalScore] FLOAT NULL;
                        ALTER TABLE [dbo].[Beats_History] ADD [EmotionalScore] FLOAT NULL;
                        ALTER TABLE [dbo].[Beats]
                            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Beats_History],
                                                         DATA_CONSISTENCY_CHECK = OFF));
                    END;
                    """);
                Console.WriteLine("  ✔ Beat.EmotionalScore column added (+ Beats_History).");

                // 2. EmotionalExaminations (parent)
                await eeDb.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmotionalExaminations]') AND type = N'U')
                    BEGIN
                        CREATE TABLE [dbo].[EmotionalExaminations] (
                            [Id]                 UNIQUEIDENTIFIER NOT NULL,
                            [NodeId]           UNIQUEIDENTIFIER NOT NULL,
                            [EffortTier]         NVARCHAR(20)     NOT NULL DEFAULT 'standard',
                            [EmotionalDepthScore] FLOAT           NOT NULL DEFAULT 0,
                            [Register]           NVARCHAR(40)     NOT NULL DEFAULT '',
                            [ContentHash]        NVARCHAR(64)     NOT NULL DEFAULT '',
                            [BeatCount]          INT              NOT NULL DEFAULT 0,
                            [BlockingCount]      INT              NOT NULL DEFAULT 0,
                            [Model]              NVARCHAR(80)         NULL,
                            [ExaminedAt]         DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
                            [CreatedAt]          DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
                            CONSTRAINT [PK_EmotionalExaminations] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_EmotionalExaminations_Nodes] FOREIGN KEY ([NodeId])
                                REFERENCES [dbo].[Nodes] ([Id]) ON DELETE CASCADE
                        );
                        CREATE INDEX [IX_EmotionalExaminations_NodeId_ExaminedAt]
                            ON [dbo].[EmotionalExaminations] ([NodeId], [ExaminedAt]);
                    END;
                    """);
                Console.WriteLine("  ✔ EmotionalExaminations table created (or already exists).");

                // 3. EmotionalDimensionResults (cascade child)
                await eeDb.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmotionalDimensionResults]') AND type = N'U')
                    BEGIN
                        CREATE TABLE [dbo].[EmotionalDimensionResults] (
                            [ExaminationId]      UNIQUEIDENTIFIER NOT NULL,
                            [Dimension]          INT              NOT NULL,
                            [Score]              INT              NOT NULL DEFAULT 0,
                            [StrongestEvidence]  NVARCHAR(MAX)        NULL,
                            [WeakestEvidence]    NVARCHAR(MAX)        NULL,
                            [WeakestBeatNumber]  INT                  NULL,
                            [Fix]                NVARCHAR(MAX)        NULL,
                            [CraftLaw]           NVARCHAR(500)        NULL,
                            [IsBlocking]         BIT              NOT NULL DEFAULT 0,
                            CONSTRAINT [PK_EmotionalDimensionResults] PRIMARY KEY ([ExaminationId], [Dimension]),
                            CONSTRAINT [FK_EmotionalDimensionResults_Examinations] FOREIGN KEY ([ExaminationId])
                                REFERENCES [dbo].[EmotionalExaminations] ([Id]) ON DELETE CASCADE
                        );
                    END;
                    """);
                Console.WriteLine("  ✔ EmotionalDimensionResults table created (or already exists).");

                // 4. EmotionalBeatScores (cascade child)
                await eeDb.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmotionalBeatScores]') AND type = N'U')
                    BEGIN
                        CREATE TABLE [dbo].[EmotionalBeatScores] (
                            [ExaminationId]  UNIQUEIDENTIFIER NOT NULL,
                            [BeatNumber]     INT              NOT NULL,
                            [Depth]          INT              NOT NULL DEFAULT 0,
                            [Note]           NVARCHAR(MAX)        NULL,
                            CONSTRAINT [PK_EmotionalBeatScores] PRIMARY KEY ([ExaminationId], [BeatNumber]),
                            CONSTRAINT [FK_EmotionalBeatScores_Examinations] FOREIGN KEY ([ExaminationId])
                                REFERENCES [dbo].[EmotionalExaminations] ([Id]) ON DELETE CASCADE
                        );
                    END;
                    """);
                Console.WriteLine("  ✔ EmotionalBeatScores table created (or already exists).");

                // 5. CharacterEmotionalLedgers (cache, not cascade — lives beyond a single examination)
                await eeDb.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CharacterEmotionalLedgers]') AND type = N'U')
                    BEGIN
                        CREATE TABLE [dbo].[CharacterEmotionalLedgers] (
                            [Id]              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                            [NodeId]        UNIQUEIDENTIFIER NOT NULL,
                            [Character]       NVARCHAR(200)    NOT NULL,
                            [Want]            NVARCHAR(MAX)        NULL,
                            [Need]            NVARCHAR(MAX)        NULL,
                            [Wound]           NVARCHAR(MAX)        NULL,
                            [Flaw]            NVARCHAR(MAX)        NULL,
                            [VoiceRegister]   NVARCHAR(200)        NULL,
                            [Inferred]        BIT              NOT NULL DEFAULT 0,
                            [SourceBibleHash] NVARCHAR(64)         NULL,
                            [UpdatedAt]       DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
                            CONSTRAINT [PK_CharacterEmotionalLedgers] PRIMARY KEY ([Id]),
                            CONSTRAINT [FK_CharacterEmotionalLedgers_Nodes] FOREIGN KEY ([NodeId])
                                REFERENCES [dbo].[Nodes] ([Id]) ON DELETE CASCADE
                        );
                        CREATE UNIQUE INDEX [IX_CharacterEmotionalLedgers_NodeId_Character]
                            ON [dbo].[CharacterEmotionalLedgers] ([NodeId], [Character]);
                    END;
                    """);
                Console.WriteLine("  ✔ CharacterEmotionalLedgers table created (or already exists).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ emotional-examination migration failed: {ex.Message}");
                failures++;
            }
        }

        if (nodeDraftFlag)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();

            Console.WriteLine();
            Console.WriteLine("[node-draft-flag]");
            try
            {
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID('Nodes') AND name = 'IsDraft')
                    BEGIN
                        ALTER TABLE [dbo].[Nodes] SET (SYSTEM_VERSIONING = OFF);
                        ALTER TABLE [dbo].[Nodes]         ADD [IsDraft] bit NOT NULL DEFAULT 0;
                        ALTER TABLE [dbo].[Nodes_History] ADD [IsDraft] bit NOT NULL DEFAULT 0;
                        ALTER TABLE [dbo].[Nodes]
                            SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Nodes_History],
                                                         DATA_CONSISTENCY_CHECK = OFF));
                    END;
                    """);
                Console.WriteLine("  ✔ IsDraft column added to Nodes (+ Nodes_History).");

                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                                   WHERE object_id = OBJECT_ID('Nodes') AND name = 'IX_Nodes_IsDraft')
                        CREATE INDEX [IX_Nodes_IsDraft] ON [dbo].[Nodes] ([IsDraft]);
                    """);
                Console.WriteLine("  ✔ IX_Nodes_IsDraft index created (or already exists).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ node-draft-flag migration failed: {ex.Message}");
                failures++;
            }
        }

        if (reviewContradictions)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();

            Console.WriteLine();
            Console.WriteLine("[review-contradictions]");
            try
            {
                // EntityReviews — not temporal, straight ALTER TABLE.
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID('EntityReviews') AND name = 'Contradictions')
                        ALTER TABLE [dbo].[EntityReviews] ADD [Contradictions] NVARCHAR(MAX) NULL;
                    """);
                Console.WriteLine("  ✔ Contradictions added to EntityReviews.");

                // NodeReviews — not temporal.
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID('NodeReviews') AND name = 'Contradictions')
                        ALTER TABLE [dbo].[NodeReviews] ADD [Contradictions] NVARCHAR(MAX) NULL;
                    """);
                Console.WriteLine("  ✔ Contradictions added to NodeReviews.");

                // NodeReviewBeatScores — not temporal.
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID('NodeReviewBeatScores') AND name = 'Gripes')
                        ALTER TABLE [dbo].[NodeReviewBeatScores] ADD [Gripes] NVARCHAR(MAX) NULL;
                    IF NOT EXISTS (SELECT 1 FROM sys.columns
                                   WHERE object_id = OBJECT_ID('NodeReviewBeatScores') AND name = 'Contradictions')
                        ALTER TABLE [dbo].[NodeReviewBeatScores] ADD [Contradictions] NVARCHAR(MAX) NULL;
                    """);
                Console.WriteLine("  ✔ Gripes + Contradictions added to NodeReviewBeatScores.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ review-contradictions migration failed: {ex.Message}");
                failures++;
            }
        }

        if (distributedQueue)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StreetSamuraiDbContext>();

            Console.WriteLine();
            Console.WriteLine("[distributed-queue]");
            try
            {
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DistributedWorkQueue]') AND type = N'U')
                    BEGIN
                        CREATE TABLE [dbo].[DistributedWorkQueue] (
                            [Id]           UNIQUEIDENTIFIER NOT NULL,
                            [WorkType]     NVARCHAR(40)     NOT NULL DEFAULT 'entity-review',
                            [TargetId]     NVARCHAR(64)     NOT NULL,
                            [TargetType]   NVARCHAR(40)     NOT NULL,
                            [TargetName]   NVARCHAR(400)    NOT NULL,
                            [PayloadJson]  NVARCHAR(MAX)        NULL,
                            [Status]       NVARCHAR(20)     NOT NULL DEFAULT 'pending',
                            [ClaimedBy]    NVARCHAR(100)        NULL,
                            [ClaimedAt]    DATETIME2            NULL,
                            [CompletedAt]  DATETIME2            NULL,
                            [RetryCount]   INT              NOT NULL DEFAULT 0,
                            [ErrorMessage] NVARCHAR(MAX)        NULL,
                            [CreatedAt]    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
                            CONSTRAINT [PK_DistributedWorkQueue] PRIMARY KEY ([Id])
                        );
                        CREATE INDEX [IX_DistributedWorkQueue_WorkType_Status_ClaimedAt]
                            ON [dbo].[DistributedWorkQueue] ([WorkType], [Status], [ClaimedAt]);
                        CREATE INDEX [IX_DistributedWorkQueue_TargetId]
                            ON [dbo].[DistributedWorkQueue] ([TargetId]);
                    END;
                    """);
                Console.WriteLine("  ✔ DistributedWorkQueue table created (or already exists).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✘ distributed-queue migration failed: {ex.Message}");
                failures++;
            }
        }

        return failures > 0 ? 1 : 0;
    }
}
