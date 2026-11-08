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
        "Grounded in the universe's canon (PRS-T926; works for game universes and draft texts with no book node): the " +
        "characters, location and about entities are resolved and the scene is written from their records, relations " +
        "(gate links, roads, faction ties), surroundings and approved descriptions, under a use-only-canon-names rule. " +
        "The result lists groundedOn, unresolved request words, unknownNames (names in the text the canon lacks; one " +
        "corrective rewrite runs first unless retryOnUnknown=false) and the call's cost. Pass node to borrow an existing " +
        "book's canon/continuity ('attached mode') without writing a Beat row to it. Writes nothing.")]
    public Task<string> GenerateScene(
        [Description("What should happen in this scene/line — the beat goal.")] string beatGoal,
        [Description("Comma-separated character names, aliases or entity ids on screen.")] string? characters = null,
        [Description("Where it happens: an entity name, alias or id of the universe (e.g. 'Earth Orbital'), or a loose hint ('the cathedral').")] string? location = null,
        [Description("What's happening beneath the surface — foreshadowing, unspoken motivation, dramatic irony.")] string? subtext = null,
        [Description("Optional slug/NodeCode/GUID of an existing Book or Chapter to borrow canon and continuity from. Omit for pure ephemeral.")] string? node = null,
        [Description("Optional universe slug override; defaults to the session's current universe.")] string? universe = null,
        [Description("Zero-based position in an imagined beat sequence, for pacing guidance. 0 if unknown.")] int beatIndex = 0,
        [Description("Total beats in that imagined sequence, for pacing guidance. 0 disables positional pacing.")] int totalBeats = 0,
        [Description("Comma-separated entity ids/names the scene is about (a game text's about), grounded like the characters.")] string? about = null,
        [Description("False: report invented names without the one corrective rewrite.")] bool retryOnUnknown = true) =>
        hub.InvokeAsync(nameof(OneShotGenerationTools), nameof(GenerateSceneImpl),
            new { beatGoal, characters, location, subtext, node, universe, beatIndex, totalBeats, about, retryOnUnknown });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GenerateSceneImpl(
        string beatGoal, string? characters, string? location, string? subtext,
        string? node, string? universe, int beatIndex, int totalBeats, string? about = null, bool retryOnUnknown = true)
    {
        try
        {
            var result = await generation.GenerateAsync(new OneShotGenerationService.OneShotGenerationRequest(
                BeatGoal: beatGoal,
                Characters: SplitList(characters),
                Location: location,
                Subtext: subtext,
                Node: node,
                Universe: universe,
                BeatIndex: beatIndex,
                TotalBeats: totalBeats,
                About: SplitList(about),
                RetryOnUnknown: retryOnUnknown));

            return JsonSerializer.Serialize(result.ToResponse(), JsonOpts);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return JsonSerializer.Serialize(new { error = "generate_scene_failed", detail = ex.Message }, JsonOpts);
        }
    }

    private static string[] SplitList(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? Array.Empty<string>()
            : csv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
