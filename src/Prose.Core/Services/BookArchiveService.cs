using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

public sealed record BookArchiveResult(Guid ArchivedBookId, int LeafNodeCount, int BeatCount, int WordCount);

/// <summary>
/// Snapshots a book's ENTIRE current live prose, plus the Node's own content fields
/// (Description, NodeOutline, Summary, Seed, Subtitle), into one <see cref="ArchivedBook"/> row —
/// a pre-edit backup. Extracted from <c>ArchiveBookCli</c> (manual `--archive-book`) so
/// <see cref="AutoCorrectOrchestratorService"/> can call the exact same, tested logic before it
/// touches a book, without going through the CLI. Read-only against Beats/Nodes/BeatNodes: never
/// deletes or modifies existing content, only adds a snapshot row. Beats/Nodes/BeatNodes are no
/// longer system-versioned and there is no soft-delete anymore
/// (<see cref="NodeWorkbenchService.DeleteBeatAsync"/>), so this snapshot is the only way to
/// recover prior content later if something downstream goes wrong.
///
/// Any future feature that bulk-overwrites a Node's content field(s) (e.g. a description
/// generator, a bible-rewrite tool) should call <see cref="ArchiveAsync"/> with a
/// reason describing the operation (e.g. "pre-description-regen") before making the change —
/// the same convention <see cref="AutoCorrectOrchestratorService"/> already follows. This is not
/// enforced automatically; there is currently no such caller.
/// </summary>
public class BookArchiveService(IDbContextFactory<ProseDbContext> dbFactory)
{
    public async Task<BookArchiveResult> ArchiveAsync(Guid nodeId, string reason, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // IgnoreQueryFilters(): the caller passes an explicit nodeId, not an ambient scope — a
        // book in a universe other than whatever the ambient default happens to be would otherwise
        // silently 404 here even though the id is correct. Found live 2026-08-17: 14 of 36 books
        // failed a corpus-wide `--archive-book --all` this way before this fix.
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"[BookArchiveService] Node {nodeId} not found.");

        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id, ct);

        var snapshotMd = new System.Text.StringBuilder();
        snapshotMd.AppendLine($"# {node.Title}");
        snapshotMd.AppendLine();
        int beatCount = 0;
        int wordCount = 0;

        foreach (var leafId in leafIds)
        {
            var beats = await db.BeatNodes
                .Where(bn => bn.NodeId == leafId)
                .OrderBy(bn => bn.SortKey)
                .Join(db.Beats, bn => bn.BeatId, b => b.Id, (bn, b) => b)
                .ToListAsync(ct);

            foreach (var beat in beats)
            {
                if (string.IsNullOrWhiteSpace(beat.Text)) continue;
                snapshotMd.AppendLine(beat.Text.Trim());
                snapshotMd.AppendLine();
                beatCount++;
                wordCount += beat.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            }
        }

        var archived = new ArchivedBook
        {
            Id = Guid.NewGuid(),
            NodeId = node.Id,
            Title = node.Title,
            Version = node.Version,
            // Defensive clamp to the column width: an archive is a safety snapshot taken right
            // before destructive prose edits, so it must never be the thing that fails because
            // the caller's note was wordy. Truncating a note is always better than losing the
            // snapshot (2026-08-23).
            Reason = reason.Length > 200 ? reason[..200] : reason,
            Markdown = snapshotMd.ToString().TrimEnd() + "\n",
            BeatCount = beatCount,
            WordCount = wordCount,
            Description = node.Description,
            NodeOutline = node.NodeOutline,
            Summary = node.Summary,
            Seed = node.Seed,
            Subtitle = node.Subtitle,
            CreatedAt = DateTime.UtcNow,
        };
        db.ArchivedBooks.Add(archived);
        await db.SaveChangesAsync(ct);

        var verified = await db.ArchivedBooks.AsNoTracking().AnyAsync(a => a.Id == archived.Id, ct);
        if (!verified)
            throw new InvalidOperationException($"[BookArchiveService] Snapshot row for node {nodeId} not found after save — refusing to proceed without a verified backup.");

        return new BookArchiveResult(archived.Id, leafIds.Count, beatCount, wordCount);
    }
}
