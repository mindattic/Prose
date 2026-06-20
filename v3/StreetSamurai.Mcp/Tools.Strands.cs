using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Strand tools — unified beat/strand schema ──────────────────────────────
// Mirrors the StrandWorkbenchService surface, exposed to chat-side callers.
// Every mutating operation goes through the workbench so hash invalidation,
// audio cleanup, and fractional-SortKey insertion stay identical between
// the UI and the MCP surface.
//
// All ids are GUID strings. Slugs are also accepted where the parameter
// description says so — the tool resolves slug→id with a single index seek.

[McpServerToolType]
public class StrandTools
{
    private readonly StrandWorkbenchService workbench;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ElevenLabsTtsService tts;
    private readonly StrandBibleService bible;
    private readonly ProseReflowService reflow;
    private readonly BeatRebuildService rebuilder;
    private readonly DocxExportService docxExport;

    public StrandTools(
        StrandWorkbenchService workbench,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ElevenLabsTtsService tts,
        StrandBibleService bible,
        ProseReflowService reflow,
        BeatRebuildService rebuilder,
        DocxExportService docxExport)
    {
        this.workbench = workbench;
        this.dbFactory = dbFactory;
        this.tts = tts;
        this.bible = bible;
        this.reflow = reflow;
        this.rebuilder = rebuilder;
        this.docxExport = docxExport;
    }

