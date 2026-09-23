using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Factory;

/// <summary>
/// One hash of a book's whole prose, in reading order: any change to any beat's text anywhere in
/// the book changes it (RFC 0015 §3.1). The press (F7) records it on every export, so "the file on
/// disk is this book" is a comparison, not a claim. Moved here from <c>LogicSweepService</c>, which
/// now calls this copy — one definition, so the sweep and the press can never disagree.
/// </summary>
public static class BookFingerprint
{
    /// <summary>Hash over every non-empty beat's own text hash, chapter order then SortKey. Computed
    /// from live <see cref="Beat.Text"/> rather than the stored TextHash column, which can be null or
    /// stale for a beat that predates stamping.</summary>
    public static async Task<string> ComputeAsync(ProseDbContext db, Guid nodeId, CancellationToken ct = default)
    {
        var nodeIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        // Ordered chapter-then-SortKey: raw SortKey alone is chapter-local (every chapter's beats
        // restart near the same values), so ordering the whole book by it would tie across chapters
        // and let the fingerprint flap between two runs of unchanged content.
        var chapterOrder = nodeIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        var rows = await db.BeatNodes.AsNoTracking()
            .Where(bn => nodeIds.Contains(bn.NodeId) && bn.Beat != null
                      && bn.Beat!.Text != null && bn.Beat.Text != "")
            .Select(bn => new { bn.NodeId, bn.SortKey, Text = bn.Beat!.Text })
            .ToListAsync(ct);
        var texts = rows
            .OrderBy(b => chapterOrder.TryGetValue(b.NodeId, out var idx) ? idx : int.MaxValue)
            .ThenBy(b => b.SortKey)
            .Select(b => b.Text);
        return Beat.ComputeHash(string.Join("|", texts.Select(Beat.ComputeHash)));
    }

    public static async Task<string> ComputeAsync(IDbContextFactory<ProseDbContext> dbFactory, Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await ComputeAsync(db, nodeId, ct);
    }
}

/// <summary>
/// Writes the proof of a press (RFC 0015 §3.10): after a shippable export has written its file —
/// and therefore after it passed the read gate — one <see cref="ExportRecord"/> row with the book's
/// fingerprint at that moment. Station F7 compares those rows with the book as it stands now.
/// </summary>
public sealed class ExportRecorder(IDbContextFactory<ProseDbContext> dbFactory)
{
    public static readonly string[] BookFormats = ["docx", "epub", "pdf"];

    public async Task<ExportRecord> RecordAsync(Guid nodeId, string format, string path, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var bookId = await NodeWorkbenchService.ResolveBookAncestorIdAsync(db, nodeId, ct) ?? nodeId;
        var version = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == nodeId)
            .Select(n => n.Version).FirstOrDefaultAsync(ct);
        var row = new ExportRecord
        {
            BookId = bookId,
            Format = format,
            Version = version,
            BookFingerprint = await BookFingerprint.ComputeAsync(db, bookId, ct),
            Path = path,
            At = DateTime.UtcNow,
        };
        db.Exports.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }
}
