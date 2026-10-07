using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --hub-narration on|off|status
///
/// Runtime toggle for <c>CommandNarrator</c> (Prose.Hub): when ON, every Hub command's existing
/// log line is followed by a Haiku-generated plain-English gloss of what it did. Default OFF —
/// this flips the same <see cref="SettingsService.HubNarrationEnabled"/> flag the narrator reads
/// before each call, so it takes effect immediately on the running Hub, no restart required.
/// </summary>
public static class HubNarrationCli
{
    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var settings = services.GetRequiredService<SettingsService>();

        if (args.Contains("on"))
        {
            settings.HubNarrationEnabled = true;
            Console.WriteLine("[hub-narration] ON — commands will get a Haiku-generated gloss after their log line.");
        }
        else if (args.Contains("off"))
        {
            settings.HubNarrationEnabled = false;
            Console.WriteLine("[hub-narration] OFF.");
        }
        else
        {
            Console.WriteLine($"[hub-narration] {(settings.HubNarrationEnabled ? "ON" : "OFF")}");
            if (!args.Contains("status"))
                Console.WriteLine("Usage: prose --hub-narration on|off|status");
        }

        return Task.FromResult(0);
    }
}
