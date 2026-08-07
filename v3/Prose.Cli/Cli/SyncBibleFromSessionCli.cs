using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

public static class SyncBibleFromSessionCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? sessionIdStr = null;
        bool dryRun = args.Contains("--dry-run");
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--session-id") { sessionIdStr = args[i + 1]; i++; }
        }

        if (sessionIdStr == null || !Guid.TryParse(sessionIdStr, out var sessionId))
        {
            Console.Error.WriteLine("Usage: ss --sync-bible-from-session --session-id <guid> [--dry-run]");
            return 2;
        }

        var svc = services.GetRequiredService<BibleSyncService>();
        Console.WriteLine(dryRun ? "Dry run — will not write to file." : "Extracting facts and updating bible...");

        var report = await svc.ExtractFromSessionAsync(sessionId, dryRun);

        Console.WriteLine();
        Console.WriteLine($"Session : {report.SessionLabel}");
        Console.WriteLine($"Node    : {report.NodeCode}");
        Console.WriteLine($"Facts   : {report.Facts.Count}");
        Console.WriteLine();

        if (report.Facts.Count == 0)
        {
            Console.WriteLine("No extractable facts found in this session.");
            return 0;
        }

        foreach (var f in report.Facts)
        {
            var beatRef = f.BeatNumber > 0 ? $" [Beat {f.BeatNumber}]" : "";
            Console.WriteLine($"  [{f.Category}]{beatRef} {f.Fact}");
        }

        if (!dryRun)
        {
            Console.WriteLine();
            if (report.WroteToFile)
                Console.WriteLine($"Appended to: {report.FilePath}");
            else
                Console.WriteLine("Nothing written (bible file not found or no facts).");
        }

        return 0;
    }
}
