using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services;

namespace Prose.Mcp;

/// <summary>
/// Portable-writing-service plan, Phase 2 — <c>generate_scene</c>: write a scene or line of
/// dialog without a pre-existing Book/Chapter/Beat row. See
/// <see cref="OneShotGenerationService"/>'s doc comment for the full rationale (ephemeral by
/// default; attach an existing node to borrow its canon/continuity without writing to it).
/// </summary>
[McpServerToolType]
public class OneShotGenerationTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly OneShotGenerationService generation;
    private readonly HubInvoker hub;

    public OneShotGenerationTools(OneShotGenerationService generation, HubInvoker hub)
    {
        this.generation = generation;
        this.hub = hub;
    }

    [McpServerTool, Description("Write a scene or line of dialog without a pre-existing Book/Chapter/Beat row. " +
        "Ephemeral by default: pacing, dialogue voice profiles, canon-fact grounding, consequence/gear constraints, " +
        "ambient sensory grounding, and entity pre-check warnings all still apply. Pass node to borrow an existing " +
        "book's canon/continuity ('attached mode') without writing a Beat row to it.")]
    public Task<string> GenerateScene(
        [Description("What should happen in this scene/line — the beat goal.")] string beatGoal,
        [Description("Comma-separated character names on screen (activates dialogue voice profiles and continuity/consequence grounding).")] string? characters = null,
        [Description("Raw location hint for ambient sensory grounding, e.g. 'The Spine, Zone 3'.")] string? location = null,
        [Description("What's happening beneath the surface — foreshadowing, unspoken motivation, dramatic irony.")] string? subtext = null,
        [Description("Optional slug/NodeCode/GUID of an existing Book or Chapter to borrow canon and continuity from. Omit for pure ephemeral.")] string? node = null,
        [Description("Optional universe slug override; defaults to the session's current universe.")] string? universe = null,
        [Description("Zero-based position in an imagined beat sequence, for pacing guidance. 0 if unknown.")] int beatIndex = 0,
        [Description("Total beats in that imagined sequence, for pacing guidance. 0 disables positional pacing.")] int totalBeats = 0) =>
        hub.InvokeAsync(nameof(OneShotGenerationTools), nameof(GenerateSceneImpl),
            new { beatGoal, characters, location, subtext, node, universe, beatIndex, totalBeats });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GenerateSceneImpl(
        string beatGoal, string? characters, string? location, string? subtext,
        string? node, string? universe, int beatIndex, int totalBeats)
    {
        var characterList = string.IsNullOrWhiteSpace(characters)
            ? Array.Empty<string>()
            : characters.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        try
        {
            var result = await generation.GenerateAsync(new OneShotGenerationService.OneShotGenerationRequest(
                BeatGoal: beatGoal,
                Characters: characterList,
                Location: location,
                Subtext: subtext,
                Node: node,
                Universe: universe,
                BeatIndex: beatIndex,
                TotalBeats: totalBeats));

            return JsonSerializer.Serialize(new
            {
                text = result.Text,
                wordCount = result.WordCount,
                universe = result.UniverseSlug,
                attachedNode = result.AttachedNodeSlug,
            }, JsonOpts);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return JsonSerializer.Serialize(new { error = "generate_scene_failed", detail = ex.Message }, JsonOpts);
        }
    }
}
