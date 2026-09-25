using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

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
            Console.Error.WriteLine("Usage: prose --close-session (--slug <slug> | --session-id <guid>)");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var svc       = services.GetRequiredService<EditSessionService>();

        Guid? nodeId = null;
        if (slug != null)
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var node = await Prose.Core.Services.NodeRefResolver.ResolveNodeAsync(db, slug); // GUID, other universes
            if (node == null) { Console.Error.WriteLine($"Node not found: {slug}"); return 1; }
            nodeId = node.Id;
        }

        // A mistyped --session-id used to fall back to closing the node's open session instead.
        Guid? sid = null;
        if (sessionIdStr != null)
        {
            if (!Guid.TryParse(sessionIdStr, out var g)) { Console.Error.WriteLine($"--session-id is not a GUID: {sessionIdStr}"); return 2; }
            sid = g;
        }
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
