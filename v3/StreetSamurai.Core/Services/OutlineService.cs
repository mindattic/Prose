using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Plans multi-scene story arcs. Generates beat sheets with act structure so each
/// generation call knows its role in the larger narrative.
///
/// Storage: <c>Chapters.OutlineJson</c> on SQL Server (since 2026-05-08;
/// the legacy <c>engine/data/chapters/&lt;projectId&gt;/outline.json</c>
/// file was retired in the same migration that drained the rest of
/// <c>engine/data</c>). On first read for a project that still has a legacy
/// file, the service migrates it into the DB column and deletes the file.
/// </summary>
public class OutlineService
{
    private readonly ILlmService llm;
    private readonly DatabaseService db;
    private readonly IPathProvider paths;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILogger<OutlineService> log;

    public OutlineService(
        ILlmService llm,
        DatabaseService db,
        IPathProvider paths,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILogger<OutlineService> log)
    {
        this.llm = llm;
        this.db = db;
        this.paths = paths;
        this.dbFactory = dbFactory;
        this.log = log;
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
            .Select(c => db.GetCharacterContext(c))
            .Where(ctx => ctx.Length > 0));

        var locationContext = location != null ? db.GetDistrictContext(location) : "";
        var literaryRules = db.GetLiteraryRulesPrompt();

        var methodology = new StoryMethodologyService();
        var methodologyPrompt = methodology.GetOutlineMethodologyPrompt(targetBeats);

        var jsonExample = """
            {"title":"working title","logline":"one-sentence summary","theme":"thematic argument (not just a topic — a claim that the story tests)",
            "acts":[{"act_number":1,"name":"act name","purpose":"what this act does",
            "beats":[{"beat_index":0,"title":"beat title","goal":"what happens","characters_present":["names"],
            "location":"where","emotional_arc":"reader feeling","stakes":"at risk",
            "seeds":["planted threads"],"payoffs":["resolved threads"],
            "facet_hint":"wound/ideal/id/shadow/mask/ghost","tension":5,
            "structure_role":"Catalyst","scene_type":"scene"}]}],
            "character_arcs":[{"character":"name","want":"external conscious goal","need":"internal unconscious truth",
            "start_state":"beginning","end_state":"end","turning_point":"the moment","cost":"the price"}],
            "seeds_and_payoffs":[{"seed":"planted","planted_in_beat":0,"payoff":"resolved","payoff_in_beat":8}]}
            """;

        var system = $"""
            You are a story architect for neo-noir literary fiction set in Meridian City.
            Design a complete story arc with act structure.

            CHARACTERS:
            {charContext}

            {(locationContext.Length > 0 ? $"LOCATION:\n{locationContext}" : "")}

            LITERARY RULES:
            {literaryRules}

            {methodologyPrompt}

            Generate a story outline as a JSON object matching this structure:
            {jsonExample}

            Design for {targetBeats} beats across 3 acts (setup/confrontation/resolution).
            Every seed must have a payoff. Every character must have an arc with both Want and Need.
            Assign structure_role and scene_type to every beat.
            Return ONLY the JSON.
            """;

        log.LogInformation("Generating outline: premise={PremiseLen}chars, characters=[{Characters}], location={Location}, targetBeats={TargetBeats}",
            premise.Length, string.Join(", ", characters), location ?? "none", targetBeats);

