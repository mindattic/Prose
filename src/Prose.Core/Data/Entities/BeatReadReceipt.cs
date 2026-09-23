namespace Prose.Core.Data.Entities;

/// <summary>
/// "Someone read this beat, exactly as it stood, in this place in the book."
///
/// <para><b>The only memory a read keeps</b> (author ruling 2026-09-22: the book is the book;
/// no outline, spine, ledger or reconciliation layer). It does not describe the story. It records
/// which text was read, so the engine can compute, never store, which beats are unread now.</para>
///
/// <para><b>Nothing here has to be kept in alignment.</b> Staleness is derived on demand by
/// <see cref="Services.ReadGateService"/> from the beat's current <see cref="Beat.TextHash"/>,
/// its current neighbours in reading order, and the <c>ModifiedAt</c> of the entities it mentions.
/// There is no flag for anyone to forget to set.</para>
///
/// <para>One row per beat, the latest read. A re-read overwrites it.</para>
/// </summary>
public class BeatReadReceipt
{
    public Guid BeatId { get; set; }

    /// <summary>The beat's <see cref="Beat.TextHash"/> when it was read.</summary>
    public string TextHash { get; set; } = "";

    /// <summary>The beat before and after it in the book's reading order when it was read. If
    /// either changes (a chapter moved, a beat was inserted or deleted beside it), the seam has not
    /// been read even though the words have not changed.</summary>
    public Guid? PrevBeatId { get; set; }
    public Guid? NextBeatId { get; set; }

    public DateTime ReadAt { get; set; } = DateTime.UtcNow;

    /// <summary>Who read it: "author", a session name, etc.</summary>
    public string ReadBy { get; set; } = "";
}

/// <summary>
/// Something a read found on one beat: a defect, an open question, a note. Tied to the beat's
/// text hash when it was written, so it goes stale the moment the beat changes. It records what a
/// reader noticed. It is not a statement of canon.
/// </summary>
public class BeatReadNote
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid BeatId { get; set; }
    public string TextHash { get; set; } = "";

    /// <summary>defect | question | note</summary>
    public string Kind { get; set; } = "note";

    public string Text { get; set; } = "";

    /// <summary>open | resolved. A question is answered or a defect fixed by resolving it.</summary>
    public string Status { get; set; } = "open";

    /// <summary>When resolved: the beat that answered or fixed it, if any.</summary>
    public Guid? ResolvedByBeatId { get; set; }

    public DateTime At { get; set; } = DateTime.UtcNow;
    public string ReadBy { get; set; } = "";
}
