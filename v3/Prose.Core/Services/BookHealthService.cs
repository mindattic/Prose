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

/// <summary>One of docs/LOGIC.md §9's five publish-readiness conditions.</summary>
public sealed record PublishReadinessCheck(string Name, bool Pass, string Detail);

/// <summary>The five-point publish-readiness convergence gate (docs/LOGIC.md §9) computed as
/// one answer — see <see cref="BookHealthService.PublishReadinessAsync"/>.</summary>
public sealed record PublishReadinessReport(Guid NodeId, string Slug, bool Ready, IReadOnlyList<PublishReadinessCheck> Checks);

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
    SanityScanService sanityScan,
    BeatRepairService beatRepair,
    NodeWorkbenchService workbench,
    ContinuityService continuity,
    ContinuityApplyService continuityApply,
    RepetitionLintService repetitionLint,
    PovVoiceAuditService povVoiceAudit,
    ChapterHookService chapterHook,
    GripePassService gripePass,
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
            .Select(n => new { n.Id, n.Slug, n.Title, n.Seed, n.NarrativeMode })
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
        await RunCheckAsync(checks, "sanity-scan", () => SanityScanAsync(nodeId, slug, ct));

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
            await RunCheckAsync(checks, "fact-ledger", () => FactLedgerAsync(slug, ct));
            await RunCheckAsync(checks, "applied-claim-drift", () => AppliedClaimDriftAsync(slug, ct));
            // 2026-08-30 fix: these three instruments (2026-08-28 tooling overhaul) already
            // self-file CraftChecklist findings from their own standalone CLI flags but were
            // never wired into the "one option" battery — --audit-book --deep/--full silently
            // never ran them. lint-prose is deterministic/zero-LLM-cost; pov-audit and
            // hook-audit are cheap batched-Haiku calls, same cost class as reader-qa above.
            await RunCheckAsync(checks, "lint-prose", async () => { await repetitionLint.LintAsync(slug, ct: ct); });
            await RunCheckAsync(checks, "pov-audit", async () => { await povVoiceAudit.AuditAsync(slug, ct: ct); });
            await RunCheckAsync(checks, "hook-audit", async () => { await chapterHook.AuditAsync(slug, ct: ct); });
        }

        // ── FULL tier — heaviest multi-call audits, cost scales with book length ────
        if (tier == BookHealthTier.Full)
        {
            await RunCheckAsync(checks, "storyscope-audit", async () => { storyScopeReport = await storyScopeAudit.AuditAsync(nodeId, ct); });
            await RunCheckAsync(checks, "swain-audit", async () => { swainReport = await SwainAsync(nodeId, slug, ct); });
            await RunCheckAsync(checks, "chekhov-audit", () => ChekhovAsync(nodeId, slug, ct));
            await RunCheckAsync(checks, "five-act-map", () => FiveActMapAsync(nodeId, slug, ct));
            await RunCheckAsync(checks, "dramatic-question", () => DramaticQuestionAsync(nodeId, slug, ct));
            // "original" only — a retelling (Paradise Lost, the Gospels: motivations fixed by the
            // source text) or historical/nonfiction book (real people/events) has no
            // author-invented psychology to "ground a flaw" in; the nudge is a category error.
            if (node.NarrativeMode == "original")
                await RunCheckAsync(checks, "sacred-flaw", () => SacredFlawAsync(nodeId, slug, ct));
            else
                checks.Add(new CheckOutcome("sacred-flaw", true, $"skipped — NarrativeMode={node.NarrativeMode}, not an invented-psychology book"));
            // 2026-08-30 fix: Reader-Proxy QA instrument 4 (the findings-only gripe jury —
            // docs/READER-QA.md) was built and self-files ReaderGripe findings from its own
            // standalone --reader-qa --gripe-pass flag, but was never wired into --audit-book.
            // RunAsync (not ProposeAndDuelFixAsync) is the report-only pass — no votes, SS-A44
            // compliant — a full multi-reader jury read, same cost class as storyscope/swain/
            // chekhov above, hence FULL tier only.
            await RunCheckAsync(checks, "gripe-pass", async () => { await gripePass.RunAsync(nodeId, ct: ct); });
        }

        var (sii, grade, deduction, rates, excluded) = await ComputeScoreAsync(
            slug, swainReport, checklistResult, storyScopeReport, ranEmotionalDepth, ct);

        return new BookHealthReport(nodeId, slug, node.Title, tier.ToString(), DateTime.UtcNow,
            checks, sii, grade, deduction, rates, excluded);
    }

    /// <summary>Sub-keys FindingCategory.CraftChecklist by its summary prefix (2026-08-30 fix —
    /// see ComputeScoreAsync's remarks) so LINT/POV+VOICE/HOOK/native craft-checklist findings
    /// each get their own capped SII bucket instead of sharing one. Every other category passes
    /// through unchanged.</summary>
    private static string SubCategoryKey(string category, string summary)
    {
        if (category != nameof(FindingCategory.CraftChecklist)) return category;
        if (summary.StartsWith("LINT ", StringComparison.Ordinal)) return "CraftChecklist:Lint";
        if (summary.StartsWith("POV ", StringComparison.Ordinal) || summary.StartsWith("VOICE ", StringComparison.Ordinal))
            return "CraftChecklist:PovVoice";
        if (summary.StartsWith("HOOK ", StringComparison.Ordinal)) return "CraftChecklist:Hook";
        return "CraftChecklist:Native";
    }

    /// <summary>
    /// docs/LOGIC.md §9's five-point publish-readiness convergence gate, computed as one answer
    /// (2026-08-30 fix) — previously nothing in the codebase computed this as a single readout;
    /// a user/agent had to manually cross-reference audit-book's findings rollup, the
    /// --until-dry round history, fact-ledger findings, and Reader-Proxy QA findings by hand.
    /// Read-only: makes no LLM calls and runs no new checks — it only reads what earlier
    /// sweep/audit/ledger runs already filed or persisted, so this is safe (and cheap) to call
    /// at any time, not just after a fresh --audit-book run.
    /// </summary>
    public async Task<PublishReadinessReport> PublishReadinessAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking()
            .Where(n => n.Id == nodeId).Select(n => new { n.Slug }).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var slug = node.Slug ?? nodeId.ToString("N");
        var prefix = $"node:{slug}";

        var openFindings = await db.Findings.AsNoTracking()
            .Where(f => f.FilePath.StartsWith(prefix) && (f.Status == "New" || f.Status == "Triaged"))
            .Select(f => new { f.Category, f.Severity, f.Summary })
            .ToListAsync(ct);

        var checks = new List<PublishReadinessCheck>();

        // 1. Zero open BLOCKER/MODERATE logic-sweep findings. Summary prefix "LOGICSWEEP " (with
        // the trailing space) distinguishes the full-book sweep's own findings from other
        // FindingCategory.Contradiction sources (e.g. ContinuityEnforcer's "CONTINUITY-VIOLATION"
        // findings, or the blast-radius mini-sweep's own "LOGICSWEEP-BLAST" prefix, which does
        // NOT match "LOGICSWEEP " since it has a hyphen, not a space, after the word).
        var sweepBad = openFindings.Count(f => f.Summary.StartsWith("LOGICSWEEP ", StringComparison.Ordinal)
            && (f.Severity == "High" || f.Severity == "Medium"));
        checks.Add(new PublishReadinessCheck("logic-sweep BLOCKER/MODERATE = 0", sweepBad == 0,
            sweepBad == 0 ? "clean" : $"{sweepBad} open BLOCKER/MODERATE logic-sweep finding(s)"));

        // 2. Zero open CONTRADICTED fact-ledger claims — "[not-extracted]" is FactLedgerAsync's
        // own honest-gap marker (never populated), not an actual contradiction; excluded.
        var contradicted = openFindings.Count(f => f.Summary.StartsWith("FACT-LEDGER [", StringComparison.Ordinal)
            && !f.Summary.Contains("[not-extracted]", StringComparison.Ordinal));
        checks.Add(new PublishReadinessCheck("fact-ledger CONTRADICTED = 0", contradicted == 0,
            contradicted == 0 ? "clean" : $"{contradicted} open contradicted fact-ledger claim(s)"));

        // 3. Two consecutive dry sweep rounds, fresh against the book's CURRENT text.
        var converged = await logicSweep.IsConvergedAsync(nodeId, ct: ct);
        checks.Add(new PublishReadinessCheck("2 consecutive dry sweep rounds", converged,
            converged ? "converged" : "not converged — run prose --logic-sweep --slug <slug> --until-dry"));

        // 4. Blast-radius recheck clean for every beat in this book — RunNarrowAsync scopes its
        // findings under "beat:{id}:blast", not "node:{slug}", so this needs its own query
        // rather than the book-prefixed openFindings list above.
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var beatIds = await db.BeatNodes.AsNoTracking()
            .Where(bn => searchIds.Contains(bn.NodeId)).Select(bn => bn.BeatId).Distinct().ToListAsync(ct);
        var blastPaths = beatIds.Select(id => $"beat:{id:N}:blast").ToHashSet();
        var openBlastPaths = await db.Findings.AsNoTracking()
            .Where(f => (f.Status == "New" || f.Status == "Triaged") && f.FilePath.EndsWith(":blast"))
            .Select(f => f.FilePath)
            .ToListAsync(ct);
        var blastBad = openBlastPaths.Count(blastPaths.Contains);
        checks.Add(new PublishReadinessCheck("blast-radius recheck clean", blastBad == 0,
            blastBad == 0 ? "clean" : $"{blastBad} open blast-radius finding(s) on this book's beats"));

        // 5. Zero open High/BLOCKER Reader-Proxy QA findings (comprehension, craft-checklist —
        // incl. the LINT/POV/VOICE/HOOK sub-instruments, gripe jury).
        var readerBad = openFindings.Count(f =>
            (f.Category == nameof(FindingCategory.ComprehensionDefect)
             || f.Category == nameof(FindingCategory.CraftChecklist)
             || f.Category == nameof(FindingCategory.ReaderGripe))
            && f.Severity == "High");
        checks.Add(new PublishReadinessCheck("Reader-Proxy QA High/BLOCKER = 0", readerBad == 0,
            readerBad == 0 ? "clean" : $"{readerBad} open High-severity Reader-Proxy QA finding(s)"));

        return new PublishReadinessReport(nodeId, slug, checks.All(c => c.Pass), checks);
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
            .Where(bn => searchIds.Contains(bn.NodeId) && true && bn.Beat != null && bn.Beat.Text != "")
            .Select(bn => new { bn.BeatId, Text = bn.Beat!.Text }).ToListAsync(ct);
        foreach (var b in beats)
            await postBeatValidator.QuickValidateAsync(slug, BeatMarkup.StripEntityTags(b.Text), b.BeatId, ct);
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
        var totalBeats = await db.BeatNodes.AsNoTracking().CountAsync(bn => searchIds.Contains(bn.NodeId) && true, ct);
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

    /// <summary>Attempts before Findings: max repair attempts per beat via BeatRepairService,
    /// matching AutoRunCli's write-time lens self-repair loop (MaxRepairAttempts there is also
    /// 2). Kept as its own constant since these are two independent, differently-scoped repair
    /// loops (write-time lenses vs. Full-tier audits), not one shared budget.</summary>
    private const int MaxSelfHealAttempts = 2;

    /// <summary>
    /// Repair-then-recheck pass shared by the Full-tier score/classification audits (2026-08-13):
    /// attempts a real targeted rewrite via <see cref="BeatRepairService.RepairAsync"/> — the same
    /// mechanism <c>AutoRunCli</c>'s write-time lens self-repair already uses live — before ever
    /// filing a Finding for a human to read. Only candidates still failing after
    /// <see cref="MaxSelfHealAttempts"/> genuine rounds are returned; those are what actually need
    /// to surface.
    ///
    /// Round-based, not per-beat: every candidate still outstanding gets a repair attempt each
    /// round (beats with multiple failing checks get ONE rewrite carrying all of them as MUST-FIX
    /// constraints, same grouping <c>AutoRunCli</c> uses for its own blockers), then
    /// <c>stillFailingAsync</c> is called ONCE per round for the whole remaining batch — not once
    /// per beat. This matters for checks whose only re-verification path is a whole-book re-audit
    /// (SWAIN's <c>AuditAsync</c>): calling that per beat per attempt would multiply an already
    /// expensive audit by the beat count. A check with a cheap single-beat re-check (VERIFY,
    /// DRAMATIC-Q) just loops internally inside its own <c>stillFailingAsync</c> implementation.
    ///
    /// A round with nothing successfully repaired stops immediately rather than re-checking
    /// (nothing changed, so nothing could have healed) or retrying (repair itself is failing, not
    /// the content). A single beat's repair exception is caught and logged per-beat, not
    /// per-round, so one bad beat can't stop the rest of the batch from healing.
    ///
    /// <c>repairAsync</c>/<c>writeTextAsync</c> are passed in explicitly (rather than closing over
    /// <c>beatRepair</c>/<c>workbench</c>) so this method is <c>internal static</c> — testable with
    /// fakes for both, no need to construct a full <see cref="BookHealthService"/> and its ~30
    /// unrelated dependencies just to prove the retry/escalation logic (see
    /// <c>[InternalsVisibleTo]</c> in Prose.Core's AssemblyInfo.cs, same pattern used for
    /// <c>SceneContextAssembler.FilterToBeatUniverseAsync</c>).
    /// </summary>
    internal static async Task<List<T>> SelfHealAsync<T>(
        Guid nodeId,
        IEnumerable<T> candidates,
        Func<T, Guid> beatIdOf,
        Func<T, LensIssue> issueOf,
        Func<Guid, Guid, IReadOnlyList<LensIssue>, CancellationToken, Task<string?>> repairAsync,
        Func<Guid, string, CancellationToken, Task> writeTextAsync,
        Func<IReadOnlyList<T>, CancellationToken, Task<List<T>>> stillFailingAsync,
        ILogger log,
        CancellationToken ct)
    {
        var remaining = candidates.ToList();
        for (var attempt = 0; attempt < MaxSelfHealAttempts && remaining.Count > 0; attempt++)
        {
            var repairedAnything = false;
            foreach (var group in remaining.GroupBy(beatIdOf).ToList())
            {
                var beatId = group.Key;
                try
                {
                    var newText = await repairAsync(beatId, nodeId, group.Select(issueOf).ToList(), ct);
                    if (string.IsNullOrWhiteSpace(newText)) continue;
                    await writeTextAsync(beatId, newText, ct);
                    repairedAnything = true;
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "[BookHealthService] Self-heal attempt {Attempt} failed for beat {BeatId}", attempt + 1, beatId);
                }
            }
            if (!repairedAnything) break;
            remaining = await stillFailingAsync(remaining, ct);
        }
        return remaining;
    }

    /// <summary>BeatVerificationService persists to the BeatVerifications table (its own
    /// Truth-Table dashboard source) but never files shared Findings — wrap the non-Pass
    /// rows here. Severity vocabulary (BLOCKER/MODERATE/MINOR) maps 1:1 onto High/Medium/Low.</summary>
    private async Task BeatVerificationAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        await beatVerification.VerifyBookAsync(slug, ct);

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // 2026-08-09 bug fix: was nodeId + DIRECT children only, so a book whose chapter is
        // itself a split Collection let BLOCKER/MODERATE verification failures in the nested
        // sub-chapters go completely unreported by the nightly health pass — a false-clean
        // report. Use the shared recursive helper (same class of bug as the ExportNodeCli
        // pre-export gate, found in the same audit).
        var nodeIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var beatIds = await db.BeatNodes.AsNoTracking()
            .Where(bn => nodeIds.Contains(bn.NodeId) && true).Select(bn => bn.BeatId).ToListAsync(ct);

        var failing = await db.BeatVerifications.AsNoTracking()
            .Where(v => beatIds.Contains(v.BeatId) && v.Result != "Pass" && v.Result != "Skipped")
            .Join(db.Beats.AsNoTracking(), v => v.BeatId, b => b.Id, (v, b) => new { v.BeatId, v.CheckType, v.Result, v.Severity, v.Evidence, b.Number })
            .ToListAsync(ct);

        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "VERIFY ");

        // BannedPattern is a unique textual anti-tell per beat — stays granular so a human can
        // jump straight to the offending beat, and is left untouched by self-heal for this first
        // pass (a categorical anti-tell rule, not a score-floor gap — see SwainAsync/
        // DramaticQuestionAsync for the same treatment being extended there next).
        foreach (var x in failing.Where(x => x.CheckType == "BannedPattern"))
        {
            var sev = x.Severity switch { "BLOCKER" => FindingSeverity.High, "MODERATE" => FindingSeverity.Medium, _ => FindingSeverity.Low };
            findingsSvc.Upsert($"node:{slug}/beat:{x.BeatId:N}", chapterId: null, FindingCategory.StructuralFailure, sev,
                $"VERIFY [BannedPattern] beat #{x.Number}: {x.Result} — {x.Evidence}", snippet: null, suggestedFix: null,
                sourceRuleVersion: BeatVerificationService.CurrentRuleVersion);
        }

        // EventType/SubplotCarrier/EscalationFloor/DeclaredPurpose are threshold/classification
        // checks (score vs. floor, category match) — the same shape LensIssue already carries, so
        // a real repair attempt runs before any Finding is filed (2026-08-13 fix). Only beats
        // still failing after MaxSelfHealAttempts feed the book-level rollup below.
        var nonBanned = failing.Where(x => x.CheckType != "BannedPattern").ToList();
        var stillFailing = await SelfHealAsync(
            nodeId, nonBanned,
            beatIdOf: x => x.BeatId,
            issueOf: x => new LensIssue(x.Number, x.CheckType, x.Evidence ?? x.Result, $"Fix the {x.CheckType} defect: {x.Evidence ?? x.Result}", x.Severity),
            repairAsync: (beatId, nid, issues, ct2) => beatRepair.RepairAsync(beatId, nid, issues, bookBibleOverride: null, ct2),
            writeTextAsync: (beatId, newText, ct2) => workbench.UpdateBeatTextAsync(beatId, newText, expectedUpdatedAt: null, ct: ct2),
            stillFailingAsync: async (remaining, ct2) =>
            {
                // VerifyBeatAsync is cheap per-beat, so re-check each distinct repaired beat
                // directly rather than re-running the whole-book audit (SwainAsync below does the
                // opposite — its only re-check IS the whole-book audit, so it re-runs that once).
                var stillFailingByBeat = new Dictionary<Guid, HashSet<string>>();
                foreach (var beatId in remaining.Select(x => x.BeatId).Distinct())
                {
                    var fresh = await beatVerification.VerifyBeatAsync(beatId, declaredPurposeBaseline: null, ct2);
                    stillFailingByBeat[beatId] = fresh
                        .Where(r => r.Result != "Pass" && r.Result != "Skipped")
                        .Select(r => r.CheckType)
                        .ToHashSet();
                }
                return remaining.Where(x => stillFailingByBeat[x.BeatId].Contains(x.CheckType)).ToList();
            },
            log,
            ct);

        foreach (var grp in stillFailing.GroupBy(x => x.CheckType))
        {
            var worstSev = grp.Any(x => x.Severity == "BLOCKER") ? FindingSeverity.High
                : grp.Any(x => x.Severity == "MODERATE") ? FindingSeverity.Medium : FindingSeverity.Low;
            var examples = string.Join("; ", grp.Take(5).Select(x => $"#{x.Number}: {x.Evidence}"));
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.StructuralFailure, worstSev,
                $"VERIFY [{grp.Key}]: {grp.Count()} beat(s) fail — worst examples: {examples}", snippet: null, suggestedFix: null,
                sourceRuleVersion: BeatVerificationService.CurrentRuleVersion);
        }
    }

    /// <summary>SanityScanService catches deterministic, no-LLM prose defects (internal
    /// node-code leaks like "NRST"/"MxG" appearing in a reader's actual book, undefined
    /// all-caps acronyms, mojibake, below-length-floor) but — despite its own header comment
    /// literally saying "fast enough to run ... as a pre-publish gate" — was wired to NOTHING:
    /// no Findings, not this nightly pass, not the pre-export gate. It only ever ran when a
    /// human remembered to type `--sanity-scan` and read the console output by hand. Found
    /// 2026-08-09 immediately after manually catching two real code leaks (NRST in Crimson &amp;
    /// Chrome, MxG in Iron &amp; Silk) this exact way — fixing those two beats does nothing to
    /// stop the same class of leak from shipping unnoticed in the next beat of any book. This
    /// wrapper is the actual fix: it makes the checker part of the automatic pipeline instead
    /// of a manual, easy-to-forget command.</summary>
    private async Task SanityScanAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        var report = await sanityScan.ScanAsync(nodeId, ct);
        SanityScanService.FileFindings(findingsSvc, slug, report);
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

        // A blueprint's escalation/event arrays are sized AT GENERATION TIME and are never
        // resized when the book grows later — every beat (beat-granular) or every beat in a
        // chapter past that capacity (chapter-granular) reads NO_CONSTRUCTION regardless of
        // prose quality. That's a stale blueprint, not hundreds of independent per-beat
        // defects: file ONE actionable gap for the drift and drop the out-of-range beats from
        // the per-beat loop, so the finding count reflects real coordination gaps.
        //
        // Bug fixed 2026-08-10: this consolidation only ever checked the beatGranular branch.
        // Chapter-granular books (VIGL, BLST, and likely others) got no consolidation at all —
        // BeatCoordinationService.ConstructionCapacity used to report the book's current
        // chapter count for these (always "big enough"), not the blueprint's actual escalation-
        // array length, so this whole branch silently never fired for them. Confirmed live:
        // VIGL's and BLST's chapter-granular blueprints both have escalation curves of length 1
        // (covering only chapter index 0) despite having 25 and 21 real chapters — every beat
        // outside chapter 0 was filing its own individual "COORDINATE beat #N" finding (307/318
        // for VIGL, 313/339 for BLST) instead of one consolidated gap. Fixing
        // ConstructionCapacity's computation (see that file) makes this branch's existing
        // condition true for both granularities; this change just makes the out-of-range test
        // and message granularity-aware to match.
        var capacity = report.BookScope.ConstructionCapacity;
        var chapterGranular = string.Equals(report.BookScope.Granularity, "chapter", StringComparison.OrdinalIgnoreCase);
        var totalUnits = chapterGranular
            ? (uncovered.Count > 0 ? report.Beats.Max(b => b.ChapterIndex) + 1 : 0)
            : report.TotalBeats;
        List<(BeatCoordinate Beat, int Ordinal)> perBeat = uncovered;

        if (capacity > 0 && capacity < totalUnits)
        {
            // NO_CONSTRUCTION beyond the blueprint's footprint is the only reason these beats
            // are uncovered — UNSCORED is expected noise (no book-wide rescore has run) and
            // alone would never fail Covered; MISSING_MEANING/NO_PROSE/STUB_PROSE are real
            // per-beat defects the blueprint has nothing to do with, so those still get filed.
            var outOfRange = uncovered
                .Where(x => (chapterGranular ? x.Beat.ChapterIndex : x.Ordinal) >= capacity
                         && x.Beat.Flags.Contains("NO_CONSTRUCTION")
                         && !x.Beat.Flags.Contains("MISSING_MEANING")
                         && !x.Beat.Flags.Contains("NO_PROSE")
                         && !x.Beat.Flags.Contains("STUB_PROSE"))
                .ToList();
            if (outOfRange.Count > 0)
            {
                perBeat = uncovered.Except(outOfRange).ToList();
                var unitWord = chapterGranular ? "chapter(s)" : "beat(s)";
                findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.OutlineDrift, FindingSeverity.Medium,
                    $"COORDINATE blueprint stale: sized for {capacity} {unitWord} but the book now has {totalUnits} " +
                    $"({outOfRange.Count} beat(s) past the blueprint's footprint have no construction slice) — " +
                    $"run 'prose --generate-blueprint --slug {slug}' to resize.",
                    snippet: null, suggestedFix: $"prose --generate-blueprint --slug {slug}");
            }
        }

        // One book-level finding per distinct flag combination, not one per beat (2026-08-13
        // fix — a live corpus check found 664 individual "COORDINATE beat #N" rows sitting
        // unreviewed, the same one-row-per-beat pattern as SWAIN/DRAMATIC-Q). The per-beat
        // coordination check itself is unchanged; this only changes how many rows a run files.
        foreach (var grp in perBeat.GroupBy(x => string.Join(", ", x.Beat.Flags)))
        {
            var examples = string.Join(", ", grp.Take(5).Select(x => $"#{x.Beat.Number}"));
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.OutlineDrift, FindingSeverity.Medium,
                $"COORDINATE [{grp.Key}]: {grp.Count()} beat(s) written but not coordinated to their outline slot — e.g. {examples}",
                snippet: null, suggestedFix: null);
        }
    }

    /// <summary>SwainAuditService returns per-beat classifications but never files shared
    /// Findings. File BLOCKER (Deficient) only by default — MODERATE (Ambiguous) is a soft
    /// call that would otherwise drown the inbox (same calibration lesson BeatChecklistGateService
    /// already learned filing DELIGHT flat-beats).
    ///
    /// One book-level finding, not one per beat (2026-08-13 fix — a corpus-wide SWAIN sweep had
    /// filed 6,372 individual per-beat rows, ~70% of the entire Findings backlog, none of them
    /// realistically triageable one at a time). The full per-beat detail still lives in
    /// <see cref="SwainAuditReport.Results"/>; this just changes how many Finding rows a run
    /// produces, not what gets evaluated.</summary>
    private async Task<SwainAuditReport> SwainAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        var report = await swainAudit.AuditAsync(slug, ct: ct);
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "SWAIN ");
        var blockers = report.Results.Where(b => b.Severity == "BLOCKER").ToList();

        // Self-heal before filing (2026-08-13): a real repair attempt via BeatRepairService, same
        // mechanism AutoRunCli's write-time lens self-repair already uses live. The only
        // re-verification path SwainAuditService offers is a whole-book re-audit, so
        // stillFailingAsync re-runs that once per round (not once per beat) — see SelfHealAsync's
        // own doc comment. `finalReport` tracks the latest audit state so the SII score this
        // method returns reflects post-repair reality, not the pre-repair snapshot.
        var finalReport = report;
        if (blockers.Count > 0)
        {
            await SelfHealAsync(
                nodeId, blockers,
                beatIdOf: x => x.BeatId,
                issueOf: x => new LensIssue(x.Position, x.Classification.ToString(), x.Note,
                    $"Rewrite as a proper Scene (Goal→Conflict→Disaster) or Sequel (Reaction→Dilemma→Decision) — missing: {x.MissingElement}",
                    x.Severity),
                repairAsync: (beatId, nid, issues, ct2) => beatRepair.RepairAsync(beatId, nid, issues, bookBibleOverride: null, ct2),
                writeTextAsync: (beatId, newText, ct2) => workbench.UpdateBeatTextAsync(beatId, newText, expectedUpdatedAt: null, ct: ct2),
                stillFailingAsync: async (remaining, ct2) =>
                {
                    finalReport = await swainAudit.AuditAsync(slug, ct: ct2);
                    var stillBlockerBeats = finalReport.Results.Where(r => r.Severity == "BLOCKER").Select(r => r.BeatId).ToHashSet();
                    return remaining.Where(x => stillBlockerBeats.Contains(x.BeatId)).ToList();
                },
                log,
                ct);
        }

        var stillFailing = finalReport.Results.Where(b => b.Severity == "BLOCKER").ToList();
        if (stillFailing.Count > 0)
        {
            var examples = string.Join("; ", stillFailing.Take(5)
                .Select(b => $"#{b.Position} \"{b.Title}\" ({b.Classification} missing {b.MissingElement})"));
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.StructuralFailure, FindingSeverity.High,
                $"SWAIN: {stillFailing.Count}/{finalReport.TotalBeats} beats fail compliance — worst examples: {examples}",
                snippet: null, suggestedFix: null);
        }
        // Surface incomplete evaluation distinctly rather than silently under-reporting — same
        // principle as the Round 6 ERROR-severity fix for BookAuditService/StoryScopeAuditService.
        // ErrorCount rows are already excluded from BlockerCount/ComplianceRate by construction;
        // this rollup just tells an operator WHY the audit covered fewer beats than expected.
        if (finalReport.ErrorCount > 0)
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Other, FindingSeverity.Low,
                $"SWAIN [incomplete]: {finalReport.ErrorCount}/{finalReport.TotalBeats} beats could not be evaluated (LLM/parse errors) — re-run once resolved.",
                snippet: null, suggestedFix: null);
        return finalReport;
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
    /// Findings. One LLM call per book — cheap, always run in FULL.
    /// MapFiveActStructureAsync now throws on LLM/parse failure (2026-08-14 fix) instead of
    /// returning a "(parse error)" Error stub — this used to be checked and silently swallowed
    /// (`if (map.Error != null) return;`), so a failed run and a genuinely gap-free book both
    /// produced zero FIVEACT findings, indistinguishable from each other. Now files a distinct
    /// [incomplete] finding on failure, same pattern as DramaticQuestionAsync.</summary>
    private async Task FiveActMapAsync(Guid nodeId, string slug, CancellationToken ct)
    {
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "FIVEACT [incomplete]");
        FiveActMap map;
        try
        {
            map = await narrativeScience.MapFiveActStructureAsync(nodeId, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "MapFiveActStructureAsync failed for node {NodeId}", nodeId);
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Other, FindingSeverity.Low,
                $"FIVEACT [incomplete]: five-act structure map could not be generated (LLM/parse error) — re-run once resolved.",
                snippet: null, suggestedFix: null);
            return;
        }
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "FIVEACT ");
        if (map.Error != null) return;
        foreach (var gap in map.StructuralGaps)
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.StructuralFailure, FindingSeverity.Medium,
                $"FIVEACT {gap}", snippet: null, suggestedFix: null);
    }

    /// <summary>NarrativeScienceService's dramatic-question check ("who is this person really?")
    /// is per-beat (Will Storr's framework) and never files Findings. One LLM call PER BEAT —
    /// the most direct existing match for "does prose reveal character, not just plot," so it's
    /// worth the cost, but it's real: this is why the check lives in FULL, not DEEP. Deliberately
    /// beat-scoped, not a candidate for act/book-level rollup — 2026-08-13 cost review confirmed
    /// the other three wired-in NarrativeScienceService checks (five-act, sacred-flaw, and this
    /// one) are already scoped at the right granularity (book/character/beat respectively);
    /// AuditSceneEngagementAsync's 6-point scene anatomy was the one genuinely redundant analyzer
    /// (mechanisms overlapped LogicSweep's causality dimension, DELIGHT moves, and StoryScope) and
    /// had no automated caller anywhere — it was removed outright rather than left as a manual
    /// CLI/MCP-only cost trap. See NarrativeScienceService.cs's header comment.</summary>
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

        // One book-level finding, not one per beat (2026-08-13 fix — a corpus-wide DRAMATIC-Q
        // sweep had filed 2,066 individual per-beat rows, none of them realistically triageable
        // one at a time). The per-beat LLM evaluation itself stays exactly as scoped above — this
        // only changes how many Finding rows a run produces, not what gets evaluated.
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "DRAMATIC-Q ");
        var failingInitially = evaluated.Where(e => !(e.Result.DramaticQuestionActive && e.Result.OverallScore >= 5)).ToList();

        // Self-heal before filing (2026-08-13): a real repair attempt via BeatRepairService, same
        // mechanism AutoRunCli's write-time lens self-repair already uses live. Re-check re-runs
        // CheckDramaticQuestionAsync directly on the repaired beat's fresh text — cheap, since
        // this check already operates one beat at a time (unlike SWAIN, whose only re-check is a
        // whole-book re-audit).
        var stillFailing = failingInitially.Count == 0
            ? failingInitially
            : await SelfHealAsync(
                nodeId, failingInitially,
                beatIdOf: e => e.BeatId,
                issueOf: e => new LensIssue(
                    e.Number, "DramaticQuestion",
                    string.IsNullOrWhiteSpace(e.Result.SubconsciousSummary) ? e.Result.SurfaceSummary : e.Result.SubconsciousSummary,
                    string.IsNullOrWhiteSpace(e.Result.ImprovementHint) ? "Reveal who this character really is beneath the surface action." : e.Result.ImprovementHint,
                    e.Result.OverallScore <= 2 ? "MODERATE" : "MINOR"),
                repairAsync: (beatId, nid, issues, ct2) => beatRepair.RepairAsync(beatId, nid, issues, bookBibleOverride: null, ct2),
                writeTextAsync: (beatId, newText, ct2) => workbench.UpdateBeatTextAsync(beatId, newText, expectedUpdatedAt: null, ct: ct2),
                stillFailingAsync: async (remaining, ct2) =>
                {
                    var result = new List<(Guid BeatId, int Number, DramaticQuestionResult Result)>();
                    foreach (var e in remaining)
                    {
                        var freshText = await db.Beats.AsNoTracking()
                            .Where(b => b.Id == e.BeatId).Select(b => b.Text).FirstOrDefaultAsync(ct2);
                        if (string.IsNullOrWhiteSpace(freshText)) { result.Add(e); continue; }
                        try
                        {
                            var fresh = await narrativeScience.CheckDramaticQuestionAsync(freshText, characterId: null, ct2);
                            if (!(fresh.DramaticQuestionActive && fresh.OverallScore >= 5))
                                result.Add((e.BeatId, e.Number, fresh)); // carry the FRESH result forward for accurate display
                        }
                        catch (Exception ex)
                        {
                            log.LogWarning(ex, "CheckDramaticQuestionAsync re-check failed for beat {BeatId}", e.BeatId);
                            result.Add(e); // re-check itself failed — keep as still-failing with the last-known result
                        }
                    }
                    return result;
                },
                log,
                ct);

        if (stillFailing.Count > 0)
        {
            var sev = stillFailing.Any(e => e.Result.OverallScore <= 2) ? FindingSeverity.Medium : FindingSeverity.Low;
            var examples = string.Join("; ", stillFailing.OrderBy(e => e.Result.OverallScore).Take(5)
                .Select(e => $"#{e.Number} scores {e.Result.OverallScore}/10"));
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.StructuralFailure, sev,
                $"DRAMATIC-Q: {stillFailing.Count}/{evaluated.Count} beats fail \"who is this person really\" — worst: {examples}",
                snippet: null, suggestedFix: null);
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
            .Where(bn => searchIds.Contains(bn.NodeId) && true).Select(bn => bn.BeatId).ToListAsync(ct);
        if (beatIds.Count == 0) return;

        var beatParams = beatIds.Select((id, i) => new SqlParameter($"@b{i}", id)).ToArray();
        var placeholders = string.Join(",", beatParams.Select(p => p.ParameterName));
        var povRows = await db.Database.SqlQueryRaw<PovRow>(
            "SELECT DISTINCT EntityId, EntityName FROM BeatEntityPresence " +
            $"WHERE PresenceType = 'pov' AND BeatId IN ({placeholders})",
            beatParams).ToListAsync(ct);
        // 2026-08-09 fix: silently returning here (found live — 26/30 top-level books have ZERO
        // PresenceType='pov' rows at all) made this check look like it ran and found nothing
        // clean, when really it never evaluated a single character — the exact fail-open shape
        // this session fixed repeatedly elsewhere. File the gap as a finding instead of vanishing.
        if (povRows.Count == 0)
        {
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.StructuralFailure, FindingSeverity.Low,
                "SACRED-FLAW [no-pov-data]: no BeatEntityPresence PresenceType='pov' rows found for this book — " +
                "the sacred-flaw check has never evaluated any character here, not because none needed it.",
                snippet: null,
                suggestedFix: "Tag each beat's narrating character via BeatEntityPresence (PresenceType='pov') — DCM's per-beat voice register also depends on this same data.");
            return;
        }
        // "SACRED-FLAW " also matches (and clears) a stale "SACRED-FLAW [no-pov-data]" rollup
        // from a prior run, since that summary starts with this same prefix.
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "SACRED-FLAW [incomplete]");
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "SACRED-FLAW ");
        var failedCharCount = 0;
        foreach (var pov in povRows.DistinctBy(p => p.EntityId))
        {
            SacredFlawAnalysis result;
            try
            {
                // AnalyzeSacredFlawAsync now throws on LLM/parse failure (2026-08-14 fix) instead
                // of returning a "(parse error)" TheoryOfControl — that fallback used to be filed
                // here as a real SACRED-FLAW finding ("no theory of control identified"),
                // distinguishable from a genuine low-confidence read only by string-matching
                // Diagnosis. Catch per-character so one failure doesn't lose the others.
                result = await narrativeScience.AnalyzeSacredFlawAsync(pov.EntityId, scaffold: false, ct);
            }
            catch (Exception ex)
            {
                failedCharCount++;
                log.LogWarning(ex, "AnalyzeSacredFlawAsync failed for character {EntityId}", pov.EntityId);
                continue;
            }
            if (string.Equals(result.Confidence, "high", StringComparison.OrdinalIgnoreCase)) continue;
            var sev = string.IsNullOrWhiteSpace(result.TheoryOfControl) ? FindingSeverity.Medium : FindingSeverity.Low;
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.StructuralFailure, sev,
                $"SACRED-FLAW {pov.EntityName}: confidence={result.Confidence} — " +
                $"{(string.IsNullOrWhiteSpace(result.TheoryOfControl) ? "no theory of control identified" : result.TheoryOfControl)}",
                snippet: null,
                suggestedFix: "Ground this POV character's flaw via create_character (PsychologySecret / core_fears / core_desires / blind_spots) so future beats have a firm theory-of-control to dramatize.");
        }

        if (failedCharCount > 0)
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Other, FindingSeverity.Low,
                $"SACRED-FLAW [incomplete]: {failedCharCount} character(s) could not be evaluated (LLM/parse errors) — re-run once resolved.",
                snippet: null, suggestedFix: null);
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
            .Where(bn => searchIds.Contains(bn.NodeId) && true).Select(bn => bn.BeatId).ToListAsync(ct);
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
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "THEME [incomplete]");
        ThemeCoherenceResult result;
        try
        {
            // AnalyzeAsync now throws on total LLM/parse failure (2026-08-14 fix) instead of
            // returning a stub with Error set — this caller used to just check `Error != null`
            // and silently return, filing nothing, so a failed run and a genuinely clean book
            // were indistinguishable from the Findings list.
            result = await themeCoherence.AnalyzeAsync(nodeId, ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "ThemeCoherenceService.AnalyzeAsync failed for node {NodeId}", nodeId);
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Other, FindingSeverity.Low,
                "THEME [incomplete]: controlling-idea analysis could not be generated (LLM/parse error) — re-run once resolved.",
                snippet: null, suggestedFix: null);
            return;
        }
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
                snippet: null, suggestedFix: "State the book's controlling idea in the NodeOutline via set_book_outline so future beats have a claim to dramatize.");

        // EndingEngagesOpening is now nullable (2026-08-14 fix) — null means the model didn't
        // answer the question (e.g. omitted the field), which is NOT the same as "no, it
        // doesn't engage." Only file the finding on an explicit false.
        if (result.EndingEngagesOpening == false)
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.StructuralFailure, FindingSeverity.Low,
                $"THEME [ending-drift]: closing beats don't appear to engage the opening's value-question (\"{result.ControllingIdea}\") — {result.Diagnosis}",
                snippet: null, suggestedFix: null);
    }

    /// <summary>Wires ContinuityService's ledger of atomic (entity, predicate, object) claims —
    /// fully built (Upsert, contradiction detection, resolution lifecycle) but never called from
    /// the automated battery before this fix — into a per-book Finding. Numeric predicates
    /// (ages, tenures, etc.) are compared arithmetic-safely by ContinuityService.ObjectsMatch
    /// (2026-08-14 fix), so re-derived phrasing across sweep rounds ("fifty" vs "50") no longer
    /// manufactures the false contradiction that VIGL hit repeatedly this session — only a
    /// genuine numeric discrepancy (fifty vs sixty) still surfaces here.
    /// This check's coverage is bounded by whether ContinuityExtractionService has ever been run
    /// for this book (nothing runs it automatically per beat save yet) — HasAnyClaimsForBook
    /// distinguishes "extracted and clean" from "never extracted," same honest-gap pattern as
    /// SacredFlawAsync's no-pov-data finding.</summary>
    /// <summary>Public (2026-09-01) so a narrow, zero-LLM-cost CLI command
    /// (<c>prose --fact-ledger-refresh</c>) can re-run just this check on demand — the only
    /// existing entry point was the cost-gated <c>--audit-book --deep</c> bundle (~15 other
    /// LLM-call checks alongside this free one), which made "did my ContinuityService fix
    /// actually shrink this book's fact-ledger count" a ~$70 question to answer.</summary>
    public Task FactLedgerAsync(string slug, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Read before touching Findings — a read failure here (e.g. DB connectivity) should
        // propagate to RunCheckAsync's own try/catch and leave prior Findings untouched, same
        // "never purge on a failed read" discipline as every other check in this file.
        var hasClaims = continuity.HasAnyClaimsForBook(slug);
        var groups = hasClaims ? continuity.GetContradictionGroups(slug) : [];

        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "FACT-LEDGER ");

        if (!hasClaims)
        {
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Other, FindingSeverity.Low,
                "FACT-LEDGER [not-extracted]: no continuity claims tagged for this book — the fact ledger has " +
                "never been populated here, not because no hard facts needed tracking.",
                snippet: null,
                suggestedFix: "Run prose --continuity extract --book <slug> (or the MCP ExtractContinuityFromBook tool) to backfill the ledger.");
            return Task.CompletedTask;
        }

        foreach (var g in groups)
        {
            var variants = string.Join(" vs. ", g.Claims.Select(c =>
                $"\"{c.Object}\" ({c.SourceType}{(c.SourceChapterNumber.HasValue ? $", ch.{c.SourceChapterNumber}" : "")})"));
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Contradiction, FindingSeverity.Medium,
                $"FACT-LEDGER [{g.EntityName}.{g.Predicate}]: conflicting values — {variants}",
                snippet: null,
                suggestedFix: "Resolve via the /continuity UI (or ContinuityService.Resolve/MakeCanonical) — pick the load-bearing value, or supply a custom one if neither is right.");
        }
        return Task.CompletedTask;
    }

    /// <summary>Phase D of the Bible/Book/Entities validation triangle: for every claim already
    /// applied to its entity's canon record (<c>ContinuityApplyService.ApplyAsync</c>, which sets
    /// AppliedAt/AppliedToField), verify the field still says what the claim asserted. Answers
    /// "are all entities mentioned actually correct in the repo" for the applied subset —
    /// deterministic (JSON field comparison), no LLM call. Same honest-gap framing as
    /// FactLedgerAsync/HasAnyClaimsForBook: zero applied claims for a book means "nothing has ever
    /// been applied here," not "verified clean."</summary>
    private async Task AppliedClaimDriftAsync(string slug, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var applied = continuity.GetAppliedClaims(slug);
        var drifts = applied.Count > 0
            ? await continuityApply.CheckAppliedClaimsAsync(slug, ct)
            : [];

        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "APPLIED-CLAIM-DRIFT ");

        if (applied.Count == 0)
        {
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Other, FindingSeverity.Low,
                "APPLIED-CLAIM-DRIFT [not-applied]: no continuity claims have ever been applied back to an " +
                "entity record for this book — this check has never evaluated any entity here, not because " +
                "the repo is verified correct.",
                snippet: null,
                suggestedFix: "Run prose --continuity apply --claim <uid> (or the sweep's auto-apply step, --allow-votes) on CANONICAL claims first.");
            return;
        }

        foreach (var d in drifts.Where(d => d.Drifted))
        {
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.EntityDrift, FindingSeverity.Medium,
                $"APPLIED-CLAIM-DRIFT [{d.Claim.EntityName}.{d.Claim.Predicate}] ({d.Reason}): {d.Detail ?? "the entity record no longer matches the applied claim"}",
                snippet: d.Claim.Snippet,
                suggestedFix: "Confirm whether the entity record's current value is the new truth (re-extract/re-apply a fresh claim) or whether the edit was a mistake (restore the applied value).");
        }
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
            .Where(bn => searchIds.Contains(bn.NodeId) && true)
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
        // 2026-08-09 fix: distinguish "no POV data recorded at all" (a data gap — flag it) from
        // the doc comment's INTENTIONAL "single-narrator book, nothing to drift against" skip
        // (checked further below once we know the actual distinct-POV-character count). Found
        // live: 26/30 top-level books have zero PresenceType='pov' rows, so this was silently
        // no-op'ing on data absence far more often than on the legitimate single-narrator case.
        if (povRows.Count == 0)
        {
            findingsSvc.Upsert($"node:{slug}", chapterId: null, FindingCategory.Voice, FindingSeverity.Low,
                "VOICE-DRIFT [no-pov-data]: no BeatEntityPresence PresenceType='pov' rows found for this book — " +
                "voice-drift has never been checked here, not because there's only one narrator.",
                snippet: null,
                suggestedFix: "Tag each beat's narrating character via BeatEntityPresence (PresenceType='pov').");
            return;
        }
        findingsSvc.DeleteBySummaryPrefix($"node:{slug}", "VOICE-DRIFT [no-pov-data]");

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
        // Chapter position first, then chapter-local SortKey — raw SortKey alone ties across
        // chapters (every chapter's beats restart near the same values), scrambling reading order
        // for this shared helper's callers (same fix as LogicSweepService.RunAsync).
        var chapterOrder = searchIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        var rows = await db.BeatNodes.AsNoTracking()
            .Where(bn => searchIds.Contains(bn.NodeId) && true && bn.Beat != null && bn.Beat.Text != "")
            .Select(bn => new { bn.Beat!.Id, bn.Beat.Number, bn.Beat.Text, bn.NodeId, bn.SortKey })
            .ToListAsync(ct);
        // Stripped here, once, for every caller of this shared helper (voice-fingerprint tokens,
        // etc.) rather than trusting each analysis to remember to strip its own input.
        return rows
            .OrderBy(r => chapterOrder.TryGetValue(r.NodeId, out var idx) ? idx : int.MaxValue)
            .ThenBy(r => r.SortKey)
            .Select(r => (r.Id, r.Number, BeatMarkup.StripEntityTags(r.Text)))
            .ToList();
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
            .Select(f => new { f.Category, f.Severity, f.Summary })
            .ToListAsync(ct);

        var deduction = new List<SiiCategoryDeduction>();
        var totalDeduction = 0;
        // 2026-08-30 fix: RepetitionLintService (LINT), PovVoiceAuditService (POV /VOICE ), and
        // ChapterHookService (HOOK ) all deliberately file under the shared FindingCategory.
        // CraftChecklist umbrella (see each service's own doc comment) alongside
        // BeatChecklistGateService's native checklist findings — but a single flat CategoryCap
        // over that whole group let one instrument's findings silently crowd out another's once
        // ad hoc runs accumulated. Sub-key CraftChecklist by its summary prefix so each
        // instrument gets its own capped bucket; every other category is capped as one group
        // exactly as before.
        foreach (var g in open.GroupBy(f => SubCategoryKey(f.Category, f.Summary)))
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
