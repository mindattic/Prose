using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Models;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Tool surface for the Prose MCP server ────────────────────────────
// Every method here is a tool Claude can call to look up canon, search the
// world graph, or pull writing-context blocks. Read-mostly: the only mutation
// is plant_motif, which is normally user-confirmed in the Blazor UI but is
// useful from chat too. Tools return data — never prose. The caller (Claude)
// stays the writer; tools just give it sharper context.
// ────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Tool group exposing the headline canon repositories — characters, places,
/// factions, CorpoNations, plus the literary rules block. Use these as the
/// first-pass lookup when an MCP client needs canonical identity, voice, or
/// place-of-action data for a scene.
/// </summary>
[McpServerToolType]
public class CanonTools
{
    private readonly CharacterRepository characters;
    private readonly DistrictRepository places;
    private readonly FactionRepository factions;
    private readonly CorponationRepository corponations;
    private readonly LiteraryRulesRepository literaryRules;
    private readonly HubInvoker hub;

    public CanonTools(
        CharacterRepository characters,
        DistrictRepository places,
        FactionRepository factions,
        CorponationRepository corponations,
        LiteraryRulesRepository literaryRules,
        HubInvoker hub)
    {
        this.characters = characters;
        this.places = places;
        this.factions = factions;
        this.corponations = corponations;
        this.literaryRules = literaryRules;
        this.hub = hub;
    }

    /// <summary>
    /// List every character in canon. Returns name + role + status for each. Cheap — call this first when you need to know who exists.
    /// </summary>
    [McpServerTool, Description("List every character in canon. Returns name + role + status for each. Cheap — call this first when you need to know who exists.")]
    public Task<string> ListCharacters() => hub.InvokeAsync(nameof(CanonTools), nameof(ListCharactersImpl));

    public string ListCharactersImpl()
    {
        characters.Reload();
        var list = characters.GetAll()
            .Select(c => new { name = c.Name, role = c.Role, status = c.Status, location = c.Location })
            .OrderBy(x => x.name)
            .ToList();
        return JsonSerializer.Serialize(list, JsonOpts);
    }

    /// <summary>
    /// Load a character's full canon record by name: identity, psychology, behavioral profile, speech patterns, augmentations, story hooks. Primary source for voice when writing a POV chapter.
    /// </summary>
    [McpServerTool, Description("Load a character's full canon record by name: identity, psychology (core_fears, core_desires, coping_mechanisms, blind_spots, secret), behavioral (decision_rules, escalation_ladder, contradictions, habits, breaking_points, stress_responses), speech_patterns (vocabulary, cadence, verbal_tics, example_lines), augmentations, story_hooks. This is the primary source for voice when writing a POV chapter.")]
    public Task<string> GetCharacter([Description("Exact name of the character (e.g. 'Kyle Ellen Corbin' or 'Sasha Võ').")] string name) =>
        hub.InvokeAsync(nameof(CanonTools), nameof(GetCharacterImpl), new { name });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string GetCharacterImpl(string name)
    {
        var c = characters.GetByName(name);
        if (c == null) return JsonSerializer.Serialize(new { error = "not_found", name }, JsonOpts);
        return JsonSerializer.Serialize(c, JsonOpts);
    }

    /// <summary>List every place / district in canon. Use this to find a location for a scene.</summary>
    [McpServerTool, Description("List every place / district in canon. Use this to find a location for a scene.")]
    public Task<string> ListPlaces() => hub.InvokeAsync(nameof(CanonTools), nameof(ListPlacesImpl));

    public string ListPlacesImpl()
    {
        places.Reload();
        var list = places.GetAll()
            .Select(p => new { name = p.Name, type = p.Type, demographics = p.Demographics })
            .OrderBy(x => x.name)
            .ToList();
        return JsonSerializer.Serialize(list, JsonOpts);
    }

    /// <summary>Load a place / district by name. Returns description, sensory details, parent territory, geography.</summary>
    [McpServerTool, Description("Load a place / district by name. Returns description, sensory_details, parent territory, geography.")]
    public Task<string> GetPlace([Description("Exact name of the place.")] string name) =>
        hub.InvokeAsync(nameof(CanonTools), nameof(GetPlaceImpl), new { name });

    public string GetPlaceImpl(string name)
    {
        var p = places.GetByName(name);
        if (p == null) return JsonSerializer.Serialize(new { error = "not_found", name }, JsonOpts);
        return JsonSerializer.Serialize(p, JsonOpts);
    }

