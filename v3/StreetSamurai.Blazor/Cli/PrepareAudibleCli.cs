using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --prepare-audible</c> — build an Audible AI-narration hand-off package
/// for a strand: a narration-clean manuscript (.audible.txt), a pronunciation
/// guide (.pronunciation.md), and a README with submission instructions.
///
/// All three files land in {PublishExportDirectory}/{Title}/Audible/.
///
/// Args (one of --slug / --id required):
///   --slug &lt;slug&gt;        Strand slug.
///   --id &lt;guid|prefix&gt;  Strand id; a unique prefix is accepted.
///   --no-phonetics       Skip the optional LLM phonetics pass (leave "Say it as"
///                        column blank for the author to fill in manually).
///
/// Exit codes:
///   0 — package written successfully.
///   1 — bad args / strand not found / write error.
/// </summary>
public static class PrepareAudibleCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? id = null, slug = null;
        bool withPhonetics = true;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--id":             if (i + 1 < args.Length) id   = args[++i]; break;
                case "--slug":           if (i + 1 < args.Length) slug = args[++i]; break;
                case "--no-phonetics":   withPhonetics = false; break;
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("[prepare-audible] One of --id or --slug is required.");
            Console.Error.WriteLine("Usage: ss --prepare-audible (--slug <slug> | --id <guid|prefix>) [--no-phonetics]");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        Guid strandId; string strandSlug, strandTitle;

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            Strand? strand;
            if (!string.IsNullOrWhiteSpace(slug))
            {
                strand = await db.Strands.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Slug == slug);
            }
            else if (Guid.TryParse(id, out var exact))
            {
                strand = await db.Strands.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == exact);
            }
            else
            {
                var prefix = id!.ToLowerInvariant();
                var matches = await db.Strands.AsNoTracking()
                    .Where(s => s.Id.ToString().StartsWith(prefix))
                    .Take(2)
                    .ToListAsync();
                if (matches.Count > 1)
                {
                    Console.Error.WriteLine($"[prepare-audible] Id prefix '{id}' is ambiguous. Use a longer prefix or the full id.");
                    return 1;
                }
                strand = matches.FirstOrDefault();
            }

            if (strand == null)
            {
                var locator = slug != null ? $"slug '{slug}'" : $"id '{id}'";
                Console.Error.WriteLine($"[prepare-audible] No strand found for {locator}.");
                return 1;
            }

            strandId    = strand.Id;
            strandSlug  = strand.Slug;
            strandTitle = strand.Title;
        }

        Console.WriteLine("[prepare-audible] Building Audible package:");
        Console.WriteLine($"   Id:    {strandId}");
        Console.WriteLine($"   Slug:  {strandSlug}");
        Console.WriteLine($"   Title: {strandTitle}");
        Console.WriteLine($"   Phonetics LLM pass: {(withPhonetics ? "enabled" : "disabled")}");

        var service = services.GetRequiredService<AudiblePackageService>();

        AudiblePackageResult result;
        try
        {
            result = await service.BuildAsync(strandId, withPhonetics);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[prepare-audible] Build failed: {ex.Message}");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("[prepare-audible] Package written:");
        Console.WriteLine($"   Manuscript  : {result.ManuscriptPath}");
        Console.WriteLine($"   Pronunciation: {result.LexiconPath}");
        Console.WriteLine($"   README      : {result.ReadmePath}");
        Console.WriteLine();
        Console.WriteLine($"   Word count  : {result.WordCount:N0}");
        Console.WriteLine($"   Term count  : {result.TermCount}");
        Console.WriteLine($"   Phonetics   : {(result.PhoneticsApplied ? "applied" : "skipped — fill 'Say it as' column manually")}");
        return 0;
    }
}
