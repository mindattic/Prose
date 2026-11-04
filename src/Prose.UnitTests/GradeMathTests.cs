using NUnit.Framework;
using Prose.Core.Services.Factory;

namespace Prose.UnitTests;

/// <summary>The pure half of the version grader (PRS-T866): rollup sums, weighted win count,
/// version-spec parsing and archive parsing.</summary>
[TestFixture]
public class GradeMathTests
{
    [Test]
    public void Rollup_sums_sums_and_bases_so_rates_are_not_a_mean_of_means()
    {
        IReadOnlyList<GradeMeasure> book1 = [new("words", "Words", GradeBetter.Neutral, 1000), new("ease", "Ease", GradeBetter.Higher, 60 * 1000, 1000)];
        IReadOnlyList<GradeMeasure> book2 = [new("words", "Words", GradeBetter.Neutral, 3000), new("ease", "Ease", GradeBetter.Higher, 80 * 3000, 3000)];

        var r = GradeMath.Rollup([book1, book2]);

        Assert.That(r.Select(m => m.Key), Is.EqualTo(new[] { "words", "ease" }));
        Assert.That(r[0].Value, Is.EqualTo(4000));
        Assert.That(r[0].Base, Is.Null, "a total stays a total");
        Assert.That(r[1].Value, Is.EqualTo(75).Within(1e-9), "word-weighted, not (60+80)/2");
    }

    [Test]
    public void Ratio_with_zero_base_has_no_value_and_is_not_won()
    {
        var c = GradeMath.Compare([new("paid", "Paid", GradeBetter.Higher, 0, 0, 100)], [new("paid", "Paid", GradeBetter.Higher, 1, 2, 100)]);
        Assert.That(c.Rows[0].A, Is.Null);
        Assert.That(c.Rows[0].B, Is.EqualTo(50));
        Assert.That(c.Rows[0].Winner, Is.EqualTo(""));
        Assert.That(c.AWins + c.BWins + c.Ties, Is.Zero);
    }

    [Test]
    public void Compare_counts_direction_ties_and_ignores_neutral_rows()
    {
        IReadOnlyList<GradeMeasure> a = [
            new("words", "Words", GradeBetter.Neutral, 100),
            new("ease", "Ease", GradeBetter.Higher, 70),
            new("law_hits", "Laws", GradeBetter.Lower, 3),
            new("dangling", "Dangling", GradeBetter.Lower, 1.001),
        ];
        IReadOnlyList<GradeMeasure> b = [
            new("words", "Words", GradeBetter.Neutral, 900),
            new("ease", "Ease", GradeBetter.Higher, 65),
            new("law_hits", "Laws", GradeBetter.Lower, 0),
            new("dangling", "Dangling", GradeBetter.Lower, 1.0),
        ];

        var c = GradeMath.Compare(a, b);

        Assert.That(c.Rows.Select(r => r.Winner), Is.EqualTo(new[] { "", "A", "B", "=" }));
        Assert.That((c.AWins, c.BWins, c.Ties), Is.EqualTo((1.0, 1.0, 1.0)));
        Assert.That(c.Verdict, Is.EqualTo("even"));
    }

    [Test]
    public void Weights_by_key_then_by_prefix_default_one()
    {
        IReadOnlyList<GradeMeasure> a = [new("law_hits", "Laws", GradeBetter.Lower, 0), new("metric:abcd1234", "M", GradeBetter.Lower, 5, 1000, 1000), new("ease", "Ease", GradeBetter.Higher, 50)];
        IReadOnlyList<GradeMeasure> b = [new("law_hits", "Laws", GradeBetter.Lower, 2), new("metric:abcd1234", "M", GradeBetter.Lower, 1, 1000, 1000), new("ease", "Ease", GradeBetter.Higher, 60)];
        var weights = new Dictionary<string, double> { ["LAW_HITS"] = 3, ["metric"] = 0.5 };

        var c = GradeMath.Compare(a, b, weights);

        Assert.That(c.AWins, Is.EqualTo(3));
        Assert.That(c.BWins, Is.EqualTo(1.5));
        Assert.That(c.Verdict, Is.EqualTo("A"));
    }

    [Test]
    public void Zero_weight_row_is_shown_but_counts_for_nobody()
    {
        var c = GradeMath.Compare([new("ease", "Ease", GradeBetter.Higher, 70)], [new("ease", "Ease", GradeBetter.Higher, 10)],
            new Dictionary<string, double> { ["ease"] = 0 });
        Assert.That(c.Rows[0].Winner, Is.EqualTo("A"));
        Assert.That(c.AWins, Is.Zero);
        Assert.That(c.Verdict, Is.EqualTo("even"));
    }

    [Test]
    public void A_key_only_one_side_has_is_shown_unwon()
    {
        var c = GradeMath.Compare([new("metric:x", "X", GradeBetter.Lower, 1)], [new("metric:y", "Y", GradeBetter.Lower, 2)]);
        Assert.That(c.Rows.Select(r => (r.Key, r.A, r.B, r.Winner)),
            Is.EqualTo(new (string, double?, double?, string)[] { ("metric:x", 1, null, ""), ("metric:y", null, 2, "") }));
    }

    [TestCase("working", true, null)]
    [TestCase("V12", false, 12)]
    [TestCase("v3", false, 3)]
    [TestCase("40", false, 40)]
    public void Version_spec_parses(string raw, bool working, int? version)
    {
        var s = GradeVersionSpec.Parse(raw);
        Assert.That((s.Working, s.Version), Is.EqualTo((working, version)));
    }

    [Test]
    public void Bare_date_means_end_of_that_day()
    {
        var s = GradeVersionSpec.Parse("2026-10-01");
        Assert.That(s.Before, Is.EqualTo(new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc)));
        Assert.That(s.Label, Is.EqualTo("2026-10-01"));
        Assert.Throws<ArgumentException>(() => GradeVersionSpec.Parse("someday"));
    }

    [Test]
    public void Archive_parses_beats_and_chapters()
    {
        var id1 = Guid.NewGuid(); var id2 = Guid.NewGuid();
        var md = $"# Book\n### Sub\n\n_by MindAttic_\n\n## Chapter 1 — One\n\n<!-- beat:1:{id1:N} -->\nFirst para.\n\nSecond para.\n\n### A sub-heading\n\n## Chapter 2 — Two\n\n<!-- beat:2:{id2:N} -->\nThird.\n";

        var (beats, chapters) = VersionGrader.ParseArchive(md);

        Assert.That(chapters, Is.EqualTo(2));
        Assert.That(beats.Select(b => b.BeatId), Is.EqualTo(new Guid?[] { id1, id2 }));
        Assert.That(beats[0].Raw.Replace("\r", ""), Is.EqualTo("First para.\n\nSecond para."));
        Assert.That(beats[1].Raw.Trim(), Is.EqualTo("Third."));
    }
}
