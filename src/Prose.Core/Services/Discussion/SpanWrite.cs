namespace Prose.Core.Services.Discussion;

/// <summary>Why a constrained write was refused. Every value is a thing that would otherwise have
/// silently changed prose the author did not ask about.</summary>
public enum SpanWriteRefusal
{
    None = 0,

    /// <summary>The quoted passage is no longer in the beat.</summary>
    PassageGone,

    /// <summary>It occurs more than once and nothing distinguishes them any more.</summary>
    Ambiguous,

    /// <summary>The span begins or ends part-way through an entity tag, so replacing it would
    /// leave half a tag behind.</summary>
    SplitsMarkup,

    /// <summary>The replacement's own markup is not well-formed. An unclosed tag does not fail
    /// loudly — it pairs with a later close tag and eats everything between them.</summary>
    MalformedReplacement,

    /// <summary>The replacement repeats text from outside the span: the model returned the whole
    /// beat instead of the passage, and applying it would duplicate a paragraph.</summary>
    SwallowsContext,

    /// <summary>The replacement is what is already there.</summary>
    NoChange,

    /// <summary>The write would have altered prose outside the span. Never expected to fire — it
    /// is the tripwire that makes "never expected" a fact rather than a belief.</summary>
    OutsideSpanChanged,
}

/// <param name="NewText">The whole beat as it should now be stored. Null when refused.</param>
/// <param name="Reason">Sentence the author reads. Always set, including on success.</param>
/// <param name="RemovedText">The stored text the edit took out. Carried because the post-write
/// check needs to know which entity links went with it, and by the time it runs the beat's own
/// mentions index has already been re-derived against the NEW text.</param>
public sealed record SpanWriteOutcome(
    bool Applied,
    string? NewText,
    string Reason,
    SpanWriteRefusal Refusal = SpanWriteRefusal.None,
    string? RemovedText = null)
{
    public static SpanWriteOutcome Refuse(SpanWriteRefusal why, string reason)
        => new(false, null, reason, why);
}

/// <summary>
/// Replacing one anchored passage of a beat, and nothing else.
///
/// <para><b>This is the entire safety story for a spoken edit.</b> Typed, the author sees a draft
/// on screen and can compare it to what they meant. Spoken, there is no draft — an instruction
/// crosses a microphone, a transcriber, a model and a confirmation, and what reaches the prose is
/// whatever survived. The one thing that can be guaranteed mechanically is the BLAST RADIUS: every
/// character outside the anchored span comes back byte-identical, or nothing is written.</para>
///
/// <para>Pure, and separate from anything that touches the database, so it can be attacked
/// directly. Every refusal below is a way a write could otherwise have changed prose nobody was
/// discussing.</para>
///
/// <para><b>It re-anchors against the text as it stands now</b>, not against the text the proposal
/// was made from. A proposal can sit for minutes while autosave, a CLI command or another session
/// writes to the same beat, and offsets captured earlier would by then point at different words.
/// </para>
/// </summary>
public static class SpanWrite
{
    /// <summary>
    /// How much overlap with the surrounding prose counts as the replacement having swallowed it.
    ///
    /// <para>Long enough that ordinary repetition — a character's name, "the dock", a repeated
    /// clause the author is deliberately echoing — does not trip it, short enough to catch a model
    /// that returned the whole beat.</para>
    /// </summary>
    private const int ContextOverlapChars = 40;

