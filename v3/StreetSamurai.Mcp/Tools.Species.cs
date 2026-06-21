using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

/// <summary>
/// Species lookup tools — read-only taxonomy for sentient life.
/// The five GLMZ values are the canonical set; other universes may extend it.
/// Non-sentient machines (Automata) are NOT species.
/// </summary>
[McpServerToolType]
public class SpeciesTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly SpeciesRepository repo;

    public SpeciesTools(SpeciesRepository repo) => this.repo = repo;

    [McpServerTool, Description(
        "List all species in the current universe. Returns canonical name (key used on " +
        "Character.Species), label, and sentient flag. The five GLMZ values are: " +
        "human, ai, elf, synthetic, unknown.")]
    public string ListSpecies()
    {
        var list = repo.GetAll()
            .Select(s => new { name = s.Name, label = s.Label, sentient = s.Sentient })
            .ToList();
        return JsonSerializer.Serialize(list, JsonOpts);
    }

    [McpServerTool, Description(
        "Get the full record for one species by canonical name (e.g. 'ai', 'elf', 'synthetic'). " +
        "Returns name, label, description, examples, and sentient flag. " +
        "Returns {error: not_found} when the name doesn't match.")]
    public string GetSpecies([Description("Canonical species name, e.g. 'human' or 'elf'.")] string name)
    {
        var s = repo.GetByName(name);
        if (s == null) return JsonSerializer.Serialize(new { error = "not_found", name }, JsonOpts);
        return JsonSerializer.Serialize(new
        {
            name    = s.Name,
            label   = s.Label,
            description = s.Description,
            examples    = s.Examples,
            sentient    = s.Sentient,
        }, JsonOpts);
    }
}
