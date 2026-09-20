using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// prose --list-species
///
/// Prints every species in the current universe (canonical name, label, sentience).
/// The five GLMZ values are: human, ai, elf, synthetic, unknown.
/// Returns exit code 0 always.
/// </summary>
public static class ListSpeciesCli
{
    public static int Run(IServiceProvider services)
    {
        var repo = services.GetRequiredService<SpeciesRepository>();
        var list = repo.GetAll();

        if (list.Count == 0)
        {
            Console.WriteLine("No species found. Run `prose --seed` to load the canonical set.");
            return 0;
        }

        var header = $"{"Name",-12} {"Label",-32} {"Sentient"}";
        Console.WriteLine(header);
        Console.WriteLine(new string('─', header.Length));
        foreach (var s in list)
            Console.WriteLine($"  {s.Name,-10}  {s.Label,-30}  {(s.Sentient ? "yes" : "no")}");

        Console.WriteLine($"\n{list.Count} species total.");
        return 0;
    }
}
