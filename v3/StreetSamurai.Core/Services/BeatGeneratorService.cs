using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

public class BeatGeneratorService
{
    private readonly ILlmService llm;
    private readonly WorldGraphService graph;
    private readonly LoreService canon;

    public BeatGeneratorService(ILlmService llm, WorldGraphService graph, LoreService canon)
    {
        this.llm = llm;
        this.graph = graph;
        this.canon = canon;
    }

    public async Task<string> GenerateBeatAsync(
        BeatContext context,
        FacetDefinition leadFacet,
        List<FacetDefinition> supportingFacets,
        CancellationToken ct = default)
    {
        var supportingVoices = string.Join("\n", supportingFacets.Select(f =>
            $"- {f.Label}: {f.VoiceTone}. May surface as italicized inner thoughts — the character arguing with themselves."));

        var coreMemories = string.Join("\n", leadFacet.CoreMemories.Select(m => $"  - {m}"));

        // Story bible already includes literary rules and motifs from SceneGenerationService
        var system = $"""
            {leadFacet.SystemPrompt}

            STORY BIBLE AND LITERARY RULES:
            {context.StoryBibleContext}

            SUPPORTING FACETS (may surface as italicized inner thoughts — the character's internal voices):
            {supportingVoices}

            CORE MEMORIES TO DRAW FROM:
            {coreMemories}

            WORLD CONTEXT (characters, locations, equipment, relationships — use these as canon facts):
            {context.RelationshipContext}
            {(context.LocationContext.Length > 0 ? "\nADDITIONAL LOCATION DETAIL:\n" + context.LocationContext : "")}
            """;

        var user = $"""
            SCENE SO FAR:
            {context.SceneSoFar}

            BEAT GOAL: {context.BeatGoal}

            Write the next beat of the scene. The lead voice is {leadFacet.Label} — shape the prose
            style accordingly. Supporting facets surface as the character's inner thoughts — italicized
            lines where a different part of the psyche pushes back, questions, or reacts. Format these
            as *italicized inner monologue* on their own line, like a person arguing with themselves.
            Do NOT use bracketed labels like [WOUND] or [IDEAL]. The reader should feel the shift
            in voice without being told which facet is speaking.

            Write 2-4 paragraphs. Make every word count.
            """;

        return await llm.GenerateAsync(system, user, leadFacet.Temperature, 2048, leadFacet.Model, ct);
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
