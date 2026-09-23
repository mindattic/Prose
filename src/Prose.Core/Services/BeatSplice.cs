using System.Text;

namespace Prose.Core.Services;

/// <summary>One hand-written replacement in a splice docket: in beat <see cref="Beat"/> (the global
/// <c>Beat.Number</c> findings quote), <see cref="Old"/> must occur exactly <see cref="Count"/> times in
/// the reader-visible text and every occurrence becomes <see cref="New"/>. Empty <see cref="New"/> is
/// a deletion.</summary>
public sealed record SpliceEdit(int Beat, string Old, string New, int Count = 1);

/// <param name="Text">The whole beat as it should now be stored, markup and all.</param>
/// <param name="ExpectedPlain">What a reader should see once it is stored — the same edits applied to
/// the tag-stripped text directly. The read-back after the write is checked against this.</param>
/// <param name="Failures">Guard failures. Non-empty means <see cref="Text"/> must not be written.</param>
/// <param name="UnwrappedTags">Entity tags an edit touched, removed so the save can re-derive them.</param>
public sealed record SpliceOutcome(string Text, string ExpectedPlain, IReadOnlyList<string> Failures, int UnwrappedTags);

/// <summary>
/// Applying a docket of exact-text replacements to one beat's stored text.
///
/// <para><b>Matching happens on the text a reader sees with entity tags stripped</b> — the only text
/// an author can quote — but the write happens on the stored markup. Inline markers
/// (<c>*italic*</c>) are NOT stripped, unlike <see cref="Discussion.PlainTextMap"/>: a docket is typed
/// by someone reading the raw beat, and an italicised line has to be quoted with its asterisks or the
/// replacement would silently drop them.</para>
///
/// <para><b>Tags outside an edit are kept byte-for-byte.</b> Writing the stripped text back (what the
/// 2026-09-22 scratchpad <c>splice.py</c> did) makes every save re-derive every tag, and the scanner
/// cannot re-derive a tag on an ambiguous name — so each docket quietly shed pinned mentions on beats
/// it barely touched. Here only a tag an edit actually overlaps is unwrapped to its words, so the
/// save re-derives that one and leaves the rest pinned.</para>
///
/// <para><b>Counts are checked sequentially</b>: each edit is counted against the beat as the edits
/// before it left it, so a docket reads top to bottom the way it was written.</para>
/// </summary>
public static class BeatSplice
{
    public static SpliceOutcome Apply(string? stored, IEnumerable<SpliceEdit> edits)
    {
        var text = stored ?? "";
        var expected = BeatMarkup.StripEntityTags(text);
        var failures = new List<string>();
        var unwrapped = 0;

        foreach (var e in edits)
        {
            var old = e.Old ?? "";
            var replacement = e.New ?? "";
            if (old.Length == 0) { failures.Add($"#{e.Beat}: empty 'old' — nothing to anchor to"); continue; }
            if (e.Count < 1) { failures.Add($"#{e.Beat}: count must be ≥1, got {e.Count}: {Preview(old)}"); continue; }
            if (BeatMarkup.Validate(replacement) is { Count: > 0 } bad)
            { failures.Add($"#{e.Beat}: 'new' markup is malformed ({bad[0].Message}): {Preview(replacement)}"); continue; }

            var (plain, map) = Map(text);
            var hits = Occurrences(plain, old);
            if (hits.Count != e.Count)
            {
                failures.Add($"#{e.Beat}: expected {e.Count} got {hits.Count}: {Preview(old)}");
                continue;
            }

            // Unwrap every tag an occurrence overlaps. Unwrapping never changes the plain text, so the
            // hits stay valid; afterwards no occurrence's source range contains any markup at all.
            var ranges = hits.Select(h => SourceRange(map, h, old.Length, text.Length)).ToList();
            var touched = BeatMarkup.TagSpans(text)
                .Where(t => ranges.Any(r => r.Start < t.Start + t.Length && t.Start < r.End))
                .ToList();
            if (touched.Count > 0)
            {
                var sb = new StringBuilder(text);
                foreach (var t in touched.OrderByDescending(t => t.Start))
                {
                    sb.Remove(t.Start, t.Length);
                    sb.Insert(t.Start, text.Substring(t.InnerStart, t.InnerLength));
                }
                text = sb.ToString();
                unwrapped += touched.Count;
                (_, map) = Map(text);
                ranges = hits.Select(h => SourceRange(map, h, old.Length, text.Length)).ToList();
            }

            foreach (var r in ranges.OrderByDescending(r => r.Start))
                text = text[..r.Start] + replacement + text[r.End..];

            expected = ReplaceExact(expected, old, BeatMarkup.StripEntityTags(replacement));
        }

        // Tripwire: the stored text must read exactly as the same edits applied to plain text. True by
        // construction — which is why it is asserted: a wrong index here would rewrite prose nobody
        // quoted, and nothing downstream would notice.
        if (failures.Count == 0 && BeatMarkup.StripEntityTags(text) != expected)
            failures.Add("internal: spliced markup does not strip to the expected text — nothing written (bug)");

        return new SpliceOutcome(text, expected, failures, unwrapped);
    }

    /// <summary>Non-overlapping ordinal occurrences, left to right (Python <c>str.count</c> semantics,
    /// which every docket written so far was checked against).</summary>
    public static List<int> Occurrences(string haystack, string needle)
    {
        var hits = new List<int>();
        if (needle.Length == 0) return hits;
        for (var i = haystack.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = haystack.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
            hits.Add(i);
        return hits;
    }

    public static string Preview(string s)
    {
        var flat = (s ?? "").Replace("\r", "").Replace('\n', '⏎');
        return flat.Length <= 70 ? $"\"{flat}\"" : $"\"{flat[..70]}…\"";
    }

    private static string ReplaceExact(string text, string old, string replacement)
    {
        var hits = Occurrences(text, old);
        for (var k = hits.Count - 1; k >= 0; k--)
            text = text[..hits[k]] + replacement + text[(hits[k] + old.Length)..];
        return text;
    }

    /// <summary>Tag-stripped text plus, per character, its index in the stored markup — built from
    /// <see cref="BeatMarkup.KeptRanges"/> so it cannot disagree with the stripper.</summary>
    private static (string Plain, int[] Map) Map(string stored)
    {
        var plain = new StringBuilder(stored.Length);
        var map = new List<int>(stored.Length);
        foreach (var (start, length) in BeatMarkup.KeptRanges(stored))
        {
            plain.Append(stored, start, length);
            for (var k = 0; k < length; k++) map.Add(start + k);
        }
        return (plain.ToString(), [.. map]);
    }

    /// <summary>Source range of a plain span: start of its first character to one past its last, so a
    /// span ending at a tag boundary never swallows the markup after it.</summary>
    private static (int Start, int End) SourceRange(int[] map, int plainStart, int length, int sourceLength)
    {
        var start = plainStart < map.Length ? map[plainStart] : sourceLength;
        var end = map[plainStart + length - 1] + 1;
        return (start, end);
    }
}
