namespace StreetSamurai.Core.Data.Entities;

public class StrandScoreHistory
{
    public int      Id          { get; set; }
    public Guid     StrandId    { get; set; }
    public DateTime RecordedAt  { get; set; } = DateTime.UtcNow;
    public string   ContentHash { get; set; } = "";
    public double   MeanScore   { get; set; }
    public double?  Sd          { get; set; }
    public int      ReviewCount { get; set; }
    public int      BeatCount   { get; set; }
    public Strand?  Strand      { get; set; }
}
