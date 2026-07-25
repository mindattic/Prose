namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// One reviewer's micro-score for one beat. In study mode every reviewer rates
/// every beat (1 = this beat hurt the story for me, 3 = fine, 5 = a highlight),
/// producing a reviewer x beat matrix. That matrix is what the clusterer uses to
/// DISCOVER audience segments, and what the aggregator uses to classify each beat
/// as a fix-for-everyone (consensus-weak) or a genre-vs-literary tradeoff
/// (contested). Cascade-deleted with the parent <see cref="NodeReview"/>.
/// </summary>
public class NodeReviewBeatScore
{
    public Guid ReviewId { get; set; }
    public NodeReview? Review { get; set; }

    /// <summary>1-based beat position in the node's reading order at review time.</summary>
    public int BeatNumber { get; set; }

    /// <summary>1-5: 1 = hurt the story, 3 = fine, 5 = highlight.</summary>
    public int Score { get; set; }

    /// <summary>SHA-256 hex of the beat's prose at review time (<see cref="Beat.TextHash"/>
    /// snapshotted during the run). Delta review uses this to skip re-scoring beats whose
    /// text hasn't changed since this row was written. Null on legacy rows.</summary>
    public string? BeatTextHash { get; set; }

    /// <summary>Short gripes about this beat (one per line). Append-only — never cleared.</summary>
    public string? Gripes { get; set; }

    /// <summary>Contradictions or continuity errors spotted in this beat (one per line).
    /// Append-only — never cleared.</summary>
    public string? Contradictions { get; set; }

    // ── Four-dimensional scoring (Swain doctrine — added SS-A47) ─────────────
    // New ballots populate all four. Legacy rows (pre-SS-A47) have nulls here;
    // Score above remains the canonical single value for backward compatibility.

    /// <summary>Beat intrinsic: does this beat execute its dramatic function?
    /// (Swain Scene: goal/conflict/disaster; Sequel: reaction/dilemma/decision.) 1-5.</summary>
    public int? ScoreBeat { get; set; }

    /// <summary>Chapter integration: does this beat advance the chapter's purpose
    /// and build momentum toward the chapter's climax? 1-5.</summary>
    public int? ScoreChapter { get; set; }

    /// <summary>Arc integration: does this beat serve the story arc — right escalation,
    /// plants and pays off at the correct moment? 1-5.</summary>
    public int? ScoreArc { get; set; }

    /// <summary>Story integration: does this beat contribute to the whole — theme,
    /// character arc, emotional journey, world-building? 1-5.</summary>
    public int? ScoreStory { get; set; }
}
