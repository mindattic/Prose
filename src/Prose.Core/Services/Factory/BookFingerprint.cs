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
    /// <summary>Hash over every non-empty beat's own text hash, in the reading order the exports and
    /// the read gate walk (<see cref="NodeWorkbenchService.WalkAsync"/>). Computed from live
    /// <see cref="Beat.Text"/> rather than the stored TextHash column, which can be null or stale for
    /// a beat that predates stamping.
    ///
    /// <para>The walk, not the leaf set: a leaf walk skipped beats hanging directly on a node that
    /// also has children (a chapter with scene children, a book root with chapters), so an edit to
    /// one of those exported beats left F7 passing on a stale press; and it included Drafts-bucket
    /// (Kind "book") children the exports never print, so editing a draft un-pressed the book. For a
    /// book whose beats all hang on leaves the hash is unchanged.</para></summary>
    public static async Task<string> ComputeAsync(ProseDbContext db, Guid nodeId, CancellationToken ct = default)
    {
        var ordered = new List<NodeWorkbenchService.OrderedBeat>();
        await NodeWorkbenchService.WalkAsync(db, nodeId, ordered, new HashSet<Guid>(), false, ct);
        var texts = ordered
            .Where(o => !string.IsNullOrEmpty(o.Beat.Text))
            .Select(o => o.Beat.Text);
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
/// and therefore after it passed the read gate — one <see cref="ExportRecord"/> row with the exported
/// node's fingerprint at that moment. Stations F7 and A compare the book's own rows with the book as
/// it stands now, so only an export of the whole book can press it.
/// </summary>
public sealed class ExportRecorder(IDbContextFactory<ProseDbContext> dbFactory)
{
    public static readonly string[] BookFormats = ["docx", "epub", "pdf"];

    public async Task<ExportRecord> RecordAsync(Guid nodeId, string format, string path, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // The row belongs to the node that was exported. Filed against its book at the book's
        // fingerprint, one chapter's mp3 passed station A for all 46 units (found 2026-09-23).
        var version = await db.Nodes.IgnoreQueryFilters().AsNoTracking().Where(n => n.Id == nodeId)
            .Select(n => n.Version).FirstOrDefaultAsync(ct);
        var row = new ExportRecord
        {
            BookId = nodeId,
            Format = format,
            Version = version,
            BookFingerprint = await BookFingerprint.ComputeAsync(db, nodeId, ct),
            Path = path,
            At = DateTime.UtcNow,
        };
        db.Exports.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }
}
