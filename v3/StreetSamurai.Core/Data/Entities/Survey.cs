namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// A canon-sync or contradiction-resolution survey. Stores questions, tracks
/// answers, and logs apply actions so the full decision trail is persisted in
/// the DB rather than only in markdown files.
///
/// Status lifecycle: Open → Completed.
/// Questions are answered via the MCP <c>answer_survey_question</c> tool, then
/// applied (SQL / MCP updates) and marked Applied/Skipped via
/// <c>mark_survey_question_applied</c>.
/// </summary>
public class Survey
{
    public Guid     Id          { get; set; }
    public Guid?    UniverseId  { get; set; }
    public string   Slug        { get; set; } = "";
    public string   Title       { get; set; } = "";
    public string?  Purpose     { get; set; }

    /// <summary>Open | Completed</summary>
    public string   Status      { get; set; } = "Open";

    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public List<SurveyQuestion> Questions { get; set; } = [];
}
