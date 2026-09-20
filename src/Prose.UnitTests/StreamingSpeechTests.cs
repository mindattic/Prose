using NUnit.Framework;
using Prose.Core.Services.Discussion;

namespace Prose.UnitTests;

/// <summary>
/// Cutting a streamed answer into sentences so the first can be spoken while the rest is written.
///
/// <para><b>The asymmetry that shapes every case here.</b> A false split is audible and bad: the
/// voice stops dead on "Mr." and starts again on "Chen was waiting" with a fresh breath, and the
/// author hears a machine reading badly. A missed split costs a fraction of a second on one chunk
/// and nobody notices. So every ambiguous case must resolve towards NOT splitting, and the tests
/// below are mostly about the things that look like sentence ends and are not.</para>
/// </summary>
[TestFixture]
public class SentenceChunkerTests
{
    /// <summary>Feeds text one character at a time — the worst case, and close to what a token
    /// stream actually looks like. A chunker that only works on whole sentences would pass a
    /// naive test and fail here.</summary>
    private static (List<string> Chunks, string? Tail) RunCharByChar(string text)
    {
        var chunker = new SentenceChunker();
        var chunks = new List<string>();
        foreach (var c in text) chunks.AddRange(chunker.Add(c.ToString()));
        return (chunks, chunker.Flush());
    }

    [Test]
    public void Splits_at_the_end_of_each_sentence()
    {
        var (chunks, tail) = RunCharByChar(
            "The camphor belongs to this one room and earns its place. "
            + "The ozone does not, and it is in eight of them.");

        Assert.That(chunks, Is.EqualTo(new[]
        {
            "The camphor belongs to this one room and earns its place.",
            "The ozone does not, and it is in eight of them.",
        }));
        Assert.That(tail, Is.Null, "both sentences ended in a full stop, so nothing is left held");
    }

    [Test]
    public void Does_not_split_on_an_abbreviation()
    {
        const string line = "Mrs. Chen is the vendor in this scene and has been since chapter two.";

        var (chunks, _) = RunCharByChar(line);

        Assert.That(chunks, Is.EqualTo(new[] { line }),
            "the only split is the real one at the end — \"Mrs.\" must not start a new breath");
    }

    [Test]
    public void Does_not_split_inside_a_decimal()
    {
        const string line =
            "The mesh reads 22.15 across the whole of the lower deck, which is the same as before.";

        var (chunks, _) = RunCharByChar(line);

        Assert.That(chunks, Is.EqualTo(new[] { line }));
    }

    [Test]
    public void Does_not_split_on_an_initial()
    {
        const string line =
            "It is attributed to J. Kyle in the ledger and nowhere else in the manuscript.";

        var (chunks, _) = RunCharByChar(line);

        Assert.That(chunks, Is.EqualTo(new[] { line }));
    }

    [Test]
    public void A_very_short_sentence_waits_for_the_next_one()
    {
        // "No." alone is a round trip and an audio element for one syllable, followed by an
        // audible seam. It rides along with what follows instead.
        var (chunks, tail) = RunCharByChar("No. That line is load-bearing and cutting it breaks #412.");

        Assert.That(chunks, Is.Empty);
        Assert.That(tail, Is.EqualTo("No. That line is load-bearing and cutting it breaks #412."));
    }

    [Test]
    public void Question_and_exclamation_marks_end_a_sentence_too()
    {
        var (chunks, _) = RunCharByChar(
            "Did you intend the second mention to echo the first one at all? And if so, where?");

        Assert.That(chunks, Has.Count.EqualTo(1));
        Assert.That(chunks[0], Does.EndWith("at all?"));
    }

    [Test]
    public void Never_splits_inside_a_fenced_block()
    {
        // The fence is prose being SHOWN to the author, not told to them. Reading it aloud
        // sentence by sentence would be wrong even if the sentences were right.
        var (chunks, _) = RunCharByChar(
            "Here is the replacement, which keeps the image and loses the clause.\n"
            + "```\nHe drew Silence. The dock was empty. He waited.\n```\n");

        Assert.That(chunks[0], Does.StartWith("Here is the replacement"));

        // The fence comes back whole. What must NOT happen is its three sentences arriving as
        // three chunks, each spoken with its own breath.
        var fenced = chunks.Single(c => c.Contains("He drew Silence"));
        Assert.That(fenced, Does.Contain("The dock was empty."));
        Assert.That(fenced, Does.Contain("He waited."));
    }

