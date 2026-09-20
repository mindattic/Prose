-- add_universe_20260615.sql
-- ───────────────────────────────────────────────────────────────────────────
-- Multi-Universe foundation (SS-A2 / SS-LAW-15). Introduces a Universe lookup
-- table and a single UniverseId column on each universe-scoped root
-- (Entities, Strands, Books). 1:M — every row belongs to exactly one Universe;
-- a crossover entity is DUPLICATED (one row per Universe), never shared.
--
-- No enforced FOREIGN KEY: universe integrity is maintained by the app
-- (ProseDbContext stamps UniverseId on insert). Skipping the hard FK
-- keeps EnsureCreated-based test DBs and clean rebuilds from tripping on an
-- unseeded Universe table, and is safe for this single-writer tool.
--
-- Adding a column to a system-versioned (temporal) table requires the
-- SYSTEM_VERSIONING OFF -> ALTER table + _History -> ON dance. NOT NULL DEFAULT
-- <GLMZ> backfills every existing row to Universe #1 (GLMZ) automatically.
--
-- PARTIAL-STATE SAFE + idempotent: every step is its own GO batch guarded by a
-- catalog check (column existence / temporal_type), so a re-run recovers from
-- any half-applied state. Run under QUOTED_IDENTIFIER ON (EF default; sqlcmd -I)
-- because the temporal roots carry filtered indexes.
--
-- Well-known UUIDv7 ids: GLMZ = 0197e9c9-0001-7000-8000-000000000001,
-- Fantasy = 0197e9c9-0002-7000-8000-000000000002 (match Universe.GlmzId / FantasyId).
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. Universe lookup table (non-temporal classification table).
IF OBJECT_ID('dbo.Universe', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Universe (
        Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Universe PRIMARY KEY,
        Slug        NVARCHAR(200)    NOT NULL,
        Name        NVARCHAR(400)    NOT NULL,
        Description  NVARCHAR(MAX)   NULL,
        Theme        NVARCHAR(100)   NULL,
        UniversePrimer  NVARCHAR(MAX) NULL,
        IsActive     BIT             NOT NULL CONSTRAINT DF_Universe_IsActive  DEFAULT 1,
        SortKey      FLOAT           NOT NULL CONSTRAINT DF_Universe_SortKey   DEFAULT 100,
        CreatedAt    DATETIME2       NOT NULL CONSTRAINT DF_Universe_CreatedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX UX_Universe_Slug ON dbo.Universe(Slug);
END;
GO

-- 2. Seed the two universes.
IF NOT EXISTS (SELECT 1 FROM dbo.Universe WHERE Id = '0197e9c9-0001-7000-8000-000000000001')
    INSERT INTO dbo.Universe (Id, Slug, Name, Description, Theme, SortKey)
    VALUES ('0197e9c9-0001-7000-8000-000000000001', 'glmz', 'GLMZ',
            'Greater Lake Michigan Zone, 2225 — the flagship cyberpunk universe (the Bushido Coda).',
            'cyberpunk', 100);
GO
IF NOT EXISTS (SELECT 1 FROM dbo.Universe WHERE Id = '0197e9c9-0002-7000-8000-000000000002')
    INSERT INTO dbo.Universe (Id, Slug, Name, Description, Theme, SortKey)
    VALUES ('0197e9c9-0002-7000-8000-000000000002', 'scry', 'SCRY',
            'Scry — the Cauld (medieval fantasy world; The Vigil''s End and other Cauld stories).',
            'steampunk', 200);
GO

-- ── 3a. Entities.UniverseId ────────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Entities' AND temporal_type = 2)
    ALTER TABLE dbo.Entities SET (SYSTEM_VERSIONING = OFF);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Entities') AND name = 'UniverseId')
    ALTER TABLE dbo.Entities ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_Entities_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Entities_History') AND name = 'UniverseId')
    ALTER TABLE dbo.Entities_History ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_EntitiesH_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Entities' AND temporal_type = 2)
    ALTER TABLE dbo.Entities SET (SYSTEM_VERSIONING = ON
        (HISTORY_TABLE = dbo.Entities_History, DATA_CONSISTENCY_CHECK = OFF));
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Entities_UniverseId' AND object_id = OBJECT_ID('dbo.Entities'))
    CREATE INDEX IX_Entities_UniverseId ON dbo.Entities(UniverseId);
GO

-- ── 3b. Strands.UniverseId ─────────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Strands' AND temporal_type = 2)
    ALTER TABLE dbo.Strands SET (SYSTEM_VERSIONING = OFF);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Strands') AND name = 'UniverseId')
    ALTER TABLE dbo.Strands ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_Strands_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Strands_History') AND name = 'UniverseId')
    ALTER TABLE dbo.Strands_History ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_StrandsH_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Strands' AND temporal_type = 2)
    ALTER TABLE dbo.Strands SET (SYSTEM_VERSIONING = ON
        (HISTORY_TABLE = dbo.Strands_History, DATA_CONSISTENCY_CHECK = OFF));
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Strands_UniverseId' AND object_id = OBJECT_ID('dbo.Strands'))
    CREATE INDEX IX_Strands_UniverseId ON dbo.Strands(UniverseId);
GO

-- ── 3c. Books.UniverseId (legacy story root; scoped for consistency) ───────
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Books' AND temporal_type = 2)
    ALTER TABLE dbo.Books SET (SYSTEM_VERSIONING = OFF);
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Books') AND name = 'UniverseId')
    ALTER TABLE dbo.Books ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_Books_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Books_History') AND name = 'UniverseId')
    ALTER TABLE dbo.Books_History ADD UniverseId UNIQUEIDENTIFIER NOT NULL
        CONSTRAINT DF_BooksH_UniverseId DEFAULT '0197e9c9-0001-7000-8000-000000000001';
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Books' AND temporal_type = 2)
    ALTER TABLE dbo.Books SET (SYSTEM_VERSIONING = ON
        (HISTORY_TABLE = dbo.Books_History, DATA_CONSISTENCY_CHECK = OFF));
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Books_UniverseId' AND object_id = OBJECT_ID('dbo.Books'))
    CREATE INDEX IX_Books_UniverseId ON dbo.Books(UniverseId);
GO

-- ── 4. Per-universe slug uniqueness ─────────────────────────────────────────
-- A place "Silence" or a strand "the-regular" may exist in more than one
-- universe, so the global unique slug indexes are rebuilt to include UniverseId.
-- (Regular index ops on a temporal table need no SYSTEM_VERSIONING dance.)

-- Entities: (EntityType, Slug) -> (UniverseId, EntityType, Slug)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Entities_EntityType_Slug' AND object_id = OBJECT_ID('dbo.Entities'))
    DROP INDEX IX_Entities_EntityType_Slug ON dbo.Entities;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Entities_Universe_Type_Slug' AND object_id = OBJECT_ID('dbo.Entities'))
    CREATE UNIQUE INDEX UX_Entities_Universe_Type_Slug ON dbo.Entities(UniverseId, EntityType, Slug);
GO

-- Strands: (Slug) -> (UniverseId, Slug)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Strands_Slug' AND object_id = OBJECT_ID('dbo.Strands'))
    DROP INDEX IX_Strands_Slug ON dbo.Strands;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Strands_Universe_Slug' AND object_id = OBJECT_ID('dbo.Strands'))
    CREATE UNIQUE INDEX UX_Strands_Universe_Slug ON dbo.Strands(UniverseId, Slug);
GO

-- Books: (Slug) -> (UniverseId, Slug)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Books_Slug' AND object_id = OBJECT_ID('dbo.Books'))
    DROP INDEX IX_Books_Slug ON dbo.Books;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Books_Universe_Slug' AND object_id = OBJECT_ID('dbo.Books'))
    CREATE UNIQUE INDEX UX_Books_Universe_Slug ON dbo.Books(UniverseId, Slug);
GO
