using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Models.Canon;

namespace StreetSamurai.Core.Data;

/// <summary>
/// One-shot migration that takes a live Characters table from the JSON-blob
/// schema (StatsJson / PsychologyJson / BehavioralJson / SpeechJson / PhysicalJson /
/// TerritoryJson / AncestryJson / NeuralJson / BelongingsJson / ArchetypesJson)
/// to a fully relational schema (40+ scalar columns + 25 bridge tables).
///
/// Three phases. Each phase is idempotent — re-running is safe.
///   Phase A — ALTER TABLE Characters: add the new scalar columns. Additive only;
///             nothing is dropped here so the *Json blobs remain available as a
///             rollback / verification source.
///   Phase B — EnsureCreated for the bridge tables (CharacterAliases / StoryHooks /
///             ArchetypeScores / GeneticAncestries / AncestryDetails / PsychologyTraits /
///             SpeechPhrases / BehavioralRules / BehavioralMaps / StatScalars /
///             StatPhrases / PhysicalMarks / TerritoryZones / TerritoryReputations /
///             BelongingsGear / BelongingsExtras / BioBatteryThresholds /
///             NeuralAbilities / Changelog / KnowledgeEntities / TimelineBodyChanges).
///   Phase C — Backfill: for every row in Records where EntityType='character',
///             deserialize the canonical JSON and run it through
///             <see cref="CharacterMapper"/> to populate the new columns + bridges.
///
/// Phase D (drop legacy *Json columns) is offered as a separate step so the user
/// can verify backfill before deleting source-of-truth data.
/// </summary>
public class CharacterRelationalMigration
{
    private readonly StreetSamuraiDbContext db;
    public CharacterRelationalMigration(StreetSamuraiDbContext db) { this.db = db; }

    public class Report
    {
        public int CharactersBackfilled { get; set; }
        public int CharactersFailed { get; set; }
        public List<string> Errors { get; } = new();
        public List<string> SchemaActions { get; } = new();
    }

