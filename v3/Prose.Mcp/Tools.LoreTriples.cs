using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Interfaces;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Continuity claim tooling ───────────────────────────────────────────────
// In-process MCP wrapper over the unified ContinuityService — extracts atomic
// (entity, predicate, object) claims from chapter prose or entity records,
// surfaces contradictions, resolves them, and applies the agreed value back
// to the entity record (Legion picks the field).
//
// Replaces the Node-shellout `extract_facts` tool. Same data store as the
// /continuity UI; one source of truth per session.

/// <summary>
/// In-process MCP wrapper over the unified <c>ContinuityService</c>. Extracts
/// atomic (entity, predicate, object) claims from chapter prose or entity records,
/// surfaces same-(entity,predicate)-different-object contradictions, lets the
/// caller resolve them, and applies the agreed value back to the entity record
/// (Legion picks the field). Same data store as the /continuity UI; one source of
/// truth per session.
/// </summary>
[McpServerToolType]
public class LoreTripleTools
{
    private readonly ContinuityService store;
    private readonly ContinuityExtractionService extraction;
    private readonly ContinuityApplyService apply;
    private readonly IBookRepository books;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly HubInvoker hub;

    public LoreTripleTools(
        ContinuityService store,
        ContinuityExtractionService extraction,
        ContinuityApplyService apply,
        IBookRepository books,
        IDbContextFactory<ProseDbContext> dbFactory,
        HubInvoker hub)
    {
        this.store      = store;
        this.extraction = extraction;
        this.apply      = apply;
        this.books      = books;
        this.dbFactory  = dbFactory;
        this.hub        = hub;
    }

