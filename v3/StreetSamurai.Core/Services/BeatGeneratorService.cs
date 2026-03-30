using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public class BeatGeneratorService
{
    private readonly ILlmService _llm;
    private readonly WorldGraphService _graph;
    private readonly CanonService _canon;

    public BeatGeneratorService(ILlmService llm, WorldGraphService graph, CanonService canon)
    {
        _llm = llm;
        _graph = graph;
        _canon = canon;
    }

    public async Task<string> GenerateBeatAsync(
        BeatContext context,
        FacetDefinition leadFacet,
        List<FacetDefinition> supportingFacets,
        CancellationToken ct = default)
    {
        var supportingVoices = string.Join("\n", supportingFacets.Select(f =>
            $"- {f.Label}: {f.VoiceTone}. May interject with short interior lines tagged {f.Label}."));

        var coreMemories = string.Join("\n", leadFacet.CoreMemories.Select(m => $"  - {m}"));

        var system = $"""
            {leadFacet.SystemPrompt}

            STORY BIBLE:
            {context.StoryBibleContext}

            LITERARY RULES:
            - Sentence max: 25 words
            - Every paragraph: action, sensory detail, or a lie
            - Repeat one sensory motif 3 times with shifting meaning
            - No generic noir, no slogans, no samurai cliches, no anime dialogue
            - No clean moral victories
            - Beat-by-beat character revelation, not plot racing

            SUPPORTING FACETS (may interject as labeled interior lines):
            {supportingVoices}

            CORE MEMORIES TO DRAW FROM:
            {coreMemories}

            RELATIONSHIP CONTEXT:
            {context.RelationshipContext}

            LOCATION:
            {context.LocationContext}
            """;

        var user = $"""
            SCENE SO FAR:
            {context.SceneSoFar}

            BEAT GOAL: {context.BeatGoal}

            Write the next beat of the scene. The lead voice is {leadFacet.Label} — shape the prose
            style accordingly. Supporting facets may interject as tagged interior lines (e.g., {leadFacet.Label} prose
            with [{supportingFacets.FirstOrDefault()?.Name.ToUpperInvariant() ?? "OTHER"}] interjections).

            Write 2-4 paragraphs. Make every word count.
            """;

        return await _llm.GenerateAsync(system, user, leadFacet.Temperature, 2048, leadFacet.Model, ct);
    }
}

public record BeatContext
{
    public string StoryBibleContext { get; init; } = "";
    public string RelationshipContext { get; init; } = "";
    public string LocationContext { get; init; } = "";
    public string SceneSoFar { get; init; } = "";
    public string BeatGoal { get; init; } = "";
}
