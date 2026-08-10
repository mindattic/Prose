using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --findings-staleness [--json]
///
/// RFC 0011 Brick 2 — the generic staleness report at the FindingsService layer, covering any
/// category that stamps <c>Findings.SourceRuleVersion</c> on write. Currently wired:
/// CraftChecklist (<see cref="BeatChecklistGateService.GetCurrentRuleSetVersionAsync"/>) and
/// StructuralFailure (<see cref="BeatVerificationService.CurrentRuleVersion"/>). A future check
/// category joins this report by doing the same two things Brick 1/2 already established for
/// these two: expose its own "what's current right now" value, and pass it into
/// <c>FindingsService.Upsert</c>'s <c>sourceRuleVersion</c> parameter — no new staleness query or
/// CLI flag required.
///
/// Distinct from <c>prose --verification-staleness</c> (which reports on the
/// <c>BeatVerifications</c> table directly, one level below Findings — useful for catching a
/// stale cache row before it even produces a stale Finding). This command reports on what's
/// actually visible in the Findings inbox right now.
/// </summary>
public static class FindingsStalenessCli
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var isJson = args.Contains("--json");

        var findings = services.GetRequiredService<FindingsService>();
        var checklist = services.GetRequiredService<BeatChecklistGateService>();

        var currentVersions = new Dictionary<string, string>
        {
            ["CraftChecklist"] = await checklist.GetCurrentRuleSetVersionAsync(),
            ["StructuralFailure"] = BeatVerificationService.CurrentRuleVersion,
        };

        var stale = await findings.GetStaleCategoriesAsync(currentVersions);

        if (isJson)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { currentVersions, stale }, JsonOpts));
            return stale.Count > 0 ? 1 : 0;
        }

        Console.WriteLine("[findings-staleness] current versions:");
        foreach (var (cat, ver) in currentVersions)
            Console.WriteLine($"  {cat,-20} {ver}");
        Console.WriteLine();

        if (stale.Count == 0)
        {
            Console.WriteLine("No stale findings — every wired category matches its current version.");
            return 0;
        }

        Console.WriteLine($"{stale.Count} book/category group(s) have stale findings:");
        foreach (var g in stale)
            Console.WriteLine($"  {g.StaleCount,4}/{g.TotalCount,-4} stale — {g.Category,-20} {g.FilePath}");
        Console.WriteLine();
        Console.WriteLine("Re-run: prose --audit-book --slug <slug> (StructuralFailure) or --craft-checklist --slug <slug> (CraftChecklist) for each.");
        return 1;
    }
}
