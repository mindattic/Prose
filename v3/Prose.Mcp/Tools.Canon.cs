using System.ComponentModel;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Services;

namespace Prose.Mcp;

// ── Canon document tools (Track A — Truth-First Architecture) ─────────────────
// Structured canon editing: every world-level .md file has a DB source of truth.
// Edits go through set_canon_section / set_book_bible_section; the .md artifacts
// are regenerated on demand and NEVER hand-edited.
//
// Document types are data-driven (CanonDocumentTypes table, not a fixed list) — call
// list_canon_document_types for the current set. Ships with WorldBible, WorldMaster,
// Franchise, UniverseCanon, CraftGuide, DelightGuide.
// Bible section types: Full, ArcSummary, Characters, VoiceRegister, NarrativeLocks, BeatSpine

[McpServerToolType]
public class CanonDocTools
{
    private readonly CanonDocumentService canonDocs;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly NodeDocService nodeDoc;
    private readonly MarkdownFileService markdownFiles;
    private readonly HubInvoker hub;

    public CanonDocTools(
        CanonDocumentService canonDocs,
        IDbContextFactory<ProseDbContext> dbFactory,
        NodeDocService nodeDoc,
        MarkdownFileService markdownFiles,
        HubInvoker hub)
    {
        this.canonDocs     = canonDocs;
        this.dbFactory     = dbFactory;
        this.nodeDoc       = nodeDoc;
        this.markdownFiles = markdownFiles;
        this.hub           = hub;
    }

    // ── World-level canon ─────────────────────────────────────────────────────

