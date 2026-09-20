using System.Text;

namespace Prose.Core.Services.Discussion;

/// <summary>
/// Lets the prose of a streamed reply through and stops its machine-readable trailer at the door.
///
/// <para><b>The problem it solves.</b> A confirmation arrives in the same token stream as the
/// answer, and the answer is being spoken aloud as it arrives. Without this, the author hears
/// "dash dash dash REQUEST dash dash dash, RESTATEMENT colon, you want the second sentence cut,
/// STEPS colon, one…" — the protocol read out as though it were English.</para>
///
/// <para><b>Why it cannot be a per-fragment check.</b> Deltas are tokens, not lines. The marker
/// routinely arrives as <c>"--"</c>, <c>"-REQ"</c>, <c>"UEST"</c>, <c>"---"</c> across four
/// frames, and any check that looks at one fragment in isolation sees none of them. So the gate
/// holds back any trailing text that could still turn out to be the start of the marker, and
/// releases it as soon as the next fragment proves it is not.</para>
///
/// <para>The cost of the hold is bounded by the marker's own length — at most a dozen characters
/// of delay, which is far below the threshold where a listener notices.</para>
/// </summary>
public sealed class TrailerGate(string marker)
{
    private readonly StringBuilder held = new();
    private bool closed;

    /// <summary>True once the trailer has begun. Everything after is protocol.</summary>
    public bool Closed => closed;

    /// <summary>
    /// Take a fragment, give back the part that is safe to say out loud now.
    /// </summary>
    public string Admit(string? fragment)
    {
        if (closed || string.IsNullOrEmpty(fragment)) return "";

        held.Append(fragment);
        var text = held.ToString();

        var at = text.IndexOf(marker, StringComparison.Ordinal);
        if (at >= 0)
        {
            closed = true;
            held.Clear();
            return text[..at];
        }

        // Hold back the longest tail that could still become the marker. Anything before it is
        // now provably ordinary prose, whatever arrives next.
        var keep = LongestPrefixOverlap(text, marker);
        var release = text[..^keep];
        held.Clear();
        held.Append(text.AsSpan(text.Length - keep));
        return release;
    }

    /// <summary>
    /// Whatever is still held at the end of the stream.
    ///
    /// <para>Required. A reply ending in a literal "-" — an em-dash typed as one, a trailing list
    /// marker — leaves that character held forever, and an answer that silently loses its last
    /// character is the kind of bug nobody reports and everybody notices.</para>
    /// </summary>
    public string Flush()
    {
        if (closed) return "";
        var rest = held.ToString();
        held.Clear();
        return rest;
    }

    /// <summary>The length of the longest suffix of <paramref name="text"/> that is also a prefix
    /// of <paramref name="marker"/>.</summary>
    private static int LongestPrefixOverlap(string text, string marker)
    {
        var max = Math.Min(text.Length, marker.Length - 1);
        for (var n = max; n > 0; n--)
            if (string.CompareOrdinal(text, text.Length - n, marker, 0, n) == 0)
                return n;
        return 0;
    }
}
