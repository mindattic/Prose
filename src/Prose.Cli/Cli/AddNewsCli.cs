using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI surface to insert a NewsData JSON blob into canon. Same legacy JSON
/// shape used by engine/data/news/*.json — NewsRepository.Save persists the
/// Entity row and the Records.Json blob (read by the /news UI).
///
///   prose --add-news --file path.json
/// </summary>
public static class AddNewsCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var file = ArgValue(args, "--file");
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            Console.Error.WriteLine("usage: prose --add-news --file path.json");
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
        var wasExisting = AdoptExistingId(repo, json, data);
        repo.Save(data);

        Console.WriteLine($"[add-news] {(wasExisting ? "updated" : "created")} id={data.Id} headline=\"{data.Headline}\" outlet=\"{data.Source}\" reporter=\"{data.Reporter}\" date=\"{data.Date}\"");
        return 0;
    }


    /// <summary>
    /// When a seed file omits "id", reuse the id of an existing news with the same name-slug so
    /// re-importing UPDATES instead of inserting a duplicate. See <see cref="SeedIdentity"/> for why
    /// inspecting <c>data.Id</c> cannot detect this (the model self-assigns one on deserialization).
    /// </summary>
    private static bool AdoptExistingId(NewsRepository repo, string rawJson, NewsData data)
    {
        data.Id = SeedIdentity.ResolveId(
            rawJson,
            data.Id,
            data.Headline,
            slug => repo.GetBySlug(slug)?.Id,
            JsonDirectoryRepository<NewsData>.ToSlug,
            out var wasExisting);
        return wasExisting;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
