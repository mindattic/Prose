using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prose.Core.Services.Discussion;

/// <summary>
/// One renderable element of a discussion turn.
///
/// <para>A turn is an ordered list of these rather than a string, because the conversation has to
/// be constructive — it asks questions with options, shows where a phrase appears elsewhere, and
/// offers a change to approve. Each block has its own component in the Writer.</para>
///
/// <para><b>Assistant output is data, never markup.</b> A block is rendered by a typed component,
/// and <see cref="Text"/> goes through Markdig with a restricted pipeline. Nothing the model
/// produces is ever handed to <c>MarkupString</c> — beat prose already round-trips HTML-escaped
/// through <c>ProseRenderer</c>, and a conversation about the prose must hold the same line.</para>
///
/// <para>Adding a block type is a new record here, one line of <c>[JsonDerivedType]</c>, and one
/// component. It is never a migration: the column is JSON, and old turns simply do not contain
/// the new type.</para>
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Text), "text")]
[JsonDerivedType(typeof(Quote), "quote")]
[JsonDerivedType(typeof(Choice), "choice")]
[JsonDerivedType(typeof(Proposal), "proposal")]
[JsonDerivedType(typeof(Confirm), "confirm")]
public abstract record DiscussionBlock
{
    /// <summary>Prose. Markdown, rendered through a restricted pipeline.</summary>
    public sealed record Text(string Markdown) : DiscussionBlock;

    /// <summary>
    /// A passage from the book, with where it came from. This is how "the same detail appears in
    /// eight other places" is shown rather than merely asserted — the author can click through to
    /// each one and judge it.
    /// </summary>
    public sealed record Quote(Guid BeatId, string Excerpt, string? Label = null) : DiscussionBlock;

    /// <summary>
    /// A question with options. The author's pick becomes the next author turn, so a decision is
    /// recorded in the same log as the reasoning that led to it.
    /// </summary>
    public sealed record Choice(
        string Question,
        IReadOnlyList<ChoiceOption> Options,
        bool MultiSelect = false) : DiscussionBlock;

    /// <summary>A change awaiting the author's approval. Carries only the id — the proposal itself
    /// lives in <c>ChangeProposals</c>, so a turn can never disagree with the ticket it refers to.</summary>
    public sealed record Proposal(Guid ProposalId) : DiscussionBlock;

    /// <summary>
    /// What the assistant heard, and what it would do about it — before it does any of it.
    ///
    /// <para><b>This is the error correction for voice, not a politeness.</b> Typed, the author
    /// sees their own mistake on screen. Spoken, a single misheard word becomes an edit to the
    /// wrong sentence, and nothing downstream can tell that from an instruction. Echoing the
    /// request back and waiting is what catches it, and it works whether or not transcription ever
    /// gets good.</para>
    ///
    /// <para><b>Questions never carry one.</b> Answering is free and reversible, and a loop that
    /// confirms every question trains the author to confirm blind — at which point the mechanism
    /// is worse than nothing because it still looks like it is working.</para>
    /// </summary>
    /// <param name="Restatement">One sentence, in the assistant's own words, of what it understood
    /// the author to be asking for. Deliberately not a quotation of the transcript: a restatement
    /// that repeats the mis-hearing verbatim would pass a reading the author only skims.</param>
    /// <param name="Steps">What it would do, in order. Numbered by the renderer.</param>
    /// <param name="Caution">What this change would put at risk, when the assistant went ahead
    /// but is not comfortable. A full objection is prose and carries no Confirm at all.</param>
    public sealed record Confirm(
        string Restatement,
        IReadOnlyList<string> Steps,
        string? Caution = null) : DiscussionBlock;
}

/// <param name="Label">Short, and the thing the author actually picks.</param>
/// <param name="Detail">What choosing it means or costs. Optional.</param>
public sealed record ChoiceOption(string Label, string? Detail = null);

/// <summary>Serialization for <see cref="DiscussionTurn.ContentJson"/>.</summary>
public static class DiscussionContent
{
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize(IReadOnlyList<DiscussionBlock> blocks)
        => JsonSerializer.Serialize(blocks, Options);

    /// <summary>
    /// Read a turn's blocks. Never throws: a turn written by a newer build — or a row hand-edited
    /// in the database — must not take down the panel that is trying to show the conversation.
    /// An unreadable turn degrades to a note saying so, which is visible and recoverable, unlike
    /// a circuit that dies mid-render.
    /// </summary>
    public static IReadOnlyList<DiscussionBlock> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<DiscussionBlock>>(json, Options) ?? [];
        }
        catch (JsonException)
        {
            return [new DiscussionBlock.Text("*(This turn could not be read.)*")];
        }
    }

    /// <summary>The one-line summary used for thread titles and list rows.</summary>
    public static string Summarize(IReadOnlyList<DiscussionBlock> blocks, int max = 120)
    {
        var first = blocks.OfType<DiscussionBlock.Text>().FirstOrDefault()?.Markdown
                    ?? blocks.OfType<DiscussionBlock.Choice>().FirstOrDefault()?.Question
                    // A turn that is only a confirmation still has to be summarizable, or a thread
                    // opened by a spoken request shows a blank row in the list.
                    ?? blocks.OfType<DiscussionBlock.Confirm>().FirstOrDefault()?.Restatement
                    ?? "";
        first = first.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return first.Length <= max ? first : first[..(max - 1)] + "…";
    }
}
