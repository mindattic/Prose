using NUnit.Framework;
using Prose.Core.Services.Discussion;

namespace Prose.UnitTests;

/// <summary>
/// Lifting the replacement prose out of an answer.
///
/// <para>What this gets wrong ends up as the proposal the author approves, so the failure mode is
/// not "no proposal" — it is a proposal containing the assistant's explanation of itself, or the
/// old passage, presented as the new prose with an Approve button under it.</para>
/// </summary>
[TestFixture]
public class FencedBlockTests
{
    [Test]
    public void Takes_the_contents_of_the_fence()
    {
        var body = FencedBlock.Last("""
            Cutting it is safe.

            ```
            The pallets were stacked two high.
            ```

            Changed: the second sentence.
            """);

        Assert.That(body, Is.EqualTo("The pallets were stacked two high."));
    }

    [Test]
    public void Takes_the_LAST_fence_when_the_answer_quotes_the_original_first()
    {
        // An answer that argues before it drafts shows what it is changing. Taking the first fence
        // would propose replacing the passage with itself.
        var body = FencedBlock.Last("""
            At the moment it reads:

            ```
            The pallets were empty.
            ```

            I would make it:

            ```
            The pallets were stacked two high.
            ```
            """);

        Assert.That(body, Is.EqualTo("The pallets were stacked two high."));
    }

    [Test]
    public void A_language_tag_on_the_fence_is_not_part_of_the_prose()
    {
        var body = FencedBlock.Last("```text\nHe drew Silence.\n```");

        Assert.That(body, Is.EqualTo("He drew Silence."));
    }

    [Test]
    public void Keeps_the_line_breaks_inside_the_fence()
    {
        // Paragraph breaks are part of what is being approved.
        var body = FencedBlock.Last("```\nHe drew Silence.\n\nThe dock was empty.\n```");

        Assert.That(body, Is.EqualTo("He drew Silence.\n\nThe dock was empty."));
    }

    [Test]
    public void An_answer_with_no_fence_offers_nothing()
    {
        Assert.That(FencedBlock.Last("I would not cut this — it plants the payoff in #412."),
                    Is.Null);
    }

    [Test]
    public void An_unclosed_fence_offers_nothing()
    {
        // A half-written answer. Proposing the rest of the reply as prose would be considerably
        // worse than proposing nothing at all.
        Assert.That(FencedBlock.Last("Here it is:\n```\nThe pallets were"), Is.Null);
    }

    [Test]
    public void An_empty_fence_offers_nothing()
    {
        Assert.That(FencedBlock.Last("```\n\n```"), Is.Null);
    }

    [Test]
    public void Handles_windows_line_endings_and_empty_input()
    {
        Assert.That(FencedBlock.Last("intro\r\n```\r\nnew prose\r\n```\r\n"), Is.EqualTo("new prose"));
        Assert.That(FencedBlock.Last(null), Is.Null);
        Assert.That(FencedBlock.Last(""), Is.Null);
    }
}
