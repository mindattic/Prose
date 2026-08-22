using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services;
using Prose.Core.Data.Entities;

namespace Prose.Mcp;

// ── Multi-universe tools ─────────────────────────────────────────────────────
// Select which universe (GLMZ, Scry, …) this MCP session targets.
// All canon/book reads through the other tools are scoped to the current
// universe (SS-LAW-15). The selection is per-process, so a session launched with
// `--universe scry` (or PROSE_UNIVERSE) is isolated from other clients.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Tools to inspect and switch the active universe. Switching changes what every
/// other canon/book tool returns for the rest of this session.
/// </summary>
[McpServerToolType]
public class UniverseTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly IUniverseContext universe;
    private readonly UniversalFactsService universalFacts;
    private readonly HubInvoker hub;

    public UniverseTools(IUniverseContext universe, UniversalFactsService universalFacts, HubInvoker hub)
    {
        this.universe = universe;
        this.universalFacts = universalFacts;
        this.hub = hub;
    }

    [McpServerTool, Description("List every registered universe (slug, name, theme) and which one is currently active. Call this first to discover universe slugs before switch_universe.")]
    public Task<string> ListUniverses() =>
        hub.InvokeAsync(nameof(UniverseTools), nameof(ListUniversesImpl));

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string ListUniversesImpl()
    {
        var current = universe.CurrentSlug;
        var list = universe.ListUniverses()
            .Select(u => new { slug = u.Slug, name = u.Name, theme = u.Theme, isActive = u.IsActive, current = u.Slug == current })
            .ToList();
        return JsonSerializer.Serialize(new { current, universes = list }, JsonOpts);
    }

    [McpServerTool, Description("Switch the active universe for this session by slug (e.g. 'glmz' or 'scry'). All subsequent canon/story reads are scoped to it. Returns the new current universe or an error if the slug is unknown.")]
    public Task<string> SwitchUniverse([Description("Universe slug from list_universes, e.g. 'glmz'.")] string slug) =>
        hub.InvokeAsync(nameof(UniverseTools), nameof(SwitchUniverseImpl), new { slug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string SwitchUniverseImpl(string slug)
    {
        if (!universe.UseUniverseBySlug(slug))
            return JsonSerializer.Serialize(new { error = "unknown_universe", slug, hint = "call list_universes for valid slugs" }, JsonOpts);
        return JsonSerializer.Serialize(new { ok = true, current = universe.CurrentSlug }, JsonOpts);
    }

    [McpServerTool, Description("Return the universe currently active for this session (slug + name).")]
    public Task<string> CurrentUniverse() =>
        hub.InvokeAsync(nameof(UniverseTools), nameof(CurrentUniverseImpl));

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public string CurrentUniverseImpl()
    {
        var u = universe.CurrentUniverse;
        return JsonSerializer.Serialize(new { slug = universe.CurrentSlug, name = u?.Name, theme = u?.Theme }, JsonOpts);
    }

    [McpServerTool, Description("Return the universal world facts for the current universe — world mechanics, vocabulary, and social rules injected into every beat generation prompt. These apply to all books in the universe. Book-specific facts live in each book's node bible instead.")]
    public Task<string> GetUniversalFacts() =>
        hub.InvokeAsync(nameof(UniverseTools), nameof(GetUniversalFactsImpl));

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GetUniversalFactsImpl()
    {
        var facts = await universalFacts.GetWorldFactsAsync();
        var u = universe.CurrentUniverse;
        return JsonSerializer.Serialize(new
        {
            universe = universe.CurrentSlug,
            universeName = u?.Name,
            hasWorldFacts = !string.IsNullOrWhiteSpace(facts),
            worldFacts = facts
        }, JsonOpts);
    }

    [McpServerTool, Description("Set the universal world facts for the current universe. These facts are injected into every beat generation prompt for any book in this universe, so they should cover mechanics and vocabulary that apply everywhere (transport, technology, social structure, prose vocabulary). Book-specific content belongs in the book's node bible, not here.")]
    public Task<string> SetUniversalFacts(
        [Description("The full world facts text in Markdown. Replaces any existing content. Pass empty string to clear.")] string facts) =>
        hub.InvokeAsync(nameof(UniverseTools), nameof(SetUniversalFactsImpl), new { facts });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> SetUniversalFactsImpl(string facts)
    {
        await universalFacts.SetWorldFactsAsync(facts);
        return JsonSerializer.Serialize(new { ok = true, universe = universe.CurrentSlug, length = facts.Length }, JsonOpts);
    }
}
