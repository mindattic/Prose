using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;
using Prose.Core.Services.Calibration;

namespace Prose.Cli;

/// <summary>
/// The obligation instruments' validation harness (RFC 0013 D7). gutenberg universe only.
///
///   prose --inject-calibration-defects --slug &lt;GCSH|GCTOC|GCNEG&gt; --count N [--seed S]
///   prose --calibrate-obligations      --slug &lt;…&gt; [--deep] [--json]
///   prose --revert-calibration-defects --slug &lt;…&gt;
///
/// Acceptance before the instrument may run on an author's book: precision ≥ 0.85, recall ≥ 0.70,
/// F1 ≥ 0.678 (the Lost-in-Stories bar), ≤ 1.0 MODERATE+ control finding per 10k words, and no
/// more than 10% of "resolved" injections mis-flagged as abandoned.
/// </summary>
public static class ObligationCalibrationCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? Flag(string name) { for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1]; return null; }
        var slug = Flag("--slug");
        if (string.IsNullOrWhiteSpace(slug)) { Console.Error.WriteLine("Usage: prose --inject-calibration-defects|--calibrate-obligations|--revert-calibration-defects --slug <gutenberg book> …"); return 2; }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var svc = services.GetRequiredService<ObligationCalibrationService>();
        Guid root;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var resolved = await NodeRefResolver.ResolveAsync(db, slug);
            if (resolved == null) { Console.Error.WriteLine($"[calibration] {NodeRefResolver.NotFoundMessage(slug)}"); return 1; }
            root = resolved.Value;
        }

        try
        {
            if (args.Contains("--inject-calibration-defects"))
            {
                var count = int.TryParse(Flag("--count"), out var c) ? c : 8;
                var seed = int.TryParse(Flag("--seed"), out var s) ? s : 20260915;
                var r = await svc.InjectAsync(root, count, seed);
                Console.WriteLine($"[calibration] injected {r.Injections.Count} defect(s) (seed {r.Seed}) into {slug}:");
                foreach (var i in r.Injections) Console.WriteLine($"  {i.Kind,-9} beat {i.BeatId:N}{(i.PayoffBeatId is Guid p ? $" → payoff beat {p:N}" : "")}  \"{i.Sentence}\"");
                Console.WriteLine("  Next: prose --calibrate-obligations --slug " + slug + " [--deep]");
                return 0;
            }
            if (args.Contains("--revert-calibration-defects"))
            {
                var n = await svc.RevertAsync(root);
                Console.WriteLine($"[calibration] reverted {n} injection(s) in {slug}; beat text restored byte-exact.");
                return 0;
            }
            if (args.Contains("--calibrate-obligations"))
            {
                var score = await svc.ScoreAsync(root, args.Contains("--deep"));
                if (args.Contains("--json")) { Console.WriteLine(JsonSerializer.Serialize(new { score, meets_bar = score.MeetsBar() }, new JsonSerializerOptions { WriteIndented = true })); return score.MeetsBar() ? 0 : 1; }
                Console.WriteLine($"[calibration] {slug} — {score.Injected} injection(s): {score.Abandoned} abandoned, {score.Resolved} resolved · {score.WordCount:N0} words · scorer {score.ScorerVersion}");
                Console.WriteLine($"  TP {score.TruePositives}  FN {score.FalseNegatives}  resolved mis-flagged {score.ResolvedMisflagged}  control MODERATE+ findings {score.ControlFindingsModeratePlus} ({score.ControlFalsePositivesPer10k:F2}/10k words)");
                Console.WriteLine($"  precision {score.Precision:F3}  recall {score.Recall:F3}  F1 {score.F1:F3}");
                Console.WriteLine($"  beats READ by the extractor: {score.BeatsRead}/{score.BeatsTotal}");
                Console.WriteLine($"  control split: {score.ControlStillOpen} \"not paid yet\" (overdue_open/open_at_end) + {score.ControlStructural} structural (broken ledger)");
                if (score.ControlByRule.Count > 0)
                    Console.WriteLine("    by rule: " + string.Join(", ", score.ControlByRule.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value}")));
                // The bar is never a bare number: say which branch it took and why (RFC 0013 §6a).
                Console.WriteLine($"  bar basis: {score.BarBasis}");
                Console.WriteLine($"    measured against {score.ControlAgainstBar} control finding(s) = {score.ControlFalsePositivesPer10k:F2}/10k words");
                foreach (var d in score.Details) Console.WriteLine("  " + d);
                if (score.CouldNotLook)
                    Console.WriteLine($"  RESULT: VOID — COULD NOT LOOK. The extractor never read {score.BeatsTotal - score.BeatsRead} of {score.BeatsTotal} beats, so every number above describes a partial book. This is not a pass and not a fail: fix the unread beats and re-run before tuning anything.");
                else
                    Console.WriteLine(score.MeetsBar()
                        ? "  RESULT: MEETS BAR (P≥0.85, R≥0.70, F1≥0.678, control ≤1.0/10k, resolved mis-flag ≤10%) — the instrument may run on author books."
                        : "  RESULT: BELOW BAR — do not run on author books; tune the extractor and re-run.");
                return score.MeetsBar() ? 0 : 1;
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"[calibration] REFUSED: {ex.Message}");
            return 1;
        }
        return 2;
    }
}
