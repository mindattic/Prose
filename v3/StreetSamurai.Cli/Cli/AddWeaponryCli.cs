using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// CLI surface to insert a WeaponryData JSON blob into canon. Routes through
/// WeaponryRepository.Save (EfRepository upsert) — same pattern as
/// AddCharacterCli/AddPlaceCli.
///
///   ss --add-weapon --file path.json
///   ss --add-weapon --dir path/to/folder      (one WeaponryData JSON file per weapon)
/// </summary>
public static class AddWeaponryCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var dir = ArgValue(args, "--dir");
        if (!string.IsNullOrWhiteSpace(dir))
            return await RunDirAsync(dir, sp);

        var file = ArgValue(args, "--file");
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            Console.Error.WriteLine("usage: ss --add-weapon --file path.json | --dir path/to/folder");
            return 1;
        }

        var json = await File.ReadAllTextAsync(file);
        var data = JsonSerializer.Deserialize<WeaponryData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
        if (data == null || string.IsNullOrWhiteSpace(data.Name))
        {
            Console.Error.WriteLine("[add-weapon] could not deserialize WeaponryData or missing name");
            return 1;
        }

        var repo = sp.GetRequiredService<WeaponryRepository>();
        var wasExisting = AdoptExistingId(repo, json, data);
        repo.Save(data);

        Console.WriteLine($"[add-weapon] {(wasExisting ? "updated" : "created")} id={data.Id} name=\"{data.Name}\" category=\"{data.Category}\"");
        return 0;
    }

    private static async Task<int> RunDirAsync(string dir, IServiceProvider sp)
    {
        if (!Directory.Exists(dir))
        {
            Console.Error.WriteLine($"[add-weapon] directory not found: {dir}");
            return 1;
        }

        var repo = sp.GetRequiredService<WeaponryRepository>();
        int ok = 0, failed = 0, updated = 0;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json").OrderBy(f => f))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var data = JsonSerializer.Deserialize<WeaponryData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
                if (data == null || string.IsNullOrWhiteSpace(data.Name))
                {
                    Console.Error.WriteLine($"  FAIL  {Path.GetFileName(file)} — could not deserialize or missing name");
                    failed++;
                    continue;
                }
                var wasExistingItem = AdoptExistingId(repo, json, data);
                repo.Save(data);
                Console.WriteLine($"  {(wasExistingItem ? "upd " : "new ")}  {Path.GetFileName(file)} — id={data.Id} name=\"{data.Name}\"");
                if (wasExistingItem) updated++;
                ok++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  FAIL  {Path.GetFileName(file)} — {ex.Message}");
                failed++;
            }
        }
        Console.WriteLine($"[add-weapon] {ok} saved ({ok - updated} new, {updated} updated), {failed} failed");
        return failed > 0 ? 1 : 0;
    }


    /// <summary>
    /// When a seed file omits "id", reuse the id of an existing weapon with the same name-slug so
    /// re-importing UPDATES instead of inserting a duplicate. See <see cref="SeedIdentity"/> for why
    /// inspecting <c>data.Id</c> cannot detect this (the model self-assigns one on deserialization).
    /// </summary>
    private static bool AdoptExistingId(WeaponryRepository repo, string rawJson, WeaponryData data)
    {
        data.Id = SeedIdentity.ResolveId(
            rawJson,
            data.Id,
            data.Name,
            slug => repo.GetBySlug(slug)?.Id,
            JsonDirectoryRepository<WeaponryData>.ToSlug,
            out var wasExisting);
        return wasExisting;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
