using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// ss --generate-summary (--slug &lt;slug&gt; | --id &lt;id&gt;) [--model &lt;id&gt;] [--dry-run]
/// — LLM-generates a 100–150 word back-of-book blurb (KDP product description)
///   from the strand's prose beats and saves it to Strands.Summary.
/// Use --dry-run to print without saving.
/// The KDP submission format (tagline in bold + newline + Summary) is assembled
/// by the browser-automation prompts at publish time.
/// </summary>
public static class GenerateSummaryCli
{
    private const string SystemPrompt =
        "You are a professional book cover copywriter specializing in science fiction and cyberpunk. " +
        "Your task is to write a compelling back-of-book product description for Amazon KDP. " +
        "Rules: 100–150 words only. No spoilers beyond the inciting incident. " +
        "Write in present tense. Draw the reader in with voice and stakes. " +
        "Do not use the phrase 'in a world where'. Do not summarize the ending. " +
        "Output the blurb text only — no preamble, no headers, no word count.";

    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, id = null, model = null;
        bool dryRun = false;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":    if (i + 1 < args.Length) slug  = args[++i]; break;
                case "--id":     if (i + 1 < args.Length) id    = args[++i]; break;
                case "--model":  if (i + 1 < args.Length) model = args[++i]; break;
                case "--dry-run": dryRun = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("[generate-summary] --slug or --id required.");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var strand = !string.IsNullOrWhiteSpace(slug)
            ? await db.Strands.FirstOrDefaultAsync(s => s.Slug == slug || s.StrandCode == slug)
            : Guid.TryParse(id, out var g)
                ? await db.Strands.FirstOrDefaultAsync(s => s.Id == g)
                : await db.Strands.Where(s => s.Id.ToString().StartsWith(id!.ToLower())).Take(2).ToListAsync()
                    is { Count: 1 } m ? m[0] : null;

        if (strand == null) { Console.Error.WriteLine("[generate-summary] Strand not found."); return 1; }

        // Gather the strand's prose in reading order (up to ~12k chars for context).
        var prose = await (from sb in db.StrandBeats.AsNoTracking()
                           join b in db.Beats.AsNoTracking() on sb.BeatId equals b.Id
                           where sb.StrandId == strand.Id && sb.IsEnabled && b.Text != null
                           orderby sb.SortKey
                           select b.Text).ToListAsync();

        var combined = string.Join("\n\n", prose);
        if (combined.Length > 12000) combined = combined[..12000];

        if (string.IsNullOrWhiteSpace(combined))
        {
            Console.Error.WriteLine($"[generate-summary] Strand \"{strand.Title}\" has no beat prose.");
            return 1;
        }

        var userPrompt =
            $"Story title: {strand.Title}\n\n" +
            (strand.Synopsis != null ? $"Logline: {strand.Synopsis}\n\n" : "") +
            $"Prose excerpt:\n{combined}";

        var llm = services.GetRequiredService<ILlmService>();
        Console.Write($"[generate-summary] Generating summary for \"{strand.Title}\"...");
        var summary = await llm.GenerateAsync(SystemPrompt, userPrompt, temperature: 0.7, maxTokens: 512, model: model);
        Console.WriteLine(" done.");

        Console.WriteLine();
        Console.WriteLine(summary);
        Console.WriteLine();

        if (dryRun)
        {
            Console.WriteLine("[generate-summary] Dry run — not saved. Re-run without --dry-run to commit.");
            return 0;
        }

        strand.Summary = summary.Trim();
        strand.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        Console.WriteLine($"[generate-summary] Saved to \"{strand.Title}\" ({strand.Summary.Length} chars).");
        return 0;
    }
}
