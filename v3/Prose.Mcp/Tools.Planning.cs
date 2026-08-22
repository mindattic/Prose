using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Planning tools — Tier 2 ────────────────────────────────────────────────
// Beat-level scene planning helpers. Each one is a thin wrapper around an
// existing Core service and returns structured data Claude can fold into a
// drafting decision. None of them generate prose.

/// <summary>
/// Tier-2 planning helpers — beat-level scene planning. Each tool is a thin
/// wrapper around an existing Core service that returns structured data Claude
/// can fold into a drafting decision. None of these tools generate prose.
/// </summary>
[McpServerToolType]
public class PlanningTools
{
    private readonly EntityExtractionService extraction;
    private readonly BehaviorPredictionService prediction;
    private readonly UniverseGraphService graph;
    private readonly ConsequenceEngine consequences;
    private readonly HubInvoker hub;

    public PlanningTools(
        EntityExtractionService extraction,
        BehaviorPredictionService prediction,
        UniverseGraphService graph,
        ConsequenceEngine consequences,
        HubInvoker hub)
    {
        this.extraction = extraction;
        this.prediction = prediction;
        this.graph = graph;
        this.consequences = consequences;
        this.hub = hub;
    }

    /// <summary>Extract named entities and relationships from arbitrary prose. Useful after drafting a chapter to surface new characters, places, factions, weapons, and technology mentioned that aren't in canon yet (candidates for promotion). Calls the LLM internally — slow on long text, fast on a single beat.</summary>
    [McpServerTool, Description("Extract named entities and relationships from arbitrary prose. Useful AFTER drafting a chapter — surfaces any new characters, places, factions, weapons, technology mentioned that aren't in canon yet (candidates for promotion). Returns the structured ExtractionResult with entities (with type + description + properties) and relationships (source → target). Calls the LLM internally — slow on long text, fast on a single beat.")]
    public Task<string> ExtractEntities(
        [Description("Prose text to scan.")] string text) =>
        hub.InvokeAsync(nameof(PlanningTools), nameof(ExtractEntitiesImpl), new { text });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ExtractEntitiesImpl(
        string text)
    {
        var result = await extraction.ExtractAsync(text);
        return JsonSerializer.Serialize(result, CanonTools.JsonOpts);
    }

    /// <summary>Predict a character's likely behavior in a given scene. Pulls from canon psychology, behavioral profile, and archetype influences. Returns dominant state, likely actions, dialogue mode, concealing, physical behavior, relationship dynamics, stress response, near-breaking-point flag. No LLM call — pure structural inference. Use this before drafting a scene.</summary>
    [McpServerTool, Description("Predict a character's likely behavior in a given scene. Pulls from the character's psychology (core_fears, core_desires, coping_mechanisms, blind_spots), behavioral (decision_rules, escalation_ladder, contradictions, habits, breaking_points, stress_responses), and archetype influences. Returns dominant_state, likely_actions, dialogue_mode, concealing, physical_behavior, relationship_dynamics, stress_response, near_breaking_point. No LLM call — pure structural inference. Use this BEFORE drafting a scene to know how a character will read.")]
    public Task<string> PredictBehavior(
        [Description("Character name — exact match against canon.")] string characterName,
        [Description("Scene location.")] string sceneLocation,
        [Description("Other characters present in the scene (comma-separated names).")] string othersPresent,
        [Description("What this beat is trying to accomplish narratively.")] string beatGoal,
        [Description("Tension level 1-10. Use 1-3 for low/calm, 4-6 for charged, 7-9 for crisis, 10 for breaking point.")] int tensionLevel = 5) =>
        hub.InvokeAsync(nameof(PlanningTools), nameof(PredictBehaviorImpl), new { characterName, sceneLocation, othersPresent, beatGoal, tensionLevel });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string PredictBehaviorImpl(
        string characterName,
        string sceneLocation,
        string othersPresent,
        string beatGoal,
        int tensionLevel = 5)
    {
        var others = string.IsNullOrWhiteSpace(othersPresent)
            ? new List<string>()
            : othersPresent.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        var p = prediction.PredictBehavior(
            characterName: characterName,
            projectId: "",  // chat-side has no project context; degrades gracefully
            sceneLocation: sceneLocation,
            otherCharactersPresent: others,
            beatGoal: beatGoal,
            tensionLevel: tensionLevel);

        return JsonSerializer.Serialize(p, CanonTools.JsonOpts);
    }

