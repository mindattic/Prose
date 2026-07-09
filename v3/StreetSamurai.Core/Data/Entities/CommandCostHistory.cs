namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Append-only log of one CLI command execution: the pre-run estimate and the actual
/// cost charged to the TokenLedger. <see cref="Services.CommandCostEstimatorService"/>
/// queries the last 20 rows per command to self-calibrate future estimates.
/// </summary>
public class CommandCostHistory
{
    public int      Id            { get; set; }
    public string   CommandName   { get; set; } = "";   // e.g. "--write-story"
    public double   EstimatedCost { get; set; }
    public double   ActualCost    { get; set; }
    public double   AccuracyRatio { get; set; }         // ActualCost / EstimatedCost; 0 when estimated is zero
    public DateTime RunAt         { get; set; } = DateTime.UtcNow;
    public string   Provider      { get; set; } = "";   // e.g. "claude-api", "claude-team"
}