    /// <summary>
    /// Extract atomic continuity claims (entity, predicate, object triples) from a
    /// chapter's prose. Each triple's snippet is validated against
    /// the source prose; survivors are upserted into the unified continuity store.
    /// Same-(entity,predicate) with different `object` auto-flags a contradiction.
    /// Returns: new / confirmed / contradicted counts.
    /// </summary>
    [McpServerTool, Description(
        "Extract atomic continuity claims (entity, predicate, object triples) from a chapter's prose. " +
        "Each triple's snippet is validated against the source prose; survivors are " +
        "upserted into the unified continuity store. Same-(entity,predicate) with different `object` " +
        "auto-flags a contradiction. Returns: new / confirmed / contradicted counts. ok=true when no new contradictions surfaced.")]
    public Task<string> ExtractContinuityFromChapter(
        [Description("Chapter id (32-char hex).")]
            string chapterId,
        [Description("Max tokens for the extraction response. Default 4096.")]
            int maxTokens = 4096) =>
        hub.InvokeAsync(nameof(LoreTripleTools), nameof(ExtractContinuityFromChapterImpl), new { chapterId, maxTokens });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ExtractContinuityFromChapterImpl(string chapterId, int maxTokens = 4096)
    {
        try
        {
            var r = await extraction.ExtractFromChapterAsync(chapterId, maxTokens);
            var ok = r.ContradictedClaims == 0;
            return JsonSerializer.Serialize(new
            {
                ok,
                contradictions = r.ContradictedClaims,
                report = r,
            }, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "extract_chapter_failed", detail = ex.Message }, CanonTools.JsonOpts);
        }
    }

    /// <summary>
    /// Extract continuity claims from every chapter in a book sequentially.
    /// Long-running; returns per-chapter results plus aggregate counts.
    /// </summary>
    [McpServerTool, Description(
        "Extract continuity claims from every chapter in a book (sequential — long-running). " +
        "Returns per-chapter results plus aggregate counts.")]
    public Task<string> ExtractContinuityFromBook(
        [Description("Book id (32-char hex).")]
            string bookId,
        [Description("Max tokens for the extraction response, per chapter. Default 4096.")]
            int maxTokens = 4096) =>
        hub.InvokeAsync(nameof(LoreTripleTools), nameof(ExtractContinuityFromBookImpl), new { bookId, maxTokens });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ExtractContinuityFromBookImpl(string bookId, int maxTokens = 4096)
    {
        try
        {
            var book = books.LoadBook(bookId);
            if (book == null) return JsonSerializer.Serialize(new { error = "book_not_found", bookId }, CanonTools.JsonOpts);
            var rs = await extraction.ExtractFromBookAsync(book, maxTokens);
            var totals = new
            {
                @new          = rs.Sum(r => r.NewClaims),
                confirmed     = rs.Sum(r => r.ConfirmedClaims),
                contradicted  = rs.Sum(r => r.ContradictedClaims),
                chapters      = rs.Count,
            };
            return JsonSerializer.Serialize(new
            {
                ok = totals.contradicted == 0,
                totals,
                by_chapter = rs,
            }, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "extract_book_failed", detail = ex.Message }, CanonTools.JsonOpts);
        }
    }

    /// <summary>
    /// Extract continuity claims from a book's story bible instead of its prose — the third leg
    /// of the Bible/Book/Entities validation triangle. Lands with SourceType="bible" in the same
    /// ledger prose/entity-record claims use, so a stale bible fact and a solid prose fact on the
    /// same (entity, predicate) compete and surface a contradiction automatically.
    /// </summary>
    [McpServerTool, Description(
        "Extract continuity claims from a book's story bible (prefers the NodeBibleSections " +
        "'Characters' section — settled character-sheet facts, not plot-forward arc/spine content " +
        "— falling back to the raw NodeBible blob). Claims land with SourceType=\"bible\" in the " +
        "same ledger chapter-prose and entity-record extraction already populate, so a bible fact " +
        "and a prose fact on the same (entity, predicate) compete/reconcile automatically — this " +
        "is how the Bible gets validated against (and validates) the actual prose and the entity repo.")]
    public Task<string> ExtractContinuityFromBible(
        [Description("Book/series node id (guid) or slug/NodeCode.")]
            string nodeIdOrSlug,
        [Description("NodeBibleSections section to prefer: Characters (default, settled fact) | ArcSummary | VoiceRegister | NarrativeLocks | BeatSpine. Falls back to the raw NodeBible blob if the section doesn't exist yet.")]
            string sectionType = "Characters",
        [Description("Max tokens for the extraction response. Default 8192 — higher than chapter extraction's 4096, since a book's whole character roster commonly produces a larger fact list than a single beat/chapter does.")]
            int maxTokens = 8192) =>
        hub.InvokeAsync(nameof(LoreTripleTools), nameof(ExtractContinuityFromBibleImpl), new { nodeIdOrSlug, sectionType, maxTokens });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ExtractContinuityFromBibleImpl(string nodeIdOrSlug, string sectionType = "Characters", int maxTokens = 8192)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            Guid nodeId;
            if (!Guid.TryParse(nodeIdOrSlug, out nodeId))
            {
                var found = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Slug == nodeIdOrSlug || n.NodeCode == nodeIdOrSlug);
                if (found == null) return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);
                nodeId = found.Id;
            }
            var r = await extraction.ExtractFromBibleAsync(nodeId, sectionType, maxTokens);
            if (r.Error != null) return JsonSerializer.Serialize(new { error = "extract_bible_failed", detail = r.Error }, CanonTools.JsonOpts);
            return JsonSerializer.Serialize(new { ok = r.ContradictedClaims == 0, report = r }, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "extract_bible_failed", detail = ex.Message }, CanonTools.JsonOpts);
        }
    }

    /// <summary>
    /// Extract continuity claims from a single entity record. Top-level scalar
    /// fields become direct claims; prose fields (description, personality,
    /// ideology…) go through the same single-call extraction as chapter prose.
    /// </summary>
    [McpServerTool, Description(
        "Extract continuity claims from a single entity record by EntityId (canonical Records.Json blob in SQL). " +
        "Top-level scalar fields become direct claims; prose fields (description, personality, ideology…) " +
        "go through the same single-call extraction as chapter prose.")]
    public Task<string> ExtractContinuityFromEntityRecord(
        [Description("EntityId (guid, hyphenated or 32-char hex) of the canon entity to extract from.")]
            string entityId) =>
        hub.InvokeAsync(nameof(LoreTripleTools), nameof(ExtractContinuityFromEntityRecordImpl), new { entityId });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ExtractContinuityFromEntityRecordImpl(string entityId)
    {
        try
        {
            if (!Guid.TryParse(entityId, out var id)
                && !(entityId.Length == 32 && Guid.TryParseExact(entityId, "N", out id)))
            {
                return JsonSerializer.Serialize(new { error = "extract_entity_record_failed", detail = $"unparseable entityId '{entityId}'" }, CanonTools.JsonOpts);
            }
            var r = await extraction.ExtractFromEntityRecordAsync(id);
            return JsonSerializer.Serialize(new { ok = r.ContradictedClaims == 0, report = r }, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "extract_entity_record_failed", detail = ex.Message }, CanonTools.JsonOpts);
        }
    }

    /// <summary>
    /// List continuity claims. Optional filters: entity (id or name) and status
    /// (NEW | CONFIRMED | CONTRADICTED | CANONICAL | REJECTED | SUPERSEDED).
    /// </summary>
    [McpServerTool, Description(
        "List continuity claims. Optional filters: entity (id or name), status (NEW | CONFIRMED | CONTRADICTED | CANONICAL | REJECTED | SUPERSEDED). " +
        "Returns the claims with their predicates, objects, sources, and statuses.")]
    public Task<string> GetContinuityClaims(
        [Description("Optional: entity name to filter to one entity.")]
            string entity = "",
        [Description("Optional: status filter.")]
            string status = "") =>
        hub.InvokeAsync(nameof(LoreTripleTools), nameof(GetContinuityClaimsImpl), new { entity, status });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string GetContinuityClaimsImpl(string entity = "", string status = "")
    {
        IEnumerable<ContinuityClaim> result = !string.IsNullOrWhiteSpace(status)
            ? store.GetByStatus(status)
            : new[] { "NEW", "CONFIRMED", "CONTRADICTED", "CANONICAL" }.SelectMany(store.GetByStatus);

        if (!string.IsNullOrWhiteSpace(entity))
            result = result.Where(c => c.EntityName.Equals(entity, StringComparison.OrdinalIgnoreCase) || c.EntityId == entity);

        var list = result.ToList();
        return JsonSerializer.Serialize(new { count = list.Count, claims = list }, CanonTools.JsonOpts);
    }

    /// <summary>
    /// List every CONTRADICTED claim awaiting resolution. Each entry is a pair
    /// (A, B) sharing (entity, predicate) with different object values. Use
    /// ResolveContinuityContradiction to pick a winner.
    /// </summary>
    [McpServerTool, Description(
        "List every CONTRADICTED claim awaiting resolution. Each entry is a pair (A, B) where A and B share " +
        "(entity, predicate) but have different `object` values. Use ResolveContinuityContradiction to pick a winner.")]
    public Task<string> ListContinuityContradictions() =>
        hub.InvokeAsync(nameof(LoreTripleTools), nameof(ListContinuityContradictionsImpl), new { });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string ListContinuityContradictionsImpl()
    {
        var pairs = store.GetContradictions();
        return JsonSerializer.Serialize(new
        {
            ok = pairs.Count == 0,
            count = pairs.Count,
            pairs = pairs.Select(p => new { a = p.A, b = p.B, key = p.Key }),
        }, CanonTools.JsonOpts);
    }

    /// <summary>
    /// Resolve a contradiction. Winner = A | B (winner becomes CANONICAL, loser
    /// becomes REJECTED) or "custom" (both rejected, a writer-asserted CANONICAL
    /// claim takes their place via customObject).
    /// </summary>
    [McpServerTool, Description(
        "Resolve a contradiction. Winner = A | B (one claim wins → CANONICAL, the other → REJECTED) or " +
        "`custom` (both rejected, a new writer-asserted CANONICAL claim takes their place; pass customObject).")]
    public Task<string> ResolveContinuityContradiction(
        [Description("Claim A uid.")] string aUid,
        [Description("Claim B uid (must belong to same entity as A).")] string bUid,
        [Description("Winner: A | B | custom.")] string winner,
        [Description("Required when winner=custom: the agreed value.")] string customObject = "",
        [Description("Optional resolution note (kept in audit trail).")] string note = "") =>
        hub.InvokeAsync(nameof(LoreTripleTools), nameof(ResolveContinuityContradictionImpl), new { aUid, bUid, winner, customObject, note });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string ResolveContinuityContradictionImpl(string aUid, string bUid, string winner, string customObject = "", string note = "")
    {
        try
        {
            var r = store.Resolve(aUid, bUid, winner, customObject, note);
            return JsonSerializer.Serialize(new { ok = true, winner = r.Winner, loser = r.Loser, loser2 = r.Loser2 }, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "resolve_failed", detail = ex.Message }, CanonTools.JsonOpts);
        }
    }

    /// <summary>
    /// Apply a CANONICAL or CONFIRMED claim to its entity record file. Legion's
    /// panel picks which field should hold the value (string fields are set,
    /// arrays are appended to, otherwise the claim lands in a continuity_facts[]
    /// array). The audit trail records which field was chosen.
    /// </summary>
    [McpServerTool, Description(
        "Apply a CANONICAL or CONFIRMED claim to its entity record file. Legion's panel picks which field " +
        "should hold the value (string fields are set, array fields are appended to, otherwise the claim is " +
        "appended to a continuity_facts[] array). The audit trail records which field was chosen.")]
    public Task<string> ApplyContinuityClaim(
        [Description("Claim uid to apply.")] string claimUid) =>
        hub.InvokeAsync(nameof(LoreTripleTools), nameof(ApplyContinuityClaimImpl), new { claimUid });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ApplyContinuityClaimImpl(string claimUid)
    {
        try
        {
            var r = await apply.ApplyAsync(claimUid);
            return JsonSerializer.Serialize(r, CanonTools.JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "apply_failed", detail = ex.Message }, CanonTools.JsonOpts);
        }
    }

}
