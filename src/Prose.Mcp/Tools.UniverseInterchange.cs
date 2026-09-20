using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.Mcp;

/// <summary>
/// RFC 0007 "Universe Interchange" — import/export between an app's
/// <c>&lt;app&gt;/universe/&lt;slug&gt;.universe.json</c> contract file and Prose's Entity
/// spine, plus cross-universe lookups that don't require switching the session's active
/// universe (unlike the generic CreateX/entity tools, which all operate on the ambient
/// current universe via IUniverseContext).
/// </summary>
[McpServerToolType]
public class UniverseInterchangeTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly UniverseInterchangeService interchange;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly HubInvoker hub;

    public UniverseInterchangeTools(UniverseInterchangeService interchange, IDbContextFactory<ProseDbContext> dbFactory, HubInvoker hub)
    {
        this.interchange = interchange;
        this.dbFactory = dbFactory;
        this.hub = hub;
    }

    [McpServerTool, Description("Import a Universe Interchange JSON file (RFC 0007, docs/schemas/universe.schema.json) into Prose's Entity spine. Universe slug defaults to the file's own universe.id. Idempotent — re-importing the same file is a no-op diff.")]
    public Task<string> ImportUniverseFile(
        [Description("Absolute path to the <slug>.universe.json file.")] string path,
        [Description("Optional universe slug override; defaults to the file's own universe.id.")] string? slug = null) =>
        hub.InvokeAsync(nameof(UniverseInterchangeTools), nameof(ImportUniverseFileImpl), new { path, slug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ImportUniverseFileImpl(string path, string? slug)
    {
        if (!File.Exists(path))
            return JsonSerializer.Serialize(new { error = "file_not_found", path }, JsonOpts);
        var json = await File.ReadAllTextAsync(path);
        var result = await interchange.ImportAsync(json, slug);
        return JsonSerializer.Serialize(result, JsonOpts);
    }

    [McpServerTool, Description("Export a Prose universe to Universe Interchange JSON (RFC 0007) at the given path.")]
    public Task<string> ExportUniverseFile(
        [Description("Universe slug, e.g. 'eve'.")] string slug,
        [Description("Absolute output path for the exported JSON file.")] string path) =>
        hub.InvokeAsync(nameof(UniverseInterchangeTools), nameof(ExportUniverseFileImpl), new { slug, path });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ExportUniverseFileImpl(string slug, string path)
    {
        try
        {
            await interchange.ExportToFileAsync(slug, path);
            return JsonSerializer.Serialize(new { ok = true, path }, JsonOpts);
        }
        catch (InvalidOperationException ex)
        {
            return JsonSerializer.Serialize(new { error = "export_failed", detail = ex.Message }, JsonOpts);
        }
    }

    [McpServerTool, Description("Look up one entity in a specific universe by slug. Cross-universe — does NOT switch the session's active universe (unlike the generic entity tools).")]
    public Task<string> GetUniverseEntity(
        [Description("Universe slug, e.g. 'eve'.")] string slug,
        [Description("Entity slug within that universe, e.g. 'kat-weiss'.")] string entitySlug) =>
        hub.InvokeAsync(nameof(UniverseInterchangeTools), nameof(GetUniverseEntityImpl), new { slug, entitySlug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GetUniverseEntityImpl(string slug, string entitySlug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var universe = await db.Universes.FirstOrDefaultAsync(u => u.Slug == slug.Trim().ToLowerInvariant());
        if (universe == null)
            return JsonSerializer.Serialize(new { error = "unknown_universe", slug }, JsonOpts);

        var norm = UniverseGraphService.Slugify(entitySlug);
        var entity = await db.Entities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.UniverseId == universe.Id && e.Slug == norm);
        if (entity == null)
            return JsonSerializer.Serialize(new { error = "not_found", entitySlug }, JsonOpts);

        var record = await db.Records.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.EntityId == entity.Id);
        return JsonSerializer.Serialize(new
        {
            id = entity.Slug,
            type = entity.EntityType,
            name = entity.Name,
            status = entity.Status,
            summary = entity.Description,
            record = record?.Json,
        }, JsonOpts);
    }

    [McpServerTool, Description("Search a specific universe's entities by name substring. Cross-universe — does NOT switch the session's active universe.")]
    public Task<string> SearchUniverse(
        [Description("Universe slug, e.g. 'eve'.")] string slug,
        [Description("Name substring to search for.")] string query) =>
        hub.InvokeAsync(nameof(UniverseInterchangeTools), nameof(SearchUniverseImpl), new { slug, query });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> SearchUniverseImpl(string slug, string query)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var universe = await db.Universes.FirstOrDefaultAsync(u => u.Slug == slug.Trim().ToLowerInvariant());
        if (universe == null)
            return JsonSerializer.Serialize(new { error = "unknown_universe", slug }, JsonOpts);

        var q = (query ?? "").ToLowerInvariant();
        var matches = await db.Entities.IgnoreQueryFilters()
            .Where(e => e.UniverseId == universe.Id && e.Status != "stub" && e.Name.ToLower().Contains(q))
            .OrderBy(e => e.Name)
            .Take(50)
            .Select(e => new { id = e.Slug, type = e.EntityType, name = e.Name, summary = e.Description })
            .ToListAsync();
        return JsonSerializer.Serialize(new { count = matches.Count, matches }, JsonOpts);
    }
}
