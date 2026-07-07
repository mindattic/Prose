using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using System.Text;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Unified entry point for all beat prose generation. Replaces direct calls to
/// BeatGeneratorService in the workbench + CLI paths.
///
/// What it adds over BeatGeneratorService:
///   - Beat mode detection (Combat / EmotionalClimax / Dialogue / Transition / Revelation / Narrative)
///   - PacingService injection (positional arc + beat-goal keyword override)
///   - StoryMethodologyService injection (structural role: Opening Image, Catalyst, Midpoint, etc.)
///   - Combat prose rules injection when mode = Combat (extended version with Dissociated Observer examples)
///   - SceneContextBuilder injection for ambient sensory grounding (when Location is set on BeatContext)
///   - DialogueService auto-fire on Dialogue/EmotionalClimax beats (when CharactersInScene is set)
///   - EmotionalDepthService feedback loop: prior examination findings injected as generation constraints
///   - TensionEscalationService: warns when consecutive non-escalating beats detected
///   - ReaderKnowledgeService: injects current reader knowledge state for dramatic irony management
///   - BeatServiceLog coverage tracking (WorkflowMonitorService)
///   - BeatModeLog persistence (BeatModeDetector)
///
/// BeatGeneratorService continues to handle plant/payoff + commandment injection
/// (those are triggered by BeatContext.NodeId, which ProseWriterRouter preserves).
/// </summary>
public class ProseWriterRouter(
    BeatGeneratorService generator,
    StoryMethodologyService methodology,
    BeatModeDetector modeDetector,
    WorkflowMonitorService monitor,
    ILogger<ProseWriterRouter> log,
    EntityContextService? entityContext = null,
    DocContextService? docContext = null,
    SettingsService? settings = null,
    ContextTelemetryService? telemetry = null,
    SceneContextBuilder? sceneBuilder = null,
    DialogueService? dialogue = null,
    IDbContextFactory<StreetSamuraiDbContext>? dbFactory = null,
    TensionEscalationService? tensionService = null,
    ReaderKnowledgeService? readerKnowledge = null,
    ConsequenceService? consequence = null,
    AmbientAnomalyService? ambientAnomaly = null,
    NarrativeSummaryService? narrativeSummary = null,
    WorldStateAtBeatService? worldStateAtBeat = null,
    ConsequenceEngine? consequenceEngine = null,
    MlProseGuidanceService? mlProseGuidance = null,
    ChapterSummaryService? chapterSummary = null,
    OpenThreadsService? openThreads = null,
    SceneContextAssembler? sceneAssembler = null,
    ContinuityService? continuity = null,
    StoryScienceService? storyScience = null,
    NarrativeChartService? narrativeChart = null,
    StructuralBlueprintService? structuralBlueprint = null)
{
    // Built from CombatProseConstants — single source of truth shared with CombatSceneWriter.
    static readonly string CombatProseGuidance =
        "BEAT MODE: COMBAT — action prose rules are in force.\n" +
        CombatProseConstants.ActionRules + "\n" +
        "DISSOCIATED OBSERVER — use sparingly, maximum two per beat:\n" +
        CombatProseConstants.DissociatedObserverBody;

    /// <summary>
    /// Write a beat using the full enriched pipeline: mode detection → pacing → structural role →
    /// combat rules → ambient grounding → dialogue voices → emotional feedback → tension escalation →
    /// reader knowledge state → BeatGeneratorService → coverage logging.
    /// </summary>
    /// <param name="context">Beat context assembled by SceneContextAssembler. NodeId must be set.</param>
    /// <param name="beatId">The beat's DB Guid. Use Guid.Empty for pre-save preview writes.</param>
    /// <param name="beatIndex">Zero-based position of this beat in the node. 0 if unknown.</param>
    /// <param name="totalBeats">Total beats in the node. 0 disables positional pacing/structural injection.</param>
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
        if (entityContext != null && context.NodeId != Guid.Empty)
        {
            try { entityStackContext = await entityContext.PrepareContextAsync(context.NodeId, beatId, context.BeatGoal, context.SceneSoFar, ct); }
            catch { /* non-blocking — entity context is best-effort */ }
        }

        // Fix 2: auto-populate CharactersInScene from entity context stack (character-type entries).
        // Fires DialogueService, ConsequenceService, and ConsequenceEngine for every beat once the
        // stack is warm. Gracefully empty on cold-start (stack fills after first ReconcileAsync).
        if (context.CharactersInScene.Count == 0 && context.NodeId != Guid.Empty && entityContext != null)
        {
            try
            {
                var active = entityContext.GetActiveEntities(context.NodeId)
                    .Where(e => string.Equals(e.EntityType, "character", StringComparison.OrdinalIgnoreCase))
                    .Take(3)
                    .Select(e => e.Name)
                    .ToList();
                if (active.Count > 0)
                    context = context with { CharactersInScene = active };
            }
            catch { /* non-blocking */ }
        }

        // Doc context stack: load the rotating cast of pertinent canon .md docs (non-fatal).
        DocContextService.DocContextResult? docResult = null;
        var docStackContext = "";
        if (docContext != null && settings?.DocContextEnabled == true && context.NodeId != Guid.Empty)
        {
            try
            {
                var triggerText = (context.BeatGoal ?? "") + "\n" + (context.SceneSoFar ?? "");
                docResult = string.IsNullOrEmpty(context.DocScopeCode)
                    ? await docContext.PrepareForNodeAsync(context.NodeId, triggerText, tokenBudget: 2000, ct)
                    : await docContext.PrepareContextAsync(context.NodeId, context.DocScopeCode, triggerText, tokenBudget: 2000, ct: ct);
                docStackContext = docResult.Block;
            }
            catch { /* non-blocking — doc context is best-effort */ }
        }

        // Fix 4: auto-populate Location from StoryNode.DefaultLocation when caller doesn't set it.
        // Enables SceneContextBuilder (ambient grounding) and AmbientAnomalyService for every beat.
        if (string.IsNullOrWhiteSpace(context.Location) && context.NodeId != Guid.Empty && dbFactory != null)
        {
            try
            {
                await using var locDb = await dbFactory.CreateDbContextAsync(ct);
                var defaultLoc = await locDb.Nodes
                    .AsNoTracking()
                    .Where(n => n.Id == context.NodeId)
                    .Select(n => n.DefaultLocation)
                    .FirstOrDefaultAsync(ct);
                if (!string.IsNullOrWhiteSpace(defaultLoc))
                    context = context with { Location = defaultLoc };
            }
            catch { /* non-blocking */ }
        }

        // ── New enrichments (SS-A28) ─────────────────────────────────────────

        // Ambient sensory grounding: SceneContextBuilder from the Location hint on BeatContext.
        var locationContext = context.LocationContext;
        if (sceneBuilder != null && string.IsNullOrEmpty(locationContext) && !string.IsNullOrEmpty(context.Location))
        {
            try { locationContext = sceneBuilder.BuildAmbientContext(context.Location, context.TimeOfDay, context.Weather); }
            catch { /* non-blocking */ }
        }

        // Dialogue voice profiles: auto-fire on Dialogue/EmotionalClimax beats when characters are listed.
        var dialogueContext = context.DialogueContext;
        if (dialogue != null
            && string.IsNullOrEmpty(dialogueContext)
            && context.CharactersInScene.Count > 0
            && (mode == BeatMode.Dialogue || mode == BeatMode.EmotionalClimax))
        {
            try { dialogueContext = dialogue.BuildDialogueContext(context.CharactersInScene.ToList()); }
            catch { /* non-blocking */ }
        }

        // Emotional depth feedback: pull prior examination findings for this node.
        var emotionalGuidanceContext = context.EmotionalGuidanceContext;
        if (string.IsNullOrEmpty(emotionalGuidanceContext) && dbFactory != null && context.NodeId != Guid.Empty)
        {
            try { emotionalGuidanceContext = await BuildEmotionalGuidanceAsync(context.NodeId, ct); }
            catch { /* non-blocking */ }
        }

        // Fix 1: auto-populate XRayContext via SceneContextAssembler when beatId is known.
        // Injects per-character voice/psychology/wound/behavioral profiles for every entity on screen.
        var xRayContext = context.XRayContext;
        if (sceneAssembler != null && string.IsNullOrWhiteSpace(xRayContext) && beatId != Guid.Empty)
        {
            try
            {
                var sc = await sceneAssembler.AssembleForBeatAsync(beatId, tokenBudget: 2000, ct);
                if (sc != null && !string.IsNullOrWhiteSpace(sc.ContextBlock))
                {
                    xRayContext = sc.ContextBlock;
                    // Populate BeatEntities from the canonical write path (fire-and-forget).
                    _ = Task.Run(() => sceneAssembler.PersistRosterAsync(beatId, sc, CancellationToken.None));
                }
            }
            catch { /* non-blocking */ }
        }

        // Fix 3: build canonical facts block for characters in scene from ContinuityService.
        // Injects CANONICAL/CONFIRMED claims as do-not-contradict constraints before generation.
        var continuityContext = context.ContinuityContext;
        if (continuity != null && string.IsNullOrEmpty(continuityContext) && context.CharactersInScene.Count > 0)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("## ESTABLISHED CANON — do not contradict these facts:");
                var sceneNames = context.CharactersInScene
                    .Select(n => n.Trim())
                    .Where(n => n.Length > 0)
                    .Take(3)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var claims = continuity.GetByStatus("CANONICAL")
                    .Concat(continuity.GetByStatus("CONFIRMED"))
                    .Where(c => sceneNames.Contains(c.EntityName))
                    .Take(24)
                    .ToList();
                if (claims.Count > 0)
                {
                    foreach (var claim in claims)
                        sb.AppendLine($"- {claim.EntityName}: {claim.Predicate} {claim.Object}");
                    continuityContext = sb.ToString().TrimEnd();
                }
            }
            catch { /* non-blocking */ }
        }

        // ML prose quality guidance: findings from the nightly Python model audit.
        var mlProseGuidanceContext = context.MlProseGuidanceContext;
        if (string.IsNullOrEmpty(mlProseGuidanceContext) && mlProseGuidance != null && context.NodeId != Guid.Empty)
        {
            try { mlProseGuidanceContext = await mlProseGuidance.BuildGuidanceAsync(context.NodeId, ct); }
            catch { /* non-blocking — ML guidance is best-effort */ }
        }

        // Tension escalation: warn when recent beats have stagnated at low intensity.
        var tensionGuidanceContext = context.TensionGuidanceContext;
        if (string.IsNullOrEmpty(tensionGuidanceContext) && tensionService != null && context.NodeId != Guid.Empty)
            tensionGuidanceContext = tensionService.BuildGuidanceBlock(context.NodeId, mode);

        // Reader knowledge state: what the reader knows so far in this node.
        var readerKnowledgeContext = context.ReaderKnowledgeContext;
        if (string.IsNullOrEmpty(readerKnowledgeContext) && readerKnowledge != null && context.NodeId != Guid.Empty)
        {
            try { readerKnowledgeContext = await readerKnowledge.BuildKnowledgeBlockAsync(context.NodeId, ct); }
            catch { /* non-blocking */ }
        }

        // Character state constraints: gear, cyberware, status — zero LLM cost, pure DB query.
        var consequenceContext = context.ConsequenceContext;
        if (string.IsNullOrEmpty(consequenceContext) && consequence != null && context.CharactersInScene.Count > 0)
        {
            try { consequenceContext = consequence.BuildConstraints(context.CharactersInScene.ToList()); }
            catch { /* non-blocking */ }
        }

        // Cross-story persistent consequences for named characters (contract outcomes, faction burns).
        if (consequenceEngine != null && context.CharactersInScene.Count > 0)
        {
            try
            {
                var engineBlock = consequenceEngine.BuildConsequenceContext(context.CharactersInScene[0]);
                if (!string.IsNullOrEmpty(engineBlock))
                    consequenceContext = string.IsNullOrEmpty(consequenceContext)
                        ? engineBlock
                        : consequenceContext + "\n\n" + engineBlock;
            }
            catch { /* non-blocking */ }
        }

        // Ambient anomaly: New Weird background detail tagged to scene location (60% chance gate).
        var ambientAnomalyContext = context.AmbientAnomalyContext;
        if (string.IsNullOrEmpty(ambientAnomalyContext) && ambientAnomaly != null && !string.IsNullOrEmpty(context.Location))
        {
            try { ambientAnomalyContext = ambientAnomaly.FormatHints(context.Location); }
            catch { /* non-blocking */ }
        }

        // World state at beat: live entity state snapshot from EntityStateEvents (temporal, drifted from canon).
        var worldStateContext = context.WorldStateContext;
        if (string.IsNullOrEmpty(worldStateContext) && worldStateAtBeat != null && beatId != Guid.Empty)
        {
            try
            {
                var snapshot = await worldStateAtBeat.SnapshotAsync(beatId, ct: ct);
                worldStateContext = snapshot.FormatAsContextBlock();
            }
            catch { /* non-blocking */ }
        }

        // Narrative summary: rolling compressed memory of prior beats — long-node coherence.
        var narrativeSummaryContext = context.NarrativeSummaryContext;
        if (string.IsNullOrEmpty(narrativeSummaryContext) && narrativeSummary != null && context.NodeId != Guid.Empty)
        {
            try { narrativeSummaryContext = narrativeSummary.GetSummaryChain(); }
            catch { /* non-blocking */ }
        }

        // Chapter summaries: DB-backed prior-chapter memory (cross-session coherence).
        var chapterSummaryContext = context.ChapterSummaryContext;
        if (string.IsNullOrEmpty(chapterSummaryContext) && chapterSummary != null && context.NodeId != Guid.Empty)
        {
            try { chapterSummaryContext = await chapterSummary.BuildPriorSummaryContextAsync(context.NodeId, ct); }
            catch { /* non-blocking */ }
        }

        // Open threads: unresolved promises/plants/questions from prior beats.
        var openThreadsContext = context.OpenThreadsContext;
        if (string.IsNullOrEmpty(openThreadsContext) && openThreads != null && context.NodeId != Guid.Empty)
        {
            try { openThreadsContext = await openThreads.BuildContextAsync(context.NodeId, ct); }
            catch { /* non-blocking */ }
        }

        // Story Science: King + Storr craft laws — psychometric consistency, status dynamics,
        // curiosity gap, neural narrative, sensory specificity, prose anti-patterns, theory of mind.
        var storyScienceGuidance = context.StoryScienceGuidance;
        if (string.IsNullOrEmpty(storyScienceGuidance) && storyScience != null && totalBeats > 0)
        {
            try { storyScienceGuidance = storyScience.GetBeatGuidance(context, beatIndex, totalBeats, mode); }
            catch { /* non-blocking */ }
        }

        // Structural Blueprint: this story's pre-committed anti-tell decisions (StoryScope
        // countermeasures) — subplot carrier, anachrony cut, escalation floor, event type,
        // ending/resolution mode. Empty when the node has no blueprint; never blocks writing.
        var structuralBlueprintGuidance = context.StructuralBlueprintGuidance;
        if (string.IsNullOrEmpty(structuralBlueprintGuidance) && structuralBlueprint != null
            && context.NodeId != Guid.Empty && totalBeats > 0)
        {
            try { structuralBlueprintGuidance = await structuralBlueprint.BuildBeatInjectionAsync(context.NodeId, beatId, beatIndex, totalBeats, ct); }
            catch { /* non-blocking */ }
        }

        // StoryScope audit loop-back: prior audit findings for this node become
        // generation constraints — the audit corrects future beats, not just reports.
        if (context.NodeId != Guid.Empty && dbFactory != null)
        {
            try
            {
                var storyScopeGuidance = await BuildStoryScopeGuidanceAsync(context.NodeId, ct);
                if (storyScopeGuidance.Length > 0)
                    structuralBlueprintGuidance = structuralBlueprintGuidance.Length > 0
                        ? structuralBlueprintGuidance + "\n\n" + storyScopeGuidance
                        : storyScopeGuidance;
            }
            catch { /* non-blocking */ }
        }

        // Narrative Chart: offscreen character parallel activity — what characters not in this
        // scene are doing in parallel. Keeps the world continuous; injected as subtext context.
        var offscreenActivityContext = context.OffscreenActivityContext;
        if (string.IsNullOrEmpty(offscreenActivityContext) && narrativeChart != null
            && context.NodeId != Guid.Empty && totalBeats > 0)
        {
            try
            {
                var chart = await narrativeChart.BuildChartAsync(context.NodeId, ct);
                if (beatIndex < chart.Beats.Count)
                {
                    var crossSection = chart.Beats[beatIndex];
                    offscreenActivityContext = NarrativeChartService.BuildOffscreenContextBlock(crossSection);
                }
            }
            catch { /* non-blocking */ }
        }

        // ── Assemble enriched context ─────────────────────────────────────────

        var enriched = context with
        {
            PacingGuidance         = pacingGuidance,
            StructuralRoleGuidance = structuralGuidance,
            XRayContext            = xRayContext,
            EntityStackContext     = entityStackContext,
            DocStackContext        = docStackContext,
            LocationContext        = locationContext,
            DialogueContext        = dialogueContext,
            EmotionalGuidanceContext = emotionalGuidanceContext,
            MlProseGuidanceContext   = mlProseGuidanceContext,
            TensionGuidanceContext = tensionGuidanceContext,
            ReaderKnowledgeContext = readerKnowledgeContext,
            ConsequenceContext     = consequenceContext,
            AmbientAnomalyContext    = ambientAnomalyContext,
            WorldStateContext        = worldStateContext,
            NarrativeSummaryContext  = narrativeSummaryContext,
            ChapterSummaryContext    = chapterSummaryContext,
            OpenThreadsContext       = openThreadsContext,
            ContinuityContext        = continuityContext,
            StoryScienceGuidance     = storyScienceGuidance,
            OffscreenActivityContext = offscreenActivityContext,
            StructuralBlueprintGuidance = structuralBlueprintGuidance,
        };

        var startedAt = DateTime.UtcNow;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await generator.GenerateBeatAsync(enriched, ct);
        sw.Stop();

        // Telemetry: record exactly which docs + entities this beat pulled into working memory.
        if (telemetry != null && telemetry.IsActive)
        {
            try
            {
                var docs = (docResult?.Loaded ?? (IReadOnlyList<DocContextService.LoadedDoc>)Array.Empty<DocContextService.LoadedDoc>())
                    .Select(d => new ContextTelemetryService.DocLoad(d.RelativePath, d.Tier, d.Reason, d.Score, d.Chars)).ToList();
                var entList = entityContext?.GetActiveEntities(context.NodeId) ?? new List<EntityContextStack.StackEntry>();
                var ents = entList
                    .Take(EntityContextService.MaxInjectedEntities)
                    .Select(e => new ContextTelemetryService.EntityLoad(e.Name, e.EntityType, "stack", e.Score, e.Depth)).ToList();
                var title = context.BeatGoal ?? "";
                if (title.Length > 80) title = title[..80];
                telemetry.RecordBeat(new ContextTelemetryService.BeatRecord(
                    beatIndex, beatId.ToString("N"), title, startedAt, sw.Elapsed.TotalMilliseconds, result?.Length ?? 0, docs, ents));
            }
            catch { /* telemetry is best-effort — never affect prose */ }
        }

        // Fire-and-forget: coverage logging + reconciliation + tension recording + reader knowledge extraction.
        var pacingApplicable  = totalBeats > 0;
        var structApplicable  = totalBeats > 0;
        var combatApplicable  = mode == BeatMode.Combat;
        var nodeApplicable  = context.NodeId != Guid.Empty;
        var capturedResult    = result;
        var capturedNodeId  = context.NodeId;

        _ = Task.Run(async () =>
        {
          try
          {
            await monitor.LogBeatActivityAsync(beatId, context.NodeId, universeId,
            [
                new("Pacing",              IsApplicable: pacingApplicable,  IsActive: pacingApplicable && enriched.PacingGuidance.Length > 0,                 BlockSizeChars: enriched.PacingGuidance.Length),
                new("StoryMethodology",    IsApplicable: structApplicable,  IsActive: structApplicable && enriched.StructuralRoleGuidance.Length > 0,         BlockSizeChars: enriched.StructuralRoleGuidance.Length),
                new("PlantPayoff",         IsApplicable: nodeApplicable,  IsActive: nodeApplicable,                                                       BlockSizeChars: 0),
                new("StoryAudit",          IsApplicable: nodeApplicable,  IsActive: nodeApplicable,                                                       BlockSizeChars: 0),
                new("Combat",              IsApplicable: combatApplicable,  IsActive: combatApplicable,                                                       BlockSizeChars: combatApplicable ? CombatProseGuidance.Length : 0),
                new("EntityContext",       IsApplicable: nodeApplicable,  IsActive: entityStackContext.Length > 0,                                          BlockSizeChars: entityStackContext.Length),
                new("DocContext",          IsApplicable: nodeApplicable,  IsActive: docStackContext.Length > 0,                                             BlockSizeChars: docStackContext.Length),
                new("SceneContext",        IsApplicable: nodeApplicable,  IsActive: locationContext.Length > 0,                                             BlockSizeChars: locationContext.Length),
                new("DialogueService",     IsApplicable: nodeApplicable,  IsActive: dialogueContext.Length > 0,                                             BlockSizeChars: dialogueContext.Length),
                new("EmotionalGuidance",   IsApplicable: nodeApplicable,  IsActive: emotionalGuidanceContext.Length > 0,                                    BlockSizeChars: emotionalGuidanceContext.Length),
                new("TensionEscalation",   IsApplicable: nodeApplicable,          IsActive: tensionGuidanceContext.Length > 0,                                        BlockSizeChars: tensionGuidanceContext.Length),
                new("ReaderKnowledge",     IsApplicable: nodeApplicable,          IsActive: readerKnowledgeContext.Length > 0,                                        BlockSizeChars: readerKnowledgeContext.Length),
                new("Consequence",         IsApplicable: nodeApplicable,          IsActive: consequenceContext.Length > 0,                                            BlockSizeChars: consequenceContext.Length),
                new("AmbientAnomaly",      IsApplicable: !string.IsNullOrEmpty(context.Location), IsActive: ambientAnomalyContext.Length > 0,                          BlockSizeChars: ambientAnomalyContext.Length),
                new("WorldState",          IsApplicable: beatId != Guid.Empty,      IsActive: worldStateContext.Length > 0,                                             BlockSizeChars: worldStateContext.Length),
                new("NarrativeSummary",    IsApplicable: nodeApplicable,          IsActive: narrativeSummaryContext.Length > 0,                                       BlockSizeChars: narrativeSummaryContext.Length),
                new("ChapterSummary",      IsApplicable: nodeApplicable,          IsActive: chapterSummaryContext.Length > 0,                                         BlockSizeChars: chapterSummaryContext.Length),
                new("OpenThreads",         IsApplicable: nodeApplicable,          IsActive: openThreadsContext.Length > 0,                                            BlockSizeChars: openThreadsContext.Length),
                new("SceneContextAssembler", IsApplicable: beatId != Guid.Empty,  IsActive: xRayContext.Length > 0,                                                    BlockSizeChars: xRayContext.Length),
                new("ContinuityService",   IsApplicable: nodeApplicable,          IsActive: continuityContext.Length > 0,                                              BlockSizeChars: continuityContext.Length),
                new("StoryScience",        IsApplicable: totalBeats > 0,          IsActive: storyScienceGuidance.Length > 0,                                             BlockSizeChars: storyScienceGuidance.Length),
                new("NarrativeChart",      IsApplicable: nodeApplicable,          IsActive: offscreenActivityContext.Length > 0,                                         BlockSizeChars: offscreenActivityContext.Length),
                new("StructuralBlueprint", IsApplicable: nodeApplicable && totalBeats > 0, IsActive: structuralBlueprintGuidance.Length > 0,                              BlockSizeChars: structuralBlueprintGuidance.Length),
            ], CancellationToken.None);

            await modeDetector.PersistAsync(beatId, universeId, mode, confidence, method, CancellationToken.None);

            if (entityContext != null && context.NodeId != Guid.Empty && capturedResult?.Length > 0)
                await entityContext.ReconcileAsync(capturedResult, context.NodeId, beatId, universeId, CancellationToken.None);

            // Record tension history and extract reader revelations from the completed beat.
            tensionService?.RecordBeat(capturedNodeId, mode);
            if (readerKnowledge != null && capturedNodeId != Guid.Empty && !string.IsNullOrWhiteSpace(capturedResult))
                await readerKnowledge.ExtractAsync(capturedResult, capturedNodeId, CancellationToken.None);

            // Compress completed beat into rolling narrative summary for next beat.
            if (narrativeSummary != null && capturedNodeId != Guid.Empty && !string.IsNullOrWhiteSpace(capturedResult))
                await narrativeSummary.SummarizeSceneAsync(capturedResult, CancellationToken.None);

            // Open threads: detect new setups, mark resolved threads from this beat.
            if (openThreads != null && capturedNodeId != Guid.Empty && beatId != Guid.Empty && !string.IsNullOrWhiteSpace(capturedResult))
            {
                try { await openThreads.MarkResolvedAsync(capturedNodeId, beatId, capturedResult, CancellationToken.None); }
                catch { /* non-blocking */ }
                try { await openThreads.DetectAndRegisterAsync(capturedNodeId, beatId, capturedResult, CancellationToken.None); }
                catch { /* non-blocking */ }
            }
          }
          catch (Exception ex) { log.LogWarning(ex, "Post-write side effects failed for beat {BeatId}", beatId); }
        }, CancellationToken.None);

        return result ?? "";
    }

    /// <summary>
    /// Backfill coverage logs for an already-written beat WITHOUT regenerating prose.
    /// </summary>
    public async Task LogCoverageAsync(
        Guid beatId, Guid nodeId, string? beatGoal, string? proseHint,
        int beatIndex, int totalBeats, Guid universeId = default,
        CancellationToken ct = default)
    {
        if (nodeId == Guid.Empty) return;

        var (mode, confidence, method, pacingGuidance, structuralGuidance) =
            ComputeEnrichment(beatGoal, proseHint, beatIndex, totalBeats);

        var pacingApplicable = totalBeats > 0;
        var structApplicable = totalBeats > 0;
        var combatApplicable = mode == BeatMode.Combat;

        await monitor.LogBeatActivityAsync(beatId, nodeId, universeId,
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
    /// Shared enrichment computation — single source of truth for "what fires" so coverage
    /// logs match real generation behaviour.
    /// </summary>
    private (BeatMode Mode, float Confidence, string Method, string PacingGuidance, string StructuralGuidance)
        ComputeEnrichment(string? beatGoal, string? proseHint, int beatIndex, int totalBeats)
    {
        var (mode, confidence, method) = modeDetector.Detect(beatGoal, proseHint);

        var pacingInstruction = totalBeats > 0
            ? PacingService.GetPacing(beatIndex, totalBeats, beatGoal ?? "")
            : null;
        if (mode == BeatMode.Combat)
            pacingInstruction = new PacingInstruction(PacingService.PaceMode.Strike);

        var structuralGuidance = totalBeats > 0
            ? methodology.GetBeatGenerationGuidance(beatIndex, totalBeats)
            : "";

        if (mode == BeatMode.Combat)
            structuralGuidance = CombatProseGuidance + (structuralGuidance.Length > 0 ? "\n\n" + structuralGuidance : "");

        return (mode, confidence, method, pacingInstruction?.ProseGuidance ?? "", structuralGuidance);
    }

    // ── StoryScope guidance helper ────────────────────────────────────────────

    /// <summary>
    /// Query the Findings table for recent STORYSCOPE audit findings for this node
    /// and format them as generation constraints. Same loop-back pattern as
    /// BuildEmotionalGuidanceAsync — the audit corrects future writing.
    /// </summary>
    private async Task<string> BuildStoryScopeGuidanceAsync(Guid nodeId, CancellationToken ct)
    {
        if (dbFactory == null) return "";
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var slug = await db.Nodes.AsNoTracking()
            .Where(s => s.Id == nodeId)
            .Select(s => s.Slug)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(slug)) return "";

        var fp = $"node:{slug}";
        var catKey = FindingCategory.Other.ToString();
        var statusKey = FindingStatus.New.ToString();

        var summaries = await db.Findings.AsNoTracking()
            .Where(f => f.FilePath == fp
                        && f.Category == catKey
                        && f.Status == statusKey
                        && f.Summary.StartsWith("STORYSCOPE"))
            .OrderBy(f => f.Severity == "High" ? 0 : 1)
            .ThenByDescending(f => f.DetectedAt)
            .Take(3)
            .Select(f => new { f.Summary, f.SuggestedFix })
            .ToListAsync(ct);

        if (summaries.Count == 0) return "";

        var sb = new StringBuilder();
        sb.AppendLine("STORYSCOPE AUDIT GUIDANCE — a structural audit found these AI-fiction tells in this story; do not reproduce them in this beat:");
        foreach (var s in summaries)
        {
            sb.AppendLine($"• {s.Summary.Replace("STORYSCOPE ", "").Trim()}");
            if (!string.IsNullOrEmpty(s.SuggestedFix))
                sb.AppendLine($"  → {s.SuggestedFix}");
        }
        return sb.ToString().TrimEnd();
    }

    // ── Emotional guidance helper ─────────────────────────────────────────────

    /// <summary>
    /// Query the Findings table for recent EMOTIONAL-DEPTH blocking findings for this node
    /// and format them as a compact generation constraint block.
    /// </summary>
    private async Task<string> BuildEmotionalGuidanceAsync(Guid nodeId, CancellationToken ct)
    {
        if (dbFactory == null) return "";
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var slug = await db.Nodes.AsNoTracking()
            .Where(s => s.Id == nodeId)
            .Select(s => s.Slug)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(slug)) return "";

        var fp = $"node:{slug}";
        var catKey = FindingCategory.Other.ToString();
        var statusKey = FindingStatus.New.ToString();

        var summaries = await db.Findings.AsNoTracking()
            .Where(f => f.FilePath == fp
                        && f.Category == catKey
                        && f.Status == statusKey
                        && f.Summary.StartsWith("EMOTIONAL-DEPTH"))
            .OrderBy(f => f.Severity == "High" ? 0 : 1)
            .ThenByDescending(f => f.DetectedAt)
            .Take(3)
            .Select(f => f.Summary)
            .ToListAsync(ct);

        if (summaries.Count == 0) return "";

        var sb = new StringBuilder();
        sb.AppendLine("EMOTIONAL DEPTH GUIDANCE — prior examination found these weaknesses; address them in this beat:");
        foreach (var s in summaries)
            sb.AppendLine($"• {s.Replace("EMOTIONAL-DEPTH ", "").Trim()}");
        return sb.ToString().TrimEnd();
    }
}
