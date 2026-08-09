using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Services.Audit;

namespace Prose.Core.Services;

public enum BookHealthTier { Free, Deep, Full }

/// <summary>One battery check's run outcome — did it complete, and if not, why. The check's
/// own service is the one that filed any Findings; this is just "did the call succeed."</summary>
public sealed record CheckOutcome(string Name, bool Success, string Note);

/// <summary>One FindingCategory's contribution to the Findings-deduction term — the raw
/// severity-weighted sum before the per-category cap, and what actually got deducted.</summary>
public sealed record SiiCategoryDeduction(string Category, int High, int Medium, int Low, int RawPoints, int CappedPoints);

/// <summary>One of the three named rate metrics — a book-wide rate that discrete Findings
/// counts don't distinguish (e.g. "barely over the line" vs "comfortably clean").</summary>
public sealed record SiiRateAdjustment(string Metric, string Value, int Adjustment);

public sealed record BookHealthReport(
    Guid NodeId, string Slug, string Title, string Tier, DateTime GeneratedAt,
    IReadOnlyList<CheckOutcome> Checks,
    int Sii, string Grade,
    IReadOnlyList<SiiCategoryDeduction> FindingsDeduction,
    IReadOnlyList<SiiRateAdjustment> RateAdjustments,
    IReadOnlyList<string> ExcludedFromScore);

