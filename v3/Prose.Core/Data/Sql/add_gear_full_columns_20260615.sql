-- add_gear_full_columns_20260615.sql
-- ───────────────────────────────────────────────────────────────────────────
-- RFC 0007: Make Genemod, Material, and Transportation fully lossless.
--
-- Adds missing scalar columns to the three entity tables (so blob-only fields
-- get relational homes) and creates three new bridge tables:
--   GenemodSideEffects  — GenemodData.SideEffects  (List<string>)
--   MaterialProperties  — MaterialData.Properties  (List<string>)
--   MaterialDevelopers  — MaterialData.Developers  (List<string>)
--   MaterialApplications— MaterialData.Applications(List<string>)
--
-- Pattern: SYSTEM_VERSIONING OFF → ALTER table + _History → ON
-- (see add_universe_20260615.sql for the canonical pattern).
--
-- Idempotent + partial-state safe: every step guarded by catalog checks.
-- Run under QUOTED_IDENTIFIER ON (sqlcmd -I).
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ════════════════════════════════════════════════════════════════════════════
-- 1. GENEMODS — add 10 new scalar columns
-- ════════════════════════════════════════════════════════════════════════════

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Genemods' AND temporal_type = 2)
    ALTER TABLE dbo.Genemods SET (SYSTEM_VERSIONING = OFF);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods') AND name = 'BrandName')
    ALTER TABLE dbo.Genemods ADD BrandName NVARCHAR(450) NOT NULL CONSTRAINT DF_Genemods_BrandName DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods_History') AND name = 'BrandName')
    ALTER TABLE dbo.Genemods_History ADD BrandName NVARCHAR(450) NOT NULL CONSTRAINT DF_GenemodH_BrandName DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods') AND name = 'ProductName')
    ALTER TABLE dbo.Genemods ADD ProductName NVARCHAR(450) NOT NULL CONSTRAINT DF_Genemods_ProductName DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods_History') AND name = 'ProductName')
    ALTER TABLE dbo.Genemods_History ADD ProductName NVARCHAR(450) NOT NULL CONSTRAINT DF_GenemodH_ProductName DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods') AND name = 'TargetSystem')
    ALTER TABLE dbo.Genemods ADD TargetSystem NVARCHAR(450) NOT NULL CONSTRAINT DF_Genemods_TargetSystem DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods_History') AND name = 'TargetSystem')
    ALTER TABLE dbo.Genemods_History ADD TargetSystem NVARCHAR(450) NOT NULL CONSTRAINT DF_GenemodH_TargetSystem DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods') AND name = 'SourceOrganism')
    ALTER TABLE dbo.Genemods ADD SourceOrganism NVARCHAR(450) NOT NULL CONSTRAINT DF_Genemods_SourceOrganism DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods_History') AND name = 'SourceOrganism')
    ALTER TABLE dbo.Genemods_History ADD SourceOrganism NVARCHAR(450) NOT NULL CONSTRAINT DF_GenemodH_SourceOrganism DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods') AND name = 'Legality')
    ALTER TABLE dbo.Genemods ADD Legality NVARCHAR(200) NOT NULL CONSTRAINT DF_Genemods_Legality DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods_History') AND name = 'Legality')
    ALTER TABLE dbo.Genemods_History ADD Legality NVARCHAR(200) NOT NULL CONSTRAINT DF_GenemodH_Legality DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods') AND name = 'Procedure')
    ALTER TABLE dbo.Genemods ADD [Procedure] NVARCHAR(1000) NOT NULL CONSTRAINT DF_Genemods_Procedure DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods_History') AND name = 'Procedure')
    ALTER TABLE dbo.Genemods_History ADD [Procedure] NVARCHAR(1000) NOT NULL CONSTRAINT DF_GenemodH_Procedure DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods') AND name = 'ExpressionTime')
    ALTER TABLE dbo.Genemods ADD ExpressionTime NVARCHAR(200) NOT NULL CONSTRAINT DF_Genemods_ExpressionTime DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods_History') AND name = 'ExpressionTime')
    ALTER TABLE dbo.Genemods_History ADD ExpressionTime NVARCHAR(200) NOT NULL CONSTRAINT DF_GenemodH_ExpressionTime DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods') AND name = 'Reversibility')
    ALTER TABLE dbo.Genemods ADD Reversibility NVARCHAR(200) NOT NULL CONSTRAINT DF_Genemods_Reversibility DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods_History') AND name = 'Reversibility')
    ALTER TABLE dbo.Genemods_History ADD Reversibility NVARCHAR(200) NOT NULL CONSTRAINT DF_GenemodH_Reversibility DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods') AND name = 'SocialPerception')
    ALTER TABLE dbo.Genemods ADD SocialPerception NVARCHAR(1000) NOT NULL CONSTRAINT DF_Genemods_SocialPerception DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods_History') AND name = 'SocialPerception')
    ALTER TABLE dbo.Genemods_History ADD SocialPerception NVARCHAR(1000) NOT NULL CONSTRAINT DF_GenemodH_SocialPerception DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods') AND name = 'TierAvailability')
    ALTER TABLE dbo.Genemods ADD TierAvailability NVARCHAR(450) NOT NULL CONSTRAINT DF_Genemods_TierAvailability DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Genemods_History') AND name = 'TierAvailability')
    ALTER TABLE dbo.Genemods_History ADD TierAvailability NVARCHAR(450) NOT NULL CONSTRAINT DF_GenemodH_TierAvailability DEFAULT '';
