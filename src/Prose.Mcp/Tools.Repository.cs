using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

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
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly HubInvoker hub;

    public RepositoryTools(RepositoryDefinitionService repos, IDbContextFactory<ProseDbContext> dbFactory, HubInvoker hub)
    {
        this.repos = repos;
        this.dbFactory = dbFactory;
        this.hub = hub;
    }

    [McpServerTool, Description("List all runtime-defined repositories (custom entity types): slug, name, category, route.")]
    public Task<string> ListRepositories() =>
        hub.InvokeAsync(nameof(RepositoryTools), nameof(ListRepositoriesImpl), new { });

    public string ListRepositoriesImpl()
        => JsonSerializer.Serialize(
            repos.List().Select(r => new { slug = r.Slug, name = r.Name, category = r.Category, icon = r.Icon, route = r.RoutePath }),
            JsonOpts);

    [McpServerTool, Description("Create a new repository (custom entity type). The slug is derived from the name (lowercased, hyphenated) and must be unique. Category is one of Characters/Organizations/Gear/World/Culture (defaults to World). Returns the created slug + route, or an error if the slug already exists.")]
    public Task<string> CreateRepository(
        [Description("Display name, e.g. 'Artifacts'. The slug is derived from this.")] string name,
        [Description("Board category: Characters, Organizations, Gear, World, or Culture. Defaults to World.")] string? category = null,
        [Description("Bootstrap-icon class for the tile, e.g. 'bi-box'. Optional.")] string? icon = null,
        [Description("Optional description of what this repository holds.")] string? description = null) =>
        hub.InvokeAsync(nameof(RepositoryTools), nameof(CreateRepositoryImpl), new { name, category, icon, description });

    public string CreateRepositoryImpl(
        string name,
        string? category = null,
        string? icon = null,
        string? description = null)
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

    [McpServerTool, Description(
        "Browse entities by repository/type (built-in - character, place, faction, weapon, " +
        "corponation, ... - or a custom one from create_repository) without hand-written SQL. " +
        "Omit type to list every repository type present in the current universe with counts.")]
    public Task<string> BrowseRepository(
        [Description("EntityType/repository slug to browse, e.g. 'character'. Omit to list types.")] string? type = null,
        [Description("Optional free-text filter over Name/Description.")] string? search = null,
        [Description("1-based page number (default 1).")] int page = 1,
        [Description("Rows per page, 1-200 (default 25).")] int pageSize = 25) =>
        hub.InvokeAsync(nameof(RepositoryTools), nameof(BrowseRepositoryImpl), new { type, search, page, pageSize });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> BrowseRepositoryImpl(string? type, string? search, int page, int pageSize)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        pageSize = Math.Clamp(pageSize, 1, 200);
        page = Math.Max(1, page);

        if (string.IsNullOrWhiteSpace(type))
        {
            var types = await db.Entities.AsNoTracking()
                .GroupBy(e => e.EntityType)
                .Select(g => new { type = g.Key, count = g.Count() })
                .OrderBy(x => x.type)
                .ToListAsync();
            return JsonSerializer.Serialize(types, JsonOpts);
        }

        var query = db.Entities.AsNoTracking().Where(e => e.EntityType == type);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Name.Contains(search) || (e.Description != null && e.Description.Contains(search)));

        var total = await query.CountAsync();
        var rows = await query.OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new { e.Id, e.Name, e.Slug, e.Status, e.Description })
            .ToListAsync();
        return JsonSerializer.Serialize(new { total, page, pageSize, rows }, JsonOpts);
    }
}
