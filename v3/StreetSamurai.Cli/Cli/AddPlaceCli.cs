using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// Insert or update a canon Place/District from a DistrictData JSON blob — the
/// safe, repeatable, service-layer path for canon place writes (no hand-SQL,
/// no LLM guesswork). Routes through DistrictRepository.Save (EfRepository
/// upsert): it resolves a collision-safe slug, stamps the Entity row
/// (EntityType "place", Status "canon"), and writes the Records.Json blob.
///
/// UPSERT semantics: include the existing 32-char "id" in the JSON to UPDATE
/// that place; omit it (or leave blank) to CREATE a new one with a fresh
/// UUIDv7. The JSON shape is exactly what `get_place` returns (name,
/// description, atmosphere, demographics, economy, power_structure, dangers,
/// opportunities, story_hooks, connections, coordinates, tags, aliases, …).
///
///   ss --add-place --file path.json
///   ss --add-place --file path.json --print   (echo the saved record back)
///   ss --add-place --dir path/to/folder       (one DistrictData JSON file per place;
///                                               imports all *.json in one process)
///
/// Note: embeddings + Edges are populated by their own passes (re-embed /
/// relationship import); this writes the canonical entity + record.
/// </summary>
public static class AddPlaceCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var dir = ArgValue(args, "--dir");
        if (!string.IsNullOrWhiteSpace(dir))
            return await RunDirAsync(dir, sp);

        var file = ArgValue(args, "--file");
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            Console.Error.WriteLine("usage: ss --add-place --file path.json [--print]");
            return 1;
        }

        DistrictData? data;
        string json;
        try
        {
            json = await File.ReadAllTextAsync(file);
            data = JsonSerializer.Deserialize<DistrictData>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[add-place] could not parse DistrictData JSON: {ex.Message}");
            return 1;
        }

        if (data == null || string.IsNullOrWhiteSpace(data.Name))
        {
            Console.Error.WriteLine("[add-place] DistrictData must at least have a \"name\".");
            return 1;
        }

        var repo = sp.GetRequiredService<DistrictRepository>();
        // DistrictData self-assigns a default Id, so a present Id doesn't imply "update".
        // This previously only DETECTED the existing row to label the output, then saved under the
        // freshly-minted id anyway — printing "updated" while actually inserting a duplicate.
        // Now the existing id is adopted, so the save really is an update.
        var wasUpdate = AdoptExistingId(repo, json, data);
        repo.Save(data);

        Console.WriteLine($"[add-place] {(wasUpdate ? "updated" : "created")} place id={data.Id} name=\"{data.Name}\"");

        if (args.Contains("--print"))
        {
            var saved = repo.GetBySlug(JsonDirectoryRepository<DistrictData>.ToSlug(data.Name));
            Console.WriteLine(JsonSerializer.Serialize(saved, new JsonSerializerOptions { WriteIndented = true }));
        }
        return 0;
    }

    private static async Task<int> RunDirAsync(string dir, IServiceProvider sp)
    {
        if (!Directory.Exists(dir))
        {
            Console.Error.WriteLine($"[add-place] directory not found: {dir}");
            return 1;
        }

        var repo = sp.GetRequiredService<DistrictRepository>();
        int ok = 0, failed = 0, updated = 0;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json").OrderBy(f => f))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var data = JsonSerializer.Deserialize<DistrictData>(json, new JsonSerializerOptions
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
        Console.WriteLine($"[add-place] {ok} saved ({ok - updated} new, {updated} updated), {failed} failed");
        return failed > 0 ? 1 : 0;
    }

    /// <summary>
    /// When a seed file omits "id", reuse the id of an existing place with the same name-slug so
    /// re-importing UPDATES instead of inserting a duplicate. See <see cref="SeedIdentity"/>.
    /// </summary>
    private static bool AdoptExistingId(DistrictRepository repo, string rawJson, DistrictData data)
    {
        data.Id = SeedIdentity.ResolveId(
            rawJson,
            data.Id,
            data.Name,
            slug => repo.GetBySlug(slug)?.Id,
            JsonDirectoryRepository<DistrictData>.ToSlug,
            out var wasExisting);
        return wasExisting;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