GO

-- Re-enable versioning on Genemods
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Genemods' AND temporal_type = 2)
    ALTER TABLE dbo.Genemods SET (SYSTEM_VERSIONING = ON
        (HISTORY_TABLE = dbo.Genemods_History, DATA_CONSISTENCY_CHECK = OFF));
GO

-- Index on TargetSystem (commonly queried for search/filter)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Genemods_TargetSystem' AND object_id = OBJECT_ID('dbo.Genemods'))
    CREATE INDEX IX_Genemods_TargetSystem ON dbo.Genemods(TargetSystem);
GO

-- ════════════════════════════════════════════════════════════════════════════
-- 2. GENEMOD SIDE EFFECTS bridge table (new temporal table)
-- ════════════════════════════════════════════════════════════════════════════

IF OBJECT_ID('dbo.GenemodSideEffects', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GenemodSideEffects (
        Id        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GenemodSideEffects PRIMARY KEY,
        GenemodId UNIQUEIDENTIFIER NOT NULL
                  CONSTRAINT FK_GenemodSideEffects_Genemods
                  REFERENCES dbo.Genemods(Id) ON DELETE CASCADE,
        Position  INT             NOT NULL,
        Effect    NVARCHAR(MAX)   NOT NULL CONSTRAINT DF_GenemodSideEffects_Effect DEFAULT '',
        ValidFrom   DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
                    CONSTRAINT DF_GenemodSideEffects_ValidFrom DEFAULT SYSUTCDATETIME(),
        ValidTo     DATETIME2 GENERATED ALWAYS AS ROW END   HIDDEN
                    CONSTRAINT DF_GenemodSideEffects_ValidTo   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
        PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
    )
    WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.GenemodSideEffects_History));

    CREATE INDEX IX_GenemodSideEffects_GenemodId_Position ON dbo.GenemodSideEffects(GenemodId, Position);
END;
GO

-- ════════════════════════════════════════════════════════════════════════════
-- 3. MATERIALS — add 4 new scalar columns
-- ════════════════════════════════════════════════════════════════════════════

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Materials' AND temporal_type = 2)
    ALTER TABLE dbo.Materials SET (SYSTEM_VERSIONING = OFF);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Materials') AND name = 'BrandName')
    ALTER TABLE dbo.Materials ADD BrandName NVARCHAR(450) NOT NULL CONSTRAINT DF_Materials_BrandName DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Materials_History') AND name = 'BrandName')
    ALTER TABLE dbo.Materials_History ADD BrandName NVARCHAR(450) NOT NULL CONSTRAINT DF_MaterialsH_BrandName DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Materials') AND name = 'ProductName')
    ALTER TABLE dbo.Materials ADD ProductName NVARCHAR(450) NOT NULL CONSTRAINT DF_Materials_ProductName DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Materials_History') AND name = 'ProductName')
    ALTER TABLE dbo.Materials_History ADD ProductName NVARCHAR(450) NOT NULL CONSTRAINT DF_MaterialsH_ProductName DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Materials') AND name = 'TierAvailability')
    ALTER TABLE dbo.Materials ADD TierAvailability NVARCHAR(450) NOT NULL CONSTRAINT DF_Materials_TierAvailability DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Materials_History') AND name = 'TierAvailability')
    ALTER TABLE dbo.Materials_History ADD TierAvailability NVARCHAR(450) NOT NULL CONSTRAINT DF_MaterialsH_TierAvailability DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Materials') AND name = 'Cost')
    ALTER TABLE dbo.Materials ADD Cost NVARCHAR(200) NOT NULL CONSTRAINT DF_Materials_Cost DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Materials_History') AND name = 'Cost')
    ALTER TABLE dbo.Materials_History ADD Cost NVARCHAR(200) NOT NULL CONSTRAINT DF_MaterialsH_Cost DEFAULT '';
