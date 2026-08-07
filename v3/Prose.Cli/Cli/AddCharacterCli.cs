using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Models.Canon;
using Prose.Core.Services;

namespace Prose.Cli;

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
        var wasExisting = AdoptExistingId(repo, json, data);
        repo.Save(data);

        var verb = wasExisting ? "updated" : "created";
        Console.WriteLine($"[add-character] {verb} id={data.Id} name=\"{data.Name}\" age={data.Age} role=\"{data.Role}\" affiliation=\"{data.Affiliation}\"");
        return 0;
    }

    /// <summary>
    /// When a seed file omits "id", reuse the id of an existing character with the same name-slug
    /// so re-importing UPDATES instead of inserting a duplicate. See <see cref="SeedIdentity"/> for
    /// why checking <c>data.Id</c> cannot detect this (the model self-assigns one).
    /// </summary>
    private static bool AdoptExistingId(CharacterRepository repo, string rawJson, CharacterData data)
    {
        data.Id = SeedIdentity.ResolveId(
            rawJson,
            data.Id,
            data.Name,
            slug => repo.GetBySlug(slug)?.Id,
            JsonDirectoryRepository<CharacterData>.ToSlug,
            out var wasExisting);
        return wasExisting;
    }

    private static async Task<int> RunDirAsync(string dir, IServiceProvider sp)
    {
        if (!Directory.Exists(dir))
        {
            Console.Error.WriteLine($"[add-character] directory not found: {dir}");
            return 1;
        }

        var repo = sp.GetRequiredService<CharacterRepository>();
        int ok = 0, failed = 0, updated = 0;
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
                var wasExisting = AdoptExistingId(repo, json, data);
                repo.Save(data);
                Console.WriteLine($"  {(wasExisting ? "upd " : "new ")}  {Path.GetFileName(file)} — id={data.Id} name=\"{data.Name}\"");
                if (wasExisting) updated++;
                ok++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  FAIL  {Path.GetFileName(file)} — {ex.Message}");
                failed++;
            }
        }
        Console.WriteLine($"[add-character] {ok} saved ({ok - updated} new, {updated} updated), {failed} failed");
        return failed > 0 ? 1 : 0;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
