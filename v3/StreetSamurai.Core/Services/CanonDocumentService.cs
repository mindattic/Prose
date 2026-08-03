using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Structured canon editing — read / upsert / regenerate CanonDocuments, CanonDocumentSections,
/// and NodeBibleSections. All canon edits flow through here; .md files are generated artifacts.
/// </summary>
public class CanonDocumentService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly IPathProvider paths;
    private readonly CanonDocumentTypeRegistry typeRegistry;

    public CanonDocumentService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        IPathProvider paths,
        CanonDocumentTypeRegistry typeRegistry)
    {
        this.dbFactory    = dbFactory;
        this.paths        = paths;
        this.typeRegistry = typeRegistry;
    }

    // ── Resolve a universe slug or id string → Guid ───────────────────────────

    public static Guid? ResolveUniverseId(string universeSlug)
        => universeSlug.ToLowerInvariant() switch
        {
            "glmz" or "cyberpunk" => Universe.GlmzId,
            "scry" or "fantasy" or "caul" or "cauld" => Universe.FantasyId,
            _ => Guid.TryParse(universeSlug, out var id) ? id : null,
        };

    // ── Get a full assembled document from DB sections ───────────────────────

    public async Task<string?> GetDocumentAsync(
        string documentType,
        Guid universeId,
        CancellationToken ct = default)
    {
        universeId = await typeRegistry.ResolveEffectiveUniverseIdAsync(documentType, universeId, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CanonDocuments
            .Include(d => d.Sections.OrderBy(s => s.SortKey))
            .FirstOrDefaultAsync(d => d.UniverseId == universeId
                                   && d.DocumentType == documentType, ct);

        if (doc == null) return null;

        return AssembleDocument(doc.Title ?? documentType, doc.Sections);
    }

    // ── Upsert a single section ───────────────────────────────────────────────

    public async Task<UpsertResult> UpsertSectionAsync(
        string documentType,
        Guid universeId,
        string sectionKey,
        string content,
        string? sectionTitle = null,
        CancellationToken ct = default)
    {
        universeId = await typeRegistry.ResolveEffectiveUniverseIdAsync(documentType, universeId, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CanonDocuments
            .Include(d => d.Sections)
            .FirstOrDefaultAsync(d => d.UniverseId == universeId
                                   && d.DocumentType == documentType, ct);

        if (doc == null)
            return new UpsertResult(false, null, "document_not_found",
                $"No {documentType} document for universe {universeId}. Run ss --migrate-canon-docs first.");

        var section = doc.Sections.FirstOrDefault(s =>
            s.SectionKey.Equals(sectionKey, StringComparison.OrdinalIgnoreCase));

        bool isNew = section == null;
        if (isNew)
        {
            section = new CanonDocumentSection
            {
                DocumentId   = doc.Id,
                SectionKey   = sectionKey,
                SectionTitle = sectionTitle ?? sectionKey,
                SortKey      = doc.Sections.Count > 0 ? doc.Sections.Max(s => s.SortKey) + 1 : 0,
            };
            db.CanonDocumentSections.Add(section);
        }
        else if (sectionTitle != null)
        {
            section!.SectionTitle = sectionTitle;
        }

        section!.Content   = content;
        section.UpdatedAt = DateTime.UtcNow;
        doc.UpdatedAt     = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return new UpsertResult(true, doc.Id, null, null, isNew ? "created" : "updated", sectionKey);
    }

    // ── Regenerate the .md file from DB sections ──────────────────────────────

    public async Task<GenerateResult> GenerateMdAsync(
        string documentType,
        Guid universeId,
        CancellationToken ct = default)
    {
        universeId = await typeRegistry.ResolveEffectiveUniverseIdAsync(documentType, universeId, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CanonDocuments
            .Include(d => d.Sections.OrderBy(s => s.SortKey))
            .FirstOrDefaultAsync(d => d.UniverseId == universeId
                                   && d.DocumentType == documentType, ct);

        if (doc == null)
            return new GenerateResult(false, null, "document_not_found",
                $"No {documentType} document for universe {universeId}.");

        var filePath = await typeRegistry.GetFilePathAsync(documentType, universeId, paths.DataRoot, ct);
        if (filePath == null)
            return new GenerateResult(false, null, "unknown_document_type",
                $"No CanonDocumentTypes row for document type '{documentType}'. Run ss --list-canon-types to see what's registered.");

        var assembled = AssembleDocument(doc.Title ?? documentType, doc.Sections);
        var checksum  = ComputeChecksum(assembled);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var fmBase = await typeRegistry.GetFrontMatterAsync(documentType, universeId, ct);
        var fm     = $"---\n{fmBase}updated: {today}\n---\n\n";
        var withHeader = $"{fm}<!-- GENERATED — do not hand-edit. Regenerate with: ss --generate-canon-md --type {documentType} -->\n\n{assembled}";
        // Atomic (per-process scratch file + rename) so two CLI/MCP processes regenerating the
        // same canon doc concurrently can't corrupt or race it.
        await GeneratedFileWriter.WriteReadOnlyAsync(filePath, withHeader, ct);

        // Update checksum
        var row = await db.CanonDocuments.FindAsync([doc.Id], ct)!;
        row!.LastChecksum = checksum;
        row.UpdatedAt     = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new GenerateResult(true, filePath, null, null, doc.Sections.Count, checksum);
    }

    // ── NodeBibleSection upsert ───────────────────────────────────────────────

    public async Task<UpsertResult> SetNodeBibleSectionAsync(
        Guid nodeId,
        string sectionType,
        string content,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.FirstOrDefaultAsync(n => n.Id == nodeId, ct);
        if (node == null)
            return new UpsertResult(false, null, "node_not_found", $"Node {nodeId} not found.");

        var section = await db.NodeBibleSections
            .FirstOrDefaultAsync(s => s.NodeId == nodeId
                                   && s.SectionType == sectionType, ct);

        bool isNew = section == null;
        if (isNew)
        {
            section = new NodeBibleSection
            {
                NodeId      = nodeId,
                SectionType = sectionType,
            };
            db.NodeBibleSections.Add(section);
        }

        section!.Content   = content;
        section.UpdatedAt  = DateTime.UtcNow;

        // NodeDocService.GenerateAsync (the cascade every caller of this method runs immediately
        // after) reads hand-authored content exclusively from Nodes.NodeBible — it never reads
        // NodeBibleSections. sectionType "Full" is documented as "replace the entire hand-authored
        // bible blob", so it must also land here or the cascade regenerates from the stale blob
        // and silently discards this write. (Typed sections below Full have no downstream reader
        // yet — recording them in NodeBibleSections is honest storage, not a composed bible edit.)
        if (string.Equals(sectionType, "Full", StringComparison.OrdinalIgnoreCase))
        {
            node.NodeBible = string.IsNullOrEmpty(content) ? null : content;
            node.NodeBibleGeneratedAt = DateTime.UtcNow;
            node.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        return new UpsertResult(true, nodeId, null, null, isNew ? "created" : "updated", sectionType);
    }

    // ── Get all NodeBibleSections for a node ──────────────────────────────────

    public async Task<List<NodeBibleSection>> GetNodeBibleSectionsAsync(
        Guid nodeId,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.NodeBibleSections
            .Where(s => s.NodeId == nodeId)
            .OrderBy(s => s.SectionType)
            .ToListAsync(ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string AssembleDocument(string title, IEnumerable<CanonDocumentSection> sections)
    {
        var sb = new StringBuilder();
        foreach (var section in sections)
        {
            if (section.SectionKey == "preamble")
            {
                // A preamble whose entire content is itself a YAML frontmatter block (e.g. a
                // stray `tier:`/`triggers:` block swallowed by an early, naive migration import)
                // must not be rendered into the visible body — GenerateMdAsync already writes
                // its own frontmatter header above this, so rendering it here doubled it.
                if (section.Content.TrimStart().StartsWith("---")) continue;
                sb.AppendLine(section.Content);
                sb.AppendLine();
            }
            else
            {
                var heading = section.SectionTitle ?? section.SectionKey;
                // Re-emit the explicit anchor the section was originally keyed by (e.g. "SS-LAW-1",
                // "SS-CRAFT-0") so other docs can keep citing it stably. "section-<slug>-<n>" is the
                // auto-slugified fallback MigrateCanonDocsCli assigns to a heading that had no
                // {#anchor} in the source file — those never had a real anchor to begin with, so
                // they stay anchor-less. Previously this checked for a literal '§' character, which
                // happened to cover BIBLE.md's "SS-§N" convention but silently dropped every anchor
                // that doesn't use it (CRAFT.md's "SS-CRAFT-N", etc.) — a real regression waiting to
                // trigger on the next doc migrated without a '§' in its anchor scheme.
                var anchor = section.SectionKey.StartsWith("section-", StringComparison.Ordinal)
                    ? "" : $" {{#{section.SectionKey}}}";
                sb.AppendLine($"## {heading}{anchor}");
                sb.AppendLine();
                sb.AppendLine(section.Content);
                sb.AppendLine();
            }
        }
        return sb.ToString().TrimEnd() + "\n";
    }

    private static string ComputeChecksum(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public record UpsertResult(
    bool Ok,
    Guid? DocumentId,
    string? Error,
    string? ErrorMessage,
    string? Action = null,
    string? SectionKey = null);

public record GenerateResult(
    bool Ok,
    string? FilePath,
    string? Error,
    string? ErrorMessage,
    int SectionCount = 0,
    string? Checksum = null);
