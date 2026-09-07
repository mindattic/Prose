using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --gate-check --beat-id &lt;guid&gt; --file &lt;candidate.txt&gt; [--spine &lt;original.txt&gt;] [--json]
///
/// RFC 0012 §3.4 as a standalone report: run the writer's gate against a text that was NOT
/// produced by the writer — a hand-merged draft, an author's rewrite, a pasted candidate. Builds
/// the same <see cref="BeatBrief"/> the writer would build for this beat (goal, current event
/// summary, stop-before, canon names, POV), cleans the candidate, runs <see cref="DraftGate"/>
/// (free), then the one <see cref="BriefVerifier"/> call. Saves nothing.
///
/// <para><c>--spine</c>: the merge test. Every numeral and every multi-word capitalised name in the
/// original must survive in the candidate — a finished beat's value is the specifics later beats
/// lean on (the reading that drops to sixty-eight, the nine-year-old, "eight seconds left"). A
/// merge that "improves" a beat by losing them has rewritten the story.</para>
///
/// <para>Exit: 0 pass · 1 fail · 2 could not verify (verifier outage; free checks passed).</para>
/// </summary>
public static class GateCheckCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        Guid? beatId = null; string? file = null, spine = null; var json = args.Contains("--json");
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--beat-id" && i + 1 < args.Length && Guid.TryParse(args[i + 1], out var g)) beatId = g;
            if (args[i] == "--file" && i + 1 < args.Length) file = args[++i];
            if (args[i] == "--spine" && i + 1 < args.Length) spine = args[++i];
        }
        if (beatId == null || file == null || !File.Exists(file))
        {
            Console.Error.WriteLine("Usage: prose --gate-check --beat-id <guid> --file <candidate.txt> [--spine <original.txt>] [--json]");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var briefBuilder = services.GetRequiredService<BeatBriefBuilder>();
        var verifier = services.GetService<BriefVerifier>();
        var continuity = services.GetService<ContinuityService>();

        string? goal, subtext; int number;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var b = await db.Beats.AsNoTracking().IgnoreQueryFilters()
                .Where(x => x.Id == beatId.Value)
                .Select(x => new { x.Description, x.Title, x.Subtext, x.Number })
                .FirstOrDefaultAsync();
            if (b == null) { Console.Error.WriteLine($"[gate-check] Beat {beatId} not found."); return 1; }
            goal = string.IsNullOrWhiteSpace(b.Description) ? b.Title : b.Description;
            subtext = b.Subtext; number = b.Number;
        }
        if (string.IsNullOrWhiteSpace(goal))
        {
            Console.Error.WriteLine($"[gate-check] Beat #{number} has no Description or Title — no brief can be built. COULD NOT LOOK.");
            return 1;
        }

        var brief = await briefBuilder.BuildAsync(beatId.Value, goal, subtext, 0);
        var raw = await File.ReadAllTextAsync(file);
        var cleaned = DraftPostProcessor.Clean(raw);
        var det = DraftGate.Check(cleaned.Text, brief, briefBuilder.KnownNames());
        var failures = new List<string>(det.Failures);
        var notes = new List<string>();
        foreach (var r in cleaned.Removed) notes.Add("post-processor would remove: " + r);

        // Spine check: specifics of the original must survive the merge.
        if (spine != null && File.Exists(spine))
        {
            var orig = Regex.Replace(await File.ReadAllTextAsync(spine), @"<entity[^>]*>|</entity>", "");
            var cand = cleaned.Text;
            var numerals = Regex.Matches(orig, @"\b\d[\d,.:]*\b").Select(m => m.Value).Distinct().ToList();
            var numberWords = Regex.Matches(orig, @"\b(zero|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen|twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety|hundred|thousand)(-\w+)?\b", RegexOptions.IgnoreCase)
                .Select(m => m.Value.ToLowerInvariant()).Distinct().ToList();
            var names = Regex.Matches(orig, @"\b[A-Z][a-z]+(?:\s[A-Z][a-z]+)+\b").Select(m => m.Value).Distinct()
                .Where(n => !n.StartsWith("The ") && !n.StartsWith("He ") && !n.StartsWith("She ")).ToList();
            var lostNums = numerals.Where(n => !cand.Contains(n)).ToList();
            var lostWords = numberWords.Where(w => !Regex.IsMatch(cand, $@"\b{Regex.Escape(w)}\b", RegexOptions.IgnoreCase)).ToList();
            var lostNames = names.Where(n => !cand.Contains(n)).ToList();
            if (lostNums.Count > 0) failures.Add("SPINE: numbers in the original missing from the candidate: " + string.Join(", ", lostNums));
            if (lostWords.Count > 0) failures.Add("SPINE: number-words in the original missing from the candidate: " + string.Join(", ", lostWords));
            if (lostNames.Count > 0) failures.Add("SPINE: names in the original missing from the candidate: " + string.Join(", ", lostNames));
        }

        // Canon facts, same selection the writer shows the model.
        string? facts = null;
        if (continuity != null && brief.MustInclude.Count > 0)
        {
            var names = brief.MustInclude.Select(n => n.Trim()).Where(n => n.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var claims = continuity.GetByStatus("CANONICAL").Concat(continuity.GetByStatus("CONFIRMED"))
                .Where(c => names.Any(n => c.EntityName.StartsWith(n, StringComparison.OrdinalIgnoreCase) || n.StartsWith(c.EntityName, StringComparison.OrdinalIgnoreCase)))
                .Where(c => !ContinuityService.IsVolatilePredicate(c.Predicate))
                .Take(40).ToList();
            if (claims.Count > 0) facts = string.Join("\n", claims.Select(c => $"- {c.EntityName}: {c.Predicate} = {c.Object}"));
        }

        BriefVerifier.Verdict? verdict = null; string? verifierError = null;
        if (det.Passed && verifier != null)
        {
            try { verdict = await verifier.VerifyAsync(brief, facts, cleaned.Text, det.Warnings); failures.AddRange(verdict.AsConstraints()); }
            catch (Exception ex) when (ex is not OperationCanceledException) { verifierError = ex.Message; }
        }

        var passed = failures.Count == 0 && verifierError == null;
        var status = failures.Count > 0 ? "FAIL" : verifierError != null ? "UNVERIFIED" : "PASS";

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { beatId, number, status, brief, cleaned.Removed, det.Failures, det.Warnings, spineAndVerifierFailures = failures, verdict, verifierError, factsShown = facts },
                new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            Console.WriteLine($"GATE CHECK — beat #{number} {beatId} — {status}");
            Console.WriteLine();
            Console.WriteLine(brief.ToPromptBlock());
            Console.WriteLine();
            foreach (var n in notes) Console.WriteLine("  note: " + n);
            foreach (var w in det.Warnings) Console.WriteLine("  warn: " + w);
            if (verdict != null)
            {
                Console.WriteLine("  verifier: " + verdict.Reasoning);
            }
            if (verifierError != null) Console.WriteLine("  verifier: COULD NOT EVALUATE — " + verifierError);
            foreach (var f in failures) Console.WriteLine("  FAIL: " + f);
            if (passed) Console.WriteLine("  All checks passed. Nothing was saved — save with prose --edit-beat --id <guid> --file <file>.");
        }
        return failures.Count > 0 ? 1 : verifierError != null ? 2 : 0;
    }
}
