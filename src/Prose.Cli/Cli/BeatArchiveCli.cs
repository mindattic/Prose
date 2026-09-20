using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --beat-archive --beat-id &lt;guid&gt;</c> — thin CLI wrapper around
/// <see cref="BeatArchiveService"/> (the shared assembly logic used by <c>get_beat_archive</c>
/// and the Beat Archive UI tab too). See <see cref="BeatArchiveService"/> for what's assembled.
/// </summary>
public static class BeatArchiveCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var beatIdStr = ArgValue(args, "--beat-id");
        if (!Guid.TryParse(beatIdStr, out var beatId))
        {
            Console.Error.WriteLine("Usage: prose --beat-archive --beat-id <guid>");
            return 1;
        }

        var archiveService = services.GetRequiredService<BeatArchiveService>();
        var archive = await archiveService.BuildArchiveAsync(beatId);
        if (archive == null)
        {
            Console.Error.WriteLine($"[beat-archive] Beat {beatId} not found.");
            return 1;
        }

        Console.WriteLine(JsonSerializer.Serialize(archive, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
