using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Tracks information asymmetry: what each character knows, what the reader knows,
/// and when things were learned. Prevents POV leaks (narrator revealing information
/// the POV character doesn't have) and enables dramatic irony (reader knows something
/// the character doesn't).
///
/// "The reader knows Sable has the facility files. Kyle doesn't know this.
/// When writing Kyle's POV, do not reveal this information. When writing Sable's
/// behavior, show subtle signs of concealment."
/// </summary>
public class KnowledgeMapService
{
    private readonly IPathProvider paths;
    private readonly ILogger<KnowledgeMapService> log;

    // Per-story knowledge maps
    private readonly Dictionary<string, KnowledgeMap> _maps = new();

    public KnowledgeMapService(IPathProvider paths, ILogger<KnowledgeMapService> log)
    {
        this.paths = paths;
        this.log = log;
    }

    /// <summary>Get or create the knowledge map for a story.</summary>
    public KnowledgeMap GetMap(string projectId)
    {
        if (!_maps.TryGetValue(projectId, out var map))
        {
            map = LoadFromDisk(projectId) ?? new KnowledgeMap { ProjectId = projectId };
            _maps[projectId] = map;
        }
        return map;
    }

    /// <summary>Record that a character learned a fact.</summary>
    public void CharacterLearned(string projectId, string characterName, string fact, int beatIndex, string source = "")
    {
        var map = GetMap(projectId);
        if (!map.CharacterKnowledge.ContainsKey(characterName))
            map.CharacterKnowledge[characterName] = [];

        // Don't duplicate
        if (map.CharacterKnowledge[characterName].Any(k => k.Fact == fact)) return;

        map.CharacterKnowledge[characterName].Add(new KnowledgeFact
        {
            Fact = fact,
            LearnedAtBeat = beatIndex,
            Source = source,
        });
        SaveToDisk(projectId);
    }

    /// <summary>Record that the reader learned a fact (may or may not be known to characters).</summary>
    public void ReaderLearned(string projectId, string fact, int beatIndex, string? revealedBy = null)
    {
        var map = GetMap(projectId);
        if (map.ReaderKnowledge.Any(k => k.Fact == fact)) return;

        map.ReaderKnowledge.Add(new KnowledgeFact
        {
            Fact = fact,
            LearnedAtBeat = beatIndex,
            Source = revealedBy ?? "narration",
        });
        SaveToDisk(projectId);
    }

    /// <summary>Record a secret — something that exists but hasn't been revealed to anyone yet.</summary>
    public void AddSecret(string projectId, string fact, string knownBy)
    {
        var map = GetMap(projectId);
        map.Secrets.Add(new SecretFact
        {
            Fact = fact,
            KnownBy = knownBy,
            Revealed = false,
        });
        SaveToDisk(projectId);
    }

    /// <summary>Mark a secret as revealed (to a specific character or to the reader).</summary>
    public void RevealSecret(string projectId, string fact, string revealedTo, int beatIndex)
    {
        var map = GetMap(projectId);
        var secret = map.Secrets.FirstOrDefault(s => s.Fact == fact);
        if (secret != null) secret.Revealed = true;

        if (revealedTo == "reader")
            ReaderLearned(projectId, fact, beatIndex, "secret_revealed");
        else
            CharacterLearned(projectId, revealedTo, fact, beatIndex, "secret_revealed");
    }

