using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --verify-beat --id &lt;beatId&gt; [--json]
/// ss --verify-story --slug &lt;slug&gt; [--json]
///
/// Beat Verification Engine (Track C — Truth-First Architecture).
/// Checks whether generated prose fulfilled its declared BeatBlueprintDecision contract.
///
/// Mechanical checks (SQL/pattern):
///   BannedPattern      — internal_understanding, epilogue, false-reassurance close
///   EventType          — BeatModeLog.Mode vs declared EventType (approximate)
///   SubplotCarrier     — subplot entities present when SubplotCarrier=true
///   EscalationFloor    — EmotionalBeatScore.Depth vs declared floor (when scored)
///   EscalationMonotonic — story-wide curve regression (--verify-story only)
///
/// Semantic checks (embedding similarity):
///   DeclaredPurpose    — cosine similarity: declared purpose vs prose (requires embeddings)
///
/// Results are written to BeatVerification table (upsert — re-running refreshes).
/// Severity: BLOCKER = blocks export gate | MODERATE | MINOR
/// </summary>
public static class VerifyBeatCli
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        bool isStory = args.Contains("--verify-story");
        bool isJson  = args.Contains("--json");

        var svc = services.GetRequiredService<BeatVerificationService>();

        // ── Story mode ────────────────────────────────────────────────────────
        if (isStory)
        {
            string? slug = null;
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "--slug") { slug = args[i + 1]; i++; }

            if (string.IsNullOrEmpty(slug))
            {
                Console.Error.WriteLine("Usage: ss --verify-story --slug <slug>");
                return 2;
            }

            Console.WriteLine($"[verify-story] Running verification for: {slug}");
            var summary = await svc.VerifyStoryAsync(slug);

            if (isJson)
            {
                Console.WriteLine(JsonSerializer.Serialize(summary, JsonOpts));
                return summary.Blockers > 0 ? 1 : 0;
            }

            Console.WriteLine($"Beats checked:  {summary.BeatsChecked}");
            Console.WriteLine($"BLOCKER:        {summary.Blockers}");
            Console.WriteLine($"MODERATE:       {summary.Moderates}");
            Console.WriteLine($"MINOR:          {summary.Minors}");
            Console.WriteLine($"Pass:           {summary.Passed}");
            Console.WriteLine($"Skipped:        {summary.Skipped}");
            Console.WriteLine();

            if (summary.Findings.Count == 0)
            {
                Console.WriteLine("No failures.");
                return 0;
            }

            Console.WriteLine("Failures:");
            foreach (var f in summary.Findings.OrderByDescending(f => f.Severity == "BLOCKER" ? 2 : f.Severity == "MODERATE" ? 1 : 0))
            {
                Console.WriteLine($"  [{f.Severity,-8}] {f.CheckType,-22} Beat {f.BeatId}");
                if (!string.IsNullOrEmpty(f.Evidence))
                    Console.WriteLine($"            {f.Evidence}");
            }

            return summary.Blockers > 0 ? 1 : 0;
        }

        // ── Single beat mode ──────────────────────────────────────────────────
        string? beatIdStr = null;
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--id") { beatIdStr = args[i + 1]; i++; }

        if (!Guid.TryParse(beatIdStr, out var beatId))
        {
            Console.Error.WriteLine("Usage: ss --verify-beat --id <beatId-guid>");
            return 2;
        }

        Console.WriteLine($"[verify-beat] Running verification for beat: {beatId}");
        var results = await svc.VerifyBeatAsync(beatId);

        if (isJson)
        {
            Console.WriteLine(JsonSerializer.Serialize(results, JsonOpts));
            return results.Any(r => r.Result == "Fail" && r.Severity == "BLOCKER") ? 1 : 0;
        }

        foreach (var r in results)
        {
            var icon = r.Result switch { "Pass" => "✓", "Fail" => "✗", "Partial" => "~", _ => "-" };
            Console.WriteLine($"  {icon} [{r.Severity,-8}] {r.CheckType,-22} {r.Result}");
            if (!string.IsNullOrEmpty(r.Evidence))
                Console.WriteLine($"    {r.Evidence}");
        }

        return results.Any(r => r.Result == "Fail" && r.Severity == "BLOCKER") ? 1 : 0;
    }
}
