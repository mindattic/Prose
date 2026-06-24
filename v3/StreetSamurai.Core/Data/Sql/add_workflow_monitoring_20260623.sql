-- ============================================================
-- SS-A16: Workflow Monitoring Tables
-- Tracks which prose services were active/applicable per beat.
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID('dbo.BeatServiceLog', 'U') IS NULL
BEGIN
    CREATE TABLE BeatServiceLog (
        Id              uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
        UniverseId      uniqueidentifier NOT NULL,
        BeatId          uniqueidentifier NULL,
        StrandId        uniqueidentifier NOT NULL,
        Service         nvarchar(100)    NOT NULL,
        WasApplicable   bit              NOT NULL DEFAULT 0,
        WasActive       bit              NOT NULL DEFAULT 0,
        BlockSizeChars  int              NOT NULL DEFAULT 0,
        WrittenAt       datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_BeatServiceLog PRIMARY KEY (Id)
    );
    CREATE INDEX IX_BeatServiceLog_StrandId ON BeatServiceLog (StrandId);
    CREATE INDEX IX_BeatServiceLog_BeatId   ON BeatServiceLog (BeatId) WHERE BeatId IS NOT NULL;
END

IF OBJECT_ID('dbo.BeatModeLog', 'U') IS NULL
BEGIN
    CREATE TABLE BeatModeLog (
        BeatId           uniqueidentifier NOT NULL,
        UniverseId       uniqueidentifier NOT NULL,
        Mode             nvarchar(50)     NOT NULL,
        Confidence       real             NOT NULL,
        DetectionMethod  nvarchar(50)     NOT NULL,
        DetectedAt       datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_BeatModeLog PRIMARY KEY (BeatId)
    );
END
