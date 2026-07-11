namespace StreetSamurai.Core.Data.Entities;

/// <summary>
/// Per-beat prose quality metrics computed by <see cref="StreetSamurai.Core.Services.BeatProseMetricsService"/>.
/// CPU-only — no LLM calls. Upserted nightly by <c>ss --compute-metrics</c>.
/// Used by <c>ss --morning-report</c> and the Python <c>score_correlation.py</c> phase.
/// </summary>
public class BeatProseMetrics
{
    /// <summary>PK — matches Beat.Id. Cascade-deleted when the beat is deleted.</summary>
    public Guid     BeatId                { get; set; }
    /// <summary>Denorm FK for fast per-story queries (follows BeatNode.NodeId for the story's root node).</summary>
    public Guid     NodeId                { get; set; }

    public int      WordCount             { get; set; }
    public int      SentenceCount         { get; set; }
    public double   AvgWordsPerSentence   { get; set; }

    /// <summary>Type-Token Ratio: unique words / total words. Higher = more lexical variety.</summary>
    public double   TypeTokenRatio        { get; set; }

    /// <summary>
    /// Measure of Textual Lexical Diversity (MTLD). More robust than TTR for
    /// varying text lengths. Higher = more diverse vocabulary.
    /// </summary>
    public double   LexicalDiversityMtld  { get; set; }

    /// <summary>Flesch-Kincaid Grade Level. Lower = more accessible prose.</summary>
    public double   FleschKincaidGrade    { get; set; }

    /// <summary>Flesch Reading Ease (0-100). Higher = easier to read.</summary>
    public double   FleschReadingEase     { get; set; }

    public double   AvgSyllablesPerWord   { get; set; }

    /// <summary>Fraction of words that appear inside quotation marks (straight or curly).</summary>
    public double   DialogueProportion    { get; set; }

    public DateTime ComputedAt            { get; set; } = DateTime.UtcNow;
}
