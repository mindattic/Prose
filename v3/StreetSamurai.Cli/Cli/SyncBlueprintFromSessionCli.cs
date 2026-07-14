using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

public static class SyncBlueprintFromSessionCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? sessionIdStr = null;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--session-id") { sessionIdStr = args[i + 1]; i++; }
        }

        if (sessionIdStr == null || !Guid.TryParse(sessionIdStr, out var sessionId))
        {
            Console.Error.WriteLine("Usage: ss --sync-blueprint-from-session --session-id <guid>");
            return 2;
        }

        var svc = services.GetRequiredService<BlueprintSyncService>();
        Console.WriteLine("Syncing blueprint from session...");

        var report = await svc.SyncFromSessionAsync(sessionId);

        Console.WriteLine();
        Console.WriteLine($"Session     : {report.SessionLabel}");
        Console.WriteLine($"Confirmed   : {report.Confirmed}");
        Console.WriteLine($"Diverged    : {report.Diverged}");
        Console.WriteLine($"Unverified  : {report.Unverified}");

        if (report.DriftSummaries.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("BLUEPRINT-DRIFT findings:");
            foreach (var d in report.DriftSummaries)
                Console.WriteLine($"  ! {d}");
        }

        return 0;
    }
}
