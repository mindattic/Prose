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
        foreach (var line in r.Lines.Take(40)) Console.WriteLine($"  • {line}");
        if (r.Lines.Count > 40) Console.WriteLine($"  ... and {r.Lines.Count - 40} more (see Findings)");
        return 0;
    }
}
