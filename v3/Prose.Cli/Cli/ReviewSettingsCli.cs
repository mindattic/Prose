using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>ss --review-settings [--set &lt;key&gt; &lt;value&gt;]</c> — view or update review voting settings.
///
/// With no args: print all settings.
/// With --set key value: update one setting.
///
/// Keys:
///   ballots          Score-only ballot count (integer ≥ 1)
///   prose            Full prose reviews per run (integer ≥ 0)
///   panel            Persona panel depth (integer ≥ 1)
///   readers          Default reader count (integer ≥ 1)
///   max-concurrency  Parallel ballot slots 1-50 (integer)
///   judge-provider   Provider that synthesizes the summary (claude|openai|gemini|deepseek)
///   allowed-providers Comma-separated provider whitelist (e.g. "claude,openai")
/// </summary>
public static class ReviewSettingsCli
{
    public static Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        string? key = null, value = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--set" && i + 2 < args.Length)
            {
                key = args[++i];
                value = args[++i];
            }
        }

        var settings = services.GetRequiredService<SettingsService>();

        if (key != null && value != null)
        {
            if (!ApplySetting(settings, key, value, out var err))
            {
                Console.Error.WriteLine($"[review-settings] {err}");
                return Task.FromResult(1);
            }
            settings.Flush();
            Console.WriteLine($"[review-settings] {key} = {value}");
        }

        PrintAll(settings);
        return Task.FromResult(0);
    }

    private static bool ApplySetting(SettingsService s, string key, string value, out string? err)
    {
        err = null;
        switch (key.ToLowerInvariant())
        {
            case "ballots":
                if (!int.TryParse(value, out var b)) { err = "ballots must be an integer."; return false; }
                s.ReviewBallots = b; return true;
            case "prose":
                if (!int.TryParse(value, out var p)) { err = "prose must be an integer."; return false; }
                s.ReviewProse = p; return true;
            case "panel":
                if (!int.TryParse(value, out var pn)) { err = "panel must be an integer."; return false; }
                s.ReviewPanel = pn; return true;
            case "readers":
                if (!int.TryParse(value, out var r)) { err = "readers must be an integer."; return false; }
                s.ReviewReaders = r; return true;
            case "max-concurrency":
                if (!int.TryParse(value, out var mc)) { err = "max-concurrency must be an integer."; return false; }
                s.ReviewMaxConcurrency = mc; return true;
            case "judge-provider":
                s.ReviewJudgeProvider = value; return true;
            case "allowed-providers":
                s.ReviewAllowedProviders = value; return true;
            default:
                err = $"Unknown key '{key}'. Valid keys: ballots, prose, panel, readers, max-concurrency, judge-provider, allowed-providers";
                return false;
        }
    }

    private static void PrintAll(SettingsService s)
    {
        Console.WriteLine("Review voting settings:");
        Console.WriteLine($"  ballots           = {s.ReviewBallots}");
        Console.WriteLine($"  prose             = {s.ReviewProse}");
        Console.WriteLine($"  panel             = {s.ReviewPanel}");
        Console.WriteLine($"  readers           = {s.ReviewReaders}");
        Console.WriteLine($"  max-concurrency   = {s.ReviewMaxConcurrency}");
        Console.WriteLine($"  judge-provider    = {s.ReviewJudgeProvider}");
        Console.WriteLine($"  allowed-providers = {s.ReviewAllowedProviders}");
    }
}
