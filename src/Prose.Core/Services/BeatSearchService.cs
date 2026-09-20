using System.Text;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>One beat containing the searched-for text.</summary>
/// <param name="Excerpt">Reader-visible text around the first match, with entity markup stripped.</param>
/// <param name="MatchStart">Where the match begins inside <paramref name="Excerpt"/>, so the UI can
/// highlight it without searching the excerpt again and finding a different occurrence.</param>
public sealed record BeatSearchHit(
    Guid BeatId,
    int Number,
    string? ChapterTitle,
    string? PlaceName,
    string Excerpt,
    int MatchStart,
    int MatchLength,
    int MatchesInBeat);

/// <param name="Scanned">How many beats were actually looked at. Zero here means the search could
/// not look, which is a different answer from "found nothing" and must not read as one.</param>
/// <param name="Truncated">More beats matched than were returned.</param>
public sealed record BeatSearchResult(
    IReadOnlyList<BeatSearchHit> Hits,
    int Scanned,
    int TotalMatches,
    bool Truncated);

/// <summary>
/// How fresh the semantic index is for a book.
/// </summary>
/// <remarks>
/// Reported rather than assumed. A semantic search over a stale or empty index returns a short
/// list of plausible rows and looks exactly like a search that worked — on the gutenberg fixtures
/// a k=400 sweep returned twelve rows out of 116. An instrument that can silently answer "nothing
/// here" when it means "I could not look" is worse than no instrument.
/// </remarks>
public sealed record SearchIndexCoverage(int Indexed, int Total)
{
    public bool Complete => Total > 0 && Indexed >= Total;
    public bool Empty => Indexed == 0;
}

/// <summary>
/// Finding a passage in a book.
///
/// <para><b>Why this is a service and not a console command.</b> <c>prose --grep-beats</c> loads
/// every beat in the database and prints console text, which is useless to the editor, to the
/// Discuss panel's <c>find_in_book</c> tool, and to a spoken "find the camphor line". This is the
/// same search, book-scoped, returning rows.</para>
///
/// <para><b>Lexical, and honest about it.</b> The semantic tier is deliberately not wired in here:
/// beat embeddings are not maintained on write, so a semantic search silently serves whatever
/// stale subset the index happens to hold. <see cref="CoverageAsync"/> exists so a caller can SAY
/// that instead of quietly returning less than the book contains.</para>
///
/// <para>Searches reader-visible text, not stored markup — the same rule anchors follow. A search
/// for "Kyle" must not depend on whether that instance happens to be wrapped in an entity tag, and
/// must never match a guid.</para>
/// </summary>
public sealed class BeatSearchService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeWorkbenchService workbench)
{
    private const int ExcerptRadius = 70;

    public async Task<BeatSearchResult> SearchAsync(
        Guid bookNodeId,
        string query,
        bool matchCase = false,
        bool wholeWord = false,
        int max = 200,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return new BeatSearchResult([], 0, 0, false);

        var ordered = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);
        if (ordered.Count == 0) return new BeatSearchResult([], 0, 0, false);

        var titles = await ChapterTitlesAsync(ordered.Select(o => o.NodeId).Distinct(), ct);

        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var hits = new List<BeatSearchHit>();
        var total = 0;
        var matchedBeats = 0;

        foreach (var o in ordered)
        {
            ct.ThrowIfCancellationRequested();

            var text = BeatMarkup.StripEntityTags(o.Beat.Text ?? "");
            if (text.Length == 0) continue;

            var occurrences = BeatSearchText.Count(text, query, comparison, wholeWord, out var first);
            if (occurrences == 0) continue;

            total += occurrences;
            matchedBeats++;
            // Counting continues past the cap so the author is told how much they are not seeing.
            if (hits.Count >= max) continue;

            var (excerpt, matchStart) = BeatSearchText.Excerpt(text, first, query.Length, ExcerptRadius);

            hits.Add(new BeatSearchHit(
                o.Beat.Id,
                o.Beat.Number,
                titles.GetValueOrDefault(o.NodeId),
                o.Beat.PlaceName,
                excerpt,
                MatchStart: matchStart,
                MatchLength: query.Length,
                MatchesInBeat: occurrences));
        }

