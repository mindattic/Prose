using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Cli;

public static class StartSessionCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? slug = null, label = null, type = "custom";
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--slug")  { slug  = args[i + 1]; i++; }
            if (args[i] == "--label") { label = args[i + 1]; i++; }
            if (args[i] == "--type")  { type  = args[i + 1]; i++; }
        }

        if (slug == null || label == null)
        {
            Console.Error.WriteLine("Usage: ss --start-session --slug <slug> --label \"prose-pass-1\" [--type prose-pass|gripes-cleanup|logic-sweep|custom]");
            return 2;
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<ProseDbContext>>();
        var svc       = services.GetRequiredService<EditSessionService>();
        await using var db = await dbFactory.CreateDbContextAsync();

        var node = await db.Nodes.FirstOrDefaultAsync(
            n => n.Slug == slug || (n.NodeCode != null && n.NodeCode.ToUpper() == slug.ToUpper()));
        if (node == null) { Console.Error.WriteLine($"Node not found: {slug}"); return 1; }

        var session = await svc.StartSessionAsync(node.Id, label, type);
        Console.WriteLine($"Session started: {session.EditSessionId}");
        Console.WriteLine($"  Node    : {node.NodeCode ?? node.Slug}");
        Console.WriteLine($"  Label   : {session.Label}");
        Console.WriteLine($"  Type    : {session.SessionType}");
        Console.WriteLine($"  Started : {session.StartedAt:yyyy-MM-dd HH:mm} UTC");
        return 0;
    }
}
