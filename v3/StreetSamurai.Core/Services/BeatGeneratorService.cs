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

        var dialogueBlock = !string.IsNullOrWhiteSpace(context.DialogueContext)
            ? $"\n\n{context.DialogueContext}"
            : "";

        var system = $"""
            {leadFacet.SystemPrompt}

            STORY BIBLE AND LITERARY RULES:
            {context.StoryBibleContext}

            SUPPORTING FACETS (surface as italicized inner thoughts — the character arguing with themselves):
            {supportingVoices}

            CORE MEMORIES TO DRAW FROM:
            {coreMemories}

            WORLD CONTEXT (characters, locations, equipment, relationships — use as canon facts):
            {context.RelationshipContext}
            {(context.LocationContext.Length > 0 ? "\nADDITIONAL LOCATION DETAIL:\n" + context.LocationContext : "")}{dialogueBlock}
            """;

        var hasDialogue = context.DialogueContext.Length > 0;
        var dialogueInstruction = hasDialogue
            ? """

              DIALOGUE DIRECTION:
              Characters speak in their own voice — see profiles above. Each voice must be immediately
              distinct without dialogue tags. Do not name emotions. Do not have characters explain
              themselves. Subtext is load-bearing. What a character says to fill silence reveals
              more than what they say when they mean to speak.
              """
            : "";

        var user = $"""
            SCENE SO FAR:
            {context.SceneSoFar}

            BEAT GOAL: {context.BeatGoal}

            Write the next beat of the scene. The lead voice is {leadFacet.Label} — shape the prose
            style accordingly. Supporting facets surface as the character's inner thoughts — italicized
            lines where a different part of the psyche pushes back, questions, or reacts. Format these
            as *italicized inner monologue* on their own line, like a person arguing with themselves.
            Do NOT use bracketed labels like [WOUND] or [IDEAL]. The reader should feel the shift
            in voice without being told which facet is speaking.{dialogueInstruction}

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
    /// <summary>Per-character voice profiles and cross-character relationship dynamics from DialogueService.</summary>
    public string DialogueContext { get; init; } = "";
    public string SceneSoFar { get; init; } = "";
    public string BeatGoal { get; init; } = "";
}
