using System.Text;

namespace Prose.Core.Services.Discussion;

/// <summary>
/// The text a reader sees, and where each of its characters came from in the stored markup.
///
/// <para><b>Why this has to exist.</b> A discussion anchors to a span of the text a READER sees —
/// that is what the author selected, and it is the only coordinate system that survives a save,
/// since every save re-derives entity tags and moves the stored text by ~55 characters per
/// wrapper without changing a word. But a write has to happen in the STORED text. Something has to
/// carry a position across that boundary, and doing it by eye is how an edit lands inside a tag
/// and breaks it.</para>
///
/// <para><b>Built from the real parsers, not a copy of their rules.</b> The entity ranges come
/// from <see cref="BeatMarkup.KeptRanges"/>, which shares its pattern with the stripper; the
/// formatting runs come from <see cref="ProseInline.ParseRuns"/>, which is the same walk
/// <see cref="ProseInline.Parse"/> performs. A second implementation of either would drift, and
/// the drift would be invisible until it corrupted a beat.</para>
///
/// <para>Verified against <see cref="BeatDiscussTarget.PlainText"/> in the tests: this map's
/// <see cref="Plain"/> must equal what the rest of the system calls the reader's text, or the
/// whole thing is measuring something else.</para>
/// </summary>
public sealed class PlainTextMap
{
    /// <summary>The text as a reader sees it.</summary>
    public string Plain { get; }

    /// <summary>Source index in the stored markup, per character of <see cref="Plain"/>.</summary>
    private readonly int[] toSource;

    private readonly int sourceLength;

    private PlainTextMap(string plain, int[] toSource, int sourceLength)
    {
        Plain = plain;
        this.toSource = toSource;
        this.sourceLength = sourceLength;
    }

    public static PlainTextMap Build(string? markup)
    {
        var source = markup ?? "";
        if (source.Length == 0) return new PlainTextMap("", [], 0);

        // Pass one: drop the entity wrappers, keeping where every surviving character came from.
        var untagged = new StringBuilder(source.Length);
        var untaggedToSource = new List<int>(source.Length);
        foreach (var (start, length) in BeatMarkup.KeptRanges(source))
        {
            untagged.Append(source, start, length);
            for (var k = 0; k < length; k++) untaggedToSource.Add(start + k);
        }

        // Pass two: drop the inline markers, composing the two maps as it goes. A run is
        // contiguous in its input, so the k-th character of a run came from Start + k.
        var intermediate = untagged.ToString();
        var plain = new StringBuilder(intermediate.Length);
        var plainToSource = new List<int>(intermediate.Length);
        foreach (var run in ProseInline.ParseRuns(intermediate))
        {
            plain.Append(run.Text);
            for (var k = 0; k < run.Text.Length; k++)
                plainToSource.Add(untaggedToSource[run.Start + k]);
        }

        return new PlainTextMap(plain.ToString(), [.. plainToSource], source.Length);
    }

    /// <summary>
    /// Where a plain-text position sits in the stored markup.
    /// </summary>
    /// <param name="plainIndex">0..<see cref="Plain"/>.Length. The end position is valid and maps
    /// to the end of the source, which is what makes a span running to the end of the beat work.</param>
    public int ToSource(int plainIndex)
    {
        if (plainIndex <= 0) return toSource.Length > 0 ? toSource[0] : 0;
        if (plainIndex >= toSource.Length) return sourceLength;
        return toSource[plainIndex];
    }

    /// <summary>
    /// The markup range covering a plain-text span.
    /// </summary>
    /// <remarks>
    /// The END is taken as one past the last included character rather than as the source index of
    /// the next plain character. The difference matters at a tag boundary: the next plain character
    /// may sit after a whole <c>&lt;/entity&gt;&lt;entity …&gt;</c> pair, and taking its index
    /// would silently swallow that markup into the replaced range.
    /// </remarks>
    public (int Start, int End) ToSourceRange(int plainStart, int plainEnd)
    {
        if (plainEnd <= plainStart) return (ToSource(plainStart), ToSource(plainStart));

        var start = ToSource(plainStart);
        var lastIncluded = Math.Min(plainEnd, toSource.Length) - 1;
        var end = lastIncluded >= 0 && lastIncluded < toSource.Length
            ? toSource[lastIncluded] + 1
            : start;

        return (start, Math.Max(start, end));
    }
}
