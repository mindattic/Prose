using NUnit.Framework;
using Prose.Core.Data.Entities;
using Prose.Core.Services.Discussion;

namespace Prose.UnitTests;

/// <summary>
/// A discussion, as markdown.
///
/// <para>A thread is the record of why a passage is the way it is, and that record is worth
/// nothing if it can only be read inside one panel of one window. What these tests guard is that
/// the transcript carries the EVIDENCE and not just the prose: a conversation exported without
/// its measured counts, its confirmation and its proposal reads as opinion, which is the opposite
/// of what the thread was for.</para>
/// </summary>
[TestFixture]
public class DiscussionExportTests
{
    private static DiscussionThread Thread() => new()
    {
        Id = Guid.NewGuid(),
        AnchorQuote = "The pallets were empty.",
        Title = "Why is the camphor here?",
        State = DiscussionThreadState.Live,
        CreatedAt = new DateTime(2026, 9, 20, 10, 0, 0, DateTimeKind.Utc),
    };

    private static DiscussionTurn Turn(string role, params DiscussionBlock[] blocks) => new()
    {
        Role = role,
        ContentJson = DiscussionContent.Serialize(blocks),
        At = new DateTime(2026, 9, 20, 10, 1, 0, DateTimeKind.Utc),
    };

    [Test]
    public void Carries_the_anchor_the_state_and_both_voices()
    {
        var md = DiscussionExport.ToMarkdown(
            Thread(),
            [
                Turn(DiscussionRole.Author, new DiscussionBlock.Text("Does this line earn its place?")),
                Turn(DiscussionRole.Assistant, new DiscussionBlock.Text("Yes — it is the only description of the dock.")),
            ],
            bookTitle: "Bushido Coda", beatLabel: "Beat #122");

        Assert.That(md, Does.Contain("Why is the camphor here?"));
        Assert.That(md, Does.Contain("The pallets were empty."));
        Assert.That(md, Does.Contain("Bushido Coda"));
        Assert.That(md, Does.Contain("Beat #122"));
        Assert.That(md, Does.Contain("### You"));
        Assert.That(md, Does.Contain("### Claude"));
        Assert.That(md, Does.Contain("live"), "a thread about a line that was CUT reads differently");
    }

    [Test]
    public void A_spoken_turn_says_so()
    {
        var turn = Turn(DiscussionRole.Author, new DiscussionBlock.Text("Cut the second sentence."));
        turn.InputMode = DiscussionInputMode.Voice;

        var md = DiscussionExport.ToMarkdown(Thread(), [turn]);

        Assert.That(md, Does.Contain("(spoken)"),
            "a transcription error reads exactly like a change of mind in a transcript too");
    }

    [Test]
    public void Carries_a_confirmation_and_its_steps()
    {
        var md = DiscussionExport.ToMarkdown(Thread(), [
            Turn(DiscussionRole.Assistant,
                 new DiscussionBlock.Confirm("You want the second sentence cut.",
                                             ["Remove it", "Rejoin the surrounding sentences"],
                                             "It is the only mention of the pallets.")),
        ]);

        Assert.That(md, Does.Contain("You want the second sentence cut."));
        Assert.That(md, Does.Contain("1. Remove it"));
        Assert.That(md, Does.Contain("2. Rejoin"));
        Assert.That(md, Does.Contain("only mention of the pallets"));
    }

    [Test]
    public void Carries_the_measured_evidence_as_a_real_table()
    {
        var md = DiscussionExport.ToMarkdown(Thread(), [
            Turn(DiscussionRole.Assistant, new DiscussionBlock.Table(
                ["Term", "Beats", "Places"],
                [["ozone", "9", "8"], ["camphor", "5", "1"]],
                "Where these words also appear")),
        ]);

        Assert.That(md, Does.Contain("| Term | Beats | Places |"));
        Assert.That(md, Does.Contain("| --- | --- | --- |"));
        Assert.That(md, Does.Contain("| ozone | 9 | 8 |"));
    }

    [Test]
    public void A_pipe_inside_a_cell_does_not_break_the_table()
    {
        // Escaped rather than stripped: a passage that genuinely contains one still reads right,
        // and an unescaped pipe silently splits the row into the wrong number of columns.
        var md = DiscussionExport.ToMarkdown(Thread(), [
            Turn(DiscussionRole.Assistant, new DiscussionBlock.Table(
                ["Quote"], [["he said | she said"]])),
        ]);

        Assert.That(md, Does.Contain(@"he said \| she said"));
    }

    [Test]
    public void A_short_row_is_padded_rather_than_throwing()
    {
        var md = DiscussionExport.ToMarkdown(Thread(), [
            Turn(DiscussionRole.Assistant, new DiscussionBlock.Table(
                ["A", "B", "C"], [["only one"]])),
        ]);

        Assert.That(md, Does.Contain("| only one |  |  |"));
    }

    [Test]
    public void A_chart_exports_as_its_numbers()
    {
        // Not as a picture: an image would have to be a sidecar file, and a transcript that
        // depends on one arrives broken.
        var md = DiscussionExport.ToMarkdown(Thread(), [
            Turn(DiscussionRole.Assistant, new DiscussionBlock.Chart(
                "Places per term", [new ChartBar("ozone", 8), new ChartBar("camphor", 1)])),
        ]);

        Assert.That(md, Does.Contain("Places per term"));
        Assert.That(md, Does.Contain("- ozone: 8"));
        Assert.That(md, Does.Contain("- camphor: 1"));
    }

    [Test]
    public void A_multi_line_quote_is_flattened_so_the_blockquote_stays_valid()
    {
        var thread = Thread();
        thread.AnchorQuote = "First line.\nSecond line.";

        var md = DiscussionExport.ToMarkdown(thread, []);

        Assert.That(md, Does.Contain("> First line. Second line."));
    }

    [Test]
    public void The_running_cost_is_reported_when_there_was_one()
    {
        var turn = Turn(DiscussionRole.Assistant, new DiscussionBlock.Text("An answer."));
        turn.Cost = 0.0123;

        var md = DiscussionExport.ToMarkdown(Thread(), [turn]);

        Assert.That(md, Does.Contain("Total cost:"));
    }

    [Test]
    public void An_empty_thread_exports_without_throwing()
    {
        Assert.That(DiscussionExport.ToMarkdown(Thread(), []), Is.Not.Empty);
    }
}