GO

-- Re-enable versioning on Materials
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Materials' AND temporal_type = 2)
    ALTER TABLE dbo.Materials SET (SYSTEM_VERSIONING = ON
        (HISTORY_TABLE = dbo.Materials_History, DATA_CONSISTENCY_CHECK = OFF));
GO

-- ════════════════════════════════════════════════════════════════════════════
-- 4. MATERIAL bridge tables (Properties, Developers, Applications)
-- ════════════════════════════════════════════════════════════════════════════

IF OBJECT_ID('dbo.MaterialProperties', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MaterialProperties (
        Id         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MaterialProperties PRIMARY KEY,
        MaterialId UNIQUEIDENTIFIER NOT NULL
                   CONSTRAINT FK_MaterialProperties_Materials
                   REFERENCES dbo.Materials(Id) ON DELETE CASCADE,
        Position   INT           NOT NULL,
        Value      NVARCHAR(MAX) NOT NULL CONSTRAINT DF_MaterialProperties_Value DEFAULT '',
        ValidFrom  DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
                   CONSTRAINT DF_MaterialProperties_ValidFrom DEFAULT SYSUTCDATETIME(),
        ValidTo    DATETIME2 GENERATED ALWAYS AS ROW END   HIDDEN
                   CONSTRAINT DF_MaterialProperties_ValidTo   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
        PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
    )
    WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.MaterialProperties_History));

    CREATE INDEX IX_MaterialProperties_MaterialId_Position ON dbo.MaterialProperties(MaterialId, Position);
END;
GO

IF OBJECT_ID('dbo.MaterialDevelopers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MaterialDevelopers (
        Id         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MaterialDevelopers PRIMARY KEY,
        MaterialId UNIQUEIDENTIFIER NOT NULL
                   CONSTRAINT FK_MaterialDevelopers_Materials
                   REFERENCES dbo.Materials(Id) ON DELETE CASCADE,
        Position   INT           NOT NULL,
        Value      NVARCHAR(MAX) NOT NULL CONSTRAINT DF_MaterialDevelopers_Value DEFAULT '',
        ValidFrom  DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
                   CONSTRAINT DF_MaterialDevelopers_ValidFrom DEFAULT SYSUTCDATETIME(),
        ValidTo    DATETIME2 GENERATED ALWAYS AS ROW END   HIDDEN
                   CONSTRAINT DF_MaterialDevelopers_ValidTo   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
        PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
    )
    WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.MaterialDevelopers_History));

    CREATE INDEX IX_MaterialDevelopers_MaterialId_Position ON dbo.MaterialDevelopers(MaterialId, Position);
END;
GO

IF OBJECT_ID('dbo.MaterialApplications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MaterialApplications (
        Id         BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MaterialApplications PRIMARY KEY,
        MaterialId UNIQUEIDENTIFIER NOT NULL
                   CONSTRAINT FK_MaterialApplications_Materials
                   REFERENCES dbo.Materials(Id) ON DELETE CASCADE,
        Position   INT           NOT NULL,
        Value      NVARCHAR(MAX) NOT NULL CONSTRAINT DF_MaterialApplications_Value DEFAULT '',
        ValidFrom  DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
                   CONSTRAINT DF_MaterialApplications_ValidFrom DEFAULT SYSUTCDATETIME(),
        ValidTo    DATETIME2 GENERATED ALWAYS AS ROW END   HIDDEN
                   CONSTRAINT DF_MaterialApplications_ValidTo   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
        PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
    )
    WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.MaterialApplications_History));

    CREATE INDEX IX_MaterialApplications_MaterialId_Position ON dbo.MaterialApplications(MaterialId, Position);
END;
GO

