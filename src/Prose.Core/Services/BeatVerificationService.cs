using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using System.Text.RegularExpressions;

namespace Prose.Core.Services;

public record BeatVerificationResult(
    Guid BeatId,
    string CheckType,
    string Result,
    string Severity,
    string? Evidence,
    string VerifiedBy = "mechanical");

/// <summary>
/// Beat verification: mechanical quote grounding for audit claims
/// (<see cref="VerifyQuoteGroundingAsync"/>), plus the rule-version staleness report over the
/// <see cref="BeatVerification"/> rows.
///
/// <para>The per-beat blueprint-contract checks (EscalationFloor, EventType, BannedPattern,
/// SubplotCarrier, EscalationMonotonic, DeclaredPurpose) were removed 2026-09-22 with the
/// structural blueprint itself (author ruling: the book is the beats, drawing on entities).</para>
/// </summary>
public class BeatVerificationService
{
    /// <summary>
    /// Bump whenever a check's LOGIC changes — a new threshold, a new outlier gate, a Result
    /// mapping change (mirrors <see cref="BeatChecklistGateService"/>'s PromptVersion). Stamped
    /// onto every <see cref="BeatVerification"/> row on write; <see cref="GetStaleBookSlugsAsync"/>
    /// uses it to answer "which books still have findings computed under old logic" as a direct
    /// query. "v1" here is the outlier-gate + EventType Skip-vs-Partial fix (2026-08-10) — the
    /// first version this project ever stamped; every row without a matching value (including
    /// pre-existing NULL rows from before this column existed) is stale by definition.
    /// </summary>
    public const string CurrentRuleVersion = "v1";

    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<BeatVerificationService> log;

