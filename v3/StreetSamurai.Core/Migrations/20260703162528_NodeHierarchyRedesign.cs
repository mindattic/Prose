using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreetSamurai.Core.Migrations
{
    /// <summary>
    /// Strand → Node hierarchy redesign. The single "Strand" abstraction becomes a
    /// typed tree: SeriesNode / StoryNode / ChapterNode, table-per-hierarchy on the
    /// renamed Nodes table with a NodeType discriminator. Everything is a RENAME —
    /// no data moves, no rows are dropped.
    ///
    /// Hand-written (the scaffolded auto-diff wanted drop/create). The rename passes
    /// are dynamic — every table / column / index / constraint whose name contains
    /// "Strand" is renamed via Strands→Nodes then Strand→Node — so local/prod schema
    /// drift can't leave stragglers. Four of the affected tables (Strands, StrandBeats,
    /// StrandAmendments, StrandSpineVersions) are system-versioned; versioning is
    /// suspended around the renames and re-pointed at the renamed history tables.
    ///
    /// NodeType backfill (current + history rows): Kind 'series'→series,
    /// 'chapter'→chapter, everything else ('story','book','novel','novella',
    /// 'episode','strand',…) → story.
    /// </summary>
    public partial class NodeHierarchyRedesign : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Drop the two views that reference Strand tables ──────────
            migrationBuilder.Sql("""
                IF OBJECT_ID('dbo.StrandEntityAppearances', 'V') IS NOT NULL DROP VIEW dbo.StrandEntityAppearances;
                IF OBJECT_ID('dbo.StrandEntityMentions',    'V') IS NOT NULL DROP VIEW dbo.StrandEntityMentions;
                """);

            // ── 2. Suspend system versioning on the temporal tables ─────────
            migrationBuilder.Sql("""
                DECLARE @t sysname;
                DECLARE c CURSOR LOCAL FAST_FORWARD FOR
                    SELECT name FROM sys.tables
                    WHERE temporal_type = 2
                      AND name IN ('Strands', 'StrandBeats', 'StrandAmendments', 'StrandSpineVersions');
                OPEN c; FETCH NEXT FROM c INTO @t;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    EXEC('ALTER TABLE dbo.[' + @t + '] SET (SYSTEM_VERSIONING = OFF)');
                    FETCH NEXT FROM c INTO @t;
                END
                CLOSE c; DEALLOCATE c;
                """);

            // ── 2b. Drop filtered indexes that pin Strand column names ──────
            // A filtered index's WHERE clause blocks sp_rename on the columns
            // it references. Drop them here; recreated on the renamed schema
            // in step 5b.
            migrationBuilder.Sql("""
                SET NOCOUNT ON;
                DECLARE @tbl sysname, @idx sysname, @sql nvarchar(max);
                DECLARE fidx CURSOR LOCAL FAST_FORWARD FOR
                    SELECT t.name, i.name FROM sys.indexes i
                    JOIN sys.tables t ON i.object_id = t.object_id
                    WHERE i.filter_definition LIKE '%Strand%';
                OPEN fidx; FETCH NEXT FROM fidx INTO @tbl, @idx;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @sql = 'DROP INDEX ' + QUOTENAME(@idx) + ' ON dbo.' + QUOTENAME(@tbl);
                    EXEC(@sql);
                    FETCH NEXT FROM fidx INTO @tbl, @idx;
                END
                CLOSE fidx; DEALLOCATE fidx;
                """);

            // ── 3a. Rename constraints (FK / PK / UNIQUE / DEFAULT / CHECK) ─
            migrationBuilder.Sql("""
                SET NOCOUNT ON;
                DECLARE @old sysname, @qual nvarchar(776), @new sysname;
                DECLARE cons CURSOR LOCAL FAST_FORWARD FOR
                    SELECT name FROM sys.objects
                    WHERE type IN ('F','PK','UQ','D','C') AND name LIKE '%Strand%';
                OPEN cons; FETCH NEXT FROM cons INTO @old;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @qual = 'dbo.' + QUOTENAME(@old);
                    SET @new  = REPLACE(@old COLLATE Latin1_General_CS_AS, 'Strand', 'Node');
                    EXEC sp_rename @qual, @new, 'OBJECT';
                    FETCH NEXT FROM cons INTO @old;
                END
                CLOSE cons; DEALLOCATE cons;
                """);

            // ── 3b. Rename columns (all tables, incl. history + satellites) ─
            migrationBuilder.Sql("""
                SET NOCOUNT ON;
                DECLARE @tbl sysname, @col sysname, @qual nvarchar(776), @new sysname;
                DECLARE cols CURSOR LOCAL FAST_FORWARD FOR
                    SELECT t.name, c.name FROM sys.columns c
                    JOIN sys.tables t ON c.object_id = t.object_id
                    WHERE c.name LIKE '%Strand%';
                OPEN cols; FETCH NEXT FROM cols INTO @tbl, @col;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @qual = 'dbo.' + QUOTENAME(@tbl) + '.' + QUOTENAME(@col);
                    SET @new  = REPLACE(@col COLLATE Latin1_General_CS_AS, 'Strand', 'Node');
                    EXEC sp_rename @qual, @new, 'COLUMN';
                    FETCH NEXT FROM cols INTO @tbl, @col;
                END
                CLOSE cols; DEALLOCATE cols;
                """);

            // ── 3c. Rename indexes ───────────────────────────────────────────
            migrationBuilder.Sql("""
                SET NOCOUNT ON;
                DECLARE @tbl sysname, @idx sysname, @qual nvarchar(776), @new sysname;
                DECLARE idxs CURSOR LOCAL FAST_FORWARD FOR
                    SELECT t.name, i.name FROM sys.indexes i
                    JOIN sys.tables t ON i.object_id = t.object_id
                    WHERE i.name LIKE '%Strand%';
                OPEN idxs; FETCH NEXT FROM idxs INTO @tbl, @idx;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @qual = 'dbo.' + QUOTENAME(@tbl) + '.' + QUOTENAME(@idx);
                    SET @new  = REPLACE(@idx COLLATE Latin1_General_CS_AS, 'Strand', 'Node');
                    EXEC sp_rename @qual, @new, 'INDEX';
                    FETCH NEXT FROM idxs INTO @tbl, @idx;
                END
                CLOSE idxs; DEALLOCATE idxs;
                """);

            // ── 3d. Rename tables (last, so 3a-3c address current names) ────
            migrationBuilder.Sql("""
                SET NOCOUNT ON;
                DECLARE @tbl sysname, @qual nvarchar(776), @new sysname;
                DECLARE tbls CURSOR LOCAL FAST_FORWARD FOR
                    SELECT name FROM sys.tables WHERE name LIKE '%Strand%';
                OPEN tbls; FETCH NEXT FROM tbls INTO @tbl;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @qual = 'dbo.' + QUOTENAME(@tbl);
                    SET @new  = REPLACE(@tbl COLLATE Latin1_General_CS_AS, 'Strand', 'Node');
                    EXEC sp_rename @qual, @new;
                    FETCH NEXT FROM tbls INTO @tbl;
                END
                CLOSE tbls; DEALLOCATE tbls;
                """);

            // ── 4. NodeType discriminator (current + history), backfilled ───
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Nodes', 'NodeType') IS NULL
                    ALTER TABLE dbo.Nodes ADD NodeType nvarchar(20) NULL;
                IF OBJECT_ID('dbo.Nodes_History', 'U') IS NOT NULL AND COL_LENGTH('dbo.Nodes_History', 'NodeType') IS NULL
                    ALTER TABLE dbo.Nodes_History ADD NodeType nvarchar(20) NULL;
                """);
            migrationBuilder.Sql("""
                UPDATE dbo.Nodes SET NodeType =
                    CASE WHEN Kind = 'series' THEN 'series'
                         WHEN Kind = 'chapter' THEN 'chapter'
                         ELSE 'story' END
                WHERE NodeType IS NULL;
                IF OBJECT_ID('dbo.Nodes_History', 'U') IS NOT NULL
                    EXEC('UPDATE dbo.Nodes_History SET NodeType =
                            CASE WHEN Kind = ''series'' THEN ''series''
                                 WHEN Kind = ''chapter'' THEN ''chapter''
                                 ELSE ''story'' END
                          WHERE NodeType IS NULL');
                """);
            migrationBuilder.Sql("""
                ALTER TABLE dbo.Nodes ALTER COLUMN NodeType nvarchar(20) NOT NULL;
                IF OBJECT_ID('dbo.Nodes_History', 'U') IS NOT NULL
                    EXEC('ALTER TABLE dbo.Nodes_History ALTER COLUMN NodeType nvarchar(20) NOT NULL');
                """);

            // ── 5b. Recreate the filtered indexes on the renamed schema ─────
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Nodes_ParentNodeId_SortKey' AND object_id = OBJECT_ID('dbo.Nodes'))
                    CREATE INDEX IX_Nodes_ParentNodeId_SortKey ON dbo.Nodes (ParentNodeId, SortKey) WHERE ParentNodeId IS NOT NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Nodes_NodeCode' AND object_id = OBJECT_ID('dbo.Nodes'))
                    CREATE UNIQUE INDEX IX_Nodes_NodeCode ON dbo.Nodes (NodeCode) WHERE NodeCode IS NOT NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Nodes_PreviousNodeId' AND object_id = OBJECT_ID('dbo.Nodes'))
                    CREATE INDEX IX_Nodes_PreviousNodeId ON dbo.Nodes (PreviousNodeId) WHERE PreviousNodeId IS NOT NULL;
                """);

            // ── 5. Re-enable system versioning on the renamed tables ────────
            migrationBuilder.Sql("""
                DECLARE @t sysname;
                DECLARE c CURSOR LOCAL FAST_FORWARD FOR
                    SELECT t.name FROM sys.tables t
                    WHERE t.temporal_type = 0
                      AND t.name IN ('Nodes', 'NodeBeats', 'NodeAmendments', 'NodeSpineVersions')
                      AND OBJECT_ID('dbo.' + t.name + '_History', 'U') IS NOT NULL;
                OPEN c; FETCH NEXT FROM c INTO @t;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    EXEC('ALTER TABLE dbo.[' + @t + '] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.[' + @t + '_History]))');
                    FETCH NEXT FROM c INTO @t;
                END
                CLOSE c; DEALLOCATE c;
                """);

            // ── 6. Recreate the views against the renamed schema ────────────
            migrationBuilder.Sql("""
                CREATE VIEW dbo.NodeEntityAppearances AS
                SELECT  m.EntityId,
                        e.Name        AS EntityName,
                        e.EntityType,
                        s.UniverseId,
                        sb.NodeId,
                        s.Slug        AS NodeSlug,
                        s.NodeCode,
                        COUNT(*)      AS MentionCount
                FROM    BeatEntityMentions m
                JOIN    NodeBeats sb ON sb.BeatId = m.BeatId
                JOIN    Nodes     s  ON s.Id      = sb.NodeId
                JOIN    Entities  e  ON e.Id      = m.EntityId AND e.UniverseId = s.UniverseId
                GROUP BY m.EntityId, e.Name, e.EntityType, s.UniverseId, sb.NodeId, s.Slug, s.NodeCode
                """);
            migrationBuilder.Sql("""
                CREATE VIEW dbo.NodeEntityMentions AS
                SELECT DISTINCT sb.NodeId, bem.EntityId, bem.EntityName, bem.EntityType
                FROM NodeBeats sb
                JOIN BeatEntityMentions bem ON bem.BeatId = sb.BeatId
                WHERE sb.IsEnabled = 1
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Mirror image of Up(): drop views, suspend versioning, reverse the
            // renames (drop NodeType first so the reverse pass never sees it),
            // re-enable versioning, recreate the original views. The reverse
            // rename pattern %Node% is broader than what Up() touched, but at
            // Down()-time those are exactly the objects Up() produced.
            migrationBuilder.Sql("""
                IF OBJECT_ID('dbo.NodeEntityAppearances', 'V') IS NOT NULL DROP VIEW dbo.NodeEntityAppearances;
                IF OBJECT_ID('dbo.NodeEntityMentions',    'V') IS NOT NULL DROP VIEW dbo.NodeEntityMentions;
                """);
            migrationBuilder.Sql("""
                DECLARE @t sysname;
                DECLARE c CURSOR LOCAL FAST_FORWARD FOR
                    SELECT name FROM sys.tables
                    WHERE temporal_type = 2
                      AND name IN ('Nodes', 'NodeBeats', 'NodeAmendments', 'NodeSpineVersions');
                OPEN c; FETCH NEXT FROM c INTO @t;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    EXEC('ALTER TABLE dbo.[' + @t + '] SET (SYSTEM_VERSIONING = OFF)');
                    FETCH NEXT FROM c INTO @t;
                END
                CLOSE c; DEALLOCATE c;
                """);
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.Nodes', 'NodeType') IS NOT NULL
                    ALTER TABLE dbo.Nodes DROP COLUMN NodeType;
                IF OBJECT_ID('dbo.Nodes_History', 'U') IS NOT NULL AND COL_LENGTH('dbo.Nodes_History', 'NodeType') IS NOT NULL
                    ALTER TABLE dbo.Nodes_History DROP COLUMN NodeType;
                """);
            migrationBuilder.Sql("""
                SET NOCOUNT ON;
                DECLARE @tbl sysname, @idx sysname, @sql nvarchar(max);
                DECLARE fidx CURSOR LOCAL FAST_FORWARD FOR
                    SELECT t.name, i.name FROM sys.indexes i
                    JOIN sys.tables t ON i.object_id = t.object_id
                    WHERE i.filter_definition LIKE '%Node%';
                OPEN fidx; FETCH NEXT FROM fidx INTO @tbl, @idx;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @sql = 'DROP INDEX ' + QUOTENAME(@idx) + ' ON dbo.' + QUOTENAME(@tbl);
                    EXEC(@sql);
                    FETCH NEXT FROM fidx INTO @tbl, @idx;
                END
                CLOSE fidx; DEALLOCATE fidx;
                """);
            migrationBuilder.Sql("""
                SET NOCOUNT ON;
                DECLARE @old sysname, @qual nvarchar(776), @new sysname;
                DECLARE cons CURSOR LOCAL FAST_FORWARD FOR
                    SELECT name FROM sys.objects
                    WHERE type IN ('F','PK','UQ','D','C') AND name LIKE '%Node%';
                OPEN cons; FETCH NEXT FROM cons INTO @old;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @qual = 'dbo.' + QUOTENAME(@old);
                    SET @new  = REPLACE(@old COLLATE Latin1_General_CS_AS, 'Node', 'Strand');
                    EXEC sp_rename @qual, @new, 'OBJECT';
                    FETCH NEXT FROM cons INTO @old;
                END
                CLOSE cons; DEALLOCATE cons;
                """);
            migrationBuilder.Sql("""
                SET NOCOUNT ON;
                DECLARE @tbl sysname, @col sysname, @qual nvarchar(776), @new sysname;
                DECLARE cols CURSOR LOCAL FAST_FORWARD FOR
                    SELECT t.name, c.name FROM sys.columns c
                    JOIN sys.tables t ON c.object_id = t.object_id
                    WHERE c.name LIKE '%Node%';
                OPEN cols; FETCH NEXT FROM cols INTO @tbl, @col;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @qual = 'dbo.' + QUOTENAME(@tbl) + '.' + QUOTENAME(@col);
                    SET @new  = REPLACE(@col COLLATE Latin1_General_CS_AS, 'Node', 'Strand');
                    EXEC sp_rename @qual, @new, 'COLUMN';
                    FETCH NEXT FROM cols INTO @tbl, @col;
                END
                CLOSE cols; DEALLOCATE cols;
                """);
            migrationBuilder.Sql("""
                SET NOCOUNT ON;
                DECLARE @tbl sysname, @idx sysname, @qual nvarchar(776), @new sysname;
                DECLARE idxs CURSOR LOCAL FAST_FORWARD FOR
                    SELECT t.name, i.name FROM sys.indexes i
                    JOIN sys.tables t ON i.object_id = t.object_id
                    WHERE i.name LIKE '%Node%';
                OPEN idxs; FETCH NEXT FROM idxs INTO @tbl, @idx;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @qual = 'dbo.' + QUOTENAME(@tbl) + '.' + QUOTENAME(@idx);
                    SET @new  = REPLACE(@idx COLLATE Latin1_General_CS_AS, 'Node', 'Strand');
                    EXEC sp_rename @qual, @new, 'INDEX';
                    FETCH NEXT FROM idxs INTO @tbl, @idx;
                END
                CLOSE idxs; DEALLOCATE idxs;
                """);
            migrationBuilder.Sql("""
                SET NOCOUNT ON;
                DECLARE @tbl sysname, @qual nvarchar(776), @new sysname;
                DECLARE tbls CURSOR LOCAL FAST_FORWARD FOR
                    SELECT name FROM sys.tables WHERE name LIKE '%Node%';
                OPEN tbls; FETCH NEXT FROM tbls INTO @tbl;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @qual = 'dbo.' + QUOTENAME(@tbl);
                    SET @new  = REPLACE(@tbl COLLATE Latin1_General_CS_AS, 'Node', 'Strand');
                    EXEC sp_rename @qual, @new;
                    FETCH NEXT FROM tbls INTO @tbl;
                END
                CLOSE tbls; DEALLOCATE tbls;
                """);
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Strands_ParentStrandId_SortKey' AND object_id = OBJECT_ID('dbo.Strands'))
                    CREATE INDEX IX_Strands_ParentStrandId_SortKey ON dbo.Strands (ParentStrandId, SortKey) WHERE ParentStrandId IS NOT NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Strands_StrandCode' AND object_id = OBJECT_ID('dbo.Strands'))
                    CREATE UNIQUE INDEX IX_Strands_StrandCode ON dbo.Strands (StrandCode) WHERE StrandCode IS NOT NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Strands_PreviousStrandId' AND object_id = OBJECT_ID('dbo.Strands'))
                    CREATE INDEX IX_Strands_PreviousStrandId ON dbo.Strands (PreviousStrandId) WHERE PreviousStrandId IS NOT NULL;
                """);
            migrationBuilder.Sql("""
                DECLARE @t sysname;
                DECLARE c CURSOR LOCAL FAST_FORWARD FOR
                    SELECT t.name FROM sys.tables t
                    WHERE t.temporal_type = 0
                      AND t.name IN ('Strands', 'StrandBeats', 'StrandAmendments', 'StrandSpineVersions')
                      AND OBJECT_ID('dbo.' + t.name + '_History', 'U') IS NOT NULL;
                OPEN c; FETCH NEXT FROM c INTO @t;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    EXEC('ALTER TABLE dbo.[' + @t + '] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.[' + @t + '_History]))');
                    FETCH NEXT FROM c INTO @t;
                END
                CLOSE c; DEALLOCATE c;
                """);
            migrationBuilder.Sql("""
                CREATE VIEW dbo.StrandEntityAppearances AS
                SELECT  m.EntityId,
                        e.Name        AS EntityName,
                        e.EntityType,
                        s.UniverseId,
                        sb.StrandId,
                        s.Slug        AS StrandSlug,
                        s.StrandCode,
                        COUNT(*)      AS MentionCount
                FROM    BeatEntityMentions m
                JOIN    StrandBeats sb ON sb.BeatId = m.BeatId
                JOIN    Strands     s  ON s.Id      = sb.StrandId
                JOIN    Entities    e  ON e.Id      = m.EntityId AND e.UniverseId = s.UniverseId
                GROUP BY m.EntityId, e.Name, e.EntityType, s.UniverseId, sb.StrandId, s.Slug, s.StrandCode
                """);
            migrationBuilder.Sql("""
                CREATE VIEW dbo.StrandEntityMentions AS
                SELECT DISTINCT sb.StrandId, bem.EntityId, bem.EntityName, bem.EntityType
                FROM StrandBeats sb
                JOIN BeatEntityMentions bem ON bem.BeatId = sb.BeatId
                WHERE sb.IsEnabled = 1
                """);
        }
    }
}
