using System.Text;

namespace Prose.Core.Services;

/// <summary>
/// The inline formatting vocabulary of beat prose, in one place.
///
/// <para>The corpus already uses two of these. <c>*italic*</c> is everywhere (182 spans in Bushido
/// Coda alone — emphasis, interior thought, foreign phrases). <c>**bold**</c> is rarer and means
/// something specific: text the characters are *reading* — a screen readout, a note, a message
/// (<c>**TO: STANDING CONTRACT**</c>, <c>**HAPPY FATHER'S DAY DAD.**</c>). Until 2026-09-12 the
/// .docx exporter split on a single <c>*</c> and alternated italic, which turned every one of
/// those bold spans into ordinary body text in the manuscript.</para>
///
/// <para>Parsed here once so the exporters and the editor agree on what the markers mean.</para>
/// </summary>
public static class ProseInline
{
    [Flags]
    public enum Style
    {
        None = 0,
        Italic = 1,
        Bold = 2,
        Underline = 4,
        Strikethrough = 8
    }

    /// <summary>A run of text carrying one combination of styles.</summary>
    public sealed record Span(string Text, Style Style);

    /// <summary>
    /// Splits text into styled runs. Markers are <c>**bold**</c>, <c>*italic*</c>,
    /// <c>~~strikethrough~~</c> and <c>&lt;u&gt;underline&lt;/u&gt;</c>.
    ///
    /// <para>Unmatched markers are left alone as literal characters rather than swallowed: prose
    /// contains real asterisks, and a half-typed emphasis in a draft should look wrong in the
    /// editor, not silently eat the rest of the paragraph.</para>
    /// </summary>
    public static List<Span> Parse(string? text)
        => ParseRuns(text).Select(r => new Span(r.Text, r.Style)).ToList();

    /// <summary>
    /// A run of styled text, and where it started in the source.
    ///
    /// <para>Every character of <see cref="Text"/> came from <c>source[Start + k]</c> — the parser
    /// only ever copies literal characters one at a time, so a run is contiguous in the source and
    /// the offset is exact rather than approximate.</para>
    /// </summary>
    public sealed record Run(int Start, string Text, Style Style);

    /// <summary>
    /// <see cref="Parse"/>, with source positions.
    ///
    /// <para>The positions are what makes a constrained span write possible: a span selected in
    /// the text a READER sees has to be turned back into a range in the text that is STORED, and
    /// guessing at that mapping is how an edit lands on the wrong characters. This is the same
    /// walk <see cref="Parse"/> performs — deliberately one implementation, because two would
    /// drift and the drift would be invisible until an edit corrupted a beat.</para>
    /// </summary>
    public static List<Run> ParseRuns(string? text)
    {
        var spans = new List<Run>();
        if (string.IsNullOrEmpty(text)) return spans;

        var style = Style.None;
        var buffer = new StringBuilder();
        var runStart = 0;

        void Flush()
        {
            if (buffer.Length == 0) return;
            spans.Add(new Run(runStart, buffer.ToString(), style));
            buffer.Clear();
        }

        var i = 0;
        while (i < text.Length)
        {
            // Longest marker first: "**" must win over "*".
            if (Match(text, i, "**", style, Style.Bold, out var boldLen))
            { Flush(); style ^= Style.Bold; i += boldLen; continue; }

            if (Match(text, i, "~~", style, Style.Strikethrough, out var strikeLen))
            { Flush(); style ^= Style.Strikethrough; i += strikeLen; continue; }

            if (Match(text, i, "*", style, Style.Italic, out var italLen))
            { Flush(); style ^= Style.Italic; i += italLen; continue; }

            if (!style.HasFlag(Style.Underline) && StartsWith(text, i, "<u>") && Closes(text, i, "</u>"))
            { Flush(); style |= Style.Underline; i += 3; continue; }

            if (style.HasFlag(Style.Underline) && StartsWith(text, i, "</u>"))
            { Flush(); style &= ~Style.Underline; i += 4; continue; }

            // The start of a run is the position of its first literal character, recorded as it
            // is appended rather than inferred afterwards from the marker lengths.
            if (buffer.Length == 0) runStart = i;
            buffer.Append(text[i]);
            i++;
        }

        Flush();
        return spans;
    }

    /// <summary>Every marker removed, leaving the words. What narration, embeddings and any
    /// LLM-facing prompt should see — none of them should be reading asterisks aloud or
    /// embedding them.</summary>
    public static string StripFormatting(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        var sb = new StringBuilder(text.Length);
        foreach (var span in Parse(text)) sb.Append(span.Text);
        return sb.ToString();
    }

    private static bool Match(string text, int i, string marker, Style current, Style flag, out int length)
    {
        length = marker.Length;
        if (!StartsWith(text, i, marker)) return false;

        // Opening a span requires a closing marker later on; closing one always matches.
        return current.HasFlag(flag) || Closes(text, i + marker.Length, marker);
    }

    private static bool StartsWith(string text, int i, string s) =>
        i + s.Length <= text.Length && string.CompareOrdinal(text, i, s, 0, s.Length) == 0;

    private static bool Closes(string text, int from, string marker)
    {
        if (from > text.Length) return false;
        if (marker != "*") return text.IndexOf(marker, from, StringComparison.Ordinal) >= 0;
        // A single "*" is closed only by a single "*": the first half of a later "**bold**" used
        // to count, so "5 * 3 = 15. **NOTE**" opened italic that never closed (and dropped the *).
        for (var k = from; k < text.Length; k++)
        {
            if (text[k] != '*') continue;
            if (k + 1 < text.Length && text[k + 1] == '*') { k++; continue; }
            return true;
        }
        return false;
    }
}
