using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

public static class ListSessionsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, limitStr = null;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug")  { slug     = args[i + 1]; i++; }
            if (args[i] == "--limit") { limitStr = args[i + 1]; i++; }
        }

        int limit = int.TryParse(limitStr, out var l) ? l : 20;

        if (slug == null)
        {
            Console.Error.WriteLine("Usage: ss --list-sessions --slug <slug> [--limit N]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var svc       = services.GetRequiredService<EditSessionService>();

        await using var db = await dbFactory.CreateDbContextAsync();
        var node = await db.Nodes.FirstOrDefaultAsync(
            n => n.Slug == slug || (n.NodeCode != null && n.NodeCode.ToUpper() == slug.ToUpper()));
        if (node == null) { Console.Error.WriteLine($"Node not found: {slug}"); return 1; }

        var sessions = await svc.GetSessionsAsync(node.Id, limit);
        if (sessions.Count == 0)
        {
            Console.WriteLine($"No sessions found for {node.NodeCode ?? node.Slug}.");
            return 0;
        }

        Console.WriteLine($"Sessions for {node.NodeCode ?? node.Slug} (most recent first):");
        Console.WriteLine();
        foreach (var s in sessions)
        {
            var status   = s.ClosedAt.HasValue ? "closed" : "OPEN";
            var duration = s.ClosedAt.HasValue
                ? $"{(s.ClosedAt.Value - s.StartedAt).TotalMinutes:F0}m"
                : "ongoing";
            Console.WriteLine($"  [{status}] {s.Label}  ({s.SessionType})");
            Console.WriteLine($"    ID       : {s.EditSessionId}");
            Console.WriteLine($"    Started  : {s.StartedAt:yyyy-MM-dd HH:mm} UTC  |  Duration: {duration}");
            Console.WriteLine($"    Beats    : {s.BeatCount}");
            Console.WriteLine();
        }
        return 0;
    }
}
