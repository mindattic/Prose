using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Logs which prose services were active/applicable per beat write, and surfaces
/// coverage gaps per strand or globally.
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
        Guid beatId, Guid strandId, Guid universeId,
        IReadOnlyList<ServiceLogEntry> entries,
        CancellationToken ct = default)
    {
        if (strandId == Guid.Empty) return;
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
                    StrandId = strandId,
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
    /// Returns a coverage report for a single strand: per-service activation rates and
    /// a list of gaps (services that were applicable but underused or never called).
    /// </summary>
    public async Task<StrandCoverageReport> GetStrandCoverageAsync(Guid strandId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var logs = await db.BeatServiceLogs.AsNoTracking()
            .Where(x => x.StrandId == strandId).ToListAsync(ct);
        var strand = await db.Strands.AsNoTracking()
            .Where(s => s.Id == strandId)
            .Select(s => new { s.Slug, s.Title })
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
                gaps.Add($"{svc}: never logged (no calls recorded for this strand — use ProseWriterRouter)");
        }

        return new StrandCoverageReport(
            StrandSlug:       strand?.Slug ?? strandId.ToString(),
            StrandTitle:      strand?.Title ?? "Unknown",
            ServiceStats:     byService,
            Gaps:             gaps,
            TotalBeatsLogged: logs.Select(x => x.BeatId).Distinct().Count());
    }

    /// <summary>
    /// Returns global per-service utilization across all strands, ordered by total call count.
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
    /// Returns a summary of all strands that have coverage gaps, ordered by gap count descending.
    /// </summary>
    public async Task<List<StrandGapSummary>> GetAllStrandsWithGapsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var strandIds = await db.BeatServiceLogs.AsNoTracking()
            .Select(x => x.StrandId).Distinct().ToListAsync(ct);

        var results = new List<StrandGapSummary>();
        foreach (var sid in strandIds)
        {
            var report = await GetStrandCoverageAsync(sid, ct);
            if (report.Gaps.Count > 0)
                results.Add(new StrandGapSummary(report.StrandSlug, report.StrandTitle, report.Gaps.Count, report.Gaps));
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

public record StrandCoverageReport(
    string StrandSlug,
    string StrandTitle,
    List<ServiceCoverageStat> ServiceStats,
    List<string> Gaps,
    int TotalBeatsLogged);

public record StrandGapSummary(
    string Slug,
    string Title,
    int GapCount,
    List<string> Gaps);
