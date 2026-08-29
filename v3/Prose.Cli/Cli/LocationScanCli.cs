using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --location-scan [--min-travel-minutes N]
///
/// Runs the LocationContradictionService corpus scan — "a character can only be in one place at
/// a time" — over located_at Edges (StoryValidFrom) and dated legacy chapter-beats. Conflicts
/// are filed to the Findings inbox (Contradiction category). Built 2026-07 but never invocable
/// until 2026-08-28; the per-beat place axis (Beat.PlaceEntityId, `--extract-beat-locations`)
/// now exists, while the per-beat TIME axis (a live Beat.InWorldDate) is still a known gap —
/// the scan reports its own data status honestly.
/// </summary>
public static class LocationScanCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        var svc = services.GetRequiredService<LocationContradictionService>();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--min-travel-minutes" && int.TryParse(args[i + 1], out var m))
            { svc.MinTravelMinutes = m; i++; }
        }

        Console.WriteLine("Location-contradiction scan (corpus-wide)...");
        var r = await svc.ScanAsync();

        Console.WriteLine();
        Console.WriteLine($"Characters examined : {r.CharactersExamined}");
        Console.WriteLine($"Presence facts      : {r.PresenceFacts}");
        Console.WriteLine($"Conflicts           : {r.Conflicts.Count}");
        Console.WriteLine($"Status              : {r.StatusNote}");
        foreach (var c in r.Conflicts.Take(25))
            Console.WriteLine($"  {c.CharacterName}: '{c.PlaceA}' → '{c.PlaceB}' in {c.Delta.TotalMinutes:F0}min ({c.AtA:yyyy-MM-dd HH:mm} → {c.AtB:HH:mm})");
        if (r.Conflicts.Count > 25) Console.WriteLine($"  ... and {r.Conflicts.Count - 25} more (see Findings)");
        return 0;
    }
}
