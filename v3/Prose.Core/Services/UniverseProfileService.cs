using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

public sealed record DensityBaseline(double Mean, double StdDev, int SampleSize);

/// <summary>
/// Nightly-refreshed per-universe statistical baselines — the "get to know the universe" half of
/// AutoCorrect. v1 scope: closed-form density baselines (Flesch Reading Ease/Grade, Type-Token
/// Ratio, dialogue proportion, words-per-sentence) aggregated from the already-persisted
/// <see cref="Prose.Core.Data.Entities.BeatProseMetrics"/> table — pure mean/stdev over data every
/// nightly sweep already computes, zero new LLM or embedding-API calls.
///
/// Deliberately does NOT (yet) compute per-character voice-embedding centroids: SQL Server's
/// VECTOR type has no native AVG aggregate, so a true centroid requires pulling every character's
/// raw vectors into memory and averaging component-wise — real work, left as a follow-up rather
/// than shipped half-verified in the same pass as the auto-fix machinery (which does not depend on
/// this service at all; every AutoCorrect fix is deterministic SQL, not driven by these baselines).
/// These baselines instead sharpen the EXISTING per-book z-score checks in
/// <see cref="NightlyHealthService"/>/<see cref="EmbeddingHealthService"/> by giving them a
/// corpus-wide-per-universe comparison point instead of only the current book's own beats — purely
/// additive, those services' own per-book logic is unchanged.
/// </summary>
public class UniverseProfileService(IDbContextFactory<ProseDbContext> dbFactory)
{
    /// <summary>Below this many scored beats, a universe's baseline is too thin to trust —
    /// skip rather than persist a noisy mean/stdev from a handful of beats.</summary>
    private const int MinSampleSize = 5;

    public async Task<int> RefreshDensityBaselinesAsync(Guid universeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var metrics = await db.BeatProseMetrics.AsNoTracking()
            .Join(db.Nodes.AsNoTracking().IgnoreQueryFilters(), m => m.NodeId, n => n.Id, (m, n) => new { m, n.UniverseId })
            .Where(x => x.UniverseId == universeId)
            .Select(x => x.m)
            .ToListAsync(ct);

        if (metrics.Count < MinSampleSize) return 0;

        await UpsertAsync(db, universeId, "density-baseline:flesch-reading-ease", metrics.Select(m => m.FleschReadingEase), ct);
        await UpsertAsync(db, universeId, "density-baseline:flesch-kincaid-grade", metrics.Select(m => m.FleschKincaidGrade), ct);
        await UpsertAsync(db, universeId, "density-baseline:type-token-ratio", metrics.Select(m => m.TypeTokenRatio), ct);
        await UpsertAsync(db, universeId, "density-baseline:dialogue-proportion", metrics.Select(m => m.DialogueProportion), ct);
        await UpsertAsync(db, universeId, "density-baseline:avg-words-per-sentence", metrics.Select(m => m.AvgWordsPerSentence), ct);

        await db.SaveChangesAsync(ct);
        return metrics.Count;
    }

    public async Task<DensityBaseline?> GetBaselineAsync(Guid universeId, string metricKey, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.UniverseProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UniverseId == universeId && p.MetricKey == metricKey, ct);
        if (row == null) return null;
        return JsonSerializer.Deserialize<DensityBaseline>(row.ValueJson);
    }

    private static async Task UpsertAsync(ProseDbContext db, Guid universeId, string metricKey, IEnumerable<double> values, CancellationToken ct)
    {
        var list = values.ToList();
        var mean = list.Average();
        var stdDev = Math.Sqrt(list.Average(v => (v - mean) * (v - mean)));
        var baseline = new DensityBaseline(mean, stdDev, list.Count);
        var valueJson = JsonSerializer.Serialize(baseline);

        var existing = await db.UniverseProfiles.FirstOrDefaultAsync(p => p.UniverseId == universeId && p.MetricKey == metricKey, ct);
        if (existing == null)
        {
            db.UniverseProfiles.Add(new UniverseProfile
            {
                UniverseId = universeId, MetricKey = metricKey, ValueJson = valueJson,
                SampleSize = list.Count, ComputedAt = DateTime.UtcNow,
            });
        }
        else
        {
            existing.ValueJson = valueJson;
            existing.SampleSize = list.Count;
            existing.ComputedAt = DateTime.UtcNow;
        }
    }
}
