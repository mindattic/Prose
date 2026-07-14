using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

public static class CloseSessionCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, sessionIdStr = null;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug")       { slug         = args[i + 1]; i++; }
            if (args[i] == "--session-id") { sessionIdStr = args[i + 1]; i++; }
        }

        if (slug == null && sessionIdStr == null)
        {
            Console.Error.WriteLine("Usage: ss --close-session (--slug <slug> | --session-id <guid>)");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<StreetSamuraiDbContext>>();
        var svc       = services.GetRequiredService<EditSessionService>();

        Guid? nodeId = null;
        if (slug != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var node = await db.Nodes.FirstOrDefaultAsync(
                n => n.Slug == slug || (n.NodeCode != null && n.NodeCode.ToUpper() == slug.ToUpper()));
            if (node == null) { Console.Error.WriteLine($"Node not found: {slug}"); return 1; }
            nodeId = node.Id;
        }

        Guid? sid = sessionIdStr != null && Guid.TryParse(sessionIdStr, out var g) ? g : null;
        var session = await svc.CloseSessionAsync(nodeId, sid);

        var duration = session.ClosedAt.HasValue
            ? $"{(session.ClosedAt.Value - session.StartedAt).TotalMinutes:F0}m"
            : "?";

        Console.WriteLine($"Session closed: {session.EditSessionId}");
        Console.WriteLine($"  Label     : {session.Label}");
        Console.WriteLine($"  Beat count: {session.BeatCount}");
        Console.WriteLine($"  Duration  : {duration}");
        return 0;
    }
}
