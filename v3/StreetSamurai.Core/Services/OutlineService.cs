using System.Text.Json;
using System.Text.Json.Serialization;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Plans multi-scene story arcs. Generates beat sheets with act structure so each
/// generation call knows its role in the larger narrative. The system can now write
/// strategically (toward a climax) not just tactically (next paragraph).
///
/// An outline is a living document — it can be generated upfront, modified as the
/// story evolves, and extended when the original arc completes.
/// </summary>
public class OutlineService
{
    private readonly ILlmService _llm;
    private readonly DatabaseService _db;
    private readonly IPathProvider _paths;

    public OutlineService(ILlmService llm, DatabaseService db, IPathProvider paths)
    {
        _llm = llm;
        _db = db;
        _paths = paths;
    }

    /// <summary>
    /// Generate a story outline from a premise, characters, and location.
    /// Returns a structured beat sheet with act structure.
    /// </summary>
    public async Task<StoryOutline> GenerateOutlineAsync(
        string premise, List<string> characters, string? location = null,
        int targetBeats = 12, CancellationToken ct = default)
    {
        var charContext = string.Join("\n\n", characters
            .Select(c => _db.GetCharacterContext(c))
            .Where(ctx => ctx.Length > 0));

        var locationContext = location != null ? _db.GetDistrictContext(location) : "";
        var literaryRules = _db.GetLiteraryRulesPrompt();

        var jsonExample = """
            {"title":"working title","logline":"one-sentence summary","theme":"thematic question",
            "acts":[{"act_number":1,"name":"act name","purpose":"what this act does",
            "beats":[{"beat_index":0,"title":"beat title","goal":"what happens","characters_present":["names"],
            "location":"where","emotional_arc":"reader feeling","stakes":"at risk",
            "seeds":["planted threads"],"payoffs":["resolved threads"],
            "facet_hint":"wound/ideal/id/shadow/mask/ghost","tension":5}]}],
            "character_arcs":[{"character":"name","start_state":"beginning","end_state":"end",
            "turning_point":"the moment","cost":"the price"}],
            "seeds_and_payoffs":[{"seed":"planted","planted_in_beat":0,"payoff":"resolved","payoff_in_beat":8}]}
            """;

        var system = $"""
            You are a story architect for cyberpunk literary fiction set in Meridian City.
            Design a complete story arc with act structure.

            CHARACTERS:
            {charContext}

            {(locationContext.Length > 0 ? $"LOCATION:\n{locationContext}" : "")}

            LITERARY RULES:
            {literaryRules}

            Generate a story outline as a JSON object matching this structure:
            {jsonExample}

            Design for {targetBeats} beats across 3 acts (setup/confrontation/resolution).
            Every seed must have a payoff. Every character must have an arc.
            Return ONLY the JSON.
            """;

        var response = await _llm.GenerateAsync(system,
            $"PREMISE: {premise}\nCHARACTERS: {string.Join(", ", characters)}\nTARGET BEATS: {targetBeats}",
            0.8, 4096, ct: ct);

        try
        {
            var json = response.Trim();
            if (json.StartsWith("```")) json = json[(json.IndexOf('\n') + 1)..];
            if (json.EndsWith("```")) json = json[..^3];

            var outline = JsonSerializer.Deserialize<StoryOutline>(json.Trim(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            outline.Premise = premise;
            outline.Characters = characters;
            return outline;
        }
        catch
        {
            return new StoryOutline { Premise = premise, Characters = characters, Title = "Outline generation failed" };
        }
    }

    /// <summary>Get the next unwritten beat from the outline.</summary>
    public OutlineBeat? GetNextBeat(StoryOutline outline, int currentBeatIndex)
    {
        return outline.Acts
            .SelectMany(a => a.Beats)
            .Where(b => b.BeatIndex > currentBeatIndex && !b.Written)
            .OrderBy(b => b.BeatIndex)
            .FirstOrDefault();
    }

    /// <summary>Mark a beat as written.</summary>
    public void MarkBeatWritten(StoryOutline outline, int beatIndex)
    {
        var beat = outline.Acts.SelectMany(a => a.Beats).FirstOrDefault(b => b.BeatIndex == beatIndex);
        if (beat != null) beat.Written = true;
    }

    /// <summary>Build a context block telling the LLM where this beat fits in the arc.</summary>
    public string BuildBeatContext(StoryOutline outline, int beatIndex)
    {
        var beat = outline.Acts.SelectMany(a => a.Beats).FirstOrDefault(b => b.BeatIndex == beatIndex);
        if (beat == null) return "";

        var act = outline.Acts.FirstOrDefault(a => a.Beats.Any(b => b.BeatIndex == beatIndex));
        var totalBeats = outline.Acts.SelectMany(a => a.Beats).Count();

        var lines = new List<string>
        {
            "STORY OUTLINE CONTEXT:",
            $"  Story: {outline.Title} — {outline.Logline}",
            $"  Theme: {outline.Theme}",
            $"  Current: Act {act?.ActNumber ?? 0} ({act?.Name ?? ""}) — Beat {beatIndex + 1} of {totalBeats}",
            $"  Beat Goal: {beat.Goal}",
            $"  Emotional Arc: {beat.EmotionalArc}",
            $"  Stakes: {beat.Stakes}",
            $"  Tension Target: {beat.Tension}/10",
        };

        if (beat.Seeds.Count > 0)
            lines.Add($"  PLANT these seeds: {string.Join("; ", beat.Seeds)}");
        if (beat.Payoffs.Count > 0)
            lines.Add($"  PAY OFF these threads: {string.Join("; ", beat.Payoffs)}");
        if (!string.IsNullOrEmpty(beat.FacetHint))
            lines.Add($"  Suggested facet lead: {beat.FacetHint}");

        // Show what's coming (so the LLM can foreshadow)
        var nextBeat = GetNextBeat(outline, beatIndex);
        if (nextBeat != null)
            lines.Add($"  NEXT BEAT will be: {nextBeat.Title} — set up for this.");

        return string.Join("\n", lines);
    }

    /// <summary>Save outline to disk alongside the story project.</summary>
    public void Save(string projectId, StoryOutline outline)
    {
        var path = Path.Combine(_paths.DataRoot, "story_blocks", $"{projectId}.outline.json");
        var dir = Path.GetDirectoryName(path);
        if (dir != null) Directory.CreateDirectory(dir);
        File.WriteAllText(path, JsonSerializer.Serialize(outline, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Load outline from disk.</summary>
    public StoryOutline? Load(string projectId)
    {
        var path = Path.Combine(_paths.DataRoot, "story_blocks", $"{projectId}.outline.json");
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<StoryOutline>(File.ReadAllText(path)); }
        catch { return null; }
    }
}

public class StoryOutline
{
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("logline")] public string Logline { get; set; } = "";
    [JsonPropertyName("theme")] public string Theme { get; set; } = "";
    [JsonPropertyName("premise")] public string Premise { get; set; } = "";
    [JsonPropertyName("characters")] public List<string> Characters { get; set; } = [];
    [JsonPropertyName("acts")] public List<StoryAct> Acts { get; set; } = [];
    [JsonPropertyName("character_arcs")] public List<CharacterArc> CharacterArcs { get; set; } = [];
    [JsonPropertyName("seeds_and_payoffs")] public List<SeedPayoff> SeedsAndPayoffs { get; set; } = [];
}

public class StoryAct
{
    [JsonPropertyName("act_number")] public int ActNumber { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("purpose")] public string Purpose { get; set; } = "";
    [JsonPropertyName("beats")] public List<OutlineBeat> Beats { get; set; } = [];
}

public class OutlineBeat
{
    [JsonPropertyName("beat_index")] public int BeatIndex { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("goal")] public string Goal { get; set; } = "";
    [JsonPropertyName("characters_present")] public List<string> CharactersPresent { get; set; } = [];
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("emotional_arc")] public string EmotionalArc { get; set; } = "";
    [JsonPropertyName("stakes")] public string Stakes { get; set; } = "";
    [JsonPropertyName("seeds")] public List<string> Seeds { get; set; } = [];
    [JsonPropertyName("payoffs")] public List<string> Payoffs { get; set; } = [];
    [JsonPropertyName("facet_hint")] public string FacetHint { get; set; } = "";
    [JsonPropertyName("tension")] public int Tension { get; set; }
    [JsonIgnore] public bool Written { get; set; }
}

public class CharacterArc
{
    [JsonPropertyName("character")] public string Character { get; set; } = "";
    [JsonPropertyName("start_state")] public string StartState { get; set; } = "";
    [JsonPropertyName("end_state")] public string EndState { get; set; } = "";
    [JsonPropertyName("turning_point")] public string TurningPoint { get; set; } = "";
    [JsonPropertyName("cost")] public string Cost { get; set; } = "";
}

public class SeedPayoff
{
    [JsonPropertyName("seed")] public string Seed { get; set; } = "";
    [JsonPropertyName("planted_in_beat")] public int PlantedInBeat { get; set; }
    [JsonPropertyName("payoff")] public string Payoff { get; set; } = "";
    [JsonPropertyName("payoff_in_beat")] public int PayoffInBeat { get; set; }
}
