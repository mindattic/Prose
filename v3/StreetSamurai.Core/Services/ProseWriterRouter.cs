namespace StreetSamurai.Core.Services;

/// <summary>
/// Unified entry point for all beat prose generation. Replaces direct calls to
/// BeatGeneratorService in the workbench + CLI paths.
///
/// What it adds over BeatGeneratorService:
///   - Beat mode detection (Combat / EmotionalClimax / Dialogue / Transition / Revelation / Narrative)
///   - PacingService injection (positional arc + beat-goal keyword override)
///   - StoryMethodologyService injection (structural role: Opening Image, Catalyst, Midpoint, etc.)
///   - Combat prose rules injection when mode = Combat
///   - BeatServiceLog coverage tracking (WorkflowMonitorService)
///   - BeatModeLog persistence (BeatModeDetector)
///
/// BeatGeneratorService continues to handle plant/payoff + commandment injection
/// (those are triggered by BeatContext.StrandId, which ProseWriterRouter preserves).
/// </summary>
public class ProseWriterRouter(
    BeatGeneratorService generator,
    StoryMethodologyService methodology,
    BeatModeDetector modeDetector,
    WorkflowMonitorService monitor,
    EntityContextService? entityContext = null)
{
    static readonly string CombatProseGuidance = """
        BEAT MODE: COMBAT — action prose rules are in force.
        • Verbs lead. Nouns follow. Adjectives are rare.
        • Sentences are SHORT. Fragment when needed. No compound clauses stacked.
        • No naming of emotions directly. A clenched jaw, a white knuckle, a missed breath.
        • Physical specificity: which hand, which angle, which surface. Geometry is the voice.
        • Weapons behave like the canon record says. Damage persists — a cut arm does not forget.
        • Cyberware has latency, noise, and cost. It is never a free win.
        • Bystanders exist. Crowds move, scream, flee, get in the way.
        • No omniscient summary. Stay tight to the bodies in the room.
        • Dissociated observer (Kyle only): max 2 per beat — a single italicized second-person line
          (*you chose this; remember that*) in the white space between exchanges, then prose continues.
        """;

    /// <summary>
    /// Write a beat using the full enriched pipeline: mode detection → pacing → structural role →
    /// combat rules (if applicable) → BeatGeneratorService → coverage logging.
    /// </summary>
    /// <param name="context">Beat context assembled by SceneContextAssembler. StrandId must be set.</param>
    /// <param name="beatId">The beat's DB Guid. Use Guid.Empty for pre-save preview writes.</param>
    /// <param name="beatIndex">Zero-based position of this beat in the strand. 0 if unknown.</param>
    /// <param name="totalBeats">Total beats in the strand. 0 disables positional pacing/structural injection.</param>
    /// <param name="universeId">Current universe for log stamping. Guid.Empty = GLMZ (default).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<string> WriteAsync(
        BeatContext context,
        Guid beatId          = default,
        int beatIndex        = 0,
        int totalBeats       = 0,
        Guid universeId      = default,
        CancellationToken ct = default)
    {
        var (mode, confidence, method, pacingGuidance, structuralGuidance) =
            ComputeEnrichment(context.BeatGoal, context.SceneSoFar, beatIndex, totalBeats);

        // Entity context stack: load LRU working memory for this beat (non-fatal if unavailable).
        var entityStackContext = "";
        if (entityContext != null && context.StrandId != Guid.Empty)
        {
            try { entityStackContext = await entityContext.PrepareContextAsync(context.StrandId, beatId, context.BeatGoal, context.SceneSoFar, ct); }
            catch { /* non-blocking — entity context is best-effort */ }
        }

        var enriched = context with
        {
            BeatIndex              = beatIndex,
            TotalBeats             = totalBeats,
            PacingGuidance         = pacingGuidance,
            StructuralRoleGuidance = structuralGuidance,
            DetectedMode           = mode,
            EntityStackContext     = entityStackContext,
        };

        var result = await generator.GenerateBeatAsync(enriched, ct);

        // Log coverage fire-and-forget — never blocks prose output.
        var pacingApplicable  = totalBeats > 0;
        var structApplicable  = totalBeats > 0;
        var combatApplicable  = mode == BeatMode.Combat;
        var strandApplicable  = context.StrandId != Guid.Empty;

        _ = Task.Run(async () =>
        {
            await monitor.LogBeatActivityAsync(beatId, context.StrandId, universeId,
            [
                new("Pacing",          IsApplicable: pacingApplicable,  IsActive: pacingApplicable && enriched.PacingGuidance.Length > 0,          BlockSizeChars: enriched.PacingGuidance.Length),
                new("StoryMethodology",IsApplicable: structApplicable,  IsActive: structApplicable && enriched.StructuralRoleGuidance.Length > 0,  BlockSizeChars: enriched.StructuralRoleGuidance.Length),
                new("PlantPayoff",     IsApplicable: strandApplicable,  IsActive: strandApplicable,  BlockSizeChars: 0),
                new("StoryAudit",      IsApplicable: strandApplicable,  IsActive: strandApplicable,  BlockSizeChars: 0),
                new("Combat",          IsApplicable: combatApplicable,  IsActive: combatApplicable,  BlockSizeChars: combatApplicable ? CombatProseGuidance.Length : 0),
                new("EntityContext",   IsApplicable: strandApplicable,  IsActive: entityStackContext.Length > 0,  BlockSizeChars: entityStackContext.Length),
            ], CancellationToken.None);

            await modeDetector.PersistAsync(beatId, universeId, mode, confidence, method, CancellationToken.None);

            if (entityContext != null && context.StrandId != Guid.Empty && result.Length > 0)
                await entityContext.ReconcileAsync(result, context.StrandId, beatId, universeId, CancellationToken.None);
        }, CancellationToken.None);

        return result;
    }

    /// <summary>
    /// Backfill coverage logs for an already-written beat WITHOUT regenerating prose.
    /// Runs the same enrichment computation WriteAsync uses (mode detection → pacing →
    /// structural role → combat rules) and records BeatServiceLog + BeatModeLog rows from
    /// it, so the workflow monitor reflects prose that was written before the router existed.
    ///
    /// EntityContext is intentionally NOT logged here: its activation depends on the live
    /// working-memory stack built during generation, which a backfill cannot reconstruct
    /// without mutating it. The five gap-tracked services (Pacing, StoryMethodology,
    /// PlantPayoff, StoryAudit, Combat) are fully determined by goal + position and ARE logged.
    /// </summary>
    /// <param name="beatGoal">The beat's authorial intent (its synopsis) — drives mode + pacing.</param>
    /// <param name="proseHint">Optional prose tail to sharpen mode detection (ignored if ≥500 chars).</param>
    public async Task LogCoverageAsync(
        Guid beatId, Guid strandId, string? beatGoal, string? proseHint,
        int beatIndex, int totalBeats, Guid universeId = default,
        CancellationToken ct = default)
    {
        if (strandId == Guid.Empty) return;

        var (mode, confidence, method, pacingGuidance, structuralGuidance) =
            ComputeEnrichment(beatGoal, proseHint, beatIndex, totalBeats);

        var pacingApplicable = totalBeats > 0;
        var structApplicable = totalBeats > 0;
        var combatApplicable = mode == BeatMode.Combat;

        await monitor.LogBeatActivityAsync(beatId, strandId, universeId,
        [
            new("Pacing",           IsApplicable: pacingApplicable, IsActive: pacingApplicable && pacingGuidance.Length > 0,     BlockSizeChars: pacingGuidance.Length),
            new("StoryMethodology", IsApplicable: structApplicable, IsActive: structApplicable && structuralGuidance.Length > 0, BlockSizeChars: structuralGuidance.Length),
            new("PlantPayoff",      IsApplicable: true,             IsActive: true,                                              BlockSizeChars: 0),
            new("StoryAudit",       IsApplicable: true,             IsActive: true,                                              BlockSizeChars: 0),
            new("Combat",           IsApplicable: combatApplicable, IsActive: combatApplicable,                                  BlockSizeChars: combatApplicable ? CombatProseGuidance.Length : 0),
        ], ct);

        await modeDetector.PersistAsync(beatId, universeId, mode, confidence, method, ct);
    }

    /// <summary>
    /// Shared enrichment computation used by both WriteAsync (live generation) and
    /// LogCoverageAsync (backfill). Single source of truth for "what fires" so coverage
    /// logs match real generation behaviour.
    /// </summary>
    private (BeatMode Mode, float Confidence, string Method, string PacingGuidance, string StructuralGuidance)
        ComputeEnrichment(string? beatGoal, string? proseHint, int beatIndex, int totalBeats)
    {
        var (mode, confidence, method) = modeDetector.Detect(beatGoal, proseHint);

        // Pacing: positional arc, overridden by beat-goal keywords; Combat forces STRIKE.
        var pacingInstruction = totalBeats > 0
            ? PacingService.GetPacing(beatIndex, totalBeats, beatGoal ?? "")
            : null;
        if (mode == BeatMode.Combat)
            pacingInstruction = new PacingInstruction(PacingService.PaceMode.Strike);

        // Structural role (Save the Cat / Scene-Sequel).
        var structuralGuidance = totalBeats > 0
            ? methodology.GetBeatGenerationGuidance(beatIndex, totalBeats)
            : "";

        // Combat mode: prepend combat prose rules to structural guidance.
        if (mode == BeatMode.Combat)
            structuralGuidance = CombatProseGuidance + (structuralGuidance.Length > 0 ? "\n\n" + structuralGuidance : "");

        return (mode, confidence, method, pacingInstruction?.ProseGuidance ?? "", structuralGuidance);
    }
}
