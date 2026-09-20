using System.Text.RegularExpressions;

namespace Prose.Core.Services;

/// <summary>
/// The one implementation of the house chapter-title standard: <c>Chapter N</c> or
/// <c>Chapter N — Subtitle</c>, em dash, digits.
///
/// <para>Before this existed the standard was enforced in exactly one place — a
/// <c>$"Chapter {n} — {subtitle}"</c> interpolation inside
/// <see cref="NodeWorkbenchService"/>'s split routine — and <i>read</i> nowhere. Three exporters
/// each sniffed titles with their own regex to decide whether a string was a heading
/// (<c>DocxExportService.LooksLikeChapterHeading</c> matched only <c>Chapter \d+</c> and
/// <c>Interlude:</c>), and nothing anywhere could answer "is this title actually standard?" or
/// "what number does it claim?". Parsing and formatting live together here so the two can never
/// drift: anything <see cref="Format"/> emits, <see cref="Parse"/> reads back as
/// <see cref="ParsedChapterTitle.IsStandard"/>.</para>
///
/// <para>This is a <b>title</b> parser, not a chapter-boundary detector. Where a chapter begins is
/// decided by the node tree and nothing else — see <see cref="BookSpineService"/>. A beat whose
/// prose happens to open with the words "Chapter 7" is draft debris, and
/// <see cref="LooksLikeHeading"/> exists to help report it, never to split on it.</para>
/// </summary>
public static class ChapterTitle
{
    /// <summary>The house separator between a chapter number and its subtitle. An em dash — a
    /// hyphen or colon parses, but is not standard, and the validator says so.</summary>
    public const string Separator = "—";

    /// <summary>
    /// Kind + number + subtitle, as parsed off a node title.
    ///
    /// <para><paramref name="FoundSeparator"/> is kept rather than normalised away so a report can
    /// distinguish "this title is fine" from "this title is a hyphen away from being fine" — the
    /// difference between a rename and leaving it alone.</para>
    /// </summary>
    public readonly record struct ParsedChapterTitle(
        ChapterTitleKind Kind,
        int?             Number,
        string?          Subtitle,
        string?          FoundSeparator)
    {
        /// <summary>True only for the canonical shape: a numbered Chapter, and — when it carries a
        /// subtitle — an em dash separating the two.</summary>
        public bool IsStandard =>
            Kind == ChapterTitleKind.Chapter
            && Number is not null
            && (Subtitle is null || FoundSeparator == Separator);
    }