    /// <summary>
    /// Phase A — add scalar columns to Characters. Each column is added with
    /// IF COL_LENGTH ... IS NULL so re-running is safe. NOT NULL columns get
    /// DEFAULT '' so SQL Server's system-versioning can apply them without
    /// requiring SYSTEM_VERSIONING OFF first.
    /// </summary>
    public async Task<Report> ApplyPhaseAAsync(CancellationToken ct = default)
    {
        var rep = new Report();
        if (!db.Database.IsSqlServer())
        {
            // SQLite test path — EnsureCreated already includes everything.
            rep.SchemaActions.Add("(non-SQL-Server provider — Phase A skipped)");
            return rep;
        }

        // (column name, type, default expression). Default '' for NOT NULL strings,
        // 0 for NOT NULL numerics.
        var additions = new (string Name, string Type, string Default)[]
        {
            // Names
            ("Name",        "NVARCHAR(450) NOT NULL", "''"),
            ("FirstName",   "NVARCHAR(200) NOT NULL", "''"),
            ("MiddleName",  "NVARCHAR(200) NOT NULL", "''"),
            ("LastName",    "NVARCHAR(200) NOT NULL", "''"),
            ("TitlePrefix", "NVARCHAR(40)  NOT NULL", "''"),
            // Identity / classification additions
            ("Rating",     "FLOAT          NOT NULL", "0"),
            ("VoteCount",  "INT            NOT NULL", "0"),
            ("LifeStatus", "NVARCHAR(40)   NOT NULL", "'alive'"),
            ("Location",   "NVARCHAR(450)  NOT NULL", "''"),
            // Prose blobs (kept as plain NVARCHAR(MAX))
            ("Description",      "NVARCHAR(MAX) NOT NULL", "''"),
            ("Augmentations",    "NVARCHAR(MAX) NOT NULL", "''"),
            ("DailyLife",        "NVARCHAR(MAX) NOT NULL", "''"),
            // Belongings (scalar refs)
            // BelongingsPrimaryWeapon / SecondaryWeapon / Armor / Vehicle /
            // Residence / ClothingStyle / FavoriteDrink / FavoriteFood /
            // Stimulant / CommDevice all dropped 2026-05-08 — single-row
            // buckets in CharacterBelongingsGear are the new home.
            // Operating territory scalars — TerritoryHomeTurf dropped 2026-05-08;
            // canonical source is the CharacterHomeTurfs bridge.
            ("TerritoryRange",    "NVARCHAR(40)  NOT NULL", "'local'"),
            // Physical description scalars
            ("Heritage",              "NVARCHAR(450) NOT NULL", "''"),
            ("HeightCm",              "INT           NOT NULL", "0"),
            ("WeightKg",              "INT           NOT NULL", "0"),
            ("Build",                 "NVARCHAR(120) NOT NULL", "''"),
            ("HairColor",             "NVARCHAR(80)  NOT NULL", "''"),
            ("HairStyle",             "NVARCHAR(120) NOT NULL", "''"),
            ("HairLength",            "NVARCHAR(40)  NOT NULL", "''"),
            ("EyeColor",              "NVARCHAR(80)  NOT NULL", "''"),
            ("SkinTone",              "NVARCHAR(80)  NOT NULL", "''"),
            ("Complexion",            "NVARCHAR(120) NOT NULL", "''"),
            ("VisibleAugmentations",  "NVARCHAR(MAX) NOT NULL", "''"),
            ("PostureMovement",       "NVARCHAR(MAX) NOT NULL", "''"),
            ("PhysicalClothingStyle", "NVARCHAR(MAX) NOT NULL", "''"),
            // Psychology scalar
            ("PsychologySecret", "NVARCHAR(MAX) NOT NULL", "''"),
            // Speech scalars
            ("SpeechVocabulary",       "NVARCHAR(MAX) NOT NULL", "''"),
            ("SpeechCadence",          "NVARCHAR(MAX) NOT NULL", "''"),
            ("SpeechSubtext",          "NVARCHAR(MAX) NOT NULL", "''"),
            ("SpeechUnderPressure",    "NVARCHAR(MAX) NOT NULL", "''"),
            ("SpeechIntimacyRegister", "NVARCHAR(MAX) NOT NULL", "''"),
            // Bio-battery scalars
            ("BioBatteryMaxCapacity", "NVARCHAR(MAX) NOT NULL", "''"),
            ("BioBatteryRecovery",    "NVARCHAR(MAX) NOT NULL", "''"),
        };

        foreach (var (name, type, def) in additions)
        {
            var sql = $"""
                IF COL_LENGTH(N'[dbo].[Characters]', N'{name}') IS NULL
                    ALTER TABLE [dbo].[Characters] ADD [{name}] {type} CONSTRAINT [DF_Characters_{name}] DEFAULT ({def}) WITH VALUES;
                """;
            try
            {
                await db.Database.ExecuteSqlRawAsync(sql, ct);
                rep.SchemaActions.Add($"+ Characters.{name} ({type})");
            }
            catch (Exception ex)
            {
                rep.Errors.Add($"add Characters.{name}: {ex.Message}");
            }
        }

        return rep;
    }

    /// <summary>
    /// Phase B — let EF create any bridge tables that don't yet exist. Existing
    /// tables are untouched. Required indexes are created by EF as part of this
    /// step. Also enables SYSTEM_VERSIONING on the new tables.
    /// </summary>
    public async Task<Report> ApplyPhaseBAsync(CancellationToken ct = default)
    {
        var rep = new Report();
        try
        {
            // EnsureCreated will skip the database (it exists) and the tables
            // that already exist; in EF Core 8+ it does NOT add missing tables
            // either. So we hand-roll the new tables via SQL when on SQL Server.
            if (db.Database.IsSqlServer())
            {
                foreach (var (name, ddl) in BridgeTableDdl())
                {
                    var guard = $"""
                        IF OBJECT_ID(N'[dbo].[{name}]', N'U') IS NULL
                        BEGIN
                        {ddl}
                        END
                        """;
                    try
                    {
                        await db.Database.ExecuteSqlRawAsync(guard, ct);
                        rep.SchemaActions.Add($"+ table {name}");
                    }
                    catch (Exception ex)
                    {
                        rep.Errors.Add($"create {name}: {ex.Message}");
                    }
                }

                // Now enable SYSTEM_VERSIONING on the temporal set (idempotent).
                await db.EnableSystemVersioningAsync(ct);
                rep.SchemaActions.Add("system versioning re-evaluated");
            }
            else
            {
                await db.Database.EnsureCreatedAsync(ct);
                rep.SchemaActions.Add("EnsureCreated");
            }
        }
        catch (Exception ex)
        {
            rep.Errors.Add($"phase-B failure: {ex.Message}");
        }
        return rep;
    }

