using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// CLI surface to insert a CharacterData JSON blob into canon. Same legacy JSON
/// shape used by engine/data/people/*.json — CharacterRepository.Save routes it
/// through CharacterMapper into the fully relational Characters tables.
///
///   ss --add-character --file path.json
/// </summary>
public static class AddCharacterCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var file = ArgValue(args, "--file");
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            Console.Error.WriteLine("usage: ss --add-character --file path.json");
            return 1;
        }

        var json = await File.ReadAllTextAsync(file);
        var data = JsonSerializer.Deserialize<CharacterData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
        if (data == null)
        {
            Console.Error.WriteLine("could not deserialize CharacterData");
            return 1;
        }

        var repo = sp.GetRequiredService<CharacterRepository>();
        repo.Save(data);

        Console.WriteLine($"[add-character] saved id={data.Id} name=\"{data.Name}\" age={data.Age} role=\"{data.Role}\" affiliation=\"{data.Affiliation}\"");
        return 0;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
