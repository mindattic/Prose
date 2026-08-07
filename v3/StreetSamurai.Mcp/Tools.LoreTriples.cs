using System.ComponentModel;
using System.Text.Json;
using MindAttic.Legion;
using ModelContextProtocol.Server;
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

    public LoreTripleTools(
        ContinuityService store,
        ContinuityExtractionService extraction,
        ContinuityApplyService apply,
        IBookRepository books)
    {
        this.store      = store;
        this.extraction = extraction;
        this.apply      = apply;
        this.books      = books;
    }

    /// <summary>
    /// Extract atomic continuity claims (entity, predicate, object triples) from a
    /// chapter's prose via Legion Quorum. Each triple's snippet is validated against
    /// the source prose; survivors are upserted into the unified continuity store.
    /// Same-(entity,predicate) with different `object` auto-flags a contradiction.
    /// Returns: new / confirmed / contradicted counts.
    /// </summary>
    [McpServerTool, Description(
        "Extract atomic continuity claims (entity, predicate, object triples) from a chapter's prose " +
        "via Legion Quorum. Each triple's snippet is validated against the source prose; survivors are " +
        "upserted into the unified continuity store. Same-(entity,predicate) with different `object` " +
        "auto-flags a contradiction. Returns: new / confirmed / contradicted counts. ok=true when no new contradictions surfaced.")]
    public async Task<string> ExtractContinuityFromChapter(
        [Description("Chapter id (32-char hex).")]
            string chapterId,
        [Description("Quorum: plurality | simplemajority | twothirds | unanimous. Default plurality.")]
            string quorum = "plurality",
        [Description("Max tokens per voter response. Default 4096.")]
            int maxTokens = 4096,
        [Description("Minimum voters that must propose a claim for it to be stored. Default 1.")]
            int minVoters = 1)
    {
        try
        {
            var q = ParseQuorum(quorum);
            var r = await extraction.ExtractFromChapterAsync(chapterId, q, maxTokens, minVoters);
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
    public async Task<string> ExtractContinuityFromBook(
        [Description("Book id (32-char hex).")]
            string bookId,
        [Description("Quorum: plurality | simplemajority | twothirds | unanimous. Default plurality.")]
            string quorum = "plurality",
        [Description("Max tokens per voter response. Default 4096.")]
            int maxTokens = 4096,
        [Description("Minimum voters that must propose a claim for it to be stored. Default 1.")]
            int minVoters = 1)
    {
        try
        {
            var book = books.LoadBook(bookId);
            if (book == null) return JsonSerializer.Serialize(new { error = "book_not_found", bookId }, CanonTools.JsonOpts);
            var q = ParseQuorum(quorum);
            var rs = await extraction.ExtractFromBookAsync(book, q, maxTokens, minVoters);
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
    /// Extract continuity claims from a single entity record. Top-level scalar
    /// fields become direct claims; prose fields (description, personality,
    /// ideology…) go through the same Legion Quorum vote as chapter prose.
    /// </summary>
    [McpServerTool, Description(
        "Extract continuity claims from a single entity record by EntityId (canonical Records.Json blob in SQL). " +
        "Top-level scalar fields become direct claims; prose fields (description, personality, ideology…) " +
        "go through the same Legion Quorum vote as chapter prose.")]
    public async Task<string> ExtractContinuityFromEntityRecord(
        [Description("EntityId (guid, hyphenated or 32-char hex) of the canon entity to extract from.")]
            string entityId)
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
    public string GetContinuityClaims(
        [Description("Optional: entity name to filter to one entity.")]
            string entity = "",
        [Description("Optional: status filter.")]
            string status = "")
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
    public string ListContinuityContradictions()
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
    public string ResolveContinuityContradiction(
        [Description("Claim A uid.")] string aUid,
        [Description("Claim B uid (must belong to same entity as A).")] string bUid,
        [Description("Winner: A | B | custom.")] string winner,
        [Description("Required when winner=custom: the agreed value.")] string customObject = "",
        [Description("Optional resolution note (kept in audit trail).")] string note = "")
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
    public async Task<string> ApplyContinuityClaim(
        [Description("Claim uid to apply.")] string claimUid)
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

    private static Quorum ParseQuorum(string q) => (q ?? "").ToLowerInvariant() switch
    {
        "simplemajority" => Quorum.SimpleMajority,
        "twothirds"      => Quorum.TwoThirds,
        "unanimous"      => Quorum.Unanimous,
        _                => Quorum.Plurality,
    };
}
