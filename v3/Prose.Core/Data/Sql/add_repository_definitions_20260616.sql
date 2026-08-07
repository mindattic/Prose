-- add_repository_definitions_20260616.sql
-- ───────────────────────────────────────────────────────────────────────────
-- Runtime-defined entity types. A RepositoryDefinition row declares a new
-- entity category that stores its data in the generic Entity spine
-- (EntityType string + EntityProperties flex bag + Edges). No typed table is
-- created — the slug becomes the EntityType discriminator for Entities rows.
--
-- Idempotent: guarded by IF OBJECT_ID / IF NOT EXISTS.
-- Non-temporal: this is a lookup table (rare writes, no history needed).
-- Repository definitions are GLOBAL (not universe-scoped); universe
-- filtering happens at the Entities level (UniverseId column).
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID('dbo.RepositoryDefinitions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepositoryDefinitions (
        Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RepositoryDefinitions PRIMARY KEY,
        Slug        NVARCHAR(120)    NOT NULL,   -- EntityType discriminator, e.g. 'artifact'
        Name        NVARCHAR(200)    NOT NULL,   -- display name, e.g. 'Artifacts'
        Category    NVARCHAR(50)     NOT NULL    -- board section: Characters/Organizations/Gear/World/Culture
                        CONSTRAINT DF_RepoDef_Category DEFAULT 'World',
        Icon        NVARCHAR(60)     NOT NULL    -- bootstrap-icon class, e.g. 'bi-box'
                        CONSTRAINT DF_RepoDef_Icon DEFAULT 'bi-box',
        Description NVARCHAR(MAX)    NULL,
        RoutePath   NVARCHAR(120)    NOT NULL,   -- e.g. '/repo/artifact'
        CreatedAt   DATETIME2        NOT NULL    CONSTRAINT DF_RepoDef_CreatedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX UX_RepositoryDefinitions_Slug ON dbo.RepositoryDefinitions(Slug);
END;
GO
