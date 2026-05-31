namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// One reviewer's micro-score for one beat. In study mode every reviewer rates
/// every beat (1 = this beat hurt the story for me, 3 = fine, 5 = a highlight),
/// producing a reviewer x beat matrix. That matrix is what the clusterer uses to
/// DISCOVER audience segments, and what the aggregator uses to classify each beat
/// as a fix-for-everyone (consensus-weak) or a genre-vs-literary tradeoff
/// (contested). Cascade-deleted with the parent <see cref="StrandReview"/>.
/// </summary>
public class StrandReviewBeatScore
{
    public Guid ReviewId { get; set; }
    public StrandReview? Review { get; set; }

    /// <summary>1-based beat position in the strand's reading order at review time.</summary>
    public int BeatNumber { get; set; }

    /// <summary>1-5: 1 = hurt the story, 3 = fine, 5 = highlight.</summary>
    public int Score { get; set; }
}
