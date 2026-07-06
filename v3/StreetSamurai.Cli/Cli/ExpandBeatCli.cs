using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// <c>ss --expand-beat</c> — expand one or all planned beats in a node to prose.
///
/// This is the headless counterpart to clicking ✨ in the node writer UI.
/// It uses <see cref="ProseWriterRouter.WriteAsync"/> with the node's
/// literary rules context, then saves via <see cref="NodeWorkbenchService.UpdateBeatTextAsync"/>.
/// Beats that already have prose are skipped unless <c>--force</c> is set.
///
/// Args (one of --slug / --id required):
///   --slug &lt;slug&gt;             Node slug.
///   --id &lt;guid|prefix&gt;        Node id; a unique prefix is enough.
///   --all                     Expand all planned (no prose) beats. Default when no --beat is given.
///   --beat &lt;beatId&gt;           Expand one specific beat by its UUID.
///   --force                   Re-expand beats that already have prose (overwrites).
///   --protagonist &lt;name|slug&gt; Character name or slug to add to CharactersInScene, activating
///                             DialogueService, ConsequenceService, and ConsequenceEngine during polish.
///   --model &lt;modelId&gt;         Force a specific cloud model for this run (e.g. claude-sonnet-4-6, claude-opus-4-8).
///                             Passed directly to the active cloud provider; ignored when --local is set.
///   --local                   Route generation to the configured local LLM (LocalLlmBaseUrl in settings).
///   --local-url &lt;url&gt;         Override the local endpoint URL for this run only (implies --local).
///   --local-key &lt;key&gt;         Override the local API/bearer key for this run only.
///   --local-model &lt;tag&gt;       Override the local model tag for this run only.
///
/// Exit codes:
///   0 — at least one beat expanded successfully.
///   1 — bad args, node not found, or no beats expanded.
/// </summary>
public static class ExpandBeatCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, id = null, beatId = null, modelOverride = null, protagonistArg = null;
        string? localUrl = null, localKey = null, localModel = null;
        bool force = args.Contains("--force");
        bool useLocal = args.Contains("--local");

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug":         if (i + 1 < args.Length) slug           = args[++i]; break;
                case "--id":           if (i + 1 < args.Length) id             = args[++i]; break;
                case "--beat":         if (i + 1 < args.Length) beatId         = args[++i]; break;
                case "--model":        if (i + 1 < args.Length) modelOverride  = args[++i]; break;
                case "--protagonist":  if (i + 1 < args.Length) protagonistArg = args[++i]; break;
                case "--local-url":    if (i + 1 < args.Length) { localUrl     = args[++i]; useLocal = true; } break;
                case "--local-key":    if (i + 1 < args.Length) localKey       = args[++i]; break;
                case "--local-model":  if (i + 1 < args.Length) localModel     = args[++i]; break;
            }
        }

        if (string.IsNullOrWhiteSpace(slug) && string.IsNullOrWhiteSpace(id))
        {
            Console.Error.WriteLine("[expand-beat] One of --slug or --id is required.");
            Console.Error.WriteLine("Usage: ss --expand-beat (--slug <slug> | --id <guid>) [--beat <beatId>] [--force]");
            return 1;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var router    = services.GetRequiredService<ProseWriterRouter>();
        var workbench = services.GetRequiredService<NodeWorkbenchService>();
        var canonDb   = services.GetRequiredService<IDatabaseService>();

        // Wire local LLM for this run if requested (ephemeral — not persisted to settings)
        if (useLocal)
        {
            var localSvc = services.GetRequiredService<LocalLlmService>();
            localSvc.ConfigureForRun(localUrl, localKey, localModel);
            services.GetRequiredService<LlmRouter>().SetRunProvider("local");
            Console.WriteLine($"[expand-beat] Using local LLM: {localUrl ?? services.GetRequiredService<SettingsService>().LocalLlmBaseUrl}");
        }

        // Wire cloud model override for this run (ephemeral — not persisted to settings)
        if (!string.IsNullOrWhiteSpace(modelOverride) && !useLocal)
        {
            services.GetRequiredService<LlmRouter>().SetRunModel(modelOverride);
            Console.WriteLine($"[expand-beat] Model override: {modelOverride}");
        }

        // Resolve node
        Guid nodeId; string nodeSlug, nodeTitle;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var query = db.Nodes.AsNoTracking();
            Core.Data.Entities.Node? node;
            if (!string.IsNullOrWhiteSpace(slug))
                node = await query.FirstOrDefaultAsync(s => s.Slug == slug);
            else if (Guid.TryParse(id, out var exact))
                node = await query.FirstOrDefaultAsync(s => s.Id == exact);
            else
            {
                var prefix = id!.ToLowerInvariant();
                var matches = await query.Where(s => s.Id.ToString().StartsWith(prefix)).Take(2).ToListAsync();
                node = matches.Count == 1 ? matches[0] : null;
                if (matches.Count > 1) { Console.Error.WriteLine($"[expand-beat] Id prefix '{id}' is ambiguous."); return 1; }
            }
            if (node == null) { Console.Error.WriteLine("[expand-beat] Node not found."); return 1; }
            nodeId = node.Id; nodeSlug = node.Slug; nodeTitle = node.Title;
        }

        Console.WriteLine($"[expand-beat] Node: \"{nodeTitle}\" ({nodeSlug})");

        // Resolve protagonist name for CharactersInScene (activates DialogueService + ConsequenceService)
        string? protagonistName = null;
        if (!string.IsNullOrWhiteSpace(protagonistArg))
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var parg = protagonistArg.Trim();
            var pEntity = await db.Entities.AsNoTracking()
                .Where(e => e.EntityType == "character" && e.IsActive
                            && (e.Name == parg || e.Slug == parg))
                .Select(e => e.Name)
                .FirstOrDefaultAsync();
            if (pEntity != null)
            {
                protagonistName = pEntity;
                Console.WriteLine($"[expand-beat] Protagonist: {protagonistName}");
            }
            else
            {
                Console.Error.WriteLine($"[expand-beat] Warning: protagonist '{parg}' not found — CharactersInScene will be empty.");
            }
        }

        // Load literary rules for the BeatContext
        string storyBible;
        try { storyBible = canonDb.GetLiteraryRulesPrompt() ?? ""; }
        catch { storyBible = ""; }

        // Load ordered beats
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId);

        // Filter to target beat(s)
        Guid? targetBeatId = null;
        if (!string.IsNullOrWhiteSpace(beatId) && Guid.TryParse(beatId, out var bg)) targetBeatId = bg;

        var targets = targetBeatId.HasValue
            ? ordered.Where(ob => ob.Beat.Id == targetBeatId.Value).ToList()
            : ordered.ToList();

        if (targets.Count == 0)
        {
            Console.Error.WriteLine("[expand-beat] No matching beats found.");
            return 1;
        }

        Console.WriteLine($"[expand-beat] {targets.Count} beat(s) to consider ({(force ? "force mode — overwriting existing prose" : "skipping beats with prose")}).");

        int expanded = 0, skipped = 0;
        string sceneSoFar = "";

        // Seed sceneSoFar from all beats before the first target
        int firstTargetIdx = ordered.IndexOf(targets[0]);
        for (int i = 0; i < firstTargetIdx && i < ordered.Count; i++)
            if (!string.IsNullOrWhiteSpace(ordered[i].Beat.Text))
                sceneSoFar += "\n\n" + ordered[i].Beat.Text;

        foreach (var ob in targets)
        {
            var beat = ob.Beat;
            bool hasText = !string.IsNullOrWhiteSpace(beat.Text);
            int beatIndex = ordered.IndexOf(ob);

            if (hasText && !force)
            {
                // Still accumulate existing prose into context
                sceneSoFar += "\n\n" + beat.Text;
                skipped++;
                continue;
            }

            var goal = beat.Synopsis ?? beat.BeatTitle ?? $"Beat #{beat.Number}";
            if (string.IsNullOrWhiteSpace(goal))
            {
                Console.WriteLine($"[expand-beat]   Beat #{beat.Number}: no synopsis — skipped.");
                skipped++;
                continue;
            }

            Console.Write($"[expand-beat]   Beat #{beat.Number} \"{(goal.Length > 60 ? goal[..60] + "…" : goal)}\"… ");

            try
            {
                var ctx = new BeatContext
                {
                    NodeId          = nodeId,
                    StoryBibleContext = storyBible,
                    SceneSoFar        = sceneSoFar.Length > 6000 ? sceneSoFar[^6000..] : sceneSoFar,
                    BeatGoal          = goal,
                    Subtext           = beat.Subtext ?? "",
                    CharactersInScene = protagonistName != null ? new[] { protagonistName } : Array.Empty<string>(),
                };
                var prose = await router.WriteAsync(ctx, beat.Id, beatIndex, ordered.Count);
                if (string.IsNullOrWhiteSpace(prose))
                {
                    Console.WriteLine("LLM returned empty — skipped.");
                    skipped++;
                    continue;
                }
                prose = prose.Trim();
                await workbench.UpdateBeatTextAsync(beat.Id, prose, expectedUpdatedAt: null);
                sceneSoFar += "\n\n" + prose;
                expanded++;
                Console.WriteLine($"ok ({prose.Length} chars).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"failed: {ex.Message}");
                skipped++;
            }
        }

        Console.WriteLine($"[expand-beat] Done: {expanded} expanded, {skipped} skipped.");
        return expanded > 0 ? 0 : 1;
    }
}
