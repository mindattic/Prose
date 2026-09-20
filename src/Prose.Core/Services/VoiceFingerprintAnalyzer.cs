using System.Text.RegularExpressions;

namespace Prose.Core.Services;

/// <summary>
/// Pure, DB-free vocabulary-fingerprint drift detector — extracted so the math is directly
/// unit-testable, matching this repo's established pattern (BookHealthService.ComputePlantDensity,
/// NightlyHealthService.OpensWithCapsHeaderBlock). Ports the algorithm from
/// <see cref="WritingQualityService"/>'s CheckVoiceCadence (Jaccard overlap between a chapter's
/// distinctive tokens and each protagonist's established vocabulary), which is real, working,
/// tested code — but stuck behind the legacy Books/Chapters model and the SS-A44 voting-gate
/// default, meaning it never actually runs for any book on the live Nodes/Beats pipeline. Rather
/// than migrate that whole service, this reimplements just the algorithm against plain string
/// inputs so <see cref="BookHealthService"/> can drive it directly from BeatEntityPresence data.
/// </summary>
internal static class VoiceFingerprintAnalyzer
{
    private static readonly HashSet<string> CommonStopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "this","that","with","from","have","were","been","they","them","their","what","when","then",
        "there","would","could","should","because","about","into","through","before","after","over",
        "under","than","each","other","some","much","many","just","like","said","says","made","make",
        "going","gone","took","take","gave","give","know","knew","think","came","come","want","wanted"
    };

    /// <summary>Tokens 4+ chars, lowercased, minus common stopwords — crude but functions as a
    /// per-passage vocabulary fingerprint, same threshold/shape as the WritingQualityService original.</summary>
    internal static HashSet<string> DistinctiveTokens(string text) =>
        Regex.Matches(text, @"\b[a-zA-Z]{4,}\b")
            .Select(m => m.Value.ToLowerInvariant())
            .Where(t => !CommonStopwords.Contains(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    internal readonly record struct DriftCheck(string TopMatchName, double TopMatchScore, double OwnScore, bool Drifted);

    /// <summary>Minimum absolute Jaccard-score gap the top match must beat the owner's own score
    /// by before this counts as drift rather than noise. Calibrated 2026-08-09 against a real
    /// book (Death Whispers in a Cat's Ear, per-beat granularity before the chapter-aggregation
    /// fix): of 220 raw "drift" hits, 76% had a gap &lt;=0.02 and 22 were exact ties — i.e. the vast
    /// majority of hits below this margin are indistinguishable from measurement noise, not signal.</summary>
    private const double MinDriftMargin = 0.03;

    /// <summary>Compares <paramref name="testTokens"/> against every entity's fingerprint and
    /// reports whether the top Jaccard match is someone OTHER than <paramref name="ownEntityId"/>
    /// by a real margin — not just numerically ahead, which near-zero Jaccard scores make trivial
    /// to trigger on pure noise (see <see cref="MinDriftMargin"/>). Returns null when there isn't
    /// enough signal to judge either side reliably (thin test passage or thin/missing fingerprint
    /// for the owner) rather than a false negative.</summary>
    internal static DriftCheck? CheckDrift(
        HashSet<string> testTokens,
        Guid ownEntityId,
        IReadOnlyDictionary<Guid, (string Name, HashSet<string> Fingerprint)> fingerprints)
    {
        if (testTokens.Count < 30) return null;
        if (!fingerprints.TryGetValue(ownEntityId, out var own) || own.Fingerprint.Count < 20) return null;

        var scores = fingerprints
            .Select(kv => (kv.Key, kv.Value.Name,
                Score: (double)testTokens.Intersect(kv.Value.Fingerprint).Count() /
                       Math.Max(1, testTokens.Union(kv.Value.Fingerprint).Count())))
            .OrderByDescending(x => x.Score)
            .ToList();

        var ownScore = scores.First(s => s.Key == ownEntityId).Score;
        var top = scores[0];
        var drifted = top.Key != ownEntityId && (top.Score - ownScore) >= MinDriftMargin;
        return new DriftCheck(top.Name, top.Score, ownScore, drifted);
    }
}
