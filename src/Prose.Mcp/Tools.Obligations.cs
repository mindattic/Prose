using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Obligations;

namespace Prose.Mcp;

// ── Narrative Obligation Ledger (RFC 0013) ────────────────────────────────────
// What the story owes the reader, as rows: every promise the prose makes is logged when made
// (verbatim quote + origin beat), closed only by a quote from the paying beat, and aged by the
// chapter trial balance. These tools are the author's desk: read the board, decide each row.
// Nothing here writes prose.
//
//   list_obligations          — the board for a book (filter by state/kind/overdue/chapter)
//   get_obligation            — one row + its journal
//   obligation_trial_balance  — the period close for a chapter (or the book)
//   open_obligation           — author-declared promise (provenance authored, locked)
//   close_obligation          — pay it: closing beat + verbatim quote (refused if the quote is not in that beat)
//   drop_obligation           — intentionally abandon it: reason + note
//   defer_obligation          — move its due point; a deliberate mystery is a Deferred row, not silence
//   reopen_obligation / set_obligation_due / accept_obligation / link_obligation_entity

[McpServerToolType]
public class ObligationTools(
    NarrativeObligationService obligations,
    Prose.Core.Services.Audit.ObligationReconciliationService reconciler,
    EntityRecordGroundingService grounding,
    IDbContextFactory<ProseDbContext> dbFactory,
    HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;
    const string Actor = ObligationActor.AuthorMcp;

    [McpServerTool, Description("List the narrative obligations of a book — every promise the prose has made that the ledger tracks (RFC 0013). Each row: id, kind (promise|plant|question|wound|foreshadow|introduced-referent|unexplained-presence), description, state (Open|Advanced|Closed|Dropped|Deferred), provenance (authored|observed|inferred), origin chapter/beat and the verbatim quote that made the promise, due point, overdue flag, and whether the author has locked it. Filters: state, kind, overdueOnly, chapter (origin chapter ordinal). Accepts a book or chapter node id/slug/code — always reports the whole book.")]
    public Task<string> list_obligations(
        [Description("Node id (GUID), slug, or NodeCode of the book (a chapter resolves to its book).")] string nodeIdOrSlug,
        [Description("Optional state filter: Open, Advanced, Closed, Dropped, Deferred.")] string? state = null,
        [Description("Optional kind filter.")] string? kind = null,
        [Description("Only rows past their due point with no author decision.")] bool overdueOnly = false,
        [Description("Only rows whose origin is in this chapter ordinal (1-based).")] int? chapter = null) =>
        hub.InvokeAsync(nameof(ObligationTools), nameof(list_obligationsImpl), new { nodeIdOrSlug, state, kind, overdueOnly, chapter });

    public async Task<string> list_obligationsImpl(string nodeIdOrSlug, string? state = null, string? kind = null, bool overdueOnly = false, int? chapter = null)
    {
        var nodeId = await ResolveBookAsync(nodeIdOrSlug);
        if (nodeId == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        var rows = await obligations.ListAsync(nodeId.Value, state, kind, overdueOnly, chapter);
        return JsonSerializer.Serialize(new { node_id = nodeId, count = rows.Count, obligations = rows }, JsonOpts);
    }

    [McpServerTool, Description("One obligation with its full journal (every open/advance/close/drop/defer/withdraw event, who did it, and the quote that justified it).")]
    public Task<string> get_obligation([Description("Obligation id (GUID).")] string obligationId) =>
        hub.InvokeAsync(nameof(ObligationTools), nameof(get_obligationImpl), new { obligationId });

    public async Task<string> get_obligationImpl(string obligationId)
    {
        if (!Guid.TryParse(obligationId, out var id)) return JsonSerializer.Serialize(new { error = "invalid_guid" }, JsonOpts);
        var row = await obligations.GetAsync(id);
        if (row == null) return JsonSerializer.Serialize(new { error = "not_found" }, JsonOpts);
        var events = await obligations.HistoryAsync(id);
        return JsonSerializer.Serialize(new { obligation = row, events }, JsonOpts);
    }

    [McpServerTool, Description("The chapter trial balance (RFC 0013): opened − closed − dropped − deferred = carried forward, plus the rows the hard gate reads — obligations past their due point at the end of this chapter with no author decision. Also lists what this chapter opened and closed, closures whose quote is no longer on the page, and rows whose origin beat was deleted. balanced=false means the chapter cannot close until each overdue row is closed, dropped or deferred. could_not_look=true means the ledger is empty for this book — an empty ledger FAILS, it does not pass; run a rescan first. Omit chapter for the whole book.")]
    public Task<string> obligation_trial_balance(
        [Description("Node id (GUID), slug, or NodeCode of the book.")] string nodeIdOrSlug,
        [Description("Chapter ordinal (1-based). Omit for the whole book.")] int? chapter = null) =>
        hub.InvokeAsync(nameof(ObligationTools), nameof(obligation_trial_balanceImpl), new { nodeIdOrSlug, chapter });

    public async Task<string> obligation_trial_balanceImpl(string nodeIdOrSlug, int? chapter = null)
    {
        var nodeId = await ResolveBookAsync(nodeIdOrSlug);
        if (nodeId == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        var tb = await obligations.TrialBalanceAsync(nodeId.Value, chapter);
        return JsonSerializer.Serialize(new
        {
            node_id = tb.NodeId, chapter = tb.ChapterOrdinal, chapter_count = tb.ChapterCount,
            total_beats = tb.TotalBeats, scanned_beats = tb.ScannedBeats,
            opened = tb.Opened, advanced = tb.Advanced, closed = tb.Closed, dropped = tb.Dropped, deferred = tb.Deferred, withdrawn = tb.Withdrawn, carried_forward = tb.CarriedForward,
            balanced = tb.Balanced, could_not_look = tb.CouldNotLook,
            overdue_without_decision = tb.OverdueWithoutDecision,
            opened_here = tb.OpenedHere, closed_here = tb.ClosedHere,
            stale_closures = tb.StaleClosures, dangling_origins = tb.DanglingOrigins,
        }, JsonOpts);
    }

    [McpServerTool, Description("Declare an obligation yourself (provenance authored, locked): a promise you intend the book to keep. Optionally anchor it to a beat with the verbatim quote that makes the promise — refused if the quote is not in that beat's text. due: 'chapter:7', 'beats:12', or 'book-end' (default).")]
    public Task<string> open_obligation(
        [Description("Book node id/slug/code.")] string nodeIdOrSlug,
        [Description("promise | plant | question | wound | foreshadow | introduced-referent | unexplained-presence")] string kind,
        [Description("What is owed, one line.")] string description,
        [Description("Origin beat GUID (optional).")] string? beatId = null,
        [Description("Verbatim quote from that beat (optional; ≥12 chars).")] string? quote = null,
        [Description("Narrative condition under which paying this becomes natural (optional).")] string? trigger = null,
        [Description("chapter:N | beats:N | book-end")] string due = "book-end") =>
        hub.InvokeAsync(nameof(ObligationTools), nameof(open_obligationImpl), new { nodeIdOrSlug, kind, description, beatId, quote, trigger, due });

    public async Task<string> open_obligationImpl(string nodeIdOrSlug, string kind, string description, string? beatId = null, string? quote = null, string? trigger = null, string due = "book-end")
    {
        var nodeId = await ResolveBookAsync(nodeIdOrSlug);
        if (nodeId == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        Guid? origin = null;
        if (!string.IsNullOrWhiteSpace(beatId))
        {
            if (!Guid.TryParse(beatId, out var b)) return JsonSerializer.Serialize(new { error = "invalid_guid", beatId }, JsonOpts);
            origin = b;
        }
        var (dueKind, dueValue) = ParseDue(due);
        if (dueKind == null) return BadDue(due);
        return Result(await obligations.OpenAsync(nodeId.Value, kind, description, Actor, origin, quote, null, trigger, dueKind, dueValue));
    }

    [McpServerTool, Description("Pay an obligation: name the beat that pays it and quote the sentence verbatim. Refused with quote_not_found if the quote is not in that beat's text — closure is verified against the artifact, never asserted.")]
    public Task<string> close_obligation(
        [Description("Obligation id (GUID).")] string obligationId,
        [Description("Closing beat GUID.")] string beatId,
        [Description("Verbatim quote from the closing beat (≥12 chars).")] string quote,
        [Description("Optional note.")] string? note = null) =>
        hub.InvokeAsync(nameof(ObligationTools), nameof(close_obligationImpl), new { obligationId, beatId, quote, note });

    public async Task<string> close_obligationImpl(string obligationId, string beatId, string quote, string? note = null)
    {
        if (!Guid.TryParse(obligationId, out var id) || !Guid.TryParse(beatId, out var b)) return JsonSerializer.Serialize(new { error = "invalid_guid" }, JsonOpts);
        return Result(await obligations.CloseAsync(id, b, quote, note, Actor));
    }

    [McpServerTool, Description("Intentionally abandon an obligation. reason: background-texture | pov-limited | genre-convention | intentional-mystery | series-deferred | false-extraction | duplicate-of:<id>. A note in your words is required. 'false-extraction' and 'background-texture' feed the extractor's stop-list for this universe.")]
    public Task<string> drop_obligation(
        [Description("Obligation id (GUID).")] string obligationId,
        [Description("Dropped reason (see description).")] string reason,
        [Description("Why, in your words.")] string note) =>
        hub.InvokeAsync(nameof(ObligationTools), nameof(drop_obligationImpl), new { obligationId, reason, note });

    public async Task<string> drop_obligationImpl(string obligationId, string reason, string note)
    {
        if (!Guid.TryParse(obligationId, out var id)) return JsonSerializer.Serialize(new { error = "invalid_guid" }, JsonOpts);
        return Result(await obligations.DropAsync(id, reason, note, Actor));
    }

    [McpServerTool, Description("Defer an obligation to a later due point — 'I'll pay this in Ch12' is defer to chapter:12; a deliberate slow burn is defer to book-end. A note is required. Deferred rows are never flagged before their new due point.")]
    public Task<string> defer_obligation(
        [Description("Obligation id (GUID).")] string obligationId,
        [Description("Why, in your words.")] string note,
        [Description("chapter:N | beats:N | book-end")] string due = "book-end") =>
        hub.InvokeAsync(nameof(ObligationTools), nameof(defer_obligationImpl), new { obligationId, note, due });

    public async Task<string> defer_obligationImpl(string obligationId, string note, string due = "book-end")
    {
        if (!Guid.TryParse(obligationId, out var id)) return JsonSerializer.Serialize(new { error = "invalid_guid" }, JsonOpts);
        var (dueKind, dueValue) = ParseDue(due);
        if (dueKind == null) return BadDue(due);
        return Result(await obligations.DeferAsync(id, note, dueKind, dueValue, Actor));
    }

    [McpServerTool, Description("Reopen a closed, dropped or deferred obligation (author decision; locks the row).")]
    public Task<string> reopen_obligation([Description("Obligation id (GUID).")] string obligationId, [Description("Optional note.")] string? note = null) =>
        hub.InvokeAsync(nameof(ObligationTools), nameof(reopen_obligationImpl), new { obligationId, note });

    public async Task<string> reopen_obligationImpl(string obligationId, string? note = null)
    {
        if (!Guid.TryParse(obligationId, out var id)) return JsonSerializer.Serialize(new { error = "invalid_guid" }, JsonOpts);
        return Result(await obligations.ReopenAsync(id, note, Actor));
    }

    [McpServerTool, Description("Change an obligation's due point without changing its state. due: chapter:N | beats:N | book-end.")]
    public Task<string> set_obligation_due([Description("Obligation id (GUID).")] string obligationId, [Description("chapter:N | beats:N | book-end")] string due) =>
        hub.InvokeAsync(nameof(ObligationTools), nameof(set_obligation_dueImpl), new { obligationId, due });

    public async Task<string> set_obligation_dueImpl(string obligationId, string due)
    {
        if (!Guid.TryParse(obligationId, out var id)) return JsonSerializer.Serialize(new { error = "invalid_guid" }, JsonOpts);
        var (dueKind, dueValue) = ParseDue(due);
        if (dueKind == null) return BadDue(due);
        return Result(await obligations.SetDueAsync(id, dueKind, dueValue, Actor));
    }

    [McpServerTool, Description("Accept an extracted obligation as-is: locks it so no automated rescan can withdraw, re-anchor or re-state it.")]
    public Task<string> accept_obligation([Description("Obligation id (GUID).")] string obligationId) =>
        hub.InvokeAsync(nameof(ObligationTools), nameof(accept_obligationImpl), new { obligationId });

    public async Task<string> accept_obligationImpl(string obligationId)
    {
        if (!Guid.TryParse(obligationId, out var id)) return JsonSerializer.Serialize(new { error = "invalid_guid" }, JsonOpts);
        return Result(await obligations.LockAsync(id, Actor));
    }

    [McpServerTool, Description("Point an obligation at the entity it is about (e.g. after naming an '(unnamed) girl behind the curtain' stub, or merging it into a canon character).")]
    public Task<string> link_obligation_entity([Description("Obligation id (GUID).")] string obligationId, [Description("Entity id (GUID).")] string entityId) =>
        hub.InvokeAsync(nameof(ObligationTools), nameof(link_obligation_entityImpl), new { obligationId, entityId });

    public async Task<string> link_obligation_entityImpl(string obligationId, string entityId)
    {
        if (!Guid.TryParse(obligationId, out var id) || !Guid.TryParse(entityId, out var e)) return JsonSerializer.Serialize(new { error = "invalid_guid" }, JsonOpts);
        return Result(await obligations.LinkEntityAsync(id, e, Actor));
    }

    // ── instruments ───────────────────────────────────────────────────────────

    [/* McpServerTool DEACTIVATED 2026-09-22 (RFC 0014) - 0 applied findings, ever; delete these comment markers to restore */ Description("Run the obligation reconciliation instrument on a book (RFC 0013): six free deterministic rules over the ledger — overdue_open, open_at_end, stale_closure, dangling_beat, unplanted_payoff, deferred_expired — filed as NarrativeObligation findings under node:{slug}#obligations, plus a health snapshot. deep=true first runs the paid resurfacing judge (one Haiku call per open obligation, quote-gated, cached by candidate text) so payoffs the extractor missed are closed before the balance is struck. Reports 'examined N obligations over M beats'; could_not_look=true means the ledger is empty — rescan first.")]
    public Task<string> reconcile_obligations(
        [Description("Book node id/slug/code.")] string nodeIdOrSlug,
        [Description("Also run the resurfacing judge (costs cents).")] bool deep = false) =>
        hub.InvokeAsync(nameof(ObligationTools), nameof(reconcile_obligationsImpl), new { nodeIdOrSlug, deep });

    public async Task<string> reconcile_obligationsImpl(string nodeIdOrSlug, bool deep = false)
    {
        var nodeId = await ResolveBookAsync(nodeIdOrSlug);
        if (nodeId == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        var r = await reconciler.RunAsync(nodeId.Value, deep);
        return JsonSerializer.Serialize(new
        {
            r.NodeId, r.Slug, r.Title, examined = r.Examined, total_beats = r.TotalBeats, scanned_beats = r.ScannedBeats,
            could_not_look = r.CouldNotLook, book_at_end = r.BookAtEnd, balanced = r.Balance.Balanced,
            overdue_without_decision = r.Balance.OverdueWithoutDecision, rule_counts = r.RuleCounts,
            findings = r.Verdicts.Where(v => v.Severity != "PASS").Select(v => new { v.RuleKey, v.Severity, v.Evidence, beat_id = v.Location }),
            deep = r.Deep, snapshot = r.Snapshot,
        }, JsonOpts);
    }

    [/* McpServerTool DEACTIVATED 2026-09-22 (RFC 0014) - 0 applied findings, ever; delete these comment markers to restore */ Description("Ground entity records in prose (RFC 0013): decompose every character/place/faction record tagged in the book into atomic claims and check each against the beats with a quote-gated entailment call. Unentailed/contradicted claims are filed under EntityDrift (node:{slug}#recordground) and matching non-authored ledger claims are quarantined to 'inferred'. The record text is never edited — you accept or strike. Optional entity name filter. Costs a few cents per entity.")]
    public Task<string> ground_entity_records(
        [Description("Book node id/slug/code.")] string nodeIdOrSlug,
        [Description("Only entities whose name contains this (optional).")] string? entityName = null) =>
        hub.InvokeAsync(nameof(ObligationTools), nameof(ground_entity_recordsImpl), new { nodeIdOrSlug, entityName });

    public async Task<string> ground_entity_recordsImpl(string nodeIdOrSlug, string? entityName = null)
    {
        var nodeId = await ResolveBookAsync(nodeIdOrSlug);
        if (nodeId == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        var r = await grounding.RunAsync(nodeId.Value, entityName);
        return JsonSerializer.Serialize(r, JsonOpts);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    static string Result(NarrativeObligationService.AuthorResult res) =>
        JsonSerializer.Serialize(res.Ok
            ? new { ok = true, error = (string?)null, obligation = res.Row }
            : new { ok = false, error = res.Error, obligation = (NarrativeObligation?)null }, JsonOpts);

    static string BadDue(string? due) =>
        JsonSerializer.Serialize(new { error = "bad_due", due, hint = "chapter:N | beats:N | book-end" }, JsonOpts);

    /// <summary>Kind is null when <paramref name="due"/> is none of the accepted forms: a typo used
    /// to become book-end silently and answer ok:true.</summary>
    static (string? Kind, int? Value) ParseDue(string? due)
    {
        if (string.IsNullOrWhiteSpace(due) || due.Equals("book-end", StringComparison.OrdinalIgnoreCase)) return (ObligationDueKind.BookEnd, null);
        var parts = due.Split(':', 2);
        if (parts.Length == 2 && int.TryParse(parts[1], out var n))
        {
            if (parts[0].Equals("chapter", StringComparison.OrdinalIgnoreCase)) return (ObligationDueKind.Chapter, n);
            if (parts[0].Equals("beats",   StringComparison.OrdinalIgnoreCase)) return (ObligationDueKind.Beats, n);
        }
        return (null, null);
    }

    async Task<Guid?> ResolveBookAsync(string idOrSlug)
    {
        var nodeId = await NodeRefResolver.ResolveAsync(dbFactory, idOrSlug);
        if (nodeId == null) return null;
        await using var db = await dbFactory.CreateDbContextAsync();
        // The nearest book ancestor, as the plant/payoff path files it: a book's parent can be a
        // series, and walking to the root filed and listed a series-member book's obligations
        // against the series.
        return await NodeWorkbenchService.ResolveBookAncestorIdAsync(db, nodeId.Value) ?? nodeId.Value;
    }
}
