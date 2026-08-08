using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

public static class SessionBeatsCli
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
            Console.Error.WriteLine("Usage: prose --session-beats --session-id <guid>");
            return 2;
        }

        var svc     = services.GetRequiredService<EditSessionService>();
        var session = await svc.GetSessionAsync(sessionId);
        if (session == null) { Console.Error.WriteLine($"Session not found: {sessionId}"); return 1; }

        var beats = await svc.GetSessionBeatsAsync(sessionId);
        if (beats.Count == 0)
        {
            Console.WriteLine($"Session \"{session.Label}\" has no beats yet.");
            return 0;
        }

        Console.WriteLine($"Beats in session \"{session.Label}\" ({beats.Count} total):");
        Console.WriteLine();
        foreach (var esb in beats)
        {
            var title        = esb.Beat?.Title ?? "(untitled)";
            var num          = esb.Beat?.Number ?? 0;
            var versionDelta = esb.Beat != null
                ? $"v{esb.PriorVersion}→v{esb.Beat.Version}"
                : $"v{esb.PriorVersion}→?";
            Console.WriteLine($"  Beat {num,4}  {title,-40}  {versionDelta,-12}  edited {esb.EditedAt:HH:mm:ss} UTC");
        }
        return 0;
    }
}
