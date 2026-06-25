using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// Dry-run validation surface for the Doc Context Stack (the dynamic .md working-set engine).
/// Given a strand and optional scene/goal text, prints the rotating cast of <c>.md</c> docs
/// that WOULD load — tier, why it loaded, similarity score, size — plus the assembled context
/// block and a token estimate. Read-only; changes no prompts and no canon. Use it to tune
/// tiers/triggers/thresholds before wiring the engine into prose generation or the session.
///
///   ss --doc-context --slug &lt;strand&gt; [--goal "&lt;scene text&gt;"] [--budget &lt;tokens&gt;]
///
///   --slug    strand to act as the active context (its CODE drives strand-tier scope).
///   --goal    scene/beat text to trigger topic docs against; defaults to the strand synopsis.
///   --budget  token budget for the assembled block (default 2000).
/// </summary>
public static class DocContextCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var slug   = ArgValue(args, "--slug");
        var goal   = ArgValue(args, "--goal");
        var budget = int.TryParse(ArgValue(args, "--budget"), out var b) ? b : 2000;

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: ss --doc-context --slug <strand> [--goal \"<text>\"] [--budget <tokens>]");
            return 1;
        }

        var dbFactory = sp.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        Guid strandId; string strandCode; string title; string triggerText;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var strand = await db.Strands.AsNoTracking()
                .Where(s => s.Slug == slug)
                .Select(s => new { s.Id, s.StrandCode, s.Title, s.Synopsis, s.Seed })
                .FirstOrDefaultAsync();
            if (strand == null) { Console.Error.WriteLine($"[doc-context] strand not found: {slug}"); return 1; }

            strandId    = strand.Id;
            strandCode  = strand.StrandCode ?? "";
            title       = strand.Title ?? slug!;
            triggerText = !string.IsNullOrWhiteSpace(goal)            ? goal!
                        : !string.IsNullOrWhiteSpace(strand.Synopsis) ? strand.Synopsis!
                        : (strand.Seed ?? "");
        }

        var svc = sp.GetRequiredService<DocContextService>();
        var result = await svc.PrepareContextAsync(strandId, strandCode, triggerText, budget);

        Console.WriteLine($"[doc-context] strand=\"{title}\"  code={(string.IsNullOrEmpty(strandCode) ? "(none)" : strandCode)}  budget={budget} tok");
        Console.WriteLine($"[doc-context] trigger text: {Clip(triggerText, 180)}");
        Console.WriteLine();
        Console.WriteLine($"LOADED {result.Loaded.Count} doc(s), ~{result.EstimatedTokens} tok of {budget}:");
        foreach (var d in result.Loaded)
            Console.WriteLine($"  {d.Tier,-7} {d.Reason,-22} {d.Chars,6}c  {d.RelativePath}");
        Console.WriteLine();
        Console.WriteLine("===== ASSEMBLED BLOCK =====");
        Console.WriteLine(result.Block);
        return 0;
    }

    private static string Clip(string s, int n) => s.Length <= n ? s : s[..n] + "…";

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
