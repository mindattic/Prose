using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public class SceneGenerationService
{
    private readonly FacetService facets;
    private readonly ContextAnalyzerService analyzer;
    private readonly BeatGeneratorService beatGen;
    private readonly WorldGraphService graph;
    private readonly DatabaseService canonDb;
    private readonly ValidationService validator;
    private readonly IPathProvider paths;
    private readonly SemanticIndexService semanticIndex;
    private readonly InferenceService inference;

    public event Action<BeatGenerationProgress>? OnBeatProgress;
    public event Action<GeneratedBeat>? OnBeatCompleted;

    public SceneGenerationService(
        FacetService facets, ContextAnalyzerService analyzer, BeatGeneratorService beatGen,
        WorldGraphService graph, DatabaseService canonDb, ValidationService validator,
        IPathProvider paths, SemanticIndexService semanticIndex, InferenceService inference)
    {
        this.facets = facets;
        this.analyzer = analyzer;
        this.beatGen = beatGen;
        this.graph = graph;
        this.canonDb = canonDb;
        this.validator = validator;
        this.paths = paths;
        this.semanticIndex = semanticIndex;
        this.inference = inference;
    }

    public async Task<GeneratedScene> GenerateSceneAsync(SceneRequest request, FacetState characterWeights, CancellationToken ct = default)
    {
        graph.EnsureLoaded();
        var allFacets = facets.LoadAllFacets();
        var storyBible = canonDb.GetLiteraryRulesPrompt();

        var session = new NarrativeSessionContext(graph, semanticIndex, inference);
        session.TouchAll(request.Characters);
        if (request.Location != null) session.Touch(request.Location);

        var scene = new GeneratedScene { Request = request };
        var beats = new List<GeneratedBeat>();
        var recentLeads = new List<string>();
        var sceneSoFar = "";

        for (int i = 0; i < request.NumBeats; i++)
        {
            ct.ThrowIfCancellationRequested();

            var worldContext = session.BuildContext();

            var analysis = await analyzer.AnalyzeAsync(
                $"{request.Goal}\n\nScene so far:\n{sceneSoFar}",
                request.Characters.Select(WorldGraphService.Slugify).ToList(),
                ct);

            var (lead, supporting) = facets.SelectFacets(
                characterWeights, analysis.PsychologicalTriggers, recentLeads);

            OnBeatProgress?.Invoke(new BeatGenerationProgress
            {
                BeatIndex = i + 1,
                TotalBeats = request.NumBeats,
                LeadFacet = lead.Name,
                Status = "generating",
            });

            var beatContext = new BeatContext
            {
                StoryBibleContext = storyBible,
                RelationshipContext = worldContext,
                SceneSoFar = sceneSoFar,
                BeatGoal = request.Themes.Count > i
                    ? request.Themes[i]
                    : $"Continue the scene toward: {request.Goal}",
            };

            var text = await beatGen.GenerateBeatAsync(beatContext, lead, supporting, ct);

            // Validate against canon — catch pronoun errors, dead characters, etc.
            var issues = validator.ValidateQuick(text);

            // Scan for new entity mentions (keyword + semantic)
            var newEntities = session.ScanText(text);
            session.ScanTextSemantic(text);

            var beat = new GeneratedBeat
            {
                Index = i,
                Goal = beatContext.BeatGoal,
                LeadFacet = lead.Name,
                SupportingFacets = supporting.Select(f => f.Name).ToList(),
                Text = text,
                ContextTags = analysis.PsychologicalTriggers,
                ValidationIssues = issues.Select(iss => $"[{iss.Category}] {iss.EntityName}: {iss.Description}").ToList(),
            };

            beats.Add(beat);
            recentLeads.Add(lead.Name);
            sceneSoFar += "\n\n" + text;

            OnBeatCompleted?.Invoke(beat);
        }

        return scene with { Beats = beats };
    }

}
