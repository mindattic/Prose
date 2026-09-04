using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services.Audit;

/// <summary>
/// Backfills <c>ContinuityClaim.SourceBeatId</c> by finding, mechanically, which beat each claim's
/// recorded snippet actually came from. Deterministic and free — no LLM call, no re-extraction.
///
/// <para><b>The problem it solves, measured.</b> A corpus survey on 2026-09-04 found <b>24 of
/// 24,758</b> live claims carrying a beat anchor — 0.1%. That is the ceiling on everything the
/// Tuned Read can do, and it sat under the ontology work like a floor nobody had checked:</para>
/// <list type="bullet">
/// <item><c>TunedReadService.AdjudicateAsync</c> REFUSES a pair where neither claim has a beat
/// anchor, because adjudicating without prose means ruling on two summaries — the exact
/// paraphrase-only reasoning that invented "Dae-jung Seo" in the first place. So an unanchored
/// pair can never become a finding, however sound the axiom.</item>
/// <item>A temporal axiom (<c>PredicateExclusion.TemporalOrder</c>) cannot order two claims that
/// have no position on the book's clock, so it never fires at all.</item>
/// </list>
/// <para>The consequence is the failure mode this whole programme exists to break: the instrument
/// returns zero findings and that reads as <i>clean</i>, when it actually means <i>could not
/// look</i>. Anchoring is what turns the ledger from a list of assertions back into evidence.</para>
///
/// <para><b>Why a text match is the right instrument and not a shortcut.</b> Every extracted claim
/// already carries a mandatory verbatim <c>Snippet</c>, and the engine already trusts exactly this
/// operation elsewhere: <c>LogicSweepService.QuotedEvidenceAppearsInBeat</c> and
/// <c>TunedReadService.QuoteAppearsIn</c> both decide whether a quote is real by normalizing
/// whitespace and testing containment. This reuses that same test, so an anchor is only written
/// where the claim's own evidence is literally present in exactly one beat. Re-extracting 12,000
/// prose claims to recover a field the snippets already imply would cost real money to learn
/// something the text can be asked for free.</para>
///
/// <para><b>Fails closed, deliberately.</b> A snippet matching several beats is left unanchored
/// rather than assigned to the first hit. A wrong anchor is worse than none: it would put a
/// finding's "beat #N" citation on innocent prose, hand the adjudicator the wrong carrier band,
/// and key the verdict cache to a beat whose text has nothing to do with the claim.</para>
/// </summary>
public sealed class ClaimBeatAnchorService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<ClaimBeatAnchorService> log)
{
    /// <summary>Below this a snippet is not evidence of anything — the same floor
    /// <c>TunedReadService.QuoteAppearsIn</c> applies before it will call a quote grounded.</summary>
    private const int MinSnippetLength = 8;

    public sealed record AnchorReport(
        int Considered, int Anchored, int NoSnippet, int TooShort,
        int NotFound, int Ambiguous, int NoChapterScope, List<string> Notes);

