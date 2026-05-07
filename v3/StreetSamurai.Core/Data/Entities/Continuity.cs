namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// EF row mirroring the legacy SQLite <c>claim_contradictions</c> edge.
/// Linked into the unified StreetSamurai database so contradictions can be
/// joined against entities and chapters.
/// </summary>
public class ClaimContradictionRow
{
    public string AUid { get; set; } = "";
    public string BUid { get; set; } = "";
    public string DetectedAt { get; set; } = "";
}

/// <summary>EF row mirroring the legacy SQLite <c>claim_confirmations</c> edge.</summary>
public class ClaimConfirmationRow
{
    public string ClaimUid { get; set; } = "";
    public string SourceChapterId { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public string ConfirmedAt { get; set; } = "";
}

/// <summary>EF row mirroring the legacy SQLite <c>extraction_runs</c> table.</summary>
public class ExtractionRunRow
{
    public int Id { get; set; }
    public string StartedAt { get; set; } = "";
    public string? CompletedAt { get; set; }
    public string ScopeType { get; set; } = "";
    public string? ScopeId { get; set; }
    public int NewClaims { get; set; }
    public int ConfirmedClaims { get; set; }
    public int ContradictedClaims { get; set; }
    public string? Error { get; set; }
}
