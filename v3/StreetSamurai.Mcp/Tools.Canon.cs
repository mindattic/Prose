using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Canon document tools (Track A — Truth-First Architecture) ─────────────────
// Structured canon editing: every world-level .md file has a DB source of truth.
// Edits go through set_canon_section / set_story_bible_section; the .md artifacts
// are regenerated on demand and NEVER hand-edited.
//
// Document types: WorldBible, WorldMaster, Franchise, UniverseCanon
// Bible section types: Full, ArcSummary, Characters, VoiceRegister, NarrativeLocks, BeatSpine

[McpServerToolType]
public class CanonDocTools
{
    private readonly CanonDocumentService canonDocs;
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly NodeDocService nodeDoc;

    public CanonDocTools(
        CanonDocumentService canonDocs,
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        NodeDocService nodeDoc)
    {
        this.canonDocs = canonDocs;
        this.dbFactory = dbFactory;
        this.nodeDoc   = nodeDoc;
    }

    // ── World-level canon ─────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Get a full world-canon document assembled from its DB sections. " +
        "documentType: WorldBible | WorldMaster | Franchise | UniverseCanon. " +
        "universeSlug: glmz | scry/fantasy/caul (or a universe GUID). " +
        "Returns the complete assembled markdown — same content that generate_canon_md would write to disk.")]
    public async Task<string> GetCanonDocument(
        [Description("Document type: WorldBible, WorldMaster, Franchise, or UniverseCanon.")] string documentType,
        [Description("Universe slug: glmz, scry/caul/fantasy, or universe GUID. Defaults to glmz.")] string universeSlug = "glmz")
    {
        var universeId = CanonDocumentService.ResolveUniverseId(universeSlug);
        if (universeId == null)
            return JsonSerializer.Serialize(new { error = "unknown_universe", universeSlug }, CanonTools.JsonOpts);

        var doc = await canonDocs.GetDocumentAsync(documentType, universeId.Value);
        if (doc == null)
            return JsonSerializer.Serialize(new { error = "document_not_found", documentType, universeSlug,
                hint = "Run ss --migrate-canon-docs first to seed the DB from existing .md files." },
                CanonTools.JsonOpts);

        return doc;
    }

    [McpServerTool, Description(
        "List all sections in a world-canon document with their keys, titles, sort order, and last-updated times. " +
        "Use this to find the sectionKey you need before calling set_canon_section.")]
    public async Task<string> ListCanonSections(
        [Description("Document type: WorldBible, WorldMaster, Franchise, or UniverseCanon.")] string documentType,
        [Description("Universe slug: glmz, scry/caul/fantasy, or universe GUID. Defaults to glmz.")] string universeSlug = "glmz")
    {
        var universeId = CanonDocumentService.ResolveUniverseId(universeSlug);
        if (universeId == null)
            return JsonSerializer.Serialize(new { error = "unknown_universe", universeSlug }, CanonTools.JsonOpts);

        await using var db = await dbFactory.CreateDbContextAsync();
        var doc = await db.CanonDocuments
            .Include(d => d.Sections.OrderBy(s => s.SortKey))
            .FirstOrDefaultAsync(d => d.UniverseId == universeId.Value && d.DocumentType == documentType);

        if (doc == null)
            return JsonSerializer.Serialize(new { error = "document_not_found", documentType, universeSlug }, CanonTools.JsonOpts);

        var sections = doc.Sections.Select(s => new
        {
            sort_key      = s.SortKey,
            section_key   = s.SectionKey,
            section_title = s.SectionTitle,
            content_length = s.Content.Length,
            updated_at    = s.UpdatedAt,
        }).ToList();

        return JsonSerializer.Serialize(new { document_type = documentType, title = doc.Title,
            section_count = sections.Count, sections }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Update or create a section in a world-canon document. This is the ONLY way to edit world canon — " +
        "do NOT hand-edit docs/BIBLE.md, docs/WORLD.md, docs/FRANCHISE.md, or docs/universes/CAUL.md. " +
        "After setting a section, call generate_canon_md to write the updated .md artifact to disk. " +
        "To find available sectionKeys, call list_canon_sections first.")]
    public async Task<string> SetCanonSection(
        [Description("Document type: WorldBible, WorldMaster, Franchise, or UniverseCanon.")] string documentType,
        [Description("Stable section key — e.g. 'SS-LAW-1', 'SS-§3', 'preamble'. Use list_canon_sections to find existing keys.")] string sectionKey,
        [Description("Full section content (markdown). Replaces the existing content for this key.")] string content,
        [Description("Universe slug: glmz, scry/caul/fantasy, or universe GUID. Defaults to glmz.")] string universeSlug = "glmz",
        [Description("Optional: human-readable section title (the ## heading text). Leave blank to keep the existing title.")] string? sectionTitle = null)
    {
        var universeId = CanonDocumentService.ResolveUniverseId(universeSlug);
        if (universeId == null)
            return JsonSerializer.Serialize(new { error = "unknown_universe", universeSlug }, CanonTools.JsonOpts);

        var result = await canonDocs.UpsertSectionAsync(documentType, universeId.Value, sectionKey, content, sectionTitle);

        if (!result.Ok)
            return JsonSerializer.Serialize(new { error = result.Error, message = result.ErrorMessage }, CanonTools.JsonOpts);

        return JsonSerializer.Serialize(new
        {
            ok           = true,
            action       = result.Action,
            section_key  = result.SectionKey,
            document_type = documentType,
            next_step    = $"Call generate_canon_md(documentType='{documentType}', universeSlug='{universeSlug}') to write the updated .md to disk.",
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Regenerate a world-canon .md file from its DB sections. Writes the assembled content to disk and " +
        "updates the LastChecksum so codex doctor validates the file as current. " +
        "Run this after every set_canon_section call.")]
    public async Task<string> GenerateCanonMd(
        [Description("Document type: WorldBible, WorldMaster, Franchise, or UniverseCanon.")] string documentType,
        [Description("Universe slug: glmz, scry/caul/fantasy, or universe GUID. Defaults to glmz.")] string universeSlug = "glmz")
    {
        var universeId = CanonDocumentService.ResolveUniverseId(universeSlug);
        if (universeId == null)
            return JsonSerializer.Serialize(new { error = "unknown_universe", universeSlug }, CanonTools.JsonOpts);

        var result = await canonDocs.GenerateMdAsync(documentType, universeId.Value);

        if (!result.Ok)
            return JsonSerializer.Serialize(new { error = result.Error, message = result.ErrorMessage }, CanonTools.JsonOpts);

        return JsonSerializer.Serialize(new
        {
            ok            = true,
            file_path     = result.FilePath,
            section_count = result.SectionCount,
            checksum      = result.Checksum,
        }, CanonTools.JsonOpts);
    }

    // ── NodeBibleSections ─────────────────────────────────────────────────────

    [McpServerTool, Description(
        "Update or create a structured section in a story's node bible (NodeBibleSections table). " +
        "sectionType: Full | ArcSummary | Characters | VoiceRegister | NarrativeLocks | BeatSpine. " +
        "Use 'Full' to replace the entire hand-authored bible blob; use typed sections to maintain " +
        "structured per-category content. After calling this, run generate_node_doc to refresh the " +
        "docs/nodes/<CODE>.md artifact and then ss --sync-markdown so DocContextService picks it up.")]
    public async Task<string> SetStoryBibleSection(
        [Description("Node id (GUID), slug, or NodeCode.")] string nodeIdOrSlug,
        [Description("Section type: Full, ArcSummary, Characters, VoiceRegister, NarrativeLocks, or BeatSpine.")] string sectionType,
        [Description("Section content (markdown). Replaces any existing content for this sectionType.")] string content)
    {
        var nodeId = await ResolveNodeIdAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);

        var result = await canonDocs.SetNodeBibleSectionAsync(nodeId.Value, sectionType, content);

        if (!result.Ok)
            return JsonSerializer.Serialize(new { error = result.Error, message = result.ErrorMessage }, CanonTools.JsonOpts);

        return JsonSerializer.Serialize(new
        {
            ok           = true,
            action       = result.Action,
            section_type = result.SectionKey,
            node_id      = nodeId,
            next_steps   = new[]
            {
                $"generate_node_doc(nodeIdOrSlug='{nodeIdOrSlug}') — regenerate docs/nodes/<CODE>.md",
                "ss --sync-markdown — sync the .md to MarkdownFiles table for DocContextService",
            },
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "List all NodeBibleSections for a story node. Shows section types, content lengths, and last-updated timestamps. " +
        "Use this to see which typed sections exist before calling set_story_bible_section.")]
    public async Task<string> ListStoryBibleSections(
        [Description("Node id (GUID), slug, or NodeCode.")] string nodeIdOrSlug)
    {
        var nodeId = await ResolveNodeIdAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);

        var sections = await canonDocs.GetNodeBibleSectionsAsync(nodeId.Value);

        return JsonSerializer.Serialize(new
        {
            node_id      = nodeId,
            section_count = sections.Count,
            sections     = sections.Select(s => new
            {
                section_type   = s.SectionType,
                content_length = s.Content.Length,
                updated_at     = s.UpdatedAt,
            }).ToList(),
        }, CanonTools.JsonOpts);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid?> ResolveNodeIdAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out var guid)) return guid;

        await using var db = await dbFactory.CreateDbContextAsync();
        var id = await db.Nodes
            .Where(n => n.Slug == idOrSlug || n.NodeCode == idOrSlug)
            .Select(n => (Guid?)n.Id)
            .FirstOrDefaultAsync();
        return id;
    }
}
