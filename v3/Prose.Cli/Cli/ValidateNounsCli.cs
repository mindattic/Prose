using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --validate-nouns --slug &lt;slug&gt;</c>
///
/// Scans every enabled beat in a node for deprecated / renamed noun references.
/// Any named thing whose old form is registered in DeprecatedEntityNames is
/// flagged with the beat number, the wrong name, the correct name, and a prose
/// snippet. Exits 1 if violations are found, 0 if clean.
///
/// Rules are registered via the <c>add_deprecated_name</c> MCP tool or by
/// inserting directly into the DeprecatedEntityNames table.
/// </summary>
public static class ValidateNounsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var svc = services.GetRequiredService<NounConsistencyService>();

        var slug = Flag(args, "--slug");
        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[validate-nouns] --slug <slug> is required.");
            return 1;
        }

        Console.WriteLine($"[validate-nouns] Scanning '{slug}'…");

        NounConsistencyReport report;
        try
        {
            report = await svc.ValidateSlugAsync(slug);
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"[validate-nouns] {ex.Message}");
            return 1;
        }

        if (report.IsClean)
        {
            Console.WriteLine($"[validate-nouns] ✓ Clean — {report.BeatCount} beats, no deprecated noun references.");
            return 0;
        }

        Console.WriteLine($"[validate-nouns] ✗ {report.Violations.Count} violation(s) across {report.BeatCount} beats:");
        foreach (var v in report.Violations)
        {
            Console.WriteLine($"  Beat #{v.BeatNumber}: \"{v.DeprecatedName}\" → use \"{v.CanonicalName}\"");
            Console.WriteLine($"    {v.Snippet}");
        }
        return 1;
    }

    static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
