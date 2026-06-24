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
    WorkflowMonitorService monitor)
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
        var (mode, confidence, method) = modeDetector.Detect(context.BeatGoal, context.SceneSoFar);

        // Pacing: positional arc, overridden by beat-goal keywords; Combat forces STRIKE.
        var pacingInstruction = totalBeats > 0
            ? PacingService.GetPacing(beatIndex, totalBeats, context.BeatGoal)
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

        var enriched = context with
        {
            BeatIndex              = beatIndex,
            TotalBeats             = totalBeats,
            PacingGuidance         = pacingInstruction?.ProseGuidance ?? "",
            StructuralRoleGuidance = structuralGuidance,
            DetectedMode           = mode,
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
            ], CancellationToken.None);

            await modeDetector.PersistAsync(beatId, universeId, mode, confidence, method, CancellationToken.None);
        }, CancellationToken.None);

        return result;
    }
}
