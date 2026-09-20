using NUnit.Framework;
using Prose.Core.Services.Discussion;

namespace Prose.UnitTests;

/// <summary>
/// Splitting an assistant reply into the prose the author reads and the confirmation they act on.
///
/// <para><b>Why this is the piece worth testing.</b> The confirmation block is the only error
/// correction a spoken request has: typed, the author sees their own mistake; spoken, a single
/// misheard word becomes an edit to the wrong sentence and nothing downstream can tell that from
/// an instruction. Everything about whether a confirmation appears at all — and with what in it —
/// comes down to this parse.</para>
///
/// <para>Both directions of failure are covered, and they are not symmetrical. A missed trailer
/// costs a round trip. A trailer parsed into a <i>half-empty</i> confirmation is much worse: it
/// puts a Confirm button under a blank or truncated restatement, and the author will press it.</para>
/// </summary>
[TestFixture]
public class DiscussionTrailerTests
{
    [Test]
    public void A_plain_answer_carries_no_confirmation()
    {
        const string reply = "The camphor is doing real work here — it belongs to this one room.";

        var (prose, confirm) = DiscussionChatService.SplitTrailer(reply);

        Assert.That(confirm, Is.Null, "a question must never ask the author to confirm anything");
        Assert.That(prose, Is.EqualTo(reply));
    }

    [Test]
    public void A_request_comes_back_as_prose_plus_a_confirmation()
    {
        const string reply = """
            Cutting it is safe — nothing later depends on the second sentence.

            ---REQUEST---
            RESTATEMENT: You want the second sentence of the selection cut.
            STEPS:
            1. Remove the sentence beginning "The pallets were empty."
            2. Rejoin the surrounding sentences
            """;

        var (prose, confirm) = DiscussionChatService.SplitTrailer(reply);

        Assert.That(prose, Is.EqualTo("Cutting it is safe — nothing later depends on the second sentence."));
        Assert.That(confirm, Is.Not.Null);
        Assert.That(confirm!.Restatement, Is.EqualTo("You want the second sentence of the selection cut."));
        Assert.That(confirm.Steps, Has.Count.EqualTo(2));
        Assert.That(confirm.Steps[1], Is.EqualTo("Rejoin the surrounding sentences"));
        Assert.That(confirm.Caution, Is.Null);
    }

    [Test]
    public void The_marker_never_reaches_the_reader()
    {
        var (prose, _) = DiscussionChatService.SplitTrailer("""
            Fine to cut.

            ---REQUEST---
            RESTATEMENT: Cut the line.
            STEPS:
            1. Cut it
            """);

        Assert.That(prose, Does.Not.Contain("---REQUEST---"));
        Assert.That(prose, Does.Not.Contain("RESTATEMENT"));
    }

    [Test]
    public void A_push_back_is_prose_with_no_confirmation()
    {
        // The refusal path. There is nothing to confirm, because nothing was offered — and the
        // absence of a trailer is the whole signal.
        const string reply =
            "I would not cut this. It plants the ledger the machine pays off in #412, and that "
            + "beat reads as arbitrary without it.";

        var (_, confirm) = DiscussionChatService.SplitTrailer(reply);

        Assert.That(confirm, Is.Null);
    }

    [Test]
    public void Tolerates_bold_labels_and_bracketed_step_numbers()
    {
        var (_, confirm) = DiscussionChatService.SplitTrailer("""
            Safe.
            ---REQUEST---
            **RESTATEMENT:** Make her colder in the last line.
            **STEPS:**
            1) Cut the adverb
            2) Replace "said" with nothing
            """);

        Assert.That(confirm, Is.Not.Null);
        Assert.That(confirm!.Restatement, Is.EqualTo("Make her colder in the last line."));
        Assert.That(confirm.Steps, Is.EqualTo(new[] { "Cut the adverb", "Replace \"said\" with nothing" }));
    }

    [Test]
    public void Tolerates_dashed_steps_and_a_step_on_the_label_line()
    {
        var (_, confirm) = DiscussionChatService.SplitTrailer("""
            ---REQUEST---
            RESTATEMENT: Tighten it.
            STEPS: - Drop the first clause
            - Keep the image
            """);

        Assert.That(confirm!.Steps, Is.EqualTo(new[] { "Drop the first clause", "Keep the image" }));
    }

    [Test]
    public void A_caution_is_carried_but_is_not_required()
    {
        var (_, confirm) = DiscussionChatService.SplitTrailer("""
            ---REQUEST---
            RESTATEMENT: Cut the whole paragraph.
            STEPS:
            1. Delete it
            CAUTION: This is the only place the ledger is described.
            """);

        Assert.That(confirm!.Caution, Is.EqualTo("This is the only place the ledger is described."));
    }

    [Test]
    public void A_trailer_with_no_restatement_produces_no_confirmation_at_all()
    {
        // The dangerous case. A Confirm button over a blank restatement is worse than no button:
        // the author presses it, and nothing echoed back what they were agreeing to.
        var (prose, confirm) = DiscussionChatService.SplitTrailer("""
            Sure.
            ---REQUEST---
            STEPS:
            1. Do the thing
            """);

        Assert.That(confirm, Is.Null);
        Assert.That(prose, Does.Contain("Do the thing"),
            "an unparsed trailer is shown whole rather than silently swallowed");
    }

    [Test]
    public void An_empty_restatement_line_is_treated_as_missing()
    {
        var (_, confirm) = DiscussionChatService.SplitTrailer("""
            ---REQUEST---
            RESTATEMENT:
            STEPS:
            1. Do the thing
            """);

        Assert.That(confirm, Is.Null);
    }

    [Test]
    public void A_confirmation_with_no_steps_still_confirms()
    {
        // Some changes are one act and enumerating them is noise. The restatement is the part that
        // catches a mis-hearing; the steps are explanation.
        var (_, confirm) = DiscussionChatService.SplitTrailer("""
            ---REQUEST---
            RESTATEMENT: Delete the selected sentence.
            """);

        Assert.That(confirm, Is.Not.Null);
        Assert.That(confirm!.Steps, Is.Empty);
    }

    [Test]
    public void Null_and_empty_replies_do_not_throw()
    {
        Assert.That(DiscussionChatService.SplitTrailer(null).Confirm, Is.Null);
        Assert.That(DiscussionChatService.SplitTrailer("").Prose, Is.Empty);
    }
}
