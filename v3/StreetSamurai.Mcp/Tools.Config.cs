using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Markdown file backup / restore tools ─────────────────────────────────
// Surfaces the MarkdownFileService so Claude Code can sync project-rules,
// Codex docs, and memory files to the DB and restore them by timestamp.
// ─────────────────────────────────────────────────────────────────────────

[McpServerToolType]
public class ConfigTools
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    private readonly MarkdownFileService svc;

    public ConfigTools(MarkdownFileService svc) => this.svc = svc;

    [McpServerTool, Description(
        "List all markdown files tracked in the database (project rules, Codex docs, Claude Code memory). " +
        "Returns category, relativePath, contentHash, and lastSyncedAt for each file.")]
    public async Task<string> ListMarkdownFiles()
    {
        var rows = await svc.ListAsync();
        var result = rows.Select(r => new
        {
            category     = r.Category,
            relativePath = r.RelativePath,
            fileRoot     = r.FileRoot,
            hash         = r.ContentHash.Length >= 12 ? r.ContentHash[..12] + "…" : r.ContentHash,
            lastSynced   = r.LastSyncedAt.ToString("u"),
        }).ToList();
        return JsonSerializer.Serialize(new { count = result.Count, files = result }, JsonOpts);
    }

    [McpServerTool, Description(
        "Get the content of a tracked markdown file from the database. " +
        "Pass asOf (ISO 8601 UTC) to retrieve a historical version from the temporal table. " +
        "relativePath examples: 'CLAUDE.md', 'docs/BIBLE.md', 'feedback_sequential_strand_writing.md'")]
    public async Task<string> GetMarkdownFile(
        [Description("Relative path key, e.g. 'CLAUDE.md' or 'docs/AMENDMENTS.md'.")] string relativePath,
        [Description("Optional ISO 8601 UTC datetime to retrieve the version current at that moment.")] string? asOf = null)
    {
        DateTime? asOfDt = null;
        if (asOf != null)
        {
            if (!DateTime.TryParse(asOf, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                return JsonSerializer.Serialize(new { error = "invalid_as_of", hint = "use ISO 8601, e.g. 2026-06-01T00:00:00Z" }, JsonOpts);
            asOfDt = parsed.ToUniversalTime();
        }

        var row = await svc.GetAsync(relativePath, asOfDt);
        if (row == null)
            return JsonSerializer.Serialize(new { error = "not_found", relativePath }, JsonOpts);

        return JsonSerializer.Serialize(new
        {
            relativePath = row.RelativePath,
            category     = row.Category,
            fileRoot     = row.FileRoot,
            lastSynced   = row.LastSyncedAt.ToString("u"),
            content      = row.Content,
        }, JsonOpts);
    }

    [McpServerTool, Description(
        "Sync all discovered markdown files from disk into the database. " +
        "Only files whose content hash changed produce a new history row. " +
        "Pass dryRun=true to preview without writing.")]
    public async Task<string> SyncMarkdownFiles(
        [Description("If true, report what would be synced without writing to the database.")] bool dryRun = false)
    {
        var discovered = svc.DiscoverFiles().ToList();
        var result = await svc.SyncAllAsync(dryRun);
        return JsonSerializer.Serialize(new
        {
            dryRun,
            discovered = discovered.Count,
            inserted   = result.Inserted,
            updated    = result.Updated,
            unchanged  = result.Unchanged,
            errors     = result.Errors,
        }, JsonOpts);
    }

    [McpServerTool, Description(
        "Restore markdown files from the database back to disk. " +
        "Pass relativePath to restore a single file; omit to restore all tracked files. " +
        "Pass asOf (ISO 8601 UTC) to recover a historical version from the temporal table. " +
        "Pass dryRun=true to preview without writing to disk.")]
    public async Task<string> RestoreMarkdownFile(
        [Description("Relative path of the file to restore, e.g. 'CLAUDE.md'. Omit to restore all.")] string? relativePath = null,
        [Description("Optional ISO 8601 UTC datetime for point-in-time recovery.")] string? asOf = null,
        [Description("If true, report what would be written without touching the filesystem.")] bool dryRun = false)
    {
        DateTime? asOfDt = null;
        if (asOf != null)
        {
            if (!DateTime.TryParse(asOf, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
                return JsonSerializer.Serialize(new { error = "invalid_as_of", hint = "use ISO 8601, e.g. 2026-06-01T00:00:00Z" }, JsonOpts);
            asOfDt = parsed.ToUniversalTime();
        }

        var result = await svc.RestoreAsync(relativePath, asOfDt, dryRun);
        return JsonSerializer.Serialize(new
        {
            dryRun,
            relativePath,
            asOf    = asOfDt?.ToString("u"),
            written = result.Written,
            skipped = result.Skipped,
            errors  = result.Errors,
        }, JsonOpts);
    }
}
