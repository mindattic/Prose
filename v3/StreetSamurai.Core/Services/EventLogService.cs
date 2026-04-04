using System.Text.Json;
using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Structured event log for each story. Records what happened, who was involved,
/// where, and what the consequences were. Searchable and queryable — the system
/// can answer "when did X last happen?" without re-reading the entire story.
///
/// Events are extracted from generated text via LLM after each beat, then stored
/// as structured records alongside the story project.
/// </summary>
public class EventLogService
{
    private readonly ILlmService _llm;
    private readonly IPathProvider _paths;

    // In-memory logs per project, lazy-loaded from disk
    private readonly Dictionary<string, List<StoryEvent>> _logs = new();

    public EventLogService(ILlmService llm, IPathProvider paths)
    {
        _llm = llm;
        _paths = paths;
    }

    /// <summary>Get all events for a story project.</summary>
    public List<StoryEvent> GetEvents(string projectId)
    {
        if (!_logs.ContainsKey(projectId))
            _logs[projectId] = LoadFromDisk(projectId);
        return _logs[projectId];
    }

    /// <summary>
    /// Extract events from newly generated text and add them to the log.
    /// Uses the LLM to identify discrete narrative events.
    /// </summary>
    public async Task ExtractAndLogAsync(string projectId, string newText, int beatIndex, CancellationToken ct = default)
    {
        var system = """
            You are a narrative event extractor. Read the text and identify discrete EVENTS —
            things that happened, not descriptions or atmosphere. Return a JSON array of events.

            Each event:
            {
              "type": "action|dialogue|revelation|decision|conflict|arrival|departure|injury|death|discovery|emotional_shift",
              "summary": "one-sentence description of what happened",
              "participants": ["character names involved"],
              "location": "where it happened",
              "object": "item/weapon/tech involved if any (null if none)",
              "consequence": "what this event changes or sets up (null if standalone)",
              "emotional_weight": 1-10,
              "tags": ["relevant thematic tags: betrayal, trust, violence, tenderness, etc"]
            }

            Only extract EVENTS (things that happen). Not descriptions, not atmosphere, not backstory.
            If no events occur in the text, return [].
            Return ONLY the JSON array.
            """;

        try
        {
            var response = await _llm.GenerateAsync(system, newText, 0.1, 1024, ct: ct);
            var json = response.Trim();
            if (json.StartsWith("```")) json = json[(json.IndexOf('\n') + 1)..];
            if (json.EndsWith("```")) json = json[..^3];

            var rawEvents = JsonSerializer.Deserialize<List<RawEvent>>(json.Trim(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var events = GetEvents(projectId);
            foreach (var raw in rawEvents ?? [])
            {
                events.Add(new StoryEvent
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    BeatIndex = beatIndex,
                    Type = raw.Type ?? "action",
                    Summary = raw.Summary ?? "",
                    Participants = raw.Participants ?? [],
                    Location = raw.Location ?? "",
                    Object = raw.Object ?? "",
                    Consequence = raw.Consequence ?? "",
                    EmotionalWeight = raw.EmotionalWeight,
                    Tags = raw.Tags ?? [],
                    Timestamp = DateTime.UtcNow,
                });
            }

            SaveToDisk(projectId);
        }
        catch { /* Event extraction is best-effort */ }
    }

    /// <summary>Add an event manually (for user-created events or system events).</summary>
    public void AddEvent(string projectId, StoryEvent evt)
    {
        GetEvents(projectId).Add(evt);
        SaveToDisk(projectId);
    }

    /// <summary>Search events by participant name.</summary>
    public List<StoryEvent> GetEventsForCharacter(string projectId, string characterName) =>
        GetEvents(projectId).Where(e =>
            e.Participants.Any(p => p.Equals(characterName, StringComparison.OrdinalIgnoreCase))).ToList();

    /// <summary>Search events by type.</summary>
    public List<StoryEvent> GetEventsByType(string projectId, string type) =>
        GetEvents(projectId).Where(e => e.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>Search events by tag.</summary>
    public List<StoryEvent> GetEventsByTag(string projectId, string tag) =>
        GetEvents(projectId).Where(e =>
            e.Tags.Any(t => t.Contains(tag, StringComparison.OrdinalIgnoreCase))).ToList();

    /// <summary>Get the last event involving two specific characters.</summary>
    public StoryEvent? GetLastInteraction(string projectId, string charA, string charB) =>
        GetEvents(projectId)
            .Where(e => e.Participants.Any(p => p.Equals(charA, StringComparison.OrdinalIgnoreCase))
                     && e.Participants.Any(p => p.Equals(charB, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(e => e.BeatIndex)
            .FirstOrDefault();

    /// <summary>Get events at a specific location.</summary>
    public List<StoryEvent> GetEventsAtLocation(string projectId, string location) =>
        GetEvents(projectId).Where(e =>
            e.Location.Contains(location, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>Build a context summary of recent events for LLM injection.</summary>
    public string BuildRecentContext(string projectId, int maxEvents = 10)
    {
        var events = GetEvents(projectId);
        if (events.Count == 0) return "";

        var recent = events.OrderByDescending(e => e.BeatIndex).Take(maxEvents).Reverse().ToList();
        var lines = new List<string> { "RECENT EVENTS IN THIS STORY:" };
        foreach (var e in recent)
        {
            var who = e.Participants.Count > 0 ? $"[{string.Join(", ", e.Participants)}] " : "";
            var where = !string.IsNullOrEmpty(e.Location) ? $" at {e.Location}" : "";
            lines.Add($"  Beat {e.BeatIndex}: {who}{e.Summary}{where}");
        }
        return string.Join("\n", lines);
    }

    /// <summary>Build a character-specific event history for LLM injection.</summary>
    public string BuildCharacterHistory(string projectId, string characterName, int maxEvents = 5)
    {
        var events = GetEventsForCharacter(projectId, characterName);
        if (events.Count == 0) return "";

        var recent = events.OrderByDescending(e => e.BeatIndex).Take(maxEvents).Reverse().ToList();
        var lines = new List<string> { $"WHAT {characterName.ToUpperInvariant()} HAS DONE IN THIS STORY:" };
        foreach (var e in recent)
            lines.Add($"  - {e.Summary}");
        return string.Join("\n", lines);
    }

    /// <summary>Clear all events for a story.</summary>
    public void Clear(string projectId)
    {
        _logs[projectId] = [];
        SaveToDisk(projectId);
    }

    private List<StoryEvent> LoadFromDisk(string projectId)
    {
        var path = GetLogPath(projectId);
        if (!File.Exists(path)) return [];
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<StoryEvent>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        catch { return []; }
    }

    private void SaveToDisk(string projectId)
    {
        var path = GetLogPath(projectId);
        var dir = Path.GetDirectoryName(path);
        if (dir != null) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(_logs.GetValueOrDefault(projectId, []),
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private string GetLogPath(string projectId) =>
        Path.Combine(_paths.DataRoot, "story_blocks", $"{projectId}.events.json");
}

/// <summary>A discrete narrative event — something that HAPPENED in the story.</summary>
public class StoryEvent
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("beat_index")] public int BeatIndex { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; } = "action";
    [JsonPropertyName("summary")] public string Summary { get; set; } = "";
    [JsonPropertyName("participants")] public List<string> Participants { get; set; } = [];
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("object")] public string Object { get; set; } = "";
    [JsonPropertyName("consequence")] public string Consequence { get; set; } = "";
    [JsonPropertyName("emotional_weight")] public int EmotionalWeight { get; set; }
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
    [JsonPropertyName("timestamp")] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

internal record RawEvent
{
    public string? Type { get; init; }
    public string? Summary { get; init; }
    public List<string>? Participants { get; init; }
    public string? Location { get; init; }
    public string? Object { get; init; }
    public string? Consequence { get; init; }
    [JsonPropertyName("emotional_weight")] public int EmotionalWeight { get; init; }
    public List<string>? Tags { get; init; }
}
