namespace Prose.Core.Data.Entities;

/// <summary>
/// DB-backed chapter summary. Persists per-chapter factual summaries so later
/// beats can reference what happened earlier — surviving process restarts.
/// One row per (NodeId, ChapterIndex). For flat nodes, ChapterIndex
/// corresponds to IsChapterStart segment index; for book/chapter nodes,
/// ChapterIndex is the chapter child's ordinal (0-based) within the book.
/// </summary>
// PENDING DROP (author ruling 2026-09-22): no code reads/writes this; removed by the drop migration.
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

    /// <summary>Reader-Proxy QA comprehension probe cache (docs/READER-QA.md):
    /// { probeHash, probeModel, arbiterModel, probe:{summary,facts,confusions,prediction},
    ///   defects:[…], evaluatedAt }. Null = never probed. The hash gates re-billing —
    /// an unchanged chapter re-probes for free.</summary>
    public string?  ComprehensionJson { get; set; }

    public DateTime CreatedAt      { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt      { get; set; } = DateTime.UtcNow;
}

// NodeOpenThread lived here until 2026-09-15. It is replaced by NarrativeObligation (RFC 0013):
// same idea — a promise the prose made that the book still owes — with the verbatim quote, the
// due point, the entity and the author's decision that the old rows never carried. The
// AddNarrativeObligations migration moves every NodeOpenThreads row across as provenance
// "inferred" and drops the table.
