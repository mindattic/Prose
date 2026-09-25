using NUnit.Framework;
using Prose.Core.Services;
using Prose.Core.Services.Discussion;

namespace Prose.UnitTests;

/// <summary>
/// The plain-text index map, which is what lets a span selected by a reader be written in markup.
///
/// <para>The map is built from the real parsers rather than a copy of their rules, and the test
/// that matters most is the one asserting its <c>Plain</c> is byte-identical to what the rest of
/// the system calls the reader's text. If those two ever disagree, the map is measuring something
/// nobody else believes in, and every offset it produces is wrong by however much they differ.</para>
/// </summary>
[TestFixture]
public class PlainTextMapTests
{
    private const string Tagged =
        "<entity repo=\"character\" guid=\"11111111-1111-1111-1111-111111111111\">Kyle</entity> "
        + "stepped into the *loading dock*. The pallets were **empty**.";

    [Test]
    public void Its_plain_text_is_what_the_rest_of_the_system_calls_the_readers_text()
    {
        var map = PlainTextMap.Build(Tagged);

        Assert.That(map.Plain, Is.EqualTo(BeatDiscussTarget.PlainText(Tagged)),
            "if these diverge, every offset the map produces points at the wrong character");
    }

    [Test]
    public void Maps_a_word_inside_a_tag_back_onto_that_words_characters()
    {
        var map = PlainTextMap.Build(Tagged);
        var at = map.Plain.IndexOf("Kyle", StringComparison.Ordinal);

        var (start, end) = map.ToSourceRange(at, at + 4);

        Assert.That(Tagged[start..end], Is.EqualTo("Kyle"),
            "the tag wrapper must not be dragged into the range, nor any of it left out");
    }

    [Test]
    public void Maps_a_word_inside_formatting_markers_back_onto_the_word_only()
    {
        var map = PlainTextMap.Build(Tagged);
        var at = map.Plain.IndexOf("empty", StringComparison.Ordinal);

        var (start, end) = map.ToSourceRange(at, at + 5);

        Assert.That(Tagged[start..end], Is.EqualTo("empty"));
    }

    [Test]
    public void A_span_reaching_the_end_maps_to_the_end_of_the_source()
    {
        var map = PlainTextMap.Build("plain text with no markup");

        var (_, end) = map.ToSourceRange(0, map.Plain.Length);

        Assert.That(end, Is.EqualTo("plain text with no markup".Length));
    }

    [Test]
    public void Every_plain_character_maps_to_the_same_character_in_the_source()
    {
        // The property the whole thing rests on, asserted position by position rather than at a
        // couple of hand-picked offsets.
        var map = PlainTextMap.Build(Tagged);

        for (var i = 0; i < map.Plain.Length; i++)
        {
            var (s, e) = map.ToSourceRange(i, i + 1);
            Assert.That(Tagged[s..e], Is.EqualTo(map.Plain[i].ToString()),
                $"plain index {i} ('{map.Plain[i]}') maps to the wrong source character");
        }
    }

    [Test]
    public void Empty_and_untagged_text_are_handled()
    {
        Assert.That(PlainTextMap.Build(null).Plain, Is.Empty);
        Assert.That(PlainTextMap.Build("").Plain, Is.Empty);
        Assert.That(PlainTextMap.Build("just words").Plain, Is.EqualTo("just words"));
    }
}

/// <summary>
/// The constrained span write — the whole safety story for an edit that arrived by voice.
///
/// <para>Typed, the author sees a draft and can compare it to what they meant. Spoken, there is no
/// draft: an instruction crosses a microphone, a transcriber, a model and a confirmation, and what
/// reaches the prose is whatever survived that. The one property that can be guaranteed
/// mechanically is the blast radius, so these tests attack it rather than demonstrate it.</para>
/// </summary>
[TestFixture]
public class SpanWriteTests
{
    private const string Beat =
        "Kyle stepped into the loading dock. The pallets were empty. "
        + "A phone lay on the concrete, recording his arrival. "
        + "He drew Silence and waited for the shift to change.";

    private static TextAnchor AnchorTo(string text, string quote)
    {
        var at = BeatDiscussTarget.PlainText(text).IndexOf(quote, StringComparison.Ordinal);
        Assert.That(at, Is.GreaterThanOrEqualTo(0), "test fixture: the quote is not in the text");
        return TextAnchoring.Capture(BeatDiscussTarget.PlainText(text), at, at + quote.Length);
    }

