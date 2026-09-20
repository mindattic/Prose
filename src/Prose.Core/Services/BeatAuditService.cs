using Microsoft.Extensions.Logging;

namespace Prose.Core.Services;

/// <summary>
/// Runs all four story-lens audits (causality, affect→behavior, interpersonal dynamics, craft
/// quality) on a node and consolidates the findings into a triage result.
///
/// Used by <c>AutoRunCli</c> after each chapter or flat-node expansion to detect
/// BLOCKERs before they compound across subsequent beats.
///
/// <see cref="CraftQualityService"/> added 2026-08-13 (plan "Separating rigor from fluidity") —
/// absorbs the scene-shape/structural craft judgments <c>StoryScienceService</c> used to inject
/// as a preemptive lecture on every beat regardless of need. Same seam as the other three:
/// read the finished beat in context, flag only where something's actually wrong.
/// </summary>
public class BeatAuditService(
    CausalityService causality,
    AffectBehaviorService affect,
    InterpersonalDynamicsService interpersonal,
    CraftQualityService craftQuality,
    ILogger<BeatAuditService> log)
{
    /// <summary>
    /// Aggregated result from all four lenses. <see cref="FailedLensCount"/> is non-zero
    /// when one or more lenses could not reach the LLM — callers should warn the user when
    /// coverage is degraded rather than treating a partial result as a clean pass.
    /// </summary>
    public record BeatAuditResult(
        bool IsClean,
        IReadOnlyList<LensIssue> Blockers,
        IReadOnlyList<LensIssue> Moderates,
        int FailedLensCount = 0,
        int TotalLensCount = 0);

    /// <summary>
    /// Runs all four lenses concurrently (Task.WhenAll) and consolidates findings.
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
            (nameof(CraftQualityService),          () => craftQuality.RunAsync(nodeId, ct: ct)),
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

        if (failed == lenses.Length)
        {
            // IsClean:false, not true — "every lens failed to run" is not evidence of
            // cleanliness, it's the absence of any evidence at all. The prior code returned
            // IsClean:true here (this exact class doc comment warns against exactly that:
            // "rather than treating a partial result as a clean pass"), so AutoRunCli would
            // print "audit clean — no blockers" and silently skip the self-repair pass during a
            // total LLM outage — the compounding-errors scenario this service exists to prevent.
            log.LogError("[BeatAuditService] All {Count} lenses failed on node {NodeId} — skipping repair", lenses.Length, nodeId);
            return new(IsClean: false, Blockers: [], Moderates: [], FailedLensCount: lenses.Length, TotalLensCount: lenses.Length);
        }

        var allIssues = results.Where(r => !r.Failed).SelectMany(r => r.Issues).ToList();

        static bool IsBlocker(string? s)  => string.Equals(s, "High",    StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(s, "BLOCKER", StringComparison.OrdinalIgnoreCase);
        static bool IsModerate(string? s) => string.Equals(s, "Medium",   StringComparison.OrdinalIgnoreCase)
                                          || string.Equals(s, "MODERATE", StringComparison.OrdinalIgnoreCase);

        var blockers  = allIssues.Where(i => IsBlocker(i.Severity)).ToList();
        var moderates = allIssues.Where(i => IsModerate(i.Severity)).ToList();
        return new(blockers.Count == 0, blockers, moderates, FailedLensCount: failed, TotalLensCount: lenses.Length);
    }
}
