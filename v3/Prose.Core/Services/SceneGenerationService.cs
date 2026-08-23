using Prose.Core.Interfaces;
using Prose.Core.Models;

namespace Prose.Core.Services;

public class SceneGenerationService
{
    private readonly ContextAnalyzerService analyzer;
    private readonly BeatGeneratorService beatGen;
    private readonly UniverseGraphService graph;
    private readonly DatabaseService canonDb;
    private readonly ValidationService validator;
    private readonly IPathProvider paths;
    private readonly SemanticIndexService semanticIndex;
    private readonly InferenceService inference;
    private readonly SceneContextBuilder contextBuilder;
    private readonly ConsequenceService consequences;
    private readonly AmbientAnomalyService anomalies;
    private readonly NarrativeSummaryService summaries;
    private readonly DialogueService dialogue;
    private readonly WorldStateService worldState;
    private readonly WorldStatePrecheckService precheck;
    private readonly CanonRetrievalService canonRetrieval;
    private readonly SceneContextAssembler xray;
    private readonly DocContextService? docCtx;
    private readonly ProsePatternGuard? proseGuard;

    // Dynamic Context Memory: stable session key so the DocContextStack LRU persists across beats in one scene.
    private readonly Guid sessionId = Guid.NewGuid();

    public event Action<BeatGenerationProgress>? OnBeatProgress;
    public event Action<GeneratedBeat>? OnBeatCompleted;

    public SceneGenerationService(
        ContextAnalyzerService analyzer, BeatGeneratorService beatGen,
        UniverseGraphService graph, DatabaseService canonDb, ValidationService validator,
        IPathProvider paths, SemanticIndexService semanticIndex, InferenceService inference,
        SceneContextBuilder contextBuilder, ConsequenceService consequences,
        AmbientAnomalyService anomalies, NarrativeSummaryService summaries,
        DialogueService dialogue, WorldStateService worldState,
        WorldStatePrecheckService precheck, CanonRetrievalService canonRetrieval,
        SceneContextAssembler xray,
        DocContextService? docCtx = null,
        ProsePatternGuard? proseGuard = null)
    {
        this.xray = xray;
        this.canonRetrieval = canonRetrieval;
        this.docCtx = docCtx;
        this.proseGuard = proseGuard;
        this.analyzer = analyzer;
        this.beatGen = beatGen;
        this.graph = graph;
        this.canonDb = canonDb;
        this.validator = validator;
        this.paths = paths;
        this.semanticIndex = semanticIndex;
        this.inference = inference;
        this.contextBuilder = contextBuilder;
        this.consequences = consequences;
        this.anomalies = anomalies;
        this.summaries = summaries;
        this.dialogue = dialogue;
        this.worldState = worldState;
        this.precheck = precheck;
    }

