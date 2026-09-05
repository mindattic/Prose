using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --lint-prose --slug &lt;slug&gt; [--dry-run]
///
/// Deterministic prose linter (RepetitionLintService) — echo words, crutch phrases, pet words,
/// unattributed dialogue runs, airless-narration runs, floating-heads beats. Zero LLM cost.
/// Findings land in the Findings table (CraftChecklist, "LINT " prefix) and loop back into
/// future generation via ProseWriterRouter's findings-guidance mechanism.
/// Run `prose --compute-metrics --slug` (or let the nightly sweep do it) first so
/// dialogue-proportion checks have metrics to read.
/// </summary>
public static class LintProseCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool dryRun = args.Contains("--dry-run");
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --lint-prose --slug <slug> [--dry-run]");
            return 2;
        }

        var svc = services.GetRequiredService<RepetitionLintService>();
        Console.WriteLine($"Prose lint for {slug}{(dryRun ? " (dry run — nothing filed)" : "")}...");
        var r = await svc.LintAsync(slug, dryRun);

        Console.WriteLine();
        Console.WriteLine($"Node            : {r.NodeCode}");
        Console.WriteLine($"Beats scanned   : {r.BeatsScanned}");
        Console.WriteLine($"Echo words      : {r.EchoFindings}");
        Console.WriteLine($"Crutch phrases  : {r.PhraseFindings}");
        Console.WriteLine($"Pet words       : {r.PetWordFindings}");
        Console.WriteLine($"Dialogue issues : {r.DialogueFindings}");
        Console.WriteLine($"Structure       : {r.StructureFindings}   (ALT-SCENE / OUTLINE-HOOK / FIRST-TIME / BATCH-OUTLIER)");
        // The structural checks print what they examined; those lines are the difference between
        // "clean" and "could not look", so they are never truncated.
        foreach (var line in r.Lines.Where(l => l.StartsWith("[structure]"))) Console.WriteLine($"  {line}");
        var rest = r.Lines.Where(l => !l.StartsWith("[structure]")).ToList();
        foreach (var line in rest.Take(60)) Console.WriteLine($"  • {line}");
        if (rest.Count > 60) Console.WriteLine($"  ... and {rest.Count - 60} more (see Findings)");
        return 0;
    }
}
