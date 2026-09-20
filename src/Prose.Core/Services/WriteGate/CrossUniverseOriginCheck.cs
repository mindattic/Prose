using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.WriteGate;

/// <summary>
/// Rejects an <see cref="Entity.OriginNodeId"/> write outright when the target node belongs to a
/// different Universe than the entity itself — Universe division is absolute (CLAUDE.md,
/// "Universe division absolute"; every canon/story row belongs to exactly one Universe,
/// [SS-LAW-15]). A GLMZ character's origin book can never be a SCRY/Fantasy node and vice versa;
/// this is structurally impossible today by construction (every seed/UI path already scopes node
/// pickers to the ambient universe), but <see cref="EntityOriginService.SetEntityOriginAsync"/> —
/// the one sanctioned path — takes a bare <c>originNodeId</c> with no universe check of its own,
/// so a future caller passing a mismatched id would silently corrupt cross-universe attribution
/// with nothing to catch it. Same "make the invariant structurally impossible at the one
/// chokepoint every write passes through" reasoning as <see cref="SelfAliasSyncCheck"/>.
/// </summary>
public sealed class CrossUniverseOriginCheck : IWriteGateSyncCheck
{
    public bool AppliesTo(EntityEntry entry) =>
        (entry.State == EntityState.Added || entry.State == EntityState.Modified)
        && entry.Entity is Entity { OriginNodeId: not null };

    public async Task CheckAsync(EntityEntry entry, CancellationToken ct)
    {
        var entity = (Entity)entry.Entity;
        var originNodeId = entity.OriginNodeId!.Value;
        var db = (ProseDbContext)entry.Context;

        // The origin node may be part of THIS SAME SaveChanges batch — check ChangeTracker
        // before the database, same reason SelfAliasSyncCheck does (nothing is flushed yet).
        var nodeUniverseId = db.ChangeTracker.Entries<Node>()
            .FirstOrDefault(e => e.Entity.Id == originNodeId)?.Entity.UniverseId;

        nodeUniverseId ??= await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Where(n => n.Id == originNodeId).Select(n => (Guid?)n.UniverseId).FirstOrDefaultAsync(ct);

        if (nodeUniverseId != null && nodeUniverseId.Value != entity.UniverseId)
            throw new WriteGateRejectedException(
                $"Rejected: entity {entity.Id} (universe {entity.UniverseId}) cannot set OriginNodeId " +
                $"to node {originNodeId} (universe {nodeUniverseId.Value}) — an entity's origin node " +
                "must belong to the entity's own Universe. Universe division is absolute.");
    }
}
