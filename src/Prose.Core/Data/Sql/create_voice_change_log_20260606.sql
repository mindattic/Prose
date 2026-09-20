-- Voice-change log: append-only, verifiable audit trail of changes to the world's
-- codified writing voice. Source = directive (user asked) | manual_edit (mined from
-- temporal beat-version diffs) | harvest (distilled from a >=80% strand). Status =
-- observed | proposed | applied | rejected. No FK to Strands — entries outlive the
-- strands they were learned from (StrandId is a soft reference). Re-runnable.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

IF OBJECT_ID(N'[dbo].[VoiceChangeLog]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[VoiceChangeLog] (
        [Id]          UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_VoiceChangeLog] PRIMARY KEY,
        [Source]      NVARCHAR(20)     NOT NULL,
        [StrandId]    UNIQUEIDENTIFIER NULL,
        [BeatId]      UNIQUEIDENTIFIER NULL,
        [Before]      NVARCHAR(MAX)    NULL,
        [After]       NVARCHAR(MAX)    NULL,
        [Description] NVARCHAR(MAX)    NOT NULL DEFAULT N'',
        [RuleTarget]  NVARCHAR(80)     NULL,
        [Evidence]    NVARCHAR(MAX)    NULL,
        [Status]      NVARCHAR(20)     NOT NULL DEFAULT N'observed',
        [CreatedAt]   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_VoiceChangeLog_Status_CreatedAt' AND object_id = OBJECT_ID(N'[dbo].[VoiceChangeLog]'))
    CREATE INDEX [IX_VoiceChangeLog_Status_CreatedAt] ON [dbo].[VoiceChangeLog]([Status], [CreatedAt]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_VoiceChangeLog_StrandId' AND object_id = OBJECT_ID(N'[dbo].[VoiceChangeLog]'))
    CREATE INDEX [IX_VoiceChangeLog_StrandId] ON [dbo].[VoiceChangeLog]([StrandId]);
