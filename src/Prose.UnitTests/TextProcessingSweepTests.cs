using NUnit.Framework;
using Prose.Core.Services;
using Prose.Core.Services.Factory;

namespace Prose.UnitTests;

/// <summary>
/// Pins for the 2026-09-29 text-processing sweep: measurements taken on stored markup instead of
/// reader-visible text, \b around names with edge punctuation, and excerpts that split a
/// surrogate pair.
/// </summary>
[TestFixture]
public class TextProcessingSweepTests
{
    private static readonly Guid Kyle = Guid.Parse("01a0030b-0000-7000-8000-000000000001");

    private static string Tag(string surface) =>
        $"<entity repo=\"character\" guid=\"{Kyle}\">{surface}</entity>";

    [Test]
    public void ProseStats_counts_reader_words_and_ignores_attribute_quotes()
    {
        var plain = "Kyle stepped into the rain.\nHe waited.";
        var tagged = $"{Tag("Kyle")} stepped into the rain.\nHe waited.";

        var p = ProseStatsService.Analyze(Guid.Empty, plain);
        var t = ProseStatsService.Analyze(Guid.Empty, tagged);

        Assert.That(t.WordCount, Is.EqualTo(p.WordCount), "a tag is one word, not three");
        Assert.That(t.DialogueFraction, Is.EqualTo(0), "repo=\"…\" quotes are not dialogue");
    }

    [Test]
    public void ProseStats_reads_curly_quotes_as_dialogue()
    {
        var s = ProseStatsService.Analyze(Guid.Empty, "“Go,” he said.\nShe went.");
        Assert.That(s.DialogueFraction, Is.EqualTo(0.5));
    }

    [Test]
    public void BeatProseMetrics_are_the_same_with_or_without_tags()
    {
        var plain = "Kyle stepped into the rain. Kyle waited for the shift to change.";
        var tagged = $"{Tag("Kyle")} stepped into the rain. {Tag("Kyle")} waited for the shift to change.";

        var p = BeatProseMetricsService.Compute(Guid.Empty, Guid.Empty, plain);
        var t = BeatProseMetricsService.Compute(Guid.Empty, Guid.Empty, tagged);

        Assert.That(t.WordCount, Is.EqualTo(p.WordCount));
        Assert.That(t.TypeTokenRatio, Is.EqualTo(p.TypeTokenRatio));
        Assert.That(t.DialogueProportion, Is.EqualTo(0));
    }

    [Test]
    public void Excerpt_never_splits_a_surrogate_pair()
    {
        // "😮" is two UTF-16 code units; put one right at each window edge.
        var text = "ab😮cd MATCH ef😮gh";
        var at = text.IndexOf("MATCH", StringComparison.Ordinal);
        // radius chosen so the raw window would start on the low half and end after the high half
        var (excerpt, start) = BeatSearchText.Excerpt(text, at, 5, radius: 4);

        for (var i = 0; i < excerpt.Length; i++)
        {
            if (char.IsHighSurrogate(excerpt[i]))
                Assert.That(i + 1 < excerpt.Length && char.IsLowSurrogate(excerpt[i + 1]), $"lone high surrogate at {i}");
            if (char.IsLowSurrogate(excerpt[i]))
                Assert.That(i > 0 && char.IsHighSurrogate(excerpt[i - 1]), $"lone low surrogate at {i}");
        }
        Assert.That(excerpt.Substring(start, 5), Is.EqualTo("MATCH"), "the highlight still lands on the match");
    }

    [Test]
    public void PinName_matches_a_name_that_ends_in_punctuation()
    {
        var stored = "The E.L.F. unit stood down.";
        var pinned = CaptureScanner.PinName(stored, "E.L.F.", Kyle, "character");

        Assert.That(BeatMarkup.CountTagsByEntity(pinned).GetValueOrDefault(Kyle), Is.EqualTo(1));
        Assert.That(BeatMarkup.StripEntityTags(pinned), Is.EqualTo(stored));
    }

    [Test]
    public void DraftGate_finds_a_required_name_that_ends_in_punctuation()
    {
        var draft = string.Join(' ', Enumerable.Repeat("word", 130)) + " Then the E.L.F. came.";
        var brief = new BeatBrief { Goal = "g", MustInclude = ["The E.L.F."] };

        var report = DraftGate.Check(draft, brief);

        Assert.That(report.Failures.Any(f => f.Contains("E.L.F.", StringComparison.Ordinal)), Is.False);
    }
}
