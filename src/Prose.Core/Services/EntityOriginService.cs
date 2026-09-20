using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>
/// The one sanctioned path for changing <see cref="Data.Entities.Entity.OriginNodeId"/> — the
/// book-scoping field responsible for the 2026-08-22 cross-book contamination bug (an entity's
/// origin set to the wrong book's node id, making it visible/invisible in the wrong books'
/// candidate lists). Before this existed, four independent call sites wrote this column directly
/// (<c>Tools.EntityCrud.cs</c> x2, <c>SeedGapFillRound2Cli.cs</c>, <c>SeedGapFillRound3Cli.cs</c>,
/// <c>SeedGlmzGapFillCli.cs</c>) with no shared validation and no visibility to
/// <c>ProseDbContext</c>'s write-gate hook beyond the bare fact that a save happened. Routing
/// through here (project plan "Make Prose.Hub the real gatekeeper", 2026-08-22 Phase 0) means the
/// write-gate's <c>WriteSubject.EntityOrigin</c> classification always fires for this specific
/// column change, not just "some Entity row changed."
/// </summary>
public class EntityOriginService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<EntityOriginService> log;

    public EntityOriginService(IDbContextFactory<ProseDbContext> dbFactory, ILogger<EntityOriginService> log)
    {
        this.dbFactory = dbFactory;
        this.log = log;
    }

    /// <summary>
    /// Sets the entity's <c>OriginNodeId</c> to <paramref name="originNodeId"/> (pass
    /// <c>null</c> to make it universe-wide/shared again). No-ops if the value is already
    /// correct — matches every existing raw-write call site's own no-op guard, so behavior is
    /// unchanged for callers migrated onto this method.
    /// </summary>
    public async Task SetEntityOriginAsync(Guid entityId, Guid? originNodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var entity = await db.Entities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == entityId, ct)
            ?? throw new InvalidOperationException($"Entity {entityId} not found.");

        if (entity.OriginNodeId == originNodeId) return;

        var previous = entity.OriginNodeId;
        entity.OriginNodeId = originNodeId;
        await db.SaveChangesAsync(ct);
        log.LogInformation("Entity {EntityId} OriginNodeId: {Previous} -> {New}", entityId, previous, originNodeId);
    }
}