-- ════════════════════════════════════════════════════════════════════════════
-- 5. TRANSPORTATIONS — add 9 new scalar columns
-- ════════════════════════════════════════════════════════════════════════════

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Transportations' AND temporal_type = 2)
    ALTER TABLE dbo.Transportations SET (SYSTEM_VERSIONING = OFF);
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations') AND name = 'Propulsion')
    ALTER TABLE dbo.Transportations ADD Propulsion NVARCHAR(450) NOT NULL CONSTRAINT DF_Transportations_Propulsion DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations_History') AND name = 'Propulsion')
    ALTER TABLE dbo.Transportations_History ADD Propulsion NVARCHAR(450) NOT NULL CONSTRAINT DF_TransportationsH_Propulsion DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations') AND name = 'Speed')
    ALTER TABLE dbo.Transportations ADD Speed NVARCHAR(200) NOT NULL CONSTRAINT DF_Transportations_Speed DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations_History') AND name = 'Speed')
    ALTER TABLE dbo.Transportations_History ADD Speed NVARCHAR(200) NOT NULL CONSTRAINT DF_TransportationsH_Speed DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations') AND name = 'Capacity')
    ALTER TABLE dbo.Transportations ADD Capacity NVARCHAR(200) NOT NULL CONSTRAINT DF_Transportations_Capacity DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations_History') AND name = 'Capacity')
    ALTER TABLE dbo.Transportations_History ADD Capacity NVARCHAR(200) NOT NULL CONSTRAINT DF_TransportationsH_Capacity DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations') AND name = 'Range')
    ALTER TABLE dbo.Transportations ADD [Range] NVARCHAR(200) NOT NULL CONSTRAINT DF_Transportations_Range DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations_History') AND name = 'Range')
    ALTER TABLE dbo.Transportations_History ADD [Range] NVARCHAR(200) NOT NULL CONSTRAINT DF_TransportationsH_Range DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations') AND name = 'TierAvailability')
    ALTER TABLE dbo.Transportations ADD TierAvailability NVARCHAR(450) NOT NULL CONSTRAINT DF_Transportations_TierAvailability DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations_History') AND name = 'TierAvailability')
    ALTER TABLE dbo.Transportations_History ADD TierAvailability NVARCHAR(450) NOT NULL CONSTRAINT DF_TransportationsH_TierAvailability DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations') AND name = 'Cost')
    ALTER TABLE dbo.Transportations ADD Cost NVARCHAR(200) NOT NULL CONSTRAINT DF_Transportations_Cost DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations_History') AND name = 'Cost')
    ALTER TABLE dbo.Transportations_History ADD Cost NVARCHAR(200) NOT NULL CONSTRAINT DF_TransportationsH_Cost DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations') AND name = 'Autonomy')
    ALTER TABLE dbo.Transportations ADD Autonomy NVARCHAR(450) NOT NULL CONSTRAINT DF_Transportations_Autonomy DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations_History') AND name = 'Autonomy')
    ALTER TABLE dbo.Transportations_History ADD Autonomy NVARCHAR(450) NOT NULL CONSTRAINT DF_TransportationsH_Autonomy DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations') AND name = 'Armament')
    ALTER TABLE dbo.Transportations ADD Armament NVARCHAR(1000) NOT NULL CONSTRAINT DF_Transportations_Armament DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations_History') AND name = 'Armament')
    ALTER TABLE dbo.Transportations_History ADD Armament NVARCHAR(1000) NOT NULL CONSTRAINT DF_TransportationsH_Armament DEFAULT '';
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations') AND name = 'CommonUsage')
    ALTER TABLE dbo.Transportations ADD CommonUsage NVARCHAR(1000) NOT NULL CONSTRAINT DF_Transportations_CommonUsage DEFAULT '';
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Transportations_History') AND name = 'CommonUsage')
    ALTER TABLE dbo.Transportations_History ADD CommonUsage NVARCHAR(1000) NOT NULL CONSTRAINT DF_TransportationsH_CommonUsage DEFAULT '';
GO

-- Re-enable versioning on Transportations
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Transportations' AND temporal_type = 2)
    ALTER TABLE dbo.Transportations SET (SYSTEM_VERSIONING = ON
        (HISTORY_TABLE = dbo.Transportations_History, DATA_CONSISTENCY_CHECK = OFF));
GO

-- Index on Propulsion (commonly queried for search/filter)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Transportations_Propulsion' AND object_id = OBJECT_ID('dbo.Transportations'))
    CREATE INDEX IX_Transportations_Propulsion ON dbo.Transportations(Propulsion);
GO
