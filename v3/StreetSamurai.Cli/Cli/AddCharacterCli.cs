using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// CLI surface to insert a CharacterData JSON blob into canon. Same legacy JSON
/// shape used by engine/data/people/*.json — CharacterRepository.Save routes it
/// through CharacterMapper into the fully relational Characters tables.
///
///   ss --add-character --file path.json
///   ss --add-character --dir path/to/folder      (one CharacterData JSON file per character;
///                                                  imports all *.json in one process — avoids
///                                                  a separate dotnet host startup per character
///                                                  for bulk seeding)
/// </summary>
public static class AddCharacterCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var dir = ArgValue(args, "--dir");
        if (!string.IsNullOrWhiteSpace(dir))
            return await RunDirAsync(dir, sp);

        var file = ArgValue(args, "--file");
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            Console.Error.WriteLine("usage: ss --add-character --file path.json | --dir path/to/folder");
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

    private static async Task<int> RunDirAsync(string dir, IServiceProvider sp)
    {
        if (!Directory.Exists(dir))
        {
            Console.Error.WriteLine($"[add-character] directory not found: {dir}");
            return 1;
        }

        var repo = sp.GetRequiredService<CharacterRepository>();
        int ok = 0, failed = 0;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json").OrderBy(f => f))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var data = JsonSerializer.Deserialize<CharacterData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
                if (data == null || string.IsNullOrWhiteSpace(data.Name))
                {
                    Console.Error.WriteLine($"  FAIL  {Path.GetFileName(file)} — could not deserialize or missing name");
                    failed++;
                    continue;
                }
                repo.Save(data);
                Console.WriteLine($"  ok    {Path.GetFileName(file)} — id={data.Id} name=\"{data.Name}\"");
                ok++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  FAIL  {Path.GetFileName(file)} — {ex.Message}");
                failed++;
            }
        }
        Console.WriteLine($"[add-character] {ok} saved, {failed} failed");
        return failed > 0 ? 1 : 0;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
