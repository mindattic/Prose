using System.Text.Json.Serialization;

namespace Prose.Core.Models;

/// <summary>
/// Kinds of refinement notes produced by StoryRefinementService.
/// </summary>
public enum RefinementKind
{
    /// <summary>This moment is doing the most narrative work — consider expanding.</summary>
    Impactful,
    /// <summary>Too much happens too fast; sentences stack without breathing room.</summary>
    Cluttered,
    /// <summary>A promise or tension wasn't paid off on the page.</summary>
    Underdeveloped,
    /// <summary>Draft references a canon detail the reader hasn't been told.</summary>
    ContextGap,
    /// <summary>Sentence rhythm does not match the beat's emotional register.</summary>
    PacingMismatch,
}

/// <summary>
/// Disposition of a refinement note — set by the author when they review it.
/// Persisted so re-opening the editor shows prior accept/skip decisions.
/// </summary>
public enum RefinementNoteStatus
{
    Pending,
    Accepted,
    Skipped,
}

/// <summary>
/// A single refinement note keyed to a beat, with an exact quote and a suggestion.
/// The author — not the engine — decides whether to accept, edit, or skip.
/// </summary>
public class RefinementNote
{
    [JsonPropertyName("id")]          public string Id { get; set; } = Guid.CreateVersion7().ToString("N");
    [JsonPropertyName("beat_index")]  public int BeatIndex { get; set; }
    [JsonPropertyName("kind")]        public RefinementKind Kind { get; set; }
    [JsonPropertyName("quote")]       public string Quote { get; set; } = "";
    [JsonPropertyName("rationale")]   public string Rationale { get; set; } = "";
    [JsonPropertyName("suggestion")]  public string Suggestion { get; set; } = "";
    [JsonPropertyName("canon_fact")]  public string? CanonFact { get; set; }
    [JsonPropertyName("status")]      public RefinementNoteStatus Status { get; set; } = RefinementNoteStatus.Pending;
    [JsonPropertyName("applied_at")]  public DateTime? AppliedAt { get; set; }
}

/// <summary>
/// All refinement notes for one story, saved alongside checkpoint.json.
/// </summary>
public class RefinementReport
{
    [JsonPropertyName("project_id")]  public string ProjectId { get; set; } = "";
    [JsonPropertyName("title")]       public string Title { get; set; } = "";
    [JsonPropertyName("analyzed_at")] public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("beats_analyzed")] public int BeatsAnalyzed { get; set; }
    [JsonPropertyName("notes")]       public List<RefinementNote> Notes { get; set; } = [];
    [JsonPropertyName("error")]       public string? Error { get; set; }
}
