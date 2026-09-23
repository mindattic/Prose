using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Reading (author ruling 2026-09-22: the book is the book) ──────────────────
// A read is the only instrument. What it keeps is a receipt per beat (the text hash read, and its
// neighbours) plus the notes the reader filed. Unread status is computed, never stored, and every
// shippable export refuses while any beat is unread. There is no override.
//
//   read_status      — which beats are unread and why (never read / text changed / moved / entity changed)
//   add_read_note    — file a defect | question | note on a beat, stamped with its current hash
//   list_read_notes  — notes in reading order, flagged where the beat changed since
//   resolve_read_note — a question answered or a defect fixed
// Receipts are written by read_beats(markRead:true, readBy:…), for exactly the beats it returned.

[McpServerToolType]
public class ReadingTools(ReadGateService gate, IDbContextFactory<ProseDbContext> dbFactory, HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    Task<Guid?> Resolve(string nodeIdOrSlug) => NodeRefResolver.ResolveAsync(dbFactory, nodeIdOrSlug);

    [McpServerTool, Description("Which beats of a book (or chapter) are unread as they stand now, and why: never read, text changed since it was read, moved (the beat before or after it changed — a chapter move or an insert/delete beside it), or an entity it mentions was edited after it was read. Computed on demand from text hashes; nothing is stored that could drift. Export refuses while any beat is unread, with no override — read the listed beats with read_beats(markRead:true) first.")]
    public Task<string> read_status(
        [Description("Book or chapter id, slug, or NodeCode.")] string nodeIdOrSlug,
        [Description("Max unread beats to list individually (default 200). Counts are always complete.")] int limit = 200) =>
        hub.InvokeAsync(nameof(ReadingTools), nameof(ReadStatusImpl), new { nodeIdOrSlug, limit });

    public async Task<string> ReadStatusImpl(string nodeIdOrSlug, int limit = 200)
    {
        if (await Resolve(nodeIdOrSlug) is not { } id) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        var s = await gate.GetStatusAsync(id);
        return JsonSerializer.Serialize(new
        {
            all_read = s.AllRead,
            total_beats = s.TotalBeats,
            unread = s.Unread.Count,
            by_reason = s.Unread.GroupBy(u => u.Reason.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            positions = ReadGateService.Runs(s.Unread.Select(u => u.Position)),
            summary = ReadGateService.Describe(s),
            beats = s.Unread.Take(limit).Select(u => new { position = u.Position, number = u.Number, reason = u.Reason.ToString(), detail = u.Detail }),
        }, JsonOpts);
    }

    [McpServerTool, Description("File what a read found on one beat: kind defect | question | note. Stamped with the beat's current text hash, so list_read_notes shows when the beat has changed since. This records what a reader noticed; it is not canon.")]
    public Task<string> add_read_note(
        [Description("Book or chapter id, slug, or NodeCode.")] string nodeIdOrSlug,
        [Description("Global Beat.Number of the beat.")] int beat,
        [Description("defect | question | note")] string kind,
        [Description("What the reader noticed.")] string text,
        [Description("Who read it.")] string readBy) =>
        hub.InvokeAsync(nameof(ReadingTools), nameof(AddReadNoteImpl), new { nodeIdOrSlug, beat, kind, text, readBy });

    public async Task<string> AddReadNoteImpl(string nodeIdOrSlug, int beat, string kind, string text, string readBy)
    {
        if (await Resolve(nodeIdOrSlug) is not { } id) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        try
        {
            var n = await gate.AddNoteAsync(id, beat, kind, text, readBy);
            return JsonSerializer.Serialize(new { ok = true, id = n.Id, beat, kind = n.Kind }, JsonOpts);
        }
        catch (ArgumentException ex) { return JsonSerializer.Serialize(new { error = "bad_request", message = ex.Message }, JsonOpts); }
    }

    [McpServerTool, Description("Read notes on a book's beats in reading order. beat_changed_since = the beat's text no longer matches what the note was written against.")]
    public Task<string> list_read_notes(
        [Description("Book or chapter id, slug, or NodeCode.")] string nodeIdOrSlug,
        [Description("open (default) | resolved | all")] string status = "open",
        [Description("Optional: defect | question | note")] string? kind = null) =>
        hub.InvokeAsync(nameof(ReadingTools), nameof(ListReadNotesImpl), new { nodeIdOrSlug, status, kind });

    public async Task<string> ListReadNotesImpl(string nodeIdOrSlug, string status = "open", string? kind = null)
    {
        if (await Resolve(nodeIdOrSlug) is not { } id) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        var rows = await gate.ListNotesAsync(id, status, kind);
        return JsonSerializer.Serialize(new { count = rows.Count, notes = rows }, JsonOpts);
    }

    [McpServerTool, Description("Resolve a read note: a question answered or a defect fixed. Optionally name the beat (global Beat.Number) that answered it.")]
    public Task<string> resolve_read_note(
        [Description("Book or chapter id, slug, or NodeCode.")] string nodeIdOrSlug,
        [Description("Note id from list_read_notes.")] string noteId,
        [Description("Optional Beat.Number of the beat that answered or fixed it.")] int? byBeat = null) =>
        hub.InvokeAsync(nameof(ReadingTools), nameof(ResolveReadNoteImpl), new { nodeIdOrSlug, noteId, byBeat });

    public async Task<string> ResolveReadNoteImpl(string nodeIdOrSlug, string noteId, int? byBeat = null)
    {
        if (await Resolve(nodeIdOrSlug) is not { } id) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        if (!Guid.TryParse(noteId, out var nid)) return JsonSerializer.Serialize(new { error = "bad_note_id", noteId }, JsonOpts);
        try { return JsonSerializer.Serialize(new { ok = await gate.ResolveNoteAsync(nid, id, byBeat) }, JsonOpts); }
        catch (ArgumentException ex) { return JsonSerializer.Serialize(new { error = "bad_request", message = ex.Message }, JsonOpts); }
    }
}
