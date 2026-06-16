using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Multi-universe tools ─────────────────────────────────────────────────────
// Select which universe (GLMZ, Fantasy/Steampunk, …) this MCP session targets.
// All canon/story reads through the other tools are scoped to the current
// universe (SS-LAW-15). The selection is per-process, so a session launched with
// `--universe fantasy-steampunk` (or SS_UNIVERSE) is isolated from other clients.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Tools to inspect and switch the active universe. Switching changes what every
/// other canon/story tool returns for the rest of this session.
/// </summary>
[McpServerToolType]
public class UniverseTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly IUniverseContext universe;

    public UniverseTools(IUniverseContext universe) => this.universe = universe;

    [McpServerTool, Description("List every registered universe (slug, name, theme) and which one is currently active. Call this first to discover universe slugs before switch_universe.")]
    public string ListUniverses()
    {
        var current = universe.CurrentSlug;
        var list = universe.ListUniverses()
            .Select(u => new { slug = u.Slug, name = u.Name, theme = u.Theme, isActive = u.IsActive, current = u.Slug == current })
            .ToList();
        return JsonSerializer.Serialize(new { current, universes = list }, JsonOpts);
    }

    [McpServerTool, Description("Switch the active universe for this session by slug (e.g. 'glmz' or 'fantasy-steampunk'). All subsequent canon/story reads are scoped to it. Returns the new current universe or an error if the slug is unknown.")]
    public string SwitchUniverse([Description("Universe slug from list_universes, e.g. 'glmz'.")] string slug)
    {
        if (!universe.UseUniverseBySlug(slug))
            return JsonSerializer.Serialize(new { error = "unknown_universe", slug, hint = "call list_universes for valid slugs" }, JsonOpts);
        return JsonSerializer.Serialize(new { ok = true, current = universe.CurrentSlug }, JsonOpts);
    }

    [McpServerTool, Description("Return the universe currently active for this session (slug + name).")]
    public string CurrentUniverse()
    {
        var u = universe.CurrentUniverse;
        return JsonSerializer.Serialize(new { slug = universe.CurrentSlug, name = u?.Name, theme = u?.Theme }, JsonOpts);
    }
}
