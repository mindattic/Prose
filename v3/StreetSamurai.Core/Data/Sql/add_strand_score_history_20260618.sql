-- StreetSamurai migration 2026-06-18
-- Append-only score-history table: one row per RecomputeScoresAsync call.
-- Lets the UI plot score-over-time per strand (and averaged across child strands
-- for book-level parents).  No system-versioning needed — rows are never updated.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'StrandScoreHistory')
BEGIN
    CREATE TABLE StrandScoreHistory
    (
        Id          INT             IDENTITY(1,1)  NOT NULL,
        StrandId    UNIQUEIDENTIFIER               NOT NULL,
        RecordedAt  DATETIME2       NOT NULL  DEFAULT SYSUTCDATETIME(),
        ContentHash NVARCHAR(64)    NOT NULL,
        MeanScore   FLOAT           NOT NULL,
        Sd          FLOAT               NULL,
        ReviewCount INT             NOT NULL,
        BeatCount   INT             NOT NULL,
        CONSTRAINT PK_StrandScoreHistory PRIMARY KEY (Id),
        CONSTRAINT FK_StrandScoreHistory_Strands
            FOREIGN KEY (StrandId) REFERENCES Strands(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_StrandScoreHistory_StrandId_RecordedAt
        ON StrandScoreHistory (StrandId, RecordedAt);

    PRINT 'Created StrandScoreHistory table.';
END
ELSE
BEGIN
    PRINT 'StrandScoreHistory already exists — skipped.';
END
GO