    public async Task<GeneratedScene> GenerateSceneAsync(SceneRequest request, CancellationToken ct = default)
    {
        graph.EnsureLoaded();
        var storyBible = canonDb.GetLiteraryRulesPrompt();

        // Build dialogue voice profiles once for the whole scene — all characters, all relationships
        var dialogueContext = dialogue.BuildDialogueContext(request.Characters);

        var session = new NarrativeSessionContext(graph, semanticIndex, inference, worldState);
        session.TouchAll(request.Characters);
        if (request.Location != null) session.Touch(request.Location);

        // Build ambient context (sensory profiles, weather, wildlife)
        var ambientContext = contextBuilder.BuildAmbientContext(request.Location);

        // Build character state constraints (injuries, status, possessions)
        var characterConstraints = await consequences.BuildConstraintsAsync(request.Characters, storyTime: null, ct);

        // Pre-write contradiction check — runs once per scene, output is a constraint
        // block injected into every beat prompt below. Blockers do not throw; the LLM
        // sees them as hard rules and the writer can override at review time.
        var precheckReport = precheck.Check(new PrecheckRequest(
            Characters: request.Characters,
            Location:   request.Location,
            Synopsis:   request.Goal,
            AsOf:       AsOfCursor.Current));
        var precheckConstraints = precheckReport.ToPromptConstraints();

        // Get narrative summary chain from previous scenes
        var summaryContext = summaries.GetSummaryChain();

        var scene = new GeneratedScene { Request = request };
        var beats = new List<GeneratedBeat>();
        var sceneSoFar = "";

        for (int i = 0; i < request.NumBeats; i++)
        {
            ct.ThrowIfCancellationRequested();

            var worldContext = session.BuildContext();

            // Get ambient anomaly hints for this scene
            var anomalyHints = anomalies.FormatHints(request.Location);

            // Get pacing instruction for this beat's position in the arc
            var beatGoal = request.Themes.Count > i
                ? request.Themes[i]
                : $"Continue the scene toward: {request.Goal}";
            var pacing = PacingService.GetPacing(i, request.NumBeats, beatGoal);

            // Full-interconnect reach: pull the most relevant canon across ALL
            // entity types (gear, drugs, materials, orgs, synthetics, …) for what
            // this beat is about, so the writer is grounded in the totality, not
            // just the graph's seven types. Excludes the POV cast (already in
            // worldContext) to avoid duplication.
            var sceneTail = sceneSoFar.Length > 1200 ? sceneSoFar[^1200..] : sceneSoFar;
            var canonBlock = await canonRetrieval.RetrieveContextBlockAsync(
                $"{request.Goal}\n{request.Location}\n{beatGoal}\n{sceneTail}",
                k: 12, excludeNames: request.Characters, ct: ct);
            if (canonBlock.Length > 0) worldContext = $"{worldContext}\n\n{canonBlock}";

            var analysis = await analyzer.AnalyzeAsync(
                $"{request.Goal}\n\nScene so far:\n{sceneSoFar}",
                request.Characters.Select(UniverseGraphService.Slugify).ToList(),
                ct);

            OnBeatProgress?.Invoke(new BeatGenerationProgress
            {
                BeatIndex = i + 1,
                TotalBeats = request.NumBeats,
                Status = "generating",
            });

            // X-Ray scene assembly (RFC 0002): who is on screen for THIS beat —
            // requested characters + whatever the goal/recent prose names — with
            // their voice fields, so each character speaks in their own register.
            string xrayBlock = "";
            try
            {
                var xrayCtx = await xray.AssembleAsync(
                    $"{string.Join(", ", request.Characters)}\n{request.Location}\n{beatGoal}\n{sceneTail}",
                    tokenBudget: 1200, ct);
                xrayBlock = xrayCtx.ContextBlock;
            }
            catch { /* X-Ray is an enhancer — generation proceeds without the roster */ }

            // Dynamic Context Memory: load always + topic docs for this beat (no node — freeform scene has no NodeId).
            // Gives the writer the BIBLE.digest.md universal core + any canon docs triggered by the beat goal.
            var docBlock = "";
            if (docCtx != null)
            {
                var docResult = await docCtx.PrepareContextAsync(
                    sessionId, nodeCode: null, triggerText: beatGoal,
                    tokenBudget: 1500, includeAlways: true, includeNode: false, ct: ct);
                docBlock = docResult.Block;
            }

            var beatContext = new BeatContext
            {
                StoryBibleContext = storyBible,
                RelationshipContext = worldContext,
                LocationContext = $"{ambientContext}\n{anomalyHints}\n{characterConstraints}\n{precheckConstraints}\n{summaryContext}\n{pacing.ProseGuidance}",
                DialogueContext = dialogueContext,
                XRayContext = xrayBlock,
                DocStackContext = docBlock,
                SceneSoFar = sceneSoFar,
                BeatGoal = beatGoal,
            };

            var text = await beatGen.GenerateBeatAsync(beatContext, ct);

            // Validate against canon — catch pronoun errors, dead characters, etc.
            var issues = validator.ValidateQuick(text);

            // Prose pattern guard — clichés, pseudo-profound, on-the-nose, italicised dialogue.
            var proseIssues = proseGuard?.Check(text)
                .Select(v => $"[{v.Category}] prose: {v.Rule}")
                ?? Enumerable.Empty<string>();

            // Scan for new entity mentions (keyword + semantic)
            var newEntities = session.ScanText(text);
            session.ScanTextSemantic(text);

            var beat = new GeneratedBeat
            {
                Index = i,
                Goal = beatContext.BeatGoal,
                Text = text,
                ContextTags = analysis.PsychologicalTriggers,
                ValidationIssues = issues.Select(iss => $"[{iss.Category}] {iss.EntityName}: {iss.Description}")
                    .Concat(proseIssues)
                    .ToList(),
            };

            beats.Add(beat);
            sceneSoFar += "\n\n" + text;

            OnBeatCompleted?.Invoke(beat);
        }

        // Compress completed scene into summary for the narrative chain
        if (sceneSoFar.Length > 0)
            await summaries.SummarizeSceneAsync(sceneSoFar, ct: ct);

        return scene with { Beats = beats };
    }

}
