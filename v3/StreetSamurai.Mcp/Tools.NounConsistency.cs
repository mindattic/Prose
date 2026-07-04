using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Noun consistency tools ─────────────────────────────────────────────────
// validate_nouns        — scan a node's beats for deprecated noun references
// add_deprecated_name   — register a deprecated name → canonical name rule
// list_deprecated_names — list all registered rules

/// <summary>
/// Noun consistency: ensures named things (characters, drones, job titles,
/// places, etc.) appear in prose under their canonical name only.
/// Old/deprecated names are registered with <c>add_deprecated_name</c> and
/// flagged by <c>validate_nouns</c>. Rules are universe-scoped.
/// </summary>
[McpServerToolType]
public class NounConsistencyTools(
    NounConsistencyService nounConsistency,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    /// <summary>Scan a node's prose beats for deprecated or renamed noun references.</summary>
    [McpServerTool, Description(
        "Scan a node's prose beats for deprecated or renamed noun references. " +
        "Returns ok:true when clean; ok:false with a violations list (beatNumber, " +
        "deprecatedName, canonicalName, snippet) when stale names are found. " +
        "Register rules first with add_deprecated_name.")]
    public async Task<string> ValidateNouns(
        [Description("Node slug or GUID to scan.")] string nodeIdOrSlug)
    {
        NounConsistencyReport report;
        try
        {
            report = Guid.TryParse(nodeIdOrSlug, out var id)
                ? await nounConsistency.ValidateAsync(id)
                : await nounConsistency.ValidateSlugAsync(nodeIdOrSlug);
        }
        catch (InvalidOperationException ex)
        {
            return JsonSerializer.Serialize(new { ok = false, error = ex.Message }, JsonOpts);
        }

        return JsonSerializer.Serialize(new
        {
            ok         = report.IsClean,
            nodeTitle  = report.NodeTitle,
            beatCount  = report.BeatCount,
            violations = report.Violations.Select(v => new
            {
                beatNumber     = v.BeatNumber,
                deprecatedName = v.DeprecatedName,
                canonicalName  = v.CanonicalName,
                snippet        = v.Snippet,
            }),
        }, JsonOpts);
    }

    /// <summary>Register a deprecated noun reference rule.</summary>
    [McpServerTool, Description(
        "Register a deprecated noun rule. Any beat that contains 'deprecatedName' " +
        "(whole-word, case-insensitive) in the target universe will be flagged by " +
        "validate_nouns. Use when a named thing is renamed or retired. " +
        "universeSlug defaults to 'glmz' when omitted.")]
    public async Task<string> AddDeprecatedName(
        [Description("The old/wrong name to flag in prose (e.g. 'VacCell', 'Rider').")] string deprecatedName,
        [Description("The correct name to use instead (e.g. 'Nit', 'Exo').")] string canonicalName,
        [Description("Optional explanation (e.g. 'Renamed in SS-A38 when Rider job was retired').")] string? notes = null,
        [Description("Universe slug ('glmz' or 'fantasy'). Defaults to 'glmz'.")] string? universeSlug = null)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var targetSlug = (universeSlug ?? "glmz").ToLowerInvariant();
        var universe = await db.Universes.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Slug == targetSlug);
        if (universe == null)
            return JsonSerializer.Serialize(new { ok = false, error = $"Universe '{targetSlug}' not found." }, JsonOpts);

        var rule = await nounConsistency.AddRuleAsync(universe.Id, deprecatedName, canonicalName, notes);
        return JsonSerializer.Serialize(new
        {
            ok             = true,
            id             = rule.Id,
            deprecatedName = rule.DeprecatedName,
            canonicalName  = rule.CanonicalName,
            universe       = targetSlug,
        }, JsonOpts);
    }

    /// <summary>List all registered deprecated noun rules.</summary>
    [McpServerTool, Description(
        "List all registered deprecated noun rules. Filter by universeSlug ('glmz' or 'fantasy') " +
        "or omit for all universes.")]
    public async Task<string> ListDeprecatedNames(
        [Description("Optional universe slug to filter ('glmz' or 'fantasy'). Omit for all.")] string? universeSlug = null)
    {
        Guid? universeId = null;
        if (!string.IsNullOrWhiteSpace(universeSlug))
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var u = await db.Universes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == universeSlug.ToLowerInvariant());
            if (u == null)
                return JsonSerializer.Serialize(new { ok = false, error = $"Universe '{universeSlug}' not found." }, JsonOpts);
            universeId = u.Id;
        }

        var rules = await nounConsistency.ListRulesAsync(universeId);
        return JsonSerializer.Serialize(new
        {
            ok    = true,
            count = rules.Count,
            rules = rules.Select(r => new
            {
                id             = r.Id,
                deprecatedName = r.DeprecatedName,
                canonicalName  = r.CanonicalName,
                notes          = r.Notes,
                entityName     = r.Entity?.Name,
                addedAt        = r.AddedAt,
            }),
        }, JsonOpts);
    }
}
