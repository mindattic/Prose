namespace Prose.Core.Data.Entities;

/// <summary>
/// SQL Server-backed analogue of the legacy SQLite <c>findings.db</c>. Holds
/// every contradiction / cliché / anachronism / voice flag that
/// <see cref="Prose.Core.Services.ContinuousQualityService"/> emits on
/// chapter save, plus their triage state. Migrated from SQLite to the unified
/// Prose SQL Server database 2026-05-09 — last SQLite holdout retired.
/// </summary>
public class FindingRow
{
    public long Id { get; set; }

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Source path or <c>chapter:{guid}</c> pseudo-path for in-DB chapters.</summary>
    public string FilePath { get; set; } = "";

    /// <summary>Chapter id when the finding came from the OnChapterSaved hook; null for file-only scans.</summary>
    public string? ChapterId { get; set; }

    /// <summary>Stored as the enum name (Contradiction / Cliche / Anachronism / Voice / Other).</summary>
    public string Category { get; set; } = "";

    /// <summary>Stored as the enum name (Low / Medium / High).</summary>
    public string Severity { get; set; } = "";

    public string Summary { get; set; } = "";
    public string? Snippet { get; set; }
    public string? SuggestedFix { get; set; }

    /// <summary>Stored as the enum name (New / Triaged / Applied / Dismissed).</summary>
    public string Status { get; set; } = "New";

    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Stable hash of <c>(filePath, category, summary)</c> — deduplicates
    /// re-detections so the same finding doesn't accumulate across rescans.
    /// Unique index enforces one row per logical finding.
    /// </summary>
    public string DedupKey { get; set; } = "";
}
