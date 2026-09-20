using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services.Discussion;

/// <summary>
/// One thing the edit left disagreeing with the record.
/// </summary>
/// <param name="Kind">A short slug — <c>obligation</c>, <c>entity</c>, <c>summary</c>,
/// <c>discussion</c>. Grouped on, not parsed.</param>
/// <param name="Headline">What happened, in one line the author can act on.</param>
/// <param name="Detail">The evidence — the quote that vanished, the entity that is gone.</param>
/// <param name="Fork">The three ways out, in the author's words. Offered as a choice rather than
/// resolved, because which one is right is a judgement about the story.</param>
public sealed record Mismatch(
    string Kind,
    string Headline,
    string? Detail,
    IReadOnlyList<string> Fork);

/// <summary>
/// What an approved edit left inconsistent — checked exactly, and never guessed at.
///
/// <para><b>Every check here is free and exact.</b> Not one of them asks a model whether the prose
/// still means what it meant. That is deliberate: the obligation extractor measured a 96%
/// false-positive rate on prose engineered to owe nothing, and an automatic sweep that fires four
/// spurious findings per edit is a sweep the author turns off — after which the two real ones go
/// unreported too. So this reports only things that are true by arithmetic: a quote the record
/// cites and the prose no longer contains, an entity tag that has gone, a summary written against
/// text that has since changed.</para>
///
/// <para><b>It resolves nothing.</b> Each mismatch comes back with the same three-way fork — the
/// prose is right and the record is stale, the record is right and the prose is wrong, or it needs
/// thinking about — because which of those holds is a judgement about the story and RFC 0009 puts
/// that with the author. A request must not close with a mismatch unresolved, but "unresolved"
/// is answered by the author saying so, not by this deciding.</para>
///
/// <para>Runs AFTER the write, on the beat as it now stands. Running it before would report on a
/// version of the beat that never existed.</para>
/// </summary>
public sealed class PostWriteReviewService(IDbContextFactory<ProseDbContext> dbFactory)
{
    private static readonly string[] ProseFork =
    [
        "The prose is right — update the record.",
        "The record is right — put the prose back.",
        "Leave both for now, and remember why.",
    ];

    /// <summary>
    /// Check a just-written beat against everything that cites it.
    /// </summary>
    /// <param name="removedText">The reader-visible text the edit took out, used to say which
    /// words a broken citation lost. Optional.</param>
    public async Task<IReadOnlyList<Mismatch>> ReviewAsync(
        Guid bookNodeId, Guid beatId, string? removedText = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct);
        if (beat is null) return [];

        var plain = BeatDiscussTarget.PlainText(beat.Text);
        var found = new List<Mismatch>();

        await CheckObligationsAsync(db, beatId, plain, found, ct);
        await CheckEntityTagsAsync(db, beatId, beat.Text, removedText, found, ct);
        CheckSummaries(beat, found);
        await CheckOtherDiscussionsAsync(db, beatId, plain, found, ct);

