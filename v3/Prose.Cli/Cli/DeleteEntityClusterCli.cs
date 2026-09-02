using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --delete-entity-cluster --root &lt;entityGuid&gt; --universe &lt;slug&gt; --confirm &lt;entityCount&gt;
///
/// The execution half of the archive-then-delete workflow (see
/// <see cref="ExportEntityClusterCli"/> — always run that first, read its output, and pass
/// --confirm exactly the entity count it printed). Re-walks the cluster fresh via
/// <see cref="EntityClusterWalker"/> and aborts if the count has drifted from --confirm, so a
/// stale export can't silently authorize deleting a bigger/different set than was reviewed.
///
/// Deletes in two passes inside one transaction: every internal Edge first (Edge.SourceId/
/// TargetId -&gt; Entity is DeleteBehavior.Restrict, so a live edge blocks its endpoints'
/// deletion), then every Entity — but only after <see cref="EntityDeleteGuard"/> confirms EACH
/// one has zero remaining non-cascading references anywhere in the DB. Any blocker aborts the
/// whole transaction rather than partially deleting the cluster — that would mean the walk
/// pulled in something still actually in use, which needs investigation, not force-delete.
/// Entities/Edges and their cascading child tables (Properties, Tags, Record, subtype rows) are
/// system-versioned, so this is recoverable via the *_History tables even though it's a real
/// hard delete (see ProseDbContext.SystemVersionedTables).
/// </summary>
public static class DeleteEntityClusterCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var rootArg = Flag(args, "--root");
        var universeSlug = Flag(args, "--universe");
        var confirmArg = Flag(args, "--confirm");
        var excludeArg = Flag(args, "--exclude");

        if (!Guid.TryParse(rootArg, out var rootId) || string.IsNullOrWhiteSpace(universeSlug) || !int.TryParse(confirmArg, out var confirmCount))
        {
            Console.Error.WriteLine("Usage: prose --delete-entity-cluster --root <entityGuid> --universe <slug> --confirm <entityCount> [--exclude <guid,guid,...>]");
            Console.Error.WriteLine("Run prose --export-entity-cluster first and pass the entity count it printed.");
            return 2;
        }

        var walls = (excludeArg ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
            .Where(g => g != null).Select(g => g!.Value).ToHashSet();

        var canonDocs = services.GetRequiredService<CanonDocumentService>();
        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null)
        {
            Console.Error.WriteLine($"[delete-entity-cluster] Unknown universe '{universeSlug}'.");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var cluster = await EntityClusterWalker.WalkAsync(db, rootId, universeId.Value, walls);

        if (cluster.Entities.Count != confirmCount)
        {
            Console.Error.WriteLine(
                $"[delete-entity-cluster] Live cluster has {cluster.Entities.Count} entities, " +
                $"--confirm said {confirmCount}. Re-run --export-entity-cluster and review before retrying.");
            return 1;
        }

        await using var tx = await db.Database.BeginTransactionAsync();

        var edgeIds = cluster.Edges.Select(e => e.Id).ToList();
        if (edgeIds.Count > 0)
        {
            await db.Edges.IgnoreQueryFilters().Where(e => edgeIds.Contains(e.Id)).ExecuteDeleteAsync();
            Console.WriteLine($"[delete-entity-cluster] Deleted {edgeIds.Count} internal edge(s).");
        }

        var clusterIds = cluster.Entities.Select(e => e.Id).ToHashSet();
        var externalBlockers = new List<string>();

        foreach (var entity in cluster.Entities)
        {
            var blocking = await EntityDeleteGuard.CheckBlockingReferencesAsync(db, entity.Id);
            foreach (var b in blocking)
            {
                // A blocker whose OWNER row also belongs to this cluster isn't a real problem —
                // that owner is being deleted in this same run too, and its own FK to the
                // subtype table (FactionId/PlaceId/CharacterId/ArchetypeId) cascades, taking the
                // bridge row with it. Only a blocker owned by something OUTSIDE the cluster
                // means a real, still-live entity has a relationship pointing at a row we're
                // about to delete — that's a genuine reason to abort.
                var ownerIds = await FindOwnerIdsAsync(db, b.Table, entity.Id);
                if (ownerIds == null)
                {
                    externalBlockers.Add(
                        $"{entity.Name} [{entity.EntityType}] ({entity.Id}): {b.Count} row(s) in " +
                        $"{b.Table}.{b.Column} — unrecognized table shape, can't verify containment, needs manual review.");
                    continue;
                }
                var external = ownerIds.Where(id => !clusterIds.Contains(id)).ToList();
                if (external.Count > 0)
                {
                    var owners = await db.Entities.IgnoreQueryFilters().AsNoTracking()
                        .Where(e => external.Contains(e.Id)).Select(e => new { e.Id, e.Name, e.EntityType }).ToListAsync();
                    var ownerDesc = string.Join(", ", owners.Select(o => $"{o.Name} [{o.EntityType}] ({o.Id})"));
                    externalBlockers.Add(
                        $"{entity.Name} [{entity.EntityType}] ({entity.Id}): still referenced by {b.Table}.{b.Column} " +
                        $"from OUTSIDE the cluster: {ownerDesc}");
                }
            }
        }

        if (externalBlockers.Count > 0)
        {
            await tx.RollbackAsync();
            Console.Error.WriteLine("[delete-entity-cluster] Aborted — the following entities are still referenced from outside the cluster:");
            foreach (var b in externalBlockers) Console.Error.WriteLine($"  {b}");
            return 1;
        }

        // All blocking rows are self-contained (owner also in the cluster) — clean them up
        // explicitly rather than relying on delete order, since ExecuteDelete on Entities below
        // is one batched statement, not a per-row cascade walk in a specific sequence.
        await db.Set<ArchetypeSimilar>().Where(x => x.SimilarArchetypeId != null && clusterIds.Contains(x.SimilarArchetypeId.Value)).ExecuteDeleteAsync();
        await db.Set<ArchetypeOpposite>().Where(x => x.OppositeArchetypeId != null && clusterIds.Contains(x.OppositeArchetypeId.Value)).ExecuteDeleteAsync();
        await db.Set<PlaceRelatedEntity>().Where(x => x.RelatedEntityId != null && clusterIds.Contains(x.RelatedEntityId.Value)).ExecuteDeleteAsync();
        await db.Set<FactionRelationshipRow>().Where(x => x.TargetFactionId != null && clusterIds.Contains(x.TargetFactionId.Value)).ExecuteDeleteAsync();
        await db.Set<CharacterHomeTurf>().Where(x => x.PlaceId != null && clusterIds.Contains(x.PlaceId.Value)).ExecuteDeleteAsync();
        await db.Set<PlaceAdjacency>().Where(x => x.NeighborId != null && clusterIds.Contains(x.NeighborId.Value)).ExecuteDeleteAsync();

        // Re-check: anything still blocking after the self-contained cleanup above is a real
        // gap in this method's table coverage, not a false positive — abort rather than force.
        var remaining = new List<string>();
        foreach (var entity in cluster.Entities)
        {
            var blocking = await EntityDeleteGuard.CheckBlockingReferencesAsync(db, entity.Id);
            if (blocking.Count > 0)
                remaining.Add(EntityDeleteGuard.DescribeBlockers($"{entity.Name} [{entity.EntityType}]", entity.Id, blocking));
        }
        if (remaining.Count > 0)
        {
            await tx.RollbackAsync();
            Console.Error.WriteLine("[delete-entity-cluster] Aborted — still blocked after self-contained bridge-row cleanup:");
            foreach (var b in remaining) Console.Error.WriteLine($"  {b}");
            return 1;
        }

        var entityIds = cluster.Entities.Select(e => e.Id).ToList();
        await db.Entities.IgnoreQueryFilters().Where(e => entityIds.Contains(e.Id)).ExecuteDeleteAsync();

        await tx.CommitAsync();

        Console.WriteLine($"[delete-entity-cluster] Deleted {cluster.Entities.Count} entities and {edgeIds.Count} edges from '{universeSlug}'.");
        Console.WriteLine("Recoverable via the Entities_History/Edges_History temporal tables if this was a mistake.");
        return 0;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }

    /// <summary>Maps a blocking table (from <see cref="EntityDeleteGuard"/>'s report) to the
    /// owner-side entity id of each row referencing <paramref name="targetId"/> — e.g. for
    /// PlaceAdjacencies.NeighborId, the owner is the OTHER Place (PlaceAdjacencies.PlaceId). Null
    /// means this table shape isn't recognized by this method, which the caller must treat as a
    /// hard stop, not an assumption of safety. Covers exactly the tables this one-time cluster
    /// cleanup has actually hit live (2026-09-02) — not a general-purpose catalog.</summary>
    static async Task<List<Guid>?> FindOwnerIdsAsync(ProseDbContext db, string table, Guid targetId) => table switch
    {
        "ArchetypeSimilars" => await db.Set<ArchetypeSimilar>().Where(x => x.SimilarArchetypeId == targetId).Select(x => x.ArchetypeId).ToListAsync(),
        "ArchetypeOpposites" => await db.Set<ArchetypeOpposite>().Where(x => x.OppositeArchetypeId == targetId).Select(x => x.ArchetypeId).ToListAsync(),
        "PlaceRelatedEntities" => await db.Set<PlaceRelatedEntity>().Where(x => x.RelatedEntityId == targetId).Select(x => x.PlaceId).ToListAsync(),
        "FactionRelationships" => await db.Set<FactionRelationshipRow>().Where(x => x.TargetFactionId == targetId).Select(x => x.FactionId).ToListAsync(),
        "CharacterHomeTurfs" => await db.Set<CharacterHomeTurf>().Where(x => x.PlaceId == targetId).Select(x => x.CharacterId).ToListAsync(),
        "PlaceAdjacencies" => await db.Set<PlaceAdjacency>().Where(x => x.NeighborId == targetId).Select(x => x.PlaceId).ToListAsync(),
        _ => null,
    };
}