    [McpServerTool, Description("List strands. Optional kind filter ('book', 'chapter', 'episode', etc.). Returns a flat list of id, slug, title, kind, status, beat-count, stale-count.")]
    public async Task<string> ListStrands(
        [Description("Optional Kind filter — case-insensitive equality match.")] string kind = "",
        [Description("Maximum rows to return. Default 100.")] int limit = 100)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.Strands.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(kind)) q = q.Where(s => s.Kind == kind);
        var rows = await q.OrderBy(s => s.Kind).ThenBy(s => s.Title).Take(limit).ToListAsync();

        var ids = rows.Select(r => r.Id).ToList();
        var beatCounts = await db.StrandBeats
            .Where(sb => ids.Contains(sb.StrandId))
            .GroupBy(sb => sb.StrandId)
            .Select(g => new { StrandId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StrandId, x => x.Count);

        var result = rows.Select(s => new
        {
            id = s.Id,
            slug = s.Slug,
            title = s.Title,
            kind = s.Kind,
            status = s.Status,
            beats = beatCounts.GetValueOrDefault(s.Id, 0),
            has_bible = s.StrandBible != null,
            parent_strand_id = s.ParentStrandId,
        });
        return JsonSerializer.Serialize(result, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Get a single strand with its beats in reading order. Accepts a Guid id OR a slug. Returns strand metadata + ordered beats (id, text, stale, has_audio, beat_title, synopsis).")]
    public async Task<string> GetStrand(
        [Description("Strand Guid id or slug.")] string idOrSlug)
    {
        var strand = await ResolveStrandAsync(idOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", idOrSlug }, CanonTools.JsonOpts);
        var beats = await workbench.GetOrderedBeatsAsync(strand.Id);
        return JsonSerializer.Serialize(new
        {
            id = strand.Id, slug = strand.Slug, title = strand.Title, kind = strand.Kind,
            status = strand.Status, synopsis = strand.Synopsis, seed = strand.Seed,
            voice_id = strand.VoiceId,
            parent_strand_id = strand.ParentStrandId, chars_narrated = strand.CharsNarrated,
            has_bible = strand.StrandBible != null,
            strand_bible_generated_at = strand.StrandBibleGeneratedAt,
            beats = beats.Select((b, i) => new
            {
                position = i + 1,
                id = b.Beat.Id,
                text = b.Beat.Text,
                stale = b.Beat.Stale,
                has_audio = !string.IsNullOrEmpty(b.Beat.AudioPath),
                duration_sec = b.Beat.DurationSec,
                beat_title = b.Beat.BeatTitle,
                synopsis = b.Beat.Synopsis,
            }),
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Create a new top-level strand. Pass 'seed' to also generate a strand bible and planned beats immediately. Returns the new strand's id, slug, url, and (if bible was generated) the bible text.")]
    public async Task<string> CreateStrand(
        [Description("Strand title. Required.")] string title,
        [Description("Free-form kind label: 'book', 'chapter', 'episode', 'scene', 'saga', 'anthology', or anything you want. Default 'strand'.")] string kind = "strand",
        [Description("Optional synopsis.")] string synopsis = "",
        [Description("One-line generation seed. When provided, the strand bible and planned beats are created immediately after the strand row is inserted.")] string seed = "",
        [Description("Target beat count for the bible spine (only used when seed is provided). Default 12.")] int targetBeats = 12,
        [Description("Optional parent strand Guid id (or slug). Empty = top-level.")] string parentStrandIdOrSlug = "",
        [Description("Optional short author-assigned reference code (e.g. 'ATTE', 'VATD', 'GLMZCODEX'). Uppercased and stored as a unique lookup key. Leave empty to skip.")] string code = "")
    {
        Guid? parentId = null;
        if (!string.IsNullOrWhiteSpace(parentStrandIdOrSlug))
        {
            var parent = await ResolveStrandAsync(parentStrandIdOrSlug);
            if (parent == null) return JsonSerializer.Serialize(new { error = "parent_strand_not_found", parentStrandIdOrSlug }, CanonTools.JsonOpts);
            parentId = parent.Id;
        }

        await using var db = await dbFactory.CreateDbContextAsync();
        var id = Guid.CreateVersion7();
        var baseSlug = System.Text.RegularExpressions.Regex
            .Replace((title ?? "").ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "strand";
        var slug = $"{baseSlug}-{id.ToString("N")[..8]}";

        var maxSort = parentId.HasValue
            ? await db.Strands.Where(s => s.ParentStrandId == parentId).Select(s => (double?)s.SortKey).MaxAsync() ?? 0
            : await db.Strands.Where(s => s.ParentStrandId == null).Select(s => (double?)s.SortKey).MaxAsync() ?? 0;

        db.Strands.Add(new Strand
        {
            Id = id,
            Slug = slug,
            Title = title ?? "",
            Synopsis = string.IsNullOrEmpty(synopsis) ? null : synopsis,
            Seed = string.IsNullOrEmpty(seed) ? null : seed,
            Kind = string.IsNullOrEmpty(kind) ? "strand" : kind,
            Status = "draft",
            ParentStrandId = parentId,
            SortKey = maxSort + 100.0,
            StrandCode = string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant(),
        });
        await db.SaveChangesAsync();

        // If a seed was provided, generate the strand bible and planned beats immediately.
        string? bibleText = null;
        if (!string.IsNullOrWhiteSpace(seed))
        {
            try { bibleText = await bible.GenerateAndSaveAsync(id, seed, title, targetBeats <= 0 ? 12 : targetBeats); }
            catch (Exception ex) { bibleText = $"[bible generation failed: {ex.Message}]"; }
        }

        return JsonSerializer.Serialize(new { ok = true, id, slug, url = $"/strand/{slug}", strand_bible = bibleText }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Deep-duplicate a strand (and its sub-strand tree) into a fresh, independent copy. Every beat is cloned into a new row — prose and narration metadata are preserved, but audio, review scores, and the stale flag are reset. Editing the copy never affects the original. Accepts a Guid id OR a slug. Returns the new strand's id, slug, and writer URL.")]
    public async Task<string> DuplicateStrand(
        [Description("Source strand Guid id or slug.")] string idOrSlug,
        [Description("Title for the new duplicate. Required.")] string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle))
            return JsonSerializer.Serialize(new { error = "title_required" }, CanonTools.JsonOpts);
        var source = await ResolveStrandAsync(idOrSlug);
        if (source == null) return JsonSerializer.Serialize(new { error = "strand_not_found", idOrSlug }, CanonTools.JsonOpts);

        var (id, slug) = await workbench.DuplicateStrandAsync(source.Id, newTitle);
        return JsonSerializer.Serialize(new { ok = true, id, slug, title = newTitle, url = $"/strand/{slug}", source_id = source.Id }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Insert a new beat into a strand. Pass an empty afterBeatId to insert at the top. Returns the new beat's id.")]
    public async Task<string> InsertBeat(
        [Description("Strand Guid id or slug.")] string strandIdOrSlug,
        [Description("Beat Guid id to insert after, or empty for top-of-strand.")] string afterBeatId = "",
        [Description("Initial prose text for the new beat. May be empty.")] string text = "")
    {
        var strand = await ResolveStrandAsync(strandIdOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);
        Guid? after = string.IsNullOrWhiteSpace(afterBeatId) ? null : Guid.Parse(afterBeatId);
        var beat = await workbench.InsertBeatAsync(strand.Id, after, text ?? "");
        return JsonSerializer.Serialize(new { ok = true, id = beat.Id, strand_id = strand.Id }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Get a single beat with every authoring field — prose, kind, IsChapterStart, BeatTitle, gap-after, tone/pace/facet metadata, position within strand, and the previous/next beat ids for relative insertion. Accepts a plain Beat Guid or the 'strand-guid.beat-guid' dotted handle the writer UI shows on the LLM bottom sheet.")]
    public async Task<string> GetBeat(
        [Description("Beat Guid OR the dotted 'strand-guid.beat-guid' handle.")] string beatHandle)
    {
        if (!BeatHandle.TryParse(beatHandle, out var parsedStrand, out var parsedBeat) || parsedBeat == null)
            return JsonSerializer.Serialize(new { error = "bad_beat_handle", beatHandle }, CanonTools.JsonOpts);

        await using var db = await dbFactory.CreateDbContextAsync();
        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == parsedBeat.Value);
        if (beat == null) return JsonSerializer.Serialize(new { error = "beat_not_found", beatHandle }, CanonTools.JsonOpts);

        // Resolve the strand that owns this beat — either the one from the
        // dotted handle (if any), or the first StrandBeat junction.
        var strandId = parsedStrand ?? (await db.StrandBeats.AsNoTracking()
            .Where(sb => sb.BeatId == beat.Id)
            .Select(sb => (Guid?)sb.StrandId)
            .FirstOrDefaultAsync());
        Strand? strand = null;
        int position = 0;
        Guid? prevBeatId = null;
        Guid? nextBeatId = null;
        if (strandId.HasValue)
        {
            strand = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Id == strandId.Value);
            var ordered = await workbench.GetOrderedBeatsAsync(strandId.Value);
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
            handle          = strandId.HasValue ? $"{strandId.Value}.{beat.Id}" : beat.Id.ToString(),
            number          = beat.Number,
            position,
            strand          = strand == null ? null : (object)new { id = strand.Id, slug = strand.Slug, title = strand.Title },
            prev_beat_id    = prevBeatId,
            next_beat_id    = nextBeatId,
            text            = beat.Text,
            kind            = beat.Kind,
            is_chapter_start = beat.IsChapterStart,
            beat_title      = beat.BeatTitle,
            synopsis        = beat.Synopsis,
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

    [McpServerTool, Description("Update one beat's prose. Recomputes the hash, marks the beat stale, and invalidates its audio. Beat.Text accepts inline markdown (**bold** / *italic* / __underline__ / ~~strike~~) and ElevenLabs-style tone tags ([WHISPERING] [GASP] [LAUGHS] [PAUSES] etc.) that render as emoji in the read view. Accepts a Beat Guid OR the 'strand-guid.beat-guid' handle.")]
    public async Task<string> UpdateBeatText(
        [Description("Beat Guid OR 'strand-guid.beat-guid' handle.")] string beatHandle,
        [Description("New prose. Replaces the entire beat text. Markdown markers + tone-tag brackets are preserved verbatim in storage.")] string text)
    {
        if (!BeatHandle.TryParse(beatHandle, out _, out var bid) || bid == null)
            return JsonSerializer.Serialize(new { error = "bad_beat_handle", beatHandle }, CanonTools.JsonOpts);
        await workbench.UpdateBeatTextAsync(bid.Value, text ?? "");
        return JsonSerializer.Serialize(new { ok = true, id = bid.Value }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Update a beat's metadata: BeatTitle, Synopsis, EmotionalTone, PaceHint, StructureRole, Act, SceneType, IsChapterStart, Kind. Pass empty strings to clear nullable fields. Does NOT touch prose or audio. Use to mark a beat as a chapter start, change its kind to quote/dedication/book-title, or set the tone the next re-record uses.")]
    public async Task<string> UpdateBeatMetadata(
        [Description("Beat Guid OR 'strand-guid.beat-guid' handle.")] string beatHandle,
        [Description("Short label. When IsChapterStart=true this is the chapter heading; when Kind=quote this is the attribution.")] string beatTitle = "",
        [Description("One-line synopsis fed to LLM regenerations.")] string synopsis = "",
        [Description("Emotional tone, e.g. 'quiet' / 'tense' / 'wry'.")] string emotionalTone = "",
        [Description("Pace hint, e.g. 'flowing' / 'clipped' / 'staccato' / 'languorous'.")] string paceHint = "",
        [Description("Structure role, e.g. 'inciting-incident' / 'rising-action' / 'climax'.")] string structureRole = "",
        [Description("Plot-act number 0–5. 0 = unassigned.")] int act = 0,
        [Description("Scene type: scene | summary | transition | interstitial.")] string sceneType = "scene",
        [Description("True = this beat begins a new chapter / section. The writer renders a divider above it with BeatTitle as the heading.")] bool isChapterStart = false,
        [Description("Beat kind: prose (default) | book-title | dedication | quote. Free-form so new kinds add no schema cost.")] string kind = "prose")
    {
        if (!BeatHandle.TryParse(beatHandle, out _, out var bid) || bid == null)
            return JsonSerializer.Serialize(new { error = "bad_beat_handle", beatHandle }, CanonTools.JsonOpts);
        await workbench.UpdateBeatMetadataAsync(bid.Value, new StrandWorkbenchService.BeatMetadataUpdate(
            BeatTitle:      beatTitle,
            Synopsis:       synopsis,
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
        [Description("Beat Guid OR 'strand-guid.beat-guid' handle.")] string beatHandle,
        [Description("Silence in milliseconds, 0..6000.")] int durationMs)
    {
        if (!BeatHandle.TryParse(beatHandle, out _, out var bid) || bid == null)
            return JsonSerializer.Serialize(new { error = "bad_beat_handle", beatHandle }, CanonTools.JsonOpts);
        await workbench.SetGapAfterAsync(bid.Value, durationMs);
        return JsonSerializer.Serialize(new { ok = true, id = bid.Value, gap_after_ms = Math.Max(0, durationMs) }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Clear an explicit gap-after-beat override. The audio engine falls back to the auto-computed silence from SceneType + terminator punctuation.")]
    public async Task<string> ClearBeatGapAfter(
        [Description("Beat Guid OR 'strand-guid.beat-guid' handle.")] string beatHandle)
    {
        if (!BeatHandle.TryParse(beatHandle, out _, out var bid) || bid == null)
            return JsonSerializer.Serialize(new { error = "bad_beat_handle", beatHandle }, CanonTools.JsonOpts);
        await workbench.ClearGapAfterAsync(bid.Value);
        return JsonSerializer.Serialize(new { ok = true, id = bid.Value }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Split one beat into two at the nearest sentence boundary near its midpoint. Both halves lose their audio.")]
    public async Task<string> SplitBeat(
        [Description("Strand Guid id or slug.")] string strandIdOrSlug,
        [Description("Beat Guid id to split.")] string beatId)
    {
        var strand = await ResolveStrandAsync(strandIdOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);
        if (!Guid.TryParse(beatId, out var bid)) return JsonSerializer.Serialize(new { error = "bad_beat_id", beatId }, CanonTools.JsonOpts);
        var newBeat = await workbench.SplitBeatAsync(strand.Id, bid);
        return JsonSerializer.Serialize(new { ok = true, original = bid, new_beat = newBeat.Id }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Merge one beat into the previous one in the strand. Audio on the survivor is invalidated.")]
    public async Task<string> JoinBeat(
        [Description("Strand Guid id or slug.")] string strandIdOrSlug,
        [Description("Beat Guid id to merge upward.")] string beatId)
    {
        var strand = await ResolveStrandAsync(strandIdOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);
        if (!Guid.TryParse(beatId, out var bid)) return JsonSerializer.Serialize(new { error = "bad_beat_id", beatId }, CanonTools.JsonOpts);
        await workbench.JoinBeatWithPreviousAsync(strand.Id, bid);
        return JsonSerializer.Serialize(new { ok = true, id = bid }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Remove a beat from a strand. If the beat is not referenced by any other strand, the beat row + audio file are deleted entirely.")]
    public async Task<string> DeleteBeat(
        [Description("Strand Guid id or slug.")] string strandIdOrSlug,
        [Description("Beat Guid id to delete.")] string beatId)
    {
        var strand = await ResolveStrandAsync(strandIdOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);
        if (!Guid.TryParse(beatId, out var bid)) return JsonSerializer.Serialize(new { error = "bad_beat_id", beatId }, CanonTools.JsonOpts);
        await workbench.DeleteBeatAsync(strand.Id, bid);
        return JsonSerializer.Serialize(new { ok = true, id = bid }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Kick off TTS narration for every un-narrated beat in this strand (and its child strands recursively). Returns immediately — narration runs in the background; poll get_strand to observe progress. Returns an error response (without spawning anything) if TTS is not configured.")]
    public async Task<string> NarrateStrand(
        [Description("Strand Guid id or slug.")] string strandIdOrSlug)
    {
        var strand = await ResolveStrandAsync(strandIdOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);
        // Pre-flight: without this check, an unconfigured TTS account causes
        // NarrateAsync to throw InvalidOperationException into the
        // unobserved-task void; the MCP caller saw {ok:true} and nothing
        // ever happened. Return the typed error here instead.
        if (!await tts.IsConfiguredAsync())
            return JsonSerializer.Serialize(new { error = "tts_not_configured", message = "ElevenLabs API key is missing. Set it in Settings before calling narrate_strand." }, CanonTools.JsonOpts);
        _ = Task.Run(async () =>
        {
            try { await workbench.NarrateAsync(strand.Id); }
            catch (Exception ex) { Console.Error.WriteLine($"[mcp:narrate_strand] {strand.Id}: {ex.Message}"); }
        });
        return JsonSerializer.Serialize(new { ok = true, id = strand.Id, status = "narrating" }, CanonTools.JsonOpts);
    }

    // ── Strand Bible tools ────────────────────────────────────────────────

    [McpServerTool, Description("Get the strand bible for a strand — the dry structural plan (logline, premise, register, characters, beat spine, seeds & payoffs). Returns the raw markdown text plus the parsed beat spine entries so you can see the planned arc at a glance. Returns has_bible=false when no bible exists yet.")]
    public async Task<string> GetStrandBible(
        [Description("Strand Guid id or slug.")] string idOrSlug)
    {
        var strand = await ResolveStrandAsync(idOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", idOrSlug }, CanonTools.JsonOpts);

        if (string.IsNullOrEmpty(strand.StrandBible))
            return JsonSerializer.Serialize(new { has_bible = false, id = strand.Id, slug = strand.Slug, title = strand.Title }, CanonTools.JsonOpts);

        var spine = StrandBibleService.ParseBeatSpine(strand.StrandBible)
            .Select(p => new { index = p.Index, title = p.Title, goal = p.Goal, structure_role = p.StructureRole });

        return JsonSerializer.Serialize(new
        {
            has_bible = true,
            id = strand.Id,
            slug = strand.Slug,
            title = strand.Title,
            generated_at = strand.StrandBibleGeneratedAt,
            bible = strand.StrandBible,
            beat_spine = spine,
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Generate (or regenerate) the strand bible for a strand. Uses the strand's Seed field (falls back to Synopsis then Title) plus the literary rules to produce a dry structural plan: logline, premise, register, characters, numbered beat spine, seeds & payoffs. Creates planned Beat rows from the spine when the strand has no beats yet. Returns the generated bible text.")]
    public async Task<string> GenerateStrandBible(
        [Description("Strand Guid id or slug.")] string idOrSlug,
        [Description("Target number of beats in the spine. 0 = auto (use existing beat count or 12).")] int targetBeats = 0)
    {
        var strand = await ResolveStrandAsync(idOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", idOrSlug }, CanonTools.JsonOpts);

        var seed = strand.Seed ?? strand.Synopsis ?? strand.Title;
        if (string.IsNullOrWhiteSpace(seed))
            return JsonSerializer.Serialize(new { error = "no_seed", message = "Strand has no Seed or Synopsis to drive generation. Set one first with SetStrandBible or UpdateBeatMetadata." }, CanonTools.JsonOpts);

        if (targetBeats <= 0)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            targetBeats = await db.StrandBeats.CountAsync(sb => sb.StrandId == strand.Id && sb.IsEnabled);
            if (targetBeats <= 0) targetBeats = 12;
        }

        string bibleText;
        try { bibleText = await bible.GenerateAndSaveAsync(strand.Id, seed, strand.Title, targetBeats); }
        catch (Exception ex) { return JsonSerializer.Serialize(new { error = "generation_failed", message = ex.Message }, CanonTools.JsonOpts); }

        var spine = StrandBibleService.ParseBeatSpine(bibleText)
            .Select(p => new { index = p.Index, title = p.Title, goal = p.Goal, structure_role = p.StructureRole });

        return JsonSerializer.Serialize(new { ok = true, id = strand.Id, slug = strand.Slug, bible = bibleText, beat_spine = spine }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Manually set or replace the strand bible text. Use when you want to hand-write the plan instead of generating it. The text is saved verbatim; beat spine parsing still applies for planned-beat creation. Pass an empty string to clear the bible.")]
    public async Task<string> SetStrandBible(
        [Description("Strand Guid id or slug.")] string idOrSlug,
        [Description("Full bible markdown text to store. Empty string clears the bible.")] string bibleText)
    {
        var strand = await ResolveStrandAsync(idOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", idOrSlug }, CanonTools.JsonOpts);

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.Strands.FindAsync(strand.Id)
            ?? throw new InvalidOperationException($"Strand {strand.Id} missing.");

        row.StrandBible = string.IsNullOrEmpty(bibleText) ? null : bibleText;
        row.StrandBibleGeneratedAt = DateTime.UtcNow;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return JsonSerializer.Serialize(new { ok = true, id = strand.Id, slug = strand.Slug, cleared = string.IsNullOrEmpty(bibleText) }, CanonTools.JsonOpts);
    }

    /// <summary>Copy-edit a strand's prose in-place: proper paragraph/dialogue spacing, "?" on questions, "asks"/"asked" on question dialogue. Dry-run by default — pass apply=true to commit. Returns a report of what changed, was rejected, or errored.</summary>
    [McpServerTool, Description("Copy-edit a strand's prose in-place: adds missing '?' on questions, swaps 'says/said' → 'asks/asked' on question dialogue lines, and normalises paragraph/dialogue spacing. Dry-run by default — set apply=true to commit. Beats the model modified beyond those specific edits are rejected and left untouched. Returns changed/unchanged/rejected/errors counts plus per-beat diff previews.")]
    public async Task<string> ReflowStrand(
        [Description("Strand id (GUID) or slug.")] string strandIdOrSlug,
        [Description("Set to true to write the edits to the DB. Default false = dry run.")] bool apply = false)
    {
        var strand = await ResolveStrandAsync(strandIdOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);

        var report = await reflow.ReflowStrandAsync(strand.Id, apply);
        return JsonSerializer.Serialize(new
        {
            strand_id    = report.StrandId,
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

    /// <summary>LLM-rebeat a strand: re-segment all beats to the beat doctrine (proper formatting, no run-ons, no sentence-shrapnel). Dry-run by default; set apply=true to export a backup then replace beats (only if the word-retention guard passes).</summary>
    [McpServerTool, Description("Re-segment a strand's beats to the codified beat doctrine via LLM re-segmentation. Dry-run by default (safe to call freely). Set apply=true to export a Markdown backup then replace the beats — only committed if the word-retention guard passes (prevents silent content loss). Returns old/new beat counts, retention %, guard result, and a note if it was blocked.")]
    public async Task<string> RebeatStrand(
        [Description("Strand id (GUID) or slug.")] string strandIdOrSlug,
        [Description("Set to true to commit the new segmentation. Default false = dry run.")] bool apply = false)
    {
        var strand = await ResolveStrandAsync(strandIdOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);

        var r = await rebuilder.RebuildAsync(strand.Id, apply);
        return JsonSerializer.Serialize(new
        {
            strand_id      = r.StrandId,
            slug           = r.Slug,
            title          = r.Title,
            applied        = r.Applied,
            old_beats      = r.OldBeats,
            new_beats      = r.NewBeats,
            word_retention = r.WordRetention,
            guard_passed   = r.GuardPassed,
            backup_path    = r.BackupPath,
            note           = r.Note,
        }, CanonTools.JsonOpts);
    }

    /// <summary>Export a strand as a KDP-ready Word .docx to the configured publish directory (defaults to Downloads).</summary>
    [McpServerTool, Description("Render a strand to a KDP-ready Word .docx and write it to the configured publish directory (defaults to Downloads). Returns the path of the written file. Use get_strand first to confirm the strand exists.")]
    public async Task<string> PublishDocx(
        [Description("Strand id (GUID) or slug.")] string strandIdOrSlug,
        [Description("Author name to embed in the document properties. Optional.")] string author = "")
    {
        var strand = await ResolveStrandAsync(strandIdOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);

        var path = await docxExport.ExportStrandAsync(strand.Id, string.IsNullOrWhiteSpace(author) ? null : author);
        return JsonSerializer.Serialize(new { ok = true, path }, CanonTools.JsonOpts);
    }

    /// <summary>Render a strand as a single continuous MP3 audiobook and write it to the configured publish directory.</summary>
    [McpServerTool, Description("Render the whole strand as one continuous narration (no per-beat voice drift) and write the MP3 to the configured publish directory (defaults to Downloads). TTS engine: 'elevenlabs' (default, paid, highest fidelity), 'piper' (free/local, fastest), 'kokoro' (free/local, recommended), 'chatterbox' (free/local, most expressive). Returns the path of the written file, or null if the strand has no beat text.")]
    public async Task<string> PublishAudiobook(
        [Description("Strand id (GUID) or slug.")] string strandIdOrSlug,
        [Description("TTS engine: elevenlabs (default) | piper | kokoro | chatterbox.")] string ttsEngine = "",
        [Description("Set to true to retune this strand's frozen voice snapshot to Robust stability (1.0) before recording.")] bool robust = false)
    {
        var strand = await ResolveStrandAsync(strandIdOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", strandIdOrSlug }, CanonTools.JsonOpts);

        var path = await workbench.PublishAudiobookAsync(strand.Id, robust, string.IsNullOrWhiteSpace(ttsEngine) ? null : ttsEngine);
        if (path == null) return JsonSerializer.Serialize(new { ok = false, error = "no_beat_text" }, CanonTools.JsonOpts);
        return JsonSerializer.Serialize(new { ok = true, path }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("List strands with their latest review score, word count, and estimated page count (250 words/page). Optionally filter by kind ('book', 'chapter', 'episode', etc.) and/or status ('draft', 'canon', 'ready', 'archived'). Returns code, title, kind, status, score (null if unreviewed), words, pages, scored_on. Sorted by score descending (unscored strands last). Use this for a quick quality dashboard without running new reviews.")]
    public async Task<string> ListScores(
        [Description("Optional kind filter (case-insensitive). E.g. 'book', 'chapter', 'novella'. Empty = all kinds.")] string kind = "",
        [Description("Optional status filter (case-insensitive). E.g. 'draft', 'canon', 'ready'. Empty = all statuses except archived.")] string status = "",
        [Description("Include archived strands. Default false.")] bool includeArchived = false,
        [Description("Maximum rows to return. Default 200.")] int limit = 200)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var q = db.Strands.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(kind))   q = q.Where(s => s.Kind == kind);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(s => s.Status == status);
        else if (!includeArchived)               q = q.Where(s => s.Status != "archived");

        var strands = await q.OrderBy(s => s.Kind).ThenBy(s => s.Title).Take(limit).ToListAsync();
        var ids = strands.Select(s => s.Id).ToList();

        // Latest review score per strand (from StrandReviewSummaries — the authoritative aggregate)
        var scores = await db.StrandReviewSummaries
            .AsNoTracking()
            .Where(r => ids.Contains(r.StrandId))
            .GroupBy(r => r.StrandId)
            .Select(g => new
            {
                StrandId  = g.Key,
                Score     = g.OrderByDescending(r => r.GeneratedAt).Select(r => (double?)r.AvgScore).First(),
                ScoredAt  = g.OrderByDescending(r => r.GeneratedAt).Select(r => (DateTime?)r.GeneratedAt).First(),
                Reviews   = g.OrderByDescending(r => r.GeneratedAt).Select(r => (int?)r.ReviewCount).First(),
            })
            .ToDictionaryAsync(x => x.StrandId);

        // Word counts from beats
        var wordCounts = await db.StrandBeats
            .AsNoTracking()
            .Where(sb => ids.Contains(sb.StrandId) && sb.IsEnabled)
            .Join(db.Beats.AsNoTracking().Where(b => b.Text != null && b.Text != ""),
                  sb => sb.BeatId, b => b.Id, (sb, b) => new { sb.StrandId, b.Text })
            .GroupBy(x => x.StrandId)
            .Select(g => new { StrandId = g.Key, Chars = g.Sum(x => (long)x.Text!.Length) })
            .ToDictionaryAsync(x => x.StrandId);

        var rows = strands.Select(s =>
        {
            scores.TryGetValue(s.Id, out var sc);
            wordCounts.TryGetValue(s.Id, out var wc);
            // Rough word count from char count (avg English word ≈ 5 chars + 1 space)
            var words = wc != null ? (int)(wc.Chars / 5.2) : 0;
            return new
            {
                id        = s.Id,
                code      = s.StrandCode,
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

        return JsonSerializer.Serialize(new { count = rows.Count, strands = rows }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Update a strand's metadata fields. Pass only the fields you want to change — omit the rest to leave them unchanged. Editable fields: title, synopsis, kind, status, seed, code (StrandCode), voice_id. Status valid values: draft | ready | canon | archived. Code is uppercased and must be unique across non-null values — pass empty string to clear it. Does NOT touch beats or audio.")]
    public async Task<string> UpdateStrand(
        [Description("Strand id (GUID) or slug.")] string idOrSlug,
        [Description("New title. Omit to leave unchanged.")] string? title = null,
        [Description("Short synopsis. Omit to leave unchanged; pass empty string to clear.")] string? synopsis = null,
        [Description("Kind label: book | chapter | episode | novella | novel | strand | scene | saga | anthology. Omit to leave unchanged.")] string? kind = null,
        [Description("Status: draft | ready | canon | archived. Omit to leave unchanged.")] string? status = null,
        [Description("Generation seed (one-line premise). Omit to leave unchanged; pass empty string to clear.")] string? seed = null,
        [Description("Short author reference code (e.g. 'ATTE'). Uppercased; pass empty string to clear. Omit to leave unchanged.")] string? code = null,
        [Description("ElevenLabs or local TTS voice id. Omit to leave unchanged; pass empty string to clear.")] string? voiceId = null)
    {
        var strand = await ResolveStrandAsync(idOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", idOrSlug }, CanonTools.JsonOpts);

        await using var db = await dbFactory.CreateDbContextAsync();
        var row = await db.Strands.FindAsync(strand.Id)
            ?? throw new InvalidOperationException($"Strand {strand.Id} missing.");

        if (title    != null) row.Title    = title;
        if (synopsis != null) row.Synopsis = string.IsNullOrEmpty(synopsis) ? null : synopsis;
        if (kind     != null) row.Kind     = kind;
        if (status   != null) row.Status   = status;
        if (seed     != null) row.Seed     = string.IsNullOrEmpty(seed) ? null : seed;
        if (code     != null) row.StrandCode = string.IsNullOrEmpty(code) ? null : code.Trim().ToUpperInvariant();
        if (voiceId  != null) row.VoiceId  = string.IsNullOrEmpty(voiceId) ? null : voiceId;
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
            code   = row.StrandCode,
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Return the score history for a strand as a time-series — every review run that produced a summary, with its mean score, SD, review count, and date. Use to track whether an edit moved the needle, or to compare pre/post-edit trajectories. Accepts strand id (GUID) or slug.")]
    public async Task<string> GetScoreHistory(
        [Description("Strand id (GUID) or slug.")] string idOrSlug,
        [Description("Maximum history points to return (most recent first). Default 20.")] int limit = 20)
    {
        var strand = await ResolveStrandAsync(idOrSlug);
        if (strand == null) return JsonSerializer.Serialize(new { error = "strand_not_found", idOrSlug }, CanonTools.JsonOpts);

        await using var db = await dbFactory.CreateDbContextAsync();
        var history = await db.StrandScoreHistories
            .AsNoTracking()
            .Where(h => h.StrandId == strand.Id)
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

        // Also include StrandReviewSummaries for runs pre-dating StrandScoreHistories
        var srsHistory = await db.StrandReviewSummaries
            .AsNoTracking()
            .Where(r => r.StrandId == strand.Id)
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
            strand_id    = strand.Id,
            slug         = strand.Slug,
            title        = strand.Title,
            point_count  = merged.Count,
            history      = merged,
        }, CanonTools.JsonOpts);
    }

    private async Task<Strand?> ResolveStrandAsync(string idOrSlug)
    {
        if (string.IsNullOrWhiteSpace(idOrSlug)) return null;
        await using var db = await dbFactory.CreateDbContextAsync();
        if (Guid.TryParse(idOrSlug, out var guid))
        {
            var byId = await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Id == guid);
            if (byId != null) return byId;
        }
        return await db.Strands.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == idOrSlug);
    }
}
