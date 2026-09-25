using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Obligations;

namespace Prose.WriterUi.Services;

/// <summary>
/// The obligation ledger, for the editor.
///
/// <para>Two reads in one call, because the panel needs both and they answer different questions:
/// the trial balance says whether the book is square and — crucially — whether anyone has LOOKED,
/// while the list is the rows themselves. Fetching them separately would let the panel show a
/// balance from one moment and rows from another.</para>
///
/// <para>Read-only on purpose. Opening, closing, dropping and deferring are authored decisions
/// that carry an actor and an audit trail; the way to make one from the editor is to talk about
/// the beat, which records the reasoning beside the decision.</para>
/// </summary>
public sealed class LedgerUiService(
    IDbContextFactory<ProseDbContext> dbFactory,
    IUniverseContext universe,
    NodeWorkbenchService workbench,
    NarrativeObligationService obligations)
{
    /// <summary>
    /// One obligation with the SHAPE of it — how far the book held the promise open.
    /// </summary>
    /// <param name="Span">
    /// Beats between the plant and its payoff, or between the plant and the end of the written
    /// book for one still open.
    ///
    /// <para>This is the number the ledger was missing. A count of obligations says how many
    /// promises a book makes; it says nothing about weight, and weight is time under tension. A
    /// promise planted and paid two beats later is a beat, not a thread; the same promise carried
    /// three hundred beats is the spine of the book. Both are one row in a count.</para>
    /// </param>
    /// <param name="Open">True when the span is measured to the end of the book rather than to a
    /// payoff — the distance is real either way, but one of them is still growing.</param>
    public sealed record Shape(
        NarrativeObligationService.ObligationView Obligation,
        int? PlantedAt,
        int? PaidAt,
        int? Span,
        bool Open);

    public async Task<(NarrativeObligationService.TrialBalance Balance,
                       IReadOnlyList<NarrativeObligationService.ObligationView> All,
                       IReadOnlyList<Shape> Shapes)>
        LoadAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);

        // Whole book, not a chapter: the editor's question is "what does this book still owe",
        // and a per-chapter close is a different instrument with a different answer.
        var balance = await obligations.TrialBalanceAsync(bookNodeId, chapterOrdinal: null, ct);
        var all = await obligations.ListAsync(bookNodeId, ct: ct);
        return (balance, all, await ShapeAsync(bookNodeId, all, ct));
    }

    /// <summary>
    /// Distances, in beats, from the book's own reading order.
    /// </summary>
    /// <remarks>
    /// Measured in BEATS rather than chapters, which the obligation rows already carry. A chapter
    /// is not a unit of time — BCODA's run from a handful of beats to a hundred and fifty — so
    /// "planted chapter 3, paid chapter 5" describes two completely different distances depending
    /// on where in the book it happens.
    /// </remarks>
    private async Task<IReadOnlyList<Shape>> ShapeAsync(
        Guid bookNodeId,
        IReadOnlyList<NarrativeObligationService.ObligationView> all,
        CancellationToken ct)
    {
        var ordered = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);
        if (ordered.Count == 0) return [];

        var ordinal = new Dictionary<Guid, int>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++) ordinal[ordered[i].Beat.Id] = i + 1;
        var lastBeat = ordered.Count;

        var shapes = new List<Shape>(all.Count);
        foreach (var o in all)
        {
            int? planted = o.OriginBeatId is { } p && ordinal.TryGetValue(p, out var pi) ? pi : null;
            int? paid = o.ClosingBeatId is { } c && ordinal.TryGetValue(c, out var ci) ? ci : null;

            var stillOpen = paid is null;
            // An unplanted obligation has no span at all. Reporting zero would put it at the top
            // of a list sorted by distance, which is the opposite of what it means.
            var span = planted is null ? (int?)null : (paid ?? lastBeat) - planted;

            shapes.Add(new Shape(o, planted, paid, span, stillOpen));
        }

        return shapes.OrderByDescending(s => s.Span ?? -1).ToList();
    }

    // NOT async, deliberately. The flow universe is an AsyncLocal, and a value set inside an async
    // method is discarded when that method returns to its caller — so the old async version pinned
    // nothing, and every read after "await ScopeToBookAsync(...)" ran in the Hub's default
    // universe (empty or wrong rows for any book outside it). A synchronous method shares its
    // caller's execution context, so the assignment survives. One single-row lookup.
    private Task ScopeToBookAsync(Guid bookNodeId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var db = dbFactory.CreateDbContext();
        var universeId = db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId)
            .Select(n => (Guid?)n.UniverseId)
            .FirstOrDefault();
        if (universeId is { } id && id != Guid.Empty) universe.SetFlowUniverse(id);
        return Task.CompletedTask;
    }
}
