using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

public enum SequentialReadStatus
{
    /// <summary>No BookSequentialReads row exists for this book at all.</summary>
    Never,
    /// <summary>A row exists, but the book's live beat sequence has changed since (reparented,
    /// beats added/removed/reordered, a chapter nested under another chapter) — the recorded
    /// read no longer covers what the book actually is today.</summary>
    Stale,
    /// <summary>The most recent recorded read's hash matches the book's current beat sequence.</summary>
    Current,
}

public class SequentialReadReport
{
    public Guid NodeId { get; set; }
    public string BookTitle { get; set; } = "";
    public SequentialReadStatus Status { get; set; }
    public int CurrentBeatCount { get; set; }
    public int CurrentChapterCount { get; set; }
    public DateTime? LastReadAt { get; set; }
    public string? LastReadBy { get; set; }
    public int? LastReadBeatCount { get; set; }
    public int? LastReadChapterCount { get; set; }
}

/// <summary>
/// Tracks whether a book has ever actually been read front-to-back as one continuous sequence,
/// as distinct from being swept in scoped/parallel chunks or having its structure (ParentNodeId)
/// corrected without anyone reading what was inside it.
///
/// Root cause this exists to fix (2026-08-15): BCODA had 15 chapters (Ch23-37, 155 beats, ~30%
/// of the book) nested under a mislabeled "Chapter 22 - Ghost Period" wrapper node. The
/// 2026-08-14 structural fix reparented those chapters (fixed WHERE they sit in the tree) but
/// nobody had ever read what was INSIDE them — and the first real read (2026-08-15) found a
/// genuine spoiler-duplicate beat that had sat there, live, since before the fix. A structural
/// fix and a content read are different operations; this service makes the second one a tracked,
/// verifiable fact instead of an assumption.
///
/// The freshness check is self-invalidating by construction: <see cref="ComputeBeatSequenceHashAsync"/>
/// walks the book's full chapter/beat sequence FRESH every time (recursive descendant walk per
/// CLAUDE.md's HARD RULE — never a flat ParentNodeId=book query, which would silently miss
/// anything nested deeper). Any structural change changes the hash automatically, so staleness
/// is detected, not trusted — no invalidation trigger or manual "mark stale" step is needed.
/// </summary>
public class SequentialReadTrackingService(IDbContextFactory<ProseDbContext> dbFactory)
{
    /// <summary>
    /// Walks every chapter under <paramref name="bookNodeId"/> (recursive descendant walk — finds
    /// chapters nested at any depth, not just direct children), then every beat under each chapter
    /// in reading order, and hashes the resulting (chapter, beat) sequence. Returns the hash plus
    /// the beat/chapter counts it was computed from.
    /// </summary>
    public async Task<(string Hash, int BeatCount, int ChapterCount)> ComputeBeatSequenceHashAsync(
        Guid bookNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var chapters = await db.Database.SqlQuery<ChapterRow>($"""
            WITH descendants AS (
                SELECT Id, ParentNodeId, Kind, Title, SortKey FROM Nodes WHERE Id = {bookNodeId}
                UNION ALL
                SELECT n.Id, n.ParentNodeId, n.Kind, n.Title, n.SortKey
                FROM Nodes n JOIN descendants d ON n.ParentNodeId = d.Id
            )
            SELECT Id, Title, SortKey FROM descendants WHERE Kind = 'chapter'
            ORDER BY SortKey
            """).ToListAsync(ct);

        var sb = new StringBuilder();
        int beatCount = 0;
        foreach (var chapter in chapters)
        {
            sb.Append("CH|").Append(chapter.Id).Append('|').Append(chapter.SortKey).Append('\n');
            var beats = await db.Database.SqlQuery<BeatRow>($"""
                SELECT bn.BeatId AS Id, bn.SortKey
                FROM BeatNodes bn
                WHERE bn.NodeId = {chapter.Id}
                ORDER BY bn.SortKey
                """).ToListAsync(ct);
            foreach (var beat in beats)
            {
                sb.Append("B|").Append(beat.Id).Append('|').Append(beat.SortKey).Append('\n');
                beatCount++;
            }
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
        return (hash, beatCount, chapters.Count);
    }

    /// <summary>
    /// Compares the book's live beat sequence against the most recent recorded read.
    /// </summary>
    public async Task<SequentialReadReport> GetStatusAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): explicit bookNodeId, not an ambient scope (same bug class found
        // and fixed in BookArchiveService.ArchiveAsync/WalkAsync, 2026-08-17).
        var book = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == bookNodeId, ct)
            ?? throw new InvalidOperationException($"No node found with Id {bookNodeId}");

        var (hash, beatCount, chapterCount) = await ComputeBeatSequenceHashAsync(bookNodeId, ct);

        var last = await db.BookSequentialReads.AsNoTracking()
            .Where(r => r.NodeId == bookNodeId)
            .OrderByDescending(r => r.ReadAt)
            .FirstOrDefaultAsync(ct);

        var status = last is null
            ? SequentialReadStatus.Never
            : last.BeatSequenceHash == hash
                ? SequentialReadStatus.Current
                : SequentialReadStatus.Stale;

        return new SequentialReadReport
        {
            NodeId = bookNodeId,
            BookTitle = book.Title,
            Status = status,
            CurrentBeatCount = beatCount,
            CurrentChapterCount = chapterCount,
            LastReadAt = last?.ReadAt,
            LastReadBy = last?.ReadBy,
            LastReadBeatCount = last?.BeatCount,
            LastReadChapterCount = last?.ChapterCount,
        };
    }

    /// <summary>
    /// Records that a genuine sequential (front-to-back) read of this book just completed.
    /// Computes the current beat-sequence hash fresh — callers do not supply it — so the record
    /// always reflects what was actually read, not what the caller believes was read.
    /// </summary>
    public async Task RecordReadAsync(
        Guid bookNodeId, string readBy, int stageCount, string? findingsSummary, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): explicit bookNodeId, not an ambient scope (same bug class found
        // and fixed in BookArchiveService.ArchiveAsync/WalkAsync, 2026-08-17).
        var book = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == bookNodeId, ct)
            ?? throw new InvalidOperationException($"No node found with Id {bookNodeId}");

        var (hash, beatCount, chapterCount) = await ComputeBeatSequenceHashAsync(bookNodeId, ct);

        db.BookSequentialReads.Add(new BookSequentialRead
        {
            NodeId = bookNodeId,
            UniverseId = book.UniverseId,
            BeatSequenceHash = hash,
            BeatCount = beatCount,
            ChapterCount = chapterCount,
            StageCount = stageCount,
            ReadBy = readBy,
            FindingsSummary = findingsSummary,
            ReadAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }

    private sealed class ChapterRow
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public double SortKey { get; set; }
    }

    private sealed class BeatRow
    {
        public Guid Id { get; set; }
        public double SortKey { get; set; }
    }
}
