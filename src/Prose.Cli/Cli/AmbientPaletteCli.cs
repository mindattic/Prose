using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --ambient-palette --character &lt;characterId&gt; [--as-of "date"]
/// Prints the sensory detail palette for a character's carried gear.
/// Use the output as a prompt injection block when writing a scene.
/// </summary>
public static class AmbientPaletteCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider services)
    {
        Guid? characterId = null;
        DateTime? asOf = null;

        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--character":
                    if (Guid.TryParse(args[i + 1], out var g)) characterId = g;
                    i++;
                    break;
                case "--as-of":
                    if (DateTime.TryParse(args[i + 1], out var dt)) asOf = dt;
                    i++;
                    break;
            }
        }

        if (characterId == null)
        {
            Console.Error.WriteLine("Usage: prose --ambient-palette --character <characterId> [--as-of date]");
            return 1;
        }

        var svc = services.GetRequiredService<AmbientDetailInjector>();
        var palette = await svc.GetPaletteAsync(characterId.Value, asOf);

        if (palette.IsEmpty)
        {
            Console.WriteLine($"No carry edges or sensory_hints found for {palette.CharacterName}.");
            Console.WriteLine("To add hints: insert a WeaponSpec row with SpecKey='sensory_hints' and");
            Console.WriteLine("  SpecValue='hint1; hint2; hint3' for the weapon entity.");
            return 0;
        }

        var block = svc.FormatPaletteAsPromptBlock(palette);
        Console.WriteLine(block);
        return 0;
    }
}
