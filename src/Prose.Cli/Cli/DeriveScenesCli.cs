using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --derive-scenes --slug &lt;slug|id&gt; [--apply] [--gap-minutes N] [--universe x]
///
/// <para>Groups a book's existing beats into scenes using only what the beats already record —
/// where they happen and how much time passes — and never touches prose. Dry-run by default;
/// <c>--apply</c> is the only thing that writes.</para>
/// </summary>
public static class DeriveScenesCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        var apply = false;
        var gap = SceneDerivationService.DefaultTimeGapMinutes;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--slug" or "--node" when i + 1 < args.Length: slug = args[++i]; break;
                case "--apply": apply = true; break;
                case "--gap-minutes" when i + 1 < args.Length && int.TryParse(args[i + 1], out var g):
                    gap = g; i++; break;
            }
        }

        if (slug == null)
        {
            Console.Error.WriteLine("Usage: prose --derive-scenes --slug <slug|id> [--apply] [--gap-minutes N]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        Guid nodeId;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var node = Guid.TryParse(slug, out var parsed)
                ? await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == parsed)
                : await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Slug == slug);
            if (node == null) { Console.Error.WriteLine($"Node not found: {slug}"); return 1; }
            nodeId = node.Id;
        }

        var svc = services.GetRequiredService<SceneDerivationService>();
        var report = await svc.DeriveAsync(nodeId, apply, gap);

        Console.WriteLine($"[derive-scenes] {report.NodeTitle} — {report.Chapters.Count} chapter(s), "
                          + $"{report.ScenesProposed} scene(s) {(apply ? "APPLIED" : "proposed (dry run)")}");
        Console.WriteLine();

        foreach (var c in report.Chapters)
        {
            // Coverage before findings: a chapter with no place and no timing data yields one scene
            // because every rule was inert, not because it is genuinely one continuous scene.
            var coverage = $"place {c.BeatsWithPlace}/{c.BeatsTotal}, timing {c.BeatsWithTiming}/{c.BeatsTotal}";
            if (c.CouldNotLook)
            {
                Console.WriteLine($"  {c.ChapterTitle}  [{c.BeatsTotal} beats]");
                Console.WriteLine($"    COULD NOT LOOK — no beat carries a place or a time ({coverage}). "
                                  + "Not derived. Populate locations first (prose --extract-beat-locations).");
                continue;
            }

            Console.WriteLine($"  {c.ChapterTitle}  [{c.BeatsTotal} beats → {c.Scenes.Count} scene(s); {coverage}]");
            foreach (var s in c.Scenes)
                Console.WriteLine($"      • {s.Title}  ({s.BeatCount} beats, #{s.Beats[0].Number}–#{s.Beats[^1].Number}) — {s.Reason}");
        }

        if (report.ChaptersCouldNotLook > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"  {report.ChaptersCouldNotLook} chapter(s) had no location or timing data at all. "
                              + "Those are unknown, not single-scene.");
        }

        if (!apply)
        {
            Console.WriteLine();
            Console.WriteLine("  Dry run — nothing written. Re-run with --apply to create the scenes.");
        }
        return 0;
    }
}
