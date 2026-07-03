namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// DB-backed chapter summary. Persists per-chapter factual summaries so later
/// beats can reference what happened earlier — surviving process restarts.
/// One row per (NodeId, ChapterIndex). For flat nodes, ChapterIndex
/// corresponds to IsChapterStart segment index; for book/chapter nodes,
/// ChapterIndex is the chapter child's ordinal (0-based) within the book.
/// </summary>
public class NodeChapterSummary
{
    public Guid     Id             { get; set; }
    public Guid     NodeId       { get; set; }
    public Node?  Node         { get; set; }

    /// <summary>0-based chapter index within the parent node.</summary>
    public int      ChapterIndex   { get; set; }

    /// <summary>3-4 sentence prose summary of what happened in this chapter.</summary>
    public string   SummaryText    { get; set; } = "";

    /// <summary>Structured facts as JSON: { entities, locations, events, state_changes }.</summary>
    public string   FactsJson      { get; set; } = "{}";

    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt      { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// An open narrative thread — a setup, promise, or unresolved question introduced
/// in prose that the system auto-detected. IsResolved flips when a later beat
/// closes the loop. Injected into BeatContext so the generator knows what it
/// must eventually pay off.
/// </summary>
public class NodeOpenThread
{
    public Guid     Id              { get; set; }
    public Guid     NodeId        { get; set; }
    public Node?  Node          { get; set; }

    /// <summary>Beat where this thread was first detected. Null = seeded manually.</summary>
    public Guid?    OriginBeatId    { get; set; }

    /// <summary>Human-readable description of the open thread.</summary>
    public string   Description     { get; set; } = "";

    /// <summary>Category hint: "promise" | "plant" | "question" | "wound" | "foreshadow".</summary>
    public string   Category        { get; set; } = "promise";

    public bool     IsResolved      { get; set; }

    /// <summary>Beat where this thread was marked resolved. Null = still open.</summary>
    public Guid?    ResolvedBeatId  { get; set; }

    public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt       { get; set; } = DateTime.UtcNow;
}
