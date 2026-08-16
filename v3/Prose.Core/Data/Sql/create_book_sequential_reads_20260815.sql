-- create_book_sequential_reads_20260815.sql
-- ───────────────────────────────────────────────────────────────────────────
-- Tracks whether a book has ever actually been read front-to-back as one
-- continuous sequence, as opposed to swept in scoped/parallel chunks.
--
-- Root cause this exists to fix: BCODA had 15 chapters (Ch23-37, 155 beats,
-- ~30% of the book) nested under a mislabeled "Chapter 22 - Ghost Period"
-- wrapper node. The 2026-08-14 structural fix (reparenting) corrected WHERE
-- those chapters sit in the tree, but nobody had ever read what was INSIDE
-- them until 2026-08-15 - and that first read found a genuine spoiler-
-- duplicate beat that had sat there, live, since before the fix. Four other
-- books (VIGL, Ballast, It Came From Iowa, Read the Room) had the identical
-- wrapper-chapter bug and, as of this table's creation, have no verified
-- record of ever having had a true sequential read either.
--
-- BeatSequenceHash is a SHA256 of the book's full ordered (chapter, beat)
-- sequence, computed via a recursive descendant walk (never a flat
-- ParentNodeId=book query - see CLAUDE.md's HARD RULE on this). Any
-- structural change (reparenting, beat insert/disable/reorder, a chapter
-- nested under another chapter) changes this hash automatically, so
-- staleness is DETECTED, not trusted. A book is only "Current" when its
-- latest recorded hash matches what the live DB produces right now.
--
-- Idempotent. Run under QUOTED_IDENTIFIER ON (sqlcmd -I).
-- ───────────────────────────────────────────────────────────────────────────

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BookSequentialReads')
BEGIN
    CREATE TABLE dbo.BookSequentialReads (
        Id              bigint IDENTITY(1,1) NOT NULL,
        NodeId          uniqueidentifier NOT NULL,
        UniverseId      uniqueidentifier NOT NULL,
        BeatSequenceHash char(64)        NOT NULL,
        BeatCount       int              NOT NULL,
        ChapterCount    int              NOT NULL,
        StageCount      int              NOT NULL DEFAULT 1,
        ReadBy          nvarchar(200)    NOT NULL,
        FindingsSummary nvarchar(max)    NULL,
        ReadAt          datetime2        NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_BookSequentialReads PRIMARY KEY (Id),
        CONSTRAINT FK_BookSequentialReads_Node
            FOREIGN KEY (NodeId) REFERENCES dbo.Nodes(Id)
    );
    CREATE INDEX IX_BookSequentialReads_NodeId ON dbo.BookSequentialReads(NodeId, ReadAt DESC);
    CREATE INDEX IX_BookSequentialReads_UniverseId ON dbo.BookSequentialReads(UniverseId);
END
GO
