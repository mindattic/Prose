using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --review-report</c> — (re)generate the portable per-voter report (JSON +
/// filterable HTM) from a node's MOST RECENT stored review batch, without
/// re-running the panel. Auto-export already fires on every fresh review run; this
/// is for rebuilding the artifact from history (or after a viewer-template change).
///
/// Args (one of --id / --slug / --code required):
///   --slug &lt;slug&gt; | --id &lt;guid|prefix&gt; | --code &lt;CODE&gt;
///   --provider &lt;local|cloud|all&gt;  Restrict to one brain's ballots in the latest
///                                  batch (default: all). "local" = ProviderId 'local'.
/// </summary>
public static class ReviewReportCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null, code = null, provider = "all";
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":       if (i + 1 < args.Length) id = args[++i]; break;
                case "--slug":     if (i + 1 < args.Length) slug = args[++i]; break;
                case "--code":     if (i + 1 < args.Length) code = args[++i]; break;
                case "--provider": if (i + 1 < args.Length) provider = args[++i].ToLowerInvariant(); break;
            }
        }
        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(code))
        {
            Console.Error.WriteLine("[review-report] One of --id, --slug, or --code is required.");
            Console.Error.WriteLine("Usage: prose --review-report (--slug <slug> | --id <guid|prefix> | --code <CODE>) [--provider local|cloud|all]");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var exporter  = services.GetRequiredService<ReviewReportExporter>();

        await using var db = await dbFactory.CreateDbContextAsync();
        var q = db.Nodes.AsNoTracking();
        Node? node;
        if (!string.IsNullOrWhiteSpace(code)) node = await q.FirstOrDefaultAsync(s => s.NodeCode == code!.ToUpperInvariant());
        else if (!string.IsNullOrWhiteSpace(slug)) node = await q.FirstOrDefaultAsync(s => s.Slug == slug);
        else if (Guid.TryParse(id, out var g)) node = await q.FirstOrDefaultAsync(s => s.Id == g);
        else { var p = id!.ToLowerInvariant(); node = await q.FirstOrDefaultAsync(s => s.Id.ToString().StartsWith(p)); }
        if (node == null) { Console.Error.WriteLine("[review-report] Node not found."); return 1; }

        // Latest batch = reviews carrying the most-recent content fingerprint.
        var latestHash = await db.NodeReviews.Where(r => r.NodeId == node.Id)
            .OrderByDescending(r => r.ReviewedAt).Select(r => r.ContentHash).FirstOrDefaultAsync();
        if (string.IsNullOrEmpty(latestHash))
        {
            Console.Error.WriteLine($"[review-report] No reviews found for {node.Slug}. Run prose --review-node first.");
            return 1;
        }

        var query = db.NodeReviews.AsNoTracking()
            .Where(r => r.NodeId == node.Id && r.ContentHash == latestHash);
        if (provider == "local")      query = query.Where(r => r.ProviderId == "local");
        else if (provider == "cloud") query = query.Where(r => r.ProviderId != "local");
        var reviews = await query.Include(r => r.BeatScores).ToListAsync();

        if (reviews.Count == 0)
        {
            Console.Error.WriteLine($"[review-report] No '{provider}' ballots in the latest batch for {node.Slug}.");
            return 1;
        }

        // Recompute the run headline from the loaded ballots.
        var scores = reviews.Select(r => (double)r.Score).ToList();
        var mean = scores.Average();
        var sd = scores.Count > 1 ? Math.Sqrt(scores.Sum(x => (x - mean) * (x - mean)) / (scores.Count - 1)) : 0.0;
        var ci = scores.Count > 1 ? 1.96 * sd / Math.Sqrt(scores.Count) : 0.0;
        var flowMean = reviews.Where(r => r.FlowScore.HasValue).Select(r => (double)r.FlowScore!.Value).DefaultIfEmpty(0).Average();
        var clusters = reviews.Where(r => r.ClusterId.HasValue).Select(r => r.ClusterId!.Value).Distinct().Count();

        // Brain/model inferred from the ballots themselves.
        bool allLocal = reviews.All(r => r.ProviderId == "local");
        var brain = allLocal ? "local" : "cloud";
        var model = allLocal
            ? (reviews.Select(r => r.Model).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "local")
            : "trusted-4 panel";

        var (jsonPath, htmPath) = await exporter.ExportAsync(new ReviewReportExporter.ReportInput(
            node.Id, node.Slug, node.Title, latestHash, node.TotalBeatsToNarrate > 0 ? node.TotalBeatsToNarrate : reviews.Max(r => r.BeatCount),
            brain, model, Math.Round(mean, 1), Math.Round(sd, 1), Math.Round(ci, 2), flowMean, clusters, reviews));

        Console.WriteLine($"[review-report] {node.Title} — {reviews.Count} {brain} voters · {Math.Round(mean, 1)}/100 (SD {Math.Round(sd, 1)}, CI ±{Math.Round(ci, 2)})");
        Console.WriteLine($"[review-report] Report (open in browser): {htmPath}");
        Console.WriteLine($"[review-report] Report data (JSON):       {jsonPath}");
        return 0;
    }
}
