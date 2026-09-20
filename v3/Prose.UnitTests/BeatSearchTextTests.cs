using NUnit.Framework;
using Prose.Core.Services;

namespace Prose.UnitTests;

/// <summary>
/// Finding a passage in a book, and pointing at the right characters when you do.
///
/// <para><b>Why the offset matters as much as the match.</b> A hit list the author cannot trust is
/// worse than no hit list: an excerpt with the highlight one character to the left looks like the
/// search found something slightly different from what it found, and the natural conclusion is
/// that the search is unreliable rather than that the arithmetic is. The ellipsis on a truncated
/// excerpt is part of that arithmetic, which is exactly the sort of thing that is right in the
/// middle of a beat and wrong at its start.</para>
/// </summary>
[TestFixture]
public class BeatSearchTextTests
{
    private const string Beat =
        "Kyle stepped into the loading dock. The pallets were empty and smelled of camphor. "
        + "Kyle's hands were steady. He drew Silence and waited for the shift to change.";

    [Test]
    public void Counts_every_occurrence_and_reports_the_first()
    {
        var n = BeatSearchText.Count(Beat, "Kyle", StringComparison.Ordinal, false, out var first);

        Assert.That(n, Is.EqualTo(2), "\"Kyle\" and the \"Kyle\" inside \"Kyle's\"");
        Assert.That(first, Is.Zero);
    }

    [Test]
    public void Whole_word_treats_an_apostrophe_as_part_of_the_word()
    {
        // A novelist searching for a bare name is usually after the places it is NOT possessive.
        var n = BeatSearchText.Count(Beat, "Kyle", StringComparison.Ordinal, wholeWord: true, out _);

        Assert.That(n, Is.EqualTo(1), "\"Kyle's\" is a different word");
    }

    [Test]
    public void Whole_word_also_handles_a_typographic_apostrophe()
    {
        // The manuscript uses curly quotes; a rule that only knows the ASCII one would silently
        // behave differently on half the book.
        var n = BeatSearchText.Count("Kyle’s hands were steady.", "Kyle",
                                     StringComparison.Ordinal, wholeWord: true, out _);

        Assert.That(n, Is.Zero);
    }

    [Test]
    public void Case_sensitivity_is_the_callers_choice()
    {
        Assert.That(BeatSearchText.Count(Beat, "kyle", StringComparison.Ordinal, false, out _),
                    Is.Zero);
        Assert.That(BeatSearchText.Count(Beat, "kyle", StringComparison.OrdinalIgnoreCase, false, out _),
                    Is.EqualTo(2));
    }

    [Test]
    public void Overlapping_occurrences_are_counted_separately()
    {
        // "aa" occurs three times in "aaaa", not two. They are separate places in the prose.
        var n = BeatSearchText.Count("aaaa", "aa", StringComparison.Ordinal, false, out _);

        Assert.That(n, Is.EqualTo(3));
    }

    [Test]
    public void No_match_reports_minus_one_rather_than_zero()
    {
        // Zero would be a valid offset, and a caller that trusted it would excerpt from the start
        // of the beat and highlight its first characters for a search that matched nothing.
        var n = BeatSearchText.Count(Beat, "ozone", StringComparison.Ordinal, false, out var first);

        Assert.That(n, Is.Zero);
        Assert.That(first, Is.EqualTo(-1));
    }

    [Test]
    public void An_empty_query_matches_nothing_rather_than_everything()
    {
        Assert.That(BeatSearchText.Count(Beat, "", StringComparison.Ordinal, false, out _), Is.Zero);
        Assert.That(BeatSearchText.Count("", "Kyle", StringComparison.Ordinal, false, out _), Is.Zero);
    }

    // ── excerpting ─────────────────────────────────────────────────────────

    [Test]
    public void The_reported_offset_lands_exactly_on_the_match()
    {
        var at = Beat.IndexOf("camphor", StringComparison.Ordinal);

        var (excerpt, start) = BeatSearchText.Excerpt(Beat, at, "camphor".Length, radius: 20);

        Assert.That(excerpt.Substring(start, "camphor".Length), Is.EqualTo("camphor"),
            "the highlight must cover the match, ellipsis included in the arithmetic");
    }

    [Test]
    public void A_match_at_the_very_start_has_no_leading_ellipsis_and_offset_zero()
    {
        var (excerpt, start) = BeatSearchText.Excerpt(Beat, 0, 4, radius: 20);

        Assert.That(excerpt, Does.StartWith("Kyle"));
        Assert.That(start, Is.Zero);
    }

    [Test]
    public void A_match_at_the_very_end_has_no_trailing_ellipsis()
    {
        const string text = "He waited for the shift to change.";
        var at = text.IndexOf("change.", StringComparison.Ordinal);

        var (excerpt, start) = BeatSearchText.Excerpt(text, at, "change.".Length, radius: 10);

        Assert.That(excerpt, Does.Not.EndWith("…"));
        Assert.That(excerpt.Substring(start), Is.EqualTo("change."));
    }

    [Test]
    public void Newlines_are_flattened_so_a_hit_row_stays_one_line()
    {
        var (excerpt, _) = BeatSearchText.Excerpt("first line\nsecond line", 11, 6, radius: 40);

        Assert.That(excerpt, Does.Not.Contain("\n"));
    }

    [Test]
    public void A_short_beat_is_returned_whole_with_no_ellipsis_at_either_end()
    {
        var (excerpt, start) = BeatSearchText.Excerpt("Kyle waited.", 0, 4, radius: 200);

        Assert.That(excerpt, Is.EqualTo("Kyle waited."));
        Assert.That(start, Is.Zero);
    }
}
