using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// CLI surface to insert a FactionData JSON blob into canon. Routes through
/// FactionRepository.Save (EfRepository upsert): resolves a collision-safe slug,
/// stamps the Entity row (EntityType "faction", Status "canon"). The safe,
/// repeatable, service-layer path for canon faction writes — no hand-SQL.
///
/// UPSERT semantics: include the existing 32-char "id" to UPDATE that faction;
/// omit it (or leave blank) to CREATE a new one with a fresh UUIDv7. The JSON
/// shape is FactionData (name, motto, description, ideology, territory,
/// leadership, methods, goals, story_hooks, tags, …).
///
///   ss --add-faction --file path.json
/// </summary>
public static class AddFactionCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var file = ArgValue(args, "--file");
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            Console.Error.WriteLine("usage: ss --add-faction --file path.json");
            return 1;
        }

        FactionData? data;
        string json;
        try
        {
            json = await File.ReadAllTextAsync(file);
            data = JsonSerializer.Deserialize<FactionData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[add-faction] could not parse FactionData JSON: {ex.Message}");
            return 1;
        }

        if (data == null || string.IsNullOrWhiteSpace(data.Name))
        {
            Console.Error.WriteLine("[add-faction] FactionData must at least have a \"name\".");
            return 1;
        }

        var repo = sp.GetRequiredService<FactionRepository>();
        var wasExisting = AdoptExistingId(repo, json, data);
        repo.Save(data);

        Console.WriteLine($"[add-faction] {(wasExisting ? "updated" : "created")} id={data.Id} name=\"{data.Name}\" territory=\"{data.Territory}\"");
        return 0;
    }


    /// <summary>
    /// When a seed file omits "id", reuse the id of an existing faction with the same name-slug so
    /// re-importing UPDATES instead of inserting a duplicate. See <see cref="SeedIdentity"/> for why
    /// inspecting <c>data.Id</c> cannot detect this (the model self-assigns one on deserialization).
    /// </summary>
    private static bool AdoptExistingId(FactionRepository repo, string rawJson, FactionData data)
    {
        data.Id = SeedIdentity.ResolveId(
            rawJson,
            data.Id,
            data.Name,
            slug => repo.GetBySlug(slug)?.Id,
            JsonDirectoryRepository<FactionData>.ToSlug,
            out var wasExisting);
        return wasExisting;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
