using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --auto-correct-nightly [--universe &lt;slug&gt;] [--dry-run] [--json]
///
/// One-shot entry point for the nightly AutoCorrect pass — pure ML/deterministic, zero LLM calls.
/// This is what the Windows Task Scheduler task <c>ProseAutoCorrectNightly</c> invokes at 2:00 AM
/// Central every night (see scripts/register-autocorrect-task.ps1). Can also be run manually for
/// testing. See <see cref="AutoCorrectOrchestratorService"/> for the full pipeline and the exact
/// whitelist of what gets auto-fixed vs. flag-only.
///
/// --dry-run: runs detection and refreshes Findings exactly as a live run would, but skips every
/// mutating fix — recommended for the first several nights before trusting live writes.
/// </summary>
public static class AutoCorrectNightlyCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var universeSlug = Flag(args, "--universe");
        bool dryRun = args.Contains("--dry-run");
        bool jsonMode = args.Contains("--json");

        Guid? universeId = null;
        if (!string.IsNullOrWhiteSpace(universeSlug))
        {
            var canonDocs = services.GetRequiredService<CanonDocumentService>();
            universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
            if (universeId == null)
            {
                Console.Error.WriteLine($"[auto-correct-nightly] Unknown universe '{universeSlug}'.");
                return 2;
            }
        }

        var orchestrator = services.GetRequiredService<AutoCorrectOrchestratorService>();
        var report = await orchestrator.RunAsync(new AutoCorrectOptions(universeId, dryRun));

        if (jsonMode)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                run_id = report.RunId,
                dry_run = dryRun,
                started_at = report.StartedAt,
                finished_at = report.FinishedAt,
                universe_profiles_refreshed = report.UniverseProfilesRefreshed,
                books_processed = report.Books.Count,
                entities_merged = report.EntitiesMerged,
                consistency_fixes_applied = report.ConsistencyFixesApplied,
                continuity_resolutions = report.ContinuityResolutions,
                notes = report.Notes,
                books = report.Books.Select(b => new { b.NodeId, b.Slug, b.FindingsRefreshed, b.Notes }),
            }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"[auto-correct-nightly] Run {report.RunId}{(dryRun ? " (DRY RUN — no mutations applied)" : "")}");
        Console.WriteLine($"  {report.StartedAt:u} → {report.FinishedAt:u}");
        Console.WriteLine($"  Universe profiles refreshed: {report.UniverseProfilesRefreshed}");
        Console.WriteLine($"  Books scanned: {report.Books.Count}");
        Console.WriteLine($"  Entities merged: {report.EntitiesMerged}");
        Console.WriteLine($"  Consistency fixes applied: {report.ConsistencyFixesApplied}");
        Console.WriteLine($"  Continuity majority-resolutions: {report.ContinuityResolutions}");
        if (report.Notes.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Notes:");
            foreach (var n in report.Notes) Console.WriteLine($"  - {n}");
        }
        if (!dryRun && (report.EntitiesMerged + report.ConsistencyFixesApplied + report.ContinuityResolutions) > 0)
            Console.WriteLine($"\n  To undo this run: prose --auto-correct-undo --run-id {report.RunId}");

        return 0;
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
