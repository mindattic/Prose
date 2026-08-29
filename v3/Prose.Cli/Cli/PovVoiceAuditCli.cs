using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --pov-audit --slug &lt;slug&gt; [--dry-run]
///
/// POV discipline + voice distinctiveness audit (PovVoiceAuditService): head-hopping out of
/// the recorded POV narrator, and same-scene characters speaking in interchangeable registers.
/// Batched Haiku per chapter; findings ("POV " / "VOICE ", CraftChecklist) loop back into
/// future generation. Explicit invocation only — an LLM-cost decision.
/// </summary>
public static class PovVoiceAuditCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool dryRun = args.Contains("--dry-run");
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --pov-audit --slug <slug> [--dry-run]");
            return 2;
        }

        var svc = services.GetRequiredService<PovVoiceAuditService>();
        Console.WriteLine($"POV/voice audit for {slug}{(dryRun ? " (dry run — nothing filed)" : "")}...");
        var r = await svc.AuditAsync(slug, dryRun, Console.WriteLine);

        Console.WriteLine();
        Console.WriteLine($"Node             : {r.NodeCode}");
        Console.WriteLine($"Beats audited    : {r.BeatsAudited} (only beats with a recorded POV)");
        Console.WriteLine($"Head-hops        : {r.HeadHopFindings}");
        Console.WriteLine($"Voice sameness   : {r.VoiceSamenessFindings}");
        return 0;
    }
}

/// <summary>
/// prose --hook-audit --slug &lt;slug&gt; [--dry-run]
///
/// Chapter-hook strength analysis (ChapterHookService): classifies every chapter's final
/// passage (question/danger/decision/revelation/arrival/emotional/none, strength 0-3) in one
/// batched Haiku call. Weak non-final endings file "HOOK " CraftChecklist findings. New
/// chapters get this automatically at chapter close.
/// </summary>
public static class HookAuditCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        bool dryRun = args.Contains("--dry-run");
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--slug") { slug = args[i + 1]; i++; }
        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --hook-audit --slug <slug> [--dry-run]");
            return 2;
        }

        var svc = services.GetRequiredService<ChapterHookService>();
        Console.WriteLine($"Chapter-hook audit for {slug}{(dryRun ? " (dry run — nothing filed)" : "")}...");
        var r = await svc.AuditAsync(slug, dryRun);

        Console.WriteLine();
        Console.WriteLine($"Node             : {r.NodeCode}");
        Console.WriteLine($"Chapters audited : {r.ChaptersAudited}");
        Console.WriteLine($"Weak endings     : {r.WeakEndings} (non-final chapters, strength ≤ 1)");
        foreach (var c in r.Results.OrderBy(x => x.ChapterIndex))
            Console.WriteLine($"  [{c.Strength}] {c.HookType,-11} {c.ChapterTitle} — {c.Rationale}");
        return 0;
    }
}
