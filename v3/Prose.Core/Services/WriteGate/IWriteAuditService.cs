namespace Prose.Core.Services.WriteGate;

/// <summary>
/// Post-save, fire-and-forget dispatch of a committed write to the slower, judgment-based checks
/// (blast radius + narrow logic sweep, continuity re-extraction, semantic drift, single-entity
/// dedup scan) that used to be optional constructor hooks on <c>NodeWorkbenchService</c> — wired
/// once here so every write path gets them, not just the ones that remembered to pass them in.
/// Implementations must never throw back into the caller's save path (the save already
/// committed); log failures instead of propagating them.
/// </summary>
public interface IWriteAuditService
{
    Task DispatchAsync(WriteEvent evt, CancellationToken ct);
}
