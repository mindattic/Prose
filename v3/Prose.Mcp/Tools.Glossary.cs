using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Master Glossary tools — universe-scoped back-matter glossaries ────────
// A term defined here never needs an in-voice gloss on the page (SS-LAW-20 is satisfied by
// back-matter reference instead of prose interruption). generate_glossary regenerates the
// universe's Glossary.htm/.json/.txt; generate_book_glossary regenerates one book's subset
// (only the terms that actually appear in its live prose).

[McpServerToolType]
public class GlossaryTools
{
    private readonly GlossaryService glossary;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;

    public GlossaryTools(GlossaryService glossary, IDbContextFactory<ProseDbContext> dbFactory)
    {
        this.glossary = glossary;
        this.dbFactory = dbFactory;
    }

    [McpServerTool, Description(
        "Add or update one Master Glossary entry for the current universe. term is the word/acronym " +
        "as it appears in prose (e.g. 'GLMZ'); fullForm is its expansion if it's an acronym (e.g. " +
        "'Great Lakes Metropolitan Zone'), empty for plain vocabulary; definition is the reader-facing " +
        "back-matter explanation (can carry more context than an in-voice gloss would); category groups " +
        "entries in the rendered glossary (e.g. 'Enforcement', 'Currency', 'Tech'). Upserts by " +
        "(universe, term) — case-sensitive exact match, calling again with the same term overwrites it.")]
    public async Task<string> UpsertGlossaryTerm(
        [Description("The term/acronym as it appears in prose.")] string term,
        [Description("Full expansion if an acronym; empty for plain vocabulary.")] string fullForm,
        [Description("Reader-facing definition shown in the glossary.")] string definition,
        [Description("Optional grouping category (e.g. 'Enforcement', 'Currency').")] string category = "")
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var universeId = Prose.Core.Services.UniverseScope.EffectiveId;
        if (universeId == Guid.Empty)
            return JsonSerializer.Serialize(new { error = "no_universe_scope" }, CanonTools.JsonOpts);

        var row = await glossary.UpsertAsync(
            universeId, term, string.IsNullOrWhiteSpace(fullForm) ? null : fullForm,
            definition, string.IsNullOrWhiteSpace(category) ? null : category);
        return JsonSerializer.Serialize(new { ok = true, id = row.Id, term = row.Term }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "List every Master Glossary entry for the current universe, grouped by category.")]
    public async Task<string> ListGlossaryTerms()
    {
        var universeId = Prose.Core.Services.UniverseScope.EffectiveId;
        if (universeId == Guid.Empty)
            return JsonSerializer.Serialize(new { error = "no_universe_scope" }, CanonTools.JsonOpts);

        var terms = await glossary.ListAsync(universeId);
        return JsonSerializer.Serialize(new
        {
            count = terms.Count,
            terms = terms.Select(t => new { term = t.Term, full_form = t.FullForm, definition = t.Definition, category = t.Category }),
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Regenerate the current universe's Master Glossary — Glossary.htm/.json/.txt under " +
        "docs/universes/{SLUG}/ — from the GlossaryTerms table. Run after upsert_glossary_term calls.")]
    public async Task<string> GenerateGlossary()
    {
        var universeId = Prose.Core.Services.UniverseScope.EffectiveId;
        if (universeId == Guid.Empty)
            return JsonSerializer.Serialize(new { error = "no_universe_scope" }, CanonTools.JsonOpts);

        var result = await glossary.GenerateMasterAsync(universeId);
        return JsonSerializer.Serialize(new
        {
            universe = result.UniverseSlug, term_count = result.TermCount,
            html_path = result.HtmlPath, json_path = result.JsonPath, txt_path = result.TxtPath,
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Regenerate one book's Glossary (docs/nodes/{CODE}-Glossary.htm/.json/.txt) — the subset " +
        "of its universe's Master Glossary whose terms actually appear in the book's live prose, " +
        "detected fresh each call (not a stored join). A term the book stops using drops out on " +
        "the next regenerate; a term added to the universe glossary after the book's last edit " +
        "picks up automatically.")]
    public async Task<string> GenerateBookGlossary(
        [Description("Node Guid id or slug/NodeCode of the book.")] string idOrSlug)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var node = Guid.TryParse(idOrSlug, out var gid)
            ? await db.Nodes.FirstOrDefaultAsync(n => n.Id == gid)
            : await db.Nodes.FirstOrDefaultAsync(n => n.Slug == idOrSlug || n.NodeCode == idOrSlug);
        if (node == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", idOrSlug }, CanonTools.JsonOpts);

        var result = await glossary.GenerateForBookAsync(node.Id);
        return JsonSerializer.Serialize(new
        {
            node_code = result.NodeCode, term_count = result.TermCount, universe_term_count = result.UniverseTermCount,
            html_path = result.HtmlPath, json_path = result.JsonPath, txt_path = result.TxtPath,
        }, CanonTools.JsonOpts);
    }
}
