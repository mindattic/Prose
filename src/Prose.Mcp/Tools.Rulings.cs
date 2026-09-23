using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;
using Prose.Core.Services.Factory;

namespace Prose.Mcp;

// ── The author's law as data (RFC 0015 §3.6–3.7) ──────────────────────────────
// A decision goes into the world the moment it is made. Constraints and style law are rulings;
// entity FACTS go on the entity record instead, so a ruling is never a second copy of the world.
//   record_ruling / list_rulings / supersede_ruling — the law
//   law_violations — every place a law's zero-tolerance pattern matches the prose
//   book_metrics   — book-wide tic counts against the author's metric ceilings
// CLI twin: prose --ruling add|seed|list|supersede|violations|metrics.

[McpServerToolType]
public class RulingTools(RulingService rulings, MetricsReport metrics, IDbContextFactory<ProseDbContext> dbFactory, HubInvoker hub)
{
    static readonly JsonSerializerOptions JsonOpts = CanonTools.JsonOpts;

    Task<Guid?> Resolve(string? r) => string.IsNullOrWhiteSpace(r) ? Task.FromResult<Guid?>(null) : NodeRefResolver.ResolveAsync(dbFactory, r);

    [McpServerTool, Description("Record one of the author's rulings the moment it is made. kind law = a constraint (pattern-less text the writer is shown, or a zero-tolerance regex neither the prose nor any record the book tags may match; case-insensitive unless the pattern starts with (?-i)); kind page-law = a fact the world holds but the page must never say (its pattern binds the prose only); kind metric = a book-wide tic ceiling (pattern + maxPer1kWords); kind incidental = a proper name that intentionally has no entity (pattern = the name). Entity facts do NOT go here — write them on the entity record. Returns the stored row as the read-back.")]
    public Task<string> record_ruling(
        [Description("law | page-law | metric | incidental")] string kind,
        [Description("The author's words, verbatim.")] string text,
        [Description("Book id, slug or NodeCode (omit only for a universe-wide ruling).")] string? nodeIdOrSlug = null,
        [Description(".NET regex (law/metric) or the name (incidental).")] string? pattern = null,
        [Description("Metric only: the ceiling per 1,000 words of the whole book.")] decimal? maxPer1kWords = null,
        [Description("author (default) or session:<id>.")] string source = "author") =>
        hub.InvokeAsync(nameof(RulingTools), nameof(RecordRulingImpl), new { kind, text, nodeIdOrSlug, pattern, maxPer1kWords, source });

    public async Task<string> RecordRulingImpl(string kind, string text, string? nodeIdOrSlug = null, string? pattern = null, decimal? maxPer1kWords = null, string source = "author")
    {
        try
        {
            var row = await rulings.RecordAsync(new RulingDraft(kind, text, await Resolve(nodeIdOrSlug), null, pattern, maxPer1kWords, source));
            return JsonSerializer.Serialize(new { ok = true, row.Id, row.Kind, row.Text, row.Pattern, row.MaxPer1kWords, row.Source, row.At }, JsonOpts);
        }
        catch (ArgumentException ex) { return JsonSerializer.Serialize(new { ok = false, error = ex.Message }, JsonOpts); }
    }

    [McpServerTool, Description("The active rulings that apply to a book (its own plus universe-wide ones).")]
    public Task<string> list_rulings([Description("Book id, slug or NodeCode.")] string nodeIdOrSlug, [Description("law | page-law | metric | incidental")] string? kind = null) =>
        hub.InvokeAsync(nameof(RulingTools), nameof(ListRulingsImpl), new { nodeIdOrSlug, kind });

    public async Task<string> ListRulingsImpl(string nodeIdOrSlug, string? kind = null)
    {
        if (await Resolve(nodeIdOrSlug) is not { } id) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        var rows = await rulings.ListAsync(id, kind);
        return JsonSerializer.Serialize(rows.Select(r => new { r.Id, r.Kind, r.Text, r.Pattern, r.MaxPer1kWords, r.Source, r.At }), JsonOpts);
    }

