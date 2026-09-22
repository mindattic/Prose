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
/// walks the book's full chapter/beat sequence FRESH every time. Any structural change changes the
/// hash automatically, so staleness is detected, not trusted — no invalidation trigger or manual
/// "mark stale" step is needed.
///
/// It gets that walk from <see cref="BookSpineService"/> rather than running its own (2026-09-22).
/// The hand-rolled walk this replaced found chapters with a recursive CTE but then took only the
/// beats hanging DIRECTLY off each chapter node (<c>WHERE bn.NodeId = chapter.Id</c>), and selected
/// chapters on <c>Kind = 'chapter'</c> instead of the NodeType discriminator. Once a chapter gained
/// a scene or sequence layer, its beats moved onto those child nodes and vanished from the hash:
/// Bushido Coda hashed 476 of its 521 beats, and all 45 missing ones were Chapter 15's, sitting
/// under 21 derived scene nodes. Every word of that chapter could have been rewritten and this
/// service would still have reported "Current" — the precise failure it exists to prevent, silently.
/// The spine is now the one walk that decides what a chapter is, for exporters and for this.
/// </summary>
public class SequentialReadTrackingService(IDbContextFactory<ProseDbContext> dbFactory)
{
    /// <summary>
    /// Hashes the book's reading-order (chapter, beat) sequence exactly as <see cref="BookSpineService"/>
    /// resolves it — so a chapter's beats are counted whether they hang off the chapter itself or off
    /// a scene/sequence layer beneath it. Returns the hash plus the beat/chapter counts it was
    /// computed from.
    /// </summary>
    public async Task<(string Hash, int BeatCount, int ChapterCount)> ComputeBeatSequenceHashAsync(
        Guid bookNodeId, CancellationToken ct = default)
    {
        // Constructed rather than injected, the same way BookSpineService reaches NodeWorkbenchService:
        // its only dependency is the factory this service already holds, and taking it through DI here
        // would add an edge to a graph that already documents a cycle at that seam.
        var spine = await new BookSpineService(dbFactory).GetAsync(bookNodeId, ct);

        var sb = new StringBuilder();
        int beatCount = 0;
        foreach (var chapter in spine.Chapters)
        {
            sb.Append("CH|").Append(chapter.NodeId).Append('|').Append(chapter.Ordinal).Append('\n');
            foreach (var beat in chapter.Beats)
            {
                // NodeId is included so that moving a beat between scenes inside one chapter — which
                // changes nothing about reading order — still invalidates the recorded read, because
                // it changes what a re-reader would have to re-check.
                sb.Append("B|").Append(beat.BeatId).Append('|').Append(beat.NodeId)
                  .Append('|').Append(beat.Ordinal).Append('\n');
                beatCount++;
            }
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
        return (hash, beatCount, spine.Chapters.Count);
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

}
