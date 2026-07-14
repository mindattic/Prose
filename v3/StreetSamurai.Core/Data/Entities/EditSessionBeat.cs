namespace StreetSamurai.Core.Data.Entities;

public class EditSessionBeat
{
    public Guid     EditSessionId { get; set; }
    public Guid     BeatId        { get; set; }
    public DateTime EditedAt      { get; set; } = DateTime.UtcNow;
    /// <summary>Beat.Version before this prose edit.</summary>
    public int      PriorVersion  { get; set; }
    /// <summary>Beat.TextHash (SHA-256) before this prose edit.</summary>
    public string?  PriorTextHash { get; set; }

    public EditSession? Session { get; set; }
    public Beat?        Beat    { get; set; }
}
