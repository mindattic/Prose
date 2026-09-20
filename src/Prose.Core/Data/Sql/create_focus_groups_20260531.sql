-- Named, reusable persona panels (focus groups) so review runs can be compared
-- like a recurring focus group ("Group A", "Group B", …). FocusGroups holds the
-- panel; FocusGroupMembers is its fixed roster of Legion personas. StrandReviews
-- gains a soft FocusGroupId + denormalized name so every review is attributable
-- to who was in the room. Re-runnable via OBJECT_ID / COL_LENGTH guards.

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ARITHABORT ON;

IF OBJECT_ID(N'[dbo].[FocusGroups]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FocusGroups] (
        [Id]          UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_FocusGroups] PRIMARY KEY,
        [Name]        NVARCHAR(100)    NOT NULL,
        [Description] NVARCHAR(1000)   NULL,
        [CreatedAt]   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FocusGroups_Name' AND object_id = OBJECT_ID(N'[dbo].[FocusGroups]'))
    CREATE UNIQUE INDEX [IX_FocusGroups_Name] ON [dbo].[FocusGroups]([Name]);

IF OBJECT_ID(N'[dbo].[FocusGroupMembers]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FocusGroupMembers] (
        [FocusGroupId] UNIQUEIDENTIFIER NOT NULL,
        [PersonaId]    NVARCHAR(40)     NOT NULL,
        [PersonaName]  NVARCHAR(80)     NOT NULL,
        [PersonaBlurb] NVARCHAR(400)    NULL,
        CONSTRAINT [PK_FocusGroupMembers] PRIMARY KEY ([FocusGroupId], [PersonaId]),
        CONSTRAINT [FK_FocusGroupMembers_FocusGroups]
            FOREIGN KEY ([FocusGroupId]) REFERENCES [dbo].[FocusGroups]([Id]) ON DELETE CASCADE
    );
END;

IF COL_LENGTH('dbo.StrandReviews', 'FocusGroupId') IS NULL
    EXEC(N'ALTER TABLE [dbo].[StrandReviews] ADD [FocusGroupId] UNIQUEIDENTIFIER NULL;');

IF COL_LENGTH('dbo.StrandReviews', 'FocusGroupName') IS NULL
    EXEC(N'ALTER TABLE [dbo].[StrandReviews] ADD [FocusGroupName] NVARCHAR(100) NULL;');
