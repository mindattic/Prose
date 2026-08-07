-- create_plant_payoffs_20260623.sql
-- ───────────────────────────────────────────────────────────────────────────
-- Tracks seeded narrative details (plants) and their payoffs across beats.
-- Enforces the principle: "reward re-reading without requiring it."
--
-- IsTransparent = true means the payoff beat reads completely for a cold
-- reader who never caught the plant. A false row is a writing bug.
--
-- Categories:
--   detail          — a small fact or observation that pays off later
--   echo            — a scene/gesture mirroring an earlier one, meaning shifted
--   irony           — a line that reads differently knowing the outcome
--   motif           — a recurring symbol or image accumulating meaning
--   character-truth — a behavioral tell that, in hindsight, reveals who someone is
--   structural      — an architecture element (opening/closing mirror, etc.)
--
-- Idempotent. Run under QUOTED_IDENTIFIER ON (sqlcmd -I).
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlantPayoffs')
BEGIN
    CREATE TABLE dbo.PlantPayoffs (
        Id                uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        UniverseId        uniqueidentifier NOT NULL,
        StrandId          uniqueidentifier NOT NULL,
        PlantBeatId       uniqueidentifier NULL,
        PayoffBeatId      uniqueidentifier NULL,
        PlantDescription  nvarchar(500)    NOT NULL,
        PayoffDescription nvarchar(500)    NOT NULL,
        Category          nvarchar(50)     NOT NULL DEFAULT 'detail',
        IsTransparent     bit              NOT NULL DEFAULT 1,
        TransparencyNote  nvarchar(500)    NULL,
        SortKey           float            NOT NULL DEFAULT 0,
        CreatedAt         datetime2        NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt         datetime2        NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT PK_PlantPayoffs PRIMARY KEY (Id),
        CONSTRAINT FK_PlantPayoffs_Strands
            FOREIGN KEY (StrandId) REFERENCES dbo.Strands(Id) ON DELETE CASCADE,
        CONSTRAINT FK_PlantPayoffs_PlantBeat
            FOREIGN KEY (PlantBeatId) REFERENCES dbo.Beats(Id),
        CONSTRAINT FK_PlantPayoffs_PayoffBeat
            FOREIGN KEY (PayoffBeatId) REFERENCES dbo.Beats(Id),
        CONSTRAINT CHK_PlantPayoffs_Category
            CHECK (Category IN ('detail','echo','irony','motif','character-truth','structural'))
    );
    CREATE INDEX IX_PlantPayoffs_StrandId    ON dbo.PlantPayoffs(StrandId);
    CREATE INDEX IX_PlantPayoffs_PlantBeatId  ON dbo.PlantPayoffs(PlantBeatId) WHERE PlantBeatId IS NOT NULL;
    CREATE INDEX IX_PlantPayoffs_PayoffBeatId ON dbo.PlantPayoffs(PayoffBeatId) WHERE PayoffBeatId IS NOT NULL;
END
GO
