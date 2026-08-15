using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

public sealed record BookArchiveResult(Guid ArchivedBookId, int LeafNodeCount, int BeatCount, int WordCount);

/// <summary>
/// Snapshots a book's ENTIRE current live prose into one <see cref="ArchivedBook"/> row — a
/// pre-edit backup. Extracted from <c>ArchiveBookCli</c> (manual `--archive-book`) so
/// <see cref="AutoCorrectOrchestratorService"/> can call the exact same, tested logic before it
/// touches a book, without going through the CLI. Read-only against Beats/Nodes/BeatNodes: never
/// deletes or modifies existing content, only adds a snapshot row. Beats/Nodes/BeatNodes are no
/// longer system-versioned and there is no soft-delete anymore
/// (<see cref="NodeWorkbenchService.DeleteBeatAsync"/>), so this snapshot is the only way to
/// recover prior content later if something downstream goes wrong.
/// </summary>
public class BookArchiveService(IDbContextFactory<ProseDbContext> dbFactory)
{
    public async Task<BookArchiveResult> ArchiveAsync(Guid nodeId, string reason, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
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
            Reason = reason,
            Markdown = snapshotMd.ToString().TrimEnd() + "\n",
            BeatCount = beatCount,
            WordCount = wordCount,
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
