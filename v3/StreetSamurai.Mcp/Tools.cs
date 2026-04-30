using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Tool surface for the StreetSamurai MCP server ────────────────────────────
// Every method here is a tool Claude can call to look up canon, search the
// world graph, or pull writing-context blocks. Read-mostly: the only mutation
// is plant_motif, which is normally user-confirmed in the Blazor UI but is
// useful from chat too. Tools return data — never prose. The caller (Claude)
// stays the writer; tools just give it sharper context.
// ────────────────────────────────────────────────────────────────────────────

[McpServerToolType]
public class CanonTools
{
    private readonly CharacterRepository characters;
    private readonly DistrictRepository places;
    private readonly FactionRepository factions;
    private readonly CorponationRepository corponations;
    private readonly LiteraryRulesRepository literaryRules;

    public CanonTools(
        CharacterRepository characters,
        DistrictRepository places,
        FactionRepository factions,
        CorponationRepository corponations,
        LiteraryRulesRepository literaryRules)
    {
        this.characters = characters;
        this.places = places;
        this.factions = factions;
        this.corponations = corponations;
        this.literaryRules = literaryRules;
    }

    [McpServerTool, Description("List every character in canon. Returns name + role + status for each. Cheap — call this first when you need to know who exists.")]
    public string ListCharacters()
    {
        characters.Reload();
        var list = characters.GetAll()
            .Select(c => new { name = c.Name, role = c.Role, status = c.Status, location = c.Location })
            .OrderBy(x => x.name)
            .ToList();
        return JsonSerializer.Serialize(list, JsonOpts);
    }

    [McpServerTool, Description("Load a character's full canon record by name: identity, psychology (core_fears, core_desires, coping_mechanisms, blind_spots, secret), behavioral (decision_rules, escalation_ladder, contradictions, habits, breaking_points, stress_responses), speech_patterns (vocabulary, cadence, verbal_tics, example_lines), augmentations, story_hooks. This is the primary source for voice when writing a POV chapter.")]
    public string GetCharacter([Description("Exact name of the character (e.g. 'Kyle Ellen Corbin-Vasik' or 'Sasha Võ').")] string name)
    {
        var c = characters.GetByName(name);
        if (c == null) return JsonSerializer.Serialize(new { error = "not_found", name }, JsonOpts);
        return JsonSerializer.Serialize(c, JsonOpts);
    }

    [McpServerTool, Description("List every place / district in canon. Use this to find a location for a scene.")]
    public string ListPlaces()
    {
        places.Reload();
        var list = places.GetAll()
            .Select(p => new { name = p.Name, type = p.Type, demographics = p.Demographics })
            .OrderBy(x => x.name)
            .ToList();
        return JsonSerializer.Serialize(list, JsonOpts);
    }

    [McpServerTool, Description("Load a place / district by name. Returns description, sensory_details, parent territory, geography.")]
    public string GetPlace([Description("Exact name of the place.")] string name)
    {
        var p = places.GetByName(name);
        if (p == null) return JsonSerializer.Serialize(new { error = "not_found", name }, JsonOpts);
        return JsonSerializer.Serialize(p, JsonOpts);
    }

    [McpServerTool, Description("List every faction in canon: street gangs, syndicates, cells, advocacy groups, etc.")]
    public string ListFactions()
    {
        factions.Reload();
        var list = factions.GetAll()
            .Select(f => new { name = f.Name, type = f.Type, territory = f.Territory })
            .OrderBy(x => x.name)
            .ToList();
        return JsonSerializer.Serialize(list, JsonOpts);
    }

    [McpServerTool, Description("Load a faction by name: leadership, structure, territory, motives, alliances, rivalries.")]
    public string GetFaction([Description("Exact faction name.")] string name)
    {
        var f = factions.GetByName(name);
        if (f == null) return JsonSerializer.Serialize(new { error = "not_found", name }, JsonOpts);
        return JsonSerializer.Serialize(f, JsonOpts);
    }