    [Test]
    public void Flush_returns_a_final_sentence_that_never_got_a_full_stop()
    {
        // A list, a fenced block, a trailing clause. Without the flush the author hears a reply
        // that stops one sentence early with no indication that it did.
        var chunker = new SentenceChunker();
        chunker.Add("- it plants the ledger\n");
        chunker.Add("- it is the only description of the machine");

        Assert.That(chunker.Flush(), Is.EqualTo("- it is the only description of the machine"));
    }

    [Test]
    public void Flush_on_an_empty_chunker_returns_nothing()
    {
        Assert.That(new SentenceChunker().Flush(), Is.Null);
    }

    [Test]
    public void Chunking_loses_nothing_from_the_original_text()
    {
        // The property that matters most: whatever the splits are, the author must hear the whole
        // answer. Any rule added later has to keep this true.
        const string answer =
            "Mrs. Chen appears in 3.5 percent of the beats. That is not the problem here! "
            + "The problem is that the ozone attaches to nothing at all, and J. Kyle is the only "
            + "witness to any of it. Cut it?";

        var (chunks, tail) = RunCharByChar(answer);

        var rebuilt = string.Join(" ", chunks.Concat(tail is null ? [] : new[] { tail }));
        Assert.That(Squash(rebuilt), Is.EqualTo(Squash(answer)));

        static string Squash(string s) => string.Join(" ", s.Split((char[]?)null,
            StringSplitOptions.RemoveEmptyEntries));
    }
}

/// <summary>
/// Keeping the confirmation protocol out of the author's ears.
///
/// <para>The trailer arrives in the same token stream as the prose, and the prose is being spoken
/// aloud as it arrives. Without the gate the author hears "dash dash dash REQUEST dash dash dash,
/// RESTATEMENT colon…" read out as though it were English.</para>
///
/// <para><b>Why a per-fragment check cannot work, and why these tests split the marker.</b> Deltas
/// are tokens. The marker routinely arrives across several frames, and any check that inspects one
/// fragment in isolation sees none of them.</para>
/// </summary>
[TestFixture]
public class TrailerGateTests
{
    private const string Marker = "---REQUEST---";

    private static string Admit(params string[] fragments)
    {
        var gate = new TrailerGate(Marker);
        var said = string.Concat(fragments.Select(gate.Admit));
        return said + gate.Flush();
    }

    [Test]
    public void Ordinary_prose_passes_through_unchanged()
    {
        Assert.That(Admit("The camphor ", "earns its ", "place."),
                    Is.EqualTo("The camphor earns its place."));
    }

    [Test]
    public void The_marker_stops_everything_after_it()
    {
        Assert.That(Admit("Safe to cut.\n", Marker, "\nRESTATEMENT: Cut it."),
                    Is.EqualTo("Safe to cut.\n"));
    }

    [Test]
    public void The_marker_is_caught_even_when_it_arrives_a_token_at_a_time()
    {
        // This is the case the naive implementation gets wrong, and the reason this class exists.
        var said = Admit("Safe to cut.\n", "--", "-REQ", "UEST", "---", "\nRESTATEMENT: Cut it.");

        Assert.That(said, Is.EqualTo("Safe to cut.\n"));
        Assert.That(said, Does.Not.Contain("REQ"));
    }

    [Test]
    public void A_dash_that_turns_out_to_be_prose_is_released_not_swallowed()
    {
        // An em-dash typed as hyphens, mid-sentence. Held for one fragment, then let go.
        Assert.That(Admit("It is load-bearing ", "--", " and cutting it breaks #412."),
                    Is.EqualTo("It is load-bearing -- and cutting it breaks #412."));
    }

    [Test]
    public void A_reply_ending_in_a_partial_marker_does_not_lose_its_last_characters()
    {
        // The flush. Without it a reply ending in "-" silently loses that character forever, which
        // is the kind of bug nobody reports and everybody notices.
        Assert.That(Admit("Cut it, then reflow", " --"), Is.EqualTo("Cut it, then reflow --"));
    }

    [Test]
    public void Nothing_escapes_after_the_gate_has_closed()
    {
        var gate = new TrailerGate(Marker);
        gate.Admit("Fine.\n" + Marker);

        Assert.That(gate.Admit("RESTATEMENT: Cut it."), Is.Empty);
        Assert.That(gate.Flush(), Is.Empty);
        Assert.That(gate.Closed, Is.True);
    }

    [Test]
    public void An_empty_or_null_fragment_is_harmless()
    {
        var gate = new TrailerGate(Marker);
        Assert.That(gate.Admit(null), Is.Empty);
        Assert.That(gate.Admit(""), Is.Empty);
        Assert.That(gate.Closed, Is.False);
    }
}