        return new BeatSearchResult(hits, ordered.Count, total, Truncated: matchedBeats > hits.Count);
    }

    /// <summary>
    /// How much of this book the semantic index actually covers.
    ///
    /// <para>Call it before offering semantic search, and show the answer. "0 of 521 beats are
    /// indexed" is a usable fact; an empty result list from a stub index is a lie.</para>
    /// </summary>
    public async Task<SearchIndexCoverage> CoverageAsync(Guid bookNodeId, CancellationToken ct = default)
    {
        var ordered = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);
        if (ordered.Count == 0) return new SearchIndexCoverage(0, 0);

        var beatIds = ordered.Select(o => o.Beat.Id).ToList();

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var indexed = await db.ProseEmbeddings.AsNoTracking()
            .Where(e => e.ScopeKind == "beat" && beatIds.Contains(e.ScopeId))
            .Select(e => e.ScopeId)
            .Distinct()
            .CountAsync(ct);

        return new SearchIndexCoverage(indexed, ordered.Count);
    }

    private async Task<Dictionary<Guid, string>> ChapterTitlesAsync(
        IEnumerable<Guid> nodeIds, CancellationToken ct)
    {
        var ids = nodeIds.ToList();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters: these are already-resolved ids from the book's own walk, so the
        // ambient universe scope is irrelevant and applying it would blank the titles of any book
        // outside whatever universe happens to be pinned.
        return await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .Where(n => ids.Contains(n.Id))
            .ToDictionaryAsync(n => n.Id, n => n.Title ?? "", ct);
    }

}

/// <summary>
/// The matching itself, separated out so it can be tested without a database.
///
/// <para>This is where a search goes quietly wrong: an off-by-one in the excerpt offset highlights
/// the wrong word and the author trusts it, and a word-boundary rule that treats an apostrophe as
/// punctuation makes a whole-word search for "Kyle" match "Kyle's" — or not — with no way to tell
/// which from the result list.</para>
/// </summary>
public static class BeatSearchText
{
    /// <summary>
    /// Occurrences of <paramref name="query"/> in <paramref name="text"/>, and where the first is.
    /// </summary>
    /// <param name="first">-1 when there are none.</param>
    public static int Count(
        string text, string query, StringComparison comparison, bool wholeWord, out int first)
    {
        first = -1;
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query)) return 0;

        var count = 0;
        var at = 0;

        while (at <= text.Length - query.Length)
        {
            var found = text.IndexOf(query, at, comparison);
            if (found < 0) break;

            if (!wholeWord || IsWholeWord(text, found, query.Length))
            {
                if (first < 0) first = found;
                count++;
            }
            // Advance past the START of the match, not its end: overlapping occurrences of a
            // repeated phrase are still separate places the author may want to look at.
            at = found + 1;
        }

        return count;
    }

    /// <summary>
    /// A window of text around a match, and where the match sits inside that window.
    /// </summary>
    /// <returns>The excerpt, and the match's offset within it — so a caller can highlight without
    /// searching the excerpt again and landing on a different occurrence.</returns>
    public static (string Excerpt, int MatchStart) Excerpt(
        string text, int matchAt, int matchLength, int radius)
    {
        if (matchAt < 0) return (text, 0);

        var from = Math.Max(0, matchAt - radius);
        var to = Math.Min(text.Length, matchAt + matchLength + radius);

        // The ellipsis is part of the excerpt, so it shifts the offset. Getting this wrong puts
        // the highlight one character to the left on every truncated hit in the book.
        var lead = from > 0 ? "…" : "";
        var trail = to < text.Length ? "…" : "";

        return (lead + text[from..to].Replace('\n', ' ') + trail, lead.Length + (matchAt - from));
    }

    /// <summary>
    /// Whether a match stands alone as a word.
    ///
    /// <para>An apostrophe counts as part of a word, deliberately: whole-word "Kyle" should not
    /// match inside "Kyle's", because a novelist searching for a bare name is usually looking for
    /// the places it is NOT possessive.</para>
    /// </summary>
    public static bool IsWholeWord(string text, int at, int length)
    {
        var before = at > 0 ? text[at - 1] : ' ';
        var after = at + length < text.Length ? text[at + length] : ' ';
        return !IsWordChar(before) && !IsWordChar(after);

        static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '\'' || c == '’';
    }
}
