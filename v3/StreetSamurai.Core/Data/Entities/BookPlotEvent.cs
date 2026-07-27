namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// One arc-level plot state transition recorded for a book node.
/// The BookStateLedgerService appends events here (auto-extracted from prose
/// or manually seeded) and queries the latest value per StateKey to build the
/// per-beat context block that prevents crisis-amnesia between beats.
///
/// State machine per StateKey:
///   Crisis          : Open → Escalated → Climaxed → Resolved → (Reopened)
///   DramaticQuestion: Open → Answered | Deferred
///   Objective       : Active → Achieved | Failed | Abandoned
///   Threat          : Active → Contained → Neutralized | Escalated
///   Alliance        : Active → Strained → Broken | Restored
///   Information     : Hidden → Revealed → Confirmed | Contested
/// </summary>
public class BookPlotEvent
{
    public Guid   Id      { get; set; }
    public Guid   NodeId  { get; set; }
    public Node?  Node    { get; set; }

    /// <summary>Beat where this event was auto-extracted. Null when seeded manually.</summary>
    public Guid?  BeatId     { get; set; }

    /// <summary>Zero-based index of the beat that produced this event. -1 when seeded manually.</summary>
    public int    BeatIndex  { get; set; } = -1;

    /// <summary>
    /// Dot-namespaced state identifier. Examples:
    ///   "crisis:behemoth_approach", "question:who_controls_it",
    ///   "objective:civilian_evacuation", "threat:arcsec_neutralize"
    /// </summary>
    public string StateKey   { get; set; } = "";

    /// <summary>Crisis | DramaticQuestion | Objective | Threat | Alliance | Information</summary>
    public string StateType  { get; set; } = "";

    /// <summary>
    /// Transition verb: open, escalate, climax, resolve, reopen, defer, answer,
    /// establish, achieve, fail, abandon, contain, neutralize, reveal, confirm, contest, shift.
    /// </summary>
    public string Verb       { get; set; } = "";

    /// <summary>Human-readable one-line description of this state transition (max 500 chars).</summary>
    public string Label      { get; set; } = "";

    /// <summary>
    /// Current state value after this event:
    ///   Open, Escalated, Climaxed, Resolved, Reopened,
    ///   Answered, Deferred,
    ///   Active, Achieved, Failed, Abandoned,
    ///   Contained, Neutralized,
    ///   Strained, Broken, Restored,
    ///   Hidden, Revealed, Confirmed, Contested
    /// </summary>
    public string NewValue   { get; set; } = "";

    /// <summary>"auto" = LLM-extracted from prose | "manual" = seeded by author or CLI.</summary>
    public string Source     { get; set; } = "auto";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
