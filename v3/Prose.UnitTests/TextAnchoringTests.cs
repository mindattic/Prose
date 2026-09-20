using NUnit.Framework;
using Prose.Core.Services.Discussion;

namespace Prose.UnitTests;

/// <summary>
/// Re-anchoring a discussion after the text around it has moved.
///
/// <para><b>Why offsets alone cannot be trusted here.</b> Every beat save re-derives entity tags
/// (<c>NodeWorkbenchService.UpdateBeatTextAsync</c>), so <c>Beat.Text</c> can grow or shrink by
/// dozens of characters — a single <c>&lt;entity guid="…"&gt;</c> wrapper is ~55 — without the
/// author touching the sentence a thread is attached to. A thread anchored by position alone
/// would silently slide onto different words, which is the failure mode this whole feature exists
/// to prevent, reproduced one level down.</para>
///
/// <para>The asymmetry that shapes these tests: an anchor must re-find its passage after ordinary
/// editing, and must <b>refuse</b> to guess when the passage is genuinely gone. Quietly pointing
/// at the wrong sentence is far worse than admitting the thread is detached, so every recovery
/// test below has a matching negative.</para>
/// </summary>
[TestFixture]
public class TextAnchoringTests
{
    private const string Beat =
        "Kyle stepped into the loading dock. The pallets were empty. " +
        "A phone lay on the concrete, recording his arrival. " +
        "He drew Silence and waited for the shift to change.";

    private static TextAnchor AnchorTo(string text, string quote)
    {
        var i = text.IndexOf(quote, StringComparison.Ordinal);
        Assert.That(i, Is.GreaterThanOrEqualTo(0), $"fixture does not contain \"{quote}\"");
        return TextAnchoring.Capture(text, i, i + quote.Length);
    }

    // ── The anchor holds ───────────────────────────────────────────────────

    [Test]
    public void Unchanged_text_resolves_exactly()
    {
        var anchor = AnchorTo(Beat, "The pallets were empty.");

        var result = TextAnchoring.Resolve(Beat, anchor);

        Assert.That(result.Outcome, Is.EqualTo(AnchorOutcome.Exact));
        Assert.That(Beat[result.Start..result.End], Is.EqualTo("The pallets were empty."));
    }

    [Test]
    public void Survives_an_edit_earlier_in_the_beat()
    {
        var anchor = AnchorTo(Beat, "recording his arrival");
        // The shape an entity-tag re-derivation makes: text inserted well before the anchor.
        var edited = Beat.Replace("Kyle stepped", "<entity guid=\"019d\">Kyle</entity> stepped");

        var result = TextAnchoring.Resolve(edited, anchor);

        Assert.That(result.Outcome, Is.EqualTo(AnchorOutcome.Reanchored));
        Assert.That(edited[result.Start..result.End], Is.EqualTo("recording his arrival"));
    }

    [Test]
    public void Survives_the_beat_being_shortened_past_the_old_offsets()
    {
        var anchor = AnchorTo(Beat, "He drew Silence");
        var edited = Beat.Replace("Kyle stepped into the loading dock. The pallets were empty. ", "");

        var result = TextAnchoring.Resolve(edited, anchor);

        Assert.That(result.Outcome, Is.EqualTo(AnchorOutcome.Reanchored));
        Assert.That(edited[result.Start..result.End], Is.EqualTo("He drew Silence"));
    }

    // ── The anchor refuses to guess ────────────────────────────────────────

    [Test]
    public void Detaches_when_the_quoted_sentence_is_deleted()
    {
        var anchor = AnchorTo(Beat, "The pallets were empty.");
        var edited = Beat.Replace("The pallets were empty. ", "");

        var result = TextAnchoring.Resolve(edited, anchor);

        Assert.That(result.Outcome, Is.EqualTo(AnchorOutcome.Detached));
        Assert.That(result.Found, Is.False);
    }

    [Test]
    public void Detaches_rather_than_matching_a_partial_rewrite()
    {
        var anchor = AnchorTo(Beat, "He drew Silence and waited");
        var edited = Beat.Replace("He drew Silence and waited", "He drew Cacophony and waited");

        var result = TextAnchoring.Resolve(edited, anchor);

        Assert.That(result.Outcome, Is.EqualTo(AnchorOutcome.Detached));
    }

