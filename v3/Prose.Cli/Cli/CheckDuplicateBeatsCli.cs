using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --check-duplicate-beats --slug &lt;nodeSlug&gt; [--threshold 0.90] [--json]
///
/// Corpus-wide near-duplicate-scene detector: flags beat pairs whose prose embeddings
/// are near-identical, excluding beats merely adjacent in the same chapter (a continuous
/// scene is supposed to share vocabulary). Candidate generator, not a verdict — verify by
/// reading both beats in full before disabling either.
///
/// Default threshold (0.90) is deliberately high-precision/low-recall: real-corpus
/// calibration against a known duplicate (an abandoned draft vs. its developed rewrite)
/// found it scored only 0.844 similarity, while lowering the floor to catch it also surfaced
/// 70+ candidates dominated by BCODA's own deliberate recurring formulaic devices (contract
/// postings, crew logbook entries) — real stylistic recurrence, not a bug. Pass a lower
/// --threshold (e.g. 0.80) for an occasional deliberate deep pass when you specifically
/// suspect this bug class; expect to filter more by hand.
///
/// Exit codes: 0 = no candidates, 1 = candidates found (review), 2 = embedding pass incomplete.
/// </summary>
public static class CheckDuplicateBeatsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        double threshold = BeatDuplicateService.DefaultThreshold;
        bool jsonMode = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
            else if (args[i] == "--threshold" && double.TryParse(args[i + 1], out var t)) { threshold = t; i++; }
        }

        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --check-duplicate-beats --slug <nodeSlug> [--threshold 0.90] [--json]");
            return 2;
        }

        var svc       = services.GetRequiredService<BeatDuplicateService>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug || s.NodeCode == slug);
        if (node == null)
        {
            Console.Error.WriteLine($"Node '{slug}' not found.");
            return 2;
        }

        if (!jsonMode)
            Console.WriteLine($"Scanning '{node.Title}' for near-duplicate beats (threshold {threshold:P0})…\n");

        var result = await svc.CheckNodeAsync(node.Id, threshold);
        var complete = result.BeatsEmbedded == result.BeatsScanned;

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_id = result.NodeId,
                slug = result.Slug,
                beats_scanned = result.BeatsScanned,
                beats_embedded = result.BeatsEmbedded,
                complete,
                candidates = result.Candidates.Select(c => new
                {
                    beat_a = c.NumberA, chapter_a = c.ChapterA,
                    beat_b = c.NumberB, chapter_b = c.ChapterB,
                    similarity = c.Similarity,
                }),
            }, new JsonSerializerOptions { WriteIndented = true }));
            return !complete ? 2 : result.Candidates.Count > 0 ? 1 : 0;
        }

        if (!complete)
            Console.WriteLine($"  ❓ INCOMPLETE — only {result.BeatsEmbedded}/{result.BeatsScanned} beats could be embedded. Re-run once resolved.\n");

        if (result.Candidates.Count == 0)
        {
            Console.WriteLine("  ✅ No near-duplicate candidates found.");
        }
        else
        {
            Console.WriteLine($"  {result.Candidates.Count} candidate pair(s) — READ BOTH BEFORE ACTING:\n");
            foreach (var c in result.Candidates)
            {
                Console.WriteLine($"  {c.Similarity:P0}  beat #{c.NumberA} (\"{c.ChapterA}\")  ↔  beat #{c.NumberB} (\"{c.ChapterB}\")");
            }
        }

        return !complete ? 2 : result.Candidates.Count > 0 ? 1 : 0;
    }
}
