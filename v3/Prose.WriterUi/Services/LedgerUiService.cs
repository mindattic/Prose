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
    NarrativeObligationService obligations)
{
    public async Task<(NarrativeObligationService.TrialBalance Balance,
                       IReadOnlyList<NarrativeObligationService.ObligationView> All)>
        LoadAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        await ScopeToBookAsync(bookNodeId, ct);

        // Whole book, not a chapter: the editor's question is "what does this book still owe",
        // and a per-chapter close is a different instrument with a different answer.
        var balance = await obligations.TrialBalanceAsync(bookNodeId, chapterOrdinal: null, ct);
        var all = await obligations.ListAsync(bookNodeId, ct: ct);
        return (balance, all);
    }

    private async Task ScopeToBookAsync(Guid bookNodeId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var universeId = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => n.Id == bookNodeId)
            .Select(n => (Guid?)n.UniverseId)
            .FirstOrDefaultAsync(ct);
        if (universeId is { } id && id != Guid.Empty) universe.SetFlowUniverse(id);
    }
}
