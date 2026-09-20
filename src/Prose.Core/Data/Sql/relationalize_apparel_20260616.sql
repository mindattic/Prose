-- RFC 0007: Complete Apparel relational schema.
-- Adds 5 missing scalar columns to Apparels / Apparels_History
-- and creates 2 new bridge tables (ApparelMaterials, ApparelWornBy).
-- All operations are idempotent (catalog-guarded).

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. Add missing scalar columns to Apparels
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE Apparels SET (SYSTEM_VERSIONING = OFF)
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Apparels') AND name = 'Functionality')
    ALTER TABLE Apparels ADD Functionality NVARCHAR(MAX) NOT NULL DEFAULT ''
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Apparels') AND name = 'WhatItSays')
    ALTER TABLE Apparels ADD WhatItSays NVARCHAR(MAX) NOT NULL DEFAULT ''
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Apparels') AND name = 'PriceRange')
    ALTER TABLE Apparels ADD PriceRange NVARCHAR(MAX) NOT NULL DEFAULT ''
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Apparels') AND name = 'AugCompatible')
    ALTER TABLE Apparels ADD AugCompatible BIT NOT NULL DEFAULT 0
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Apparels') AND name = 'GeneCompatible')
    ALTER TABLE Apparels ADD GeneCompatible BIT NOT NULL DEFAULT 0
GO

-- Mirror the same columns in the history table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Apparels_History') AND name = 'Functionality')
    ALTER TABLE Apparels_History ADD Functionality NVARCHAR(MAX) NOT NULL DEFAULT ''
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Apparels_History') AND name = 'WhatItSays')
    ALTER TABLE Apparels_History ADD WhatItSays NVARCHAR(MAX) NOT NULL DEFAULT ''
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Apparels_History') AND name = 'PriceRange')
    ALTER TABLE Apparels_History ADD PriceRange NVARCHAR(MAX) NOT NULL DEFAULT ''
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Apparels_History') AND name = 'AugCompatible')
    ALTER TABLE Apparels_History ADD AugCompatible BIT NOT NULL DEFAULT 0
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Apparels_History') AND name = 'GeneCompatible')
    ALTER TABLE Apparels_History ADD GeneCompatible BIT NOT NULL DEFAULT 0
GO

ALTER TABLE Apparels SET (
    SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.Apparels_History)
)
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. Bridge: ApparelMaterials (list of material strings)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApparelMaterials')
BEGIN
    CREATE TABLE ApparelMaterials (
        Id        BIGINT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
        ApparelId UNIQUEIDENTIFIER NOT NULL,
        Position  INT              NOT NULL DEFAULT 0,
        Value     NVARCHAR(MAX)    NOT NULL DEFAULT '',
        CONSTRAINT FK_ApparelMaterials_Apparel FOREIGN KEY (ApparelId)
            REFERENCES Apparels(Id) ON DELETE CASCADE
    )
    CREATE INDEX IX_ApparelMaterials_ApparelId_Position ON ApparelMaterials (ApparelId, Position)
END
GO

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. Bridge: ApparelWornBy (list of entity aliases, resolves to character/faction)
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApparelWornBy')
BEGIN
    CREATE TABLE ApparelWornBy (
        Id              BIGINT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
        ApparelId       UNIQUEIDENTIFIER NOT NULL,
        Position        INT              NOT NULL DEFAULT 0,
        Alias           NVARCHAR(450)    NOT NULL DEFAULT '',
        CharacterEntityId UNIQUEIDENTIFIER NULL,
        CONSTRAINT FK_ApparelWornBy_Apparel FOREIGN KEY (ApparelId)
            REFERENCES Apparels(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ApparelWornBy_Character FOREIGN KEY (CharacterEntityId)
            REFERENCES Entities(Id) ON DELETE NO ACTION
    )
    CREATE INDEX IX_ApparelWornBy_ApparelId_Position ON ApparelWornBy (ApparelId, Position)
    CREATE INDEX IX_ApparelWornBy_CharacterEntityId   ON ApparelWornBy (CharacterEntityId)
    CREATE INDEX IX_ApparelWornBy_Alias               ON ApparelWornBy (Alias)
END
GO
