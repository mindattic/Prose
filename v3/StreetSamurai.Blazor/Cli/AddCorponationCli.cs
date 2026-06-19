using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// CLI surface to insert a CorponationData JSON blob into canon. Routes through
/// CorponationRepository.Save (EfRepository upsert): resolves a collision-safe slug,
/// stamps the Entity row (EntityType "corponation", Status "canon").
///
/// UPSERT semantics: include the existing 32-char "id" to UPDATE that corp;
/// omit it (or leave blank) to CREATE a new one with a fresh UUIDv7.
///
///   ss --add-corponation --file path.json
/// </summary>
public static class AddCorponationCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var file = ArgValue(args, "--file");
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            Console.Error.WriteLine("usage: ss --add-corponation --file path.json");
            return 1;
        }

        CorponationData? data;
        try
        {
            var json = await File.ReadAllTextAsync(file);
            data = JsonSerializer.Deserialize<CorponationData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[add-corponation] could not parse CorponationData JSON: {ex.Message}");
            return 1;
        }

        if (data == null || string.IsNullOrWhiteSpace(data.Name))
        {
            Console.Error.WriteLine("[add-corponation] CorponationData must at least have a \"name\".");
            return 1;
        }

        var repo = sp.GetRequiredService<CorponationRepository>();
        repo.Save(data);

        Console.WriteLine($"[add-corponation] saved id={data.Id} name=\"{data.Name}\" sector=\"{data.Sector}\"");
        return 0;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