    /// <summary>List every faction in canon: street gangs, syndicates, cells, advocacy groups, etc.</summary>
    [McpServerTool, Description("List every faction in canon: street gangs, syndicates, cells, advocacy groups, etc.")]
    public Task<string> ListFactions() => hub.InvokeAsync(nameof(CanonTools), nameof(ListFactionsImpl));

    public string ListFactionsImpl()
    {
        factions.Reload();
        var list = factions.GetAll()
            .Select(f => new { name = f.Name, type = f.Type, territory = f.Territory })
            .OrderBy(x => x.name)
            .ToList();
        return JsonSerializer.Serialize(list, JsonOpts);
    }

    /// <summary>Load a faction by name: leadership, structure, territory, motives, alliances, rivalries.</summary>
    [McpServerTool, Description("Load a faction by name: leadership, structure, territory, motives, alliances, rivalries.")]
    public Task<string> GetFaction([Description("Exact faction name.")] string name) =>
        hub.InvokeAsync(nameof(CanonTools), nameof(GetFactionImpl), new { name });

    public string GetFactionImpl(string name)
    {
        var f = factions.GetByName(name);
        if (f == null) return JsonSerializer.Serialize(new { error = "not_found", name }, JsonOpts);
        return JsonSerializer.Serialize(f, JsonOpts);
    }

    /// <summary>List every CorpoNation (corporate sovereign entity).</summary>
    [McpServerTool, Description("List every CorpoNation (corporate sovereign entity).")]
    public Task<string> ListCorponations() => hub.InvokeAsync(nameof(CanonTools), nameof(ListCorponationsImpl));

    public string ListCorponationsImpl()
    {
        corponations.Reload();
        var list = corponations.GetAll()
            .Select(c => new { name = c.Name, sector = c.Sector, sovereign_territory = c.SovereignTerritory })
            .OrderBy(x => x.name)
            .ToList();
        return JsonSerializer.Serialize(list, JsonOpts);
    }

    /// <summary>Load a CorpoNation by name: sector, hierarchy, holdings, public-facing brand, dirty laundry.</summary>
    [McpServerTool, Description("Load a CorpoNation by name: sector, hierarchy, holdings, public-facing brand, dirty laundry.")]
    public Task<string> GetCorponation([Description("Exact CorpoNation name.")] string name) =>
        hub.InvokeAsync(nameof(CanonTools), nameof(GetCorponationImpl), new { name });

    public string GetCorponationImpl(string name)
    {
        var c = corponations.GetByName(name);
        if (c == null) return JsonSerializer.Serialize(new { error = "not_found", name }, JsonOpts);
        return JsonSerializer.Serialize(c, JsonOpts);
    }

    /// <summary>Load the world's literary rules: prohibitions, paragraph requirements, POV voice differentiation rules, register permissions, paragraph economy, interior monologue source. Inject this near the top of any prose-generation prompt.</summary>
    [McpServerTool, Description("Load the world's literary rules: prohibitions, paragraph requirements, POV voice differentiation rules, register permissions, paragraph economy, interior_monologue source. Inject this near the top of any prose-generation prompt.")]
    public Task<string> GetLiteraryRules() => hub.InvokeAsync(nameof(CanonTools), nameof(GetLiteraryRulesImpl));

    public string GetLiteraryRulesImpl()
    {
        return JsonSerializer.Serialize(literaryRules.Get(), JsonOpts);
    }

    internal static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };
}

/// <summary>
/// Tool group for the active book shelf — listing books, loading chapters, pulling
/// outlines, and assembling the per-chapter director context that feeds drafting
/// prompts. Also exposes the type-the-id archive operation.
/// </summary>
[McpServerToolType]
public class StoryTools
{
    private readonly Prose.Core.Interfaces.IBookRepository books;
    private readonly Prose.Core.Interfaces.IChapterRepository chapters;
    private readonly BookOutlineService outlines;
    private readonly HubInvoker hub;

    public StoryTools(
        Prose.Core.Interfaces.IBookRepository books,
        Prose.Core.Interfaces.IChapterRepository chapters,
        BookOutlineService outlines,
        HubInvoker hub)
    {
        this.books = books;
        this.chapters = chapters;
        this.outlines = outlines;
        this.hub = hub;
    }

    /// <summary>List every book on the shelf. Returns id, title, premise, chapter count, status, protagonists.</summary>
    [McpServerTool, Description("List every book on the shelf. Returns id, title, premise, chapter count, status, protagonists.")]
    public Task<string> ListBooks() => hub.InvokeAsync(nameof(StoryTools), nameof(ListBooksImpl));

