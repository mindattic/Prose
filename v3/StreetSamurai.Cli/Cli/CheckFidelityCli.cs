using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --check-fidelity (--slug &lt;nodeSlug&gt; | --id &lt;nodeId&gt;) [--json]
///
/// Detects the Semantic Fidelity Gap for a node — beats that score high on the
/// Legion review metric but have drifted from the story's original meaning.
///
/// Two checks:
///   Bible alignment  — prose vs story Seed/Synopsis (north-star drift)
///   Intent alignment — prose vs beat Synopsis (purpose drift)
///
/// Violations are filed as SEMANTIC-DRIFT findings and printed to stdout.
/// Exit code 0 = no violations; 1 = violations found; 2 = usage/not-found error.
/// </summary>
public static class CheckFidelityCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? nodeSlug = null;
        Guid? nodeId = null;
        bool json = args.Contains("--json");

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--slug": nodeSlug = args[i + 1]; i++; break;
                case "--id":
                    if (Guid.TryParse(args[i + 1], out var g)) { nodeId = g; i++; }
                    break;
            }
        }

        if (nodeSlug == null && nodeId == null)
        {
            Console.Error.WriteLine("Usage: ss --check-fidelity (--slug <nodeSlug> | --id <nodeId>) [--json]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = dbFactory.CreateDbContext();

        if (nodeId == null)
        {
            var node = await db.Nodes.AsNoTracking()
                .Where(s => s.Slug == nodeSlug)
                .Select(s => new { s.Id })
                .FirstOrDefaultAsync();
            if (node == null)
            {
                Console.Error.WriteLine($"Node '{nodeSlug}' not found.");
                return 2;
            }
            nodeId = node.Id;
        }

        var fidelity = services.GetRequiredService<SemanticFidelityService>();

        if (!json)
            Console.WriteLine("Running semantic fidelity audit (embedding beats + querying alignment)…");

        var report = await fidelity.AuditNodeAsync(nodeId.Value);

        if (json)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                node_id            = report.NodeId,
                slug                 = report.Slug,
                node_score         = report.NodeScore,
                beats_checked        = report.BeatsChecked,
                beats_scored         = report.BeatsScored,
                mean_bible_alignment = Math.Round(report.MeanBibleAlignment, 4),
                mean_intent_alignment = report.MeanIntentAlignment.HasValue
                    ? Math.Round(report.MeanIntentAlignment.Value, 4) : (double?)null,
                violations_count     = report.Violations.Count,
                findings_emitted     = report.FindingsEmitted,
                violations           = report.Violations.Select(v => new
                {
                    beat_id         = v.BeatId,
                    beat_number     = v.BeatNumber,
                    beat_title      = v.BeatTitle,
                    score           = v.Score,
                    bible_alignment = Math.Round(v.BibleAlignment, 4),
                    intent_alignment = v.IntentAlignment.HasValue ? Math.Round(v.IntentAlignment.Value, 4) : (double?)null,
                    kind            = v.Kind,
                    message         = v.Message,
                    suggested_fix   = v.SuggestedFix,
                }),
            }));
            return report.Violations.Count > 0 ? 1 : 0;
        }

        // Human-readable output
        Console.WriteLine();
        Console.WriteLine($"Node : {report.Slug}");
        Console.WriteLine($"Score  : {report.NodeScore?.ToString("0.#") ?? "unscored"}%");
        Console.WriteLine($"Beats  : {report.BeatsChecked} checked, {report.BeatsScored} above score threshold ({SemanticFidelityService.ScoreGamingThreshold:0}%)");
        Console.WriteLine($"Bible alignment (mean) : {report.MeanBibleAlignment:P1}  (floor {SemanticFidelityService.BibleAlignmentFloor:P0})");
        if (report.MeanIntentAlignment.HasValue)
            Console.WriteLine($"Intent alignment (mean): {report.MeanIntentAlignment.Value:P1}  (floor {SemanticFidelityService.IntentAlignmentFloor:P0})");

        if (report.Violations.Count == 0)
        {
            Console.WriteLine("\n✔ No semantic fidelity violations found.");
            return 0;
        }

        Console.WriteLine($"\n{report.Violations.Count} SEMANTIC-DRIFT violation(s):");
        var byBeat = report.Violations.GroupBy(v => v.BeatNumber).OrderBy(g => g.Key);
        foreach (var grp in byBeat)
        {
            var first = grp.First();
            var label = first.BeatTitle != null ? $"Beat #{first.BeatNumber} — {first.BeatTitle}" : $"Beat #{first.BeatNumber}";
            Console.WriteLine($"\n  {label}  (score {first.Score:0.#}%)");
            foreach (var v in grp)
            {
                var kindLabel = v.Kind == "bible" ? "[BIBLE DRIFT]" : "[INTENT DRIFT]";
                var alignVal = v.Kind == "bible" ? $"{v.BibleAlignment:P0}" : $"{v.IntentAlignment:P0}";
                Console.WriteLine($"    {kindLabel} alignment {alignVal}");
                Console.WriteLine($"      {v.Message}");
                if (v.SuggestedFix != null)
                    Console.WriteLine($"      Fix: {v.SuggestedFix}");
            }
        }

        Console.WriteLine($"\n{report.FindingsEmitted} finding(s) filed — review at /findings.");
        return 1;
    }
}
