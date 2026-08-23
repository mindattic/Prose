using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Prose.Core.Services.WriteGate;

/// <summary>
/// A fast, deterministic, pre-save check run synchronously inside
/// <c>ProseDbContext.SaveChanges(Async)</c> for every tracked entity entry, before the underlying
/// save commits. Implementations must be cheap (no LLM calls, no slow cross-table scans) — this
/// runs on every single write in the application. Slow, judgment-based checks belong in
/// <see cref="IWriteAuditService"/> instead, dispatched after the save succeeds.
/// </summary>
public interface IWriteGateSyncCheck
{
    /// <summary>
    /// Returns true if this check applies to the given entry (e.g. it only cares about
    /// <c>CharacterAlias</c> inserts) — checked before <see cref="CheckAsync"/> is called.
    /// </summary>
    bool AppliesTo(EntityEntry entry);

    /// <summary>
    /// Validates the entry. Throw <see cref="WriteGateRejectedException"/> to abort the save.
    /// </summary>
    Task CheckAsync(EntityEntry entry, CancellationToken ct);
}
