using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Logs which prose services were active/applicable per beat write, and surfaces
/// coverage gaps per node or globally.
///
/// Called fire-and-forget by ProseWriterRouter after each beat is generated.
/// Query via ss --workflow-status or the workflow_status MCP tools.
/// </summary>
public class WorkflowMonitorService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    public record ServiceLogEntry(string Service, bool IsApplicable, bool IsActive, int BlockSizeChars);

    /// <summary>
    /// Append BeatServiceLog rows for each service entry. Silently swallows errors so
    /// prose generation is never blocked by monitoring writes.
    /// </summary>
    public async Task LogBeatActivityAsync(
        Guid beatId, Guid nodeId, Guid universeId,
        IReadOnlyList<ServiceLogEntry> entries,
        CancellationToken ct = default)
    {
        if (nodeId == Guid.Empty) return;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var now = DateTime.UtcNow;
            foreach (var e in entries)
            {
                db.BeatServiceLogs.Add(new BeatServiceLog
                {
                    UniverseId = universeId,
                    BeatId = beatId == Guid.Empty ? null : beatId,
                    NodeId = nodeId,
                    Service = e.Service,
                    WasApplicable = e.IsApplicable,
                    WasActive = e.IsActive,
                    BlockSizeChars = e.BlockSizeChars,
                    WrittenAt = now,
                });
            }
            await db.SaveChangesAsync(ct);
        }
        catch { /* non-blocking */ }
    }

    /// <summary>
    /// Returns a coverage report for a single node: per-service activation rates and
    /// a list of gaps (services that were applicable but underused or never called).
    /// </summary>
    public async Task<NodeCoverageReport> GetNodeCoverageAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Roll up child nodes: a book's coverage is the union of its chapters' logs.
        // Draft subtrees are out-of-scope material and excluded from coverage.
        // IgnoreQueryFilters(): nodeId is already a resolved, specific id by the time it
        // reaches here (from a properly-scoped slug lookup, or from GetAllNodesWithGapsAsync's
        // cross-universe sweep) — an id-exact lookup is unambiguous regardless of universe, so
        // the universe filter is redundant here and would otherwise wrongly drop non-default-
        // universe nodes when this runs with no ambient universe selected.
        var childIds = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.ParentNodeId == nodeId)
            .Select(s => s.Id).ToListAsync(ct);
        var scopeIds = new List<Guid>(childIds) { nodeId };

        var logs = await db.BeatServiceLogs.AsNoTracking()
            .Where(x => scopeIds.Contains(x.NodeId)).ToListAsync(ct);
        var node = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(s => s.Id == nodeId)
            .Select(s => new { s.Slug, s.Title, s.NodeCode, s.NodeBibleGeneratedAt })
            .FirstOrDefaultAsync(ct);

        var byService = logs
            .GroupBy(x => x.Service)
            .Select(g => new ServiceCoverageStat(
                Service:         g.Key,
                TotalCalls:      g.Count(),
                ActiveCalls:     g.Count(x => x.WasActive),
                ApplicableCalls: g.Count(x => x.WasApplicable),
                ActivationRate:  g.Count(x => x.WasApplicable) > 0
                    ? (double)g.Count(x => x.WasActive) / g.Count(x => x.WasApplicable) : 0.0))
            .OrderBy(s => s.Service)
            .ToList();

        var gaps = byService
            .Where(s => s.ApplicableCalls > 0 && s.ActivationRate < 0.5)
            .Select(s => $"{s.Service}: {s.ActivationRate:P0} activation ({s.ActiveCalls}/{s.ApplicableCalls} applicable calls)")
            .ToList();

        // Services that were applicable but never logged at all
        var knownServices = new[] { "Pacing", "StoryMethodology", "PlantPayoff", "StoryAudit", "Combat" };
        foreach (var svc in knownServices)
        {
            if (!byService.Any(s => s.Service == svc))
                gaps.Add($"{svc}: never logged (no calls recorded for this node — use ProseWriterRouter)");
        }

        // Health check: structural blueprint
        var hasBlueprint = await db.NodeStructuralBlueprints.AsNoTracking()
            .AnyAsync(b => scopeIds.Contains(b.NodeId), ct);
        if (!hasBlueprint)
            gaps.Add($"StructuralBlueprint: not found — run 'ss --generate-blueprint --slug {node?.Slug ?? "?"}'");

        // Health check: NodeBible freshness vs MarkdownFiles
        var nodeCode = node?.NodeCode ?? "";
        if (!string.IsNullOrEmpty(nodeCode) && node?.NodeBibleGeneratedAt.HasValue == true)
        {
            var mdSynced = await db.MarkdownFiles.AsNoTracking()
                .Where(m => m.Tier == "node" && m.Scope.Contains(nodeCode))
                .Select(m => (DateTime?)m.LastSyncedAt)
                .FirstOrDefaultAsync(ct);
            if (mdSynced == null)
                gaps.Add($"NodeBible: generated but never synced — run 'ss --generate-node-doc --slug {node?.Slug ?? "?"} && ss --sync-markdown'");
            else if (node!.NodeBibleGeneratedAt!.Value > mdSynced.Value)
                gaps.Add($"NodeBible: generated {node.NodeBibleGeneratedAt.Value:yyyy-MM-dd HH:mm} UTC but MarkdownFiles synced {mdSynced.Value:yyyy-MM-dd HH:mm} UTC — run 'ss --sync-markdown'");
        }

        return new NodeCoverageReport(
            NodeSlug:       node?.Slug ?? nodeId.ToString(),
            NodeTitle:      node?.Title ?? "Unknown",
            ServiceStats:     byService,
            Gaps:             gaps,
            TotalBeatsLogged: logs.Where(x => x.BeatId != null).Select(x => x.BeatId).Distinct().Count());
    }

    /// <summary>
    /// Returns the count of active entities that have no row in EntityEmbeddings.
    /// These entities are invisible to FindSimilarAsync — embedding lookup silently misses them.
    /// </summary>
    public async Task<int> GetEntityEmbeddingGapCountAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): this is a global corpus-wide check by design (every universe's
        // embedding coverage), not scoped to whichever universe happens to be ambient.
        return await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.IsActive && !db.EntityEmbeddings.Any(em => em.EntityId == e.Id))
            .CountAsync(ct);
    }

    /// <summary>
    /// Returns global per-service utilization across all nodes, ordered by total call count.
    /// </summary>
    public async Task<List<ServiceCoverageStat>> GetGlobalStatsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var logs = await db.BeatServiceLogs.AsNoTracking().ToListAsync(ct);
        return logs.GroupBy(x => x.Service)
            .Select(g => new ServiceCoverageStat(
                Service:         g.Key,
                TotalCalls:      g.Count(),
                ActiveCalls:     g.Count(x => x.WasActive),
                ApplicableCalls: g.Count(x => x.WasApplicable),
                ActivationRate:  g.Count(x => x.WasApplicable) > 0
                    ? (double)g.Count(x => x.WasActive) / g.Count(x => x.WasApplicable) : 0.0))
            .OrderByDescending(s => s.TotalCalls)
            .ToList();
    }

    /// <summary>
    /// Returns a summary of all nodes that have coverage gaps, ordered by gap count descending.
    /// </summary>
    public async Task<List<NodeGapSummary>> GetAllNodesWithGapsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var nodeIds = await db.BeatServiceLogs.AsNoTracking()
            .Select(x => x.NodeId).Distinct().ToListAsync(ct);

        var results = new List<NodeGapSummary>();
        foreach (var sid in nodeIds)
        {
            var report = await GetNodeCoverageAsync(sid, ct);
            if (report.Gaps.Count > 0)
                results.Add(new NodeGapSummary(report.NodeSlug, report.NodeTitle, report.Gaps.Count, report.Gaps));
        }
        return results.OrderByDescending(r => r.GapCount).ToList();
    }
}

public record ServiceCoverageStat(
    string Service,
    int TotalCalls,
    int ActiveCalls,
    int ApplicableCalls,
    double ActivationRate);

public record NodeCoverageReport(
    string NodeSlug,
    string NodeTitle,
    List<ServiceCoverageStat> ServiceStats,
    List<string> Gaps,
    int TotalBeatsLogged);

public record NodeGapSummary(
    string Slug,
    string Title,
    int GapCount,
    List<string> Gaps);
