using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --write-story</c> — create a new node via the bible-first workflow:
///
///   1. Insert a Node row (status=draft, no beats yet).
///   2. Call NodeOutlineService to generate the node bible and planned beats.
///   3. Print the bible + URL. Stop here with <c>--outline-only</c>.
///   4. With <c>--narrate</c>, also run TTS after the prose pass (future).
///
/// The bible's ## BEAT SPINE section is parsed into Beat rows with Synopsis set
/// to the planned goal. Open the node in the UI to expand beats into prose.
///
/// Args:
///   --seed "..."         One-line prompt that drives the bible. Required.
///   --title "..."        Override the auto-generated working title.
///   --kind &lt;k&gt;          Kind tag: "episode" (default), "vignette", "chapter", etc.
///   --beats N            Target beat count in the spine (default: 12).
///   --compete N          Generate N competing outlines (2-5), score each, keep the winner.
///   --outline-only         Stop after generating the bible; do not open the URL.
///   --narrate            (placeholder) Run TTS after prose expansion.
/// </summary>
public static class WriteNodeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? seed = null, title = null;
        string kind = "episode";
        int targetBeats = 12, compete = 1;
        bool bibleOnly = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed":       if (i + 1 < args.Length) seed       = args[++i]; break;
                case "--title":      if (i + 1 < args.Length) title      = args[++i]; break;
                case "--kind":       if (i + 1 < args.Length) kind       = args[++i]; break;
                case "--beats":      if (i + 1 < args.Length && int.TryParse(args[++i], out var n)) targetBeats = n; break;
                case "--compete":    if (i + 1 < args.Length && int.TryParse(args[++i], out var c)) compete = c; break;
                case "--outline-only": bibleOnly = true; break;
            }
        }

        if (string.IsNullOrWhiteSpace(seed))
        {
            Console.Error.WriteLine("[write-story] --seed is required.");
            Console.Error.WriteLine("Usage: prose --write-story --seed \"...\" [--title \"...\"] [--kind episode] [--beats 12] [--compete N] [--outline-only]");
            return 2;
        }

        var dbFactory    = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var bibleService = services.GetRequiredService<NodeOutlineService>();

        Guid nodeId;
        string bibleText, workingTitle, slug;

        if (compete >= 2)
        {
            // ── Compete mode: N outlines, Legion scores, keep winner ──────────
            var competeService = services.GetRequiredService<PremiseToOutlineService>();
            Console.WriteLine($"[write-story] Compete mode: {compete} outlines");
            try
            {
                var (wId, wBible, winnerIdx) = await competeService.CreateNodeAsync(
                    seed!, title, kind, targetBeats, compete);
                nodeId     = wId;
                bibleText    = wBible;
                workingTitle = title ?? DeriveTitle(seed!);
                slug         = EpisodeGeneratorService.Slugify(workingTitle) + "-" + nodeId.ToString("N")[..8];
                Console.WriteLine($"[write-story] Outline {winnerIdx} selected as winner.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[write-story] Compete failed: {ex.Message}");
                return 1;
            }
        }
        else
        {
            // ── Standard single-outline path ─────────────────────────────────
            nodeId     = Guid.CreateVersion7();
            workingTitle = !string.IsNullOrEmpty(title) ? title : DeriveTitle(seed!);
            slug         = EpisodeGeneratorService.Slugify(workingTitle) + "-" + nodeId.ToString("N")[..8];

            Console.WriteLine($"[write-story] Creating node: \"{workingTitle}\"");

            await using (var db = await dbFactory.CreateDbContextAsync())
            {
                var node = NodeFactory.Create(kind);
                node.Id        = nodeId;
                node.Title     = workingTitle;
                node.Slug      = slug;
                node.Seed      = seed!;
                node.Status    = "draft";
                node.Description = seed!.Length > 200 ? seed![..200] : seed!;
                node.CreatedAt = DateTime.UtcNow;
                node.UpdatedAt = DateTime.UtcNow;
                db.Nodes.Add(node);
                await db.SaveChangesAsync();
            }

            Console.WriteLine($"[write-story] Node created: {nodeId}");
            Console.WriteLine($"[write-story] Generating node bible ({targetBeats} beats)…");
            try
            {
                bibleText = await bibleService.GenerateAndSaveAsync(nodeId, seed!, workingTitle, targetBeats);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[write-story] Bible generation failed: {ex.Message}");
                return 1;
            }
        }

        // 3. Print the bible
        Console.WriteLine();
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine(bibleText);
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine();

        // Report planned beats
        var beatPlans = NodeOutlineService.ParseBeatSpine(bibleText);
        Console.WriteLine($"[write-story] {beatPlans.Count} planned beats created from the spine.");

        var url = $"https://localhost:7103/node/{slug}";
        Console.WriteLine($"[write-story] Open in the unified writer to expand beats into prose:");
        Console.WriteLine($"   Id:    {nodeId}");
        Console.WriteLine($"   Slug:  {slug}");
        Console.WriteLine($"   Title: {workingTitle}");
        Console.WriteLine($"   Kind:  {kind}");
        Console.WriteLine($"   Beats: {beatPlans.Count} planned (prose not yet written)");
        Console.WriteLine($"   URL:   {url}");

        if (!bibleOnly)
            Console.WriteLine("   Next:  open the URL, then click ✨ on each beat to write prose from the plan.");

        return 0;
    }

    private static string DeriveTitle(string seed)
    {
        // Use the first ~8 words of the seed as a working title
        var words = seed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var titleWords = words.Take(8);
        var raw = string.Join(" ", titleWords);
        return raw.Length < seed.Length ? raw + "…" : raw;
    }
}