    [Test]
    public void An_empty_quote_never_matches()
    {
        var result = TextAnchoring.Resolve(Beat, new TextAnchor("", "", "", 0, 0));

        Assert.That(result.Outcome, Is.EqualTo(AnchorOutcome.Detached));
    }

    // ── Repeated text ──────────────────────────────────────────────────────

    [Test]
    public void Context_picks_the_right_one_of_two_identical_sentences()
    {
        const string doubled =
            "She waited. The rain kept on. She waited. The dock stayed empty.";
        // The SECOND "She waited." — distinguishable only by what follows it.
        var second = doubled.LastIndexOf("She waited.", StringComparison.Ordinal);
        var anchor = TextAnchoring.Capture(doubled, second, second + "She waited.".Length);

        // Something changes at the front, so the offsets no longer line up.
        var edited = doubled.Replace("She waited. The rain", "She waited a while. The rain");

        var result = TextAnchoring.Resolve(edited, anchor);

        Assert.That(result.Outcome, Is.EqualTo(AnchorOutcome.Reanchored));
        Assert.That(edited[result.Start..], Does.StartWith("She waited. The dock stayed empty."),
            "must land on the occurrence whose surroundings match, not simply the first one");
    }

    [Test]
    public void Identical_context_reports_ambiguous_and_takes_the_nearest()
    {
        // Genuine ambiguity is harder to construct than it looks. Two copies of a sentence are
        // usually still distinguishable, because the text on either side differs — which is the
        // whole point of keeping prefix and suffix. To tie, the surroundings must match for the
        // full ContextLength in BOTH directions, so the fixture is deliberately periodic and the
        // two candidates are taken from the middle, away from the string's ends.
        const string unit = "alpha REPEAT omega ";                 // 19 chars
        var text = string.Concat(Enumerable.Repeat(unit, 10));     // 190 chars
        var anchored = 6 + (19 * 5);                               // an interior occurrence
        var anchor = TextAnchoring.Capture(text, anchored, anchored + "REPEAT".Length);

        Assert.That(anchor.Prefix.Length, Is.EqualTo(TextAnchoring.ContextLength),
            "fixture must give the anchor a full context window on both sides");
        Assert.That(anchor.Suffix.Length, Is.EqualTo(TextAnchoring.ContextLength));

        // Shift everything so the stored offsets are stale, without disturbing the periodicity
        // around the interior candidates.
        var edited = "xx" + text;

        var result = TextAnchoring.Resolve(edited, anchor);

        Assert.That(result.Outcome, Is.EqualTo(AnchorOutcome.Ambiguous),
            "the panel must be able to say the anchor is uncertain rather than imply confidence");
        Assert.That(edited[result.Start..result.End], Is.EqualTo("REPEAT"));
        Assert.That(result.Start, Is.EqualTo(anchored + 2),
            "with nothing to choose between them, the one nearest the original position wins");
    }

    // ── Capture ────────────────────────────────────────────────────────────

    [Test]
    public void Capture_takes_context_from_both_sides_and_clamps_at_the_edges()
    {
        var anchor = AnchorTo(Beat, "A phone lay on the concrete");

        Assert.That(anchor.Prefix, Does.EndWith("The pallets were empty. "));
        Assert.That(anchor.Suffix, Does.StartWith(", recording"));
        Assert.That(anchor.Prefix.Length, Is.LessThanOrEqualTo(TextAnchoring.ContextLength));
        Assert.That(anchor.Suffix.Length, Is.LessThanOrEqualTo(TextAnchoring.ContextLength));

        var atStart = TextAnchoring.Capture(Beat, 0, 4);
        Assert.That(atStart.Quote, Is.EqualTo("Kyle"));
        Assert.That(atStart.Prefix, Is.Empty, "no text before offset 0 to borrow");
    }

    [Test]
    public void Capture_clamps_a_selection_that_runs_past_the_end()
    {
        var anchor = TextAnchoring.Capture("short", 3, 999);

        Assert.That(anchor.Quote, Is.EqualTo("rt"));
        Assert.That(TextAnchoring.Resolve("short", anchor).Outcome, Is.EqualTo(AnchorOutcome.Exact));
    }
}
