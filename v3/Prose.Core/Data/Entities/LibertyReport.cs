namespace Prose.Core.Data.Entities;

/// <summary>
/// Per-beat liberty analysis record: every creative departure the LLM took relative
/// to the beat goal and entity roster, scored by the "Rule of Cool" (0–10 CoolFactor).
/// Written by <see cref="Prose.Core.Services.LibertyReportService"/> as a
/// post-write fire-and-forget task inside ProseWriterRouter.
/// </summary>
public class LibertyReport
{
    public int      Id             { get; set; }
    public Guid     BeatId         { get; set; }
    /// <summary>UTC timestamp when the report was generated.</summary>
    public DateTime GeneratedAt    { get; set; } = DateTime.UtcNow;
    /// <summary>JSON array of <see cref="LibertyItem"/> records.</summary>
    public string   LibertiesJson  { get; set; } = "[]";
    /// <summary>Highest CoolFactor across all liberties; -1 when no liberties found.</summary>
    public int      CoolFactorMax  { get; set; } = -1;
}

/// <summary>
/// One creative liberty identified in a beat by <see cref="Prose.Core.Services.LibertyReportService"/>.
/// Deserialized from <see cref="LibertyReport.LibertiesJson"/>.
/// </summary>
public record LibertyItem(
    string Kind,        // "entity_invention" | "tech_departure" | "creative_departure"
    string Name,        // entity name or short label
    string Evidence,    // ≤30-char prose snippet
    string Explanation, // why this is a departure
    int    CoolFactor); // 0–10: 10 = canon candidate, ≤4 = warning territory