    /// <summary>
    /// Apply a replacement to the passage <paramref name="anchor"/> points at.
    /// </summary>
    /// <param name="storedText">The beat's text as stored, markup and all.</param>
    /// <param name="replacement">What the passage should become. Empty is a legitimate deletion.</param>
    public static SpanWriteOutcome Apply(string? storedText, TextAnchor anchor, string? replacement)
    {
        var stored = storedText ?? "";
        var incoming = replacement ?? "";

        var map = PlainTextMap.Build(stored);

        // 1. Where is it NOW. Not where it was when the proposal was made.
        var resolved = TextAnchoring.Resolve(map.Plain, anchor);
        if (resolved.Outcome == AnchorOutcome.Detached || !resolved.Found)
            return SpanWriteOutcome.Refuse(SpanWriteRefusal.PassageGone,
                "That passage is no longer in the beat — it has been changed or removed since this "
                + "was proposed. Nothing was written.");

        if (resolved.Outcome == AnchorOutcome.Ambiguous)
            return SpanWriteOutcome.Refuse(SpanWriteRefusal.Ambiguous,
                "That passage now occurs more than once in the beat and nothing distinguishes "
                + "them. Select it again so the edit lands where you mean it. Nothing was written.");

        var (start, end) = map.ToSourceRange(resolved.Start, resolved.End);
        var removed = stored[start..end];

        // 2. The replacement's own markup. An unclosed tag is the dangerous case: it does not fail,
        //    it pairs with the next close tag further down the beat and swallows everything between.
        var problems = BeatMarkup.Validate(incoming);
        if (problems.Count > 0)
            return SpanWriteOutcome.Refuse(SpanWriteRefusal.MalformedReplacement,
                $"The replacement's markup is malformed ({problems[0].Message}), which would break "
                + "the prose around it. Nothing was written.");

        if (string.Equals(removed, incoming, StringComparison.Ordinal))
            return SpanWriteOutcome.Refuse(SpanWriteRefusal.NoChange,
                "The replacement is identical to what is already there. Nothing was written.");

        var report = $"Replaced {removed.Length} characters with {incoming.Length}.";
        if (incoming.Length == 0) report = $"Removed {removed.Length} characters.";

        // 3. An edit landing inside an entity link takes the link with it.
        //
        //    Leaving the wrapper in place would produce <entity guid="…Kyle's guid">Pixel</entity>:
        //    a link that points at one character and reads as another. That is not self-correcting.
        //    The save path treats an incoming tag as the author's deliberate disambiguation, so it
        //    would PIN the wrong pairing rather than re-derive it away. Dropping the wrapper and
        //    keeping the words lets the name scan decide afresh, which is the behaviour every other
        //    write in the system already relies on.
        if (BeatMarkup.TagAround(stored, start, end) is { } tag)
        {
            var innerEnd = tag.InnerStart + tag.InnerLength;
            incoming = stored[tag.InnerStart..start] + incoming + stored[end..innerEnd];
            start = tag.Start;
            end = tag.Start + tag.Length;
            removed = stored[start..end];
            report += " The entity link around it was dropped, so it can be re-derived on save.";
        }

        // 4. Otherwise the span must be a whole piece of markup, not part of one. A range that
        //    starts inside <entity guid="…"> leaves half a tag behind, which survives every reader
        //    unchanged and is persisted into the prose as literal angle brackets.
        if (BeatMarkup.Validate(removed).Count > 0)
            return SpanWriteOutcome.Refuse(SpanWriteRefusal.SplitsMarkup,
                "That passage starts or ends part-way through an entity link, so replacing it "
                + "would leave the link broken. Select whole words, including the whole link.");

        // 5. Did the model hand back the whole beat instead of the passage? The byte comparison
        //    below cannot catch this — the surroundings WOULD be preserved, with the replacement
        //    duplicating them in the middle.
        if (SwallowsContext(map.Plain, resolved.Start, resolved.End, incoming))
            return SpanWriteOutcome.Refuse(SpanWriteRefusal.SwallowsContext,
                "The replacement repeats prose from outside the passage, which would duplicate it. "
                + "A replacement must be for the selected passage only. Nothing was written.");

        var updated = stored[..start] + incoming + stored[end..];

        // 6. The tripwire. True by construction above — which is exactly why it is worth asserting:
        //    the construction is one line that a later change could get wrong, and the cost of
        //    being wrong is prose the author never discussed, silently rewritten.
        if (!OutsideIsIdentical(stored, updated, start, end, incoming.Length))
            return SpanWriteOutcome.Refuse(SpanWriteRefusal.OutsideSpanChanged,
                "The edit would have changed text outside the passage. Nothing was written. "
                + "This is a bug — please report it.");

        return new SpanWriteOutcome(true, updated, report, RemovedText: removed);
    }

    /// <summary>
    /// Everything before the span and everything after it, byte for byte.
    /// </summary>
    private static bool OutsideIsIdentical(
        string before, string after, int start, int end, int replacementLength)
    {
        if (start > before.Length || start > after.Length) return false;
        if (!before.AsSpan(0, start).SequenceEqual(after.AsSpan(0, start))) return false;

        var tailLength = before.Length - end;
        var afterTailStart = start + replacementLength;
        if (afterTailStart + tailLength != after.Length) return false;

        return before.AsSpan(end, tailLength).SequenceEqual(after.AsSpan(afterTailStart, tailLength));
    }

    /// <summary>
    /// Whether the replacement has dragged the surrounding prose in with it.
    ///
    /// <para>Compares against the reader-visible text on both sides, because a model that returns
    /// the whole beat returns it as prose, not as markup. A long verbatim run on either side is
    /// the signature: the author echoing a phrase deliberately does not reproduce forty unbroken
    /// characters of the sentence next door.</para>
    /// </summary>
    private static bool SwallowsContext(string plain, int start, int end, string replacement)
    {
        var incoming = BeatDiscussTarget.PlainText(replacement);
        if (incoming.Length < ContextOverlapChars) return false;

        var beforeSpan = plain[..start];
        if (beforeSpan.Length >= ContextOverlapChars
            && incoming.Contains(beforeSpan[^ContextOverlapChars..], StringComparison.Ordinal))
            return true;

        var afterSpan = plain[end..];
        return afterSpan.Length >= ContextOverlapChars
               && incoming.Contains(afterSpan[..ContextOverlapChars], StringComparison.Ordinal);
    }
}
