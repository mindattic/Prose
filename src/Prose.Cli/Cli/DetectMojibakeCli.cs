using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --detect-mojibake --slug &lt;slug|code|id&gt; [--json]
///
/// Read-only. Reports UTF-8-read-as-Windows-1252 corruption in a node's beats. Exit code 0
/// when clean, 1 when anything is found — so a retrofit runbook can loop
/// <c>prose --repair --fix-mojibake</c> until this returns 0.
/// </summary>
public static class DetectMojibakeCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null;
        var json = args.Contains("--json");
        for (int i = 0; i < args.Length; i++)
            if (args[i] == "--slug" && i + 1 < args.Length) slug = args[++i];

        if (string.IsNullOrWhiteSpace(slug))
        {
            Console.Error.WriteLine("Usage: prose --detect-mojibake --slug <slug|code|id> [--json]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var detector  = services.GetRequiredService<MojibakeRepairService>();

        Guid nodeId;
        string title;
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            var resolved = await NodeRefResolver.ResolveAsync(db, slug);
            if (resolved == null)
            {
                Console.Error.WriteLine($"[detect-mojibake] {NodeRefResolver.NotFoundMessage(slug)}");
                return 1;
            }
            nodeId = resolved.Value;
            title = await db.Nodes.IgnoreQueryFilters().Where(n => n.Id == nodeId).Select(n => n.Title).FirstAsync();
        }

        var beats   = await detector.DetectNodeAsync(nodeId);
        var dirty   = beats.BeatsAffected > 0;

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                node_id = nodeId,
                title,
                clean = !dirty,
                beats_affected = beats.BeatsAffected,
                beat_hits = beats.Hits.Take(20).Select(h => new { beat_id = h.BeatId, excerpt = h.Excerpt }),
            }, new JsonSerializerOptions { WriteIndented = true }));
            return dirty ? 1 : 0;
        }

        Console.WriteLine($"[detect-mojibake] {title}");
        Console.WriteLine($"  beats affected: {beats.BeatsAffected}");
        foreach (var h in beats.Hits.Take(10))
            Console.WriteLine($"    beat {h.BeatId}: {h.Excerpt}");
        Console.WriteLine(dirty
            ? "  RESULT: DIRTY — run `prose --repair --fix-mojibake` (one run now peels every layer), then re-check."
            : "  RESULT: CLEAN");
        return dirty ? 1 : 0;
    }
}
