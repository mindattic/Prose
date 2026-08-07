using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Tracks real-time narrative state within a story. After each generation beat,
/// the LLM extracts what changed (who moved, who got hurt, what was revealed)
/// and this service updates the state. The next generation call receives this
/// state as hard constraints — preventing continuity errors at generation time
/// rather than catching them after.
///
/// This is the STORY model, separate from the WORLD model (WorldGraphService).
/// The world model says "Kyle carries a katana." The story state says
/// "Kyle set the katana down on Mrs. Chen's counter in paragraph 3."
///
/// ── WHY (CLOSED-LOOP FEEDBACK) ──
/// Without this service, each beat would be generated in isolation. The LLM would
/// not know that a character was injured two beats ago, or that a weapon was dropped.
/// This creates a closed feedback loop: generate text -> extract state changes ->
/// inject updated state into next generation call -> repeat. The result is that
/// dead characters stay dead, dropped items stay dropped, and the story maintains
/// internal consistency across beats without manual tracking.
///
/// ── HOW IT CONNECTS ──
/// CALLS: ILlmService (to extract state changes from generated prose via structured
///        JSON extraction — a secondary LLM call after each beat generation).
/// CALLED BY: StoryDirectorService (after each beat in the generation loop),
///            StoryStarterService (for constraint injection into prompts).
/// PROVIDES: BuildConstraints() output is injected into LLM system prompts as
///           hard facts about the current narrative state.
///
/// ── WHEN IT RUNS ──
/// Per-beat during story generation. InitializeCharacter() at story start (Phase 5).
/// UpdateFromTextAsync() after each beat is written. BuildConstraints() before each
/// beat generation. State is ephemeral — keyed by projectId, discarded when done.
///
/// ── STATE TRACKED ──
/// Per-character: location, emotional state, injuries, inventory, knowledge, alive/dead.
/// Per-story: current scene location, time of day, tension level, active conflicts,
///            what the reader has learned (for dramatic irony / reveal tracking).
/// </summary>
public class StoryStateService
{
    private readonly ILlmService llm;
    private readonly ILogger<StoryStateService> log;

    // Per-story state, keyed by story project ID
    private readonly Dictionary<string, StoryState> _states = new();

    public StoryStateService(ILlmService llm, ILogger<StoryStateService> log)
    {
        this.llm = llm;
        this.log = log;
    }

    /// <summary>Get or create state for a story project.</summary>
    public StoryState GetState(string projectId)
    {
        if (!_states.TryGetValue(projectId, out var state))
        {
            state = new StoryState { ProjectId = projectId };
            _states[projectId] = state;
        }
        return state;
    }

    /// <summary>
    /// After a beat of text is generated, extract what changed and update state.
    /// Uses the LLM to parse the narrative for state changes.
    /// </summary>
    public async Task UpdateFromTextAsync(string projectId, string newText, string fullStorySoFar, CancellationToken ct = default)
    {
        var state = GetState(projectId);

        var system = """
            You are a narrative state extractor. Read the NEW TEXT (the latest addition to a story)
            and extract what changed. Return a JSON object with:

            {
              "character_updates": [
                {
                  "name": "character name",
                  "location": "where they are now (null if unchanged)",
                  "emotional_state": "current emotion (null if unchanged)",
                  "injuries": "new injuries (null if none)",
                  "inventory_gained": ["items picked up"],
                  "inventory_lost": ["items dropped/given away"],
                  "learned": ["facts this character now knows"],
                  "status": "alive/dead/unconscious/missing (null if unchanged)"
                }
              ],
              "scene_location": "where this scene takes place",
              "time_of_day": "morning/afternoon/evening/night (null if unclear)",
              "tension_level": 1-10,
              "active_conflicts": ["brief description of unresolved tensions"],
              "reader_learned": ["new facts revealed to the reader in this text"]
            }

            Return ONLY the JSON object. If nothing changed for a field, use null.
            Only include characters who appear or are mentioned in the new text.
            """;

        try
        {
            var priorContext = string.IsNullOrWhiteSpace(fullStorySoFar) ? ""
                : $"PRIOR STORY CONTEXT (for reference — extract changes from NEW TEXT only):\n{(fullStorySoFar.Length > 4000 ? fullStorySoFar[^4000..] : fullStorySoFar)}\n\n";
            var response = await llm.GenerateAsync(system, $"{priorContext}NEW TEXT:\n{newText}", 0.1, 1024, ct: ct);
            var json = response.Trim();
            json = JsonDefaults.StripCodeFences(json);

            var update = JsonSerializer.Deserialize<StateUpdate>(json.Trim(),
                JsonDefaults.LlmParsing);

            if (update != null)
                ApplyUpdate(state, update);
        }
        catch (Exception ex) { log.LogWarning(ex, "State extraction failed for project={ProjectId}", projectId); }
    }

