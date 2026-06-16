using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

/// <summary>
/// Tools to define new repositories (custom entity types) at runtime. A repository is just a named
/// EntityType on the universal Entity spine — no typed table. Definitions are global; entities in
/// each repo are separated by their universe, so an empty repo in a universe is simply not shown.
/// </summary>
[McpServerToolType]
public class RepositoryTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly RepositoryDefinitionService repos;

    public RepositoryTools(RepositoryDefinitionService repos) => this.repos = repos;

    [McpServerTool, Description("List all runtime-defined repositories (custom entity types): slug, name, category, route.")]
    public string ListRepositories()
        => JsonSerializer.Serialize(
            repos.List().Select(r => new { slug = r.Slug, name = r.Name, category = r.Category, icon = r.Icon, route = r.RoutePath }),
            JsonOpts);

    [McpServerTool, Description("Create a new repository (custom entity type). The slug is derived from the name (lowercased, hyphenated) and must be unique. Category is one of Characters/Organizations/Gear/World/Culture (defaults to World). Returns the created slug + route, or an error if the slug already exists.")]
    public string CreateRepository(
        [Description("Display name, e.g. 'Artifacts'. The slug is derived from this.")] string name,
        [Description("Board category: Characters, Organizations, Gear, World, or Culture. Defaults to World.")] string? category = null,
        [Description("Bootstrap-icon class for the tile, e.g. 'bi-box'. Optional.")] string? icon = null,
        [Description("Optional description of what this repository holds.")] string? description = null)
    {
        try
        {
            var def = repos.Create(name, category, icon, description);
            return JsonSerializer.Serialize(new { ok = true, slug = def.Slug, name = def.Name, category = def.Category, route = def.RoutePath }, JsonOpts);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = "create_failed", message = ex.Message }, JsonOpts);
        }
    }
}
