using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

/// <summary>
/// MCP tools for querying the prose workflow coverage monitoring system.
/// Populated by ProseWriterRouter — use these after generating beats via that entry point.
/// </summary>
[McpServerToolType]
public class WorkflowMonitorTools(
    WorkflowMonitorService monitor,
    IDbContextFactory<ProseDbContext> dbFactory,
    HubInvoker hub)
{
    [McpServerTool, Description("Get prose service coverage for a node. Returns which services (Pacing, StoryMethodology, PlantPayoff, StoryAudit, Combat) were active when beats were written, and flags gaps where applicable services weren't used.")]
    public Task<string> workflow_status(
        [Description("Node slug (e.g. 'ATTE', 'BCODA')")] string slug) =>
        hub.InvokeAsync(nameof(WorkflowMonitorTools), nameof(workflow_statusImpl), new { slug });

    public async Task<string> workflow_statusImpl(string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.AsNoTracking()
            .Where(s => s.Slug == slug || s.NodeCode == slug)
            .Select(s => new { s.Id, s.Title })
            .FirstOrDefaultAsync();
        if (node == null) return $"Node not found: {slug}";

        var report = await monitor.GetNodeCoverageAsync(node.Id);
        return JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Get global prose workflow coverage across all nodes. Returns per-service utilization rates and a list of nodes with coverage gaps.")]
    public Task<string> workflow_status_global() =>
        hub.InvokeAsync(nameof(WorkflowMonitorTools), nameof(workflow_status_globalImpl), new { });

    public async Task<string> workflow_status_globalImpl()
    {
        var stats = await monitor.GetGlobalStatsAsync();
        var gaps  = await monitor.GetAllNodesWithGapsAsync();
        return JsonSerializer.Serialize(new { GlobalStats = stats, NodesWithGaps = gaps },
            new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Get the detected beat mode log for a node. Shows how each beat was classified (Narrative/Combat/EmotionalClimax/Dialogue/Transition/Revelation) and the confidence level.")]
    public Task<string> workflow_beat_modes(
        [Description("Node slug")] string slug) =>
        hub.InvokeAsync(nameof(WorkflowMonitorTools), nameof(workflow_beat_modesImpl), new { slug });

    public async Task<string> workflow_beat_modesImpl(string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.AsNoTracking()
            .Where(s => s.Slug == slug || s.NodeCode == slug)
            .Select(s => new { s.Id })
            .FirstOrDefaultAsync();
        if (node == null) return $"Node not found: {slug}";

        // SS-A43: beats live on chapter descendants for book-mode stories. Descend to LEAF
        // nodes, not just direct children — a split-collection book (Book -> "Chapter N"
        // container with 0 direct beats -> real chapters -> beats, e.g. BLST/ICFI/RTR/VIGL)
        // has its real chapters two levels down. Same bug class fixed in WorkflowMonitorService
        // (2026-08-09) and BackfillCoverageCli (2026-08-10).
        var childIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id);
        var beatNodeIds = childIds.Count > 0 ? childIds : new List<Guid> { node.Id };

        var beatIds = await db.BeatNodes.AsNoTracking()
            .Where(sb => beatNodeIds.Contains(sb.NodeId) && true)
            .Select(sb => sb.BeatId)
            .ToListAsync();

        var modes = await db.BeatModeLogs.AsNoTracking()
            .Where(m => beatIds.Contains(m.BeatId))
            .OrderBy(m => m.DetectedAt)
            .ToListAsync();

        return modes.Count == 0
            ? "No beat mode logs found. Generate beats via ProseWriterRouter to populate this log."
            : JsonSerializer.Serialize(modes.Select(m => new
              {
                  m.BeatId, m.Mode, m.Confidence, m.DetectionMethod,
                  DetectedAt = m.DetectedAt.ToString("u")
              }), new JsonSerializerOptions { WriteIndented = true });
    }
}
