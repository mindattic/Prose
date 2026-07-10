using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// ss --update-register-exemplars (--slug &lt;slug&gt; | --id &lt;guid&gt;) [--top N] [--dry-run]
///
/// Closes the register feedback loop: surfaces the top-N beats by EmotionalScore,
/// asks the LLM which register law each beat best demonstrates, and appends them
/// as candidate exemplar entries to docs/registers/&lt;NAME&gt;.md.
///
/// Prerequisites: run --examine-emotion first to populate Beat.EmotionalScore.
///
/// Args:
///   --slug &lt;slug&gt;   Node slug. One of --slug / --id required.
///   --id &lt;guid&gt;     Node GUID (unique prefix accepted).
///   --top N         Number of top beats to surface (default 5).
///   --dry-run       Print candidates but do not modify the register file.
/// </summary>
public static class UpdateRegisterExemplarsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        string? id   = null;
        int     topN = 5;
        bool    dryRun = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":    if (i + 1 < args.Length) slug   = args[++i]; break;
                case "--id":      if (i + 1 < args.Length) id     = args[++i]; break;
                case "--top":     if (i + 1 < args.Length && int.TryParse(args[++i], out var n)) topN = n; break;
                case "--dry-run": dryRun = true; break;
            }
        }

        if (slug is null && id is null)
        {
            Console.Error.WriteLine("Usage: ss --update-register-exemplars (--slug <slug> | --id <guid>) [--top N] [--dry-run]");
            return 1;
        }

        await using var db = await services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>()
            .CreateDbContextAsync();

        Guid nodeId;
        if (slug is not null)
        {
            var s = await db.Nodes.FirstOrDefaultAsync(x => x.Slug == slug);
            if (s is null) { Console.Error.WriteLine($"Node '{slug}' not found."); return 1; }
            nodeId = s.Id;
        }
        else
        {
            var prefix = id!.ToUpperInvariant();
            var s = await db.Nodes.FirstOrDefaultAsync(x =>
                x.Id.ToString().Replace("-", "").ToUpperInvariant().StartsWith(prefix.Replace("-", "")));
            if (s is null) { Console.Error.WriteLine($"Node id '{id}' not found."); return 1; }
            nodeId = s.Id;
        }

        var svc = services.GetRequiredService<RegisterExemplarService>();
        Console.WriteLine($"Surfacing top {topN} exemplar candidates…");

        var (registerName, nodeSlug, candidates) =
            await svc.FindExemplarsAsync(nodeId, topN);

        if (candidates.Count == 0)
        {
            Console.WriteLine("No candidates found. Check that --examine-emotion has been run for this node.");
            return 0;
        }

        Console.WriteLine($"\nRegister: {registerName}  |  Node: {nodeSlug}");
        Console.WriteLine(new string('─', 60));

        foreach (var c in candidates)
        {
            Console.WriteLine($"\n  Beat {c.BeatNumber}  EmotionalScore={c.EmotionalScore:F1}");
            Console.WriteLine($"  Law: {c.LawName}");
            Console.WriteLine($"  Quote: \"{c.KeyQuote}\"");
            Console.WriteLine($"  Why: {c.Reason}");
        }

        Console.WriteLine();

        var markdown = svc.FormatAsMarkdown(candidates, nodeSlug, registerName);

        if (dryRun)
        {
            Console.WriteLine("── DRY RUN — register file not modified. Proposed addition: ──");
            Console.WriteLine(markdown);
        }
        else
        {
            var filePath = svc.GetRegisterFilePath(registerName);
            svc.AppendToRegisterFile(registerName, markdown);
            Console.WriteLine($"Candidates appended to: {filePath}");
            Console.WriteLine("Review and promote from '<!-- CANDIDATE -->' to '**Confirmed**' when ready.");
        }

        return 0;
    }
}
