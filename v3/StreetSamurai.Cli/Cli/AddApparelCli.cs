using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Cli;

/// <summary>
/// CLI surface to insert an ApparelData JSON blob into canon. Routes through
/// ApparelRepository.Save (EfRepository upsert) — same pattern as
/// AddCharacterCli/AddPlaceCli.
///
///   ss --add-apparel --file path.json
///   ss --add-apparel --dir path/to/folder      (one ApparelData JSON file per item)
/// </summary>
public static class AddApparelCli
{
    public static async Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var dir = ArgValue(args, "--dir");
        if (!string.IsNullOrWhiteSpace(dir))
            return await RunDirAsync(dir, sp);

        var file = ArgValue(args, "--file");
        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
        {
            Console.Error.WriteLine("usage: ss --add-apparel --file path.json | --dir path/to/folder");
            return 1;
        }

        var json = await File.ReadAllTextAsync(file);
        var data = JsonSerializer.Deserialize<ApparelData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
        if (data == null || string.IsNullOrWhiteSpace(data.Name))
        {
            Console.Error.WriteLine("[add-apparel] could not deserialize ApparelData or missing name");
            return 1;
        }

        var repo = sp.GetRequiredService<ApparelRepository>();
        repo.Save(data);

        Console.WriteLine($"[add-apparel] saved id={data.Id} name=\"{data.Name}\" category=\"{data.Category}\"");
        return 0;
    }

    private static async Task<int> RunDirAsync(string dir, IServiceProvider sp)
    {
        if (!Directory.Exists(dir))
        {
            Console.Error.WriteLine($"[add-apparel] directory not found: {dir}");
            return 1;
        }

        var repo = sp.GetRequiredService<ApparelRepository>();
        int ok = 0, failed = 0;
        foreach (var file in Directory.EnumerateFiles(dir, "*.json").OrderBy(f => f))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var data = JsonSerializer.Deserialize<ApparelData>(json, new JsonSerializerOptions
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
        Console.WriteLine($"[add-apparel] {ok} saved, {failed} failed");
        return failed > 0 ? 1 : 0;
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
