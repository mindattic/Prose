using System.Text;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services.Discussion;

/// <summary>Where else in the book a distinctive term from the selection turns up.</summary>
/// <param name="BeatCount">How many other beats contain it.</param>
/// <param name="PlaceCount">How many distinct places those beats are set in — the number that
/// separates a held detail from a regurgitated one.</param>
public sealed record EchoHit(
    string Term,
    int BeatCount,
    int PlaceCount,
    IReadOnlyList<(Guid BeatId, int Number, string? Place)> Samples);

/// <summary>One beat whose prose contains a searched-for phrase.</summary>
public sealed record BookSearchHit(Guid BeatId, int Number, string? Place, string Excerpt);

/// <summary>Everything the assistant is told before it answers a question about a span.</summary>
public sealed record DiscussionContext(
    string BookTitle,
    string BeatLabel,
    int? BeatNumber,
    string? PlaceName,
    string? Intent,
    string BeatText,
    string Selection,
    IReadOnlyList<string> Before,
    IReadOnlyList<string> After,
    IReadOnlyList<EchoHit> Echoes,
    CarriedWeight? Carried);

/// <summary>
/// Assembles the evidence for a discussion.
///
/// <para><b>The echo check is computed, not generated.</b> "This detail also appears in eight other
/// beats across seven locations" is a fact about the corpus, and a model asked to recall it would
/// guess. So the search runs first, in SQL, and its results are handed to the model as given —
/// and shown to the author as their own blocks, clickable, rather than paraphrased in prose.</para>
///
/// <para>This is the accountability test stated directly: a held detail attaches to one referent,
/// a regurgitated one attaches to many. Camphor in five beats of a single building is doing its
/// job; ozone in nine beats across eight locations is the defect. Hence
/// <see cref="EchoHit.PlaceCount"/> beside the beat count — the raw frequency alone cannot tell
/// those apart.</para>
/// </summary>
public sealed class DiscussionContextBuilder(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeWorkbenchService workbench,
    BeatSearchService search,
    RamificationService ramifications)
{
    private const int NeighbourCount = 2;

    public async Task<DiscussionContext> BuildAsync(
        Guid bookNodeId, Guid beatId, string selection, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var bookTitle = await db.Nodes.AsNoTracking()
            .Where(n => n.Id == bookNodeId).Select(n => n.Title).FirstOrDefaultAsync(ct) ?? "(untitled)";

        var ordered = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);
        var index = ordered.FindIndex(o => o.Beat.Id == beatId);
        var beat = index >= 0 ? ordered[index].Beat : null;

        var before = new List<string>();
        var after = new List<string>();
        if (index >= 0)
        {
            for (var i = Math.Max(0, index - NeighbourCount); i < index; i++)
                before.Add(Describe(ordered[i].Beat));
            for (var i = index + 1; i < Math.Min(ordered.Count, index + 1 + NeighbourCount); i++)
                after.Add(Describe(ordered[i].Beat));
        }

        var echoes = await FindEchoesAsync(ordered, beatId, selection, ct);

        // What this passage is CARRYING. Until this existed the assistant was asked whether a
        // passage was load-bearing while being shown the beat, its two neighbours and some word
        // counts — it could not see a single obligation or recorded fact, so it guessed, and
        // a confident guess is exactly the blind agreement the author does not want. Every read
        // behind this is free, which is why it can run on every question.
        CarriedWeight? carried = null;
        try { carried = await ramifications.CarriedByAsync(bookNodeId, beatId, ct); }
        catch (Exception) { /* the prompt says so below rather than pretending nothing is carried */ }

        return new DiscussionContext(
            BookTitle: bookTitle,
            BeatLabel: beat is null ? "(unknown beat)" : $"Beat #{beat.Number}",
            BeatNumber: beat?.Number,
            PlaceName: beat?.PlaceName,
            Intent: beat?.Description,
            BeatText: BeatMarkup.StripEntityTags(beat?.Text ?? ""),
            Selection: selection,
            Before: before,
            After: after,
            Echoes: echoes,
            Carried: carried);
    }

    private static string Describe(Data.Entities.Beat b)
    {
        return $"#{b.Number}: {Shorten(BeatMarkup.StripEntityTags(b.Text ?? ""), 160)}";
    }

    /// <summary>
    /// For each distinctive term in the selection, which other beats of this book contain it.
    /// One pass over beats already in memory — no second query, and no LLM call.
    /// </summary>
    private Task<IReadOnlyList<EchoHit>> FindEchoesAsync(
        List<NodeWorkbenchService.OrderedBeat> ordered,
        Guid beatId,
        string selection,
        CancellationToken ct)
    {
        var terms = SalientTerms.Extract(selection);
        var hits = new List<EchoHit>();

        // The reader's text, once per beat. Searching the stored markup counted tag attributes as
        // echoes: a selection containing "character" or "weapon" matched every beat carrying an
        // <entity repo="character" …> link, and the model was told the word was everywhere.
        var readable = ordered
            .Where(o => o.Beat.Id != beatId && !string.IsNullOrEmpty(o.Beat.Text))
            .DistinctBy(o => o.Beat.Id)
            .Select(o => (o.Beat, Plain: BeatDiscussTarget.PlainText(o.Beat.Text)))
            .ToList();

        foreach (var term in terms)
        {
            ct.ThrowIfCancellationRequested();

            var samples = new List<(Guid, int, string?)>();
            var places = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var count = 0;

            foreach (var (beat, plain) in readable)
            {
                if (plain.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0) continue;

                count++;
                places.Add(beat.PlaceName ?? "(unplaced)");
                if (samples.Count < 6) samples.Add((beat.Id, beat.Number, beat.PlaceName));
            }

            if (samples.Count > 0)
                hits.Add(new EchoHit(term, count, places.Count, samples));
        }

        // Loudest first: the many-places case is the one worth the author's attention.
        return Task.FromResult<IReadOnlyList<EchoHit>>(
            hits.OrderByDescending(h => h.PlaceCount).ThenByDescending(h => h.BeatCount).ToList());
    }

    private static string Shorten(string s, int max)
        => s.Length <= max ? s : s[..max].TrimEnd() + "…";

    /// <summary>
    /// Every beat of this book whose prose contains <paramref name="query"/>.
    ///
    /// <para>Backs the <c>find_in_book</c> tool, so the assistant can go and look instead of
    /// recalling. The search itself is <see cref="BeatSearchService"/> — the same one behind the
    /// editor's Find, deliberately, because the assistant and the author asking "is this anywhere
    /// else" must not be able to get different answers.</para>
    ///
    /// <para>Lexical and book-scoped. The corpus-wide <c>prose --grep-beats</c> loads every beat
    /// in the database and returns console text, and the semantic tier cannot be trusted until the
    /// beat embedding index is maintained on write (RFC 0013 — a k=400 sweep over gutenberg
    /// returned twelve rows of 116).</para>
    /// </summary>
    public async Task<IReadOnlyList<BookSearchHit>> FindInBookAsync(
        Guid bookNodeId, string query, int max = 12, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var found = await search.SearchAsync(bookNodeId, query, max: max, ct: ct);
        return found.Hits
            .Select(h => new BookSearchHit(h.BeatId, h.Number, h.PlaceName, h.Excerpt))
            .ToList();
    }

    /// <summary>Renders the context into the block the model is given.</summary>
    public static string ToPrompt(DiscussionContext c)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"BOOK: {c.BookTitle}");
        sb.AppendLine($"BEAT: {c.BeatLabel}{(c.PlaceName is null ? "" : $" — {c.PlaceName}")}");
        if (!string.IsNullOrWhiteSpace(c.Intent))
            sb.AppendLine($"STATED INTENT: {c.Intent}");

        if (c.Before.Count > 0)
        {
            sb.AppendLine().AppendLine("BEATS BEFORE:");
            foreach (var b in c.Before) sb.AppendLine("  " + b);
        }
        if (c.After.Count > 0)
        {
            sb.AppendLine().AppendLine("BEATS AFTER:");
            foreach (var b in c.After) sb.AppendLine("  " + b);
        }

        sb.AppendLine().AppendLine("FULL BEAT TEXT:").AppendLine(c.BeatText);
        sb.AppendLine().AppendLine("THE SELECTED PASSAGE:").AppendLine(c.Selection);

        sb.AppendLine();
        if (c.Carried is { } carried)
        {
            sb.Append(RamificationService.ToPrompt(carried));
        }
        else
        {
            // "Could not look" and "nothing to find" are the same output unless one of them says
            // so. An assistant told nothing is carried will happily agree to anything.
            sb.AppendLine("CARRIED BY THIS PASSAGE: the record could not be read. Do NOT treat this "
                          + "as the passage being free of commitments — say that you could not check.");
        }

        if (c.Echoes.Count > 0)
        {
            sb.AppendLine().AppendLine(
                "WHERE THESE WORDS ALSO APPEAR (measured across this book, not recalled — " +
                "a detail tied to one place is doing its job; the same detail spread across many " +
                "places is usually filler):");
            foreach (var e in c.Echoes)
                sb.AppendLine($"  \"{e.Term}\" — {e.BeatCount} other beat(s), {e.PlaceCount} distinct place(s): " +
                              string.Join(", ", e.Samples.Select(s => $"#{s.Number}")));
        }
        else
        {
            sb.AppendLine().AppendLine(
                "WHERE THESE WORDS ALSO APPEAR: nowhere else in this book — the passage is unique.");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Picking the words from a selection worth searching the book for.
///
/// <para>Pure, so the judgement can be tested. Deliberately crude: length and a stopword list,
/// not statistics. A cleverer ranking would need corpus frequencies, and being wrong about which
/// word is distinctive costs the author nothing here — they see the hits and judge for themselves.</para>
/// </summary>
public static class SalientTerms
{
    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    {
        "about", "above", "after", "again", "against", "because", "been", "before", "being",
        "below", "between", "both", "could", "does", "doing", "down", "during", "each",
        "from", "further", "had", "has", "have", "having", "here", "how", "into", "itself",
        "more", "most", "not", "now", "once", "only", "other", "our", "out", "over", "own",
        "same", "should", "some", "such", "than", "that", "their", "them", "then", "there",
        "these", "they", "this", "those", "through", "too", "under", "until", "very", "was",
        "were", "what", "when", "where", "which", "while", "who", "whom", "why", "with",
        "would", "you", "your", "and", "but", "for", "the", "his", "her", "him", "she",
        "back", "like", "just", "into", "onto", "still", "even", "something", "nothing",
    };

    public static IReadOnlyList<string> Extract(string? selection, int max = 5)
    {
        if (string.IsNullOrWhiteSpace(selection)) return [];

        // Entity tags carry guids, which would swamp any ranking by length.
        var text = BeatMarkup.StripEntityTags(selection);

        var words = new List<string>();
        var current = new StringBuilder();
        foreach (var ch in text)
        {
            if (char.IsLetter(ch) || ch == '\'') current.Append(ch);
            else { Flush(current, words); }
        }
        Flush(current, words);

        return words
            .Where(w => w.Length >= 5 && !Stop.Contains(w))
            .GroupBy(w => w, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Key)
            // Longer words are the distinctive ones far more often than not — "shift" is noise,
            // "camphor" is the kind of detail that either belongs to one room or to none.
            .OrderByDescending(w => w.Length)
            .Take(max)
            .ToList();

        static void Flush(StringBuilder sb, List<string> into)
        {
            if (sb.Length > 0) { into.Add(sb.ToString()); sb.Clear(); }
        }
    }
}