/// <summary>
/// The single "does this book work" battery + score. Runs the FREE/DEEP/FULL tiers of
/// checks — each check's own service files Findings as a side effect; this service does not
/// duplicate that — then computes the Structural Integrity Index (SII) as a fully
/// deterministic rollup over OPEN Findings plus three named rate metrics. NOT a vote, NOT an
/// LLM opinion score (SS-A44 compliant): the SII arithmetic itself makes no LLM calls — it
/// only counts what earlier LLM-assisted or mechanical checks already filed.
///
/// A handful of checks (BeatVerificationService, SwainAuditService, ChekhovAuditService,
/// TimelineConsistencyService, and selectively BeatCoordinationService) do not file Findings
/// on their own, so this service wraps their results into Findings itself, mapping each
/// service's own severity vocabulary onto FindingSeverity. BeatGranularityService and
/// BeatProseMetricsService are deliberately NOT wrapped — they are pacing/distribution
/// diagnostics, not correctness defects, and stay informational-only. EmotionalDepthService
/// is called (its own blocking-dimension Findings still count) but its aggregate
/// EmotionalDepthScore is excluded from the SII arithmetic — see remarks on ComputeScoreAsync.
///
/// <c>Prose.Cli</c>'s <c>--audit-book</c> and the <c>book_health</c> MCP tool are both thin
/// callers of this service — this is where the logic lives exactly once.
/// </summary>
public class BookHealthService(
    IDbContextFactory<ProseDbContext> dbFactory,
    FindingsService findingsSvc,
    PlantPayoffService plantPayoff,
    PostBeatValidationService postBeatValidator,
    NounConsistencyService nounConsistency,
    TimelineConsistencyService timelineConsistency,
    BeatVerificationService beatVerification,
    BeatCoordinationService beatCoordination,
    EmotionalDepthService emotionalDepth,
    BookAuditService bookAudit,
    StructuralDiagnosticService structuralDiagnostic,
    SemanticFidelityService semanticFidelity,
    LogicSweepService logicSweep,
    BeatChecklistGateService beatChecklist,
    CanonContradictionService canonContradiction,
    AltitudeAuditService altitudeAudit,
    ComprehensionProbeService comprehensionProbe,
    StoryScopeAuditService storyScopeAudit,
    SwainAuditService swainAudit,
    ChekhovAuditService chekhovAudit,
    NarrativeScienceService narrativeScience,
    ThemeCoherenceService themeCoherence,
    BehavioralInvariantEnforcer behaviorEnforcer,
    BeatDuplicateService beatDuplicate,
    ILogger<BookHealthService> log)
{
    // ── SII formula constants ───────────────────────────────────────────────────────
    private const int HighWeight = 8, MediumWeight = 3, LowWeight = 1;
    private const int CategoryCap = 20;
    private const int RateCap = 15;
    /// <summary>Mirrors BeatChecklistGateService's own DelightExemptWordCount — a beat this
    /// short has no job that demands a DELIGHT move, so it's excluded from the landing rate.</summary>
    private const int DelightExemptWordCount = 120;
    /// <summary>Below this, check-fidelity's Seed-anchor comparison is pure noise (a title,
    /// not a bible) — same guard AuditNodeCli already used before this consolidation.</summary>
    private const int MinSeedForFidelity = 200;

    public async Task<BookHealthReport> RunAsync(Guid nodeId, BookHealthTier tier, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking()
            .Where(n => n.Id == nodeId)
            .Select(n => new { n.Id, n.Slug, n.Title, n.Seed })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var slug = node.Slug ?? nodeId.ToString("N");

        var checks = new List<CheckOutcome>();
        SwainAuditReport? swainReport = null;
        BeatChecklistGateService.ChecklistRunResult? checklistResult = null;
        StoryScopeAuditReport? storyScopeReport = null;
        bool ranEmotionalDepth = false;

        // ── FREE tier — deterministic / near-zero API cost ──────────────────────────
        await RunCheckAsync(checks, "plant-audit", () => PlantAuditAsync(nodeId, slug, ct));
        await RunCheckAsync(checks, "plant-density", () => PlantDensityAsync(nodeId, slug, ct));
        await RunCheckAsync(checks, "prose-check", () => ProseCheckAsync(db, nodeId, slug, ct));
        await RunCheckAsync(checks, "validate-nouns", async () => { await nounConsistency.ValidateAsync(nodeId, ct); });
        await RunCheckAsync(checks, "timeline-check", () => TimelineCheckAsync(nodeId, slug, ct));
        await RunCheckAsync(checks, "verify-book", () => BeatVerificationAsync(nodeId, slug, ct));
        await RunCheckAsync(checks, "coordinate", () => BeatCoordinationAsync(slug, ct));
        await RunCheckAsync(checks, "voice-consistency", () => VoiceConsistencyAsync(nodeId, slug, ct));
        await RunCheckAsync(checks, "duplicate-beats", async () => { await beatDuplicate.CheckNodeAsync(nodeId, ct: ct); });

        // ── DEEP tier — one LLM call per check per node ─────────────────────────────
        if (tier is BookHealthTier.Deep or BookHealthTier.Full)
        {
            await RunCheckAsync(checks, "examine-emotion", async () =>
            {
                await emotionalDepth.ExamineNodeAsync(nodeId, ct: ct);
                ranEmotionalDepth = true;
            });
            await RunCheckAsync(checks, "book-audit", async () => { await bookAudit.AuditAsync(nodeId, ct); });
            await RunCheckAsync(checks, "diagnose-book", async () => { await structuralDiagnostic.DiagnoseNodeAsync(nodeId, ct: ct); });
            if ((node.Seed?.Length ?? 0) >= MinSeedForFidelity)
                await RunCheckAsync(checks, "check-fidelity", async () => { await semanticFidelity.AuditNodeAsync(nodeId, ct); });
            else
                checks.Add(new CheckOutcome("check-fidelity", true, "skipped — no Seed anchor ≥200 chars; would be noise"));
            await RunCheckAsync(checks, "logic-sweep", async () => { await logicSweep.RunAsync(nodeId, ct); });
            await RunCheckAsync(checks, "craft-checklist", async () => { checklistResult = await beatChecklist.RunAsync(nodeId, force: false, ct); });
            await RunCheckAsync(checks, "check-canon", async () => { await canonContradiction.CheckNodeAsync(nodeId, proposeFixes: false, ct); });
            await RunCheckAsync(checks, "altitude-audit", async () => { await altitudeAudit.AuditAsync(nodeId, forceSynopsis: false, ct); });
            await RunCheckAsync(checks, "reader-qa", async () => { await comprehensionProbe.RunAsync(nodeId, force: false, ct); });
            await RunCheckAsync(checks, "behavior-check", () => BehaviorCheckAsync(nodeId, slug, ct));
            await RunCheckAsync(checks, "theme-coherence", () => ThemeCoherenceAsync(nodeId, slug, ct));
        }

        // ── FULL tier — heaviest multi-call audits, cost scales with book length ────
        if (tier == BookHealthTier.Full)
        {
            await RunCheckAsync(checks, "storyscope-audit", async () => { storyScopeReport = await storyScopeAudit.AuditAsync(nodeId, ct); });
            await RunCheckAsync(checks, "swain-audit", async () => { swainReport = await SwainAsync(slug, ct); });
            await RunCheckAsync(checks, "chekhov-audit", () => ChekhovAsync(nodeId, slug, ct));
            await RunCheckAsync(checks, "five-act-map", () => FiveActMapAsync(nodeId, slug, ct));
            await RunCheckAsync(checks, "dramatic-question", () => DramaticQuestionAsync(nodeId, slug, ct));
            await RunCheckAsync(checks, "sacred-flaw", () => SacredFlawAsync(nodeId, slug, ct));
        }

        var (sii, grade, deduction, rates, excluded) = await ComputeScoreAsync(
            slug, swainReport, checklistResult, storyScopeReport, ranEmotionalDepth, ct);

        return new BookHealthReport(nodeId, slug, node.Title, tier.ToString(), DateTime.UtcNow,
            checks, sii, grade, deduction, rates, excluded);
    }

    private async Task RunCheckAsync(List<CheckOutcome> checks, string name, Func<Task> action)
    {
        try { await action(); checks.Add(new CheckOutcome(name, true, "ok")); }
        catch (Exception ex)
        {
            log.LogWarning(ex, "BookHealth check '{Check}' failed", name);
            checks.Add(new CheckOutcome(name, false, ex.Message));
        }
    }

    // ── FREE-tier wrappers for services that don't self-file Findings ──────────────

    /// <summary>Runs the deterministic prose-pattern linter over every beat and files
    /// violations via the same PostBeatValidationService.QuickValidateAsync path already
    /// used on every beat save — reuses existing Findings-filing logic (same pattern the
    /// scan_book_violations MCP tool already follows) rather than duplicating it. Passes each
    /// beat's own id so findings are beat-scoped and purge-then-refiled every run (2026-08-09
    /// fix) — without it, a since-fixed violation (or a false positive resolved by a detector
    /// refinement) never cleared, the same missing-purge bug already fixed elsewhere this
    /// session, just undiscovered here until real-corpus validation of the new AI-tell checks
    /// surfaced two false positives that needed a code fix to actually clear.</summary>
    private async Task ProseCheckAsync(ProseDbContext db, Guid nodeId, string slug, CancellationToken ct)
    {
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var beats = await db.BeatNodes.AsNoTracking()
            .Where(bn => searchIds.Contains(bn.NodeId) && bn.IsEnabled && bn.Beat != null && bn.Beat.Text != "")
            .Select(bn => new { bn.BeatId, Text = bn.Beat!.Text }).ToListAsync(ct);
        foreach (var b in beats)
            await postBeatValidator.QuickValidateAsync(slug, b.Text, b.BeatId, ct);
    }

    /// <summary>PlantPayoffService.AuditAsync returns a report but never files it anywhere —
    /// the "plant-audit" FREE-tier check was calling it and discarding the result, so an
    /// orphaned plant or an untransparent payoff never became a Finding, never affected the
    /// SII, and never appeared in the Findings inbox despite the check always reporting "ok".</summary>
    private async Task PlantAuditAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        var audit = await plantPayoff.AuditAsync(nodeId, ct);
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "PLANT-AUDIT ");
        foreach (var p in audit.OrphanedPlants)
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Other, FindingSeverity.Medium,
                $"PLANT-AUDIT [orphaned] {p.Category}: planted (\"{p.PlantDescription}\") but never paid off.",
                snippet: null, suggestedFix: "Either write the payoff beat or remove the plant.");
        foreach (var p in audit.NotTransparentPayoffs)
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Other, FindingSeverity.Low,
                $"PLANT-AUDIT [opaque] {p.Category}: payoff (\"{p.PayoffDescription}\") isn't marked transparent.",
                snippet: null, suggestedFix: p.TransparencyNote);
    }

    /// <summary>PlantPayoffService/ChekhovAuditService track plant↔payoff PAIRING (does every
    /// plant get paid off) but nothing measures DISTRIBUTION — a real, separate craft smell named
    /// explicitly by the 2026-08-09 craft-services audit as having zero code representation.
    /// Fully deterministic, zero LLM calls: (1) front-loading — most plants seeded in the book's
    /// first quarter, starving the rest of the book of anything new to promise; (2) drought — a
    /// long stretch with no new plant introduced at all. Needs ≥3 plants and ≥20 beats to mean
    /// anything; below that, any distribution is just small-sample noise, not a real pattern.</summary>
    private async Task PlantDensityAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var totalBeats = await db.BeatNodes.AsNoTracking().CountAsync(bn => searchIds.Contains(bn.NodeId) && bn.IsEnabled, ct);
        if (totalBeats < 20) return;

        var audit = await plantPayoff.AuditAsync(nodeId, ct);
        var plantBeatIds = audit.AllPairs.Where(p => p.PlantBeatId != null).Select(p => p.PlantBeatId!.Value).Distinct().ToList();
        if (plantBeatIds.Count < 3) return;

        var plantNumbers = await db.Beats.AsNoTracking()
            .Where(b => plantBeatIds.Contains(b.Id))
            .Select(b => b.Number)
            .OrderBy(n => n)
            .ToListAsync(ct);

        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "PLANT-DENSITY ");

        var metrics = ComputePlantDensity(plantNumbers, totalBeats);

        if (metrics.FrontLoaded)
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.OutlineDrift, FindingSeverity.Low,
                $"PLANT-DENSITY [front-loaded]: {metrics.FrontLoadedCount}/{plantNumbers.Count} plants ({metrics.FrontLoadRate:P0}) are introduced in the book's first quarter, leaving the rest of the book seeding little that's new.",
                snippet: null, suggestedFix: "Introduce at least a few new plants later in the book, not only in the opening.");

        if (metrics.HasDrought)
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.OutlineDrift, FindingSeverity.Low,
                $"PLANT-DENSITY [drought]: a {metrics.MaxGap}-beat stretch ({metrics.MaxGapRate:P0} of the book) introduces no new plant at all.",
                snippet: null, suggestedFix: "Seed at least one new plant/detail somewhere in that stretch.");
    }

    internal readonly record struct PlantDensityMetrics(
        int FrontLoadedCount, double FrontLoadRate, bool FrontLoaded,
        int MaxGap, double MaxGapRate, bool HasDrought);

    /// <summary>Pure, DB-free so it's directly unit-testable. Thresholds (75% front-loaded,
    /// a dormant stretch ≥50% of the book) are craft judgment calls, not derived constants —
    /// tuned to flag only clearly lopsided distributions, not any book that happens to seed
    /// its plants unevenly (nearly every book does, by nature of act structure).</summary>
    internal static PlantDensityMetrics ComputePlantDensity(IReadOnlyList<int> sortedPlantBeatNumbers, int totalBeats)
    {
        var frontQuarter = totalBeats / 4;
        var frontLoadedCount = sortedPlantBeatNumbers.Count(n => n <= frontQuarter);
        var frontLoadRate = frontLoadedCount / (double)sortedPlantBeatNumbers.Count;

        var maxGap = 0;
        for (int i = 1; i < sortedPlantBeatNumbers.Count; i++)
            maxGap = Math.Max(maxGap, sortedPlantBeatNumbers[i] - sortedPlantBeatNumbers[i - 1]);
        var maxGapRate = maxGap / (double)totalBeats;

        return new PlantDensityMetrics(
            frontLoadedCount, frontLoadRate, frontLoadRate >= 0.75,
            maxGap, maxGapRate, maxGapRate >= 0.5);
    }

    /// <summary>TimelineConsistencyService returns findings but never files them — wrap here.</summary>
    private async Task TimelineCheckAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        var results = await timelineConsistency.CheckNodeAsync(nodeId, ct);
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "TIMELINE ");
        foreach (var r in results)
        {
            var sev = r.Severity.Contains("high", StringComparison.OrdinalIgnoreCase) ? FindingSeverity.High
                    : r.Severity.Contains("med", StringComparison.OrdinalIgnoreCase) ? FindingSeverity.Medium
                    : FindingSeverity.Low;
            var who = r.EntityName != null ? $" ({r.EntityName})" : "";
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Contradiction, sev,
                $"TIMELINE [{r.Kind}]{who}: {r.Detail}" + (r.BeatNumber.HasValue ? $" (beat #{r.BeatNumber})" : ""),
                snippet: null, suggestedFix: null);
        }
    }

    /// <summary>BeatVerificationService persists to the BeatVerifications table (its own
    /// Truth-Table dashboard source) but never files shared Findings — wrap the non-Pass
    /// rows here. Severity vocabulary (BLOCKER/MODERATE/MINOR) maps 1:1 onto High/Medium/Low.</summary>
    private async Task BeatVerificationAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        await beatVerification.VerifyBookAsync(slug, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var nodeIds = new List<Guid> { nodeId };
        nodeIds.AddRange(await db.Nodes.AsNoTracking().Where(n => n.ParentNodeId == nodeId).Select(n => n.Id).ToListAsync(ct));
        var beatIds = await db.BeatNodes.AsNoTracking()
            .Where(bn => nodeIds.Contains(bn.NodeId) && bn.IsEnabled).Select(bn => bn.BeatId).ToListAsync(ct);

        var failing = await db.BeatVerifications.AsNoTracking()
            .Where(v => beatIds.Contains(v.BeatId) && v.Result != "Pass" && v.Result != "Skipped")
            .Join(db.Beats.AsNoTracking(), v => v.BeatId, b => b.Id, (v, b) => new { v.BeatId, v.CheckType, v.Result, v.Severity, v.Evidence, b.Number })
            .ToListAsync(ct);

        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "VERIFY ");
        foreach (var x in failing)
        {
            var sev = x.Severity switch { "BLOCKER" => FindingSeverity.High, "MODERATE" => FindingSeverity.Medium, _ => FindingSeverity.Low };
            findingsSvc.Upsert($"node:{slug}/beat:{x.BeatId:N}", chapterId: null, FindingCategory.StructuralFailure, sev,
                $"VERIFY [{x.CheckType}] beat #{x.Number}: {x.Result} — {x.Evidence}", snippet: null, suggestedFix: null);
        }
    }

    /// <summary>BeatCoordinationService produces a bible↔blueprint↔beat coverage report but
    /// never files Findings. Only beats that HAVE prose but are missing a coordinate are a
    /// defect — a beat with no prose yet is unwritten WIP, not a quality finding. Gated on
    /// <see cref="BookScopeContext.HasBlueprint"/>: without a blueprint, every beat's
    /// CONSTRUCTION coordinate is trivially absent (flag "NO_CONSTRUCTION" on every single
    /// beat) — that's "this book hasn't run --generate-blueprint yet" (already surfaced as a
    /// gap elsewhere), not a per-beat drift defect, and filing it here would crater every
    /// unblueprinted book's SII on a false signal.</summary>
    private async Task BeatCoordinationAsync(string slug, CancellationToken ct)
    {
        var report = await beatCoordination.CoordinateAsync(slug, jsonPath: null, stamp: false, ct);
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "COORDINATE ");
        if (!report.BookScope.HasBlueprint) return;

        var uncovered = report.Beats
            .Select((b, ordinal) => (Beat: b, Ordinal: ordinal))
            .Where(x => x.Beat.ProseLength > 0 && !x.Beat.Covered)
            .ToList();

        // A beat-granular blueprint's escalation/event arrays are sized to the beat count AT
        // GENERATION TIME (BeatCoordinationService.ConstructionCapacity) and are never resized
        // when beats are later split — every beat past that capacity reads NO_CONSTRUCTION
        // regardless of prose quality. That's a stale blueprint, not hundreds of independent
        // per-beat defects: file ONE actionable gap for the drift and drop the out-of-range
        // beats from the per-beat loop, so the finding count reflects real coordination gaps.
        var capacity = report.BookScope.ConstructionCapacity;
        var beatGranular = string.Equals(report.BookScope.Granularity, "beat", StringComparison.OrdinalIgnoreCase);
        List<(BeatCoordinate Beat, int Ordinal)> perBeat = uncovered;

        if (beatGranular && capacity > 0 && capacity < report.TotalBeats)
        {
            // NO_CONSTRUCTION beyond the blueprint's footprint is the only reason these beats
            // are uncovered — UNSCORED is expected noise (no book-wide rescore has run) and
            // alone would never fail Covered; MISSING_MEANING/NO_PROSE/STUB_PROSE are real
            // per-beat defects the blueprint has nothing to do with, so those still get filed.
            var outOfRange = uncovered
                .Where(x => x.Ordinal >= capacity
                         && x.Beat.Flags.Contains("NO_CONSTRUCTION")
                         && !x.Beat.Flags.Contains("MISSING_MEANING")
                         && !x.Beat.Flags.Contains("NO_PROSE")
                         && !x.Beat.Flags.Contains("STUB_PROSE"))
                .ToList();
            if (outOfRange.Count > 0)
            {
                perBeat = uncovered.Except(outOfRange).ToList();
                findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.OutlineDrift, FindingSeverity.Medium,
                    $"COORDINATE blueprint stale: sized for {capacity} beat(s) but the book now has {report.TotalBeats} " +
                    $"({outOfRange.Count} beat(s) past the blueprint's footprint have no construction slice) — " +
                    $"run 'prose --generate-blueprint --slug {slug}' to resize.",
                    snippet: null, suggestedFix: $"prose --generate-blueprint --slug {slug}");
            }
        }

        foreach (var (b, _) in perBeat)
            findingsSvc.Upsert($"node:{slug}/beat:{b.BeatId:N}", chapterId: null, FindingCategory.OutlineDrift, FindingSeverity.Medium,
                $"COORDINATE beat #{b.Number}: written but not coordinated to its outline slot — {string.Join(", ", b.Flags)}",
                snippet: null, suggestedFix: null);
    }

    /// <summary>SwainAuditService returns per-beat classifications but never files shared
    /// Findings. File BLOCKER (Deficient) only by default — MODERATE (Ambiguous) is a soft
    /// call that would otherwise drown the inbox (same calibration lesson BeatChecklistGateService
    /// already learned filing DELIGHT flat-beats).</summary>
    private async Task<SwainAuditReport> SwainAsync(string slug, CancellationToken ct)
    {
        var report = await swainAudit.AuditAsync(slug, ct: ct);
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "SWAIN ");
        foreach (var b in report.Results.Where(b => b.Severity == "BLOCKER"))
            findingsSvc.Upsert($"node:{slug}/beat:{b.BeatId:N}", chapterId: null, FindingCategory.StructuralFailure, FindingSeverity.High,
                $"SWAIN beat #{b.Position} \"{b.Title}\": {b.Classification} missing {b.MissingElement} — {b.Note}",
                snippet: null, suggestedFix: null);
        // Surface incomplete evaluation distinctly rather than silently under-reporting — same
        // principle as the Round 6 ERROR-severity fix for BookAuditService/StoryScopeAuditService.
        // ErrorCount rows are already excluded from BlockerCount/ComplianceRate by construction;
        // this rollup just tells an operator WHY the audit covered fewer beats than expected.
        if (report.ErrorCount > 0)
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Other, FindingSeverity.Low,
                $"SWAIN [incomplete]: {report.ErrorCount}/{report.TotalBeats} beats could not be evaluated (LLM/parse errors) — re-run once resolved.",
                snippet: null, suggestedFix: null);
        return report;
    }

    /// <summary>ChekhovAuditService returns a verdict per prop cluster but never files shared
    /// Findings. ORPHANED (setup with no payoff) and FLAG (unclear) are the two verdicts that
    /// name an actual defect; DECORATION/EARNS_IT/ATMOSPHERE are passes.</summary>
    private async Task ChekhovAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        var report = await chekhovAudit.AuditAsync(nodeId, ct);
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "CHEKHOV ");
        foreach (var f in report.Findings.Where(f => f.Verdict is "ORPHANED" or "FLAG"))
        {
            var sev = f.Verdict == "ORPHANED" ? FindingSeverity.Medium : FindingSeverity.Low;
            var loc = f.Appearances.Count > 0 ? $"/beat:{f.Appearances[0].BeatLabel}" : "";
            findingsSvc.Upsert($"node:{slug}{loc}", chapterId: null, FindingCategory.StructuralFailure, sev,
                $"CHEKHOV [{f.Verdict}] {f.PropName} ({f.PropType}): {f.Reasoning}",
                snippet: null, suggestedFix: f.Fix);
        }
    }

    /// <summary>NarrativeScienceService's five-act map returns structural gaps but never files
    /// Findings. One LLM call per book — cheap, always run in FULL.</summary>
    private async Task FiveActMapAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        var map = await narrativeScience.MapFiveActStructureAsync(nodeId, ct);
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "FIVEACT ");
        if (map.Error != null) return;
        foreach (var gap in map.StructuralGaps)
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.StructuralFailure, FindingSeverity.Medium,
                $"FIVEACT {gap}", snippet: null, suggestedFix: null);
    }

    /// <summary>NarrativeScienceService's dramatic-question check ("who is this person really?")
    /// is per-beat (Will Storr's framework) and never files Findings. One LLM call PER BEAT —
    /// the most direct existing match for "does prose reveal character, not just plot," so it's
    /// worth the cost, but it's real: this is why the check lives in FULL, not DEEP. Skipped:
    /// AuditSceneEngagementAsync's 6-point scene anatomy — its mechanisms (unexpected change,
    /// cause-effect, specificity, show-not-tell) substantially overlap with what LogicSweep's
    /// causality dimension, DELIGHT moves, and StoryScope already check per-beat; adding it
    /// would mostly re-spend LLM calls on signal already covered elsewhere.</summary>
    private async Task DramaticQuestionAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beats = await GetEnabledBeatsAsync(db, nodeId, ct);

        // CheckDramaticQuestionAsync now throws on LLM/parse failure (2026-08-09 fix — it used to
        // fabricate an OverallScore=0 result with the error text landing in a field this method
        // never reads, so an outage filed a real-looking "scores 0/10" DRAMATIC-Q finding with no
        // visible error marker). Evaluate every beat first, then only purge+refile for beats that
        // succeeded — same per-check-skip principle as the SwainAuditService/BehavioralInvariantEnforcer
        // fixes: a beat whose evaluation failed keeps its prior finding untouched rather than
        // losing it to a delete with nothing to replace it.
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "DRAMATIC-Q [incomplete]");
        var evaluated = new List<(Guid BeatId, int Number, DramaticQuestionResult Result)>();
        var failedCount = 0;
        foreach (var b in beats)
        {
            try
            {
                var result = await narrativeScience.CheckDramaticQuestionAsync(b.Text, characterId: null, ct);
                evaluated.Add((b.Id, b.Number, result));
            }
            catch (Exception ex)
            {
                failedCount++;
                log.LogWarning(ex, "CheckDramaticQuestionAsync failed for beat {BeatId}", b.Id);
            }
        }

        foreach (var e in evaluated)
            findingsSvc.DeleteBySummaryPrefix($"node:{slug}/beat:{e.BeatId:N}", "DRAMATIC-Q ");
        foreach (var e in evaluated)
        {
            if (e.Result.DramaticQuestionActive && e.Result.OverallScore >= 5) continue;
            var sev = e.Result.OverallScore <= 2 ? FindingSeverity.Medium : FindingSeverity.Low;
            findingsSvc.Upsert($"node:{slug}/beat:{e.BeatId:N}", chapterId: null, FindingCategory.StructuralFailure, sev,
                $"DRAMATIC-Q beat #{e.Number}: scores {e.Result.OverallScore}/10 on \"who is this person really\" — " +
                $"{(string.IsNullOrWhiteSpace(e.Result.SubconsciousSummary) ? "no subconscious layer detected" : e.Result.SubconsciousSummary)}",
                snippet: null, suggestedFix: e.Result.ImprovementHint);
        }

        if (failedCount > 0)
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Other, FindingSeverity.Low,
                $"DRAMATIC-Q [incomplete]: {failedCount}/{beats.Count} beats could not be evaluated (LLM/parse errors) — re-run once resolved.",
                snippet: null, suggestedFix: null);
    }

    private sealed record PovRow(Guid EntityId, string EntityName);

    /// <summary>NarrativeScienceService.AnalyzeSacredFlawAsync (Will Storr's "theory of
    /// control" — the false belief a character holds about what keeps them safe/loved/powerful)
    /// existed only as a manual CLI/MCP tool; never part of the automated battery. One LLM call
    /// PER POV CHARACTER (not per-beat, not per-mention) — POV characters are the ones whose
    /// psychological engine actually has to carry an arc, so they're the only ones scoped here.
    /// "POV" is read from BeatEntityPresence.PresenceType='pov' (the same field DCM's per-beat
    /// narrator pinning already relies on, per CLAUDE.md's Register layer) rather than any
    /// Role/Archetype heuristic, so this only ever fires for characters the book itself has
    /// already marked as narrating. A "low" confidence verdict means the character's existing
    /// Psychology*/BehavioralRules fields don't yet ground a coherent flaw — a real authoring
    /// gap, not a prose defect, so this stays a Low-severity nudge rather than blocking anything.
    /// BeatEntityPresence has no EF mapping — same raw-SQL narrowing BehaviorCheckAsync uses.</summary>
    private async Task SacredFlawAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var beatIds = await db.BeatNodes.AsNoTracking()
            .Where(bn => searchIds.Contains(bn.NodeId) && bn.IsEnabled).Select(bn => bn.BeatId).ToListAsync(ct);
        if (beatIds.Count == 0) return;

        var beatParams = beatIds.Select((id, i) => new SqlParameter($"@b{i}", id)).ToArray();
        var placeholders = string.Join(",", beatParams.Select(p => p.ParameterName));
        var povRows = await db.Database.SqlQueryRaw<PovRow>(
            "SELECT DISTINCT EntityId, EntityName FROM BeatEntityPresence " +
            $"WHERE PresenceType = 'pov' AND BeatId IN ({placeholders})",
            beatParams).ToListAsync(ct);
        if (povRows.Count == 0) return;

        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "SACRED-FLAW ");
        foreach (var pov in povRows.DistinctBy(p => p.EntityId))
        {
            var result = await narrativeScience.AnalyzeSacredFlawAsync(pov.EntityId, scaffold: false, ct);
            if (string.Equals(result.Confidence, "high", StringComparison.OrdinalIgnoreCase)) continue;
            var sev = string.IsNullOrWhiteSpace(result.TheoryOfControl) || result.Diagnosis == "(parse error)"
                ? FindingSeverity.Medium : FindingSeverity.Low;
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.StructuralFailure, sev,
                $"SACRED-FLAW {pov.EntityName}: confidence={result.Confidence} — " +
                $"{(string.IsNullOrWhiteSpace(result.TheoryOfControl) ? "no theory of control identified" : result.TheoryOfControl)}",
                snippet: null,
                suggestedFix: "Ground this POV character's flaw via create_character (PsychologySecret / core_fears / core_desires / blind_spots) so future beats have a firm theory-of-control to dramatize.");
        }
    }

    private sealed record PresenceRow(Guid BeatId, Guid EntityId, string EntityName);

    /// <summary>BehavioralInvariantEnforcer checks a beat's prose against ONE character's
    /// CharacterBehavioralRules but is never run automatically — it only fires on explicit
    /// manual --behavior-check calls today. Wires it into the battery: for each character who
    /// (a) has behavioral rules defined AND (b) is actually present (not just mentioned) in a
    /// beat of this book, ask whether that beat contradicts their established rules.
    /// EnforceAsync itself is already cost-gated (zero LLM calls for a character with no rules),
    /// so this naturally costs little on books with few or no ruled characters — safe for DEEP,
    /// not reserved for FULL. BeatEntityPresence has no EF mapping, so the character list is
    /// narrowed via the real BeatEntityMentions DbSet first (this book's characters only) before
    /// a small parameterized raw-SQL query for presence-type filtering.</summary>
    private async Task BehaviorCheckAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var beatIds = await db.BeatNodes.AsNoTracking()
            .Where(bn => searchIds.Contains(bn.NodeId) && bn.IsEnabled).Select(bn => bn.BeatId).ToListAsync(ct);
        if (beatIds.Count == 0) return;

        var bookEntityIds = await db.BeatEntityMentions.AsNoTracking()
            .Where(m => beatIds.Contains(m.BeatId)).Select(m => m.EntityId).Distinct().ToListAsync(ct);
        if (bookEntityIds.Count == 0) return;
        var ruleCharacterIds = await db.CharacterBehavioralRules.AsNoTracking()
            .Where(r => bookEntityIds.Contains(r.CharacterId))
            .Select(r => r.CharacterId).Distinct().ToListAsync(ct);
        if (ruleCharacterIds.Count == 0) return;

        var charParams = ruleCharacterIds.Select((id, i) => new SqlParameter($"@c{i}", id)).ToArray();
        var placeholders = string.Join(",", charParams.Select(p => p.ParameterName));
        var presence = await db.Database.SqlQueryRaw<PresenceRow>(
            "SELECT BeatId, EntityId, EntityName FROM BeatEntityPresence " +
            "WHERE PresenceType IN ('present-active','present-passive','pov','implied-present') " +
            $"AND EntityId IN ({placeholders})",
            charParams).ToListAsync(ct);

        var beatIdSet = beatIds.ToHashSet();
        var pairs = presence.Where(p => beatIdSet.Contains(p.BeatId)).ToList();
        if (pairs.Count == 0) return;

        var pairBeatIds = pairs.Select(p => p.BeatId).Distinct().ToList();
        var beatTexts = await db.Beats.AsNoTracking()
            .Where(b => pairBeatIds.Contains(b.Id))
            .Select(b => new { b.Id, b.Number, b.Text })
            .ToDictionaryAsync(b => b.Id, ct);

        // EnforceAsync now throws on genuine evaluation failure (2026-08-09 fix — it used to
        // swallow every failure into an empty violations list, indistinguishable from "checked,
        // found nothing"). Evaluate everything FIRST, then only purge+refile BEHAVIOR findings
        // for beats that had at least one successfully-evaluated pair this run — a beat whose
        // only pair(s) failed keeps its prior findings untouched rather than losing them to a
        // delete with nothing to replace it (same principle as the SwainAuditService fix: never
        // purge-then-recreate across a failed evaluation). The "[incomplete]" rollup itself is
        // node-scoped, not beat-scoped, so it needs its own narrow, unconditional clear — the
        // per-beat deletes below never touch it (a beat-scoped prefix doesn't match the plainer
        // node-scoped path this rollup files under), so without this line a stale "N could not be
        // evaluated" finding would survive forever even after the API recovers and everything
        // succeeds again.
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "BEHAVIOR [incomplete]");
        var evaluated = new List<(Guid BeatId, int Number, Guid EntityId, List<BehaviorViolation> Violations)>();
        var failedCount = 0;
        foreach (var p in pairs)
        {
            if (!beatTexts.TryGetValue(p.BeatId, out var beat) || string.IsNullOrWhiteSpace(beat.Text)) continue;
            try
            {
                var violations = await behaviorEnforcer.EnforceAsync(beat.Text, p.EntityId, ct);
                evaluated.Add((p.BeatId, beat.Number, p.EntityId, violations));
            }
            catch (Exception ex)
            {
                failedCount++;
                log.LogWarning(ex, "BehavioralInvariantEnforcer failed for beat {BeatId} char {EntityId}", p.BeatId, p.EntityId);
            }
        }

        foreach (var beatId in evaluated.Select(e => e.BeatId).Distinct())
            findingsSvc.DeleteBySummaryPrefix($"node:{slug}/beat:{beatId:N}", "BEHAVIOR ");
        foreach (var e in evaluated)
            foreach (var v in e.Violations)
                findingsSvc.Upsert($"node:{slug}/beat:{e.BeatId:N}", chapterId: null, FindingCategory.BehaviorContradiction, FindingSeverity.Medium,
                    $"BEHAVIOR beat #{e.Number} {v.CharacterName} [{v.RuleBucket}]: {v.RuleText} — {v.Explanation}",
                    snippet: null, suggestedFix: null);

        if (failedCount > 0)
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Other, FindingSeverity.Low,
                $"BEHAVIOR [incomplete]: {failedCount}/{pairs.Count} character checks could not be evaluated (LLM errors) — re-run once resolved.",
                snippet: null, suggestedFix: null);
    }

    /// <summary>ThemeCoherenceService.AnalyzeAsync (McKee/Truby controlling-idea framework) is a
    /// brand-new check — before this, "theme" only existed in the pipeline as StoryScienceService's
    /// generation-time PROHIBITION ("don't impose theme early, let it emerge"), never as anything
    /// audited after the fact. One LLM call per book (Seed + Bible + opening/closing beats only,
    /// not a per-beat scan) — cheap enough for DEEP, not reserved for FULL. Two distinct failure
    /// modes get two distinct findings: commentary is a craft violation (theme told, not shown —
    /// High, matches CRAFT.md's existing show-don't-tell doctrine but had no automated check for
    /// this specific manifestation of it); a low-confidence controlling idea or an ending that
    /// never engages the opening's value-question is a structural softness, not a defect on its
    /// own merits (Low) — many legitimately good books end ambiguously on purpose.</summary>
    private async Task ThemeCoherenceAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        var result = await themeCoherence.AnalyzeAsync(nodeId, ct);
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "THEME ");
        if (result.Error != null) return;

        if (result.ThemeStatedAsCommentary)
        {
            var where = result.CommentaryBeatNumber.HasValue ? $" (beat #{result.CommentaryBeatNumber})" : "";
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.StructuralFailure, FindingSeverity.High,
                $"THEME [told-not-shown]{where}: \"{result.CommentaryQuote}\" states the theme as commentary instead of dramatizing it.",
                snippet: result.CommentaryQuote, suggestedFix: "Cut the stated moral; let the preceding action/consequence carry the meaning.");
        }

        if (string.Equals(result.Confidence, "low", StringComparison.OrdinalIgnoreCase))
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.StructuralFailure, FindingSeverity.Low,
                $"THEME [unclear]: no coherent controlling idea identifiable from Seed/Bible/bookend beats — {result.Diagnosis}",
                snippet: null, suggestedFix: "State the book's controlling idea in the NodeBible via set_book_bible so future beats have a claim to dramatize.");

        if (!result.EndingEngagesOpening)
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.StructuralFailure, FindingSeverity.Low,
                $"THEME [ending-drift]: closing beats don't appear to engage the opening's value-question (\"{result.ControllingIdea}\") — {result.Diagnosis}",
                snippet: null, suggestedFix: null);
    }

    private const int MinPovBeatsForVoiceFingerprint = 6;

    /// <summary>Ports WritingQualityService.CheckVoiceCadence's algorithm (see
    /// VoiceFingerprintAnalyzer) onto the live Nodes/Beats model — that service's real, working
    /// voice-drift check has never run for any book on this pipeline, stuck on the legacy
    /// Books/Chapters model and gated behind SS-A44's default-off voting path. Fully deterministic,
    /// zero LLM calls. Per POV character (BeatEntityPresence.PresenceType='pov', ≥6 of their own
    /// beats): split their beats in half by book order, build a vocabulary fingerprint from the
    /// FIRST half, then test every SECOND-half beat (theirs and everyone else's) against every
    /// character's first-half fingerprint. A second-half beat that reads closer to a DIFFERENT
    /// character's established vocabulary than its own POV's is voice drift. Needs ≥2 POV
    /// characters with enough beats each — a single-narrator book has no other voice to drift
    /// relative to, so this check has nothing to measure and is skipped, not falsely clean.</summary>
    private async Task VoiceConsistencyAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        // Keep each beat's chapter-node (bn.NodeId) — test passages are aggregated per chapter,
        // not per beat, so Jaccard has enough tokens on both sides to mean anything (see below).
        var beatChapters = await db.BeatNodes.AsNoTracking()
            .Where(bn => searchIds.Contains(bn.NodeId) && bn.IsEnabled)
            .Select(bn => new { bn.BeatId, ChapterNodeId = bn.NodeId })
            .ToListAsync(ct);
        if (beatChapters.Count == 0) return;
        var chapterOf = beatChapters.ToDictionary(x => x.BeatId, x => x.ChapterNodeId);
        var beatIds = beatChapters.Select(x => x.BeatId).ToList();

        var beatParams = beatIds.Select((id, i) => new SqlParameter($"@b{i}", id)).ToArray();
        var placeholders = string.Join(",", beatParams.Select(p => p.ParameterName));
        var povRows = await db.Database.SqlQueryRaw<PresenceRow>(
            "SELECT BeatId, EntityId, EntityName FROM BeatEntityPresence " +
            $"WHERE PresenceType = 'pov' AND BeatId IN ({placeholders})",
            beatParams).ToListAsync(ct);
        if (povRows.Count == 0) return;

        var beatIdSet = beatIds.ToHashSet();
        var pairs = povRows.Where(p => beatIdSet.Contains(p.BeatId)).ToList();
        if (pairs.Count == 0) return;

        var pairBeatIds = pairs.Select(p => p.BeatId).Distinct().ToList();
        var beatTexts = await db.Beats.AsNoTracking()
            .Where(b => pairBeatIds.Contains(b.Id) && b.Text != null && b.Text != "")
            .Select(b => new { b.Id, b.Number, b.Text })
            .ToDictionaryAsync(b => b.Id, ct);

        var byEntity = pairs
            .Where(p => beatTexts.ContainsKey(p.BeatId))
            .GroupBy(p => p.EntityId)
            .Select(g => new
            {
                EntityId = g.Key,
                Name = g.First().EntityName,
                Beats = g.Select(p => beatTexts[p.BeatId]).OrderBy(b => b.Number).ToList(),
            })
            .Where(x => x.Beats.Count >= MinPovBeatsForVoiceFingerprint)
            .ToList();
        if (byEntity.Count < 2) return;

        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "VOICE-DRIFT ");

        var fingerprints = new Dictionary<Guid, (string Name, HashSet<string> Tokens)>();
        // (EntityId, Name, ChapterNodeId, beat ids in that chapter, min/max beat number, combined tokens)
        var testPassages = new List<(Guid EntityId, string Name, Guid ChapterNodeId, List<Guid> BeatIds, int MinNumber, int MaxNumber, HashSet<string> Tokens)>();
        foreach (var e in byEntity)
        {
            var half = e.Beats.Count / 2;
            var trainTokens = e.Beats.Take(half)
                .SelectMany(b => VoiceFingerprintAnalyzer.DistinctiveTokens(b.Text!))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            fingerprints[e.EntityId] = (e.Name, trainTokens);

            // Aggregate the held-out half's beats BY CHAPTER — a single beat is too short for a
            // reliable Jaccard estimate (real-corpus check 2026-08-09: per-beat testing against
            // Death Whispers in a Cat's Ear produced 217 findings, 76% with a score gap <=0.02 and
            // 22 exact ties reported as "drift" by dictionary-iteration accident — pure noise, not
            // signal). A whole chapter's worth of one character's prose gives both sides of the
            // Jaccard comparison enough tokens to separate real drift from measurement noise.
            foreach (var chapterGroup in e.Beats.Skip(half).GroupBy(b => chapterOf[b.Id]))
            {
                var chapterBeats = chapterGroup.ToList();
                var tokens = chapterBeats.SelectMany(b => VoiceFingerprintAnalyzer.DistinctiveTokens(b.Text!))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                testPassages.Add((e.EntityId, e.Name, chapterGroup.Key, chapterBeats.Select(b => b.Id).ToList(),
                    chapterBeats.Min(b => b.Number), chapterBeats.Max(b => b.Number), tokens));
            }
        }

        foreach (var t in testPassages)
        {
            var drift = VoiceFingerprintAnalyzer.CheckDrift(t.Tokens, t.EntityId, fingerprints);
            if (drift is { Drifted: true } d)
                findingsSvc.Upsert($"node:{slug}/beat:{t.BeatIds[0]:N}", chapterId: null, FindingCategory.Voice, FindingSeverity.Low,
                    $"VOICE-DRIFT beats #{t.MinNumber}-#{t.MaxNumber} ({t.Name}): vocabulary reads closer to {d.TopMatchName} ({d.TopMatchScore:F2}) than to {t.Name}'s own established voice ({d.OwnScore:F2}).",
                    snippet: null, suggestedFix: $"Push this stretch's prose harder toward {t.Name}'s specific cadence and vocabulary.");
        }
    }

    private async Task<List<(Guid Id, int Number, string Text)>> GetEnabledBeatsAsync(
        ProseDbContext db, Guid nodeId, CancellationToken ct)
    {
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var rows = await db.BeatNodes.AsNoTracking()
            .Where(bn => searchIds.Contains(bn.NodeId) && bn.IsEnabled && bn.Beat != null && bn.Beat.Text != "")
            .OrderBy(bn => bn.SortKey)
            .Select(bn => new { bn.Beat!.Id, bn.Beat.Number, bn.Beat.Text })
            .ToListAsync(ct);
        return rows.Select(r => (r.Id, r.Number, r.Text)).ToList();
    }

    // ── SII computation ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The Structural Integrity Index. Layer 1 (findings deduction) is a pure SQL aggregate
    /// over OPEN Findings scoped to this book — no LLM calls happen here, only counting.
    /// Layer 2 (rate adjustments) folds in exactly three named book-wide rates that discrete
    /// Findings counts don't distinguish. EmotionalDepthScore is deliberately excluded from
    /// both layers: it is a pointwise 0-100 LLM-rubric average, exactly the failure mode
    /// this project's own adopted doctrine (docs/READER-QA.md, SS-A44) says is unreliable —
    /// folding it in, even via a floor, would smuggle an unreliable opinion back into the one
    /// number this whole system exists to make trustworthy. Its own blocking-dimension
    /// Findings (filed by EmotionalDepthService itself) still count in Layer 1 normally.
    /// </summary>
    private async Task<(int Sii, string Grade, List<SiiCategoryDeduction> Deduction, List<SiiRateAdjustment> Rates, List<string> Excluded)>
        ComputeScoreAsync(
            string slug, SwainAuditReport? swain, BeatChecklistGateService.ChecklistRunResult? checklist,
            StoryScopeAuditReport? storyScope, bool ranEmotionalDepth, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var prefix = $"node:{slug}";
        var open = await db.Findings.AsNoTracking()
            .Where(f => f.FilePath.StartsWith(prefix) && (f.Status == "New" || f.Status == "Triaged"))
            .Select(f => new { f.Category, f.Severity })
            .ToListAsync(ct);

        var deduction = new List<SiiCategoryDeduction>();
        var totalDeduction = 0;
        foreach (var g in open.GroupBy(f => f.Category))
        {
            var high = g.Count(x => x.Severity == "High");
            var medium = g.Count(x => x.Severity == "Medium");
            var low = g.Count(x => x.Severity == "Low");
            var raw = high * HighWeight + medium * MediumWeight + low * LowWeight;
            var capped = Math.Min(raw, CategoryCap);
            deduction.Add(new SiiCategoryDeduction(g.Key, high, medium, low, raw, capped));
            totalDeduction += capped;
        }
        deduction.Sort((a, b) => b.CappedPoints.CompareTo(a.CappedPoints));

        var rates = new List<SiiRateAdjustment>();
        var rateTotal = 0;

        // Evaluated = TotalBeats minus ones that hit an LLM/parse error (SwainAuditReport.
        // ComplianceRate already excludes these) — skip the adjustment entirely rather than
        // recomputing compliance inline against TotalBeats, which used to double-count error
        // rows as failures (a total API outage read as "0% compliant," not "0 beats evaluated").
        var swainEvaluated = swain != null ? swain.TotalBeats - swain.ErrorCount : 0;
        if (swain != null && swainEvaluated > 0)
        {
            var compliance = swain.ComplianceRate;
            var adj = compliance >= 0.90 ? 0 : compliance >= 0.75 ? -3 : compliance >= 0.60 ? -6 : -10;
            rates.Add(new SiiRateAdjustment("Swain scene/sequel compliance", $"{compliance:P0}", adj));
            rateTotal += adj;
        }

        if (checklist != null && checklist.Beats.Count > 0)
        {
            var eligible = checklist.Beats.Where(b => b.WordCount >= DelightExemptWordCount).ToList();
            if (eligible.Count > 0)
            {
                var landing = eligible.Count(b => b.MovesLanded.Count > 0) / (double)eligible.Count;
                var adj = landing >= 0.85 ? 0 : landing >= 0.65 ? -2 : -4;
                rates.Add(new SiiRateAdjustment("CraftChecklist DELIGHT landing rate", $"{landing:P0}", adj));
                rateTotal += adj;
            }
        }

        if (storyScope != null)
        {
            var adj = storyScope.Ready ? 0 : -5;
            rates.Add(new SiiRateAdjustment("StoryScope readiness", storyScope.Ready ? "Ready" : "Not ready", adj));
            rateTotal += adj;
        }

        var cappedRateTotal = Math.Max(rateTotal, -RateCap);

        var excluded = new List<string>();
        if (ranEmotionalDepth)
            excluded.Add("EmotionalDepthScore — informational only, not scored into SII (GLMZ/CODA-register-specific, " +
                         "near-zero corpus coverage; its own blocking-dimension findings above still count).");

        var sii = Math.Clamp(100 - totalDeduction + cappedRateTotal, 0, 100);
        var grade = sii >= 90 ? "A" : sii >= 80 ? "B" : sii >= 70 ? "C" : sii >= 60 ? "D" : "F";

        return (sii, grade, deduction, rates, excluded);
    }
}
