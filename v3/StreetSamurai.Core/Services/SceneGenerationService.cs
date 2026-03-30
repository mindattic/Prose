using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public class SceneGenerationService
{
    private readonly FacetService _facets;
    private readonly ContextAnalyzerService _analyzer;
    private readonly BeatGeneratorService _beatGen;
    private readonly WorldGraphService _graph;
    private readonly CanonService _canon;
    private readonly ICanonPathProvider _paths;
    private readonly YamlService _yaml;

    public event Action<BeatGenerationProgress>? OnBeatProgress;
    public event Action<GeneratedBeat>? OnBeatCompleted;

    public SceneGenerationService(
        FacetService facets,
        ContextAnalyzerService analyzer,
        BeatGeneratorService beatGen,
        WorldGraphService graph,
        CanonService canon,
        ICanonPathProvider paths,
        YamlService yaml)
    {
        _facets = facets;
        _analyzer = analyzer;
        _beatGen = beatGen;
        _graph = graph;
        _canon = canon;
        _paths = paths;
        _yaml = yaml;
    }

    public async Task<GeneratedScene> GenerateSceneAsync(SceneRequest request, FacetState characterWeights, CancellationToken ct = default)
    {
        _graph.EnsureLoaded();
        var allFacets = _facets.LoadAllFacets();
        var storyBible = LoadStoryBible();
        var locationContext = BuildLocationContext(request.Location);
        var relationshipContext = BuildRelationshipContext(request.Characters);

        var scene = new GeneratedScene { Request = request };
        var beats = new List<GeneratedBeat>();
        var recentLeads = new List<string>();
        var sceneSoFar = "";

        for (int i = 0; i < request.NumBeats; i++)
        {
            ct.ThrowIfCancellationRequested();

            // 1. Analyze context
            var analysis = await _analyzer.AnalyzeAsync(
                $"{request.Goal}\n\nScene so far:\n{sceneSoFar}",
                request.Characters.Select(WorldGraphService.Slugify).ToList(),
                ct);

            // 2. Select facets with rotation
            var (lead, supporting) = _facets.SelectFacets(
                characterWeights,
                analysis.PsychologicalTriggers,
                recentLeads);

            OnBeatProgress?.Invoke(new BeatGenerationProgress
            {
                BeatIndex = i + 1,
                TotalBeats = request.NumBeats,
                LeadFacet = lead.Name,
                Status = "generating",
            });

            // 3. Generate beat
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

    private string LoadStoryBible()
    {
        var path = Path.Combine(_paths.WorldDir, "story_bible.yaml");
        if (!File.Exists(path)) return "";
        try
        {
            var data = _yaml.LoadDynamic(path);
            var lines = new List<string>();
            if (data.TryGetValue("core_theme", out var theme)) lines.Add($"Core theme: {theme}");
            if (data.TryGetValue("tone", out var tone)) lines.Add($"Tone: {tone}");
            if (data.TryGetValue("genre", out var genre)) lines.Add($"Genre: {genre}");
            return string.Join("\n", lines);
        }
        catch { return ""; }
    }

    private string BuildLocationContext(string? location)
    {
        if (string.IsNullOrEmpty(location)) return "Location not specified.";
        var id = WorldGraphService.Slugify(location);
        return _graph.GetContextForNode(id);
    }

    private string BuildRelationshipContext(List<string> characters)
    {
        return string.Join("\n\n", characters.Select(c =>
        {
            var id = WorldGraphService.Slugify(c);
            return _graph.GetContextForNode(id);
        }).Where(c => !string.IsNullOrEmpty(c)));
    }
}
