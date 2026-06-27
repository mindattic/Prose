using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// ss --set-summary (--slug &lt;slug&gt; | --id &lt;id&gt;) --text "..."
/// — writes the back-of-book blurb directly to Strands.Summary.
/// Use --file path.txt to load the text from a file instead of inline.
/// The KDP submission format (tagline bold + newline + this text) is
/// assembled by the browser-automation prompts at publish time.
/// </summary>
public static class SetSummaryCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, id = null, text = null, file = null;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug": if (i + 1 < args.Length) slug = args[++i]; break;
                case "--id":   if (i + 1 < args.Length) id   = args[++i]; break;
                case "--text": if (i + 1 < args.Length) text = args[++i]; break;
                case "--file": if (i + 1 < args.Length) file = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("[set-summary] --slug or --id required.");
            return 1;
        }

        if (file != null)
        {
            if (!File.Exists(file)) { Console.Error.WriteLine($"[set-summary] File not found: {file}"); return 1; }
            text = await File.ReadAllTextAsync(file);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            Console.Error.WriteLine("[set-summary] --text \"...\" or --file path.txt required.");
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

        if (strand == null) { Console.Error.WriteLine("[set-summary] Strand not found."); return 1; }

        strand.Summary = text.Trim();
        strand.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        Console.WriteLine($"[set-summary] \"{strand.Title}\" summary updated ({strand.Summary.Length} chars).");
        return 0;
    }
}
