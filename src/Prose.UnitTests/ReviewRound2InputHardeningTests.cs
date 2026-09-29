using Prose.Core.Data.Entities;
using Prose.Core.Kdp;
using Prose.Core.Services;
using Prose.Core.Services.Factory;

namespace Prose.UnitTests;

/// <summary>
/// DB-free regressions for input that used to crash or mis-quote: a KDP run log with text before
/// its first stamp, a git "hash" that git would read as an option, and a survey key with a
/// single quote inside an inline onchange handler.
/// </summary>
[TestFixture]
public class ReviewRound2InputHardeningTests
{
    [Test]
    public void Run_log_preamble_takes_the_first_stamp_and_keeps_the_run_id()
    {
        var runId = Guid.Parse("019fd01e-bec0-796d-b1d9-a34f6a9c4df4");
        var text = "stray preamble\n"
                 + KdpRunLogFormat.Stamp(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero), KdpRunLogFormat.StartedMessage(runId, ["TLC"])) + "\n"
                 + KdpRunLogFormat.Stamp(new DateTimeOffset(2026, 9, 1, 12, 5, 0, TimeSpan.Zero), KdpRunLogFormat.FinishedMessage) + "\n";

        var parsed = KdpRunLogFormat.Parse(text);

        Assert.That(parsed.Lines, Has.Count.EqualTo(3));
        Assert.That(parsed.Lines[0].Message, Is.EqualTo("stray preamble"));
        Assert.That(parsed.Lines[0].At, Is.EqualTo(parsed.Lines[1].At), "preamble is dated at the first stamp, never MinValue");
        Assert.That(parsed.RunId, Is.EqualTo(runId), "the started line is found past the preamble");
        Assert.That(parsed.Codes, Is.EqualTo(new[] { "TLC" }));
        Assert.DoesNotThrow(() => Guid.CreateVersion7(parsed.Lines[0].At));
    }

    [Test]
    public void Run_log_with_no_stamp_is_dated_by_the_fallback()
    {
        var fallback = new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);
        var parsed = KdpRunLogFormat.Parse("just some text\n", fallback);

        Assert.That(parsed.Lines, Has.Count.EqualTo(1));
        Assert.That(parsed.Lines[0].At, Is.EqualTo(fallback));
        Assert.That(parsed.RunId, Is.Null);
        Assert.DoesNotThrow(() => Guid.CreateVersion7(KdpRunLogFormat.Parse("x").Lines[0].At));
    }

    [TestCase("fbefd8362", true)]
    [TestCase("FBEFD8362ABCDEF0123456789abcdef012345678", true)]
    [TestCase("abc123", false)]                                      // too short
    [TestCase("--output=/tmp/x", false)]                             // an option, not a hash
    [TestCase("-abcdef1", false)]
    [TestCase("HEAD", false)]
    [TestCase("fbefd8362 ", false)]
    [TestCase("fbefd8362abcdef0123456789abcdef0123456789", false)]   // 41 chars
    public void Commit_hash_must_be_7_to_40_hex(string hash, bool ok) =>
        Assert.That(GitProbe.IsCommitHash(hash), Is.EqualTo(ok));

    [Test]
    public void Survey_keys_with_quotes_stay_inside_the_js_string()
    {
        var survey = new Survey
        {
            Title = "T",
            Questions =
            [
                new SurveyQuestion
                {
                    QuestionKey = "Q-0'1",
                    Title = "Which?",
                    OptionsJson = """[{"key":"it's","label":"A"},{"key":"a\"b","label":"B"}]""",
                },
            ],
        };

        // GenerateHtml touches no database.
        var html = new SurveyService(null!).GenerateHtml(survey);

        Assert.That(html, Does.Contain("pick('0\\u00271','it\\u0027s',this)"));
        Assert.That(html, Does.Contain("pick('0\\u00271','a\\u0022b',this)"));
        Assert.That(html, Does.Not.Contain("pick('0'1'"));
    }
}
