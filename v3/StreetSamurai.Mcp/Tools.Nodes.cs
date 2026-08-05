using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Node tools — unified beat/node schema ──────────────────────────────
// Mirrors the NodeWorkbenchService surface, exposed to chat-side callers.
// Every mutating operation goes through the workbench so hash invalidation,
// audio cleanup, and fractional-SortKey insertion stay identical between
// the UI and the MCP surface.
//
// All ids are GUID strings. Slugs are also accepted where the parameter
// description says so — the tool resolves slug→id with a single index seek.

[McpServerToolType]
public class NodeTools
{
    private readonly NodeWorkbenchService workbench;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ElevenLabsTtsService tts;
    private readonly NodeBibleService bible;
    private readonly ProseReflowService reflow;
    private readonly BeatRebuildService rebuilder;
    private readonly NodeFullExportService fullExport;
    private readonly NodeSpineService spine;
    private readonly AudiblePackageService audible;
    private readonly NodeDocService nodeDoc;
    private readonly MarkdownFileService markdownFiles;
    private readonly CoverPromptService coverPrompts;
    private readonly CoverImageService coverImages;
    private readonly CoverTitleCompositorService titleCompositor;
    private readonly StreetSamurai.Core.Interfaces.IPathProvider paths;

    public NodeTools(
        NodeWorkbenchService workbench,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ElevenLabsTtsService tts,
        NodeBibleService bible,
        ProseReflowService reflow,
        BeatRebuildService rebuilder,
        NodeFullExportService fullExport,
        NodeSpineService spine,
        AudiblePackageService audible,
        NodeDocService nodeDoc,
        MarkdownFileService markdownFiles,
        CoverPromptService coverPrompts,
        CoverImageService coverImages,
        CoverTitleCompositorService titleCompositor,
        StreetSamurai.Core.Interfaces.IPathProvider paths)
    {
        this.workbench = workbench;
        this.dbFactory = dbFactory;
        this.tts = tts;
        this.bible = bible;
        this.reflow = reflow;
        this.rebuilder = rebuilder;
        this.fullExport = fullExport;
        this.spine = spine;
        this.audible = audible;
        this.nodeDoc = nodeDoc;
        this.markdownFiles = markdownFiles;
        this.coverPrompts = coverPrompts;
        this.coverImages = coverImages;
        this.titleCompositor = titleCompositor;
        this.paths = paths;
    }

    [McpServerTool, Description("Create a SeriesNode — the top-level grouping (saga / anthology) that BookNodes hang under. Never holds beats. Returns the new id, slug, and URL.")]
    public Task<string> CreateSeries(
        [Description("Series title. Required.")] string title,
        [Description("Optional short reference code (e.g. 'BCODA'). Upper-cased; rejected if already in use.")] string code = "",
        [Description("Optional one-line description (back-of-book text).")] string description = "")
        => CreateNodeCoreAsync(title, "series", description, seed: "", targetBeats: 0, parentNodeIdOrSlug: "", code: code, previous: "");

    [McpServerTool, Description("Create a BookNode — a single book arc (book / novella / standalone). Pass 'seed' to also generate a book bible and planned beats immediately. Optional parent makes it part of a series; optional previous marks it a sequel (sequel commandments apply). Returns the new id, slug, url, and (if generated) the bible text.")]
    public Task<string> CreateBook(
        [Description("Book title. Required.")] string title,
        [Description("Optional back-of-book description.")] string description = "",
        [Description("One-line generation seed. When provided, the book bible and planned beats are created immediately after the row is inserted.")] string seed = "",
        [Description("Target beat count for the bible spine (only used when seed is provided). Default 12.")] int targetBeats = 12,
        [Description("Optional parent SeriesNode Guid id (or slug). Empty = standalone.")] string parentNodeIdOrSlug = "",
        [Description("Optional short author-assigned reference code (e.g. 'ATTE'). Uppercased, unique lookup key.")] string code = "",
        [Description("Optional prior book this one continues (slug or GUID) — sequel commandments apply.")] string previous = "")
        => CreateNodeCoreAsync(title, "book", description, seed, targetBeats, parentNodeIdOrSlug, code, previous);

    [McpServerTool, Description("Create a ChapterNode under a book. Chapters hold beats and never carry a reference code. parentNodeIdOrSlug is REQUIRED. Returns the new id, slug, and url.")]
    public Task<string> CreateChapter(
        [Description("Chapter title. Required.")] string title,
        [Description("Parent BookNode Guid id or slug. Required.")] string parentNodeIdOrSlug,
        [Description("Optional back-of-book description.")] string description = "")
        => CreateNodeCoreAsync(title, "chapter", description, seed: "", targetBeats: 0, parentNodeIdOrSlug: parentNodeIdOrSlug, code: "", previous: "");