    [McpServerTool, Description(
        "List every registered canon DocumentType (e.g. WorldBible, CraftGuide) — the current " +
        "valid values for the documentType parameter on every other tool in this file. Data-driven " +
        "(CanonDocumentTypes table), so this grows as new document types are migrated; don't rely " +
        "on a hardcoded list from memory.")]
    public Task<string> ListCanonDocumentTypes() =>
        hub.InvokeAsync(nameof(CanonDocTools), nameof(ListCanonDocumentTypesImpl), new { });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ListCanonDocumentTypesImpl()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var types = await db.CanonDocumentTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortKey)
            .Select(t => new { document_type = t.DocumentType, scope = t.Scope, path_template = t.PathTemplate })
            .ToListAsync();
        return JsonSerializer.Serialize(new { types }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Get a full world-canon document assembled from its DB sections. " +
        "Call list_canon_document_types for the current valid documentType values. " +
        "universeSlug: glmz | scry/fantasy/caul (or a universe GUID). " +
        "Returns the complete assembled markdown — same content that generate_canon_md would write to disk.")]
    public Task<string> GetCanonDocument(
        [Description("Document type — call list_canon_document_types for the current valid values.")] string documentType,
        [Description("Universe slug: glmz, scry/caul/fantasy, or universe GUID. Defaults to glmz.")] string universeSlug = "glmz") =>
        hub.InvokeAsync(nameof(CanonDocTools), nameof(GetCanonDocumentImpl), new { documentType, universeSlug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GetCanonDocumentImpl(string documentType, string universeSlug = "glmz")
    {
        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null)
            return JsonSerializer.Serialize(new { error = "unknown_universe", universeSlug }, CanonTools.JsonOpts);

        var doc = await canonDocs.GetDocumentAsync(documentType, universeId.Value);
        if (doc == null)
            return JsonSerializer.Serialize(new { error = "document_not_found", documentType, universeSlug,
                hint = "Run prose --migrate-canon-docs first to seed the DB from existing .md files." },
                CanonTools.JsonOpts);

        return doc;
    }

    [McpServerTool, Description(
        "List all sections in a world-canon document with their keys, titles, sort order, and last-updated times. " +
        "Use this to find the sectionKey you need before calling set_canon_section.")]
    public Task<string> ListCanonSections(
        [Description("Document type — call list_canon_document_types for the current valid values.")] string documentType,
        [Description("Universe slug: glmz, scry/caul/fantasy, or universe GUID. Defaults to glmz.")] string universeSlug = "glmz") =>
        hub.InvokeAsync(nameof(CanonDocTools), nameof(ListCanonSectionsImpl), new { documentType, universeSlug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ListCanonSectionsImpl(string documentType, string universeSlug = "glmz")
    {
        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null)
            return JsonSerializer.Serialize(new { error = "unknown_universe", universeSlug }, CanonTools.JsonOpts);

        var doc = await canonDocs.FindDocumentAsync(documentType, universeId.Value);

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
        "do NOT hand-edit the generated .md files under docs/ (BIBLE.md, WORLD.md, FRANCHISE.md, " +
        "CRAFT.md, DELIGHT.md, docs/universes/*.md). " +
        "The .md artifact and the MarkdownFiles sync (what DocContextService reads at generation time) " +
        "are regenerated automatically as part of this call — no follow-up call needed. " +
        "To find available sectionKeys, call list_canon_sections first.")]
    public Task<string> SetCanonSection(
        [Description("Document type — call list_canon_document_types for the current valid values.")] string documentType,
        [Description("Stable section key — e.g. 'SS-LAW-1', 'SS-§3', 'preamble'. Use list_canon_sections to find existing keys.")] string sectionKey,
        [Description("Full section content (markdown). Replaces the existing content for this key.")] string content,
        [Description("Universe slug: glmz, scry/caul/fantasy, or universe GUID. Defaults to glmz.")] string universeSlug = "glmz",
        [Description("Optional: human-readable section title (the ## heading text). Leave blank to keep the existing title.")] string? sectionTitle = null) =>
        hub.InvokeAsync(nameof(CanonDocTools), nameof(SetCanonSectionImpl), new { documentType, sectionKey, content, universeSlug, sectionTitle });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> SetCanonSectionImpl(string documentType, string sectionKey, string content, string universeSlug = "glmz", string? sectionTitle = null)
    {
        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
        if (universeId == null)
            return JsonSerializer.Serialize(new { error = "unknown_universe", universeSlug }, CanonTools.JsonOpts);

        var result = await canonDocs.UpsertSectionAsync(documentType, universeId.Value, sectionKey, content, sectionTitle);

        if (!result.Ok)
            return JsonSerializer.Serialize(new { error = result.Error, message = result.ErrorMessage }, CanonTools.JsonOpts);

        // Cascade immediately — the edit isn't "done" until the .md on disk and the MarkdownFiles
        // row DocContextService actually reads both reflect it. No hint string, no follow-up call
        // to remember: the write and the propagation are one operation.
        var genResult = await canonDocs.GenerateMdAsync(documentType, universeId.Value);
        var syncResult = await markdownFiles.SyncAllAsync();

        return JsonSerializer.Serialize(new
        {
            ok            = true,
            action        = result.Action,
            section_key   = result.SectionKey,
            document_type = documentType,
            regenerated   = genResult.Ok,
            file_path     = genResult.FilePath,
            checksum      = genResult.Checksum,
            synced        = new { inserted = syncResult.Inserted, updated = syncResult.Updated, unchanged = syncResult.Unchanged, errors = syncResult.Errors },
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "Regenerate a world-canon .md file from its DB sections. Writes the assembled content to disk and " +
        "updates the LastChecksum so codex doctor validates the file as current. " +
        "Run this after every set_canon_section call.")]
    public Task<string> GenerateCanonMd(
        [Description("Document type — call list_canon_document_types for the current valid values.")] string documentType,
        [Description("Universe slug: glmz, scry/caul/fantasy, or universe GUID. Defaults to glmz.")] string universeSlug = "glmz") =>
        hub.InvokeAsync(nameof(CanonDocTools), nameof(GenerateCanonMdImpl), new { documentType, universeSlug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> GenerateCanonMdImpl(string documentType, string universeSlug = "glmz")
    {
        var universeId = await canonDocs.ResolveUniverseIdAsync(universeSlug);
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
        "Update or create a structured section in a book's node bible (NodeBibleSections table). " +
        "sectionType: Full | ArcSummary | Characters | VoiceRegister | NarrativeLocks | BeatSpine. " +
        "Use 'Full' to replace the entire hand-authored bible blob; use typed sections to maintain " +
        "structured per-category content. The docs/nodes/<CODE>.md artifact and the MarkdownFiles " +
        "sync (what DocContextService reads) are regenerated automatically as part of this call.")]
    public Task<string> SetBookBibleSection(
        [Description("Node id (GUID), slug, or NodeCode.")] string nodeIdOrSlug,
        [Description("Section type: Full, ArcSummary, Characters, VoiceRegister, NarrativeLocks, or BeatSpine.")] string sectionType,
        [Description("Section content (markdown). Replaces any existing content for this sectionType.")] string content) =>
        hub.InvokeAsync(nameof(CanonDocTools), nameof(SetBookBibleSectionImpl), new { nodeIdOrSlug, sectionType, content });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> SetBookBibleSectionImpl(string nodeIdOrSlug, string sectionType, string content)
    {
        var nodeId = await ResolveNodeIdAsync(nodeIdOrSlug);
        if (nodeId == null)
            return JsonSerializer.Serialize(new { error = "node_not_found", nodeIdOrSlug }, CanonTools.JsonOpts);

        var result = await canonDocs.SetNodeBibleSectionAsync(nodeId.Value, sectionType, content);

        if (!result.Ok)
            return JsonSerializer.Serialize(new { error = result.Error, message = result.ErrorMessage }, CanonTools.JsonOpts);

        // Cascade immediately — same reasoning as SetCanonSection: propagation is part of the write,
        // not a follow-up step a caller has to remember. GenerateAsync throws on genuine failure
        // (e.g. node not found) rather than returning an error union — nodeId is already validated
        // above, so a thrown exception here is a real regeneration bug, not an expected outcome.
        var genResult = await nodeDoc.GenerateAsync(nodeId.Value);
        var syncResult = await markdownFiles.SyncAllAsync();

        return JsonSerializer.Serialize(new
        {
            ok           = true,
            action       = result.Action,
            section_type = result.SectionKey,
            node_id      = nodeId,
            regenerated  = true,
            file_path    = genResult.Path,
            beat_count   = genResult.BeatCount,
            synced       = new { inserted = syncResult.Inserted, updated = syncResult.Updated, unchanged = syncResult.Unchanged, errors = syncResult.Errors },
        }, CanonTools.JsonOpts);
    }

    [McpServerTool, Description(
        "List all NodeBibleSections for a book node. Shows section types, content lengths, and last-updated timestamps. " +
        "Use this to see which typed sections exist before calling set_book_bible_section.")]
    public Task<string> ListBookBibleSections(
        [Description("Node id (GUID), slug, or NodeCode.")] string nodeIdOrSlug) =>
        hub.InvokeAsync(nameof(CanonDocTools), nameof(ListBookBibleSectionsImpl), new { nodeIdOrSlug });

    /// <summary>The real logic — runs inside the Hub's process via ToolDispatch reflection, never called directly by this process.</summary>
    public async Task<string> ListBookBibleSectionsImpl(string nodeIdOrSlug)
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