    [McpServerTool, Description("Replace a ruling: the new one is recorded and the old one goes inert (history is kept).")]
    public Task<string> supersede_ruling(
        [Description("The ruling to replace.")] string id,
        [Description("law | page-law | metric | incidental")] string kind,
        [Description("The author's new words, verbatim.")] string text,
        [Description("New pattern.")] string? pattern = null,
        [Description("Metric only.")] decimal? maxPer1kWords = null,
        [Description("author (default) or session:<id>.")] string source = "author") =>
        hub.InvokeAsync(nameof(RulingTools), nameof(SupersedeRulingImpl), new { id, kind, text, pattern, maxPer1kWords, source });

    public async Task<string> SupersedeRulingImpl(string id, string kind, string text, string? pattern = null, decimal? maxPer1kWords = null, string source = "author")
    {
        if (!Guid.TryParse(id, out var gid)) return JsonSerializer.Serialize(new { ok = false, error = "bad_id" }, JsonOpts);
        try
        {
            var row = await rulings.SupersedeAsync(gid, new RulingDraft(kind, text, null, null, pattern, maxPer1kWords, source));
            return JsonSerializer.Serialize(new { ok = true, superseded = gid, row.Id, row.Kind, row.Text, row.Pattern }, JsonOpts);
        }
        catch (ArgumentException ex) { return JsonSerializer.Serialize(new { ok = false, error = ex.Message }, JsonOpts); }
    }

    [McpServerTool, Description("Every place an active law's zero-tolerance pattern matches the book's prose, in reading order, with context. Each hit is either a real violation (fix the prose by splice) or a pattern that is too broad (supersede the ruling with a tighter pattern).")]
    public Task<string> law_violations([Description("Book id, slug or NodeCode.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(RulingTools), nameof(LawViolationsImpl), new { nodeIdOrSlug });

    public async Task<string> LawViolationsImpl(string nodeIdOrSlug)
    {
        if (await Resolve(nodeIdOrSlug) is not { } id) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        var hits = await rulings.FindLawViolationsAsync(id);
        return JsonSerializer.Serialize(new { count = hits.Count, hits }, JsonOpts);
    }

    [McpServerTool, Description("Every place an active law's pattern matches the canonical record of an entity the book tags (the world must not hold what the page may not say). Page-laws are not applied to records: they name facts the record is meant to hold. Each hit is fixed on the record (set_character_fields / create_*), or the pattern is superseded if too broad.")]
    public Task<string> record_law_violations(
        [Description("Book id, slug or NodeCode.")] string nodeIdOrSlug,
        [Description("Narrow to one entity's record.")] string? entityId = null,
        [Description("Search the records for this one .NET regex instead of the laws (read-only, recorded nowhere).")] string? searchPattern = null) =>
        hub.InvokeAsync(nameof(RulingTools), nameof(RecordLawViolationsImpl), new { nodeIdOrSlug, entityId, searchPattern });

    public async Task<string> RecordLawViolationsImpl(string nodeIdOrSlug, string? entityId = null, string? searchPattern = null)
    {
        if (await Resolve(nodeIdOrSlug) is not { } id) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        var hits = await rulings.FindRecordViolationsAsync(id, Guid.TryParse(entityId, out var e) ? e : null, searchPattern);
        return JsonSerializer.Serialize(new { count = hits.Count, records = hits.Select(h => h.EntityId).Distinct().Count(), hits }, JsonOpts);
    }

    [McpServerTool, Description("Book-wide tic counts against the author's metric rulings (counting, not judging). Only author-sourced metrics gate the press.")]
    public Task<string> book_metrics([Description("Book id, slug or NodeCode.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(RulingTools), nameof(BookMetricsImpl), new { nodeIdOrSlug });

    public async Task<string> BookMetricsImpl(string nodeIdOrSlug)
    {
        if (await Resolve(nodeIdOrSlug) is not { } id) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, JsonOpts);
        return JsonSerializer.Serialize(await metrics.ComputeAsync(id), JsonOpts);
    }
}
