using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Models;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// Headless driver for <see cref="CombatSceneWriter"/> — generate a resource-tracked
/// combat sequence from the command line (battlefield geometry, sides, loadouts,
/// and an ammo / grenade / bio-battery ledger the writer must respect), print the
/// action prose, and optionally write it to a file.
///
///   prose --combat --file scene.json [--out prose.txt]
///   prose --combat --location "Hegewisch" --objective "survive the kill team" --exchanges 6 --tone Cinematic
///
/// The --file JSON is a <see cref="CombatSceneRequest"/>: BattlefieldLocation,
/// Environment, Sides[] (Label, Combatants[], UnnamedCombatants[], InitialPosition,
/// Goal, SharedLoadout), Objective, NumExchanges, Tone, PrecedingContext, OpeningBeat,
/// and InitialResources{} (per-character AmmoByWeapon, Grenades, BioBatteryPercent,
/// MealContext). When InitialResources are present the writer tracks ammo/grenades/
/// neural charge across every beat and enforces hard limits (no firing empty weapons,
/// no abilities past the flatline threshold).
///
/// Inline flags build a MINIMAL request when no --file is given; rich sides and
/// resource tracking require --file. Tone accepts the CombatTone names
/// (Brutal | Cinematic | Desperate | Clinical | Chaotic).
/// </summary>
public static class CombatCli
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        CombatSceneRequest request;

        var file = ArgValue(args, "--file");
        if (!string.IsNullOrWhiteSpace(file))
        {
            if (!File.Exists(file))
            {
                Console.Error.WriteLine($"[combat] file not found: {file}");
                return 1;
            }
            try
            {
                var json = await File.ReadAllTextAsync(file);
                request = JsonSerializer.Deserialize<CombatSceneRequest>(json, JsonOpts)
                          ?? throw new InvalidOperationException("deserialized to null");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[combat] could not parse CombatSceneRequest JSON: {ex.Message}");
                return 1;
            }
        }
        else
        {
            var location = ArgValue(args, "--location");
            if (string.IsNullOrWhiteSpace(location))
            {
                Console.Error.WriteLine("usage: prose --combat --file scene.json [--out prose.txt]");
                Console.Error.WriteLine("   or: prose --combat --location \"<place>\" [--objective \"...\"] [--exchanges N]");
                Console.Error.WriteLine("           [--tone Brutal|Cinematic|Desperate|Clinical|Chaotic] [--environment \"...\"] [--opening \"...\"]");
                return 1;
            }
            var toneStr = ArgValue(args, "--tone") ?? "Brutal";
            if (!Enum.TryParse<CombatTone>(toneStr, ignoreCase: true, out var tone))
            {
                Console.Error.WriteLine($"[combat] unknown tone '{toneStr}'. Use: Brutal | Cinematic | Desperate | Clinical | Chaotic");
                return 1;
            }
            int.TryParse(ArgValue(args, "--exchanges"), out var exchanges);
            request = new CombatSceneRequest
            {
                BattlefieldLocation = location,
                Environment = ArgValue(args, "--environment") ?? "",
                Objective = ArgValue(args, "--objective") ?? "",
                NumExchanges = exchanges > 0 ? exchanges : 4,
                Tone = tone,
                OpeningBeat = ArgValue(args, "--opening") ?? "",
                PrecedingContext = ArgValue(args, "--preceding") ?? "",
            };
        }

        var writer = sp.GetRequiredService<CombatSceneWriter>();
        writer.OnBeatProgress += p =>
            Console.Error.WriteLine($"   …beat {p.BeatIndex}/{p.TotalBeats} [{p.ActingSide}] {p.Status}");

        Console.WriteLine($"[combat] {Math.Max(1, request.NumExchanges)} exchanges · tone {request.Tone} · location \"{request.BattlefieldLocation}\"");
        if (request.InitialResources.Count > 0)
            Console.WriteLine($"[combat] resource tracking ON for: {string.Join(", ", request.InitialResources.Keys)}");
        Console.WriteLine("[combat] writing — this calls the LLM once per exchange…");

        GeneratedCombatScene scene;
        try
        {
            scene = await writer.WriteCombatSceneAsync(request);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[combat] generation failed: {ex.Message}");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine(scene.FullText);
        Console.WriteLine();

        if (scene.FinalResources.Count > 0)
        {
            Console.WriteLine("=== FINAL RESOURCES ===");
            foreach (var (name, res) in scene.FinalResources)
            {
                var ammo = res.AmmoByWeapon.Count > 0
                    ? "ammo " + string.Join(", ", res.AmmoByWeapon.Select(kv => $"{kv.Key}={kv.Value}"))
                    : "";
                var nades = res.Grenades.Count > 0
                    ? "grenades " + string.Join(", ", res.Grenades.Select(g => $"{g.Type} x{g.Count}"))
                    : "";
                var parts = new[] { ammo, nades, $"neural {res.BioBatteryPercent}%" }
                    .Where(s => !string.IsNullOrEmpty(s));
                Console.WriteLine($"  {name}: {string.Join(" | ", parts)}");
            }
        }

        var outFile = ArgValue(args, "--out");
        if (!string.IsNullOrWhiteSpace(outFile))
        {
            await File.WriteAllTextAsync(outFile, scene.FullText);
            Console.WriteLine($"[combat] prose written to {outFile}");
        }

        return scene.Beats.Count > 0 ? 0 : 1;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