        string response;
        try
        {
            response = await llm.GenerateAsync(system,
                $"PREMISE: {premise}\nCHARACTERS: {string.Join(", ", characters)}\nTARGET BEATS: {targetBeats}",
                0.8, 16384, ct: ct);

            log.LogDebug("Outline LLM response received: {ResponseLen} chars", response.Length);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Outline generation LLM call failed for characters=[{Characters}]",
                string.Join(", ", characters));
            return new StoryOutline { Premise = premise, Characters = characters, Title = "Outline generation failed" };
        }

        var json = response.Trim();
        if (json.StartsWith("```")) json = json[(json.IndexOf('\n') + 1)..];
        if (json.EndsWith("```")) json = json[..^3];
        json = json.Trim();

        // First attempt: parse as-is
        try
        {
            var outline = JsonSerializer.Deserialize<StoryOutline>(json,
                JsonDefaults.LlmParsing) ?? new();

            outline.Premise = premise;
            outline.Characters = characters;

            log.LogInformation("Outline generated: title={Title}, acts={ActCount}, beats={BeatCount}",
                outline.Title, outline.Acts.Count, outline.Acts.SelectMany(a => a.Beats).Count());

            return outline;
        }
        catch (JsonException firstEx)
        {
            log.LogWarning("Outline JSON parse failed on first attempt, trying truncation repair: {Error}", firstEx.Message);

            // Second attempt: repair truncated JSON by closing open structures
            try
            {
                var repaired = RepairTruncatedJson(json);
                var outline = JsonSerializer.Deserialize<StoryOutline>(repaired,
                    JsonDefaults.LlmParsing) ?? new();

                outline.Premise = premise;
                outline.Characters = characters;

                var beatCount = outline.Acts.SelectMany(a => a.Beats).Count();
                log.LogInformation("Outline RESCUED from truncated JSON: title={Title}, acts={ActCount}, beats={BeatCount} (some beats may be incomplete)",
                    outline.Title, outline.Acts.Count, beatCount);

                return outline;
            }
            catch (Exception repairEx)
            {
                log.LogError(repairEx, "Outline JSON repair also failed. Raw response (first 500 chars): {ResponsePreview}",
                    response.Length > 500 ? response[..500] : response);
                return new StoryOutline { Premise = premise, Characters = characters, Title = "Outline generation failed" };
            }
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

    /// <summary>
    /// Attempt to repair truncated JSON by closing all open structures.
    /// When the LLM runs out of tokens mid-response, the JSON is valid up to
    /// the truncation point. We close open strings, arrays, and objects to
    /// salvage whatever was successfully generated.
    /// </summary>
    private static string RepairTruncatedJson(string json)
    {
        // Walk forward tracking structure, detect what state we ended in,
        // then trim back to the last safe boundary before closing braces.
        var trimmed = json.TrimEnd();

        var inString = false;
        var escaped = false;
        var stack = new Stack<char>();
        var lastColon = -1;        // byte index of the last ':' seen at current depth
        var lastComma = -1;        // byte index of the last ',' seen at current depth
        var stringStart = -1;      // byte index of the last opening '"'

        for (int i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            if (escaped) { escaped = false; continue; }
            if (c == '\\' && inString) { escaped = true; continue; }
            if (c == '"') { if (!inString) stringStart = i; inString = !inString; continue; }
            if (inString) continue;

            if (c == '{') { stack.Push('}'); lastColon = -1; lastComma = -1; }
            else if (c == '[') { stack.Push(']'); lastColon = -1; lastComma = -1; }
            else if (c == '}' || c == ']') { if (stack.Count > 0 && stack.Peek() == c) stack.Pop(); }
            else if (c == ':') lastColon = i;
            else if (c == ',') lastComma = i;
        }

        // Case 1: ended inside a string. Decide if it's a value or an orphan key.
        if (inString)
        {
            // A value-string always comes after a ':'. A key-string comes after '{' or ','.
            // If the last ':' we saw is BEFORE the string start, this is an orphan key — trim it.
            if (lastColon < stringStart)
            {
                // Trim back to the last comma or opening brace at this depth
                var cut = Math.Max(lastComma, stringStart - 1);
                // Walk back past whitespace to find the comma or opening brace
                while (cut > 0 && (trimmed[cut] == ',' || char.IsWhiteSpace(trimmed[cut]))) cut--;
                trimmed = trimmed[..(cut + 1)];
            }
            else
            {
                // It's a value — just close the string
                trimmed += "\"";
            }
        }
        else if (lastComma > lastColon && lastColon >= 0)
        {
            // Case 2: ended after a ':' but before a value, or after a key with no value.
            // If there's a dangling "key": with nothing after, trim back to the previous comma.
            var tail = trimmed[(lastColon + 1)..].TrimEnd();
            if (tail.Length == 0 || tail == "," || !HasValueChar(tail))
            {
                // Walk back to before the orphan key
                var cut = FindKeyStart(trimmed, lastColon);
                if (cut > 0) trimmed = trimmed[..cut].TrimEnd(',', ' ', '\t', '\r', '\n');
            }
        }

        // Close all open structures
        while (stack.Count > 0)
            trimmed += stack.Pop();

        return trimmed;
    }

    private static bool HasValueChar(string s)
    {
        foreach (var c in s) if (!char.IsWhiteSpace(c) && c != ',') return true;
        return false;
    }

    private static int FindKeyStart(string json, int colonIndex)
    {
        // Walk backwards from the colon: skip whitespace, then skip the key string,
        // then return the index right after the preceding comma or opening brace.
        int i = colonIndex - 1;
        while (i >= 0 && char.IsWhiteSpace(json[i])) i--;
        if (i < 0 || json[i] != '"') return -1;
        i--; // inside the key
        while (i >= 0 && json[i] != '"') i--;
        i--; // before the opening quote
        while (i >= 0 && char.IsWhiteSpace(json[i])) i--;
        if (i < 0) return -1;
        if (json[i] == ',' || json[i] == '{' || json[i] == '[') return i;
        return -1;
    }

    /// <summary>
    /// Idempotent column-add. Called from <c>--repair</c>'s schema-bootstrap
    /// phase so subsequent EF queries don't trip on a missing column.
    /// </summary>
    public async Task EnsureOutlineJsonColumnAsync(CancellationToken ct = default)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync(ct);
        const string ddl = """
            IF COL_LENGTH('dbo.Chapters', 'OutlineJson') IS NULL
                ALTER TABLE [dbo].[Chapters] ADD [OutlineJson] NVARCHAR(MAX) NULL;
            """;
        await ctx.Database.ExecuteSqlRawAsync(ddl, ct);
    }

    /// <summary>Save outline to <c>Chapters.OutlineJson</c>.</summary>
    public void Save(string projectId, StoryOutline outline)
    {
        if (!Guid.TryParse(projectId, out var chapterId)
            && !Guid.TryParseExact(projectId, "N", out chapterId))
        {
            log.LogWarning("Outline: project id is not a Guid, skipping save: {ProjectId}", projectId);
            return;
        }

        try
        {
            using var ctx = dbFactory.CreateDbContext();
            var row = ctx.Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (row == null)
            {
                log.LogDebug("Outline: no Chapters row for {ProjectId}; outline NOT persisted (chapter must be saved first)", projectId);
                return;
            }
            row.OutlineJson = JsonSerializer.Serialize(outline, JsonDefaults.Indented);
            row.ModifiedAt = DateTime.UtcNow;
            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Outline: save failed for {ProjectId}", projectId);
        }
    }

    /// <summary>
    /// Load outline from <c>Chapters.OutlineJson</c>. Migrates a legacy
    /// <c>outline.json</c> file (and deletes it) on first read miss.
    /// </summary>
    public StoryOutline? Load(string projectId)
    {
        if (!Guid.TryParse(projectId, out var chapterId)
            && !Guid.TryParseExact(projectId, "N", out chapterId))
        {
            return TryReadLegacyDiskFile(projectId);
        }

        try
        {
            using var ctx = dbFactory.CreateDbContext();
            var row = ctx.Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (row == null) return TryReadLegacyDiskFile(projectId);

            // Migration on first miss: empty DB column + legacy file present
            // → import file then delete it.
            if (string.IsNullOrEmpty(row.OutlineJson))
            {
                var fromDisk = TryReadLegacyDiskFile(projectId, deleteAfterRead: true);
                if (fromDisk != null)
                {
                    row.OutlineJson = JsonSerializer.Serialize(fromDisk, JsonDefaults.Indented);
                    row.ModifiedAt = DateTime.UtcNow;
                    ctx.SaveChanges();
                    return fromDisk;
                }
                return null;
            }
            return JsonSerializer.Deserialize<StoryOutline>(row.OutlineJson);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Outline: load failed for {ProjectId}", projectId);
            return null;
        }
    }

    private StoryOutline? TryReadLegacyDiskFile(string projectId, bool deleteAfterRead = false)
    {
        var path = StoryFolderHelper.FindFile(paths.ChaptersDir, projectId, "outline.json");
        if (path == null || !File.Exists(path)) return null;
        try
        {
            var outline = JsonSerializer.Deserialize<StoryOutline>(File.ReadAllText(path));
            if (deleteAfterRead && outline != null)
            {
                try { File.Delete(path); }
                catch (Exception ex) { log.LogDebug(ex, "Outline: legacy file delete failed for {Path}", path); }
            }
            return outline;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Outline: legacy disk read failed for {Path}", path);
            return null;
        }
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
    [JsonPropertyName("structure_role")] public string StructureRole { get; set; } = "";
    [JsonPropertyName("scene_type")] public string SceneType { get; set; } = "scene";
    [JsonIgnore] public bool Written { get; set; }
}

public class CharacterArc
{
    [JsonPropertyName("character")] public string Character { get; set; } = "";
    [JsonPropertyName("want")] public string Want { get; set; } = "";
    [JsonPropertyName("need")] public string Need { get; set; } = "";
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
