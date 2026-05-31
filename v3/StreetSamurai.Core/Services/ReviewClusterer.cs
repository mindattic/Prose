namespace StreetSamurai.Core.Services;

/// <summary>
/// Discovers audience segments from a reviewer x beat micro-score matrix by
/// k-means, choosing k by best mean silhouette over a small range. Pure and
/// deterministic (fixed seed) so a study reproduces. No external dependencies.
///
/// The matrix is z-scored per beat (column) before clustering so a beat with a
/// high baseline doesn't dominate the geometry — clusters form on RELATIVE
/// taste ("this group loves the anomaly, that group hates it"), not on overall
/// generosity, which is already captured by the per-reviewer overall score.
/// </summary>
public static class ReviewClusterer
{
    public sealed record Result(int K, int[] Assignments, double[][] Centroids, double Silhouette);

    /// <param name="matrix">reviewers (rows) x beats (cols), missing values
    /// already mean-imputed by the caller.</param>
    public static Result Cluster(double[][] matrix, int kMin = 2, int kMax = 4, int seed = 12345)
    {
        int n = matrix.Length;
        if (n == 0) return new Result(1, Array.Empty<int>(), Array.Empty<double[]>(), 0);
        int d = matrix[0].Length;

        // Z-score per column (beat). Zero-variance columns become all-zero.
        var z = new double[n][];
        for (int i = 0; i < n; i++) z[i] = new double[d];
        for (int j = 0; j < d; j++)
        {
            double mean = 0; for (int i = 0; i < n; i++) mean += matrix[i][j]; mean /= n;
            double var = 0; for (int i = 0; i < n; i++) { var t = matrix[i][j] - mean; var += t * t; } var /= n;
            double sd = Math.Sqrt(var);
            for (int i = 0; i < n; i++) z[i][j] = sd > 1e-9 ? (matrix[i][j] - mean) / sd : 0.0;
        }

        Result? best = null;
        int kCap = Math.Min(kMax, n);
        for (int k = Math.Max(2, kMin); k <= kCap; k++)
        {
            var (assign, cent) = KMeans(z, k, seed);
            // Skip degenerate solutions (an empty cluster).
            if (Enumerable.Range(0, k).Any(c => !assign.Contains(c))) continue;
            var sil = Silhouette(z, assign, k);
            if (best == null || sil > best.Silhouette)
                best = new Result(k, assign, cent, sil);
        }
        if (best == null)
        {
            // Fallback: everyone in one cluster.
            var one = new int[n];
            var c0 = ColumnMeans(z);
            best = new Result(1, one, new[] { c0 }, 0);
        }
        return best;
    }

    private static (int[] assign, double[][] centroids) KMeans(double[][] x, int k, int seed)
    {
        int n = x.Length, d = x[0].Length;
        var rng = new Random(seed + k); // deterministic, varies by k
        // k-means++ seeding.
        var centroids = new double[k][];
        centroids[0] = (double[])x[rng.Next(n)].Clone();
        var dist2 = new double[n];
        for (int c = 1; c < k; c++)
        {
            double sum = 0;
            for (int i = 0; i < n; i++) { dist2[i] = NearestDist2(x[i], centroids, c); sum += dist2[i]; }
            double r = rng.NextDouble() * sum, acc = 0; int pick = n - 1;
            for (int i = 0; i < n; i++) { acc += dist2[i]; if (acc >= r) { pick = i; break; } }
            centroids[c] = (double[])x[pick].Clone();
        }

        var assign = new int[n];
        for (int iter = 0; iter < 50; iter++)
        {
            bool changed = false;
            for (int i = 0; i < n; i++)
            {
                int bestC = 0; double bestD = double.MaxValue;
                for (int c = 0; c < k; c++) { var dd = Dist2(x[i], centroids[c]); if (dd < bestD) { bestD = dd; bestC = c; } }
                if (assign[i] != bestC) { assign[i] = bestC; changed = true; }
            }
            var sums = new double[k][]; var counts = new int[k];
            for (int c = 0; c < k; c++) sums[c] = new double[d];
            for (int i = 0; i < n; i++) { counts[assign[i]]++; var s = sums[assign[i]]; for (int j = 0; j < d; j++) s[j] += x[i][j]; }
            for (int c = 0; c < k; c++) if (counts[c] > 0) for (int j = 0; j < d; j++) centroids[c][j] = sums[c][j] / counts[c];
            if (!changed && iter > 0) break;
        }
        return (assign, centroids);
    }

    private static double Silhouette(double[][] x, int[] assign, int k)
    {
        int n = x.Length;
        if (k < 2) return 0;
        double total = 0;
        for (int i = 0; i < n; i++)
        {
            var intra = new double[k]; var cnt = new int[k];
            for (int j = 0; j < n; j++)
            {
                if (j == i) continue;
                var dd = Math.Sqrt(Dist2(x[i], x[j]));
                intra[assign[j]] += dd; cnt[assign[j]]++;
            }
            double a = cnt[assign[i]] > 0 ? intra[assign[i]] / cnt[assign[i]] : 0;
            double b = double.MaxValue;
            for (int c = 0; c < k; c++) { if (c == assign[i] || cnt[c] == 0) continue; b = Math.Min(b, intra[c] / cnt[c]); }
            if (b == double.MaxValue) continue;
            double s = (b - a) / Math.Max(a, b);
            if (!double.IsNaN(s)) total += s;
        }
        return total / n;
    }

    private static double Dist2(double[] a, double[] b)
    { double s = 0; for (int j = 0; j < a.Length; j++) { var t = a[j] - b[j]; s += t * t; } return s; }

    private static double NearestDist2(double[] p, double[][] cs, int count)
    { double m = double.MaxValue; for (int c = 0; c < count; c++) m = Math.Min(m, Dist2(p, cs[c])); return m; }

    private static double[] ColumnMeans(double[][] x)
    {
        int n = x.Length, d = x[0].Length; var m = new double[d];
        for (int i = 0; i < n; i++) for (int j = 0; j < d; j++) m[j] += x[i][j];
        for (int j = 0; j < d; j++) m[j] /= n; return m;
    }
}
