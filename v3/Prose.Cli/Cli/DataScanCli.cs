using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --data-scan --tool &lt;name&gt; [--apply] [--overwrite] [--universe &lt;slug&gt;]
///
/// CLI surface for the six DataScanUtility subclasses (batch canon-entity maintenance:
/// FixPhiService, FixIdentityCorruptionService, TagWeaponLethalityService,
/// TagNormalizerService, AssignTiersService, CrossReferenceService) — all SQL-backed
/// (Records.Json), none had any CLI or MCP wrapper before this file.
///
/// Defaults to --dry-run (preview only, no writes) — these are mass-mutation tools with no
/// other confirmation step, so previewing what WOULD change is the only safe way to run one
/// against live canon data before committing. Pass --apply to actually write.
///
/// Added 2026-08-09, same "unreachable service" pattern as DataConsistencyService/
/// GraphHealthService fixed earlier this session — but these six also had NO dry-run capability
/// at all before this change (added to DataScanUtility.RunScanAsync + all 6 subclasses'
/// RunAsync signatures) since nothing had ever needed to preview them safely before.
/// </summary>
public static class DataScanCli
{
    static readonly string[] ToolNames =
        ["fix-phi", "fix-identity", "tag-lethality", "tag-normalize", "assign-tiers", "cross-reference"];

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var tool = Flag(args, "--tool");
        if (tool == null || !ToolNames.Contains(tool))
        {
            Console.Error.WriteLine($"[data-scan] --tool is required, one of: {string.Join(", ", ToolNames)}");
            return 2;
        }

        var apply     = args.Contains("--apply");
        var overwrite = args.Contains("--overwrite");
        var dryRun    = !apply;

        Console.WriteLine($"[data-scan] Running '{tool}' ({(dryRun ? "DRY RUN — no writes" : "APPLYING")})…");

        UtilityResult result = tool switch
        {
            "fix-phi"         => await services.GetRequiredService<FixPhiService>().RunAsync(dryRun: dryRun),
            "fix-identity"    => await services.GetRequiredService<FixIdentityCorruptionService>().RunAsync(dryRun: dryRun),
            "tag-lethality"   => await services.GetRequiredService<TagWeaponLethalityService>().RunAsync(overwrite: overwrite, dryRun: dryRun),
            "tag-normalize"   => await services.GetRequiredService<TagNormalizerService>().RunAsync(dryRun: dryRun),
            "assign-tiers"    => await services.GetRequiredService<AssignTiersService>().RunAsync(overwrite: overwrite, dryRun: dryRun),
            "cross-reference" => await services.GetRequiredService<CrossReferenceService>().RunAsync(dryRun: dryRun),
            _ => throw new InvalidOperationException("unreachable"),
        };

        Console.WriteLine($"Scanned: {result.FilesScanned}   {(dryRun ? "Would modify" : "Modified")}: {result.FilesModified}   {(dryRun ? "Would change" : "Changes")}: {result.ChangesApplied}");
        if (result.Warnings is { Count: > 0 })
        {
            Console.WriteLine($"Warnings ({result.Warnings.Count}):");
            foreach (var w in result.Warnings.Take(20))
                Console.WriteLine($"  - {w}");
            if (result.Warnings.Count > 20)
                Console.WriteLine($"  … and {result.Warnings.Count - 20} more");
        }

        if (dryRun && result.FilesModified > 0)
            Console.WriteLine($"\nThis was a dry run — re-run with --apply to write {result.FilesModified} change(s).");

        return 0;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
