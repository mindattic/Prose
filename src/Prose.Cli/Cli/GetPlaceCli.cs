using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Prose.Core.Services;

namespace Prose.Cli;

/// <summary>
/// <c>prose --get-place --name "&lt;exact name&gt;" [--print-raw]</c> — read a canon Place/District's
/// full DistrictData record (description, atmosphere, demographics, economy, power_structure,
/// dangers, opportunities, story_hooks, connections, coordinates, tags, aliases, id, …).
///
/// Built 2026-08-31: no CLI read path existed for places — only the write-side <c>--add-place</c>
/// upsert, which needs a complete DistrictData JSON to avoid clobbering fields the caller didn't
/// know about. This closes that gap the same way <c>get_place</c> (Prose.Mcp/Tools.cs) does —
/// same <c>DistrictRepository.GetByName</c> read, just reachable when the MCP server is down.
/// Mirrors <c>get_place</c>'s output shape 1:1 so a saved <c>--print-raw</c> blob round-trips
/// straight into <c>--add-place --file</c> after editing.
/// </summary>
public static class GetPlaceCli
{
    public static Task<int> RunAsync(string[] args, IServiceProvider sp)
    {
        var name = ArgValue(args, "--name");
        if (string.IsNullOrWhiteSpace(name))
        {
            Console.Error.WriteLine("usage: prose --get-place --name \"<exact name>\" [--print-raw]");
            return Task.FromResult(1);
        }

        var repo = sp.GetRequiredService<DistrictRepository>();
        var place = repo.GetByName(name);
        if (place == null)
        {
            Console.Error.WriteLine($"[get-place] not found: \"{name}\"");
            return Task.FromResult(1);
        }

        var json = JsonSerializer.Serialize(place, new JsonSerializerOptions { WriteIndented = true });
        if (!args.Contains("--print-raw"))
            Console.WriteLine($"[get-place] id={place.Id} name=\"{place.Name}\"");
        Console.WriteLine(json);
        return Task.FromResult(0);
    }

    private static string? ArgValue(string[] args, string flag)
    {
        var i = Array.IndexOf(args, flag);
        return (i >= 0 && i + 1 < args.Length) ? args[i + 1] : null;
    }
}
