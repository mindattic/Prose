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

    public StrandTools(StrandWorkbenchService workbench, IDbContextFactory<StreetSamuraiDbContext> dbFactory, ElevenLabsTtsService tts)
    {
        this.workbench = workbench;
        this.dbFactory = dbFactory;
        this.tts = tts;
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
            status = strand.Status, synopsis = strand.Synopsis, voice_id = strand.VoiceId,
            parent_strand_id = strand.ParentStrandId, chars_narrated = strand.CharsNarrated,
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
                facet_tag = b.Beat.FacetTag,
            }),
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description("Create a new top-level strand. Returns the new strand's id, slug, and a relative URL to open it in the unified writer.")]
    public async Task<string> CreateStrand(
        [Description("Strand title. Required.")] string title,
        [Description("Free-form kind label: 'book', 'chapter', 'episode', 'scene', 'saga', 'anthology', or anything you want. Default 'strand'.")] string kind = "strand",
        [Description("Optional synopsis.")] string synopsis = "",
        [Description("Optional parent strand Guid id (or slug). Empty = top-level.")] string parentStrandIdOrSlug = "")
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
            Kind = string.IsNullOrEmpty(kind) ? "strand" : kind,
            Status = "draft",
            ParentStrandId = parentId,
            SortKey = maxSort + 100.0,
        });
        await db.SaveChangesAsync();
        return JsonSerializer.Serialize(new { ok = true, id, slug, url = $"/strand/{slug}" }, CanonTools.JsonOpts);
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
            facet_tag       = beat.FacetTag,
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

    [McpServerTool, Description("Update a beat's metadata: BeatTitle, Synopsis, EmotionalTone, PaceHint, FacetTag, StructureRole, Act, SceneType, IsChapterStart, Kind. Pass empty strings to clear nullable fields. Does NOT touch prose or audio. Use to mark a beat as a chapter start, change its kind to quote/dedication/book-title, or set the tone the next re-record uses.")]
    public async Task<string> UpdateBeatMetadata(
        [Description("Beat Guid OR 'strand-guid.beat-guid' handle.")] string beatHandle,
        [Description("Short label. When IsChapterStart=true this is the chapter heading; when Kind=quote this is the attribution.")] string beatTitle = "",
        [Description("One-line synopsis fed to LLM regenerations.")] string synopsis = "",
        [Description("Emotional tone, e.g. 'quiet' / 'tense' / 'wry'.")] string emotionalTone = "",
        [Description("Pace hint, e.g. 'flowing' / 'clipped' / 'staccato' / 'languorous'.")] string paceHint = "",
        [Description("Character facet: WOUND / SHADOW / MASK / IDEAL.")] string facetTag = "",
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
            FacetTag:       facetTag,
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
