using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --verify-quote --id &lt;beatId&gt; --quote "..." [--claimed-by &lt;name&gt;] [--json]
/// prose --verify-quotes-batch --json-file &lt;path&gt; [--json]
/// prose --verification-staleness [--json]
///
/// Mechanical quote grounding for audit claims (does the quoted text really appear in the beat it
/// is attributed to?), plus the BeatVerification rule-version staleness report. The blueprint-
/// contract checks behind --verify-beat / --verify-book were removed 2026-09-22 with the
/// structural blueprint (author ruling: the book is the beats, drawing on entities).
/// </summary>
public static class VerifyBeatCli
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private record QuoteClaim(string BeatId, string Quote, string? ClaimedBy);

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        bool isJson  = args.Contains("--json");
        bool isQuote = args.Contains("--verify-quote");
        bool isQuoteBatch = args.Contains("--verify-quotes-batch");
        bool isStaleness = args.Contains("--verification-staleness");

        var svc = services.GetRequiredService<BeatVerificationService>();

        // ── Staleness report: which books have BeatVerification rows computed under
        //    old check logic and need an --audit-book re-run ───────────
        if (isStaleness)
        {
            var stale = await svc.GetStaleBookSlugsAsync();

            if (isJson)
            {
                Console.WriteLine(JsonSerializer.Serialize(
                    new { currentRuleVersion = BeatVerificationService.CurrentRuleVersion, stale }, JsonOpts));
                return stale.Count > 0 ? 1 : 0;
            }

            Console.WriteLine($"[verification-staleness] current rule version: {BeatVerificationService.CurrentRuleVersion}");
            if (stale.Count == 0)
            {
                Console.WriteLine("No stale books — every BeatVerification row corpus-wide matches the current rule version.");
                return 0;
            }

            Console.WriteLine($"{stale.Count} book(s) have stale BeatVerification rows:");
            foreach (var b in stale)
                Console.WriteLine($"  {b.StaleRows,4}/{b.TotalRows,-4} stale — {b.Title} ({b.Slug})");
            Console.WriteLine();
            Console.WriteLine("Re-run: prose --audit-book --slug <slug> for each.");
            return 1;
        }

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
                Console.Error.WriteLine("Usage: prose --verify-quote --id <beatId-guid> --quote \"<claimed text>\" [--claimed-by <name>] [--json]");
                return 2;
            }

            var qr = await svc.VerifyQuoteGroundingAsync(qBeatId, quote, claimedBy);

            if (isJson)
            {
                Console.WriteLine(JsonSerializer.Serialize(qr, JsonOpts));
                return qr.Result != "Pass" ? 1 : 0;
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
                Console.Error.WriteLine("Usage: prose --verify-quotes-batch --json-file <path> [--json]");
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
                    // A claim that cannot be checked is not grounded: skipping it let a batch of
                    // "#5501"-style ids report "0 FAILED, all confirmed".
                    Console.Error.WriteLine($"  Invalid beatId (counted as a failure): {claim.BeatId}");
                    batchResults.Add(new BeatVerificationResult(Guid.Empty, "quote_grounding", "Fail", "BLOCKER", $"invalid beatId '{claim.BeatId}'"));
                    continue;
                }
                batchResults.Add(await svc.VerifyQuoteGroundingAsync(cbid, claim.Quote, claim.ClaimedBy));
            }

            var failed = batchResults.Where(r => r.Result != "Pass").ToList(); // "Skipped" (empty quote) is not grounded either

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

        Console.Error.WriteLine("Usage: prose --verify-quote --id <beatId> --quote \"<text>\" [--claimed-by <name>] [--json]");
        Console.Error.WriteLine("       prose --verify-quotes-batch --json-file <path> [--json]");
        Console.Error.WriteLine("       prose --verification-staleness [--json]");
        return 2;
    }
}