    public string ListBooksImpl()
    {
        var list = books.ListBooks()
            .Select(b => new { id = b.Id, title = b.Title, premise = b.Premise, chapter_count = b.ChapterIds.Count, status = b.Status, protagonists = b.Protagonists })
            .OrderBy(x => x.title)
            .ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }

    /// <summary>Load a book by id: full metadata, chapter id list (canonical order), state_at_end (open threads, character status carry-forward, canon changes).</summary>
    [McpServerTool, Description("Load a book by id: full metadata, chapter id list (canonical order), state_at_end (open threads, character status carry-forward, canon changes).")]
    public Task<string> GetBook([Description("Book id (32-char hex like 'eb91080d9c9c4f2b9b405fa5996bdea1').")] string id) =>
        hub.InvokeAsync(nameof(StoryTools), nameof(GetBookImpl), new { id });

    public string GetBookImpl(string id)
    {
        var b = books.LoadBook(id);
        if (b == null) return JsonSerializer.Serialize(new { error = "not_found", id }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(b, CanonTools.JsonOpts);
    }

    /// <summary>Load a single chapter by id: synopsis, full HTML body, persisted beats list, participating characters. Use this to read existing prose before extending or revising.</summary>
    [McpServerTool, Description("Load a single chapter by id: synopsis, full HTML body, persisted beats list (each with structure_role + text), participating characters. Use this to read existing prose before extending or revising.")]
    public Task<string> GetChapter([Description("Chapter id (32-char hex).")] string id) =>
        hub.InvokeAsync(nameof(StoryTools), nameof(GetChapterImpl), new { id });

    public string GetChapterImpl(string id)
    {
        var c = chapters.LoadChapter(id);
        if (c == null) return JsonSerializer.Serialize(new { error = "not_found", id }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(c, CanonTools.JsonOpts);
    }

    /// <summary>Load a book's shared outline (plot spine): premise/arc/theme/structure, per-chapter outlines, book-level threads, pending adjustments. Approval status gates prose generation in the UI.</summary>
    [McpServerTool, Description("Load a book's shared outline (the plot spine). Returns premise/arc_target/theme/structure, per-chapter outlines (title, short_synopsis, long_synopsis, key_beats, opens_threads, closes_threads, state_changes, pov_character), book-level threads (planted_in / pays_off_in), pending_adjustments (LLM-proposed neighbor edits). Approval status gates prose generation in the UI.")]
    public Task<string> GetBookOutline([Description("Book id.")] string bookId) =>
        hub.InvokeAsync(nameof(StoryTools), nameof(GetBookOutlineImpl), new { bookId });

    public string GetBookOutlineImpl(string bookId)
    {
        return JsonSerializer.Serialize(outlines.Load(bookId), CanonTools.JsonOpts);
    }

    /// <summary>Build the "WHERE WE ARE" director-context block for a specific chapter: prior chapters' content, this chapter's outline, upcoming setup needs, open book-level threads. Highest-value writing-context tool — call before drafting prose.</summary>
    [McpServerTool, Description("Build the 'WHERE WE ARE' director context block for writing a specific chapter: PRIOR chapters' content, THIS chapter's outline, UPCOMING chapters' setup needs, plus open book-level threads. This is the highest-value writing-context tool — call it before drafting prose for any chapter that's part of a book.")]
    public Task<string> GetDirectorContext(
        [Description("Book id.")] string bookId,
        [Description("Chapter id whose prose you're about to write.")] string chapterId) =>
        hub.InvokeAsync(nameof(StoryTools), nameof(GetDirectorContextImpl), new { bookId, chapterId });

    public string GetDirectorContextImpl(string bookId, string chapterId)
    {
        return outlines.BuildDirectorContext(bookId, chapterId);
    }

    /// <summary>Archive a book — moves the book file from engine/data/books/ to engine/data/archives/books/. Non-destructive (chapters stay in place). Requires the caller to retype the full book id as a confirmation token, matching the UI's type-the-guid modal.</summary>
    [McpServerTool, Description("Archive a book: moves the book file from engine/data/books/ to engine/data/archives/books/. Non-destructive — the original chapters stay in place but the book record is removed from the active shelf. Requires the caller to retype the full book id as a confirmation token (matches the UI's type-the-guid modal). Returns ok:true on success or error:'confirmation_mismatch' / error:'not_found' otherwise.")]
    public Task<string> ArchiveBook(
        [Description("Book id (32-char hex).")] string id,
        [Description("Confirmation token — must equal the same full book id. Mismatched or missing values abort the archive.")] string confirmId) =>
        hub.InvokeAsync(nameof(StoryTools), nameof(ArchiveBookImpl), new { id, confirmId });

    public string ArchiveBookImpl(string id, string confirmId)
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

/// <summary>
/// Tool group for thematic context retrieval — semantic search across the world
/// graph, motif inventory access, and graph-walk neighbor lookups. These help
/// surface canon content by meaning rather than name.
/// </summary>
[McpServerToolType]
public class ContextTools
{
    private readonly SemanticIndexService semanticIndex;
    private readonly UniverseGraphService graph;
    private readonly MotifService motifs;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly HubInvoker hub;

    public ContextTools(
        SemanticIndexService semanticIndex,
        UniverseGraphService graph,
        MotifService motifs,
        IDbContextFactory<ProseDbContext> dbFactory,
        HubInvoker hub)
    {
        this.semanticIndex = semanticIndex;
        this.graph = graph;
        this.motifs = motifs;
        this.dbFactory = dbFactory;
        this.hub = hub;
    }

    /// <summary>Search the world graph by theme rather than by name. TF-IDF cosine similarity over every entity description. Surfaces entities thematically relevant to what you're about to write. Returns ranked id+name+type+score.</summary>
    [McpServerTool, Description("Search the world graph by theme, not by name. TF-IDF cosine similarity across every entity description. Use this to surface entities that are *thematically relevant* to what you're about to write — e.g. searching 'corporate betrayal under-table contract' might return Sable's backstory, the Lotus Syndicate, the Ferrogate enforcement arm. Returns ranked id+name+type+score.")]
    public Task<string> SearchSemantic(
        [Description("Free-text query — describe the theme/scene/concept.")] string query,
        [Description("Number of top hits to return. Default 8.")] int topK = 8) =>
        hub.InvokeAsync(nameof(ContextTools), nameof(SearchSemanticImpl), new { query, topK });

    public string SearchSemanticImpl(string query, int topK = 8)
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

    /// <summary>List the registered motifs for a book — recurring objects, phrases, gestures, sensory threads. Use these in chapters where natural; the review pipeline flags chapters that drop the whole inventory.</summary>
    [McpServerTool, Description("List the registered motifs for a book — recurring objects, phrases, gestures, sensory threads. Mention these in the chapter you're writing where natural; the review pipeline flags chapters that drop the whole inventory.")]
    public Task<string> GetMotifs([Description("Book id.")] string bookId) =>
        hub.InvokeAsync(nameof(ContextTools), nameof(GetMotifsImpl), new { bookId });

    public string GetMotifsImpl(string bookId)
    {
        return JsonSerializer.Serialize(motifs.Load(bookId), CanonTools.JsonOpts);
    }

    /// <summary>Plant a new motif in a book's inventory. Idempotent by name (re-planting with a longer description merges). Same write the UI's Motifs panel issues, exposed here so chat-side authoring can register motifs too.</summary>
    [McpServerTool, Description("Plant a new motif in a book's inventory. Idempotent by name (re-planting with a longer description merges). The user normally accepts these from the Motifs panel in the UI; this tool exposes the same write so chat-side authoring can register them too.")]
    public Task<string> PlantMotif(
        [Description("Book id.")] string bookId,
        [Description("Motif name, e.g. 'brick-wall notebook' or 'the door is unlocked'.")] string name,
        [Description("Short description of what this motif means and where it lands.")] string description,
        [Description("MotifKind: Object, Phrase, Gesture, Sensory, Ritual.")] string kind,
        [Description("Chapter id where this motif is being introduced.")] string introducedInChapterId) =>
        hub.InvokeAsync(nameof(ContextTools), nameof(PlantMotifImpl), new { bookId, name, description, kind, introducedInChapterId });

    public string PlantMotifImpl(string bookId, string name, string description, string kind, string introducedInChapterId)
    {
        if (!Enum.TryParse<MotifKind>(kind, ignoreCase: true, out var kindEnum))
            return JsonSerializer.Serialize(new { error = "invalid_kind", kind, valid = Enum.GetNames(typeof(MotifKind)) }, CanonTools.JsonOpts);
        motifs.Plant(bookId, name, description, kindEnum, introducedInChapterId);
        return JsonSerializer.Serialize(new { ok = true, name, kind = kindEnum.ToString() }, CanonTools.JsonOpts);
    }

    /// <summary>Scan a node's actual written prose for motif candidates. MotifService's heuristic
    /// detector (recurring italicized phrases, recurring capitalized named objects not already
    /// characters/places) existed only against the legacy Chapter model and was never reachable
    /// against a live book — this is the first entry point that runs it on real Nodes/Beats prose.
    /// Read-only: proposals are returned for review, never auto-planted (call plant_motif for any
    /// you want to keep, same as the existing UI workflow this mirrors).</summary>
    [McpServerTool, Description("Scan a node's actual written prose for motif candidates — italicized phrases that recur, or capitalized named objects (not already characters/places) that repeat 3+ times. Returns proposals for review; nothing is written automatically. Pass a chapter-level node for one chapter's beats, or a book-level node to aggregate every chapter's beats. Plant any you want to keep via plant_motif.")]
    public Task<string> ProposeMotifs(
        [Description("Node id (GUID) or slug/code to scan.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(ContextTools), nameof(ProposeMotifsImpl), new { nodeIdOrSlug });

    public async Task<string> ProposeMotifsImpl(string nodeIdOrSlug)
    {
        var nodeId = await ResolveNodeIdAsync(nodeIdOrSlug);
        if (nodeId == null) return Error("node_not_found", nodeIdOrSlug);

        await using var db = await dbFactory.CreateDbContextAsync();
        // IgnoreQueryFilters(): explicit id/slug, not ambient scope (2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId);
        if (node == null) return Error("node_not_found", nodeIdOrSlug);

        // Recurses past any nested Collection (2026-08-09 fix).
        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId.Value);

        var beatIds = await db.BeatNodes.AsNoTracking()
            .Where(bn => searchIds.Contains(bn.NodeId) && true)
            .OrderBy(bn => bn.SortKey)
            .Select(bn => bn.BeatId)
            .ToListAsync();
        if (beatIds.Count == 0) return Error("no_beats", nodeIdOrSlug);

        var beatTexts = await db.Beats.AsNoTracking()
            .Where(b => beatIds.Contains(b.Id) && b.Text != null && b.Text != "")
            .Select(b => b.Text)
            .ToListAsync();
        if (beatTexts.Count == 0) return Error("no_written_beats", nodeIdOrSlug);

        var knownNames = await db.BeatEntityMentions.AsNoTracking()
            .Where(m => beatIds.Contains(m.BeatId))
            .Select(m => m.EntityName)
            .Distinct()
            .ToListAsync();

        var prose = string.Join("\n\n", beatTexts);
        var proposals = motifs.ProposeFromText(nodeId.Value.ToString(), node.Title ?? node.Slug ?? nodeIdOrSlug, prose, knownNames);

        return JsonSerializer.Serialize(new { node_id = nodeId, beat_count = beatTexts.Count, proposals }, CanonTools.JsonOpts);
    }

    /// <summary>
    /// 2026-08-24 consolidation. This copy was already correct on both branches, but a correct
    /// duplicate is still how the next one goes wrong — six of the twelve copies found by the
    /// audit were broken, and the same split defect had been re-patched four times in eight days.
    /// Delegates to <see cref="NodeRefResolver"/>, which additionally accepts a unique GUID prefix.
    /// </summary>
    private Task<Guid?> ResolveNodeIdAsync(string idOrSlug) =>
        NodeRefResolver.ResolveAsync(dbFactory, idOrSlug);

    private static string Error(string code, string detail) =>
        JsonSerializer.Serialize(new { error = code, detail }, CanonTools.JsonOpts);

    /// <summary>Get a graph node's neighbors (relationships) up to N hops. Walks from a known entity to entities related by canon — alliances, rivalries, family, mentor links, location ownership.</summary>
    [McpServerTool, Description("Get a graph node's neighbors (relationships) up to N hops. Use this to walk from a known entity to entities related by canon — alliances, rivalries, family, mentor links, location ownership.")]
    public Task<string> GetNeighbors(
        [Description("Node id (use search_semantic or list_characters to find the id).")] string nodeId,
        [Description("Hops to traverse. 1 = direct neighbors. Default 1.")] int hops = 1) =>
        hub.InvokeAsync(nameof(ContextTools), nameof(GetNeighborsImpl), new { nodeId, hops });

    public string GetNeighborsImpl(string nodeId, int hops = 1)
    {
        graph.EnsureLoaded();
        var neighbors = graph.GetNeighbors(nodeId, hops);
        var list = neighbors.Select(n => new { id = n.Id, name = n.Name, nodeType = n.NodeType }).ToList();
        return JsonSerializer.Serialize(list, CanonTools.JsonOpts);
    }
}
