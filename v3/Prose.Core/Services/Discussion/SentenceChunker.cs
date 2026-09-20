using System.Text;

namespace Prose.Core.Services.Discussion;

/// <summary>
/// Cuts a stream of text fragments into sentences, as soon as each one is complete.
///
/// <para><b>What it buys.</b> Synthesis cannot begin until it has something to say, and "the whole
/// answer" arrives three to eight seconds after the question. One sentence arrives in well under
/// one. Speaking sentence one while sentence two is still being written is most of the difference
/// between a conversation and a wait, and it costs nothing but this.</para>
///
/// <para><b>Deliberately conservative about where a sentence ends.</b> A false split mid-sentence
/// is audible and bad — the voice stops dead on "Mr." and starts again on "Chen was waiting" with
/// a fresh breath. A missed split merely costs a little latency on one chunk. So every ambiguous
/// case resolves towards NOT splitting: known abbreviations, initials, decimals, and anything
/// inside a fenced code block, which is prose the author is being shown rather than told.</para>
///
/// <para>Stateful and single-threaded by design — it is fed in order by one stream.</para>
/// </summary>
public sealed class SentenceChunker
{
    /// <summary>
    /// Below this, a completed sentence waits for the next one rather than being emitted alone.
    ///
    /// <para>"Yes." is a legitimate sentence and a wasteful synthesis: a round trip and an audio
    /// element for four syllables, followed by an audible seam. The exception is the FIRST chunk,
    /// where getting any sound out quickly is the entire point — see <see cref="firstEmitted"/>.</para>
    /// </summary>
    private const int MinChunkChars = 40;

    /// <summary>The first chunk may be this short, because time-to-first-sound dominates.</summary>
    private const int MinFirstChunkChars = 12;

    /// <summary>
    /// Words that end in a full stop without ending a sentence. Not exhaustive, and does not need
    /// to be: a miss costs one late chunk, and the list covers what actually turns up in a
    /// conversation about a manuscript.
    /// </summary>
    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "mr", "mrs", "ms", "dr", "prof", "st", "mt", "sgt", "lt", "capt", "rev", "hon",
        "jr", "sr", "vs", "etc", "eg", "ie", "al", "fig", "no", "vol", "ch", "pp", "ed",
        "approx", "dept", "est", "inc", "ltd", "co",
    };

    private readonly StringBuilder buffer = new();
    private bool inFence;
    private bool firstEmitted;

    /// <summary>
    /// Add a fragment and take whatever complete sentences that produced.
    /// </summary>
    /// <returns>Zero or more chunks, in order. Usually zero — most deltas are a few tokens.</returns>
    public IReadOnlyList<string> Add(string? fragment)
    {
        if (string.IsNullOrEmpty(fragment)) return [];

        var emitted = new List<string>();
        foreach (var ch in fragment)
        {
            buffer.Append(ch);

            // A fence toggles on its third backtick. Inside one, nothing splits: a code or prose
            // sample is being shown to the author, and reading it aloud sentence by sentence would
            // be wrong even if the sentences were right.
            if (ch == '`' && EndsWith("```"))
            {
                inFence = !inFence;
                continue;
            }
            if (inFence) continue;

            if (!IsTerminator(ch)) continue;
            if (!BoundaryIsReal()) continue;

            var chunk = buffer.ToString().Trim();
            if (chunk.Length < (firstEmitted ? MinChunkChars : MinFirstChunkChars)) continue;

            emitted.Add(chunk);
            firstEmitted = true;
            buffer.Clear();
        }

        return emitted;
    }

    /// <summary>
    /// Everything still held, whether or not it ends in a full stop.
    ///
    /// <para>Must be called when the stream ends. An answer whose last sentence has no terminator
    /// — a list item, a fenced block, a trailing clause — would otherwise be silently dropped, and
    /// the author would hear a reply that stops one sentence early with no indication it had.</para>
    /// </summary>
    public string? Flush()
    {
        var rest = buffer.ToString().Trim();
        buffer.Clear();
        inFence = false;
        firstEmitted = true;
        return rest.Length == 0 ? null : rest;
    }

    private static bool IsTerminator(char c) => c is '.' or '!' or '?' or '…' or '\n';

    /// <summary>
    /// Whether the terminator just appended really ends a sentence.
    ///
    /// <para>Resolved on what precedes it, because what follows has not arrived yet — a streaming
    /// chunker cannot look ahead, which is exactly why it must be cautious.</para>
    /// </summary>
    private bool BoundaryIsReal()
    {
        var text = buffer.ToString();
        var last = text[^1];

        // A newline always ends a chunk: a list item or a paragraph break is a real pause however
        // it is punctuated.
        if (last == '\n') return true;
        if (last is '!' or '?' or '…') return true;

        // From here it is a full stop, which is the only ambiguous one.
        var before = text.Length >= 2 ? text[^2] : ' ';

        // "3.5", "v1.2" — a decimal point, not a stop.
        if (char.IsDigit(before)) return false;

        // "J. Kyle" — a single capital before the stop is an initial.
        var word = TrailingWord(text[..^1]);
        if (word.Length == 1 && char.IsUpper(word[0])) return false;

        // "Mr." — and "etc." is deliberately in the list too: it usually continues.
        return !Abbreviations.Contains(word);
    }

    /// <summary>The run of letters immediately before the terminator.</summary>
    private static string TrailingWord(string text)
    {
        var end = text.Length;
        var start = end;
        while (start > 0 && char.IsLetter(text[start - 1])) start--;
        return text[start..end];
    }

    private bool EndsWith(string suffix)
    {
        if (buffer.Length < suffix.Length) return false;
        for (var i = 0; i < suffix.Length; i++)
            if (buffer[buffer.Length - suffix.Length + i] != suffix[i]) return false;
        return true;
    }
}
