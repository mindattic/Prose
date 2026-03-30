using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public class SceneGenerationService
{
    private readonly FacetService _facets;
    private readonly ContextAnalyzerService _analyzer;
    private readonly BeatGeneratorService _beatGen;
    private readonly WorldGraphService _graph;
    private readonly CanonDatabaseService _canonDb;
    private readonly ICanonPathProvider _paths;

    public event Action<BeatGenerationProgress>? OnBeatProgress;
    public event Action<GeneratedBeat>? OnBeatCompleted;

    public SceneGenerationService(
        FacetService facets, ContextAnalyzerService analyzer, BeatGeneratorService beatGen,
        WorldGraphService graph, CanonDatabaseService canonDb, ICanonPathProvider paths)
    {
        _facets = facets;
        _analyzer = analyzer;
        _beatGen = beatGen;
        _graph = graph;
        _canonDb = canonDb;
        _paths = paths;
    }

    public async Task<GeneratedScene> GenerateSceneAsync(SceneRequest request, FacetState characterWeights, CancellationToken ct = default)
    {
        _graph.EnsureLoaded();
        var allFacets = _facets.LoadAllFacets();
        var storyBible = _canonDb.GetLiteraryRulesPrompt();
        var locationContext = request.Location != null ? _canonDb.GetDistrictContext(request.Location) : "";
        var relationshipContext = string.Join("\n\n---\n\n", request.Characters.Select(c => _canonDb.GetCharacterContext(c)).Where(c => c.Length > 0));

        var scene = new GeneratedScene { Request = request };
        var beats = new List<GeneratedBeat>();
        var recentLeads = new List<string>();
        var sceneSoFar = "";

        for (int i = 0; i < request.NumBeats; i++)
        {
            ct.ThrowIfCancellationRequested();

            var analysis = await _analyzer.AnalyzeAsync(
                $"{request.Goal}\n\nScene so far:\n{sceneSoFar}",
                request.Characters.Select(WorldGraphService.Slugify).ToList(),
                ct);

            var (lead, supporting) = _facets.SelectFacets(
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
                RelationshipContext = relationshipContext,
                LocationContext = locationContext,
                SceneSoFar = sceneSoFar,
                BeatGoal = request.Themes.Count > i
                    ? request.Themes[i]
                    : $"Continue the scene toward: {request.Goal}",
            };

            var text = await _beatGen.GenerateBeatAsync(beatContext, lead, supporting, ct);

            var beat = new GeneratedBeat
            {
                Index = i,
                Goal = beatContext.BeatGoal,
                LeadFacet = lead.Name,
                SupportingFacets = supporting.Select(f => f.Name).ToList(),
                Text = text,
                ContextTags = analysis.PsychologicalTriggers,
            };

            beats.Add(beat);
            recentLeads.Add(lead.Name);
            sceneSoFar += "\n\n" + text;

            OnBeatCompleted?.Invoke(beat);
        }

        return scene with { Beats = beats };
    }

}