    /// <summary>Resolve a node reference (GUID or slug) to its id. Empty input → null.</summary>
    private async Task<Guid?> ResolveNodeIdAsync(string? slugOrId)
    {
        if (string.IsNullOrWhiteSpace(slugOrId)) return null;
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(slugOrId, out var gid))
            return await db.Nodes.AsNoTracking().AnyAsync(s => s.Id == gid) ? gid : (Guid?)null;
        return await db.Nodes.AsNoTracking()
            .Where(s => s.Slug == slugOrId || s.NodeCode == slugOrId).Select(s => (Guid?)s.Id).FirstOrDefaultAsync();
    }

    [McpServerTool, Description("List nodes. Use kind='book' to list all root narratives; kind='chapter' for all sub-nodes (contain beats). Returns a flat list of id, slug, title, kind, status, beat-count, stale-count.")]
    public async Task<string> ListBooks(
        [Description("Optional Kind filter — 'book' (root nodes) or 'chapter' (sub-nodes with beats). Case-insensitive equality match.")] string kind = "",
        [Description("Maximum rows to return. Default 100.")] int limit = 100)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.Nodes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(kind))
            q = q.Where(s => s.Kind == kind.ToLowerInvariant());
        var rows = await q.OrderBy(s => s.Kind).ThenBy(s => s.Title).Take(limit).ToListAsync();

        var ids = rows.Select(r => r.Id).ToList();
        var beatCounts = await db.BeatNodes
            .Where(sb => ids.Contains(sb.NodeId) && sb.IsEnabled)
            .GroupBy(sb => sb.NodeId)
            .Select(g => new { NodeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.NodeId, x => x.Count);

        var result = rows.Select(s => new
        {
            id = s.Id,
            slug = s.Slug,
            title = s.Title,
            kind = s.Kind,
            status = s.Status,
            beats = beatCounts.GetValueOrDefault(s.Id, 0),
            has_bible = s.NodeBible != null,
            parent_node_id = s.ParentNodeId,
        });
        return JsonSerializer.Serialize(result, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Get a single node with its beats in reading order. Accepts a Guid id OR a slug. Returns node metadata + ordered beats (id, text, stale, has_audio, title, description).")]
    public async Task<string> GetBook(
        [Description("Node Guid id or slug.")] string idOrSlug)
    {
        var node = await ResolveNodeAsync(idOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);
        var beats = await workbench.GetOrderedBeatsAsync(node.Id);
        return JsonSerializer.Serialize(new
        {
            id = node.Id, slug = node.Slug, title = node.Title, kind = node.Kind,
            status = node.Status, description = node.Description, seed = node.Seed,
            voice_id = node.VoiceId,
            parent_node_id = node.ParentNodeId, chars_narrated = node.CharsNarrated,
            has_bible = node.NodeBible != null,
            node_bible_generated_at = node.NodeBibleGeneratedAt,
            beats = beats.Select((b, i) => new
            {
                position = i + 1,
                id = b.Beat.Id,
                text = b.Beat.Text,
                stale = b.Beat.Stale,
                has_audio = !string.IsNullOrEmpty(b.Beat.AudioPath),
                duration_sec = b.Beat.DurationSec,
                title = b.Beat.Title,
                description = b.Beat.Description,
            }),
        }, CanonTools.JsonOpts);
    }

    /// <summary>Shared implementation behind CreateSeries / CreateBook / CreateChapter.</summary>
    private async Task<string> CreateNodeCoreAsync(
        string title, string kind, string description, string seed,
        int targetBeats, string parentNodeIdOrSlug, string code, string previous)
    {
        if (string.IsNullOrWhiteSpace(title))
            return JsonSerializer.Serialize(new { error = "title_required" }, CanonTools.JsonOpts);
        var resolvedKind = string.IsNullOrEmpty(kind) ? "book" : kind;

        Guid? previousId = await ResolveNodeIdAsync(previous);
        if (!string.IsNullOrWhiteSpace(previous) && previousId == null)
            return JsonSerializer.Serialize(new { error = "previous_node_not_found", previous }, CanonTools.JsonOpts);

        Guid? parentId = null;
        if (!string.IsNullOrWhiteSpace(parentNodeIdOrSlug))
        {
            var parent = await ResolveNodeAsync(parentNodeIdOrSlug);
            if (parent == null) return JsonSerializer.Serialize(new { error = "parent_node_not_found", parentNodeIdOrSlug }, CanonTools.JsonOpts);
            var kindErr = KindCompatibilityError(parent.Kind, resolvedKind);
            if (kindErr != null) return JsonSerializer.Serialize(new { error = "kind_incompatible", message = kindErr }, CanonTools.JsonOpts);
            parentId = parent.Id;
        }
        else if (resolvedKind == "chapter")
        {
            return JsonSerializer.Serialize(new { error = "kind_incompatible", message = "A chapter must have a parent book. Provide parentNodeIdOrSlug." }, CanonTools.JsonOpts);
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.CreateVersion7();
        var baseSlug = System.Text.RegularExpressions.Regex
            .Replace((title ?? "").ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "node";
        var slug = $"{baseSlug}-{id.ToString("N")[..8]}";

        await using var nodeSortTx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var maxSort = parentId.HasValue
            ? await db.Nodes.Where(s => s.ParentNodeId == parentId).Select(s => (double?)s.SortKey).MaxAsync() ?? 0
            : await db.Nodes.Where(s => s.ParentNodeId == null).Select(s => (double?)s.SortKey).MaxAsync() ?? 0;

        var node = NodeFactory.Create(resolvedKind);
        node.Id = id;
        node.Slug = slug;
        node.Title = title ?? "";
        node.Description = string.IsNullOrEmpty(description) ? null : description;
        node.Seed = string.IsNullOrEmpty(seed) ? null : seed;
        node.Status = "draft";
        node.ParentNodeId = parentId;
        node.PreviousNodeId = previousId;
        node.SortKey = maxSort + 100.0;
        node.NodeCode = resolvedKind == "chapter" || string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
        db.Nodes.Add(node);
        await db.SaveChangesAsync();
        await nodeSortTx.CommitAsync();

        // If a seed was provided, generate the node bible and planned beats immediately.
        string? bibleText = null;
        if (!string.IsNullOrWhiteSpace(seed))
        {
            try { bibleText = await bible.GenerateAndSaveAsync(id, seed, title, targetBeats <= 0 ? 12 : targetBeats); }
            catch (Exception ex) { bibleText = $"[bible generation failed: {ex.Message}]"; }
        }

        // Scaffold user stories (and a bible template if no seed was provided).
        await spine.ScaffoldAsync(id, title ?? "", bibleAlreadySet: bibleText != null);

        return JsonSerializer.Serialize(new { ok = true, id, slug, url = $"/node/{slug}", node_bible = bibleText }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Deep-duplicate a node (and its sub-node tree) into a fresh, independent copy. Every beat is cloned into a new row — prose and narration metadata are preserved, but audio, review scores, and the stale flag are reset. Editing the copy never affects the original. Accepts a Guid id OR a slug. Returns the new node's id, slug, and writer URL.")]
    public async Task<string> DuplicateBook(
        [Description("Source node Guid id or slug.")] string idOrSlug,
        [Description("Title for the new duplicate. Required.")] string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            return JsonSerializer.Serialize(new { error = "title_required" }, CanonTools.JsonOpts);
        var source = await ResolveNodeAsync(idOrSlug);
        if (source == null) return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

        var (id, slug) = await workbench.DuplicateNodeAsync(source.Id, newTitle);
        return JsonSerializer.Serialize(new { ok = true, id, slug, title = newTitle, url = $"/node/{slug}", source_id = source.Id }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Clone a node into a fully independent copy: new Node row + new Beat rows, same prose. Audio, scores, and review history are NOT copied — clone starts fresh. Supports nodeCode for per-experiment isolation. Use this instead of DuplicateBook when you need nodeCode or per-experiment isolation. Returns new id, slug, beat count.")]
    public async Task<string> CloneBook(
        [Description("Source node Guid id or slug.")] string idOrSlug,
        [Description("Title for the clone. Defaults to 'Source Title (Clone)'.")] string title = "",
        [Description("Optional short reference code for the clone (e.g. 'SM1'). Rejected if already in use.")] string nodeCode = "",
        [Description("Status value to stamp on the clone: 'ready', 'draft', etc. Default 'ready'.")] string status = "ready")
    {
        var source = await ResolveNodeAsync(idOrSlug);
        if (source == null) return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

        var code = string.IsNullOrWhiteSpace(nodeCode) ? null : nodeCode.Trim().ToUpperInvariant();
        if (code != null)
        {
            await using var check = await dbFactory.CreateDbContextAsync();
            var clash = await check.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.NodeCode == code);
            if (clash != null)
                return JsonSerializer.Serialize(new { error = "node_code_in_use", code, clash_slug = clash.Slug }, CanonTools.JsonOpts);
        }

        await using var db = await dbFactory.CreateDbContextAsync();

        var sourceBeats = await db.Set<BeatNode>()
            .AsNoTracking()
            .Where(sb => sb.NodeId == source.Id && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats.AsNoTracking(), sb => sb.BeatId, b => b.Id,
                  (sb, b) => new { sb.SortKey, Beat = b })
            .ToListAsync();

        var newTitle  = string.IsNullOrWhiteSpace(title) ? $"{source.Title} (Clone)" : title.Trim();
        var newId     = Guid.CreateVersion7();
        var sanitised = new string(newTitle.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        var parts     = sanitised.Split('-').Where(p => p.Length > 0).Take(8);
        var newSlug   = $"{string.Join("-", parts)}-{newId.ToString("N")[..8]}";

        var now = DateTime.UtcNow;

        // Serializable isolation covers both the SortKey MAX (prevents duplicate sort order)
        // and the Beat.Number MAX (prevents duplicate-key on concurrent clones).
        await using var tx = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);

        var maxSort = await db.Nodes.Where(s => s.ParentNodeId == null).Select(s => (double?)s.SortKey).MaxAsync() ?? 0;

        var clone = NodeFactory.CreateLike(source);
        clone.Id              = newId;
        clone.Slug            = newSlug;
        clone.Title           = newTitle;
        clone.NodeCode        = code;
        clone.Kind            = source.Kind;
        clone.Status          = status;
        clone.Description     = source.Description;
        clone.Seed            = source.Seed;
        clone.UniverseId      = source.UniverseId;
        clone.VoiceId         = source.VoiceId;
        clone.VoiceModel      = source.VoiceModel;
        clone.VoiceStability  = source.VoiceStability;
        clone.VoiceSimilarity = source.VoiceSimilarity;
        clone.VoiceStyle      = source.VoiceStyle;
        clone.VoiceSeed       = source.VoiceSeed;
        clone.TtsEngine       = source.TtsEngine;
        clone.SortKey         = maxSort + 100.0;
        clone.CreatedAt       = now;
        clone.UpdatedAt       = now;
        db.Nodes.Add(clone);

        var beatMax = await db.Beats.MaxAsync(b => (int?)b.Number) ?? 0;
        int nextNum = beatMax + 1;

        foreach (var entry in sourceBeats)
        {
            var src    = entry.Beat;
            var beatId = Guid.CreateVersion7();
            db.Beats.Add(new Beat
            {
                Id               = beatId,
                Number           = nextNum++,
                Text             = src.Text,
                Title            = src.Title,
                Description      = src.Description,
                StructureRole    = src.StructureRole,
                Act              = src.Act,
                SceneType        = src.SceneType,
                EmotionalTone    = src.EmotionalTone,
                PaceHint         = src.PaceHint,
                Kind             = src.Kind,
                IsChapterStart   = src.IsChapterStart,
                GapAfterMs       = src.GapAfterMs,
                GapAfterAudioPath = src.GapAfterAudioPath,
                CreatedAt        = now,
                UpdatedAt        = now,
            });
            db.BeatNodes.Add(new BeatNode
            {
                NodeId  = newId,
                BeatId    = beatId,
                SortKey   = entry.SortKey,
                IsEnabled = true,
            });
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return JsonSerializer.Serialize(new
        {
            ok         = true,
            id         = newId,
            slug       = newSlug,
            title      = newTitle,
            code,
            beat_count = sourceBeats.Count,
            source_id  = source.Id,
            url        = $"/node/{newSlug}",
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Insert a new beat into a node. Pass an empty afterBeatId to insert at the top. Returns the new beat's id.")]
    public async Task<string> InsertBeat(
        [Description("Node Guid id or slug.")] string nodeIdOrSlug,
        [Description("Beat Guid id to insert after, or empty for top-of-node.")] string afterBeatId = "",
        [Description("Initial prose text for the new beat. May be empty.")] string text = "")
    {
        var node = await ResolveNodeAsync(nodeIdOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);
        Guid? after = null;
        if (!string.IsNullOrWhiteSpace(afterBeatId))
        {
            if (!Guid.TryParse(afterBeatId, out var ag))
                return JsonSerializer.Serialize(new { error = "bad_beat_id", afterBeatId }, CanonTools.JsonOpts);
            after = ag;
        }
        var beat = await workbench.InsertBeatAsync(node.Id, after, text ?? "");
        return JsonSerializer.Serialize(new { ok = true, id = beat.Id, node_id = node.Id }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Get a single beat with every authoring field — prose, kind, IsChapterStart, BeatTitle, gap-after, tone/pace/facet metadata, position within node, and the previous/next beat ids for relative insertion. Accepts a plain Beat Guid or the 'node-guid.beat-guid' dotted handle the writer UI shows on the LLM bottom sheet.")]
    public async Task<string> GetBeat(
        [Description("Beat Guid OR the dotted 'node-guid.beat-guid' handle.")] string beatHandle)
    {
        if (!BeatHandle.TryParse(beatHandle, out var parsedNode, out var parsedBeat) || parsedBeat == null)
            return JsonSerializer.Serialize(new { error = "bad_beat_handle", beatHandle }, CanonTools.JsonOpts);

        await using var db = await dbFactory.CreateDbContextAsync();
        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == parsedBeat.Value);
        if (beat == null) return JsonSerializer.Serialize(new { error = "beat_not_found", beatHandle }, CanonTools.JsonOpts);

        // Resolve the node that owns this beat — either the one from the
        // dotted handle (if any), or the first BeatNode junction.
        var nodeId = parsedNode ?? (await db.BeatNodes.AsNoTracking()
            .Where(sb => sb.BeatId == beat.Id && sb.IsEnabled)
            .Select(sb => (Guid?)sb.NodeId)
            .FirstOrDefaultAsync());
        Node? node = null;
        int position = 0;
        Guid? prevBeatId = null;
        Guid? nextBeatId = null;
        if (nodeId.HasValue)
        {
            node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == nodeId.Value);
            var ordered = await workbench.GetOrderedBeatsAsync(nodeId.Value);
            var idx = ordered.FindIndex(o => o.Beat.Id == beat.Id);
            if (idx >= 0)
            {
                position = idx + 1;
                if (idx > 0)                   prevBeatId = ordered[idx - 1].Beat.Id;
                if (idx < ordered.Count - 1)   nextBeatId = ordered[idx + 1].Beat.Id;
            }
        }
        return JsonSerializer.Serialize(new
        {
            id              = beat.Id,
            handle          = nodeId.HasValue ? $"{nodeId.Value}.{beat.Id}" : beat.Id.ToString(),
            number          = beat.Number,
            position,
            node          = node == null ? null : (object)new { id = node.Id, slug = node.Slug, title = node.Title },
            prev_beat_id    = prevBeatId,
            next_beat_id    = nextBeatId,
            text            = beat.Text,
            kind            = beat.Kind,
            is_chapter_start = beat.IsChapterStart,
            title           = beat.Title,
            description     = beat.Description,
            subtext         = beat.Subtext,
            structure_role  = beat.StructureRole,
            act             = beat.Act,
            scene_type      = beat.SceneType,
            emotional_tone  = beat.EmotionalTone,
            pace_hint       = beat.PaceHint,
            gap_after_ms    = beat.GapAfterMs,
            gap_after_audio = beat.GapAfterAudioPath,
            has_audio       = !string.IsNullOrEmpty(beat.AudioPath),
            stale           = beat.Stale,
            updated_at      = beat.UpdatedAt,
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Update one beat's prose. Recomputes the hash, marks the beat stale, and invalidates its audio. Beat.Text accepts inline markdown (**bold** / *italic* / __underline__ / ~~strike~~) and ElevenLabs-style tone tags ([WHISPERING] [GASP] [LAUGHS] [PAUSES] etc.) that render as emoji in the read view. Accepts a Beat Guid OR the 'node-guid.beat-guid' handle.")]
    public async Task<string> UpdateBeatText(
        [Description("Beat Guid OR 'node-guid.beat-guid' handle.")] string beatHandle,
        [Description("New prose. Replaces the entire beat text. Markdown markers + tone-tag brackets are preserved verbatim in storage.")] string text)
    {
        if (!BeatHandle.TryParse(beatHandle, out _, out var bid) || bid == null)
            return JsonSerializer.Serialize(new { error = "bad_beat_handle", beatHandle }, CanonTools.JsonOpts);
        await workbench.UpdateBeatTextAsync(bid.Value, text ?? "");
        return JsonSerializer.Serialize(new { ok = true, id = bid.Value }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Update a beat's metadata: Title, Description, EmotionalTone, PaceHint, StructureRole, Act, SceneType, IsChapterStart, Kind. Pass empty strings to clear nullable fields. Does NOT touch prose or audio. Use to mark a beat as a chapter start, change its kind to quote/dedication/book-title, or set the tone the next re-record uses.")]
    public async Task<string> UpdateBeatMetadata(
        [Description("Beat Guid OR 'node-guid.beat-guid' handle.")] string beatHandle,
        [Description("Short label. When IsChapterStart=true this is the chapter heading; when Kind=quote this is the attribution.")] string title = "",
        [Description("One-line description fed to LLM regenerations.")] string description = "",
        [Description("What is happening beneath the prose — foreshadowing, unspoken motivations, dramatic irony. Visible to the prose writer LLM but never printed.")] string subtext = "",
        [Description("Emotional tone, e.g. 'quiet' / 'tense' / 'wry'.")] string emotionalTone = "",
        [Description("Pace hint, e.g. 'flowing' / 'clipped' / 'staccato' / 'languorous'.")] string paceHint = "",
        [Description("Structure role, e.g. 'inciting-incident' / 'rising-action' / 'climax'.")] string structureRole = "",
        [Description("Plot-act number 0–5. 0 = unassigned.")] int act = 0,
        [Description("Scene type: scene | summary | transition | interstitial.")] string sceneType = "scene",
        [Description("True = this beat begins a new chapter / section. The writer renders a divider above it with Title as the heading.")] bool isChapterStart = false,
        [Description("Beat kind: prose (default) | book-title | dedication | quote. Free-form so new kinds add no schema cost.")] string kind = "prose")
    {
        if (!BeatHandle.TryParse(beatHandle, out _, out var bid) || bid == null)
            return JsonSerializer.Serialize(new { error = "bad_beat_handle", beatHandle }, CanonTools.JsonOpts);
        await workbench.UpdateBeatMetadataAsync(bid.Value, new NodeWorkbenchService.BeatMetadataUpdate(
            Title:          title,
            Description:    description,
            Subtext:        subtext,
            EmotionalTone:  emotionalTone,
            PaceHint:       paceHint,
            StructureRole:  structureRole,
            Act:            act,
            SceneType:      sceneType,
            IsChapterStart: isChapterStart,
            Kind:           kind));
        return JsonSerializer.Serialize(new { ok = true, id = bid.Value }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Set the silence (in ms) the audio engine inserts AFTER this beat, before the next. 0 = no silence (explicit override). Use ClearBeatGapAfter to revert to the auto-computed default from SceneType + terminator punctuation.")]
    public async Task<string> SetBeatGapAfter(
        [Description("Beat Guid OR 'node-guid.beat-guid' handle.")] string beatHandle,
        [Description("Silence in milliseconds, 0..6000.")] int durationMs)
    {
        if (!BeatHandle.TryParse(beatHandle, out _, out var bid) || bid == null)
            return JsonSerializer.Serialize(new { error = "bad_beat_handle", beatHandle }, CanonTools.JsonOpts);
        await workbench.SetGapAfterAsync(bid.Value, durationMs);
        return JsonSerializer.Serialize(new { ok = true, id = bid.Value, gap_after_ms = Math.Max(0, durationMs) }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Clear an explicit gap-after-beat override. The audio engine falls back to the auto-computed silence from SceneType + terminator punctuation.")]
    public async Task<string> ClearBeatGapAfter(
        [Description("Beat Guid OR 'node-guid.beat-guid' handle.")] string beatHandle)
    {
        if (!BeatHandle.TryParse(beatHandle, out _, out var bid) || bid == null)
            return JsonSerializer.Serialize(new { error = "bad_beat_handle", beatHandle }, CanonTools.JsonOpts);
        await workbench.ClearGapAfterAsync(bid.Value);
        return JsonSerializer.Serialize(new { ok = true, id = bid.Value }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Split one beat into two at the nearest sentence boundary near its midpoint. Both halves lose their audio.")]
    public async Task<string> SplitBeat(
        [Description("Node Guid id or slug.")] string nodeIdOrSlug,
        [Description("Beat Guid id to split.")] string beatId)
    {
        var node = await ResolveNodeAsync(nodeIdOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);
        if (!Guid.TryParse(beatId, out var bid)) return JsonSerializer.Serialize(new { error = "bad_beat_id", beatId }, CanonTools.JsonOpts);
        var newBeat = await workbench.SplitBeatAsync(node.Id, bid);
        return JsonSerializer.Serialize(new { ok = true, original = bid, new_beat = newBeat.Id }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Merge one beat into the previous one in the node. Audio on the survivor is invalidated.")]
    public async Task<string> JoinBeat(
        [Description("Node Guid id or slug.")] string nodeIdOrSlug,
        [Description("Beat Guid id to merge upward.")] string beatId)
    {
        var node = await ResolveNodeAsync(nodeIdOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);
        if (!Guid.TryParse(beatId, out var bid)) return JsonSerializer.Serialize(new { error = "bad_beat_id", beatId }, CanonTools.JsonOpts);
        await workbench.JoinBeatWithPreviousAsync(node.Id, bid);
        return JsonSerializer.Serialize(new { ok = true, id = bid }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Remove a beat from a node. If the beat is not referenced by any other node, the beat row + audio file are deleted entirely.")]
    public async Task<string> DeleteBeat(
        [Description("Node Guid id or slug.")] string nodeIdOrSlug,
        [Description("Beat Guid id to delete.")] string beatId)
    {
        var node = await ResolveNodeAsync(nodeIdOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);
        if (!Guid.TryParse(beatId, out var bid)) return JsonSerializer.Serialize(new { error = "bad_beat_id", beatId }, CanonTools.JsonOpts);
        await workbench.DeleteBeatAsync(node.Id, bid);
        return JsonSerializer.Serialize(new { ok = true, id = bid }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Kick off TTS narration for every un-narrated beat in this node (and its child nodes recursively). Returns immediately — narration runs in the background; poll get_node to observe progress. Returns an error response (without spawning anything) if TTS is not configured.")]
    public async Task<string> NarrateBook(
        [Description("Node Guid id or slug.")] string nodeIdOrSlug)
    {
        var node = await ResolveNodeAsync(nodeIdOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);
        // Pre-flight: without this check, an unconfigured TTS account causes
        // NarrateAsync to throw InvalidOperationException into the
        // unobserved-task void; the MCP caller saw {ok:true} and nothing
        // ever happened. Return the typed error here instead.
        if (!await tts.IsConfiguredAsync())
            return JsonSerializer.Serialize(new { error = "tts_not_configured", message = "ElevenLabs API key is missing. Set it in Settings before calling narrate_node." }, CanonTools.JsonOpts);
        _ = Task.Run(async () =>
        {
            try { await workbench.NarrateAsync(node.Id); }
            catch (Exception ex) { Console.Error.WriteLine($"[mcp:narrate_node] {node.Id}: {ex.Message}"); }
        });
        return JsonSerializer.Serialize(new { ok = true, id = node.Id, status = "narrating" }, CanonTools.JsonOpts);
    }

    // ── Node Bible tools ────────────────────────────────────────────────

    [McpServerTool, Description("Get the node bible for a node — the dry structural plan (logline, premise, register, characters, beat spine, seeds & payoffs). Returns the raw markdown text plus the parsed beat spine entries so you can see the planned arc at a glance. Returns has_bible=false when no bible exists yet.")]
    public async Task<string> GetBookBible(
        [Description("Node Guid id or slug.")] string idOrSlug)
    {
        var node = await ResolveNodeAsync(idOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

        if (string.IsNullOrEmpty(node.NodeBible))
            return JsonSerializer.Serialize(new { has_bible = false, id = node.Id, slug = node.Slug, title = node.Title }, CanonTools.JsonOpts);

        var spine = NodeBibleService.ParseBeatSpine(node.NodeBible)
            .Select(p => new { index = p.Index, title = p.Title, goal = p.Goal, structure_role = p.StructureRole });

        return JsonSerializer.Serialize(new
        {
            has_bible = true,
            id = node.Id,
            slug = node.Slug,
            title = node.Title,
            generated_at = node.NodeBibleGeneratedAt,
            bible = node.NodeBible,
            beat_spine = spine,
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Generate (or regenerate) the node bible for a node. Uses the node's Seed field (falls back to Synopsis then Title) plus the literary rules to produce a dry structural plan: logline, premise, register, characters, numbered beat spine, seeds & payoffs. Creates planned Beat rows from the spine when the node has no beats yet. Returns the generated bible text.")]
    public async Task<string> GenerateBookBible(
        [Description("Node Guid id or slug.")] string idOrSlug,
        [Description("Target number of beats in the spine. 0 = auto (use existing beat count or 12).")] int targetBeats = 0)
    {
        var node = await ResolveNodeAsync(idOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

        var seed = node.Seed ?? node.Description ?? node.Title;
        if (string.IsNullOrWhiteSpace(seed))
            return JsonSerializer.Serialize(new { error = "no_seed", message = "Node has no Seed or Description to drive generation. Set one first with SetBookBible or UpdateBeatMetadata." }, CanonTools.JsonOpts);

        if (targetBeats <= 0)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            targetBeats = await db.BeatNodes.CountAsync(sb => sb.NodeId == node.Id && sb.IsEnabled);
            if (targetBeats <= 0) targetBeats = 12;
        }

        string bibleText;
        try { bibleText = await bible.GenerateAndSaveAsync(node.Id, seed, node.Title, targetBeats); }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = "generation_failed", message = ex.Message, inner = ex.InnerException?.Message, deep = ex.InnerException?.InnerException?.Message }, CanonTools.JsonOpts); }

        var spine = NodeBibleService.ParseBeatSpine(bibleText)
            .Select(p => new { index = p.Index, title = p.Title, goal = p.Goal, structure_role = p.StructureRole });

        // Cascade immediately — GenerateAndSaveAsync only wrote Node.NodeBible; the docs/nodes/{CODE}.md
        // mirror and the MarkdownFiles row DocContextService actually reads both need to reflect it too.
        var genResult = await nodeDoc.GenerateAsync(node.Id);
        var syncResult = await markdownFiles.SyncAllAsync();

        return JsonSerializer.Serialize(new
        {
            ok          = true,
            id          = node.Id,
            slug        = node.Slug,
            bible       = bibleText,
            beat_spine  = spine,
            regenerated = true,
            file_path   = genResult.Path,
            synced      = new { inserted = syncResult.Inserted, updated = syncResult.Updated, unchanged = syncResult.Unchanged, errors = syncResult.Errors },
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Manually set or replace the node bible text. Use when you want to hand-write the plan instead of generating it. The text is saved verbatim; beat spine parsing still applies for planned-beat creation. Pass an empty string to clear the bible. The docs/nodes/{CODE}.md mirror and MarkdownFiles sync (what DocContextService reads) are regenerated automatically as part of this call.")]
    public async Task<string> SetBookBible(
        [Description("Node Guid id or slug.")] string idOrSlug,
        [Description("Full bible markdown text to store. Empty string clears the bible.")] string bibleText)
    {
        var node = await ResolveNodeAsync(idOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.Nodes.FindAsync(node.Id)
            ?? throw new InvalidOperationException($"Node {node.Id} missing.");

        row.NodeBible = string.IsNullOrEmpty(bibleText) ? null : bibleText;
        row.NodeBibleGeneratedAt = DateTime.UtcNow;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Cascade immediately — same reasoning as GenerateBookBible/SetCanonSection: propagation
        // is part of the write, not a follow-up step the caller has to remember.
        var genResult = await nodeDoc.GenerateAsync(node.Id);
        var syncResult = await markdownFiles.SyncAllAsync();

        return JsonSerializer.Serialize(new
        {
            ok          = true,
            id          = node.Id,
            slug        = node.Slug,
            cleared     = string.IsNullOrEmpty(bibleText),
            regenerated = true,
            file_path   = genResult.Path,
            synced      = new { inserted = syncResult.Inserted, updated = syncResult.Updated, unchanged = syncResult.Unchanged, errors = syncResult.Errors },
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Assemble the unified Book Context Document for a node: merges hand-authored NodeBible content with the Structural Blueprint and Beat Spine from the DB, then writes the result to both Nodes.NodeBible and docs/nodes/{CODE}.md. The MarkdownFiles sync (what DocContextService reads at generation time) runs automatically as part of this call — no follow-up call needed. Run this before editing a book to get a fresh, complete context document. The disk file is a read-only generated mirror — never hand-edit it.")]
    public async Task<string> GenerateNodeDoc(
        [Description("Node id (GUID), slug, or NodeCode.")] string nodeIdOrSlug)
    {
        var node = await ResolveNodeAsync(nodeIdOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);

        try
        {
            var result = await nodeDoc.GenerateAsync(node.Id);
            var syncResult = await markdownFiles.SyncAllAsync();
            return JsonSerializer.Serialize(new
            {
                ok           = true,
                node_code    = result.NodeCode,
                beat_count   = result.BeatCount,
                has_blueprint = result.HasBlueprint,
                path         = result.Path,
                generated_at = result.GeneratedAt,
                synced       = new { inserted = syncResult.Inserted, updated = syncResult.Updated, unchanged = syncResult.Unchanged, errors = syncResult.Errors },
            }, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "generation_failed", message = ex.Message }, CanonTools.JsonOpts);
        }
    }

    /// <summary>Copy-edit a node's prose in-place: proper paragraph/dialogue spacing, "?" on questions, "asks"/"asked" on question dialogue. Dry-run by default — pass apply=true to commit. Returns a report of what changed, was rejected, or errored.</summary>
    [McpServerTool, Description("Copy-edit a node's prose in-place: adds missing '?' on questions, swaps 'says/said' → 'asks/asked' on question dialogue lines, and normalises paragraph/dialogue spacing. Dry-run by default — set apply=true to commit. Beats the model modified beyond those specific edits are rejected and left untouched. Returns changed/unchanged/rejected/errors counts plus per-beat diff previews.")]
    public async Task<string> ReflowBook(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("Set to true to write the edits to the DB. Default false = dry run.")] bool apply = false)
    {
        var node = await ResolveNodeAsync(nodeIdOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);

        var report = await reflow.ReflowNodeAsync(node.Id, apply);
        return JsonSerializer.Serialize(new
        {
            node_id    = report.NodeId,
            slug         = report.Slug,
            applied      = report.Applied,
            total        = report.Total,
            changed      = report.Changed,
            unchanged    = report.Unchanged,
            rejected     = report.Rejected,
            errors       = report.Errors,
            beats        = report.Beats.Where(b => b.Status is not "unchanged" and not "empty").Select(b => new
            {
                beat_id              = b.BeatId,
                position             = b.Position,
                status               = b.Status,
                question_marks_added = b.QuestionMarksAdded,
                attribution_swaps    = b.AttributionSwaps,
                reason               = b.Reason,
                before_preview       = b.BeforePreview,
                after_preview        = b.AfterPreview,
            }),
        }, CanonTools.JsonOpts);
    }

    /// <summary>LLM-rebeat a node: re-segment all beats to the beat doctrine (proper formatting, no run-ons, no sentence-shrapnel). Dry-run by default; set apply=true to export a backup then replace beats (only if the word-retention guard passes).</summary>
    [McpServerTool, Description("Re-segment a node's beats to the codified beat doctrine via LLM re-segmentation. Dry-run by default (safe to call freely). Set apply=true to export a Markdown backup then replace the beats — only committed if the word-retention guard passes (prevents silent content loss). Returns old/new beat counts, retention %, guard result, and a note if it was blocked.")]
    public async Task<string> RebeatBook(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("Set to true to commit the new segmentation. Default false = dry run.")] bool apply = false)
    {
        var node = await ResolveNodeAsync(nodeIdOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);

        // Beats live on chapter children, not the book node (SS-A43) — rebeat must
        // target the chapter(s). RebuildAsync's own beat lookup walks down to child
        // beats, but its write-back (delete old BeatNodes / insert new) uses whatever
        // id it's given, so passing the book id here silently wrote the resegmented
        // beats onto the book node while the chapter's original beats sat untouched.
        var targets = new List<(Guid Id, string Label)> { (node.Id, node.Title ?? node.Slug ?? node.Id.ToString()) };
        if (node.Kind == "book")
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var chapters = await db.Nodes.AsNoTracking()
                .Where(c => c.ParentNodeId == node.Id && c.Kind == "chapter")
                .OrderBy(c => c.SortKey)
                .Select(c => new { c.Id, c.Title })
                .ToListAsync();
            if (chapters.Count > 0)
                targets = chapters.Select(c => (c.Id, $"{node.Title} / {c.Title}")).ToList();
        }

        var reports = new List<BeatRebuildService.BeatRebuildReport>();
        foreach (var (id, _) in targets)
            reports.Add(await rebuilder.RebuildAsync(id, apply));

        object Shape(BeatRebuildService.BeatRebuildReport r) => new
        {
            node_id      = r.NodeId,
            slug           = r.Slug,
            title          = r.Title,
            applied        = r.Applied,
            old_beats      = r.OldBeats,
            new_beats      = r.NewBeats,
            word_retention = r.WordRetention,
            guard_passed   = r.GuardPassed,
            backup_path    = r.BackupPath,
            note           = r.Note,
        };

        return reports.Count == 1
            ? JsonSerializer.Serialize(Shape(reports[0]), CanonTools.JsonOpts)
            : JsonSerializer.Serialize(new { chapters = reports.Select(Shape).ToList() }, CanonTools.JsonOpts);
    }

    /// <summary>Export a node to every KDP-ready format (docx/epub/pdf/txt) plus description.txt/keywords.txt/cover.jpg, to the configured export directory (defaults to Desktop). Same pipeline as the CLI's `ss --export-node`, via the shared NodeFullExportService. Local file rendering only — no KDP API integration.</summary>
    [McpServerTool, Description("Render a node to .docx + .epub + .pdf + .txt, plus description.txt (from Node.Description), keywords.txt (from seeded NodeKeywords), and cover.jpg (only if missing), all written to the configured export directory (defaults to Desktop). Same full pipeline as the CLI's `ss --export-node --slug <slug>`. Returns the path of every artifact written (nulls for the optional ones that had no source data). This only generates local files — it does not publish anything to Amazon/KDP. Use get_node first to confirm the node exists.")]
    public async Task<string> ExportNode(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("Author name to embed in the document properties. Optional.")] string author = "")
    {
        var node = await ResolveNodeAsync(nodeIdOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);

        var result = await fullExport.ExportAllAsync(node.Id, string.IsNullOrWhiteSpace(author) ? null : author);
        return JsonSerializer.Serialize(new
        {
            ok = true,
            path = result.DocxPath,
            docx_path = result.DocxPath,
            epub_path = result.EpubPath,
            pdf_path = result.PdfPath,
            txt_path = result.TxtPath,
            docx_mojibake_hits = result.DocxMojibakeHits,
            description_path = result.DescriptionPath,
            description_mojibake_repaired = result.DescriptionMojibakeRepaired,
            synopsis_path = result.SynopsisPath,
            keywords_path = result.KeywordsPath,
            keyword_count = result.KeywordCount,
            cover_path = result.CoverPath,
        }, CanonTools.JsonOpts);
    }

    /// <summary>Render a node as a single continuous MP3 audiobook and write it to the configured export directory. Local file rendering only — no KDP/Audible API integration.</summary>
    [McpServerTool, Description("Render the whole node as one continuous narration (no per-beat voice drift) and write the MP3 to the configured export directory (defaults to Desktop). TTS engine: 'elevenlabs' (default, paid, highest fidelity), 'piper' (free/local, fastest), 'kokoro' (free/local, recommended), 'chatterbox' (free/local, most expressive). Returns the path of the written file, or null if the node has no beat text. This only generates a local MP3 — it does not publish anything to Audible/ACX.")]
    public async Task<string> ExportAudiobook(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("TTS engine: elevenlabs (default) | piper | kokoro | chatterbox.")] string ttsEngine = "",
        [Description("Set to true to retune this node's frozen voice snapshot to Robust stability (1.0) before recording.")] bool robust = false)
    {
        var node = await ResolveNodeAsync(nodeIdOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);

        var path = await workbench.ExportAudiobookAsync(node.Id, robust, string.IsNullOrWhiteSpace(ttsEngine) ? null : ttsEngine);
        if (path == null) return JsonSerializer.Serialize(new { ok = false, error = "no_beat_text" }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(new { ok = true, path }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List nodes with their latest review score, word count, and estimated page count (250 words/page). Optionally filter by kind ('book', 'chapter', 'episode', etc.) and/or status ('draft', 'canon', 'ready', 'archived'). Returns code, title, kind, status, score (null if unreviewed), words, pages, scored_on. Sorted by score descending (unscored nodes last). Use this for a quick quality dashboard without running new reviews.")]
    public async Task<string> ListScores(
        [Description("Optional kind filter (case-insensitive). E.g. 'book', 'chapter', 'novella'. Empty = all kinds.")] string kind = "",
        [Description("Optional status filter (case-insensitive). E.g. 'draft', 'canon', 'ready'. Empty = all statuses except archived.")] string status = "",
        [Description("Include archived nodes. Default false.")] bool includeArchived = false,
        [Description("Maximum rows to return. Default 200.")] int limit = 200)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var q = db.Nodes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(kind))   q = q.Where(s => s.Kind == kind);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(s => s.Status == status);
        else if (!includeArchived)               q = q.Where(s => s.Status != "archived");

        var nodes = await q.OrderBy(s => s.Kind).ThenBy(s => s.Title).Take(limit).ToListAsync();
        var ids = nodes.Select(s => s.Id).ToList();

        // Latest review score per node (from NodeReviewSummaries — the authoritative aggregate)
        var scores = await db.NodeReviewSummaries
            .AsNoTracking()
            .Where(r => ids.Contains(r.NodeId))
            .GroupBy(r => r.NodeId)
            .Select(g => new
            {
                NodeId  = g.Key,
                Score     = g.OrderByDescending(r => r.GeneratedAt).Select(r => (double?)r.AvgScore).First(),
                ScoredAt  = g.OrderByDescending(r => r.GeneratedAt).Select(r => (DateTime?)r.GeneratedAt).First(),
                Reviews   = g.OrderByDescending(r => r.GeneratedAt).Select(r => (int?)r.ReviewCount).First(),
            })
            .ToDictionaryAsync(x => x.NodeId);

        // Word counts from beats
        var wordCounts = await db.BeatNodes
            .AsNoTracking()
            .Where(sb => ids.Contains(sb.NodeId) && sb.IsEnabled)
            .Join(db.Beats.AsNoTracking().Where(b => b.Text != null && b.Text != ""),
                  sb => sb.BeatId, b => b.Id, (sb, b) => new { sb.NodeId, b.Text })
            .GroupBy(x => x.NodeId)
            .Select(g => new { NodeId = g.Key, Chars = g.Sum(x => (long)x.Text!.Length) })
            .ToDictionaryAsync(x => x.NodeId);

        var rows = nodes.Select(s =>
        {
            scores.TryGetValue(s.Id, out var sc);
            wordCounts.TryGetValue(s.Id, out var wc);
            // Rough word count from char count (avg English word ≈ 5 chars + 1 space)
            var words = wc != null ? (int)(wc.Chars / 5.2) : 0;
            return new
            {
                id        = s.Id,
                code      = s.NodeCode,
                slug      = s.Slug,
                title     = s.Title,
                kind      = s.Kind,
                status    = s.Status,
                score     = sc?.Score.HasValue == true ? (double?)Math.Round(sc.Score.Value, 1) : null,
                scored_on = sc?.ScoredAt.HasValue == true ? sc.ScoredAt.Value.ToString("yyyy-MM-dd") : null,
                review_count = sc?.Reviews,
                words,
                pages     = words / 250,
            };
        })
        .OrderBy(r => r.score == null ? 1 : 0)
        .ThenByDescending(r => r.score ?? 0)
        .ToList();

        return JsonSerializer.Serialize(new { count = rows.Count, nodes = rows }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Update a node's metadata fields. Pass only the fields you want to change — omit the rest to leave them unchanged. Editable fields: title, description, kind, status, seed, code (NodeCode), voice_id, kdp_page_count, cover_prompt. Status valid values: draft | ready | canon | archived. Code is uppercased and must be unique across non-null values — pass empty string to clear it. Does NOT touch beats or audio.")]
    public async Task<string> UpdateBook(
        [Description("Node id (GUID) or slug.")] string idOrSlug,
        [Description("New title. Omit to leave unchanged.")] string? title = null,
        [Description("Subtitle (e.g. 'A GLMZ Novella'). Omit to leave unchanged; pass empty string to clear.")] string? subtitle = null,
        [Description("Back-of-book description. Omit to leave unchanged; pass empty string to clear.")] string? description = null,
        [Description("Kind label: book | chapter | episode | novella | novel | node | scene | saga | anthology. Omit to leave unchanged.")] string? kind = null,
        [Description("Status: draft | ready | canon | archived. Omit to leave unchanged.")] string? status = null,
        [Description("Generation seed (one-line premise). Omit to leave unchanged; pass empty string to clear.")] string? seed = null,
        [Description("Short author reference code (e.g. 'ATTE'). Uppercased; pass empty string to clear. Omit to leave unchanged.")] string? code = null,
        [Description("ElevenLabs or local TTS voice id. Omit to leave unchanged; pass empty string to clear.")] string? voiceId = null,
        [Description("KDP print-page count from Word (File → Info → Properties → Pages). Used to calculate the correct inside margin on the next export. Pass 0 to clear.")] int? kdpPageCount = null,
        [Description("Hand-set cover art image prompt (overrides the generated one). Omit to leave unchanged; pass empty string to clear. Prefer generate_cover_prompt to derive this from the book itself.")] string? coverPrompt = null)
    {
        try
        {
            var node = await ResolveNodeAsync(idOrSlug);
            if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

            await using var db = await dbFactory.CreateDbContextAsync();
            var row = await db.Nodes.FindAsync(node.Id);
            if (row == null) return JsonSerializer.Serialize(new { error = "node_row_missing", id = node.Id }, CanonTools.JsonOpts);

            if (title        != null) row.Title        = title;
            if (subtitle     != null) row.Subtitle     = string.IsNullOrEmpty(subtitle) ? null : subtitle;
            if (description  != null) row.Description  = string.IsNullOrEmpty(description) ? null : description;
            if (kind         != null) row.Kind         = kind;
            if (status       != null) row.Status       = status;
            if (seed         != null) row.Seed         = string.IsNullOrEmpty(seed) ? null : seed;
            if (code         != null) row.NodeCode     = string.IsNullOrEmpty(code) ? null : code.Trim().ToUpperInvariant();
            if (voiceId      != null) row.VoiceId      = string.IsNullOrEmpty(voiceId) ? null : voiceId;
            if (kdpPageCount != null) row.KdpPageCount = kdpPageCount == 0 ? null : kdpPageCount;
            if (coverPrompt  != null)
            {
                row.CoverPrompt            = string.IsNullOrEmpty(coverPrompt) ? null : coverPrompt;
                row.CoverPromptGeneratedAt = DateTime.UtcNow;
            }
            row.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return JsonSerializer.Serialize(new
            {
                ok     = true,
                id     = row.Id,
                slug   = row.Slug,
                title  = row.Title,
                kind   = row.Kind,
                status = row.Status,
                code   = row.NodeCode,
            }, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "update_failed", message = ex.Message, idOrSlug }, CanonTools.JsonOpts);
        }
    }

    [McpServerTool, Description("Generate and save a book-cover image prompt (Node.CoverPrompt) from the book's own Title/Summary/Description and universe — a single paragraph describing subject, setting, mood, palette, and composition for an image model. Kept commercial-cover-safe (never explicit) regardless of interior content. Overwrites any existing CoverPrompt. Accepts node id (GUID) or slug.")]
    public async Task<string> GenerateCoverPrompt(
        [Description("Node id (GUID) or slug.")] string idOrSlug)
    {
        try
        {
            var node = await ResolveNodeAsync(idOrSlug);
            if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

            var prompt = await coverPrompts.GenerateAndSaveAsync(node.Id);
            return JsonSerializer.Serialize(new { ok = true, id = node.Id, slug = node.Slug, coverPrompt = prompt }, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "generate_cover_prompt_failed", message = ex.Message, idOrSlug }, CanonTools.JsonOpts);
        }
    }

    [McpServerTool, Description("Render and save a book cover image (png/jpg) via a chosen image provider, using Node.CoverPrompt as the prompt (generating one first via generate_cover_prompt if it's not set yet). Requires that provider's API key to be configured in Settings — costs real money per call. Saves to the media dir under covers/{slug}.{ext} and records the path/provider on the node. Accepts node id (GUID) or slug.")]
    public async Task<string> GenerateCoverImage(
        [Description("Node id (GUID) or slug.")] string idOrSlug,
        [Description("Image provider: \"openai\" (gpt-image-1), \"stability\" (Stable Image SD3.5), or \"google\" (Imagen via Gemini API).")] string provider)
    {
        try
        {
            var node = await ResolveNodeAsync(idOrSlug);
            if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

            var relativePath = await coverImages.GenerateAndSaveAsync(node.Id, provider);
            return JsonSerializer.Serialize(new { ok = true, id = node.Id, slug = node.Slug, provider, coverImagePath = relativePath }, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "generate_cover_image_failed", message = ex.Message, idOrSlug, provider }, CanonTools.JsonOpts);
        }
    }

    [McpServerTool, Description("Redraw the book title onto an already-saved cover image file in place, without calling an image-generation API again. Useful after tweaking the compositor or for a cover saved before title-compositing existed. Requires Node.CoverImagePath to already be set (run generate_cover_image first). Accepts node id (GUID) or slug.")]
    public async Task<string> CompositeCoverTitle(
        [Description("Node id (GUID) or slug.")] string idOrSlug)
    {
        try
        {
            var node = await ResolveNodeAsync(idOrSlug);
            if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);
            if (string.IsNullOrWhiteSpace(node.CoverImagePath))
                return JsonSerializer.Serialize(new { error = "no_cover_image_yet", idOrSlug }, CanonTools.JsonOpts);

            var fullPath  = Path.Combine(paths.MediaDir, node.CoverImagePath);
            var extension = Path.GetExtension(fullPath).TrimStart('.');
            var bytes     = await File.ReadAllBytesAsync(fullPath);
            var composited = await titleCompositor.CompositeTitleAsync(bytes, node.Title, extension);
            await File.WriteAllBytesAsync(fullPath, composited);

            return JsonSerializer.Serialize(new { ok = true, id = node.Id, slug = node.Slug, coverImagePath = node.CoverImagePath }, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "composite_cover_title_failed", message = ex.Message, idOrSlug }, CanonTools.JsonOpts);
        }
    }

    [McpServerTool, Description("Return the current status of the cover pipeline: for each registered image provider, its id and whether an API key is configured. Use before calling generate_cover_image to know which providers are actually usable.")]
    public string GetCoverProviderStatus()
    {
        var rows = coverImages.AvailableProviders.Select(p => new { id = p.Id, configured = p.Configured });
        return JsonSerializer.Serialize(new { providers = rows }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Return the score history for a node as a time-series — every review run that produced a summary, with its mean score, SD, review count, and date. Use to track whether an edit moved the needle, or to compare pre/post-edit trajectories. Accepts node id (GUID) or slug.")]
    public async Task<string> GetScoreHistory(
        [Description("Node id (GUID) or slug.")] string idOrSlug,
        [Description("Maximum history points to return (most recent first). Default 20.")] int limit = 20)
    {
        var node = await ResolveNodeAsync(idOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

        await using var db = await dbFactory.CreateDbContextAsync();
        var history = await db.NodeScoreHistories
            .AsNoTracking()
            .Where(h => h.NodeId == node.Id)
            .OrderByDescending(h => h.RecordedAt)
            .Take(limit)
            .Select(h => new
            {
                recorded_at   = h.RecordedAt,
                score         = Math.Round(h.MeanScore, 2),
                sd            = h.Sd.HasValue ? (double?)Math.Round(h.Sd.Value, 2) : null,
                review_count  = h.ReviewCount,
                beat_count    = h.BeatCount,
                content_hash  = h.ContentHash,
            })
            .ToListAsync();

        // Also include NodeReviewSummaries for runs pre-dating NodeScoreHistories
        var srsHistory = await db.NodeReviewSummaries
            .AsNoTracking()
            .Where(r => r.NodeId == node.Id)
            .OrderByDescending(r => r.GeneratedAt)
            .Take(limit)
            .Select(r => new
            {
                recorded_at   = r.GeneratedAt,
                score         = Math.Round(r.AvgScore, 2),
                sd            = (double?)null,
                review_count  = r.ReviewCount,
                beat_count    = 0,
                content_hash  = r.ContentHash ?? "",
            })
            .ToListAsync();

        // Merge and deduplicate by content_hash (prefer SSH when present)
        var sshHashes = new HashSet<string>(history.Select(h => h.content_hash));
        var merged = history
            .Cast<object>()
            .Concat(srsHistory.Where(r => !sshHashes.Contains(r.content_hash ?? "")).Cast<object>())
            .Take(limit)
            .ToList();

        return JsonSerializer.Serialize(new
        {
            node_id    = node.Id,
            slug         = node.Slug,
            title        = node.Title,
            point_count  = merged.Count,
            history      = merged,
        }, CanonTools.JsonOpts);
    }

    // ── Node Spine tools ─────────────────────────────────────────────────

    [McpServerTool, Description(
        "Return the full narrative spine for a node: bible, user stories, all amendments (in order), " +
        "and the latest spine version pin (which records the content hashes and amendment count at the " +
        "last docx export). Use this before writing prose to understand the narrative contract.")]
    public async Task<string> GetBookSpine(
        [Description("Node id (GUID) or slug.")] string idOrSlug)
    {
        var node = await ResolveNodeAsync(idOrSlug);
        if (node == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

        var dto = await spine.GetSpineAsync(node.Id);
        if (dto == null)
            return JsonSerializer.Serialize(new { error = "spine_not_found" }, CanonTools.JsonOpts);

        return JsonSerializer.Serialize(new
        {
            node_id           = dto.NodeId,
            bible               = dto.Bible,
            bible_updated_at    = dto.BibleUpdatedAt?.ToString("u"),
            user_stories        = dto.UserStories,
            user_stories_updated_at = dto.UserStoriesUpdatedAt?.ToString("u"),
            amendments          = dto.Amendments.Select(a => new
            {
                code       = a.Code,
                seq        = a.SequenceNo,
                summary    = a.Summary,
                body       = a.Body,
                created_at = a.CreatedAt.ToString("u"),
                created_by = a.CreatedBy,
            }).ToList(),
            latest_pin = dto.LatestPin == null ? null : new
            {
                node_version    = dto.LatestPin.NodeVersion,
                bible_hash        = dto.LatestPin.BibleHash[..Math.Min(12, dto.LatestPin.BibleHash.Length)] + "…",
                user_stories_hash = dto.LatestPin.UserStoriesHash[..Math.Min(12, dto.LatestPin.UserStoriesHash.Length)] + "…",
                amendment_count   = dto.LatestPin.AmendmentCount,
                pinned_at         = dto.LatestPin.PinnedAt.ToString("u"),
                notes             = dto.LatestPin.Notes,
            },
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Set (replace) the user stories / acceptance criteria for a node. " +
        "Write this before starting prose — it defines what scenes, arcs, and voice moments must be present " +
        "for the node to reach ≥82% standalone and ≥85% cumulative book score.")]
    public async Task<string> SetBookUserStories(
        [Description("Node id (GUID) or slug.")] string idOrSlug,
        [Description("Full user stories markdown. Will replace any existing content.")] string userStoriesText)
    {
        var node = await ResolveNodeAsync(idOrSlug);
        if (node == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

        await spine.SetUserStoriesAsync(node.Id, userStoriesText, "mcp");
        return JsonSerializer.Serialize(new { ok = true, node_id = node.Id, slug = node.Slug }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Append an amendment to the node's narrative spine. " +
        "Amendments are append-only — they form an auditable change log of narrative decisions. " +
        "Use when: changing a character's motivation after beats are written, retconning world rules, " +
        "or noting why a section was expanded or cut.")]
    public async Task<string> AppendBookAmendment(
        [Description("Node id (GUID) or slug.")] string idOrSlug,
        [Description("One-line summary of the change.")] string summary,
        [Description("Full amendment body (markdown). Explain what changed and why.")] string body)
    {
        var node = await ResolveNodeAsync(idOrSlug);
        if (node == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

        var amendment = await spine.AppendAmendmentAsync(node.Id, summary, body, "mcp");
        return JsonSerializer.Serialize(new
        {
            ok         = true,
            code       = amendment.Code,
            seq        = amendment.SequenceNo,
            node_id  = node.Id,
            slug       = node.Slug,
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Create a spine version pin for the node's current docx version. " +
        "Records the SHA-256 hashes of the current bible and user stories, plus the amendment count, " +
        "so future drift checks can tell when prose was written against a stale spine. " +
        "Call this after every significant prose session or whenever the spine changes.")]
    public async Task<string> PinBookSpineVersion(
        [Description("Node id (GUID) or slug.")] string idOrSlug,
        [Description("Optional human note explaining what changed at this version.")] string notes = "")
    {
        var node = await ResolveNodeAsync(idOrSlug);
        if (node == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

        var (drifted, reason) = await spine.CheckDriftAsync(node.Id);
        var pin = await spine.PinVersionAsync(node.Id, notes, "mcp");

        return JsonSerializer.Serialize(new
        {
            ok              = true,
            node_version  = pin.NodeVersion,
            bible_hash      = pin.BibleHash[..Math.Min(12, pin.BibleHash.Length)] + "…",
            user_stories_hash = pin.UserStoriesHash[..Math.Min(12, pin.UserStoriesHash.Length)] + "…",
            amendment_count = pin.AmendmentCount,
            prior_drift     = drifted,
            prior_drift_reason = reason,
            pinned_at       = pin.PinnedAt.ToString("u"),
        }, CanonTools.JsonOpts);
    }

    private static string? KindCompatibilityError(string parentKind, string childKind) => (parentKind, childKind) switch
    {
        ("series", "book")    => null,
        ("book",   "chapter") => null,
        ("book",   "book")    => "A book cannot contain another book — only a series can.",
        ("series", "chapter") => "A chapter must be under a book, not directly under a series.",
        ("chapter", _)        => "A chapter cannot contain other nodes (it holds beats).",
        _                     => $"A '{childKind}' cannot be placed under a '{parentKind}'.",
    };

    /// <summary>Build an Audible AI-narration hand-off package for a node.</summary>
    [McpServerTool, Description(
        "Build an Audible AI-narration hand-off package for a node. Produces three files in " +
        "{publishDir}/{Title}/Audible/: (1) a narration-clean manuscript (.audible.txt) " +
        "with markdown artifacts stripped and Φ expanded to 'QUANTA'; " +
        "(2) a pronunciation guide (.pronunciation.md) listing entity names with plain-English " +
        "respellings; (3) AUDIBLE_README.md with submission instructions. " +
        "No API is called on Audible's side — the author uploads the .audible.txt via ACX/Audible " +
        "publisher portal. Returns paths + word/term counts.")]
    public async Task<string> PrepareAudible(
        [Description("Node id (GUID) or slug.")] string nodeIdOrSlug,
        [Description("Run the optional LLM phonetics pass to fill in 'Say it as' respellings. Default true. Set false to skip and leave the column blank for manual completion.")] bool withPhonetics = true)
    {
        var node = await ResolveNodeAsync(nodeIdOrSlug);
        if (node == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);

        try
        {
            var result = await audible.BuildAsync(node.Id, withPhonetics);
            return JsonSerializer.Serialize(new
            {
                ok               = true,
                manuscript_path  = result.ManuscriptPath,
                lexicon_path     = result.LexiconPath,
                readme_path      = result.ReadmePath,
                word_count       = result.WordCount,
                term_count       = result.TermCount,
                phonetics_applied = result.PhoneticsApplied,
            }, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "build_failed", message = ex.Message }, CanonTools.JsonOpts);
        }
    }

    [McpServerTool, Description("Print all beats of a node as continuous prose — each beat's Text joined by a blank line. No headers, no beat numbers, no metadata. Accepts node id (GUID) or slug. Use this to read the full prose of a node in one call.")]
    public async Task<string> PrintBook(
        [Description("Node Guid id or slug.")] string idOrSlug)
    {
        var node = await ResolveNodeAsync(idOrSlug);
        if (node == null) return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

        await using var db = await dbFactory.CreateDbContextAsync();

        var childIds = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == node.Id).Select(n => n.Id).ToListAsync();
        var searchIds = childIds.Count > 0 ? childIds : new List<Guid> { node.Id };

        var texts = await db.BeatNodes
            .AsNoTracking()
            .Where(sb => searchIds.Contains(sb.NodeId) && sb.IsEnabled)
            .OrderBy(sb => sb.SortKey)
            .Join(db.Beats.AsNoTracking(),
                  sb => sb.BeatId,
                  b  => b.Id,
                  (sb, b) => b.Text)
            .ToListAsync();

        var prose = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (prose.Count == 0) return JsonSerializer.Serialize(new { error = "no_prose", node_id = node.Id, slug = node.Slug }, CanonTools.JsonOpts);

        return string.Join("\n\n", prose);
    }

    private async Task<Node?> ResolveNodeAsync(string idOrSlug)
    {
        if (string.IsNullOrWhiteSpace(idOrSlug)) return null;
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(idOrSlug, out var guid))
        {
            var byId = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == guid);
            if (byId != null) return byId;
        }
        return await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == idOrSlug || s.NodeCode == idOrSlug);
    }
}
