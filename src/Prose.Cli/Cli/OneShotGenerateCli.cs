using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// Portable-writing-service plan, Phase 2 — <c>prose --generate-scene</c>: write a scene or line
/// of dialog without a pre-existing Book/Chapter/Beat row.
///
///   prose --generate-scene "&lt;beat goal&gt;" [--characters "Name1,Name2"] [--location "..."]
///       [--subtext "..."] [--node &lt;slug|code|guid&gt;] [--universe &lt;slug&gt;]
///       [--beat-index N] [--total-beats N]
///
/// Ephemeral by default (no --node): pacing, dialogue voice profiles, canon-fact grounding,
/// consequence/gear constraints, ambient sensory grounding, and entity pre-check warnings all
/// still apply — everything gated on caller-supplied context rather than persisted node/beat
/// history. Pass --node to borrow an existing book's canon/continuity ("attached mode") without
/// writing a Beat row to it. See OneShotGenerationService's doc comment for the full rationale.
///
/// Universe: uses the ambient --universe/PROSE_UNIVERSE scope like any other command; --universe
/// here is also accepted as a per-call override (same token, same effect — see
/// UniverseBootstrap.ParseSlug), matching UniverseInterchangeCli's identical pattern.
/// </summary>
public static class OneShotGenerateCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        if (args.Length < 1 || args[0].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Usage: prose --generate-scene \"<beat goal>\" [--characters \"A,B\"] " +
                "[--location \"...\"] [--subtext \"...\"] [--node <slug>] [--universe <slug>] " +
                "[--beat-index N] [--total-beats N]");
            return 1;
        }

        var beatGoal = args[0];
        var charactersArg = Flag(args, "--characters");
        var location = Flag(args, "--location");
        var subtext = Flag(args, "--subtext");
        var node = Flag(args, "--node");
        var universe = Flag(args, "--universe");
        var beatIndex = int.TryParse(Flag(args, "--beat-index"), out var bi) ? bi : 0;
        var totalBeats = int.TryParse(Flag(args, "--total-beats"), out var tb) ? tb : 0;

        var characters = string.IsNullOrWhiteSpace(charactersArg)
            ? Array.Empty<string>()
            : charactersArg.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var svc = services.GetRequiredService<OneShotGenerationService>();
        try
        {
            var result = await svc.GenerateAsync(new OneShotGenerationService.OneShotGenerationRequest(
                BeatGoal: beatGoal,
                Characters: characters,
                Location: location,
                Subtext: subtext,
                Node: node,
                Universe: universe,
                BeatIndex: beatIndex,
                TotalBeats: totalBeats));

            if (string.IsNullOrWhiteSpace(result.Text))
            {
                Console.Error.WriteLine("[generate-scene] LLM returned empty prose.");
                return 1;
            }

            Console.WriteLine(result.Text);
            Console.Error.WriteLine();
            Console.Error.WriteLine($"[generate-scene] universe={result.UniverseSlug} " +
                $"{(result.AttachedNodeSlug != null ? $"node={result.AttachedNodeSlug} " : "")}words={result.WordCount}");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"[generate-scene] {ex.Message}");
            return 1;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"[generate-scene] {ex.Message}");
            return 1;
        }
    }

    private static string? Flag(string[] args, string name)
    {
        var idx = Array.IndexOf(args, name);
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
