using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Structured event log for each chapter. Records what happened, who was involved,
/// where, and what the consequences were. Searchable and queryable — the system
/// can answer "when did X last happen?" without re-reading the entire story.
///
/// Storage: <c>Chapters.EventsJson</c> on the SQL Server Prose database
/// (since 2026-05-08; the legacy
/// <c>engine/data/chapters/&lt;projectId&gt;/events.json</c> path was retired
/// in the same migration that drained the rest of <c>engine/data</c>).
/// On first read for a project that still has a legacy file, the service
/// migrates it into the DB column and deletes the disk copy.
///
/// The public API is unchanged from the disk-backed version so callers don't
/// need to know about the storage swap.
/// </summary>
public class EventLogService
{
    private readonly ILlmService llm;
    private readonly IPathProvider paths;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<EventLogService> log;

    // In-memory logs per project, lazy-loaded from DB (migrating from disk on first miss).
    private readonly Dictionary<string, List<StoryEvent>> logs = new();

    public EventLogService(
        ILlmService llm,
        IPathProvider paths,
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<EventLogService> log)
    {
        this.llm = llm;
        this.paths = paths;
        this.dbFactory = dbFactory;
        this.log = log;
    }

    /// <summary>Get all events for a chapter project.</summary>
    public List<StoryEvent> GetEvents(string projectId)
    {
        if (!logs.ContainsKey(projectId))
            logs[projectId] = LoadFromDb(projectId);
        return logs[projectId];
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
            var response = await llm.GenerateAsync(system, newText, 0.1, 1024, ct: ct);
            var json = response.Trim();
            json = JsonDefaults.StripCodeFences(json);

            var rawEvents = JsonSerializer.Deserialize<List<RawEvent>>(json.Trim(),
                JsonDefaults.LlmParsing);

            var events = GetEvents(projectId);
            foreach (var raw in rawEvents ?? [])
            {
                events.Add(new StoryEvent
                {
                    Id = Guid.CreateVersion7().ToString("N")[..8],
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

            SaveToDb(projectId);
        }
        catch (Exception ex) { log.LogWarning(ex, "Event extraction failed for project={ProjectId}, beat={BeatIndex}", projectId, beatIndex); }
    }

    /// <summary>Add an event manually (for user-created events or system events).</summary>
    public void AddEvent(string projectId, StoryEvent evt)
    {
        GetEvents(projectId).Add(evt);
        SaveToDb(projectId);
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
        logs[projectId] = [];
        SaveToDb(projectId);
    }

    // ── storage ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Idempotent column-add. Mirrors the EnsureXxxSchemaAsync pattern in
    /// <c>AmmunitionLinkerService</c>. Safe to call on every repair run.
    /// </summary>
    public async Task EnsureEventsJsonColumnAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        const string ddl = """
            IF COL_LENGTH('dbo.Chapters', 'EventsJson') IS NULL
                ALTER TABLE [dbo].[Chapters] ADD [EventsJson] NVARCHAR(MAX) NULL;
            """;
        await db.Database.ExecuteSqlRawAsync(ddl, ct);
    }

    /// <summary>
    /// Sync read path. Resolves the project's chapter row, returns the
    /// deserialized event list. If the DB column is null/empty but a legacy
    /// <c>events.json</c> still exists on disk, migrates the file content into
    /// the DB and deletes the source file in the same call.
    /// </summary>
    private List<StoryEvent> LoadFromDb(string projectId)
    {
        if (!Guid.TryParse(projectId, out var chapterId)
            && !Guid.TryParseExact(projectId, "N", out chapterId))
        {
            log.LogWarning("EventLog: project id is not a Guid, returning empty: {ProjectId}", projectId);
            return [];
        }

        try
        {
            using var db = dbFactory.CreateDbContext();
            var row = db.Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (row == null)
            {
                // Chapter row doesn't exist in DB. Fall back to disk so we
                // don't lose the events while the chapter row catches up.
                return TryReadLegacyDiskFile(projectId);
            }

            // Migration on first miss: empty DB column + legacy file present
            // → import file then delete it.
            if (string.IsNullOrEmpty(row.EventsJson))
            {
                var fromDisk = TryReadLegacyDiskFile(projectId, deleteAfterRead: true);
                if (fromDisk.Count > 0)
                {
                    row.EventsJson = JsonSerializer.Serialize(fromDisk, JsonDefaults.Indented);
                    row.ModifiedAt = DateTime.UtcNow;
                    db.SaveChanges();
                    return fromDisk;
                }
                return [];
            }

            return JsonSerializer.Deserialize<List<StoryEvent>>(row.EventsJson,
                JsonDefaults.LlmParsing) ?? [];
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "EventLog: load failed for {ProjectId}", projectId);
            return [];
        }
    }

    /// <summary>
    /// Sync write path. Updates <c>Chapters.EventsJson</c> for the project's
    /// chapter row. Creates no row — if the chapter doesn't exist yet, the
    /// caller is mid-bootstrap and will save the chapter row separately
    /// before any callers depend on these events being durable.
    /// </summary>
    private void SaveToDb(string projectId)
    {
        if (!Guid.TryParse(projectId, out var chapterId)
            && !Guid.TryParseExact(projectId, "N", out chapterId))
        {
            log.LogWarning("EventLog: project id is not a Guid, skipping save: {ProjectId}", projectId);
            return;
        }

        try
        {
            using var db = dbFactory.CreateDbContext();
            var row = db.Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (row == null)
            {
                log.LogDebug("EventLog: no Chapters row for {ProjectId}; events held in-memory only until chapter exists", projectId);
                return;
            }
            var events = logs.GetValueOrDefault(projectId, []);
            row.EventsJson = events.Count == 0 ? null : JsonSerializer.Serialize(events, JsonDefaults.Indented);
            row.ModifiedAt = DateTime.UtcNow;
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "EventLog: save failed for {ProjectId}", projectId);
        }
    }

    /// <summary>
    /// One-shot legacy reader. Looks for the old
    /// <c>engine/data/chapters/&lt;projectId&gt;/events.json</c> file. If
    /// <paramref name="deleteAfterRead"/> is true, removes the file once the
    /// content is in hand — used by the migrate-on-first-miss path.
    /// </summary>
    private List<StoryEvent> TryReadLegacyDiskFile(string projectId, bool deleteAfterRead = false)
    {
        var path = StoryFolderHelper.FindFile(paths.ChaptersDir, projectId, "events.json");
        if (path == null || !File.Exists(path)) return [];
        try
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<StoryEvent>>(json, JsonDefaults.LlmParsing) ?? [];
            if (deleteAfterRead && list.Count >= 0)
            {
                try { File.Delete(path); }
                catch (Exception ex) { log.LogDebug(ex, "EventLog: legacy file delete failed for {Path}", path); }
            }
            return list;
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "EventLog: legacy disk read failed for {ProjectId}", projectId);
            return [];
        }
    }
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