    [McpServerTool, Description("List every corponation (corporate sovereign entity).")]
    public string ListCorponations()
    {
        corponations.Reload();
        var list = corponations.GetAll()
            .Select(c => new { name = c.Name, sector = c.Sector, sovereign_territory = c.SovereignTerritory })
            .OrderBy(x => x.name)
            .ToList();
        return JsonSerializer.Serialize(list, JsonOpts);
    }

    [McpServerTool, Description("Load a corponation by name: sector, hierarchy, holdings, public-facing brand, dirty laundry.")]
    public string GetCorponation([Description("Exact corponation name.")] string name)
    {
        var c = corponations.GetByName(name);
        if (c == null) return JsonSerializer.Serialize(new { error = "not_found", name }, JsonOpts);
        return JsonSerializer.Serialize(c, JsonOpts);
    }

    [McpServerTool, Description("Load the world's literary rules: prohibitions, paragraph requirements, POV voice differentiation rules, register permissions, paragraph economy, interior_monologue source. Inject this near the top of any prose-generation prompt.")]
    public string GetLiteraryRules()
    {
        return JsonSerializer.Serialize(literaryRules.Get(), JsonOpts);
    }

    internal static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };
}

[McpServerToolType]
public class StoryTools
{
    private readonly StreetSamurai.Core.Interfaces.IBookRepository books;
    private readonly StreetSamurai.Core.Interfaces.IChapterRepository chapters;
    private readonly BookOutlineService outlines;

    public StoryTools(
        StreetSamurai.Core.Interfaces.IBookRepository books,
        StreetSamurai.Core.Interfaces.IChapterRepository chapters,
        BookOutlineService outlines)
    {
        this.books = books;
        this.chapters = chapters;
        this.outlines = outlines;
    }

