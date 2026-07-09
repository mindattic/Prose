using Microsoft.Extensions.Logging;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Runs all three story-lens audits (causality, affect→behavior, interpersonal dynamics)
/// on a node and consolidates the findings into a triage result.
///
/// Used by <c>AutoRunCli</c> after each chapter or flat-node expansion to detect
/// BLOCKERs before they compound across subsequent beats.
/// </summary>
public class BeatAuditService(
    CausalityService causality,
    AffectBehaviorService affect,
    InterpersonalDynamicsService interpersonal,
    ILogger<BeatAuditService> log)
{
    /// <summary>
    /// Aggregated result from all three lenses. <see cref="FailedLensCount"/> is non-zero
    /// when one or more lenses could not reach the LLM — callers should warn the user when
    /// coverage is degraded rather than treating a partial result as a clean pass.
    /// </summary>
    public record BeatAuditResult(
        bool IsClean,
        IReadOnlyList<LensIssue> Blockers,
        IReadOnlyList<LensIssue> Moderates,
        int FailedLensCount = 0);

    /// <summary>
    /// Runs all three lenses concurrently (Task.WhenAll) and consolidates findings.
    /// Returns a degraded result (not a full clean) when any lens fails, surfacing
    /// <see cref="BeatAuditResult.FailedLensCount"/> so callers can warn the user.
    /// </summary>
    public async Task<BeatAuditResult> AuditAsync(Guid nodeId, CancellationToken ct = default)
    {
        var lenses = new (string Name, Func<Task<LensResult>> Run)[]
        {
            (nameof(CausalityService),            () => causality.RunAsync(nodeId, ct: ct)),
            (nameof(AffectBehaviorService),        () => affect.RunAsync(nodeId, ct: ct)),
            (nameof(InterpersonalDynamicsService), () => interpersonal.RunAsync(nodeId, ct: ct)),
        };

        var tasks = lenses.Select(async l =>
        {
            try   { return (Issues: (await l.Run()).Issues, Failed: false, Name: l.Name); }
            catch (Exception ex)
            {
                // [SS-BeatAudit-001] Lens failed during self-repair audit — check LLM connectivity and node slug.
                log.LogWarning(ex, "[BeatAuditService] {Lens} failed on node {NodeId}", l.Name, nodeId);
                return (Issues: (IReadOnlyList<LensIssue>)[], Failed: true, Name: l.Name);
            }
        }).ToList();

        var results  = await Task.WhenAll(tasks);
        var failed   = results.Count(r => r.Failed);

        if (failed == 3)
        {
            log.LogError("[BeatAuditService] All three lenses failed on node {NodeId} — skipping repair", nodeId);
            return new(IsClean: true, Blockers: [], Moderates: [], FailedLensCount: 3);
        }

        var allIssues = results.Where(r => !r.Failed).SelectMany(r => r.Issues).ToList();

        static bool IsBlocker(string? s)  => string.Equals(s, "High",    StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(s, "BLOCKER", StringComparison.OrdinalIgnoreCase);
        static bool IsModerate(string? s) => string.Equals(s, "Medium",   StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(s, "MODERATE", StringComparison.OrdinalIgnoreCase);

        var blockers  = allIssues.Where(i => IsBlocker(i.Severity)).ToList();
        var moderates = allIssues.Where(i => IsModerate(i.Severity)).ToList();
        return new(blockers.Count == 0, blockers, moderates, FailedLensCount: failed);
    }
}
