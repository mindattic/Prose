using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// <c>ss --prepare-audible</c> — build an Audible AI-narration hand-off package
/// for a node: a narration-clean manuscript (.audible.txt), a pronunciation
/// guide (.pronunciation.md), and a README with submission instructions.
///
/// All three files land in {PublishExportDirectory}/{Title}/Audible/.
///
/// Args (one of --slug / --id required):
///   --slug &lt;slug&gt;        Node slug.
///   --id &lt;guid|prefix&gt;  Node id; a unique prefix is accepted.
///   --no-phonetics       Skip the optional LLM phonetics pass (leave "Say it as"
///                        column blank for the author to fill in manually).
///
/// Exit codes:
///   0 — package written successfully.
///   1 — bad args / node not found / write error.
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
        Guid nodeId; string nodeSlug, nodeTitle;

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            Node? node;
            if (!string.IsNullOrWhiteSpace(slug))
            {
                node = await db.Nodes.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Slug == slug);
            }
            else if (Guid.TryParse(id, out var exact))
            {
                node = await db.Nodes.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == exact);
            }
            else
            {
                var prefix = id!.ToLowerInvariant();
                var matches = await db.Nodes.AsNoTracking()
                    .Where(s => s.Id.ToString().StartsWith(prefix))
                    .Take(2)
                    .ToListAsync();
                if (matches.Count > 1)
                {
                    Console.Error.WriteLine($"[prepare-audible] Id prefix '{id}' is ambiguous. Use a longer prefix or the full id.");
                    return 1;
                }
                node = matches.FirstOrDefault();
            }

            if (node == null)
            {
                var locator = slug != null ? $"slug '{slug}'" : $"id '{id}'";
                Console.Error.WriteLine($"[prepare-audible] No node found for {locator}.");
                return 1;
            }

            nodeId    = node.Id;
            nodeSlug  = node.Slug;
            nodeTitle = node.Title;
        }

        Console.WriteLine("[prepare-audible] Building Audible package:");
        Console.WriteLine($"   Id:    {nodeId}");
        Console.WriteLine($"   Slug:  {nodeSlug}");
        Console.WriteLine($"   Title: {nodeTitle}");
        Console.WriteLine($"   Phonetics LLM pass: {(withPhonetics ? "enabled" : "disabled")}");

        var service = services.GetRequiredService<AudiblePackageService>();

        AudiblePackageResult result;
        try
        {
            result = await service.BuildAsync(nodeId, withPhonetics);
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
