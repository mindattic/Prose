using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.WriteGate;

/// <summary>
/// Rejects a <see cref="Node.PreviousNodeId"/> write that would create a cycle in the sequel chain
/// (a book pointing at itself, directly or via a longer loop — e.g. A.Previous=B, B.Previous=A).
/// The sequel chain (MxG -> NxR -> CxC -> IxS, etc.) is read by walking <c>PreviousNodeId</c>
/// backward with no cycle guard anywhere downstream (<c>BookAuditService</c>'s Gateway/Sequel
/// commandments, cross-book consequence lookups) — a cycle would hang or silently mis-attribute
/// whichever of those walks hits it first, instead of failing loudly at the one place that
/// actually created the bad edge.
/// </summary>
public sealed class PreviousNodeCycleCheck : IWriteGateSyncCheck
{
    /// <summary>Real sequel chains are a handful of books deep; this is a generous safety cap so a
    /// pathological (but non-cyclic) long chain can't make every save do unbounded DB work.</summary>
    private const int MaxChainDepth = 200;

    public bool AppliesTo(EntityEntry entry) =>
        (entry.State == EntityState.Added || entry.State == EntityState.Modified)
        && entry.Entity is Node { PreviousNodeId: not null };

    public async Task CheckAsync(EntityEntry entry, CancellationToken ct)
    {
        var node = (Node)entry.Entity;
        var db = (ProseDbContext)entry.Context;

        var visited = new HashSet<Guid> { node.Id };
        var currentId = node.PreviousNodeId;

        for (var depth = 0; currentId != null && depth < MaxChainDepth; depth++)
        {
            if (!visited.Add(currentId.Value))
                throw new WriteGateRejectedException(
                    $"Rejected: setting node {node.Id}'s PreviousNodeId to {node.PreviousNodeId} " +
                    $"would create a cycle in the sequel chain (node {currentId.Value} is already " +
                    "in the chain being walked). A book's sequel-link ancestry must be a straight line.");

            // The next link may be part of THIS SAME SaveChanges batch — check ChangeTracker
            // before the database, same reason SelfAliasSyncCheck does (nothing is flushed yet).
            // A tracked entry's PreviousNodeId of null is a real, authoritative "end of chain" —
            // must not fall through to a (possibly stale, pre-update) DB value in that case, so
            // this checks tracked-presence separately rather than null-coalescing the two lookups.
            var trackedNext = db.ChangeTracker.Entries<Node>()
                .FirstOrDefault(e => e.Entity.Id == currentId.Value);

            currentId = trackedNext != null
                ? trackedNext.Entity.PreviousNodeId
                : await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                    .Where(n => n.Id == currentId.Value).Select(n => n.PreviousNodeId).FirstOrDefaultAsync(ct);
        }
    }
}
