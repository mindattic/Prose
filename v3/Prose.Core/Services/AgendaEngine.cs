using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Character agenda engine. Each character has active goals, and scenes emerge
/// from goal conflicts. This is what makes the system truly autonomous — instead
/// of the user saying "write a scene where X happens," the engine identifies
/// where character goals collide and generates scene premises from the collision.
///
/// "Sable wants to protect her information network. Kyle wants to expose the
/// facility. These goals collide when the facility's location is in Sable's files."
/// </summary>
public class AgendaEngine
{
    private readonly ILlmService llm;
    private readonly DatabaseService db;
    private readonly WorldGraphService graph;
    private readonly CanonRetrievalService canonRetrieval;
    private readonly ILogger<AgendaEngine> log;

    public AgendaEngine(ILlmService llm, DatabaseService db, WorldGraphService graph,
        CanonRetrievalService canonRetrieval, ILogger<AgendaEngine> log)
    {
        this.llm = llm;
        this.db = db;
        this.graph = graph;
        this.canonRetrieval = canonRetrieval;
        this.log = log;
    }

    /// <summary>
    /// Generate agendas for characters based on their psychology, current story state,
    /// and recent events. Returns what each character WANTS to do next.
    /// </summary>
    public async Task<List<CharacterAgenda>> GenerateAgendasAsync(
        List<string> characterNames, StoryState? storyState = null,
        string? recentEvents = null, CancellationToken ct = default)
    {
        var charContexts = characterNames
            .Select(n => db.GetCharacterContext(n))
            .Where(c => c.Length > 0)
            .ToList();

        var stateBlock = "";
        if (storyState != null)
        {
            var parts = new List<string>();
            foreach (var (name, cs) in storyState.Characters)
            {
                var line = $"{name}: at {cs.Location}, feeling {cs.EmotionalState}";
                if (cs.Injuries.Count > 0) line += $", injured ({string.Join(", ", cs.Injuries)})";
                if (cs.Knowledge.Count > 0) line += $", knows: {string.Join(", ", cs.Knowledge.TakeLast(3))}";
                parts.Add(line);
            }
            stateBlock = string.Join("\n", parts);
        }

        // Totality: pull relevant canon across ALL entity types (the orgs, gear,
        // drugs, places, factions the cast is entangled with) so goals form from
        // the whole world, not just the character dossiers.
        var canonBlock = await canonRetrieval.RetrieveContextBlockAsync(
            string.Join("\n", characterNames) + "\n" + (recentEvents ?? ""),
            k: 12, excludeNames: characterNames, ct: ct);

        var system = $"""
            You are a character motivation engine for near-future fiction. Given character profiles
            and the current story state, determine what each character WANTS to do next.

            Characters are not plot devices — they have their own goals that may conflict with
            each other. The most interesting scenes emerge when two characters want incompatible things.

            CHARACTERS:
            {string.Join("\n\n---\n\n", charContexts)}

            {(stateBlock.Length > 0 ? $"CURRENT STATE:\n{stateBlock}" : "")}
            {(recentEvents?.Length > 0 ? $"RECENT EVENTS:\n{recentEvents}" : "")}
            {(canonBlock.Length > 0 ? $"\n{canonBlock}" : "")}

            For each character, return a JSON array of objects with fields:
            character, primary_goal, secondary_goal, obstacle, desperation (1-10),
            next_action, needs_from_others (array), willing_to_sacrifice, ticking_clock (null if none).

            Base goals on each character's core_desires, drives, and current situation.
            Not on what would make a good story — on what the CHARACTER would actually want.
            Return ONLY the JSON array.
            """;

        try
        {
            var response = await llm.GenerateAsync(system, "Generate character agendas now.", 0.6, 2048, ct: ct);
            var json = response.Trim();
            json = JsonDefaults.StripCodeFences(json);

            var agendas = JsonSerializer.Deserialize<List<CharacterAgenda>>(json.Trim(),
                JsonDefaults.LlmParsing) ?? [];
            log.LogInformation("Generated {Count} character agendas", agendas.Count);
            return agendas;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            log.LogError(ex, "Agenda generation failed for characters=[{Characters}]", string.Join(", ", characterNames));
            return [];
        }
    }