    [Test]
    public void Replaces_the_passage_and_nothing_else()
    {
        var anchor = AnchorTo(Beat, "The pallets were empty.");

        var result = SpanWrite.Apply(Beat, anchor, "The pallets were stacked two high.");

        Assert.That(result.Applied, Is.True, result.Reason);
        Assert.That(result.NewText, Does.Contain("The pallets were stacked two high."));
        Assert.That(result.NewText, Does.StartWith("Kyle stepped into the loading dock. "));
        Assert.That(result.NewText, Does.EndWith("He drew Silence and waited for the shift to change."));
        Assert.That(result.NewText, Does.Not.Contain("The pallets were empty."));
    }

    [Test]
    public void An_empty_replacement_is_a_deletion_not_a_refusal()
    {
        var anchor = AnchorTo(Beat, "A phone lay on the concrete, recording his arrival. ");

        var result = SpanWrite.Apply(Beat, anchor, "");

        Assert.That(result.Applied, Is.True, result.Reason);
        Assert.That(result.NewText, Does.Not.Contain("A phone lay"));
        Assert.That(result.NewText, Does.Contain("The pallets were empty. He drew Silence"));
    }

    [Test]
    public void Refuses_when_the_passage_is_no_longer_there()
    {
        // The proposal sat while autosave, a CLI command or another session rewrote the beat.
        var anchor = AnchorTo(Beat, "The pallets were empty.");
        var moved = Beat.Replace("The pallets were empty.", "The dock was swept clean.");

        var result = SpanWrite.Apply(moved, anchor, "anything at all");

        Assert.That(result.Applied, Is.False);
        Assert.That(result.Refusal, Is.EqualTo(SpanWriteRefusal.PassageGone));
        Assert.That(result.NewText, Is.Null);
    }

    [Test]
    public void Refuses_a_replacement_that_repeats_the_surrounding_prose()
    {
        // The model returned the WHOLE BEAT instead of the passage. The byte comparison cannot
        // catch this on its own: the surroundings would be preserved, with the replacement
        // duplicating them in the middle.
        var anchor = AnchorTo(Beat, "The pallets were empty.");

        var result = SpanWrite.Apply(Beat, anchor, Beat.Replace("empty", "bare"));

        Assert.That(result.Applied, Is.False);
        Assert.That(result.Refusal, Is.EqualTo(SpanWriteRefusal.SwallowsContext));
    }

    [Test]
    public void Allows_a_replacement_that_merely_shares_a_short_phrase_with_its_surroundings()
    {
        // The overlap rule must not fire on a name or a repeated clause the author is echoing
        // deliberately — a guard that refuses ordinary edits gets turned off.
        var anchor = AnchorTo(Beat, "The pallets were empty.");

        var result = SpanWrite.Apply(Beat, anchor, "The pallets were empty, and the dock was too.");

        Assert.That(result.Applied, Is.True, result.Reason);
    }

    [Test]
    public void Refuses_a_replacement_whose_markup_is_unclosed()
    {
        // The dangerous one. An unclosed tag does not fail loudly — it pairs with the next close
        // tag further down the beat and swallows everything between them.
        const string tagged =
            "Kyle waited. <entity guid=\"11111111-1111-1111-1111-111111111111\">Mira</entity> did not.";
        var anchor = AnchorTo(tagged, "Kyle waited.");

        var result = SpanWrite.Apply(tagged, anchor,
            "<entity guid=\"22222222-2222-2222-2222-222222222222\">Kyle waited.");

        Assert.That(result.Applied, Is.False);
        Assert.That(result.Refusal, Is.EqualTo(SpanWriteRefusal.MalformedReplacement));
    }

    [Test]
    public void Refuses_a_span_that_starts_part_way_through_an_entity_link()
    {
        // Selecting "yle stepped" cuts the tag in half. Replacing it would leave "<entity …>K"
        // behind, which survives every reader unchanged and is persisted as literal angle brackets.
        const string tagged =
            "<entity guid=\"11111111-1111-1111-1111-111111111111\">Kyle</entity> stepped into the dock.";
        var anchor = AnchorTo(tagged, "yle stepped");

        var result = SpanWrite.Apply(tagged, anchor, "nothing");

        Assert.That(result.Applied, Is.False);
        Assert.That(result.Refusal, Is.EqualTo(SpanWriteRefusal.SplitsMarkup));
    }

