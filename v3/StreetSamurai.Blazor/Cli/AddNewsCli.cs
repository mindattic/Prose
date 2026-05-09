using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Blazor.Cli;

/// <summary>
/// CLI surface to insert a NewsData JSON blob into canon. Same legacy JSON
/// shape used by engine/data/news/*.json — NewsRepository.Save persists the
/// Entity row and the Records.Json blob (read by the /news UI).
///
///   ss --add-news --file path.json
/// </summary>
public static class AddNewsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var file = ArgValue(args, "--file");
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            Console.Error.WriteLine("usage: ss --add-news --file path.json");
            return 1;
        }

        var json = await File.ReadAllTextAsync(file);
        var data = JsonSerializer.Deserialize<NewsData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
        if (data == null)
        {
            Console.Error.WriteLine("could not deserialize NewsData");
            return 1;
        }

        var repo = sp.GetRequiredService<NewsRepository>();
        repo.Save(data);

        Console.WriteLine($"[add-news] saved id={data.Id} headline=\"{data.Headline}\" outlet=\"{data.Source}\" reporter=\"{data.Reporter}\" date=\"{data.Date}\"");
        return 0;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
