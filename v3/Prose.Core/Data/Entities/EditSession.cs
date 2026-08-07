namespace Prose.Core.Data.Entities;

public class EditSession
{
    public Guid      EditSessionId { get; set; } = Guid.NewGuid();
    public Guid      NodeId        { get; set; }
    /// <summary>prose-pass | gripes-cleanup | logic-sweep | auto | custom</summary>
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
