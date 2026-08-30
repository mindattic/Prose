using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using System.Text;

namespace Prose.Core.Services;

/// <summary>
/// Unified entry point for all beat prose generation. Replaces direct calls to
/// BeatGeneratorService in the workbench + CLI paths.
///
/// What it adds over BeatGeneratorService:
///   - Beat mode detection (Combat / EmotionalClimax / Dialogue / Transition / Revelation / Narrative)
///   - PacingService injection (positional arc + beat-goal keyword override)
///   - StoryMethodologyService injection (structural role: Opening Image, Catalyst, Midpoint, etc.)
///   - Combat prose rules injection when mode = Combat (extended version with Dissociated Observer examples)
///   - SceneContextBuilder injection for ambient sensory grounding, incl. the New Weird anomaly
///     layer absorbed from the retired AmbientAnomalyService (when Location is set on BeatContext)
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
    IDbContextFactory<ProseDbContext>? dbFactory = null,
    TensionEscalationService? tensionService = null,
    ReaderKnowledgeService? readerKnowledge = null,
    ConsequenceService? consequence = null,
    NarrativeSummaryService? narrativeSummary = null,
    WorldStateAtBeatService? worldStateAtBeat = null,
    ChapterSummaryService? chapterSummary = null,
    OpenThreadsService? openThreads = null,
    SceneContextAssembler? sceneAssembler = null,
    ContinuityService? continuity = null,
    StoryScienceService? storyScience = null,
    NarrativeChartService? narrativeChart = null,
    StructuralBlueprintService? structuralBlueprint = null,
    BookStateLedgerService? bookStateLedger = null,
    UniverseGraphService? universeGraph = null,
    CanonGroundingService? canonGrounding = null,
    LibertyReportService? libertyReport = null,
    SemanticFidelityService? semanticFidelity = null,
    PlantPayoffService? plantPayoffs = null,
    BookAuditService? bookAudit = null,
    SceneCollisionService? sceneCollision = null,
    VerificationContextService? verificationContext = null,
    BeatExtractionService? beatExtraction = null,
    ContinuityEnforcer? continuityEnforcer = null,
    FindingsService? findings = null,
    BeatPlaceService? beatPlace = null,
    MotifLedgerService? motifLedger = null)
{
    // Built from CombatProseConstants — single source of truth shared with CombatSceneWriter.
    static readonly string CombatProseGuidance =
        "BEAT MODE: COMBAT — action prose rules are in force.\n" +
        CombatProseConstants.ActionRules;

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
            await TraceStageAsync(nameof(EntityContextService), async () =>
                { entityStackContext = await entityContext.PrepareContextAsync(context.NodeId, beatId, context.BeatGoal, context.SceneSoFar, ct); });
        }

        // Fix 2: auto-populate CharactersInScene from entity context stack (character-type entries).
        // Fires DialogueService and ConsequenceService for every beat once the
        // stack is warm. Gracefully empty on cold-start (stack fills after first ReconcileAsync).
        if (context.CharactersInScene.Count == 0 && context.NodeId != Guid.Empty && entityContext != null)
        {
            TraceStage("CharactersInScene auto-populate", () =>
            {
                var active = entityContext.GetActiveEntities(context.NodeId)
                    .Where(e => string.Equals(e.EntityType, "character", StringComparison.OrdinalIgnoreCase))
                    .Take(3)
                    .Select(e => e.Name)
                    .ToList();
                if (active.Count > 0)
                    context = context with { CharactersInScene = active };
            });
        }

        // Fix 1: auto-populate XRayContext via SceneContextAssembler when beatId is known.
        // Injects per-character voice/psychology/wound/behavioral profiles for every entity on screen.
        //
        // 2026-08-28: moved ABOVE the DocContextService stage (was after the findings-guidance
        // blocks) and PersistPovAsync is now awaited, not fire-and-forget. The old order was an
        // ordering race: DocContextService read the beat's BeatEntityPresence 'pov' row to pin
        // the narrator's register (SS-A46 layer 4) BEFORE this stage — the only live writer of
        // that row — had run, so POV register pinning was a guaranteed no-op on every beat's
        // FIRST generation and only took effect on regeneration passes over the same beatId.
        var xRayContext = context.XRayContext;
        if (sceneAssembler != null && string.IsNullOrWhiteSpace(xRayContext) && beatId != Guid.Empty)
        {
            await TraceStageAsync(nameof(SceneContextAssembler), async () =>
            {
                var sc = await sceneAssembler.AssembleForBeatAsync(beatId, tokenBudget: 2000, ct);
                if (sc != null && !string.IsNullOrWhiteSpace(sc.ContextBlock))
                {
                    xRayContext = sc.ContextBlock;
                    // Populate BeatEntities from the canonical write path (fire-and-forget).
                    var capturedBeatId = beatId;
                    _ = Task.Run(() => sceneAssembler.PersistRosterAsync(capturedBeatId, sc, CancellationToken.None))
                        .ContinueWith(t => { if (t.IsFaulted) log.LogWarning(t.Exception, "[ProseWriterRouter] PersistRosterAsync failed for beat {Id}", capturedBeatId); }, TaskScheduler.Default);
                    // POV presence: awaited so the row exists before DocContextService reads it
                    // below. PersistPovAsync itself defers to any pre-existing 'pov' row (bible
                    // POV-map backfills win over the roster heuristic).
                    try { await sceneAssembler.PersistPovAsync(beatId, sc, ct); }
                    catch (Exception ex) { log.LogWarning(ex, "[ProseWriterRouter] PersistPovAsync failed for beat {Id}", beatId); }
                }
            });
        }

        // Doc context stack: load the rotating cast of pertinent canon .md docs (non-fatal).
        DocContextService.DocContextResult? docResult = null;
        var docStackContext = "";
        if (docContext != null && settings?.DocContextEnabled == true && context.NodeId != Guid.Empty)
        {
            await TraceStageAsync(nameof(DocContextService), async () =>
            {
                // POV register priority (GLMZ §0 / SS-A46 layer 4): find this beat's narrator from the
                // bible POV map (BeatEntityPresence 'pov' row) so its register is pinned/dominant — a
                // multi-POV book voices each beat in that beat's narrator, not a blend of everyone present.
                // Shared lookup (RFC 0011 Brick 1) — was inlined here AND independently re-implemented
                // in BeatChecklistGateService; now one place for both.
                var povEntityId = verificationContext != null
                    ? await verificationContext.GetPovEntityIdAsync(beatId, ct)
                    : null;

                var triggerText = (context.BeatGoal ?? "") + "\n" + (context.SceneSoFar ?? "");
                docResult = string.IsNullOrEmpty(context.DocScopeCode)
                    ? await docContext.PrepareForNodeAsync(context.NodeId, triggerText, tokenBudget: 8000, povEntityId: povEntityId, ct: ct)
                    : await docContext.PrepareContextAsync(context.NodeId, context.DocScopeCode, triggerText, tokenBudget: 8000, ct: ct);
                docStackContext = docResult.Block;
            });
        }

        // Node-bible fallback: an empty doc stack means the book's bible never reached the prompt
        // (typically docs/nodes/<CODE>.md not yet synced into MarkdownFiles). A book node must never
        // generate bible-blind — fall back to Nodes.NodeOutline and warn so the missing sync stays visible.
        if (docStackContext.Length == 0 && settings?.DocContextEnabled == true
            && context.NodeId != Guid.Empty && dbFactory != null)
        {
            await TraceStageAsync("NodeOutlineFallback", async () =>
            {
                await using var bibleDb = await dbFactory.CreateDbContextAsync(ct);
                var nodeBible = await bibleDb.Nodes.AsNoTracking()
                    .Where(n => n.Id == context.NodeId)
                    .Select(n => n.NodeOutline)
                    .FirstOrDefaultAsync(ct);
                if (!string.IsNullOrWhiteSpace(nodeBible))
                {
                    const int maxBibleChars = 16000;
                    docStackContext = "## NODE BIBLE (authoritative for this book — do not contradict)\n"
                        + (nodeBible.Length > maxBibleChars ? nodeBible[..maxBibleChars] : nodeBible);
                    log.LogWarning(
                        "Doc context stack EMPTY for node {NodeId} — fell back to Nodes.NodeOutline ({Chars} chars). Run 'prose --sync-markdown' to restore the full doc stack.",
                        context.NodeId, docStackContext.Length);
                }
                else
                {
                    log.LogWarning(
                        "Doc context stack EMPTY for node {NodeId} and no NodeOutline on the node — prose will generate WITHOUT canon context.",
                        context.NodeId);
                }
            });
        }

        // Fix 4: auto-populate Location from BookNode.DefaultLocation when caller doesn't set it.
        // Enables SceneContextBuilder (ambient grounding + New Weird anomaly layer) for every beat.
        //
        // 2026-08-22 fix: DefaultLocation only ever lives on the BOOK node (confirmed live:
        // 14/46 book nodes have it set, 0/465 chapter nodes ever do — there is no UI/CLI path
        // that writes it onto a chapter). Every real beat-generation call site sets
        // context.NodeId to the beat's owning CHAPTER, per the Book -> Chapter -> Beat hard
        // hierarchy (CLAUDE.md) — so the original `n.Id == context.NodeId` lookup queried the
        // chapter row's own (always-null) DefaultLocation and NEVER found a value for ANY beat
        // in ANY book, not just the 32/46 books that never had one authored. This walks up
        // ParentNodeId to the nearest book/series ancestor first, the same ancestor-walk shape
        // EntityDisambiguationService.ResolveNearestBookOrSeriesNodeIdAsync already uses for the
        // identical "given a chapter, find its book" problem.
        // Per-beat scene location (2026-08-28): the nearest prior beat's extracted PlaceName in
        // this chapter — scene-continuity default, far more scene-granular than the single
        // book-wide DefaultLocation below. Only consulted when the caller didn't set Location.
        if (string.IsNullOrWhiteSpace(context.Location) && beatPlace != null
            && context.NodeId != Guid.Empty && beatId != Guid.Empty)
        {
            await TraceStageAsync($"{nameof(BeatPlaceService)} prior-place", async () =>
            {
                var priorPlace = await beatPlace.GetPriorPlaceAsync(context.NodeId, beatId, ct);
                if (!string.IsNullOrWhiteSpace(priorPlace))
                    context = context with { Location = priorPlace };
            });
        }

        if (string.IsNullOrWhiteSpace(context.Location) && context.NodeId != Guid.Empty && dbFactory != null)
        {
            await TraceStageAsync("DefaultLocation fallback", async () =>
            {
                await using var locDb = await dbFactory.CreateDbContextAsync(ct);
                var bookId = await ResolveBookAncestorAsync(locDb, context.NodeId, ct);
                if (bookId == null) return;

                var defaultLoc = await locDb.Nodes.AsNoTracking()
                    .Where(n => n.Id == bookId.Value)
                    .Select(n => n.DefaultLocation)
                    .FirstOrDefaultAsync(ct);
                if (!string.IsNullOrWhiteSpace(defaultLoc))
                    context = context with { Location = defaultLoc };
            });
        }

        // ── New enrichments (SS-A28) ─────────────────────────────────────────

        // Ambient sensory grounding: SceneContextBuilder from the Location hint on BeatContext.
        var locationContext = context.LocationContext;
        if (sceneBuilder != null && string.IsNullOrEmpty(locationContext) && !string.IsNullOrEmpty(context.Location))
        {
            TraceStage(nameof(SceneContextBuilder), () =>
                { locationContext = sceneBuilder.BuildAmbientContext(context.Location, context.TimeOfDay, context.Weather); });
        }

        // Dialogue voice profiles: auto-fire on Dialogue/EmotionalClimax beats when characters are listed.
        // Gate: only inject voice profiles when the beat mode actually calls for dialogue.
        var dialogueContext = context.DialogueContext;
        if (dialogue != null && string.IsNullOrEmpty(dialogueContext) && context.CharactersInScene.Count > 0)
        {
            if (mode == BeatMode.Dialogue || mode == BeatMode.EmotionalClimax)
            {
                TraceStage(nameof(DialogueService), () =>
                    {
                        // XRay (assembled above) already carries the canonical Speech* fields for
                        // everyone on-page — ask DialogueService for only its complement then.
                        dialogueContext = dialogue.BuildDialogueContext(
                            context.CharactersInScene.ToList(),
                            includeVoiceProfiles: string.IsNullOrWhiteSpace(xRayContext));
                    });
            }
            else
                log.LogDebug("[gate] DialogueService skipped (mode={Mode}, not Dialogue/EmotionalClimax)", mode);
        }

        // Emotional depth feedback: pull prior examination findings for this node.
        var emotionalGuidanceContext = context.EmotionalGuidanceContext;
        if (string.IsNullOrEmpty(emotionalGuidanceContext) && dbFactory != null && context.NodeId != Guid.Empty)
        {
            await TraceStageAsync(nameof(EmotionalDepthService), async () =>
            {
                emotionalGuidanceContext = await BuildFindingsGuidanceAsync(
                    context.NodeId,
                    summaryPrefix: "EMOTIONAL-DEPTH",
                    headerLine: "EMOTIONAL DEPTH GUIDANCE — prior examination found these weaknesses; address them in this beat:",
                    ct: ct);
            });
        }

        // Readability feedback (plan "Making Prose readable...", 2026-08-13): recent
        // low-Flesch READABILITY findings (filed by SanityScanBackgroundService) become a
        // forward-looking guidance block — same "prior findings become future generation
        // constraints" pattern as EMOTIONAL-DEPTH above and STORYSCOPE below.
        var readabilityGuidanceContext = context.ReadabilityGuidanceContext;
        if (string.IsNullOrEmpty(readabilityGuidanceContext) && dbFactory != null && context.NodeId != Guid.Empty)
        {
            await TraceStageAsync(nameof(BeatProseMetricsService), async () =>
            {
                readabilityGuidanceContext = await BuildFindingsGuidanceAsync(
                    context.NodeId,
                    summaryPrefix: "READABILITY",
                    headerLine: "READABILITY — recent beats in this book scored below the clarity floor; write shorter, plainer sentences and cut interpretive gloss:",
                    category: FindingCategory.ProseHealth,
                    ct: ct);
            });
        }

        // Reader-Proxy QA loop-back (2026-08-22 fix): ComprehensionDefect (comprehension probes),
        // CraftChecklist (docs/DELIGHT.md binary checklist), and ReaderGripe (findings-only gripe
        // jury) findings were filed correctly by their own audit tools but NEVER queried by any
        // loop-back mechanism — only EMOTIONAL-DEPTH/READABILITY/STORYSCOPE fed forward into later
        // beats. Same "prior findings become future generation constraints" pattern as those three.
        var readerProxyGuidanceContext = context.ReaderProxyGuidanceContext;
        if (string.IsNullOrEmpty(readerProxyGuidanceContext) && dbFactory != null && context.NodeId != Guid.Empty)
        {
            await TraceStageAsync("ReaderProxyQA guidance", async () =>
            {
                var comprehension = await BuildFindingsGuidanceAsync(
                    context.NodeId, summaryPrefix: "COMPREHENSION",
                    headerLine: "READER COMPREHENSION — prior probes found readers missed or misread these; make them unambiguous:",
                    category: FindingCategory.ComprehensionDefect, ct: ct);
                var craft = await BuildFindingsGuidanceAsync(
                    context.NodeId, summaryPrefix: "CHECKLIST",
                    headerLine: "CRAFT CHECKLIST — recent beats hit these banned mannerisms/DELIGHT violations; avoid repeating them:",
                    category: FindingCategory.CraftChecklist, ct: ct);
                var gripe = await BuildFindingsGuidanceAsync(
                    context.NodeId, summaryPrefix: "GRIPE",
                    headerLine: "READER GRIPES — a reader jury confirmed these complaints; do not repeat the pattern:",
                    category: FindingCategory.ReaderGripe, ct: ct);
                var lint = await BuildFindingsGuidanceAsync(
                    context.NodeId, summaryPrefix: "LINT",
                    headerLine: "PROSE LINT — the mechanical linter flagged these habits in this book; avoid them in this beat:",
                    category: FindingCategory.CraftChecklist, ct: ct);
                readerProxyGuidanceContext = string.Join("\n\n", new[] { comprehension, craft, gripe, lint }.Where(s => !string.IsNullOrEmpty(s)));
            });
        }

        // Continuity-violation feedback (2026-08-22 fix): prior CONTINUITY-VIOLATION findings
        // (filed by ContinuityEnforcer below, after generation) become forward-looking
        // guidance — same "prior findings become future generation constraints" pattern as
        // EMOTIONAL-DEPTH/READABILITY above and Reader-Proxy QA below.
        var continuityViolationGuidanceContext = context.ContinuityViolationGuidanceContext;
        if (string.IsNullOrEmpty(continuityViolationGuidanceContext) && dbFactory != null && context.NodeId != Guid.Empty)
        {
            await TraceStageAsync($"{nameof(ContinuityEnforcer)} guidance", async () =>
            {
                continuityViolationGuidanceContext = await BuildFindingsGuidanceAsync(
                    context.NodeId, summaryPrefix: "CONTINUITY-VIOLATION",
                    headerLine: "CONTINUITY — a prior beat contradicted established canon; do not repeat the mistake:",
                    category: FindingCategory.Contradiction, ct: ct);
            });
        }

        // Fix 3: build canonical facts block for characters in scene from ContinuityService.
        // Injects CANONICAL/CONFIRMED claims as do-not-contradict constraints before generation.
        var continuityContext = context.ContinuityContext;
        if (continuity != null && string.IsNullOrEmpty(continuityContext) && context.CharactersInScene.Count > 0)
        {
            TraceStage(nameof(ContinuityService), () =>
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
                    .Where(c => sceneNames.Any(n =>
                        c.EntityName.StartsWith(n, StringComparison.OrdinalIgnoreCase) ||
                        n.StartsWith(c.EntityName, StringComparison.OrdinalIgnoreCase)))
                    // 2026-08-23: never hand the model point-in-time state as a
                    // do-not-contradict constraint. A stale location_current from a previous
                    // book would instruct this beat to put the character where they used to be
                    // — actively causing the drift this block exists to prevent. See
                    // ContinuityService.VolatilePredicates.
                    .Where(c => !ContinuityService.IsVolatilePredicate(c.Predicate))
                    .Take(24)
                    .ToList();
                if (claims.Count > 0)
                {
                    foreach (var claim in claims)
                        sb.AppendLine($"- {claim.EntityName}: {claim.Predicate} {claim.Object}");
                    continuityContext = sb.ToString().TrimEnd();
                }
            });
        }

        // Tension escalation: warn when recent beats have stagnated at low intensity.
        // Gate: fewer than 3 prior beats means no escalation history to analyse.
        var tensionGuidanceContext = context.TensionGuidanceContext;
        if (string.IsNullOrEmpty(tensionGuidanceContext) && tensionService != null && context.NodeId != Guid.Empty)
        {
            if (beatIndex > 2)
                TraceStage(nameof(TensionEscalationService), () =>
                    { tensionGuidanceContext = tensionService.BuildGuidanceBlock(context.NodeId, mode); });
            else
                log.LogDebug("[gate] TensionEscalationService skipped (beatIndex={BeatIndex} ≤ 2, insufficient history)", beatIndex);
        }

        // Reader knowledge state: what the reader knows so far in this node.
        var readerKnowledgeContext = context.ReaderKnowledgeContext;
        if (string.IsNullOrEmpty(readerKnowledgeContext) && readerKnowledge != null && context.NodeId != Guid.Empty)
        {
            await TraceStageAsync(nameof(ReaderKnowledgeService), async () =>
                { readerKnowledgeContext = await readerKnowledge.BuildKnowledgeBlockAsync(context.NodeId, ct); });
        }

        // Character state constraints: gear, cyberware, status — zero LLM cost, pure DB query.
        var consequenceContext = context.ConsequenceContext;
        if (string.IsNullOrEmpty(consequenceContext) && consequence != null && context.CharactersInScene.Count > 0)
        {
            await TraceStageAsync(nameof(ConsequenceService), async () =>
                { consequenceContext = await consequence.BuildConstraintsAsync(context.CharactersInScene.ToList(), storyTime: null, ct); });
        }

        // ConsequenceEngine stage removed 2026-08-28: its KV store ('world_consequences') lost
        // its only writer when the --write-story/--refine-story contract loop was removed, so
        // every beat paid to read a permanently stale/empty blob. Cross-book consequences, if
        // rebuilt, should come from the DB-backed ConsequenceService family, not a KV file.

        // World state at beat: live entity state snapshot from EntityStateEvents (temporal, drifted from canon).
        // Scoped to CharactersInScene when possible — SnapshotAsync's own docstring warns the
        // unscoped path is "expensive on large DBs"; it also caps Edges/EntityStateEvents at
        // Take(500)/Take(2000) with no meaningful ORDER BY for the unscoped case, so leaving this
        // unscoped silently drops rows once either table exceeds the cap (found 2026-08-15 while
        // verifying the motorcycle Dynamic Edge State backfill — see README "World Graph and
        // Interconnectivity").
        var worldStateContext = context.WorldStateContext;
        if (string.IsNullOrEmpty(worldStateContext) && worldStateAtBeat != null && beatId != Guid.Empty)
        {
            await TraceStageAsync(nameof(WorldStateAtBeatService), async () =>
            {
                IEnumerable<Guid>? sceneEntityIds = null;
                if (context.CharactersInScene.Count > 0 && dbFactory != null)
                {
                    await using var namesDb = await dbFactory.CreateDbContextAsync(ct);
                    var names = context.CharactersInScene.Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
                    sceneEntityIds = await namesDb.Entities.AsNoTracking()
                        .Where(e => names.Contains(e.Name))
                        .Select(e => e.Id)
                        .ToListAsync(ct);
                }

                var snapshot = await worldStateAtBeat.SnapshotAsync(beatId, entityIds: sceneEntityIds, ct: ct);
                worldStateContext = snapshot.FormatAsContextBlock();
            });
        }

        // Narrative summary: rolling compressed memory of prior beats — long-node coherence.
        // LoadAsync restores the chain from DB so it survives app restarts.
        var narrativeSummaryContext = context.NarrativeSummaryContext;
        if (narrativeSummary != null && context.NodeId != Guid.Empty)
        {
            await TraceStageAsync(nameof(NarrativeSummaryService), async () =>
            {
                await narrativeSummary.LoadAsync(context.NodeId, ct);
                if (string.IsNullOrEmpty(narrativeSummaryContext))
                    narrativeSummaryContext = narrativeSummary.GetSummaryChain();
            });
        }

        // Chapter summaries: DB-backed prior-chapter memory (cross-session coherence).
        // Gate: beat 0 cannot have prior chapter summaries to inject; skip the DB query.
        var chapterSummaryContext = context.ChapterSummaryContext;
        if (string.IsNullOrEmpty(chapterSummaryContext) && chapterSummary != null && context.NodeId != Guid.Empty)
        {
            if (beatIndex > 0)
            {
                await TraceStageAsync(nameof(ChapterSummaryService), async () =>
                    { chapterSummaryContext = await chapterSummary.BuildPriorSummaryContextAsync(context.NodeId, ct); });
            }
            else
                log.LogDebug("[gate] ChapterSummaryService skipped (beatIndex=0, no prior chapters yet)");
        }

        // Open threads: unresolved promises/plants/questions from prior beats.
        var openThreadsContext = context.OpenThreadsContext;
        if (string.IsNullOrEmpty(openThreadsContext) && openThreads != null && context.NodeId != Guid.Empty)
        {
            await TraceStageAsync(nameof(OpenThreadsService), async () =>
                { openThreadsContext = await openThreads.BuildContextAsync(context.NodeId, ct); });
        }

        // Motifs in play (2026-08-28): recurring images from the BookMotifs ledger — the LLM
        // deepens/refracts established motifs instead of scattering new one-off images.
        var motifContext = context.MotifContext;
        if (string.IsNullOrEmpty(motifContext) && motifLedger != null && context.NodeId != Guid.Empty)
        {
            await TraceStageAsync(nameof(MotifLedgerService), async () =>
                { motifContext = await motifLedger.BuildGuidanceAsync(context.NodeId, ct); });
        }

        // Story plot state: arc-level named states (crises, dramatic questions, objectives,
        // threats, alliances) across all beats — prevents crisis-amnesia on long nodes.
        var plotEventsContext = context.PlotEventsContext;
        if (string.IsNullOrEmpty(plotEventsContext) && bookStateLedger != null && context.NodeId != Guid.Empty)
        {
            await TraceStageAsync(nameof(BookStateLedgerService), async () =>
                { plotEventsContext = await bookStateLedger.BuildContextAsync(context.NodeId, ct); });
        }

        // Story Science: King + Storr craft laws — psychometric consistency, status dynamics,
        // curiosity gap, neural narrative, sensory specificity, prose anti-patterns, theory of mind.
        var storyScienceGuidance = context.StoryScienceGuidance;
        if (string.IsNullOrEmpty(storyScienceGuidance) && storyScience != null && totalBeats > 0)
        {
            TraceStage(nameof(StoryScienceService), () =>
                { storyScienceGuidance = storyScience.GetBeatGuidance(context, beatIndex, totalBeats, mode); });
        }

        // Scene Collision: what specifically happens when the on-page characters' documented
        // psychology and circumstance collide — refines HOW the beat goal plays out for these
        // exact people, does not change WHAT the beat goal is. Gated on 2+ characters (a
        // "collision" needs at least two parties) and skipped for Combat (CombatProseGuidance
        // already owns that texture) and beats with no XRay roster to compute from.
        var sceneCollisionGuidance = context.SceneCollisionGuidance;
        if (string.IsNullOrEmpty(sceneCollisionGuidance) && sceneCollision != null
            && mode != BeatMode.Combat && context.CharactersInScene.Count >= 2
            && !string.IsNullOrWhiteSpace(xRayContext) && !string.IsNullOrWhiteSpace(context.BeatGoal))
        {
            await TraceStageAsync(nameof(SceneCollisionService), async () =>
            {
                var collision = await sceneCollision.ComputeAsync(
                    context.CharactersInScene, xRayContext, worldStateContext, consequenceContext,
                    context.BeatGoal, locationContext, ct);
                if (collision != null)
                    sceneCollisionGuidance = SceneCollisionService.FormatForPrompt(collision);
            });
        }

        // Structural Blueprint: this book's pre-committed anti-tell decisions (StoryScope
        // countermeasures) — subplot carrier, anachrony cut, escalation floor, event type,
        // ending/resolution mode. Empty when the node has no blueprint; never blocks writing.
        //
        // 2026-08-28: the three structural mechanisms below (blueprint slice, Track B beat
        // contract, STORYSCOPE findings loop-back) used to be concatenated into one string as
        // they were computed, so the coverage log's "StructuralBlueprint" and "BeatContract"
        // rows both keyed off the same merged value and could not distinguish which mechanism
        // actually fired. They are now tracked separately and merged only at prompt-assembly.
        var blueprintSliceGuidance = context.StructuralBlueprintGuidance;
        if (string.IsNullOrEmpty(blueprintSliceGuidance) && structuralBlueprint != null
            && context.NodeId != Guid.Empty && totalBeats > 0)
        {
            await TraceStageAsync(nameof(StructuralBlueprintService), async () =>
                { blueprintSliceGuidance = await structuralBlueprint.BuildBeatInjectionAsync(context.NodeId, beatId, beatIndex, totalBeats, ct); });
        }

        // Beat contract (Track B — Truth-First Architecture): load the BeatBlueprintDecision row
        // for this beat and augment the structural guidance with its declared purpose + pre-state.
        // Non-blocking: if the node has a blueprint but no decision row, log a warning only.
        var beatContractGuidance = "";
        if (beatId != Guid.Empty && dbFactory != null)
        {
            await TraceStageAsync("BeatBlueprintDecision", async () =>
            {
                await using var bdDb = await dbFactory.CreateDbContextAsync(ct);
                var decision = await bdDb.BeatBlueprintDecisions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.BeatId == beatId, ct);

                if (decision == null)
                {
                    // Check whether the node has a blueprint at all (only warn when blueprint exists)
                    var hasBlueprint = await bdDb.NodeStructuralBlueprints
                        .AnyAsync(bp => bp.NodeId == context.NodeId, ct);
                    if (hasBlueprint)
                        log.LogWarning(
                            "[ProseWriterRouter] Beat {BeatId} has a structural blueprint but no BeatBlueprintDecision row. " +
                            "Run prose --generate-blueprint --slug <slug> to generate per-beat contracts, or " +
                            "prose --migrate-blueprint-rows to backfill existing stories.",
                            beatId);
                }
                else
                {
                    var contractLines = new List<string>();
                    if (!string.IsNullOrWhiteSpace(decision.DeclaredPurpose))
                        contractLines.Add($"BEAT CONTRACT — declared purpose: {decision.DeclaredPurpose}");
                    if (!string.IsNullOrWhiteSpace(decision.WorldStatePre))
                        contractLines.Add($"WORLD STATE ENTERING: {decision.WorldStatePre}");

                    if (contractLines.Count > 0)
                        beatContractGuidance = string.Join("\n", contractLines);
                }
            });
        }

        // StoryScope audit loop-back: prior audit findings for this node become
        // generation constraints — the audit corrects future beats, not just reports.
        var storyScopeLoopbackGuidance = "";
        if (context.NodeId != Guid.Empty && dbFactory != null)
        {
            await TraceStageAsync("StoryScopeGuidance", async () =>
            {
                storyScopeLoopbackGuidance = await BuildFindingsGuidanceAsync(
                    context.NodeId,
                    summaryPrefix: "STORYSCOPE",
                    headerLine: "STORYSCOPE AUDIT GUIDANCE — a structural audit found these AI-fiction tells in this book; do not reproduce them in this beat:",
                    includeSuggestedFix: true,
                    category: FindingCategory.StoryScope,
                    ct: ct);
            });
        }

        // Merge the three structural signals for the prompt (tracked separately for coverage).
        var structuralBlueprintGuidance = string.Join("\n\n",
            new[] { blueprintSliceGuidance, beatContractGuidance, storyScopeLoopbackGuidance }
                .Where(s => !string.IsNullOrEmpty(s)));

        // Narrative Chart: offscreen character parallel activity — what characters not in this
        // scene are doing in parallel. Keeps the world continuous; injected as subtext context.
        // Gate: first 3 beats have no parallel activity context worth fetching.
        var offscreenActivityContext = context.OffscreenActivityContext;
        if (string.IsNullOrEmpty(offscreenActivityContext) && narrativeChart != null
            && context.NodeId != Guid.Empty && totalBeats > 0)
        {
            if (beatIndex > 2)
            {
                await TraceStageAsync(nameof(NarrativeChartService), async () =>
                {
                    var chart = await narrativeChart.BuildChartAsync(context.NodeId, ct);
                    if (beatIndex < chart.Beats.Count)
                    {
                        var crossSection = chart.Beats[beatIndex];
                        offscreenActivityContext = NarrativeChartService.BuildOffscreenContextBlock(crossSection);
                    }
                });
            }
            else
                log.LogDebug("[gate] NarrativeChartService skipped (beatIndex={BeatIndex} ≤ 2, no prior activity to cross-cut)", beatIndex);
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
            ReadabilityGuidanceContext = readabilityGuidanceContext,
            ReaderProxyGuidanceContext = readerProxyGuidanceContext,
            ContinuityViolationGuidanceContext = continuityViolationGuidanceContext,
            TensionGuidanceContext = tensionGuidanceContext,
            ReaderKnowledgeContext = readerKnowledgeContext,
            ConsequenceContext     = consequenceContext,
            WorldStateContext        = worldStateContext,
            NarrativeSummaryContext  = narrativeSummaryContext,
            ChapterSummaryContext    = chapterSummaryContext,
            OpenThreadsContext       = openThreadsContext,
            MotifContext             = motifContext,
            PlotEventsContext        = plotEventsContext,
            ContinuityContext        = continuityContext,
            StoryScienceGuidance     = storyScienceGuidance,
            OffscreenActivityContext = offscreenActivityContext,
            StructuralBlueprintGuidance = structuralBlueprintGuidance,
            SceneCollisionGuidance   = sceneCollisionGuidance,
            BeatIndex                = beatIndex,
            TotalBeats               = totalBeats,
        };

        // ── C1: Entity pre-check (soft gate — warns, never blocks) ────────────────
        // Extract candidate proper nouns from BeatGoal and flag any that are not in
        // the canon WorldGraph. Unknown names get an ENTITY PRE-CHECK WARNINGS block
        // injected into the dynamic system prompt so the LLM keeps them ambiguous.
        if (universeGraph != null && !string.IsNullOrWhiteSpace(context.BeatGoal))
        {
            TraceStage("EntityPreCheck", () =>
            {
                var unknowns = FindUnknownEntityNames(context.BeatGoal, universeGraph);
                if (unknowns.Count > 0)
                {
                    var warnBlock = "ENTITY PRE-CHECK WARNINGS — the following names in the beat goal are NOT established in canon:\n" +
                        string.Join("\n", unknowns.Select(n => $"  • {n}")) + "\n" +
                        "Do not invent backstory, abilities, or relationships for these names. " +
                        "If you reference them, keep them ambiguous until the user seeds them into the database.\n\n";
                    enriched = enriched with { EntityPreCheckWarnings = warnBlock };
                }
            });
        }

        var startedAt = DateTime.UtcNow;

        // Beat Context Archive, Part F2: persist the whole merged BeatContext BEFORE the LLM
        // call, so a trace exists even for a beat that fails to generate. Best-effort, fire-
        // and-forget - same posture as every other archive write in this pipeline; a DB
        // hiccup here must never block or fail the write itself.
        if (dbFactory != null && beatId != Guid.Empty)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await using var db = await dbFactory.CreateDbContextAsync(CancellationToken.None);
                    db.BeatContextTraces.Add(new Data.Entities.BeatContextTrace
                    {
                        BeatId = beatId,
                        NodeId = enriched.NodeId,
                        UniverseId = universeId,
                        ContextJson = System.Text.Json.JsonSerializer.Serialize(enriched),
                    });
                    await db.SaveChangesAsync(CancellationToken.None);
                }
                catch (Exception ex) { log.LogWarning(ex, "[ProseWriterRouter] failed to record BeatContextTrace for beat {BeatId}", beatId); }
            }, CancellationToken.None);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        string? result;
        // Beat Context Archive, Part F1: ambient tag so LlmRouter's LlmPromptCapture rows for
        // this call know which beat they belong to, without widening ILlmService's signature.
        LlmActionContext.CurrentBeatId = beatId;
        try
        {
            result = await generator.GenerateBeatAsync(enriched, ct);
        }
        finally
        {
            LlmActionContext.CurrentBeatId = null;
        }
        sw.Stop();

        // Telemetry: record exactly which docs + entities this beat pulled into working memory.
        // Beat Context Archive follow-up (2026-08-21): this block used to run only when
        // telemetry.IsActive - i.e. only inside an external BeginRun/EndRun-bracketed batch.
        // Nothing in the codebase ever calls BeginRun, so DcmRun/DcmBeatSnapshot rows (and the
        // live SignalR DCM-Viz push) never fired for a real beat, and --beat-archive's Docs[]
        // section was always empty. Every beat now gets its own implicit single-beat Run when
        // no batch Run is already active, so the common case (one beat via --expand-beat,
        // the UI, etc.) is captured too, not just a hypothetical future batch caller.
        if (telemetry != null)
        {
            TraceStage(nameof(ContextTelemetryService), () =>
            {
                var docs = (docResult?.Loaded ?? (IReadOnlyList<DocContextService.LoadedDoc>)Array.Empty<DocContextService.LoadedDoc>())
                    .Select(d => new ContextTelemetryService.DocLoad(d.RelativePath, d.Tier, d.Reason, d.Score, d.Chars)).ToList();
                var entList = entityContext?.GetActiveEntities(context.NodeId) ?? new List<EntityContextStack.StackEntry>();
                var ents = entList
                    .Take(EntityContextService.MaxInjectedEntities)
                    .Select(e => new ContextTelemetryService.EntityLoad(e.EntityId, e.Name, e.EntityType, "stack", e.Score, e.Depth)).ToList();

                // DCM Gantt logging: capture the FULL working set (not budget-clipped) when DcmLoggingEnabled.
                // This gives the visualization accurate lifecycle data for all resident docs, not just the
                // subset that fit within the token budget.
                IReadOnlyList<ContextTelemetryService.StackDocEntry>? dcmFullSet = null;
                if (settings?.DcmLoggingEnabled == true && docContext != null && context.NodeId != Guid.Empty)
                {
                    dcmFullSet = docContext.GetActive(context.NodeId)
                        .Select(e => new ContextTelemetryService.StackDocEntry(e.RelativePath, e.Tier, e.Reason, e.Score))
                        .ToList();
                }

                var title = context.BeatGoal ?? "";
                if (title.Length > 80) title = title[..80];

                var ownRun = !telemetry.IsActive;
                if (ownRun)
                    telemetry.BeginRun(Guid.NewGuid(), context.NodeId, "", "single-beat", docContext != null, startedAt, 0, 0);
                telemetry.RecordBeat(new ContextTelemetryService.BeatRecord(
                    beatIndex, beatId.ToString("N"), title, startedAt, sw.Elapsed.TotalMilliseconds, result?.Length ?? 0, docs, ents, dcmFullSet));
                if (ownRun)
                    telemetry.EndRun(DateTime.UtcNow, 0, 0);
            });
        }

        // PlantPayoff/StoryAudit coverage was previously hardcoded to "active whenever
        // applicable" (IsActive: nodeApplicable) regardless of whether BeatGeneratorService's
        // internal plantBlock/commandmentBlock actually came back non-empty — those are local
        // variables inside BeatGeneratorService.GenerateBeatAsync never surfaced back here, so
        // workflow_status kept reporting 100% coverage for both even if either silently failed
        // or was unwired for a book. Mirror BeatGeneratorService's own computation (same
        // services, same non-blocking try/catch) so coverage logging measures the real content
        // length like every other entry in the table below.
        var plantBlockLen = 0;
        if (plantPayoffs != null && context.NodeId != Guid.Empty)
        {
            try { plantBlockLen = (await plantPayoffs.BuildPlantContextAsync(context.NodeId, beatIndex, totalBeats, ct)).Length; }
            catch (Exception ex) when (ex is not OperationCanceledException) { /* non-blocking */ }
        }
        var commandmentBlockLen = 0;
        if (bookAudit != null && context.NodeId != Guid.Empty && dbFactory != null)
        {
            try
            {
                await using var cdb = await dbFactory.CreateDbContextAsync(ct);
                var s = await cdb.Nodes.AsNoTracking()
                    .Where(x => x.Id == context.NodeId)
                    .Select(x => new { x.PreviousNodeId, x.UniverseId })
                    .FirstOrDefaultAsync(ct);
                if (s != null)
                    commandmentBlockLen = bookAudit.BuildCommandmentContext(s.PreviousNodeId.HasValue, s.UniverseId).Length;
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { /* non-blocking */ }
        }

        // Fire-and-forget: coverage logging + reconciliation + tension recording + reader knowledge extraction.
        var pacingApplicable      = totalBeats > 0;
        var structApplicable      = totalBeats > 0;
        var combatApplicable      = mode == BeatMode.Combat;
        var nodeApplicable        = context.NodeId != Guid.Empty;
        var beatContractApplicable = beatId != Guid.Empty;
        var capturedResult        = result;
        var capturedNodeId    = context.NodeId;
        var capturedBeatGoal  = context.BeatGoal;
        var capturedEntityRoster = entityStackContext.Length > 0 ? entityStackContext : null;

        _ = Task.Run(async () =>
        {
          try
          {
            await TraceStageAsync(nameof(WorkflowMonitorService), async () => { await monitor.LogBeatActivityAsync(beatId, context.NodeId, universeId,
            [
                new("Pacing",              IsApplicable: pacingApplicable,  IsActive: pacingApplicable && enriched.PacingGuidance.Length > 0,                 BlockSizeChars: enriched.PacingGuidance.Length),
                new("StoryMethodology",    IsApplicable: structApplicable,  IsActive: structApplicable && enriched.StructuralRoleGuidance.Length > 0,         BlockSizeChars: enriched.StructuralRoleGuidance.Length),
                new("PlantPayoff",         IsApplicable: nodeApplicable,    IsActive: plantBlockLen > 0,                                                      BlockSizeChars: plantBlockLen),
                new("StoryAudit",          IsApplicable: nodeApplicable,    IsActive: commandmentBlockLen > 0,                                                BlockSizeChars: commandmentBlockLen),
                new("Combat",              IsApplicable: combatApplicable,  IsActive: combatApplicable,                                                       BlockSizeChars: combatApplicable ? CombatProseGuidance.Length : 0),
                new("EntityContext",       IsApplicable: nodeApplicable,    IsActive: entityStackContext.Length > 0,                                          BlockSizeChars: entityStackContext.Length),
                new("DocContext",          IsApplicable: nodeApplicable,    IsActive: docStackContext.Length > 0,                                             BlockSizeChars: docStackContext.Length),
                new("SceneContext",        IsApplicable: nodeApplicable,    IsActive: locationContext.Length > 0,                                             BlockSizeChars: locationContext.Length),
                new("DialogueService",     IsApplicable: nodeApplicable,    IsActive: dialogueContext.Length > 0,                                             BlockSizeChars: dialogueContext.Length),
                new("EmotionalGuidance",   IsApplicable: nodeApplicable,    IsActive: emotionalGuidanceContext.Length > 0,                                    BlockSizeChars: emotionalGuidanceContext.Length),
                new("ReadabilityGuidance", IsApplicable: nodeApplicable,    IsActive: readabilityGuidanceContext.Length > 0,                                  BlockSizeChars: readabilityGuidanceContext.Length),
                new("ReaderProxyGuidance", IsApplicable: nodeApplicable,    IsActive: readerProxyGuidanceContext.Length > 0,                                  BlockSizeChars: readerProxyGuidanceContext.Length),
                new("ContinuityViolationGuidance", IsApplicable: nodeApplicable, IsActive: continuityViolationGuidanceContext.Length > 0,                     BlockSizeChars: continuityViolationGuidanceContext.Length),
                new("TensionEscalation",   IsApplicable: nodeApplicable,    IsActive: tensionGuidanceContext.Length > 0,                                      BlockSizeChars: tensionGuidanceContext.Length),
                new("ReaderKnowledge",     IsApplicable: nodeApplicable,    IsActive: readerKnowledgeContext.Length > 0,                                      BlockSizeChars: readerKnowledgeContext.Length),
                new("Consequence",         IsApplicable: nodeApplicable,    IsActive: consequenceContext.Length > 0,                                          BlockSizeChars: consequenceContext.Length),
                new("WorldState",          IsApplicable: beatId != Guid.Empty,  IsActive: worldStateContext.Length > 0,                                       BlockSizeChars: worldStateContext.Length),
                new("NarrativeSummary",    IsApplicable: nodeApplicable,    IsActive: narrativeSummaryContext.Length > 0,                                     BlockSizeChars: narrativeSummaryContext.Length),
                new("ChapterSummary",      IsApplicable: nodeApplicable,    IsActive: chapterSummaryContext.Length > 0,                                       BlockSizeChars: chapterSummaryContext.Length),
                new("OpenThreads",         IsApplicable: nodeApplicable,    IsActive: openThreadsContext.Length > 0,                                          BlockSizeChars: openThreadsContext.Length),
                new("MotifLedger",         IsApplicable: nodeApplicable,    IsActive: motifContext.Length > 0,                                                BlockSizeChars: motifContext.Length),
                new("StoryStateLedger",    IsApplicable: nodeApplicable,    IsActive: plotEventsContext.Length > 0,                                           BlockSizeChars: plotEventsContext.Length),
                new("SceneContextAssembler", IsApplicable: beatId != Guid.Empty, IsActive: xRayContext.Length > 0,                                            BlockSizeChars: xRayContext.Length),
                new("ContinuityService",   IsApplicable: nodeApplicable,    IsActive: continuityContext.Length > 0,                                           BlockSizeChars: continuityContext.Length),
                new("StoryScience",        IsApplicable: totalBeats > 0,    IsActive: storyScienceGuidance.Length > 0,                                        BlockSizeChars: storyScienceGuidance.Length),
                new("NarrativeChart",      IsApplicable: nodeApplicable,    IsActive: offscreenActivityContext.Length > 0,                                    BlockSizeChars: offscreenActivityContext.Length),
                // Three independent structural signals (2026-08-28 — previously all three rows'
                // IsActive derived from the same merged string, over-counting each mechanism).
                new("StructuralBlueprint", IsApplicable: nodeApplicable && totalBeats > 0, IsActive: !string.IsNullOrEmpty(blueprintSliceGuidance),              BlockSizeChars: blueprintSliceGuidance?.Length ?? 0),
                new("BeatContract",        IsApplicable: beatContractApplicable,           IsActive: beatContractApplicable && !string.IsNullOrEmpty(beatContractGuidance), BlockSizeChars: beatContractGuidance.Length),
                new("StoryScopeLoopback",  IsApplicable: nodeApplicable,                   IsActive: !string.IsNullOrEmpty(storyScopeLoopbackGuidance),          BlockSizeChars: storyScopeLoopbackGuidance.Length),
            ], CancellationToken.None); });

            await TraceStageAsync(nameof(BeatModeDetector), async () =>
                { await modeDetector.PersistAsync(beatId, universeId, mode, confidence, method, CancellationToken.None); });

            if (entityContext != null && context.NodeId != Guid.Empty && !string.IsNullOrWhiteSpace(capturedResult))
            {
                await TraceStageAsync($"{nameof(EntityContextStack)}.ReconcileAsync", async () =>
                    { await entityContext.ReconcileAsync(capturedResult, context.NodeId, beatId, universeId, CancellationToken.None); });
            }

            // Record tension history.
            tensionService?.RecordBeat(capturedNodeId, mode);

            // Post-write extraction cluster (RFC 0009 §9.4, "item 1"): reader-knowledge facts,
            // scene summary, new/resolved open threads, and arc-level plot-state transitions all
            // used to be five separate Haiku calls each re-reading capturedResult.
            // BeatExtractionService fans this out to each service's Persist*-only method in ONE
            // call. 2026-08-23: removed the five-call fallback that used to run when
            // beatExtraction was null — BeatExtractionService is unconditionally singleton-
            // registered in production DI (ServiceCollectionExtensions.cs), so that branch was
            // confirmed dead in every real deployment; it only doubled the maintenance surface.
            if (capturedNodeId != Guid.Empty && !string.IsNullOrWhiteSpace(capturedResult) && beatExtraction != null)
            {
                await TraceStageAsync($"{nameof(BeatExtractionService)}.ExtractAllAsync", async () =>
                    { await beatExtraction.ExtractAllAsync(capturedNodeId, beatId, beatIndex, capturedResult, CancellationToken.None); });
            }

            // Chapter-close summary extraction (2026-08-22 fix): ChapterSummaryService's write
            // side previously only ran inside `prose --auto-run`'s own ChapterCloseProcessorService
            // call — a beat written via --expand-beat/--run-corpus never persisted a
            // NodeChapterSummaries row, even though the READ side (BuildPriorSummaryContextAsync
            // above) fires unconditionally every beat. Fires here when this was the chapter's
            // LAST beat (beatIndex == totalBeats - 1, scoped to whatever node context.NodeId is —
            // the chapter, for every real call site except the flat-book legacy path), keyed by
            // the resolved book id + this chapter's position among its book's leaf chapters —
            // the same numbering AutoRunCli itself uses — so both paths write the same
            // (nodeId, chapterIndex) row. Harmless if AutoRunCli's own explicit call also fires
            // for the same chapter later (ExtractAndSaveAsync upserts) — that call runs AFTER
            // reflow, so it naturally supersedes this pre-reflow snapshot with the final text.
            if (chapterSummary != null && dbFactory != null && capturedNodeId != Guid.Empty
                && totalBeats > 0 && beatIndex == totalBeats - 1 && !string.IsNullOrWhiteSpace(capturedResult))
            {
                await TraceStageAsync($"{nameof(ChapterSummaryService)}.ExtractAndSaveAsync", async () =>
                {
                    await using var chDb = await dbFactory.CreateDbContextAsync(CancellationToken.None);
                    var bookId = await ResolveBookAncestorAsync(chDb, capturedNodeId, CancellationToken.None);
                    if (bookId == null) return;

                    var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(chDb, bookId.Value, CancellationToken.None);
                    var chapterIndex = leafIds.IndexOf(capturedNodeId);
                    if (chapterIndex < 0) return; // not a real chapter under this book — nothing to key by

                    var chapterBeats = await (
                        from bn in chDb.BeatNodes.AsNoTracking()
                        join b in chDb.Beats.AsNoTracking() on bn.BeatId equals b.Id
                        where bn.NodeId == capturedNodeId
                        orderby bn.SortKey
                        select b.Text).ToListAsync(CancellationToken.None);
                    var chapterProse = string.Join("\n\n", chapterBeats.Where(t => !string.IsNullOrWhiteSpace(t)));

                    await chapterSummary.ExtractAndSaveAsync(bookId.Value, chapterIndex, chapterProse, CancellationToken.None);
                });
            }

            // ContinuityEnforcer (2026-08-22 fix): closes the "ContinuityService constraints are
            // pure prompt-side hope with zero verification" gap. Immediate, same-beat check
            // against exactly the CANONICAL/CONFIRMED claims shown in the canon block above —
            // not the asynchronous, indirect Trinity Reconciliation re-extraction sweep, which
            // can lag by sessions and never ties back to the specific beat/constraint set.
            // Findings loop back into later beats via BuildFindingsGuidanceAsync exactly like
            // EMOTIONAL-DEPTH/READABILITY/STORYSCOPE/Reader-Proxy QA above.
            if (continuityEnforcer != null && findings != null && capturedNodeId != Guid.Empty
                && context.CharactersInScene.Count > 0 && !string.IsNullOrWhiteSpace(capturedResult))
            {
                await TraceStageAsync(nameof(ContinuityEnforcer), async () =>
                {
                    List<ContinuityViolation> violations;
                    try
                    {
                        violations = await continuityEnforcer.EnforceAsync(
                            capturedResult, context.CharactersInScene.ToList(), CancellationToken.None);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        log.LogWarning(ex, "ContinuityEnforcer failed for node {NodeId} — not filing (could-not-evaluate is not the same as clean)", capturedNodeId);
                        return;
                    }
                    if (violations.Count == 0 || dbFactory == null) return;

                    await using var fdb = await dbFactory.CreateDbContextAsync(CancellationToken.None);
                    var slug = await fdb.Nodes.AsNoTracking()
                        .Where(n => n.Id == capturedNodeId).Select(n => n.Slug).FirstOrDefaultAsync(CancellationToken.None);
                    if (string.IsNullOrEmpty(slug)) return;
                    var fp = $"node:{slug}";
                    foreach (var v in violations)
                    {
                        findings.Upsert(
                            filePath: fp,
                            chapterId: null,
                            category: FindingCategory.Contradiction,
                            severity: FindingSeverity.High,
                            summary: $"CONTINUITY-VIOLATION [{v.EntityName}] {v.Predicate}: {v.Explanation}",
                            snippet: v.EstablishedFact,
                            suggestedFix: $"Established fact: {v.EstablishedFact}. Rewrite the contradicting line.");
                    }
                });
            }

            // C2: CanonGroundingService — flag PROVISIONAL-ENTITY findings for invented names (opt-in).
            if (canonGrounding != null && (settings?.AutoCanonGrounding ?? false) && beatId != Guid.Empty && !string.IsNullOrWhiteSpace(capturedResult))
            {
                await TraceStageAsync($"{nameof(CanonGroundingService)}.AnalyzeAndScaffoldAsync", async () =>
                    { await canonGrounding.AnalyzeAndScaffoldAsync(capturedResult, $"beat:{beatId}", CancellationToken.None); });
            }

            // C3: HarvestRevealedDetails — propose XRAY-REVEAL findings for new details revealed in prose (opt-in).
            if (sceneAssembler != null && (settings?.AutoHarvestRevealedDetails ?? false) && beatId != Guid.Empty && !string.IsNullOrWhiteSpace(capturedResult))
            {
                await TraceStageAsync($"{nameof(SceneContextAssembler)}.HarvestRevealedDetailsAsync", async () =>
                    { await sceneAssembler.HarvestRevealedDetailsAsync(beatId, CancellationToken.None); });
            }

            // D: Liberty Report — Rule of Cool analysis (always fires when beatId is real; Haiku call, ~$0.002).
            if (libertyReport != null && beatId != Guid.Empty && !string.IsNullOrWhiteSpace(capturedResult))
            {
                await TraceStageAsync($"{nameof(LibertyReportService)}.AnalyseAsync", async () =>
                    { await libertyReport.AnalyseAsync(beatId, capturedResult, capturedBeatGoal, capturedEntityRoster, CancellationToken.None); });
            }

            // E: Semantic Fidelity — Goodhart's Law check (prose vs. its own stated beat goal).
            // Was previously only wired into the manual-edit save path, never the generation
            // path that authors most beats — closed 2026-08-08.
            if (semanticFidelity != null && beatId != Guid.Empty && capturedNodeId != Guid.Empty
                && !string.IsNullOrWhiteSpace(capturedResult) && !string.IsNullOrWhiteSpace(capturedBeatGoal))
            {
                await TraceStageAsync($"{nameof(SemanticFidelityService)}.CheckBeatIntentDriftAsync", async () =>
                    { await semanticFidelity.CheckBeatIntentDriftAsync(beatId, capturedNodeId, capturedResult, capturedBeatGoal, CancellationToken.None); });
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

        // DELIGHT (positive prose doctrine, docs/DELIGHT.md): emphasize the reader-loved moves that
        // fit this beat's mode. CRAFT.md keeps the beat from being disliked; this pushes it toward loved.
        var delightGuidance = DelightProseGuidance.GetForMode(mode);
        structuralGuidance = structuralGuidance.Length > 0
            ? structuralGuidance + "\n\n" + delightGuidance
            : delightGuidance;

        return (mode, confidence, method, pacingInstruction?.ProseGuidance ?? "", structuralGuidance);
    }

    // Beat Context Archive, Part F3 (2026-08-21): generic per-stage tracing. Every enrichment
    // block above already shared a `try { ... } catch (ex) { log.LogWarning(...) }` shape but
    // only ever logged on FAILURE - a successful stage produced no log line at all, and
    // nothing recorded timing. Wrapping the SAME try body in these two helpers (async for
    // await-ing services, sync for the handful of purely synchronous ones) gets a uniform
    // "[beat-trace] {Service} ok in {Ms}ms" or "FAILED after {Ms}ms" line through the router's
    // own ILogger for literally every stage, with ZERO edits to any of the ~20 service files -
    // RingBufferLoggerProvider/Serilog (Part E) already sit on that same ILogger pipeline, so
    // this shows up live in the Logs tab and durably in log-*.txt for free. This answers "what's
    // executing when, in what order" without touching a single one of the individual services;
    // "what did each stage actually produce" is BeatContextTrace's job (Part F2), not this one's.
    private void TraceStage(string serviceName, Action body)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            body();
            log.LogInformation("[beat-trace] {Service} ok in {Ms}ms", serviceName, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[beat-trace] {Service} FAILED after {Ms}ms, continuing", serviceName, sw.ElapsedMilliseconds);
        }
    }

    private async Task TraceStageAsync(string serviceName, Func<Task> body)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await body();
            log.LogInformation("[beat-trace] {Service} ok in {Ms}ms", serviceName, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[beat-trace] {Service} FAILED after {Ms}ms, continuing", serviceName, sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Query the Findings table for recent audit findings matching a summary prefix
    /// and format them as generation constraints (the audit loop-back pattern).
    /// Replaces the former BuildEmotionalGuidanceAsync and BuildStoryScopeGuidanceAsync methods.
    /// </summary>
    private async Task<string> BuildFindingsGuidanceAsync(
        Guid nodeId, string summaryPrefix, string headerLine,
        bool includeSuggestedFix = false, int maxItems = 3,
        FindingCategory category = FindingCategory.Other,
        CancellationToken ct = default)
    {
        if (dbFactory == null) return "";
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var slug = await db.Nodes.AsNoTracking()
            .Where(s => s.Id == nodeId)
            .Select(s => s.Slug)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(slug)) return "";

        var fp = $"node:{slug}";
        var catKey    = category.ToString();
        var statusKey = FindingStatus.New.ToString();

        // StartsWith, not ==: several Reader-Proxy QA categories (CraftChecklist, ComprehensionDefect,
        // ReaderGripe) file findings scoped to a specific beat/chapter subpath
        // ("node:{slug}/beat:{id}", "node:{slug}/ch:{index}"), not the bare book-level path —
        // an exact match against `fp` silently found zero rows for those categories even when
        // findings existed (2026-08-22 fix). Safe for the categories that DO use the bare path
        // (EMOTIONAL-DEPTH, READABILITY, STORYSCOPE) since StartsWith is a strict superset of ==.
        var findings = await db.Findings.AsNoTracking()
            .Where(f => f.FilePath.StartsWith(fp)
                        && f.Category == catKey
                        && f.Status == statusKey
                        && f.Summary.StartsWith(summaryPrefix))
            .OrderBy(f => f.Severity == "High" ? 0 : 1)
            .ThenByDescending(f => f.DetectedAt)
            .Take(maxItems)
            .Select(f => new { f.Summary, f.SuggestedFix })
            .ToListAsync(ct);

        if (findings.Count == 0) return "";

        var sb = new StringBuilder();
        sb.AppendLine(headerLine);
        foreach (var f in findings)
        {
            sb.AppendLine($"• {f.Summary.Replace(summaryPrefix + " ", "").Trim()}");
            if (includeSuggestedFix && !string.IsNullOrEmpty(f.SuggestedFix))
                sb.AppendLine($"  → {f.SuggestedFix}");
        }
        return sb.ToString().TrimEnd();
    }

    // C1 helper — extract capitalized word-groups from the beat goal that look like proper nouns
    // and are NOT found in the WorldGraph. Short words (≤2 chars) and common English words are
    // filtered. Zero-cost: graph is already in memory.
    private static readonly System.Text.RegularExpressions.Regex ProperNounPattern =
        new(@"\b([A-Z][a-z]{2,}(?:\s+[A-Z][a-z]{2,})*)\b",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly HashSet<string> CommonWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "The", "This", "That", "Here", "There", "When", "What", "Where", "Who", "Which",
        "And", "But", "Yet", "For", "Nor", "So", "Then", "Now", "Just", "Still",
        "He", "She", "They", "His", "Her", "Their", "Its", "Our", "Your", "My",
        "After", "Before", "During", "Inside", "Outside", "Through", "Against",
    };

    private static List<string> FindUnknownEntityNames(string beatGoal, UniverseGraphService graph)
    {
        var allNodes   = graph.AllNodes();
        var knownNames = new HashSet<string>(allNodes.Select(n => n.Name), StringComparer.OrdinalIgnoreCase);

        var candidates = ProperNounPattern.Matches(beatGoal)
            .Select(m => m.Value.Trim())
            .Where(n => !CommonWords.Contains(n) && n.Length > 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return candidates.Where(c => !knownNames.Contains(c)).ToList();
    }

    /// <summary>
    /// Walk ParentNodeId from any node (typically a chapter) up to the nearest "book" or
    /// "series" ancestor. Shared by the DefaultLocation fallback and the chapter-close summary
    /// extraction below — same shape as EntityDisambiguationService.ResolveNearestBookOrSeriesNodeIdAsync,
    /// duplicated locally (rather than taking a dependency on that service) since ProseWriterRouter
    /// already opens its own short-lived DbContext for each of these one-off lookups.
    /// </summary>
    private static async Task<Guid?> ResolveBookAncestorAsync(ProseDbContext db, Guid nodeId, CancellationToken ct)
    {
        var currentId = (Guid?)nodeId;
        for (var depth = 0; depth < 5 && currentId is { } cid; depth++)
        {
            var row = await db.Nodes.AsNoTracking()
                .Where(n => n.Id == cid)
                .Select(n => new { n.Kind, n.ParentNodeId })
                .FirstOrDefaultAsync(ct);
            if (row == null) return null;
            if (row.Kind is "book" or "series") return cid;
            currentId = row.ParentNodeId;
        }
        return null;
    }
}
