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

    /// <summary>
    /// RFC 0011 Brick 2 — the version string the WRITING service considered "current" for its own
    /// check logic at the moment this finding was filed (e.g. <c>BeatChecklistGateService</c>'s
    /// PromptVersion, <c>BeatVerificationService.CurrentRuleVersion</c>). Optional and
    /// caller-defined — <see cref="Prose.Core.Services.FindingsService"/> doesn't know what a
    /// "rule" means for any given category, it only stores what the caller says. Null for
    /// categories that don't (yet) track a version, and for every finding filed before this
    /// column existed. Lets <c>FindingsService.GetStaleCategoriesAsync</c> answer "which
    /// book/category combinations were filed under an older version than the one currently
    /// declared" as a single generic query, instead of a bespoke staleness implementation per
    /// service — the same class of manual-timestamp-diffing gap that bit
    /// <c>BeatVerificationService</c> twice in one session before it got its own dedicated
    /// (table-scoped, not Findings-scoped) RuleVersion column.
    /// </summary>
    public string? SourceRuleVersion { get; set; }
}