    /// <summary>List a node's edges filtered by relation type. Subset of get_neighbors that returns only relationships matching a specific relation type — 'rival', 'allied', 'mentor', 'family', 'controls_territory', 'frequents', etc.</summary>
    [McpServerTool, Description("List a node's edges filtered by relation type. Subset of get_neighbors that returns only relationships matching a specific relation_type — e.g. 'rival', 'allied', 'mentor', 'family', 'controls_territory', 'frequents'. Useful for targeted lookups: 'who are Kyle's known rivals'.")]
    public Task<string> GetNeighborsByRelation(
        [Description("Source node id (use search_semantic / list_characters / etc. to find).")] string nodeId,
        [Description("Relation type to filter on. Case-insensitive substring match (e.g. 'rival' matches 'rivalry').")] string relationType) =>
        hub.InvokeAsync(nameof(PlanningTools), nameof(GetNeighborsByRelationImpl), new { nodeId, relationType });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string GetNeighborsByRelationImpl(
        string nodeId,
        string relationType)
    {
        graph.EnsureLoaded();
        var edges = graph.GetAllEdges(nodeId)
            .Where(e => e.RelationType.Contains(relationType, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var hits = edges.Select(e =>
        {
            var otherId = e.Source == nodeId ? e.Target : e.Source;
            var direction = e.Source == nodeId ? "outgoing" : "incoming";
            var other = graph.GetNode(otherId);
            return new
            {
                relation_type = e.RelationType,
                direction,
                other_id = otherId,
                other_name = other?.Name,
                other_type = other?.NodeType,
                description = e.Description,
            };
        }).ToList();
        return JsonSerializer.Serialize(hits, CanonTools.JsonOpts);
    }

    /// <summary>Get the most recent world consequences (cross-story state changes — assassinations, faction shifts, public scandals, infrastructure damage). Use when extending a chapter sequence to honour what's already happened in the world.</summary>
    [McpServerTool, Description("Get the most recent world consequences (cross-story state changes — assassinations, faction shifts, public scandals, infrastructure damage). Use this when extending a chapter sequence to honour what's already happened in the world.")]
    public Task<string> GetRecentConsequences(
        [Description("Maximum number of recent entries to return. Default 10.")] int count = 10) =>
        hub.InvokeAsync(nameof(PlanningTools), nameof(GetRecentConsequencesImpl), new { count });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string GetRecentConsequencesImpl(
        int count = 10)
    {
        var list = consequences.GetRecent(count);
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    /// <summary>Get every world consequence affecting a specific entity (character, faction, place), ordered by recorded_at descending.</summary>
    [McpServerTool, Description("Get every world consequence affecting a specific entity (character, faction, place). Returns the consequences ordered by recorded_at descending.")]
    public Task<string> GetConsequencesFor(
        [Description("Entity name (character, faction, place, etc.).")] string entityName) =>
        hub.InvokeAsync(nameof(PlanningTools), nameof(GetConsequencesForImpl), new { entityName });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string GetConsequencesForImpl(
        string entityName)
    {
        var list = consequences.GetConsequencesFor(entityName);
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    /// <summary>Build the LLM-ready "consequences in play" context block for a protagonist. Combines protagonist-specific consequences with the 5 most recent world events, dedupes, caps at 10, flags unresolved threads. Plug directly into a chapter prompt's situational context.</summary>
    [McpServerTool, Description("Build the LLM-ready 'consequences in play' context block for a protagonist. Combines protagonist-specific consequences with the 5 most recent world events, dedupes, caps at 10 entries, flags unresolved threads. Plug this directly into a chapter prompt's situational context.")]
    public Task<string> GetConsequenceContext(
        [Description("Protagonist name. Optional — pass empty for unfocused 'world events' context.")] string protagonistName = "") =>
        hub.InvokeAsync(nameof(PlanningTools), nameof(GetConsequenceContextImpl), new { protagonistName });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string GetConsequenceContextImpl(
        string protagonistName = "")
    {
        var ctx = consequences.BuildConsequenceContext(string.IsNullOrWhiteSpace(protagonistName) ? null : protagonistName);
        return ctx;  // already a formatted text block
    }
}
