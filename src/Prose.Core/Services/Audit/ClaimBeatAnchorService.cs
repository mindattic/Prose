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

    /// <summary>
    /// Beat text for one book, normalized once, keyed by book slug. Used as the fallback scope
    /// when a claim's snippet is not in the chapter it was recorded from.
    ///
    /// <para><b>Book-wide, never corpus-wide.</b> A claim already belongs to a book, so widening
    /// to that book adds no new class of error — the beat genuinely moved between chapters, which
    /// happens constantly (splits, re-seats, the split-collection restructures). Widening to the
    /// whole corpus would be a different thing entirely: it could anchor a claim to another book's
    /// prose that happens to share a sentence, and a wrong anchor is worse than none.</para>
    /// </summary>
    private sealed record BookBeats(List<(Guid BeatId, string Text)> Beats);

    private async Task<BookBeats?> LoadBookBeatsAsync(
        ProseDbContext db, string? bookSlug, Dictionary<string, BookBeats?> cache, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(bookSlug)) return null;
        if (cache.TryGetValue(bookSlug, out var hit)) return hit;

        // IgnoreQueryFilters: BookSlug names a node that may sit in another universe than the
        // ambient scope, and an explicit identifier must not be filtered into "not found".
        var bookNodeId = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Where(n => n.Slug == bookSlug || n.NodeCode == bookSlug)
            .Select(n => (Guid?)n.Id).FirstOrDefaultAsync(ct);
        if (bookNodeId is null) { cache[bookSlug] = null; return null; }

        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, bookNodeId.Value, ct);
        var rows = await db.BeatNodes.AsNoTracking()
            .Where(bn => leafIds.Contains(bn.NodeId) && bn.Beat != null && bn.Beat.Text != "")
            .Select(bn => new { bn.BeatId, Text = bn.Beat!.Text ?? "" })
            .ToListAsync(ct);

        var loaded = new BookBeats(rows
            .GroupBy(r => r.BeatId)
            .Select(g => (g.Key, NormalizeForMatch(BeatMarkup.StripEntityTags(g.First().Text))))
            .ToList());
        cache[bookSlug] = loaded;
        return loaded;
    }

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
        var bookCache = new Dictionary<string, BookBeats?>(StringComparer.Ordinal);

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

                // Not in its own chapter: the beat may simply have moved (split, re-seat, a
                // split-collection restructure). Fall back to the rest of the BOOK before calling
                // the claim stale — this recovered a real population the chapter-only check was
                // mislabelling, found 2026-09-04 by grepping a "stale" snippet and finding it
                // alive one chapter over.
                if (hits.Count == 0)
                {
                    var book = await LoadBookBeatsAsync(db, claim.BookSlug, bookCache, ct);
                    if (book != null)
                        hits = book.Beats
                            .Where(b => b.Text.Contains(needle, StringComparison.OrdinalIgnoreCase))
                            .Select(b => b.BeatId).Distinct().ToList();
                }

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

    // ── stale-snippet report ─────────────────────────────────────────────────

    public sealed record StaleClaim(
        string ClaimUid, string EntityName, string Predicate, string Object,
        string? BookSlug, int? ChapterNumber, string? ChapterTitle,
        string Status, string Provenance, bool WasAnchored, string Snippet);

    public sealed record StaleReport(int Checked, int Stale, List<StaleClaim> Claims);

    /// <summary>
    /// Every live prose claim whose own recorded snippet no longer appears in the prose it was
    /// read from.
    ///
    /// <para><b>Why these matter more than a backfill statistic.</b> A claim's snippet is what
    /// makes it evidence rather than an assertion. When the snippet is gone, the ledger is holding
    /// a fact the book has stopped saying — and that is the same shape as the defect that started
    /// the whole Story Ledger programme: canon asserting something the prose does not support. The
    /// cause is usually innocent (a beat was edited, split, or replaced after extraction) and the
    /// claim is simply superseded, but the population also contains claims that were wrong when
    /// they were written and were quietly fixed in the prose without anyone telling the ledger.
    /// Nothing distinguishes the two from the outside, which is exactly why they need listing
    /// rather than counting.</para>
    ///
    /// <para>Read-only. Files nothing, resolves nothing, changes no status — deciding that a claim
    /// is superseded rather than fabricated is an author call, and docs/LOGIC.md §4 keeps audits
    /// out of the writing seat.</para>
    /// </summary>
    /// <param name="includeAnchored">Also re-check claims that already carry a beat anchor,
    /// against that specific beat. Default true: an anchored claim whose beat was later rewritten
    /// is precisely the drift worth catching, and it is invisible to the backfill (which only ever
    /// looks at unanchored rows).</param>
    public async Task<StaleReport> FindStaleSnippetsAsync(
        string? bookSlug = null, bool includeAnchored = true, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var q = db.ContinuityClaims.AsNoTracking()
            .Where(c => c.SourceType == "prose"
                     && c.Snippet != null && c.Snippet != ""
                     && c.Status != "REJECTED" && c.Status != "SUPERSEDED");
        if (!string.IsNullOrWhiteSpace(bookSlug)) q = q.Where(c => c.BookSlug == bookSlug);
        if (!includeAnchored) q = q.Where(c => c.SourceBeatId == null);

        var claims = await q.ToListAsync(ct);
        var stale = new List<StaleClaim>();

        // Beat text is read once per chapter and once per anchored beat, then reused — the same
        // "normalize the haystack once, not per needle" shape as the backfill.
        var chapterCache = new Dictionary<Guid, List<string>>();
        var beatCache = new Dictionary<Guid, string>();
        var bookCache = new Dictionary<string, BookBeats?>(StringComparer.Ordinal);

        foreach (var claim in claims)
        {
            ct.ThrowIfCancellationRequested();

            var needle = NormalizeForMatch(claim.Snippet!).Trim('"', '\'');
            if (needle.Length < MinSnippetLength) continue;

            bool found;
            if (claim.SourceBeatId is { } beatId)
            {
                if (!beatCache.TryGetValue(beatId, out var text))
                {
                    var raw = await db.Beats.AsNoTracking()
                        .Where(b => b.Id == beatId).Select(b => b.Text).FirstOrDefaultAsync(ct);
                    text = NormalizeForMatch(BeatMarkup.StripEntityTags(raw ?? ""));
                    beatCache[beatId] = text;
                }
                found = text.Contains(needle, StringComparison.OrdinalIgnoreCase);
            }
            else if (Guid.TryParse(claim.SourceChapterId, out var chapterId))
            {
                if (!chapterCache.TryGetValue(chapterId, out var texts))
                {
                    var raws = await db.BeatNodes.AsNoTracking()
                        .Where(bn => bn.NodeId == chapterId && bn.Beat != null && bn.Beat.Text != "")
                        .Select(bn => bn.Beat!.Text ?? "")
                        .ToListAsync(ct);
                    texts = raws.Select(t => NormalizeForMatch(BeatMarkup.StripEntityTags(t))).ToList();
                    chapterCache[chapterId] = texts;
                }
                found = texts.Any(t => t.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // No chapter scope — fall straight through to the book check below rather than
                // skipping, so a claim with a lost chapter id is still evaluated on its evidence.
                found = false;
            }

            // Same book-wide fallback the backfill uses: prose that moved chapters is not prose
            // that vanished, and calling it stale would put innocent claims in front of an author
            // as suspected fabrications.
            if (!found)
            {
                var book = await LoadBookBeatsAsync(db, claim.BookSlug, bookCache, ct);
                if (book != null)
                    found = book.Beats.Any(b => b.Text.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }

            if (found) continue;
            stale.Add(new StaleClaim(
                claim.ClaimUid, claim.EntityName, claim.Predicate, claim.Object,
                claim.BookSlug, claim.SourceChapterNumber, claim.SourceChapterTitle,
                claim.Status, claim.Provenance, claim.SourceBeatId != null, claim.Snippet!));
        }

        log.LogInformation("[stale-snippets] {Scope}: {Stale}/{Checked} live prose claim(s) can no longer be grounded.",
            bookSlug ?? "corpus-wide", stale.Count, claims.Count);

        return new StaleReport(claims.Count, stale.Count, stale);
    }

    /// <summary>
    /// Moves ungroundable claims to <c>SUPERSEDED</c> — the status that already means "a later
    /// extraction or edit replaced this", which is exactly what happened to them.
    ///
    /// <para><b>Why this is a status change and not a delete.</b> <c>SUPERSEDED</c> rows stay in
    /// the table and stay readable; the table is system-versioned, so the change is reversible.
    /// Nothing about the fact is being judged wrong — only that its evidence is gone, which
    /// disqualifies it from being asserted as canon while leaving the record of it intact.</para>
    ///
    /// <para><b>Guarded by an exact count, deliberately.</b> The caller must pass the number the
    /// report just produced. If the corpus moved between the read and the write — a beat edited,
    /// an extraction run — the count differs and the write refuses rather than superseding a
    /// different set than the one a human looked at. Same discipline as
    /// <c>--orphan-beats --delete --confirm</c>.</para>
    /// </summary>
    public async Task<int> SupersedeStaleAsync(
        string? bookSlug, int expectedCount, string note, CancellationToken ct = default)
    {
        var report = await FindStaleSnippetsAsync(bookSlug, includeAnchored: true, ct);
        if (report.Stale != expectedCount)
            throw new InvalidOperationException(
                $"Refusing to write: {report.Stale} stale claim(s) found, but --confirm said {expectedCount}. " +
                "The corpus changed since the report was read — re-run the report and confirm the new number.");
        if (report.Stale == 0) return 0;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var uids = report.Claims.Select(c => c.ClaimUid).ToHashSet(StringComparer.Ordinal);
        var rows = await db.ContinuityClaims.Where(c => uids.Contains(c.ClaimUid)).ToListAsync(ct);

        var stamp = DateTime.UtcNow.ToString("O");
        foreach (var r in rows)
        {
            r.Status = "SUPERSEDED";
            r.ResolvedAt = stamp;
            r.ResolutionNote = note;
        }
        await db.SaveChangesAsync(ct);

        log.LogWarning("[stale-snippets] Superseded {Count} ungroundable claim(s) in {Scope}.",
            rows.Count, bookSlug ?? "corpus-wide");
        return rows.Count;
    }
}