    /// <summary>
    /// Matches a unit heading by name, optional number, and optional separated subtitle.
    ///
    /// <para>Digits only: "Chapter Seven" and "Chapter IV" deliberately fall through to
    /// <see cref="ChapterTitleKind.Other"/> so the validator flags them rather than this parser
    /// quietly blessing a second house style.</para>
    /// </summary>
    private static readonly Regex Pattern = new(
        @"^\s*(?<kind>Chapter|Interlude|Prologue|Epilogue)\b\s*(?<num>\d+)?\s*(?:(?<sep>—|–|-|:)\s*(?<sub>\S.*?))?\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Leading markdown hashes, so a heading written as <c>## Chapter 7</c> in prose is
    /// still recognised as a heading by <see cref="LooksLikeHeading"/>.</summary>
    private static readonly Regex LeadingHashes = new(@"^\s*#{1,6}\s*", RegexOptions.Compiled);

    /// <summary>
    /// The narrow test the exporters have always applied to a <i>beat</i> title, kept byte-identical
    /// to the regex that was duplicated in <c>DocxExportService</c> and
    /// <c>ManuscriptExportService</c>.
    ///
    /// <para>Deliberately NOT <see cref="LooksLikeHeading"/>, which is broader — it also catches
    /// "Prologue", "Interlude 3" without a colon, and markdown hashes. The two answer different
    /// questions. This one decides whether a beat's title should be trusted as a chapter heading in
    /// preference to the node's own title, a decision that governs what every exported book
    /// currently prints; broadening it would silently re-title real chapters in the corpus, so it
    /// stays exactly as strict as it has always been until someone changes it on purpose.</para>
    /// </summary>
    private static readonly Regex LegacyBeatHeadingPattern =
        new(@"^(Chapter\s+\d+\b|Interlude\s*:)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <inheritdoc cref="LegacyBeatHeadingPattern"/>
    public static bool IsLegacyBeatHeading(string? title) =>
        !string.IsNullOrWhiteSpace(title) && LegacyBeatHeadingPattern.IsMatch(title.Trim());

    /// <summary>Read a node title. Anything that is not a recognised unit heading comes back as
    /// <see cref="ChapterTitleKind.Other"/> with the whole string as the subtitle — a book whose
    /// chapters are titled "Teeth" and "The Long Dark" is not an error, it is just not the house
    /// standard, and the caller decides whether that matters.</summary>
    public static ParsedChapterTitle Parse(string? title)
    {
        var text = (title ?? "").Trim();
        if (text.Length == 0) return new ParsedChapterTitle(ChapterTitleKind.Other, null, null, null);

        var m = Pattern.Match(text);
        if (!m.Success) return new ParsedChapterTitle(ChapterTitleKind.Other, null, text, null);

        var kind = m.Groups["kind"].Value.ToLowerInvariant() switch
        {
            "chapter"   => ChapterTitleKind.Chapter,
            "interlude" => ChapterTitleKind.Interlude,
            "prologue"  => ChapterTitleKind.Prologue,
            _           => ChapterTitleKind.Epilogue,
        };

        int? number = m.Groups["num"].Success && int.TryParse(m.Groups["num"].Value, out var n) ? n : null;
        var subtitle = m.Groups["sub"].Success ? m.Groups["sub"].Value.Trim() : null;
        var separator = m.Groups["sep"].Success ? m.Groups["sep"].Value : null;

        return new ParsedChapterTitle(kind, number, string.IsNullOrEmpty(subtitle) ? null : subtitle, separator);
    }

    /// <summary>The canonical title for a numbered chapter. This is what every rename, split and
    /// chapter creation must emit.</summary>
    public static string Format(int number, string? subtitle)
    {
        var sub = (subtitle ?? "").Trim();
        return sub.Length == 0 ? $"Chapter {number}" : $"Chapter {number} {Separator} {sub}";
    }

    /// <summary>Re-emit a parsed title in canonical form where one exists. A non-Chapter kind keeps
    /// its own word ("Interlude 3 — Static"); an <see cref="ChapterTitleKind.Other"/> round-trips
    /// unchanged, because inventing a number for it would be a guess.</summary>
    public static string Format(ParsedChapterTitle parsed)
    {
        if (parsed.Kind == ChapterTitleKind.Other) return parsed.Subtitle ?? "";

        var word = parsed.Kind.ToString();
        var head = parsed.Number is { } n ? $"{word} {n}" : word;
        var sub = (parsed.Subtitle ?? "").Trim();
        return sub.Length == 0 ? head : $"{head} {Separator} {sub}";
    }

    /// <summary>
    /// Does this line read as a unit heading? Used to report a heading that has leaked into prose,
    /// and by the exporters to recognise a legacy heading beat.
    ///
    /// <para>Tolerates markdown hashes and a trailing colon, because that is how the leaked ones
    /// were actually written.</para>
    /// </summary>
    public static bool LooksLikeHeading(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        var text = LeadingHashes.Replace(line.Trim(), "").TrimEnd(':').Trim();
        if (text.Length == 0) return false;

        var parsed = Parse(text);
        return parsed.Kind != ChapterTitleKind.Other;
    }

    /// <summary>The first line of a beat's prose, for <see cref="LooksLikeHeading"/>. A heading
    /// beat puts it on its own line; a paragraph that merely begins with the word "Chapter" runs
    /// on, which is why only a short standalone line counts.</summary>
    public static string FirstLine(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var span = text.AsSpan();
        var end = span.IndexOfAny('\r', '\n');
        return (end < 0 ? span : span[..end]).Trim().ToString();
    }
}

/// <summary>The unit kinds a node title can name. <see cref="Other"/> covers every title that does
/// not open with one of the house words — common, and not in itself a defect.</summary>
public enum ChapterTitleKind
{
    Other,
    Chapter,
    Interlude,
    Prologue,
    Epilogue,
}
