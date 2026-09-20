namespace Prose.Core.Data.Entities;

/// <summary>
/// A nightly-refreshed statistical baseline for one universe, one metric — the "get to know the
/// universe" half of AutoCorrect (see <see cref="Prose.Core.Services.UniverseProfileService"/>).
/// Pure closed-form/vector-math statistics (mean, stdev, centroid vectors), computed by aggregating
/// data every detector already produces (BeatProseMetrics, ProseEmbeddings) — never a fresh LLM or
/// embedding-API call of its own. Persisted so a single book's z-score/cosine-distance checks can be
/// measured against the WHOLE universe's distribution instead of just that book's own beats, which
/// is what makes outlier/drift detection sharper over time as the corpus grows.
///
/// One row per (UniverseId, MetricKey) — re-upserted wholesale on every AutoCorrect run, not
/// versioned; the ledger doesn't track this table since it's a derived cache, not authored content
/// (nothing is ever lost by recomputing it).
/// </summary>
public class UniverseProfile
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid UniverseId { get; set; }

    /// <summary>"density-baseline" | "voice-centroid:{characterEntityId}" — see
    /// UniverseProfileService for the full key vocabulary.</summary>
    public string MetricKey { get; set; } = "";

    /// <summary>The computed baseline itself — mean/stdev pair for density metrics, a serialized
    /// centroid vector for voice-centroid keys.</summary>
    public string ValueJson { get; set; } = "";

    /// <summary>How many beats/entities fed this baseline — surfaced so a baseline built from a
    /// tiny sample (e.g. a character with 2 beats) can be treated as low-confidence.</summary>
    public int SampleSize { get; set; }

    public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
}
