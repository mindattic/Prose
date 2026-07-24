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

    private record QuoteClaim(string BeatId, string Quote, string? ClaimedBy);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        bool isStory = args.Contains("--verify-story");
        bool isJson  = args.Contains("--json");
        bool isQuote = args.Contains("--verify-quote");
        bool isQuoteBatch = args.Contains("--verify-quotes-batch");

        var svc = services.GetRequiredService<BeatVerificationService>();

        // ── Quote-grounding mode (audit-claim verification) ──────────────────────
        // Logic-sweep audit agents report findings as "beat X contains quote Y." Before
        // trusting that for triage/fix, confirm the quote actually appears in beat X.
        if (isQuote)
        {
            string? qBeatIdStr = null, quote = null, claimedBy = null;
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--id") { qBeatIdStr = args[i + 1]; i++; }
                else if (args[i] == "--quote") { quote = args[i + 1]; i++; }
                else if (args[i] == "--claimed-by") { claimedBy = args[i + 1]; i++; }
            }

            if (!Guid.TryParse(qBeatIdStr, out var qBeatId) || string.IsNullOrEmpty(quote))
            {
                Console.Error.WriteLine("Usage: ss --verify-quote --id <beatId-guid> --quote \"<claimed text>\" [--claimed-by <name>] [--json]");
                return 2;
            }

            var qr = await svc.VerifyQuoteGroundingAsync(qBeatId, quote, claimedBy);

            if (isJson)
            {
                Console.WriteLine(JsonSerializer.Serialize(qr, JsonOpts));
                return qr.Result == "Fail" ? 1 : 0;
            }

            var qIcon = qr.Result switch { "Pass" => "✓", "Fail" => "✗", _ => "-" };
            Console.WriteLine($"  {qIcon} [{qr.Severity}] QuoteGrounding: {qr.Result}");
            if (!string.IsNullOrEmpty(qr.Evidence))
                Console.WriteLine($"    {qr.Evidence}");
            return qr.Result == "Fail" ? 1 : 0;
        }

        // ── Batch quote-grounding mode: gate an entire audit report at once ──────
        // File format: JSON array of { "beatId": "<guid>", "quote": "<text>", "claimedBy": "<optional>" }
        if (isQuoteBatch)
        {
            string? filePath = null;
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "--json-file") { filePath = args[i + 1]; i++; }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Console.Error.WriteLine("Usage: ss --verify-quotes-batch --json-file <path> [--json]");
                return 2;
            }

            var claimsRaw = JsonSerializer.Deserialize<List<QuoteClaim>>(
                await File.ReadAllTextAsync(filePath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            var batchResults = new List<BeatVerificationResult>();
            foreach (var claim in claimsRaw)
            {
                if (!Guid.TryParse(claim.BeatId, out var cbid))
                {
                    Console.Error.WriteLine($"  Skipping claim with invalid beatId: {claim.BeatId}");
                    continue;
                }
                batchResults.Add(await svc.VerifyQuoteGroundingAsync(cbid, claim.Quote, claim.ClaimedBy));
            }

            var failed = batchResults.Where(r => r.Result == "Fail").ToList();

            if (isJson)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { total = batchResults.Count, failed = failed.Count, results = batchResults }, JsonOpts));
                return failed.Count > 0 ? 1 : 0;
            }

            Console.WriteLine($"Quote-grounding batch: {batchResults.Count} claims checked, {failed.Count} FAILED.");
            foreach (var f in failed)
                Console.WriteLine($"  ✗ Beat {f.BeatId}: {f.Evidence}");
            if (failed.Count == 0)
                Console.WriteLine("All claimed quotes confirmed grounded in their attributed beats.");

            return failed.Count > 0 ? 1 : 0;
        }

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