    [Test]
    public void A_span_with_one_edge_on_a_tagged_name_takes_the_whole_link()
    {
        const string tagged =
            "Then <entity guid=\"11111111-1111-1111-1111-111111111111\">Kyle</entity> stepped into the dock.";

        var starts = SpanWrite.Apply(tagged, AnchorTo(tagged, "Kyle stepped"), "Pixel walked");
        Assert.That(starts.Applied, Is.True, starts.Reason);
        Assert.That(starts.NewText, Is.EqualTo("Then Pixel walked into the dock."));

        var ends = SpanWrite.Apply(tagged, AnchorTo(tagged, "Then Kyle"), "Later Pixel");
        Assert.That(ends.Applied, Is.True, ends.Reason);
        Assert.That(ends.NewText, Is.EqualTo("Later Pixel stepped into the dock."));
    }

    [Test]
    public void Refuses_a_span_that_cuts_an_emphasis_pair_and_would_restyle_text_outside_it()
    {
        const string styled = "Kyle stepped into the *loading dock*. Later she was *tired*.";

        var result = SpanWrite.Apply(styled, AnchorTo(styled, "into the loading"), "toward the");

        Assert.That(result.Applied, Is.False, "the orphaned * would italicise '. Later she was '");
        Assert.That(result.Refusal, Is.EqualTo(SpanWriteRefusal.SplitsMarkup));

        var whole = SpanWrite.Apply(styled, AnchorTo(styled, "into the loading dock"), "toward the *gate*");
        Assert.That(whole.Applied, Is.True, whole.Reason);
    }

    [Test]
    public void Replaces_a_whole_tagged_name_cleanly()
    {
        const string tagged =
            "<entity guid=\"11111111-1111-1111-1111-111111111111\">Kyle</entity> stepped into the dock.";
        var anchor = AnchorTo(tagged, "Kyle");

        var result = SpanWrite.Apply(tagged, anchor, "Pixel");

        Assert.That(result.Applied, Is.True, result.Reason);
        Assert.That(result.NewText, Is.EqualTo("Pixel stepped into the dock."),
            "the dead wrapper goes with the name it wrapped — a tag around nothing is worse debris");
    }

    [Test]
    public void Refuses_when_the_passage_now_occurs_twice_with_nothing_to_tell_them_apart()
    {
        const string doubled = "He waited. He waited.";
        // Start = -1 is what Locate uses when the stored offsets no longer hold, which is the
        // situation that actually produces ambiguity: two identical candidates, nothing around
        // either one to tell them apart, and no usable position to fall back on.
        var anchor = new TextAnchor("He waited.", "", "", -1, -1);

        var result = SpanWrite.Apply(doubled, anchor, "He did not wait.");

        Assert.That(result.Applied, Is.False);
        Assert.That(result.Refusal, Is.EqualTo(SpanWriteRefusal.Ambiguous));
    }

    [Test]
    public void Refuses_a_replacement_identical_to_what_is_there()
    {
        var anchor = AnchorTo(Beat, "The pallets were empty.");

        var result = SpanWrite.Apply(Beat, anchor, "The pallets were empty.");

        Assert.That(result.Applied, Is.False);
        Assert.That(result.Refusal, Is.EqualTo(SpanWriteRefusal.NoChange));
    }

    [Test]
    public void Everything_outside_the_span_is_byte_identical_on_every_accepted_write()
    {
        // The property the whole feature exists to guarantee, asserted over every span in the beat
        // rather than at a couple of chosen offsets.
        var plain = BeatDiscussTarget.PlainText(Beat);

        for (var start = 0; start < plain.Length - 12; start += 7)
        {
            var end = Math.Min(plain.Length, start + 12);
            var anchor = TextAnchoring.Capture(plain, start, end);
            var result = SpanWrite.Apply(Beat, anchor, "XYZZY");
            if (!result.Applied) continue;   // a refusal is a pass: nothing was written

            var before = Beat[..start];
            Assert.That(result.NewText![..start], Is.EqualTo(before),
                $"prose before the span at {start} changed");

            var tail = Beat[end..];
            Assert.That(result.NewText![^tail.Length..], Is.EqualTo(tail),
                $"prose after the span at {start} changed");
        }
    }

    [Test]
    public void Null_and_empty_inputs_refuse_rather_than_throw()
    {
        var anchor = new TextAnchor("anything", "", "", 0, 8);

        Assert.That(SpanWrite.Apply(null, anchor, "x").Applied, Is.False);
        Assert.That(SpanWrite.Apply("", anchor, "x").Applied, Is.False);
    }
}