        return found;
    }

    /// <summary>
    /// An obligation whose evidence the edit deleted.
    ///
    /// <para>The highest-value check in the set and the cheapest: an obligation records the exact
    /// quote that opened or closed it, so "is that quote still there" is a substring test. A
    /// closing quote that has gone means a promise the book no longer pays off, and nothing else
    /// in the system would notice.</para>
    /// </summary>
    private static async Task CheckObligationsAsync(
        ProseDbContext db, Guid beatId, string plain, List<Mismatch> found, CancellationToken ct)
    {
        var citing = await db.NarrativeObligations.AsNoTracking()
            .Where(o => o.OriginBeatId == beatId || o.ClosingBeatId == beatId)
            .ToListAsync(ct);

        foreach (var o in citing)
        {
            if (o.OriginBeatId == beatId && Missing(plain, o.OriginQuote))
                found.Add(new Mismatch(
                    "obligation",
                    $"The line that opened an obligation is gone: \"{Shorten(o.Description, 90)}\"",
                    $"It was opened on this quote, which is no longer in the beat: \"{Shorten(o.OriginQuote, 120)}\"",
                    ProseFork));

            if (o.ClosingBeatId == beatId && Missing(plain, o.ClosingQuote))
                found.Add(new Mismatch(
                    "obligation",
                    $"The line that PAID OFF an obligation is gone: \"{Shorten(o.Description, 90)}\"",
                    $"It was closed on this quote, which is no longer in the beat: "
                    + $"\"{Shorten(o.ClosingQuote, 120)}\". The promise is now unpaid.",
                    ProseFork));
        }
    }

    /// <summary>
    /// An entity the beat used to mention and no longer does.
    ///
    /// <para>Read from the indexed mentions rather than from the text, because the index is what
    /// every other reader of "who is in this beat" consults — presence, world state, the
    /// navigator. A character who has silently left a beat is a continuity problem that surfaces
    /// chapters later.</para>
    /// </summary>
    private static async Task CheckEntityTagsAsync(
        ProseDbContext db, Guid beatId, string? storedText, string? removedText,
        List<Mismatch> found, CancellationToken ct)
    {
        // The mentions row is re-derived on save, so by the time this runs it already reflects the
        // NEW text. What the edit removed has to come from the removed text itself.
        if (string.IsNullOrWhiteSpace(removedText)) return;

        var goneGuids = BeatMarkup.ExtractEntityGuids(removedText).ToList();
        if (goneGuids.Count == 0) return;

        var stillThere = BeatMarkup.ExtractEntityGuids(storedText).ToHashSet();
        var lost = goneGuids.Where(g => !stillThere.Contains(g)).ToList();
        if (lost.Count == 0) return;

        var names = await db.Entities.AsNoTracking()
            .Where(e => lost.Contains(e.Id))
            .Select(e => e.Name)
            .ToListAsync(ct);
        if (names.Count == 0) return;

        found.Add(new Mismatch(
            "entity",
            names.Count == 1
                ? $"{names[0]} is no longer mentioned in this beat."
                : $"{names.Count} entities are no longer mentioned in this beat.",
            string.Join(", ", names)
            + ". If they are still present in the scene, the beat no longer says so.",
            ProseFork));
    }

    /// <summary>
    /// A summary written against prose that has since changed.
    ///
    /// <para>Uses <c>Beat.DescriptionState</c> / <c>EventSummaryState</c>, which already compare
    /// the hash a summary was written against with the beat's current text hash. Reported rather
    /// than silently corrected: an event summary is a claim about what happens, and rewriting it
    /// from the new prose would make the intent check tautological.</para>
    /// </summary>
    private static void CheckSummaries(Beat beat, List<Mismatch> found)
    {
        if (!string.IsNullOrWhiteSpace(beat.Description) && beat.DescriptionState is "stale")
            found.Add(new Mismatch(
                "summary",
                "The beat's stated intent was written against different prose.",
                $"Intent: \"{Shorten(beat.Description, 140)}\"",
                ProseFork));

        if (!string.IsNullOrWhiteSpace(beat.EventSummary) && beat.EventSummaryState is "stale")
            found.Add(new Mismatch(
                "summary",
                "The beat's recorded event summary was written against different prose.",
                $"Recorded: \"{Shorten(beat.EventSummary, 140)}\"",
                ProseFork));
    }

    /// <summary>
    /// Another conversation the edit just detached.
    ///
    /// <para>A thread whose quoted passage is gone is kept rather than deleted — a discussion about
    /// a cut line is part of why it was cut — but it stops pointing at anything, and the author
    /// should hear that from the edit that did it rather than discover it later in a sidebar.</para>
    /// </summary>
    private static async Task CheckOtherDiscussionsAsync(
        ProseDbContext db, Guid beatId, string plain, List<Mismatch> found, CancellationToken ct)
    {
        var threads = await db.DiscussionThreads.AsNoTracking()
            .Where(t => t.BeatId == beatId && t.State != DiscussionThreadState.Resolved)
            .Select(t => new { t.Id, t.AnchorQuote, t.Title })
            .ToListAsync(ct);

        var detached = threads
            .Where(t => Missing(plain, t.AnchorQuote))
            .ToList();
        if (detached.Count == 0) return;

        found.Add(new Mismatch(
            "discussion",
            detached.Count == 1
                ? "Another discussion on this beat no longer points at anything."
                : $"{detached.Count} other discussions on this beat no longer point at anything.",
            string.Join(" · ", detached.Select(t => $"\"{Shorten(t.Title ?? t.AnchorQuote, 70)}\"")),
            ProseFork));
    }

    /// <summary>A cited quote that the prose no longer contains. An empty citation is not a
    /// mismatch — it is a record that never cited anything, which is a different problem.</summary>
    private static bool Missing(string plain, string? quote)
        => !string.IsNullOrWhiteSpace(quote)
           && !plain.Contains(quote.Trim(), StringComparison.Ordinal);

    private static string Shorten(string? s, int max)
    {
        s = (s ?? "").Replace('\n', ' ').Replace('\r', ' ').Trim();
        return s.Length <= max ? s : s[..(max - 1)] + "…";
    }
}
