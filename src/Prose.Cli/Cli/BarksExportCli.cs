using System.Text.Json;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// Portable-writing-service plan, Phase 4 — <c>prose --barks-export &lt;universe&gt; &lt;path&gt;
/// [--node &lt;slug&gt;]</c>: walk a universe's (or one book/chapter's) beats and emit every beat
/// with a single recorded POV speaker as <c>{barkId, speakerEntitySlug, text, context}</c> JSON —
/// see BarksExportService's doc comment for the full rationale.
/// </summary>
public static class BarksExportCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        if (args.Length < 2 || args[0].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Usage: prose --barks-export <universe> <path> [--node <slug>]");
            return 1;
        }
        var universe = args[0];
        var path = args[1];
        var node = Flag(args, "--node");

        var svc = services.GetRequiredService<BarksExportService>();
        try
        {
            var result = await svc.ExportAsync(universe, node);
            var json = JsonSerializer.Serialize(result.Barks, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);
            Console.WriteLine($"[barks-export] Wrote {result.Barks.Count} bark(s) to {path} " +
                $"(universe: {result.UniverseSlug}, {result.Skipped} beat(s) skipped — no single recorded POV).");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"[barks-export] {ex.Message}");
            return 1;
        }
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