    /// <param name="bookSlug">Restrict to one book's claims. Null = corpus-wide.</param>
    /// <param name="dryRun">Compute and report without writing a single anchor.</param>
    public async Task<AnchorReport> BackfillAsync(
        string? bookSlug = null, bool dryRun = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var notes = new List<string>();

        // Prose claims only. An entity-record or bible claim has no beat to point at — anchoring
        // one would be inventing a provenance it never had.
        var q = db.ContinuityClaims
            .Where(c => c.SourceBeatId == null
                     && c.SourceType == "prose"
                     && c.Status != "REJECTED" && c.Status != "SUPERSEDED");
        if (!string.IsNullOrWhiteSpace(bookSlug)) q = q.Where(c => c.BookSlug == bookSlug);

        var claims = await q.ToListAsync(ct);
        if (claims.Count == 0)
            return new AnchorReport(0, 0, 0, 0, 0, 0, 0,
                ["No unanchored live prose claims in scope — nothing to do."]);

        int anchored = 0, noSnippet = 0, tooShort = 0, notFound = 0, ambiguous = 0, noScope = 0;

        // Group by the chapter the claim was extracted from, so each chapter's beats are read and
        // normalized ONCE rather than per claim. A chapter is typically tens of beats against
        // hundreds of claims.
        foreach (var group in claims.GroupBy(c => c.SourceChapterId))
        {
            ct.ThrowIfCancellationRequested();

            if (!Guid.TryParse(group.Key, out var chapterId))
            {
                // No usable chapter scope. Searching the whole corpus for the snippet instead
                // would make an ambiguous match far likelier, and a wrong anchor is worse than
                // none — so these are reported, not guessed.
                noScope += group.Count();
                continue;
            }

            // No IsEnabled filter: BeatNode carries no such column despite the schema notes, and
            // the same idiom is used by BookHealthService.ProseCheckAsync. A disabled beat would
            // only ever widen the search, and a snippet matching two beats is refused anyway.
            var beats = await db.BeatNodes.AsNoTracking()
                .Where(bn => bn.NodeId == chapterId && bn.Beat != null && bn.Beat.Text != "")
                .OrderBy(bn => bn.SortKey)
                .Select(bn => new { bn.BeatId, Text = bn.Beat!.Text ?? "" })
                .ToListAsync(ct);

            if (beats.Count == 0) { notFound += group.Count(); continue; }

            var normalized = beats
                .Select(b => (b.BeatId, Text: NormalizeForMatch(BeatMarkup.StripEntityTags(b.Text))))
                .ToList();

            foreach (var claim in group)
            {
                if (string.IsNullOrWhiteSpace(claim.Snippet)) { noSnippet++; continue; }

                var needle = NormalizeForMatch(claim.Snippet).Trim('"', '\'');
                if (needle.Length < MinSnippetLength) { tooShort++; continue; }

                var hits = normalized
                    .Where(b => b.Text.Contains(needle, StringComparison.OrdinalIgnoreCase))
                    .Select(b => b.BeatId)
                    .Distinct()
                    .ToList();

                switch (hits.Count)
                {
                    case 0: notFound++; break;
                    case 1:
                        if (!dryRun) claim.SourceBeatId = hits[0];
                        anchored++;
                        break;
                    default: ambiguous++; break;
                }
            }
        }

        if (!dryRun && anchored > 0) await db.SaveChangesAsync(ct);
        else db.ChangeTracker.Clear();

        if (noScope > 0)
            notes.Add($"{noScope} claim(s) carry no parseable SourceChapterId, so there was no scope to " +
                      "search. Corpus-wide snippet search was NOT attempted — a wrong anchor cites innocent " +
                      "prose and poisons the verdict cache, so these stay unanchored until re-extracted.");
        if (ambiguous > 0)
            notes.Add($"{ambiguous} snippet(s) matched more than one beat in their own chapter and were left " +
                      "alone. Usually a short or formulaic snippet; re-extraction is the fix, not a guess.");
        if (notFound > 0)
            notes.Add($"{notFound} snippet(s) no longer appear in their chapter's prose — the beat was edited, " +
                      "split or deleted since extraction. These are stale claims, and worth reading as a " +
                      "signal in their own right rather than only as a backfill miss.");

        log.LogInformation(
            "[anchor-beats] {Scope}: {Anchored}/{Considered} anchored ({Ambiguous} ambiguous, {NotFound} stale, {NoScope} unscoped){Dry}",
            bookSlug ?? "corpus-wide", anchored, claims.Count, ambiguous, notFound, noScope,
            dryRun ? " [DRY RUN — nothing written]" : "");

        return new AnchorReport(
            claims.Count, anchored, noSnippet, tooShort, notFound, ambiguous, noScope, notes);
    }

    /// <summary>Whitespace-normalized for containment testing — byte-identical in behaviour to
    /// <c>TunedReadService.QuoteAppearsIn</c>'s normalization, so a snippet that anchors here is a
    /// snippet that will ground there.</summary>
    private static string NormalizeForMatch(string s) => Regex.Replace(s, @"\s+", " ").Trim();
}