    /// <summary>
    /// Phase C — backfill the new schema from <see cref="Record.Json"/>. Iterates
    /// every active character record, deserializes the JSON, and persists via
    /// <see cref="CharacterMapper.PersistAsync"/>. Per-character try/catch so one
    /// bad row doesn't poison the batch; failures are aggregated into the report.
    /// </summary>
    public async Task<Report> ApplyPhaseCAsync(CancellationToken ct = default, IProgress<int>? progress = null)
    {
        var rep = new Report();

        var rows = await db.Records
            .AsNoTracking()
            .Where(r => r.Entity!.EntityType == "character")
            .Select(r => new { r.EntityId, r.Json })
            .ToListAsync(ct);

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var done = 0;
        foreach (var row in rows)
        {
            try
            {
                var data = JsonSerializer.Deserialize<CharacterData>(row.Json, opts);
                if (data == null)
                {
                    rep.CharactersFailed++;
                    rep.Errors.Add($"{row.EntityId:N}: deserialize returned null");
                    continue;
                }
                await CharacterMapper.PersistAsync(db, row.EntityId, data, ct);

                // Make sure the parent Characters row exists and its scalars are
                // populated. PersistAsync handles bridge tables; this writes
                // FullName / FirstName / LastName / etc.
                var ch = await db.Characters.FirstOrDefaultAsync(c => c.Id == row.EntityId, ct);
                if (ch == null)
                {
                    ch = new Character { Id = row.EntityId };
                    db.Characters.Add(ch);
                }
                CharacterMapper.FillScalars(ch, data);

                await db.SaveChangesAsync(ct);
                rep.CharactersBackfilled++;
            }
            catch (Exception ex)
            {
                rep.CharactersFailed++;
                rep.Errors.Add($"{row.EntityId:N}: {DeepestMessage(ex)}");
                foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State != EntityState.Unchanged).ToList())
                    entry.State = EntityState.Detached;
            }
            done++;
            if (done % 50 == 0) progress?.Report(done);
        }
        progress?.Report(done);
        return rep;
    }

    /// <summary>
    /// Phase D — drop the legacy *Json columns from Characters. Run only after
    /// Phase C is verified (e.g. spot-check a few characters in the new schema).
    /// Disables SYSTEM_VERSIONING for the drop, then re-enables.
    /// </summary>
    public async Task<Report> ApplyPhaseDAsync(CancellationToken ct = default)
    {
        var rep = new Report();
        if (!db.Database.IsSqlServer())
        {
            rep.SchemaActions.Add("(non-SQL-Server — Phase D skipped)");
            return rep;
        }

        var legacy = new[]
        {
            "StatsJson", "PsychologyJson", "BehavioralJson", "SpeechJson",
            "PhysicalJson", "TerritoryJson", "AncestryJson", "NeuralJson",
            "BelongingsJson", "ArchetypesJson",
        };

        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE [dbo].[Characters] SET (SYSTEM_VERSIONING = OFF);", ct);

            foreach (var col in legacy)
            {
                var sql = $"""
                    IF COL_LENGTH(N'[dbo].[Characters]', N'{col}') IS NOT NULL
                    BEGIN
                        DECLARE @cn nvarchar(200);
                        SELECT @cn = dc.name FROM sys.default_constraints dc
                            INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
                            WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Characters]')
                              AND c.name = N'{col}';
                        IF @cn IS NOT NULL EXEC('ALTER TABLE [dbo].[Characters] DROP CONSTRAINT [' + @cn + ']');
                        ALTER TABLE [dbo].[Characters] DROP COLUMN [{col}];
                    END;
                    -- Mirror the drop on the history table.
                    IF COL_LENGTH(N'[dbo].[Characters_History]', N'{col}') IS NOT NULL
                        ALTER TABLE [dbo].[Characters_History] DROP COLUMN [{col}];
                    """;
                try
                {
                    await db.Database.ExecuteSqlRawAsync(sql, ct);
                    rep.SchemaActions.Add($"- Characters.{col}");
                }
                catch (Exception ex)
                {
                    rep.Errors.Add($"drop {col}: {ex.Message}");
                }
            }
        }
        finally
        {
            // Always try to re-enable system-versioning so the table doesn't
            // sit in a half-migrated state.
            try
            {
                await db.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE [dbo].[Characters]
                        SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[Characters_History]));
                    """, ct);
                rep.SchemaActions.Add("system versioning re-enabled");
            }
            catch (Exception ex)
            {
                rep.Errors.Add($"re-enable system versioning: {ex.Message}");
            }
        }
        return rep;
    }

    private static string DeepestMessage(Exception ex)
    {
        var e = ex;
        while (e.InnerException != null) e = e.InnerException;
        return e.Message;
    }

    /// <summary>
    /// DDL for every bridge table this migration creates. Kept here (rather than
    /// relying on EF Core 10's silent migration of an existing DB) so the SQL is
    /// auditable and rerunnable. EF Core's OnModelCreating still owns the
    /// runtime mapping; this is the deployment-side schema author.
    /// </summary>
    private static (string Name, string Ddl)[] BridgeTableDdl()
    {
        const string fkChar = "FOREIGN KEY ([CharacterId]) REFERENCES [dbo].[Characters]([Id]) ON DELETE CASCADE";
        return new (string, string)[]
        {
            ("CharacterAliases", $"""
                CREATE TABLE [dbo].[CharacterAliases] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Position] INT NOT NULL,
                    [Value] NVARCHAR(450) NOT NULL,
                    {fkChar}
                );
                CREATE INDEX IX_CharacterAliases_Pos ON [dbo].[CharacterAliases]([CharacterId],[Position]);
                CREATE INDEX IX_CharacterAliases_Value ON [dbo].[CharacterAliases]([Value]);
                """),

            ("CharacterStoryHooks", $"""
                CREATE TABLE [dbo].[CharacterStoryHooks] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Position] INT NOT NULL,
                    [Hook] NVARCHAR(MAX) NOT NULL,
                    {fkChar}
                );
                CREATE INDEX IX_CharacterStoryHooks_Pos ON [dbo].[CharacterStoryHooks]([CharacterId],[Position]);
                """),

            ("CharacterArchetypeScores", $"""
                CREATE TABLE [dbo].[CharacterArchetypeScores] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [ArchetypeName] NVARCHAR(120) NOT NULL,
                    [Score] FLOAT NOT NULL,
                    {fkChar}
                );
                CREATE UNIQUE INDEX IX_CharacterArchetypeScores_KV ON [dbo].[CharacterArchetypeScores]([CharacterId],[ArchetypeName]);
                CREATE INDEX IX_CharacterArchetypeScores_Name ON [dbo].[CharacterArchetypeScores]([ArchetypeName]);
                """),

            ("CharacterGeneticAncestries", $"""
                CREATE TABLE [dbo].[CharacterGeneticAncestries] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Region] NVARCHAR(120) NOT NULL,
                    [Percent] FLOAT NOT NULL,
                    {fkChar}
                );
                CREATE UNIQUE INDEX IX_CharacterGeneticAncestries_KV ON [dbo].[CharacterGeneticAncestries]([CharacterId],[Region]);
                CREATE INDEX IX_CharacterGeneticAncestries_Region ON [dbo].[CharacterGeneticAncestries]([Region]);
                """),

            ("CharacterAncestryDetails", $"""
                CREATE TABLE [dbo].[CharacterAncestryDetails] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Region] NVARCHAR(120) NOT NULL,
                    [SubRegion] NVARCHAR(120) NOT NULL,
                    [Nationality] NVARCHAR(120) NOT NULL,
                    [Percent] FLOAT NOT NULL,
                    {fkChar}
                );
                CREATE UNIQUE INDEX IX_CharacterAncestryDetails_KV ON [dbo].[CharacterAncestryDetails]([CharacterId],[Region],[SubRegion],[Nationality]);
                CREATE INDEX IX_CharacterAncestryDetails_Nat ON [dbo].[CharacterAncestryDetails]([Nationality]);
                """),

            ("CharacterPsychologyTraits", $"""
                CREATE TABLE [dbo].[CharacterPsychologyTraits] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Bucket] NVARCHAR(40) NOT NULL,
                    [Position] INT NOT NULL,
                    [Trait] NVARCHAR(MAX) NOT NULL,
                    {fkChar}
                );
                CREATE INDEX IX_CharacterPsychologyTraits_BP ON [dbo].[CharacterPsychologyTraits]([CharacterId],[Bucket],[Position]);
                """),

            ("CharacterSpeechPhrases", $"""
                CREATE TABLE [dbo].[CharacterSpeechPhrases] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Bucket] NVARCHAR(40) NOT NULL,
                    [Position] INT NOT NULL,
                    [Phrase] NVARCHAR(MAX) NOT NULL,
                    {fkChar}
                );
                CREATE INDEX IX_CharacterSpeechPhrases_BP ON [dbo].[CharacterSpeechPhrases]([CharacterId],[Bucket],[Position]);
                """),

            ("CharacterBehavioralRules", $"""
                CREATE TABLE [dbo].[CharacterBehavioralRules] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Bucket] NVARCHAR(40) NOT NULL,
                    [Position] INT NOT NULL,
                    [Rule] NVARCHAR(MAX) NOT NULL,
                    {fkChar}
                );
                CREATE INDEX IX_CharacterBehavioralRules_BP ON [dbo].[CharacterBehavioralRules]([CharacterId],[Bucket],[Position]);
                """),

            ("CharacterBehavioralMaps", $"""
                CREATE TABLE [dbo].[CharacterBehavioralMaps] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Bucket] NVARCHAR(40) NOT NULL,
                    [KeyName] NVARCHAR(200) NOT NULL,
                    [Value] NVARCHAR(MAX) NOT NULL,
                    {fkChar}
                );
                CREATE UNIQUE INDEX IX_CharacterBehavioralMaps_KV ON [dbo].[CharacterBehavioralMaps]([CharacterId],[Bucket],[KeyName]);
                """),

            ("CharacterStatScalars", $"""
                CREATE TABLE [dbo].[CharacterStatScalars] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Bucket] NVARCHAR(40) NOT NULL,
                    [KeyName] NVARCHAR(200) NOT NULL,
                    [ValueKind] NVARCHAR(20) NOT NULL,
                    [ValueText] NVARCHAR(MAX) NULL,
                    [ValueNumber] FLOAT NULL,
                    [ValueBool] BIT NULL,
                    {fkChar}
                );
                CREATE UNIQUE INDEX IX_CharacterStatScalars_KV ON [dbo].[CharacterStatScalars]([CharacterId],[Bucket],[KeyName]);
                CREATE INDEX IX_CharacterStatScalars_BK ON [dbo].[CharacterStatScalars]([Bucket],[KeyName]);
                """),

            ("CharacterStatPhrases", $"""
                CREATE TABLE [dbo].[CharacterStatPhrases] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Bucket] NVARCHAR(40) NOT NULL,
                    [Position] INT NOT NULL,
                    [Phrase] NVARCHAR(MAX) NOT NULL,
                    {fkChar}
                );
                CREATE INDEX IX_CharacterStatPhrases_BP ON [dbo].[CharacterStatPhrases]([CharacterId],[Bucket],[Position]);
                """),

            ("CharacterPhysicalMarks", $"""
                CREATE TABLE [dbo].[CharacterPhysicalMarks] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Position] INT NOT NULL,
                    [Mark] NVARCHAR(MAX) NOT NULL,
                    {fkChar}
                );
                CREATE INDEX IX_CharacterPhysicalMarks_Pos ON [dbo].[CharacterPhysicalMarks]([CharacterId],[Position]);
                """),

            ("CharacterTerritoryZones", $"""
                CREATE TABLE [dbo].[CharacterTerritoryZones] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Bucket] NVARCHAR(20) NOT NULL,
                    [Position] INT NOT NULL,
                    [Zone] NVARCHAR(450) NOT NULL,
                    {fkChar}
                );
                CREATE INDEX IX_CharacterTerritoryZones_BP ON [dbo].[CharacterTerritoryZones]([CharacterId],[Bucket],[Position]);
                CREATE INDEX IX_CharacterTerritoryZones_Zone ON [dbo].[CharacterTerritoryZones]([Zone]);
                """),

            ("CharacterTerritoryReputations", $"""
                CREATE TABLE [dbo].[CharacterTerritoryReputations] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Zone] NVARCHAR(450) NOT NULL,
                    [Reputation] NVARCHAR(MAX) NOT NULL,
                    {fkChar}
                );
                CREATE UNIQUE INDEX IX_CharacterTerritoryReputations_KV ON [dbo].[CharacterTerritoryReputations]([CharacterId],[Zone]);
                """),

            ("CharacterBelongingsGear", $"""
                CREATE TABLE [dbo].[CharacterBelongingsGear] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Bucket] NVARCHAR(40) NOT NULL,
                    [Position] INT NOT NULL,
                    [GearName] NVARCHAR(450) NOT NULL,
                    {fkChar}
                );
                CREATE INDEX IX_CharacterBelongingsGear_BP ON [dbo].[CharacterBelongingsGear]([CharacterId],[Bucket],[Position]);
                CREATE INDEX IX_CharacterBelongingsGear_Name ON [dbo].[CharacterBelongingsGear]([GearName]);
                """),

            ("CharacterBelongingsExtras", $"""
                CREATE TABLE [dbo].[CharacterBelongingsExtras] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [KeyName] NVARCHAR(200) NOT NULL,
                    [Value] NVARCHAR(MAX) NOT NULL,
                    {fkChar}
                );
                CREATE UNIQUE INDEX IX_CharacterBelongingsExtras_KV ON [dbo].[CharacterBelongingsExtras]([CharacterId],[KeyName]);
                """),

            ("CharacterBioBatteryThresholds", $"""
                CREATE TABLE [dbo].[CharacterBioBatteryThresholds] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Threshold] NVARCHAR(40) NOT NULL,
                    [Consequence] NVARCHAR(MAX) NOT NULL,
                    {fkChar}
                );
                CREATE UNIQUE INDEX IX_CharacterBioBatteryThresholds_KV ON [dbo].[CharacterBioBatteryThresholds]([CharacterId],[Threshold]);
                """),

            ("CharacterNeuralAbilities", $"""
                CREATE TABLE [dbo].[CharacterNeuralAbilities] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Position] INT NOT NULL,
                    [Name] NVARCHAR(200) NOT NULL,
                    [CostPercent] INT NOT NULL,
                    [Description] NVARCHAR(MAX) NOT NULL,
                    [OverdrawnRisk] NVARCHAR(MAX) NOT NULL,
                    [Passive] BIT NOT NULL,
                    {fkChar}
                );
                CREATE INDEX IX_CharacterNeuralAbilities_Pos ON [dbo].[CharacterNeuralAbilities]([CharacterId],[Position]);
                CREATE INDEX IX_CharacterNeuralAbilities_Name ON [dbo].[CharacterNeuralAbilities]([Name]);
                """),

            ("CharacterChangelog", $"""
                CREATE TABLE [dbo].[CharacterChangelog] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [Position] INT NOT NULL,
                    [StoryId] NVARCHAR(80) NOT NULL,
                    [Beat] NVARCHAR(80) NOT NULL,
                    [Date] NVARCHAR(MAX) NOT NULL,
                    [InWorldDate] DATETIME2 NULL,
                    [FieldName] NVARCHAR(200) NOT NULL,
                    [FromValue] NVARCHAR(MAX) NOT NULL,
                    [ToValue] NVARCHAR(MAX) NOT NULL,
                    [Reason] NVARCHAR(MAX) NOT NULL,
                    {fkChar}
                );
                CREATE INDEX IX_CharacterChangelog_Pos ON [dbo].[CharacterChangelog]([CharacterId],[Position]);
                CREATE INDEX IX_CharacterChangelog_Story ON [dbo].[CharacterChangelog]([StoryId]);
                """),

            ("CharacterKnowledgeEntities", $"""
                CREATE TABLE [dbo].[CharacterKnowledgeEntities] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [KnowledgeId] BIGINT NOT NULL,
                    [Position] INT NOT NULL,
                    [EntityRef] NVARCHAR(80) NOT NULL,
                    FOREIGN KEY ([KnowledgeId]) REFERENCES [dbo].[CharacterKnowledge]([Id]) ON DELETE CASCADE
                );
                CREATE INDEX IX_CharacterKnowledgeEntities_Pos ON [dbo].[CharacterKnowledgeEntities]([KnowledgeId],[Position]);
                CREATE INDEX IX_CharacterKnowledgeEntities_Ref ON [dbo].[CharacterKnowledgeEntities]([EntityRef]);
                """),

            ("CharacterTimelineBodyChanges", $"""
                CREATE TABLE [dbo].[CharacterTimelineBodyChanges] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [TimelineEventId] BIGINT NOT NULL,
                    [Position] INT NOT NULL,
                    [BodyChange] NVARCHAR(MAX) NOT NULL,
                    FOREIGN KEY ([TimelineEventId]) REFERENCES [dbo].[CharacterTimeline]([Id]) ON DELETE CASCADE
                );
                CREATE INDEX IX_CharacterTimelineBodyChanges_Pos ON [dbo].[CharacterTimelineBodyChanges]([TimelineEventId],[Position]);
                """),

            // Resolved-entity bridges. Alias is required (always populated from
            // the source string); the FK is nullable so unresolved references
            // don't block import. INNER JOIN on PlaceId / FactionId returns the
            // characters whose home/affiliation has a canonical entity record;
            // LEFT JOIN gives the full set with the alias as fallback.
            ("CharacterHomeTurfs", """
                CREATE TABLE [dbo].[CharacterHomeTurfs] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [PlaceId] UNIQUEIDENTIFIER NULL,
                    [Alias] NVARCHAR(450) NOT NULL,
                    [Position] INT NOT NULL,
                    FOREIGN KEY ([CharacterId]) REFERENCES [dbo].[Characters]([Id]) ON DELETE CASCADE,
                    FOREIGN KEY ([PlaceId]) REFERENCES [dbo].[Entities]([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX IX_CharacterHomeTurfs_Pos   ON [dbo].[CharacterHomeTurfs]([CharacterId],[Position]);
                CREATE INDEX IX_CharacterHomeTurfs_Place ON [dbo].[CharacterHomeTurfs]([PlaceId]);
                CREATE INDEX IX_CharacterHomeTurfs_Alias ON [dbo].[CharacterHomeTurfs]([Alias]);
                """),

            ("CharacterAffiliations", """
                CREATE TABLE [dbo].[CharacterAffiliations] (
                    [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [CharacterId] UNIQUEIDENTIFIER NOT NULL,
                    [FactionId] UNIQUEIDENTIFIER NULL,
                    [Alias] NVARCHAR(450) NOT NULL,
                    [Position] INT NOT NULL,
                    FOREIGN KEY ([CharacterId]) REFERENCES [dbo].[Characters]([Id]) ON DELETE CASCADE,
                    FOREIGN KEY ([FactionId]) REFERENCES [dbo].[Entities]([Id]) ON DELETE NO ACTION
                );
                CREATE INDEX IX_CharacterAffiliations_Pos     ON [dbo].[CharacterAffiliations]([CharacterId],[Position]);
                CREATE INDEX IX_CharacterAffiliations_Faction ON [dbo].[CharacterAffiliations]([FactionId]);
                CREATE INDEX IX_CharacterAffiliations_Alias   ON [dbo].[CharacterAffiliations]([Alias]);
                """),
        };
    }
}