    public BeatVerificationService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<BeatVerificationService> log)
    {
        this.dbFactory  = dbFactory;
        this.log        = log;
    }

    // ── Quote grounding (audit-claim verification) ───────────────────────────
    //
    // Logic-sweep audit agents report findings as quoted text attributed to a SortKey/BeatId.
    // Two real incidents (2026-07-24 VIGL sweep) showed agents can misattribute or fabricate
    // an exact quote — usually by reading the wrong beat under time pressure, not by intent.
    // This check is the mechanical guard: before a claimed finding is trusted for triage/fix,
    // confirm the claimed quote actually appears in the beat it's attributed to. Each
    // quote-grounding call is a distinct historical claim-check, so rows are always INSERTED, never upserted — a beat can accumulate many of these across
    // many sweeps without one overwriting another.

    /// <summary>
    /// Verifies that <paramref name="claimedQuote"/> actually appears in the given beat's text.
    /// Comparison is normalized (dash variants, curly/straight quotes, collapsed whitespace)
    /// so a claim isn't rejected merely because sqlcmd/terminal display altered punctuation —
    /// only genuine misattribution or fabrication fails this check.
    /// </summary>
    public async Task<BeatVerificationResult> VerifyQuoteGroundingAsync(
        Guid beatId, string claimedQuote, string? claimedBy = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct);

        BeatVerificationResult result;
        if (beat == null)
        {
            result = new(beatId, "QuoteGrounding", "Fail", "BLOCKER",
                $"Beat {beatId} does not exist — the claim's SortKey/BeatId is itself wrong.",
                claimedBy ?? "unknown");
        }
        else if (string.IsNullOrWhiteSpace(claimedQuote))
        {
            result = new(beatId, "QuoteGrounding", "Skipped", "BLOCKER", "No quote supplied to check.",
                claimedBy ?? "unknown");
        }
        else
        {
            var normalizedQuote = NormalizeForComparison(ProseInline.StripFormatting(claimedQuote)); // markers never count
            // Strip inline <entity guid="...">Name</entity> tags before comparing — same fix
            // LogicSweepService.QuotedEvidenceAppearsInBeat already applies (2026-08-14): a tag
            // wrapping a proper noun inside a genuinely-quoted span breaks literal Contains()
            // continuity and turns a true, correctly-cited quote into a false "fabricated" verdict.
            // Confirmed live 2026-08-22 (BCODA sweep): "Moss, from an earlier job" and "the catalog
            // value of the Atlas hardware..." both failed this check purely because "Moss"/"Atlas"
            // were entity-tagged in the stored text, not because the quotes were fabricated.
            var normalizedText = NormalizeForComparison(ProseInline.StripFormatting(BeatMarkup.StripEntityTags(beat.Text ?? string.Empty)));
            // StripFormatting too: "*Run.* She ran." quoted as the reader sees it ("Run. She ran.")
            // failed Contains() on the emphasis markers and was stored as a fabricated BLOCKER.
            // Case-insensitive: a re-typed or paraphrase-adjacent quote (e.g. mid-sentence lowercase
            // vs. the beat's actual sentence-initial capital) is not fabrication — same "don't reject
            // over incidental transcription differences" principle as the dash/quote normalization
            // above.
            var found = normalizedQuote.Length > 0 && normalizedText.Contains(normalizedQuote, StringComparison.OrdinalIgnoreCase);

            result = found
                ? new(beatId, "QuoteGrounding", "Pass", "BLOCKER",
                    $"Quote confirmed present in beat (normalized match). Quote: \"{Truncate(claimedQuote, 160)}\"",
                    claimedBy ?? "unknown")
                : new(beatId, "QuoteGrounding", "Fail", "BLOCKER",
                    $"Quote NOT found in this beat's text — likely misattributed to the wrong beat or fabricated. Reject this finding until re-verified. Quote: \"{Truncate(claimedQuote, 160)}\"",
                    claimedBy ?? "unknown");
        }

        db.BeatVerifications.Add(new BeatVerification
        {
            BeatId     = beatId,
            CheckType  = "QuoteGrounding",
            Result     = result.Result,
            Severity   = result.Severity,
            Evidence   = result.Evidence,
            VerifiedAt = DateTime.UtcNow,
            VerifiedBy = result.VerifiedBy,
        });
        await db.SaveChangesAsync(ct);

        if (result.Result == "Fail")
            log.LogWarning("[QuoteGrounding] REJECTED — beat {BeatId} does not contain claimed quote (claimed by {ClaimedBy}): {Evidence}",
                beatId, claimedBy ?? "unknown", result.Evidence);

        return result;
    }

    /// <summary>
    /// Batch form: verify every (BeatId, Quote) claim from an audit report in one pass.
    /// Use this to gate an entire audit report before triage — any Fail means that specific
    /// finding must be re-verified against the real beat before it's acted on.
    /// </summary>
    public async Task<List<BeatVerificationResult>> VerifyQuoteGroundingBatchAsync(
        IEnumerable<(Guid BeatId, string Quote)> claims, string? claimedBy = null, CancellationToken ct = default)
    {
        var results = new List<BeatVerificationResult>();
        foreach (var (beatId, quote) in claims)
            results.Add(await VerifyQuoteGroundingAsync(beatId, quote, claimedBy, ct));
        return results;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    private static string NormalizeForComparison(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s)
        {
            char n = c switch
            {
                '‐' or '‑' or '‒' or '–' or '—' or '―' => '-',
                '‘' or '’' or '‚' or '‛'                        => '\'',
                '“' or '”' or '„' or '‟'                        => '"',
                _ => c,
            };
            sb.Append(n);
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    // ── Staleness reporting ──────────────────────────────────────────────────

    public sealed record StaleBook(string Slug, string Title, int StaleRows, int TotalRows);

    /// <summary>
    /// Every book with at least one enabled beat carrying a <see cref="BeatVerification"/> row
    /// whose <see cref="BeatVerification.RuleVersion"/> doesn't match <see cref="CurrentRuleVersion"/>
    /// (including legacy rows with a null RuleVersion, predating this column). Answers "which
    /// books need an `--audit-book` re-run after a check-logic change" directly —
    /// see the RuleVersion doc comment for why this exists (the same staleness gap was found and
    /// manually re-diffed twice in one session before this method did).
    /// </summary>
    public async Task<List<StaleBook>> GetStaleBookSlugsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // IgnoreQueryFilters — this report is deliberately corpus-wide across every universe,
        // not scoped to whatever universe happens to be ambient for this CLI invocation (see
        // Program.cs's UniverseAgnosticCommands entry for --verification-staleness).
        var rows = await (
            from bv in db.BeatVerifications.AsNoTracking().IgnoreQueryFilters()
            join bn in db.BeatNodes.AsNoTracking().IgnoreQueryFilters() on bv.BeatId equals bn.BeatId
            where true
            select new { bn.NodeId, bv.RuleVersion }
        ).ToListAsync(ct);
        if (rows.Count == 0) return new List<StaleBook>();

        var nodeIds = rows.Select(r => r.NodeId).Distinct().ToList();
        var bookByLeaf = new Dictionary<Guid, Guid>();
        foreach (var leafId in nodeIds)
        {
            // Falls back to the leaf itself when nothing above it is a book, which is what the
            // previous root-walk effectively did for an orphaned node.
            bookByLeaf[leafId] = await NodeWorkbenchService.ResolveBookAncestorIdAsync(db, leafId, ct) ?? leafId;
        }

        var byBook = rows.GroupBy(r => bookByLeaf[r.NodeId]).Select(g => new
        {
            BookId = g.Key,
            Total = g.Count(),
            Stale = g.Count(r => r.RuleVersion != CurrentRuleVersion),
        }).Where(g => g.Stale > 0).ToList();
        if (byBook.Count == 0) return new List<StaleBook>();

        var bookIds = byBook.Select(b => b.BookId).ToList();
        var titles = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => bookIds.Contains(n.Id))
            .Select(n => new { n.Id, n.Slug, n.Title })
            .ToDictionaryAsync(n => n.Id, ct);

        return byBook
            .Where(b => titles.ContainsKey(b.BookId))
            .Select(b => new StaleBook(titles[b.BookId].Slug ?? "", titles[b.BookId].Title ?? "", b.Stale, b.Total))
            .OrderByDescending(b => b.StaleRows)
            .ToList();
    }

    /// <summary>Walks ParentNodeId up from a leaf (chapter) node to its book ancestor (no parent,
    /// or a Collection-kind root) — same walk-up shape as the rest of this service's book-scoping,
    /// inverted (leaf-to-root instead of root-to-leaf via GetLeafDescendantIdsAsync).</summary>
    // Book resolution now lives in NodeWorkbenchService.ResolveBookAncestorIdAsync. The copy that
    // was here walked to the TREE ROOT, so for a book under a series it returned the series and
    // every stale-book grouping below was keyed on the wrong node.
}