    /// <summary>
    /// Identify where character agendas collide and generate scene premises.
    /// This is the autonomous story generation entry point — no user prompt needed.
    /// </summary>
    public async Task<List<ConflictPremise>> FindConflictsAsync(
        List<CharacterAgenda> agendas, CancellationToken ct = default)
    {
        if (agendas.Count < 2) return [];

        var agendaText = string.Join("\n\n", agendas.Select(a =>
            $"{a.Character}: wants '{a.PrimaryGoal}', obstacle: '{a.Obstacle}', " +
            $"desperation: {a.Desperation}/10, next action: '{a.NextAction}'"));

        var system = """
            You are a conflict architect. Given character agendas, identify where goals COLLIDE
            and generate scene premises from the collisions.

            A good conflict is not just "they disagree" — it's "they both want something legitimate,
            and getting it requires the other to lose." The best conflicts make the reader
            sympathize with both sides.

            Return a JSON array of conflict-based scene premises:
            [
              {
                "title": "scene title",
                "characters_involved": ["names"],
                "conflict_type": "goal_collision|resource_competition|information_asymmetry|loyalty_test|moral_dilemma",
                "premise": "2-3 sentence scene setup",
                "why_now": "why this conflict erupts at this moment",
                "possible_outcomes": ["2-3 ways this could resolve, each with different costs"],
                "tension": 1-10,
                "thematic_resonance": "what larger theme this conflict explores"
              }
            ]

            Generate 2-4 conflicts, ranked by dramatic potential.
            Return ONLY the JSON array.
            """;

        // Ground the conflict invention in the relevant canon totality too.
        var conflictCanon = await canonRetrieval.RetrieveContextBlockAsync(agendaText, k: 10, ct: ct);
        var userMsg = conflictCanon.Length > 0 ? $"{conflictCanon}\n\n{agendaText}" : agendaText;

        try
        {
            var response = await llm.GenerateAsync(system, userMsg, 0.7, 2048, ct: ct);
            var json = response.Trim();
            json = JsonDefaults.StripCodeFences(json);

            var conflicts = JsonSerializer.Deserialize<List<ConflictPremise>>(json.Trim(),
                JsonDefaults.LlmParsing) ?? [];
            log.LogInformation("Found {Count} conflicts from agendas", conflicts.Count);
            return conflicts;
        }
        catch (JsonException)
        {
            log.LogDebug("Conflict discovery skipped — LLM returned truncated JSON");
            return [];
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Conflict discovery failed");
            return [];
        }
    }

    /// <summary>
    /// Full autonomous pipeline: generate agendas → find conflicts → return scene premises.
    /// This is "write me a story" with no user input beyond character selection.
    /// </summary>
    public async Task<List<ConflictPremise>> GenerateScenePremisesAsync(
        List<string> characterNames, StoryState? storyState = null,
        string? recentEvents = null, CancellationToken ct = default)
    {
        var agendas = await GenerateAgendasAsync(characterNames, storyState, recentEvents, ct);
        if (agendas.Count < 2) return [];
        return await FindConflictsAsync(agendas, ct);
    }
}

public class CharacterAgenda
{
    [JsonPropertyName("character")] public string Character { get; set; } = "";
    [JsonPropertyName("primary_goal")] public string PrimaryGoal { get; set; } = "";
    [JsonPropertyName("secondary_goal")] public string SecondaryGoal { get; set; } = "";
    [JsonPropertyName("obstacle")] public string Obstacle { get; set; } = "";
    [JsonPropertyName("desperation")] public int Desperation { get; set; }
    [JsonPropertyName("next_action")] public string NextAction { get; set; } = "";
    [JsonPropertyName("needs_from_others")] public List<string> NeedsFromOthers { get; set; } = [];
    [JsonPropertyName("willing_to_sacrifice")] public string WillingToSacrifice { get; set; } = "";
    [JsonPropertyName("ticking_clock")] public string? TickingClock { get; set; }
}

public class ConflictPremise
{
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("characters_involved")] public List<string> CharactersInvolved { get; set; } = [];
    [JsonPropertyName("conflict_type")] public string ConflictType { get; set; } = "";
    [JsonPropertyName("premise")] public string Premise { get; set; } = "";
    [JsonPropertyName("why_now")] public string WhyNow { get; set; } = "";
    [JsonPropertyName("possible_outcomes")] public List<string> PossibleOutcomes { get; set; } = [];
    [JsonPropertyName("tension")] public int Tension { get; set; }
    [JsonPropertyName("thematic_resonance")] public string ThematicResonance { get; set; } = "";
}
