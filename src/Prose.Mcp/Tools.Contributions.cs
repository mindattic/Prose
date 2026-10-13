using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Beat contributions and the story's answers (author 2026-10-12, WO:01a13278) ──────────────
// Every pronoun in a beat carries an entity tag or the "not a character" mark, and every character
// the beat tags has one line saying what they do there and what it changes, bound to the beat's
// text hash. A character's history and the relations the story establishes are read back from
// those lines, never stored on their own, so they cannot disagree with the prose.
//
//   set_beat_contributions — replace a beat's lines (must name exactly the characters it tags)
//   get_beat_contributions — the lines and where they stand (missing / extra / stale / untagged
//                            pronouns / untagged ambiguous names — author 2026-10-13)
//   character_history      — a character's lines across the books, in reading order, cited
//   story_edges            — "who killed Bram Osei": typed relations, each with the beat that makes it

[McpServerToolType]
public class ContributionTools(BeatContributionService contributions, EntityMentionService mentions, HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    [McpServerTool, Description("Replace one beat's contribution lines: one per character the beat tags (by name or pronoun), saying what they do in the beat and what it changes, with optional typed edges (e.g. relation was_killed_by, target the killer's guid). Refused unless the lines name EXACTLY the beat's tagged characters. Each line is stamped with the beat's text hash and goes stale on any edit. CLI parity: prose --beat contributions --id <beat> --file <json>.")]
    public Task<string> set_beat_contributions(
        [Description("Beat Guid.")] string beatId,
        [Description("JSON object: {\"<character guid>\": \"line\"} or {\"<guid>\": {\"text\": \"line\", \"edges\": [{\"relation\": \"was_killed_by\", \"target\": \"<guid>\"}]}}.")] string linesJson) =>
        hub.InvokeAsync(nameof(ContributionTools), nameof(SetBeatContributionsImpl), new { beatId, linesJson });

    public async Task<string> SetBeatContributionsImpl(string beatId, string linesJson)
    {
        if (!Guid.TryParse(beatId, out var id)) return JsonSerializer.Serialize(new { error = "bad_beat_id", beatId }, JsonOpts);
        var lines = new Dictionary<Guid, ContributionInput>();
        using (var doc = JsonDocument.Parse(linesJson))
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (!Guid.TryParse(p.Name, out var entity)) return JsonSerializer.Serialize(new { error = "bad_entity_id", p.Name }, JsonOpts);
                lines[entity] = p.Value.ValueKind == JsonValueKind.String
                    ? new ContributionInput(p.Value.GetString() ?? "", [])
                    : new ContributionInput(p.Value.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "",
                        p.Value.TryGetProperty("edges", out var es)
                            ? es.EnumerateArray().Select(e => new ContributionEdge(e.GetProperty("relation").GetString() ?? "",
                                Guid.TryParse(e.GetProperty("target").GetString(), out var g) ? g : Guid.Empty)).ToList()
                            : []);
            }
        try { await contributions.SetAsync(id, lines); }
        catch (InvalidOperationException ex) { return JsonSerializer.Serialize(new { error = "refused", message = ex.Message }, JsonOpts); }
        return await GetBeatContributionsImpl(beatId);
    }

    [McpServerTool, Description("A beat's contribution lines and where they stand against its prose: characters tagged with no line, lines for characters it does not tag, stale lines, pronouns with no tag, and bare mentions of an ambiguous name sitting untagged (e.g. a 'Sable' the universe has five of, mentioned by name but never pinned with an explicit tag — tag one occurrence to resolve it). CLI parity: prose --beat contributions --id <beat>.")]
    public Task<string> get_beat_contributions([Description("Beat Guid.")] string beatId) =>
        hub.InvokeAsync(nameof(ContributionTools), nameof(GetBeatContributionsImpl), new { beatId });

    public async Task<string> GetBeatContributionsImpl(string beatId)
    {
        if (!Guid.TryParse(beatId, out var id)) return JsonSerializer.Serialize(new { error = "bad_beat_id", beatId }, JsonOpts);
        var check = await contributions.CheckAsync(id);
        var rows = await contributions.GetAsync(id);
        return JsonSerializer.Serialize(new
        {
            complete = check.Ok,
            lines = rows.Select(r => new { entity = r.EntityId, text = r.Text, edges = BeatContributionService.ParseEdges(r.EdgesJson), stale = check.Stale.Contains(r.EntityId) }),
            missing = check.Missing, extra = check.Extra,
            untagged_pronouns = check.UntaggedPronouns.Select(p => new { offset = p.Offset, word = p.Word }),
            untagged_names = check.UntaggedNames.Select(n => new { text = n.Text, candidates = n.Candidates }),
        }, JsonOpts);
    }

    [McpServerTool, Description("A character's history, read from the beats: every beat that tags them (by name or pronoun), in reading order, cited as book code, chapter and beat number, with the line saying what they did there. A beat with no line reads missing; a line written against other text reads stale. CLI parity: prose --character-history.")]
    public Task<string> character_history(
        [Description("Character id or slug.")] string entity,
        [Description("Optional book code to limit to (e.g. VATD).")] string book = "") =>
        hub.InvokeAsync(nameof(ContributionTools), nameof(CharacterHistoryImpl), new { entity, book });

    public async Task<string> CharacterHistoryImpl(string entity, string book = "")
    {
        if (await mentions.ResolveEntityAsync(entity) is not { } e) return JsonSerializer.Serialize(new { error = "entity_not_found", entity }, JsonOpts);
        var h = await contributions.HistoryAsync(e.Id, string.IsNullOrWhiteSpace(book) ? null : book);
        return JsonSerializer.Serialize(new { entity = e.Name, beats = h.Count, missing = h.Count(x => x.State == "missing"), stale = h.Count(x => x.State == "stale"), history = h }, JsonOpts);
    }

    [McpServerTool, Description("Relations the story establishes from or to an entity (e.g. who killed Bram Osei: relation was_killed_by), each with the beat that establishes it and its line. Answers a question about the story without reading the book. CLI parity: prose --story-edges.")]
    public Task<string> story_edges(
        [Description("Entity id or slug.")] string entity,
        [Description("Optional relation to filter by, e.g. was_killed_by.")] string relation = "") =>
        hub.InvokeAsync(nameof(ContributionTools), nameof(StoryEdgesImpl), new { entity, relation });

    public async Task<string> StoryEdgesImpl(string entity, string relation = "")
    {
        if (await mentions.ResolveEntityAsync(entity) is not { } e) return JsonSerializer.Serialize(new { error = "entity_not_found", entity }, JsonOpts);
        var edges = await contributions.EdgesAsync(e.Id, string.IsNullOrWhiteSpace(relation) ? null : relation);
        return JsonSerializer.Serialize(new { entity = e.Name, edges }, JsonOpts);
    }
}
