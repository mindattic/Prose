using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Prose.Core.Services;

namespace Prose.Mcp;

/// <summary>
/// Portable-writing-service plan, Phase 4 — <c>export_barks</c>: walk a universe's (or one
/// book/chapter's) beats and return every beat with a single recorded POV speaker as
/// <c>{barkId, speakerEntitySlug, text, context}</c>. See BarksExportService's doc comment for
/// the full rationale.
/// </summary>
[McpServerToolType]
public class BarksExportTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly BarksExportService export;
    private readonly HubInvoker hub;

    public BarksExportTools(BarksExportService export, HubInvoker hub)
    {
        this.export = export;
        this.hub = hub;
    }

    [McpServerTool, Description("Walk a universe's (or one book/chapter's) beats and return every beat with a single " +
        "recorded POV speaker as {barkId, speakerEntitySlug, text, context}. Beats with no recorded POV are skipped " +
        "and counted, never silently dropped.")]
    public Task<string> ExportBarks(
        [Description("Universe slug, e.g. 'eve'.")] string universe,
        [Description("Optional slug/NodeCode/GUID of one book or chapter to scope the walk to. Omit for the whole universe.")] string? node = null) =>
        hub.InvokeAsync(nameof(BarksExportTools), nameof(ExportBarksImpl), new { universe, node });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ExportBarksImpl(string universe, string? node)
    {
        try
        {
            var result = await export.ExportAsync(universe, node);
            return JsonSerializer.Serialize(new
            {
                universe = result.UniverseSlug,
                skipped = result.Skipped,
                barks = result.Barks,
            }, JsonOpts);
        }
        catch (InvalidOperationException ex)
        {
            return JsonSerializer.Serialize(new { error = "barks_export_failed", detail = ex.Message }, JsonOpts);
        }
    }
}
