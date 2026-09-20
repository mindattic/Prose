using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.WriterUi.Services;

/// <summary>
/// Backs the Spine view — the one screen that answers "can the order of events be summed up
/// simply?". It puts three things that have always lived apart side by side:
///
/// <list type="number">
///   <item>the book's <see cref="Node.NodeOutline"/>, editable in place;</item>
///   <item>every beat in manuscript order with its event summary;</item>
///   <item>the <see cref="EntityStateEvent"/> ledger rows each beat emitted, editable.</item>
/// </list>
///
/// <para>Why it exists: BCODA4 was reconstructed from database history and carried no outline at
/// all, and nothing in the toolchain let a human read the order of events end to end. Two full
/// chapters turned out to narrate the same job (the carousel line is cut twice), which is not
/// findable by reading one chapter at a time — only by seeing the spine whole.</para>
///
/// <para>Universe scoping is ambient and non-optional here for the same reason it is in
/// <see cref="WriterService"/>: an unscoped write strips entity tags. Every book-touching method
/// pins the flow universe first.</para>
/// </summary>
public sealed class SpineService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeWorkbenchService workbench,
    IUniverseContext universe)
{
    /// <summary>The outline as stored, plus when it was last written. <paramref name="Text"/> is
    /// raw — entity markup included — because that is what round-trips.</summary>
    public sealed record Outline(string Text, DateTime? GeneratedAt);

    /// <summary>One row of the spine. A chapter header row has <paramref name="BeatId"/> null and
    /// carries only <paramref name="ChapterTitle"/>; a beat row carries the rest.</summary>
    public sealed record SpineRow(
        Guid? BeatId,
        Guid ChapterNodeId,
        string ChapterTitle,
        int Ordinal,
        string? BeatTitle,
        string? EventSummary,
        string? SummaryState,
        int Chars,
        int LedgerCount);

    /// <summary>One editable ledger row, joined to its entity's display name.</summary>
    public sealed record LedgerRow(
        long Id,
        Guid EntityId,
        string EntityName,
        string EntityType,
        string AspectKey,
        string Verb,
        string? OldValue,
        string? NewValue,
        DateTime AtStoryTime,
        string Source,
        string? Snippet);

    private async Task ScopeToBookAsync(Guid bookNodeId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universeId = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId).Select(n => n.UniverseId).FirstOrDefaultAsync(ct);
        if (universeId != Guid.Empty) universe.SetFlowUniverse(universeId);
    }

    public async Task<Outline> GetOutlineAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId)
            .Select(n => new { n.NodeOutline, n.NodeOutlineGeneratedAt })
            .FirstOrDefaultAsync(ct);
        return new Outline(row?.NodeOutline ?? "", row?.NodeOutlineGeneratedAt);
    }

    /// <summary>
    /// Writes the outline back. Deliberately does NOT regenerate, re-tag, or reflow anything —
    /// the outline is the author's own summary of the order of events, and a tool that rewrote it
    /// on save would defeat the only purpose it has.
    /// </summary>
    public async Task<Outline> SaveOutlineAsync(Guid bookNodeId, string text, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == bookNodeId, ct)
                   ?? throw new InvalidOperationException($"Book node not found: {bookNodeId}");
        node.NodeOutline = text;
        node.NodeOutlineGeneratedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new Outline(node.NodeOutline ?? "", node.NodeOutlineGeneratedAt);
    }

    /// <summary>
    /// The whole book in manuscript order, chapter headers interleaved. One query for the beats,
    /// one for the chapter titles, one for the ledger counts — deliberately not per-beat, because
    /// a 507-beat book would otherwise issue 507 round trips to draw one screen.
    /// </summary>
    public async Task<List<SpineRow>> GetSpineAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);

        var ordered = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);
        if (ordered.Count == 0) return [];

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var chapterIds = ordered.Select(o => o.NodeId).Distinct().ToList();
        var chapterTitles = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => chapterIds.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n.Title, ct);

        var beatIds = ordered.Select(o => o.Beat.Id).ToList();
        var ledgerCounts = await db.EntityStateEvents.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.BeatGuid != null && beatIds.Contains(e.BeatGuid.Value))
            .GroupBy(e => e.BeatGuid!.Value)
            .Select(g => new { BeatId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BeatId, x => x.Count, ct);

        var rows = new List<SpineRow>(ordered.Count + chapterIds.Count);
        Guid lastChapter = Guid.Empty;
        for (var i = 0; i < ordered.Count; i++)
        {
            var o = ordered[i];
            var title = chapterTitles.GetValueOrDefault(o.NodeId, "(unfiled)");
            if (o.NodeId != lastChapter)
            {
                rows.Add(new SpineRow(null, o.NodeId, title, 0, null, null, null, 0, 0));
                lastChapter = o.NodeId;
            }
            rows.Add(new SpineRow(
                o.Beat.Id, o.NodeId, title, i + 1,
                o.Beat.Title,
                o.Beat.EventSummary,
                o.Beat.EventSummaryState,
                (o.Beat.Text ?? "").Length,
                ledgerCounts.GetValueOrDefault(o.Beat.Id, 0)));
        }
        return rows;
    }

    public async Task<List<LedgerRow>> GetLedgerAsync(Guid beatId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await (
            from e in db.EntityStateEvents.AsNoTracking().IgnoreQueryFilters()
            join en in db.Entities.AsNoTracking().IgnoreQueryFilters() on e.EntityId equals en.Id into g
            from en in g.DefaultIfEmpty()
            where e.BeatGuid == beatId
            orderby e.AtStoryTime, e.Id
            select new LedgerRow(
                e.Id, e.EntityId,
                en != null ? en.Name : "(unknown entity)",
                en != null ? en.EntityType : "",
                e.AspectKey, e.Verb, e.OldValue, e.NewValue,
                e.AtStoryTime, e.Source, e.Snippet)
        ).ToListAsync(ct);
    }

    /// <summary>
    /// Edits one ledger row in place. Stamps <c>Source</c> as <c>manual</c> so a later extraction
    /// pass can tell an author's correction from a machine's guess and not clobber it — the ledger
    /// is append-only by design, but a wrong row that nothing can correct is worse than a mutable
    /// one, and the author is the authority on their own canon.
    /// </summary>
    public async Task SaveLedgerRowAsync(long id, string aspectKey, string verb,
                                         string? oldValue, string? newValue,
                                         DateTime atStoryTime, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.EntityStateEvents.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id, ct)
                  ?? throw new InvalidOperationException($"Ledger row not found: {id}");
        row.AspectKey   = aspectKey.Trim();
        row.Verb        = verb.Trim();
        row.OldValue    = string.IsNullOrWhiteSpace(oldValue) ? null : oldValue;
        row.NewValue    = string.IsNullOrWhiteSpace(newValue) ? null : newValue;
        row.AtStoryTime = atStoryTime;
        row.Source      = "manual";
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteLedgerRowAsync(long id, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.EntityStateEvents.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (row is null) return;
        db.EntityStateEvents.Remove(row);
        await db.SaveChangesAsync(ct);
    }
}