    [McpServerTool, Description("List every book on the shelf. Returns id, title, premise, chapter count, status, protagonists.")]
    public string ListBooks()
    {
        var list = books.ListBooks()
            .Select(b => new { id = b.Id, title = b.Title, premise = b.Premise, chapter_count = b.ChapterIds.Count, status = b.Status, protagonists = b.Protagonists })
            .OrderBy(x => x.title)
            .ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a book by id: full metadata, chapter id list (canonical order), state_at_end (open threads, character status carry-forward, canon changes).")]
    public string GetBook([Description("Book id (32-char hex like 'eb91080d9c9c4f2b9b405fa5996bdea1').")] string id)
    {
        var b = books.LoadBook(id);
        if (b == null) return JsonSerializer.Serialize(new { error = "not_found", id }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(b, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a single chapter by id: synopsis, full HTML body, persisted beats list (each with structure_role + text), participating characters. Use this to read existing prose before extending or revising.")]
    public string GetChapter([Description("Chapter id (32-char hex).")] string id)
    {
        var c = chapters.LoadChapter(id);
        if (c == null) return JsonSerializer.Serialize(new { error = "not_found", id }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(c, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Load a book's shared outline (the plot spine). Returns premise/arc_target/theme/structure, per-chapter outlines (title, short_synopsis, long_synopsis, key_beats, opens_threads, closes_threads, state_changes, pov_character), book-level threads (planted_in / pays_off_in), pending_adjustments (LLM-proposed neighbor edits). Approval status gates prose generation in the UI.")]
    public string GetBookOutline([Description("Book id.")] string bookId)
    {
        return JsonSerializer.Serialize(outlines.Load(bookId), CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Build the 'WHERE WE ARE' director context block for writing a specific chapter: PRIOR chapters' content, THIS chapter's outline, UPCOMING chapters' setup needs, plus open book-level threads. This is the highest-value writing-context tool — call it before drafting prose for any chapter that's part of a book.")]
    public string GetDirectorContext(
        [Description("Book id.")] string bookId,
        [Description("Chapter id whose prose you're about to write.")] string chapterId)
    {
        return outlines.BuildDirectorContext(bookId, chapterId);
    }

    [McpServerTool, Description("Archive a book: moves the book file from engine/data/books/ to engine/data/archives/books/. Non-destructive — the original chapters stay in place but the book record is removed from the active shelf. Requires the caller to retype the full book id as a confirmation token (matches the UI's type-the-guid modal). Returns ok:true on success or error:'confirmation_mismatch' / error:'not_found' otherwise.")]
    public string ArchiveBook(
        [Description("Book id (32-char hex).")] string id,
        [Description("Confirmation token — must equal the same full book id. Mismatched or missing values abort the archive.")] string confirmId)
    {
        var book = books.LoadBook(id);
        if (book == null)
            return JsonSerializer.Serialize(new { error = "not_found", id }, CanonTools.JsonOpts);

        if (!string.Equals(confirmId, book.Id, StringComparison.Ordinal))
            return JsonSerializer.Serialize(new { error = "confirmation_mismatch", expected = book.Id }, CanonTools.JsonOpts);

        books.ArchiveBook(book.Id);
        return JsonSerializer.Serialize(new { ok = true, id = book.Id, title = book.Title, archived_to = "archives/books/" }, CanonTools.JsonOpts);
    }
}

[McpServerToolType]
public class ContextTools
{
    private readonly SemanticIndexService semanticIndex;
    private readonly WorldGraphService graph;
    private readonly MotifService motifs;

    public ContextTools(
        SemanticIndexService semanticIndex,
        WorldGraphService graph,
        MotifService motifs)
    {
        this.semanticIndex = semanticIndex;
        this.graph = graph;
        this.motifs = motifs;
    }

    [McpServerTool, Description("Search the world graph by theme, not by name. TF-IDF cosine similarity across every entity description. Use this to surface entities that are *thematically relevant* to what you're about to write — e.g. searching 'corporate betrayal under-table contract' might return Sable's backstory, the Lotus Syndicate, the Ferrogate enforcement arm. Returns ranked id+name+type+score.")]
    public string SearchSemantic(
        [Description("Free-text query — describe the theme/scene/concept.")] string query,
        [Description("Number of top hits to return. Default 8.")] int topK = 8)
    {
        graph.EnsureLoaded();
        var hits = semanticIndex.Search(query, topK);
        var enriched = hits.Select(h =>
        {
            var node = graph.GetNode(h.nodeId);
            return new { id = h.nodeId, name = node?.Name, nodeType = node?.NodeType, score = h.score };
        });
        return JsonSerializer.Serialize(enriched, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List the registered motifs for a book — recurring objects, phrases, gestures, sensory threads. Mention these in the chapter you're writing where natural; the review pipeline flags chapters that drop the whole inventory.")]
    public string GetMotifs([Description("Book id.")] string bookId)
    {
        return JsonSerializer.Serialize(motifs.Load(bookId), CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Plant a new motif in a book's inventory. Idempotent by name (re-planting with a longer description merges). The user normally accepts these from the Motifs panel in the UI; this tool exposes the same write so chat-side authoring can register them too.")]
    public string PlantMotif(
        [Description("Book id.")] string bookId,
        [Description("Motif name, e.g. 'brick-wall notebook' or 'the door is unlocked'.")] string name,
        [Description("Short description of what this motif means and where it lands.")] string description,
        [Description("MotifKind: Object, Phrase, Gesture, Sensory, Ritual.")] string kind,
        [Description("Chapter id where this motif is being introduced.")] string introducedInChapterId)
    {
        if (!Enum.TryParse<MotifKind>(kind, ignoreCase: true, out var kindEnum))
            return JsonSerializer.Serialize(new { error = "invalid_kind", kind, valid = Enum.GetNames(typeof(MotifKind)) }, CanonTools.JsonOpts);
        motifs.Plant(bookId, name, description, kindEnum, introducedInChapterId);
        return JsonSerializer.Serialize(new { ok = true, name, kind = kindEnum.ToString() }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Get a graph node's neighbors (relationships) up to N hops. Use this to walk from a known entity to entities related by canon — alliances, rivalries, family, mentor links, location ownership.")]
    public string GetNeighbors(
        [Description("Node id (use search_semantic or list_characters to find the id).")] string nodeId,
        [Description("Hops to traverse. 1 = direct neighbors. Default 1.")] int hops = 1)
    {
        graph.EnsureLoaded();
        var neighbors = graph.GetNeighbors(nodeId, hops);
        var list = neighbors.Select(n => new { id = n.Id, name = n.Name, nodeType = n.NodeType }).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }
}