    /// <summary>Check if a character knows a specific fact.</summary>
    public bool CharacterKnows(string projectId, string characterName, string fact)
    {
        var map = GetMap(projectId);
        return map.CharacterKnowledge.TryGetValue(characterName, out var knowledge)
            && knowledge.Any(k => k.Fact.Contains(fact, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Check if the reader knows something a specific character doesn't — dramatic irony.</summary>
    public List<string> GetDramaticIrony(string projectId, string characterName)
    {
        var map = GetMap(projectId);
        var charKnows = map.CharacterKnowledge.GetValueOrDefault(characterName, [])
            .Select(k => k.Fact).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return map.ReaderKnowledge
            .Where(k => !charKnows.Contains(k.Fact))
            .Select(k => k.Fact)
            .ToList();
    }

    /// <summary>Get unrevealed secrets.</summary>
    public List<SecretFact> GetUnrevealedSecrets(string projectId) =>
        GetMap(projectId).Secrets.Where(s => !s.Revealed).ToList();

    /// <summary>
    /// Build a constraints block for the LLM when writing from a specific character's POV.
    /// Tells the LLM what this character knows and doesn't know.
    /// </summary>
    public string BuildPovConstraints(string projectId, string povCharacter)
    {
        var map = GetMap(projectId);
        var lines = new List<string>();

        // What the POV character knows
        if (map.CharacterKnowledge.TryGetValue(povCharacter, out var knowledge) && knowledge.Count > 0)
        {
            lines.Add($"{povCharacter.ToUpperInvariant()} KNOWS:");
            foreach (var k in knowledge.TakeLast(10))
                lines.Add($"  - {k.Fact}");
        }

        // What the POV character does NOT know (but reader does) — dramatic irony
        var irony = GetDramaticIrony(projectId, povCharacter);
        if (irony.Count > 0)
        {
            lines.Add($"DRAMATIC IRONY — {povCharacter.ToUpperInvariant()} DOES NOT KNOW (but reader does):");
            foreach (var fact in irony.Take(5))
                lines.Add($"  - {fact}");
            lines.Add($"  DO NOT have {povCharacter} act on or reference this information.");
        }

        // Unrevealed secrets held by other characters
        var secrets = map.Secrets.Where(s => !s.Revealed && s.KnownBy != povCharacter).ToList();
        if (secrets.Count > 0)
        {
            lines.Add("HIDDEN INFORMATION (characters concealing something):");
            foreach (var s in secrets.Take(5))
                lines.Add($"  - {s.KnownBy} is hiding: {s.Fact}");
            lines.Add("  Show subtle behavioral cues of concealment, but DO NOT reveal the secret.");
        }

        return lines.Count > 0 ? string.Join("\n", lines) : "";
    }

    /// <summary>
    /// Update knowledge map from story state and event log.
    /// Call after StoryStateService and EventLogService have processed a beat.
    /// </summary>
    public void SyncFromState(string projectId, StoryState state, List<StoryEvent> recentEvents, int beatIndex)
    {
        // Characters present in the scene learn what happened
        var presentChars = state.Characters
            .Where(kv => kv.Value.Location == state.CurrentLocation)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var evt in recentEvents.Where(e => e.BeatIndex == beatIndex))
        {
            // Participants learn the event
            foreach (var p in evt.Participants)
                CharacterLearned(projectId, p, evt.Summary, beatIndex, "participated");

            // Bystanders learn it too (they were present)
            foreach (var bystander in presentChars.Except(evt.Participants))
                CharacterLearned(projectId, bystander, evt.Summary, beatIndex, "witnessed");

            // Reader always learns
            ReaderLearned(projectId, evt.Summary, beatIndex);
        }
    }

    /// <summary>Clear knowledge map for a story.</summary>
    public void Clear(string projectId)
    {
        _maps[projectId] = new KnowledgeMap { ProjectId = projectId };
        SaveToDisk(projectId);
    }

    private KnowledgeMap? LoadFromDisk(string projectId)
    {
        var path = GetPath(projectId);
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<KnowledgeMap>(File.ReadAllText(path)); }
        catch (Exception ex) { log.LogWarning(ex, "Failed to load knowledge map from {Path}", path); return null; }
    }

    private void SaveToDisk(string projectId)
    {
        var path = GetPath(projectId);
        var dir = Path.GetDirectoryName(path);
        if (dir != null) Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonSerializer.Serialize(GetMap(projectId),
            JsonDefaults.Indented));
    }

    private string GetPath(string projectId) =>
        StoryFolderHelper.GetFilePath(paths.ChaptersDir, projectId, "knowledge.json");
}

public class KnowledgeMap
{
    [JsonPropertyName("project_id")] public string ProjectId { get; set; } = "";
    [JsonPropertyName("character_knowledge")] public Dictionary<string, List<KnowledgeFact>> CharacterKnowledge { get; set; } = new();
    [JsonPropertyName("reader_knowledge")] public List<KnowledgeFact> ReaderKnowledge { get; set; } = [];
    [JsonPropertyName("secrets")] public List<SecretFact> Secrets { get; set; } = [];
}

public class KnowledgeFact
{
    [JsonPropertyName("fact")] public string Fact { get; set; } = "";
    [JsonPropertyName("learned_at_beat")] public int LearnedAtBeat { get; set; }
    [JsonPropertyName("source")] public string Source { get; set; } = "";
}

public class SecretFact
{
    [JsonPropertyName("fact")] public string Fact { get; set; } = "";
    [JsonPropertyName("known_by")] public string KnownBy { get; set; } = "";
    [JsonPropertyName("revealed")] public bool Revealed { get; set; }
}
