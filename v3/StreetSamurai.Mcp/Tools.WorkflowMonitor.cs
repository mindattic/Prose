using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

/// <summary>
/// MCP tools for querying the prose workflow coverage monitoring system.
/// Populated by ProseWriterRouter — use these after generating beats via that entry point.
/// </summary>
[McpServerToolType]
public class WorkflowMonitorTools(
    WorkflowMonitorService monitor,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    [McpServerTool, Description("Get prose service coverage for a strand. Returns which services (Pacing, StoryMethodology, PlantPayoff, StoryAudit, Combat) were active when beats were written, and flags gaps where applicable services weren't used.")]
    public async Task<string> workflow_status(
        [Description("Strand slug (e.g. 'ATTE', 'BCODA')")] string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var strand = await db.Strands.AsNoTracking()
            .Where(s => s.Slug == slug || s.StrandCode == slug)
            .Select(s => new { s.Id, s.Title })
            .FirstOrDefaultAsync();
        if (strand == null) return $"Strand not found: {slug}";

        var report = await monitor.GetStrandCoverageAsync(strand.Id);
        return JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Get global prose workflow coverage across all strands. Returns per-service utilization rates and a list of strands with coverage gaps.")]
    public async Task<string> workflow_status_global()
    {
        var stats = await monitor.GetGlobalStatsAsync();
        var gaps  = await monitor.GetAllStrandsWithGapsAsync();
        return JsonSerializer.Serialize(new { GlobalStats = stats, StrandsWithGaps = gaps },
            new JsonSerializerOptions { WriteIndented = true });
    }

    [McpServerTool, Description("Get the detected beat mode log for a strand. Shows how each beat was classified (Narrative/Combat/EmotionalClimax/Dialogue/Transition/Revelation) and the confidence level.")]
    public async Task<string> workflow_beat_modes(
        [Description("Strand slug")] string slug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var strand = await db.Strands.AsNoTracking()
            .Where(s => s.Slug == slug || s.StrandCode == slug)
            .Select(s => new { s.Id })
            .FirstOrDefaultAsync();
        if (strand == null) return $"Strand not found: {slug}";

        var beatIds = await db.StrandBeats.AsNoTracking()
            .Where(sb => sb.StrandId == strand.Id)
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