    /// <summary>
    /// Build a constraints block for injection into the LLM system prompt.
    /// This tells the LLM what is TRUE RIGHT NOW in the story.
    /// Includes hard prohibitions: dead characters cannot act, absent characters
    /// cannot appear in the current scene. These are the "guardrails" that
    /// prevent the most common continuity errors in multi-beat generation.
    /// </summary>
    public string BuildConstraints(string projectId)
    {
        var state = GetState(projectId);
        if (state.Characters.Count == 0 && state.ActiveConflicts.Count == 0)
            return "";

        var lines = new List<string> { "CURRENT NARRATIVE STATE (these are TRUE right now in the story):" };

        if (!string.IsNullOrEmpty(state.CurrentLocation))
            lines.Add($"SCENE LOCATION: {state.CurrentLocation}");
        if (!string.IsNullOrEmpty(state.TimeOfDay))
            lines.Add($"TIME: {state.TimeOfDay}");
        if (state.TensionLevel > 0)
            lines.Add($"TENSION: {state.TensionLevel}/10");

        foreach (var (name, cs) in state.Characters)
        {
            var parts = new List<string> { name };
            if (!string.IsNullOrEmpty(cs.Location)) parts.Add($"at {cs.Location}");
            if (!string.IsNullOrEmpty(cs.EmotionalState)) parts.Add($"feeling {cs.EmotionalState}");
            if (!string.IsNullOrEmpty(cs.Status) && cs.Status != "alive") parts.Add($"STATUS: {cs.Status}");
            if (cs.Injuries.Count > 0) parts.Add($"injured: {string.Join(", ", cs.Injuries)}");
            if (cs.Inventory.Count > 0) parts.Add($"carrying: {string.Join(", ", cs.Inventory)}");
            lines.Add($"  [{string.Join(" | ", parts)}]");
        }

        if (state.ActiveConflicts.Count > 0)
        {
            lines.Add("ACTIVE CONFLICTS:");
            foreach (var c in state.ActiveConflicts)
                lines.Add($"  - {c}");
        }

        // Hard constraints derived from state
        var dead = state.Characters.Where(kv => "dead".Equals(kv.Value.Status, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Key).ToList();
        if (dead.Count > 0)
            lines.Add($"DO NOT write these characters as alive or acting: {string.Join(", ", dead)}");

        var absent = state.Characters
            .Where(kv => !string.IsNullOrEmpty(kv.Value.Location) && kv.Value.Location != state.CurrentLocation)
            .Select(kv => kv.Key).ToList();
        if (absent.Count > 0 && !string.IsNullOrEmpty(state.CurrentLocation))
            lines.Add($"NOT PRESENT in this scene (elsewhere): {string.Join(", ", absent)}");

        return string.Join("\n", lines);
    }

    /// <summary>Reset state for a story (new story or restart).</summary>
    public void Reset(string projectId) => _states.Remove(projectId);

    /// <summary>Initialize character state from the world model.</summary>
    public void InitializeCharacter(string projectId, string name, string? location = null, List<string>? inventory = null)
    {
        var state = GetState(projectId);
        if (!state.Characters.ContainsKey(name))
            state.Characters[name] = new CharacterState();

        var cs = state.Characters[name];
        if (location != null) cs.Location = location;
        if (inventory != null) cs.Inventory = new List<string>(inventory);
    }

    /// <summary>
    /// Apply extracted state changes to the story state. Merges rather than replaces —
    /// null fields in the update are ignored, inventory is additive/subtractive,
    /// injuries accumulate. This allows partial extractions to still be useful.
    /// </summary>
    private void ApplyUpdate(StoryState state, StateUpdate update)
    {
        if (!string.IsNullOrEmpty(update.SceneLocation))
            state.CurrentLocation = update.SceneLocation;
        if (!string.IsNullOrEmpty(update.TimeOfDay))
            state.TimeOfDay = update.TimeOfDay;
        if (update.TensionLevel > 0)
            state.TensionLevel = update.TensionLevel;
        if (update.ActiveConflicts?.Count > 0)
            state.ActiveConflicts = update.ActiveConflicts;
        if (update.ReaderLearned?.Count > 0)
            state.ReaderKnowledge.AddRange(update.ReaderLearned);

        state.BeatCount++;

        foreach (var cu in update.CharacterUpdates ?? [])
        {
            if (string.IsNullOrWhiteSpace(cu.Name)) continue;
            if (!state.Characters.ContainsKey(cu.Name))
                state.Characters[cu.Name] = new CharacterState();

            var cs = state.Characters[cu.Name];
            if (!string.IsNullOrEmpty(cu.Location)) cs.Location = cu.Location;
            if (!string.IsNullOrEmpty(cu.EmotionalState)) cs.EmotionalState = cu.EmotionalState;
            if (!string.IsNullOrEmpty(cu.Status)) cs.Status = cu.Status;
            if (!string.IsNullOrEmpty(cu.Injuries)) cs.Injuries.Add(cu.Injuries);
            if (cu.InventoryGained?.Count > 0) cs.Inventory.AddRange(cu.InventoryGained);
            if (cu.InventoryLost?.Count > 0) foreach (var item in cu.InventoryLost) cs.Inventory.Remove(item);
            if (cu.Learned?.Count > 0) cs.Knowledge.AddRange(cu.Learned);
        }
    }
}

/// <summary>The complete state of a story at a point in time.</summary>
public class StoryState
{
    public string ProjectId { get; set; } = "";
    public int BeatCount { get; set; }
    public string CurrentLocation { get; set; } = "";
    public string TimeOfDay { get; set; } = "";
    public int TensionLevel { get; set; }
    public Dictionary<string, CharacterState> Characters { get; set; } = new();
    public List<string> ActiveConflicts { get; set; } = [];
    public List<string> ReaderKnowledge { get; set; } = [];
}

/// <summary>A single character's state within the story.</summary>
public class CharacterState
{
    public string Location { get; set; } = "";
    public string EmotionalState { get; set; } = "";
    public string Status { get; set; } = "alive";
    public List<string> Injuries { get; set; } = [];
    public List<string> Inventory { get; set; } = [];
    public List<string> Knowledge { get; set; } = [];
}

// Internal deserialization types
internal record StateUpdate
{
    [JsonPropertyName("character_updates")] public List<CharacterUpdate>? CharacterUpdates { get; init; }
    [JsonPropertyName("scene_location")] public string? SceneLocation { get; init; }
    [JsonPropertyName("time_of_day")] public string? TimeOfDay { get; init; }
    [JsonPropertyName("tension_level")] public int TensionLevel { get; init; }
    [JsonPropertyName("active_conflicts")] public List<string>? ActiveConflicts { get; init; }
    [JsonPropertyName("reader_learned")] public List<string>? ReaderLearned { get; init; }
}

internal record CharacterUpdate
{
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("location")] public string? Location { get; init; }
    [JsonPropertyName("emotional_state")] public string? EmotionalState { get; init; }
    [JsonPropertyName("injuries")] public string? Injuries { get; init; }
    [JsonPropertyName("inventory_gained")] public List<string>? InventoryGained { get; init; }
    [JsonPropertyName("inventory_lost")] public List<string>? InventoryLost { get; init; }
    [JsonPropertyName("learned")] public List<string>? Learned { get; init; }
    [JsonPropertyName("status")] public string? Status { get; init; }
}
