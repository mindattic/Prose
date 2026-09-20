namespace Prose.Core.Services.Discussion;

/// <summary>Where a discussion is attached, and what it quoted when it was attached there.</summary>
/// <param name="Quote">The selected text. The authority — offsets are only a shortcut.</param>
/// <param name="Prefix">Text immediately before the quote, for telling repeats apart.</param>
/// <param name="Suffix">Text immediately after the quote, for telling repeats apart.</param>
public sealed record TextAnchor(string Quote, string Prefix, string Suffix, int Start, int End);

public enum AnchorOutcome
{
    /// <summary>The offsets still point at the quote. Nothing moved.</summary>
    Exact,

    /// <summary>The text shifted; the quote was found again and the offsets were updated.</summary>
    Reanchored,

    /// <summary>The quote occurs several times and the surrounding text did not settle it. The
    /// nearest candidate to the original position was chosen — usable, but say so.</summary>
    Ambiguous,

    /// <summary>The quote is gone. The thread is not deleted; it is shown as detached.</summary>
    Detached,
}

public sealed record AnchorResult(AnchorOutcome Outcome, int Start, int End)
{
    public bool Found => Outcome != AnchorOutcome.Detached;
}

/// <summary>
/// Locating a quoted passage again after the text around it has changed.
///
/// <para>Stored offsets cannot be trusted on their own here: <b>every beat save re-derives entity
/// tags</b>, so <c>Beat.Text</c> can shift by dozens of characters without the author touching the
/// span a thread is attached to. The quote is therefore the authority and the offsets are a fast
/// path — the shape the W3C Web Annotation model settled on, for the same reason.</para>
///
/// <para>Pure and side-effect free, so the awkward cases (a quote that now appears twice, a quote
/// that was deleted, offsets past the end of a shortened beat) are testable without a database.</para>
/// </summary>
public static class TextAnchoring
{
    /// <summary>How much surrounding text to keep. Long enough to separate two occurrences of an
    /// ordinary sentence, short enough that editing nearby prose does not invalidate it.</summary>
    public const int ContextLength = 48;

    /// <summary>Capture an anchor for a selection the author just made.</summary>
    public static TextAnchor Capture(string text, int start, int end)
    {
        text ??= "";
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, start, text.Length);

        var quote = text[start..end];
        var prefixStart = Math.Max(0, start - ContextLength);
        var suffixEnd = Math.Min(text.Length, end + ContextLength);

        return new TextAnchor(
            quote,
            text[prefixStart..start],
            text[end..suffixEnd],
            start,
            end);
    }

    /// <summary>
    /// Build an anchor from a selection the browser reported, without trusting it for a position.
    ///
    /// <para>The editor knows exactly what was selected and what surrounds it, but turning a DOM
    /// range into an offset in the serialized beat means reimplementing the serializer in
    /// JavaScript and keeping the two in step forever. Since the quote is the authority anyway,
    /// the browser sends text and the server finds it — one implementation, and the awkward part
    /// stays where it is already tested.</para>
    ///
    /// <returns>Null when the selection cannot be found in the text at all, which normally means
    /// the beat changed between the selection and the request.</returns>
    /// </summary>
    public static TextAnchor? Locate(string text, string quote, string prefix, string suffix)
    {
        if (string.IsNullOrEmpty(quote)) return null;

        if (prefix.Length > ContextLength) prefix = prefix[^ContextLength..];
        if (suffix.Length > ContextLength) suffix = suffix[..ContextLength];

        // Start = -1 skips the offset fast path, so this always goes through the search.
        var found = Resolve(text, new TextAnchor(quote, prefix, suffix, -1, -1));
        return found.Found ? Capture(text, found.Start, found.End) : null;
    }

    /// <summary>Find where an anchor points in the current text.</summary>
    public static AnchorResult Resolve(string? text, TextAnchor anchor)
    {
        text ??= "";

        // An empty quote can match anywhere, which is the same as matching nowhere.
        if (string.IsNullOrEmpty(anchor.Quote))
            return new AnchorResult(AnchorOutcome.Detached, 0, 0);

        // Fast path: the offsets still hold.
        if (anchor.Start >= 0
            && anchor.End <= text.Length
            && anchor.End - anchor.Start == anchor.Quote.Length
            && string.CompareOrdinal(text, anchor.Start, anchor.Quote, 0, anchor.Quote.Length) == 0)
        {
            return new AnchorResult(AnchorOutcome.Exact, anchor.Start, anchor.End);
        }

        var hits = AllOccurrences(text, anchor.Quote);
        if (hits.Count == 0)
            return new AnchorResult(AnchorOutcome.Detached, 0, 0);

        if (hits.Count == 1)
            return new AnchorResult(AnchorOutcome.Reanchored, hits[0], hits[0] + anchor.Quote.Length);

        // Several candidates: the surrounding text decides. Score each by how much of the stored
        // prefix and suffix still abuts it.
        var best = -1;
        var bestScore = -1;
        var tied = false;

        foreach (var i in hits)
        {
            var score = CommonSuffixLength(anchor.Prefix, text.AsSpan(0, i))
                      + CommonPrefixLength(anchor.Suffix, text.AsSpan(i + anchor.Quote.Length));

            if (score > bestScore) { bestScore = score; best = i; tied = false; }
            else if (score == bestScore) { tied = true; }
        }

        if (!tied && bestScore > 0)
            return new AnchorResult(AnchorOutcome.Reanchored, best, best + anchor.Quote.Length);

        // The context could not separate them. Take the candidate nearest where the thread used to
        // be — the author is far likelier to have been talking about that one — and flag it, so the
        // panel can say the anchor is uncertain rather than quietly pointing somewhere else.
        var nearest = hits[0];
        foreach (var i in hits)
            if (Math.Abs(i - anchor.Start) < Math.Abs(nearest - anchor.Start)) nearest = i;

        return new AnchorResult(AnchorOutcome.Ambiguous, nearest, nearest + anchor.Quote.Length);
    }

    private static List<int> AllOccurrences(string haystack, string needle)
    {
        var hits = new List<int>();
        var from = 0;
        while (from <= haystack.Length - needle.Length)
        {
            var i = haystack.IndexOf(needle, from, StringComparison.Ordinal);
            if (i < 0) break;
            hits.Add(i);
            from = i + 1;   // overlapping matches still count as candidates
        }
        return hits;
    }

    private static int CommonSuffixLength(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        var n = 0;
        while (n < a.Length && n < b.Length && a[^(n + 1)] == b[^(n + 1)]) n++;
        return n;
    }

    private static int CommonPrefixLength(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        var n = 0;
        while (n < a.Length && n < b.Length && a[n] == b[n]) n++;
        return n;
    }
}
