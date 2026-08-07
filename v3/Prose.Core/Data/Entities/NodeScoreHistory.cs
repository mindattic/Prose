namespace Prose.Core.Data.Entities;

public class NodeScoreHistory
{
    public int      Id          { get; set; }
    public Guid     NodeId    { get; set; }
    public DateTime RecordedAt  { get; set; } = DateTime.UtcNow;
    public string   ContentHash { get; set; } = "";
    public double   MeanScore   { get; set; }
    public double?  Sd          { get; set; }
    public int      ReviewCount { get; set; }
    public int      BeatCount   { get; set; }
    public Node?  Node      { get; set; }
}
