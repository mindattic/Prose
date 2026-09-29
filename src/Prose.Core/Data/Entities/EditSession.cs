namespace Prose.Core.Data.Entities;

/// <summary>
/// A named batch of beat edits against one book node (see <c>EditSessionService</c>); its
/// <see cref="SessionBeats"/> record which beats changed and their prior version/hash.
/// At most one session per node is open (<see cref="ClosedAt"/> null) at a time.
/// </summary>
public class EditSession
{
    public Guid      EditSessionId { get; set; } = Guid.NewGuid();
    public Guid      NodeId        { get; set; }
    /// <summary>Free-text name shown in session lists, e.g. <c>auto-2026-09-29</c> for an auto-created session.</summary>
    public string    Label         { get; set; } = "";
    /// <summary>prose-pass | gripes-cleanup | logic-sweep | auto | custom</summary>
    public string    SessionType   { get; set; } = "custom";
    public DateTime  StartedAt     { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt      { get; set; }
    public int       BeatCount     { get; set; }
    public string?   Notes         { get; set; }

    public Node? Node { get; set; }
    public List<EditSessionBeat> SessionBeats { get; set; } = new();
}
