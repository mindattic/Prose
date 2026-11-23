BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE TABLE [BookChatMessages] (
        [Id] uniqueidentifier NOT NULL,
        [UniverseId] uniqueidentifier NOT NULL,
        [BookNodeId] uniqueidentifier NOT NULL,
        [Role] nvarchar(16) NOT NULL,
        [Text] nvarchar(max) NOT NULL,
        [DoorsJson] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BookChatMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BookChatMessages_Nodes_BookNodeId] FOREIGN KEY ([BookNodeId]) REFERENCES [Nodes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE TABLE [BrainstormCards] (
        [Id] uniqueidentifier NOT NULL,
        [UniverseId] uniqueidentifier NOT NULL,
        [BookNodeId] uniqueidentifier NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Body] nvarchar(max) NOT NULL,
        [X] float NOT NULL,
        [Y] float NOT NULL,
        [Color] nvarchar(20) NOT NULL,
        [EntityId] uniqueidentifier NULL,
        [LinkedCardId] uniqueidentifier NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BrainstormCards] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BrainstormCards_Nodes_BookNodeId] FOREIGN KEY ([BookNodeId]) REFERENCES [Nodes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE TABLE [ChapterReadings] (
        [Id] uniqueidentifier NOT NULL,
        [UniverseId] uniqueidentifier NOT NULL,
        [BookNodeId] uniqueidentifier NOT NULL,
        [ChapterNodeId] uniqueidentifier NOT NULL,
        [ChapterIndex] int NOT NULL,
        [ChapterTitle] nvarchar(400) NOT NULL,
        [ContentHash] nvarchar(64) NOT NULL,
        [Words] int NOT NULL,
        [MetJson] nvarchar(max) NOT NULL,
        [DetailCount] int NOT NULL,
        [ContradictionCount] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Error] nvarchar(max) NULL,
        [ReadAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ChapterReadings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChapterReadings_Nodes_BookNodeId] FOREIGN KEY ([BookNodeId]) REFERENCES [Nodes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE TABLE [ContinuityConflicts] (
        [Id] uniqueidentifier NOT NULL,
        [UniverseId] uniqueidentifier NOT NULL,
        [BookNodeId] uniqueidentifier NOT NULL,
        [EntityId] uniqueidentifier NULL,
        [EntityName] nvarchar(200) NOT NULL,
        [Kind] nvarchar(20) NOT NULL,
        [Category] nvarchar(40) NOT NULL,
        [Attribute] nvarchar(80) NOT NULL,
        [Confidence] int NOT NULL,
        [ExistingFactId] uniqueidentifier NULL,
        [ExistingValue] nvarchar(400) NOT NULL,
        [ExistingQuote] nvarchar(max) NOT NULL,
        [ExistingBeatId] uniqueidentifier NULL,
        [ExistingChapterNodeId] uniqueidentifier NULL,
        [ExistingChapterTitle] nvarchar(400) NOT NULL,
        [ProposedValue] nvarchar(400) NOT NULL,
        [ProposedQuote] nvarchar(max) NOT NULL,
        [ProposedBeatId] uniqueidentifier NULL,
        [ProposedChapterNodeId] uniqueidentifier NULL,
        [ProposedChapterTitle] nvarchar(400) NOT NULL,
        [Fingerprint] nvarchar(400) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Note] nvarchar(1000) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ResolvedAt] datetime2 NULL,
        CONSTRAINT [PK_ContinuityConflicts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContinuityConflicts_Nodes_BookNodeId] FOREIGN KEY ([BookNodeId]) REFERENCES [Nodes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE TABLE [StoryBibleSuggestions] (
        [Id] uniqueidentifier NOT NULL,
        [UniverseId] uniqueidentifier NOT NULL,
        [BookNodeId] uniqueidentifier NOT NULL,
        [Kind] nvarchar(20) NOT NULL,
        [Category] nvarchar(40) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [EntityId] uniqueidentifier NULL,
        [AttributesJson] nvarchar(max) NOT NULL,
        [Quote] nvarchar(max) NOT NULL,
        [BeatId] uniqueidentifier NULL,
        [ChapterNodeId] uniqueidentifier NULL,
        [Fingerprint] nvarchar(400) NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [DecidedAt] datetime2 NULL,
        CONSTRAINT [PK_StoryBibleSuggestions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StoryBibleSuggestions_Nodes_BookNodeId] FOREIGN KEY ([BookNodeId]) REFERENCES [Nodes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE TABLE [StoryFacts] (
        [Id] uniqueidentifier NOT NULL,
        [UniverseId] uniqueidentifier NOT NULL,
        [BookNodeId] uniqueidentifier NOT NULL,
        [EntityId] uniqueidentifier NULL,
        [SubjectKey] nvarchar(200) NOT NULL,
        [SubjectName] nvarchar(200) NOT NULL,
        [SuggestionId] uniqueidentifier NULL,
        [Category] nvarchar(40) NOT NULL,
        [Attribute] nvarchar(80) NOT NULL,
        [Value] nvarchar(400) NOT NULL,
        [Quote] nvarchar(max) NOT NULL,
        [BeatId] uniqueidentifier NULL,
        [ChapterNodeId] uniqueidentifier NULL,
        [ChapterIndex] int NOT NULL,
        [Source] nvarchar(20) NOT NULL,
        [Confirmed] bit NOT NULL,
        [SupersededAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StoryFacts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StoryFacts_Entities_EntityId] FOREIGN KEY ([EntityId]) REFERENCES [Entities] ([Id]),
        CONSTRAINT [FK_StoryFacts_Nodes_BookNodeId] FOREIGN KEY ([BookNodeId]) REFERENCES [Nodes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE INDEX [IX_BookChatMessages_BookNodeId_CreatedAt] ON [BookChatMessages] ([BookNodeId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE INDEX [IX_BrainstormCards_BookNodeId] ON [BrainstormCards] ([BookNodeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE INDEX [IX_ChapterReadings_BookNodeId_ReadAt] ON [ChapterReadings] ([BookNodeId], [ReadAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE INDEX [IX_ChapterReadings_ChapterNodeId] ON [ChapterReadings] ([ChapterNodeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE INDEX [IX_ContinuityConflicts_BookNodeId_Fingerprint] ON [ContinuityConflicts] ([BookNodeId], [Fingerprint]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE INDEX [IX_ContinuityConflicts_BookNodeId_Status] ON [ContinuityConflicts] ([BookNodeId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE INDEX [IX_StoryBibleSuggestions_BookNodeId_Fingerprint] ON [StoryBibleSuggestions] ([BookNodeId], [Fingerprint]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE INDEX [IX_StoryBibleSuggestions_BookNodeId_Status] ON [StoryBibleSuggestions] ([BookNodeId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE INDEX [IX_StoryFacts_BookNodeId_SubjectKey] ON [StoryFacts] ([BookNodeId], [SubjectKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    CREATE INDEX [IX_StoryFacts_EntityId_Attribute] ON [StoryFacts] ([EntityId], [Attribute]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261123082631_AddReadAlong'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261123082631_AddReadAlong', N'10.0.14');
END;

COMMIT;
GO

