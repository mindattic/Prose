using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --fix-cross-universe-contamination [--dry-run]
///
/// Root-cause cleanup for a corpus-wide data-integrity bug found 2026-08-11 while logic-sweeping
/// M101 (SCRY): BeatEntities/BeatEntityPresence rows can carry an entity from a DIFFERENT universe
/// than the beat's own book — e.g. a SCRY book's roster tagged with a GLMZ character. This is a
/// hard violation of "Universe division absolute" (every canon/story row belongs to exactly one
/// universe — SS-LAW-15). A corpus-wide scan found 30 book/wrong-universe combinations across 20
/// books (VIGL alone had 788 contaminated rows despite passing its own logic sweep — that sweep
/// checked narrative logic, not this DB-level roster-grounding accuracy).
///
/// Root cause (confirmed by reading the live code, not guessed): the CURRENT
/// entity-matching pipeline (EmbeddingService.FindSimilarAsync via QueryUniverseId(),
/// SceneContextAssembler.GetNameIndexAsync via EF's ambient Entity query filter) already scopes
/// correctly by universe. This contamination is HISTORICAL — written before an earlier fix this
/// session ("--backfill-entity-presence silently ignoring --universe scope", commit 1aeb192f6) or
/// by some other historical process with the same class of gap. This tool cleans up the resulting
/// bad data; it does not change the (already-correct) live matching pipeline.
///
/// Action: DELETE any BeatEntities/BeatEntityPresence row whose EntityId belongs to a different
/// UniverseId than the row's own beat's book. Safe: both tables are plain roster/cache tables (no
/// EF mapping, not system-versioned, not canon) — deleting a wrong row here only means a future
/// --backfill-entity-presence run will re-populate that beat's roster correctly (or leave it empty
/// if no correct-universe entity matches, which is strictly better than a wrong-universe match).
/// </summary>
public static class FixCrossUniverseContaminationCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var dryRun = args.Contains("--dry-run");
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        // Map every leaf/intermediate node to the UniverseId of its root book ancestor, walking
        // ParentNodeId — mirrors NodeWorkbenchService's tree-walk pattern (no recursive-CTE
        // dependency here since we need it in memory to cross-reference against BeatEntities).
        var allNodes = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Select(n => new { n.Id, n.ParentNodeId, n.UniverseId, n.Kind, n.NodeCode })
            .ToListAsync();
        var byId = allNodes.ToDictionary(n => n.Id);

        var rootUniverseByNode = new Dictionary<Guid, Guid>();
        var rootCodeByNode = new Dictionary<Guid, string>();
        foreach (var n in allNodes)
        {
            var cur = n;
            var depth = 0;
            while (cur.ParentNodeId != null && byId.TryGetValue(cur.ParentNodeId.Value, out var parent) && depth++ < 10)
                cur = parent;
            rootUniverseByNode[n.Id] = cur.UniverseId;
            rootCodeByNode[n.Id] = cur.NodeCode ?? cur.Id.ToString("N")[..8];
        }

        var beatEntityRows = await db.Database.SqlQuery<ContaminationRow>($"""
            SELECT be.BeatId AS BeatId, be.EntityId AS EntityId, bn.NodeId AS NodeId, e.UniverseId AS EntityUniverse, 'BeatEntities' AS TableName
            FROM BeatEntities be
            JOIN BeatNodes bn ON bn.BeatId = be.BeatId AND bn.IsEnabled = 1
            JOIN Entities e ON e.Id = be.EntityId
            """).ToListAsync();

        var presenceRows = await db.Database.SqlQuery<ContaminationRow>($"""
            SELECT bep.BeatId AS BeatId, bep.EntityId AS EntityId, bn.NodeId AS NodeId, e.UniverseId AS EntityUniverse, 'BeatEntityPresence' AS TableName
            FROM BeatEntityPresence bep
            JOIN BeatNodes bn ON bn.BeatId = bep.BeatId AND bn.IsEnabled = 1
            JOIN Entities e ON e.Id = bep.EntityId
            """).ToListAsync();

        var bad = beatEntityRows.Concat(presenceRows)
            .Where(r => rootUniverseByNode.TryGetValue(r.NodeId, out var rootU) && rootU != r.EntityUniverse)
            .ToList();

        var byBook = bad.GroupBy(r => rootCodeByNode.GetValueOrDefault(r.NodeId, "?"))
            .OrderByDescending(g => g.Count());

        Console.WriteLine($"[fix-cross-universe-contamination] {bad.Count} contaminated row(s) across {byBook.Count()} book(s):");
        foreach (var g in byBook)
            Console.WriteLine($"  {g.Key,-10} {g.Count()} row(s)");

        if (bad.Count == 0 || dryRun)
        {
            if (dryRun) Console.WriteLine("(DRY RUN — no changes written)");
            return 0;
        }

        var beatEntityIds = bad.Where(r => r.TableName == "BeatEntities").Select(r => (r.BeatId, r.EntityId)).ToList();
        var presenceIds = bad.Where(r => r.TableName == "BeatEntityPresence").Select(r => (r.BeatId, r.EntityId)).ToList();

        int deletedBE = 0, deletedBEP = 0;
        foreach (var (beatId, entityId) in beatEntityIds)
            deletedBE += await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM BeatEntities WHERE BeatId = {beatId} AND EntityId = {entityId}");
        foreach (var (beatId, entityId) in presenceIds)
            deletedBEP += await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM BeatEntityPresence WHERE BeatId = {beatId} AND EntityId = {entityId}");

        Console.WriteLine($"[fix-cross-universe-contamination] deleted {deletedBE} BeatEntities row(s), {deletedBEP} BeatEntityPresence row(s).");
        Console.WriteLine("Run prose --backfill-entity-presence --universe <glmz|scry> to regenerate correct rosters for affected beats.");
        return 0;
    }

    private sealed class ContaminationRow
    {
        public Guid BeatId { get; set; }
        public Guid EntityId { get; set; }
        public Guid NodeId { get; set; }
        public Guid EntityUniverse { get; set; }
        public string TableName { get; set; } = "";
    }
}
