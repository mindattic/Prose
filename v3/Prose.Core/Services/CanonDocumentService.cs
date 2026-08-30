using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Structured canon editing — read / upsert / regenerate CanonDocuments, CanonDocumentSections,
/// and NodeOutlineSections. All canon edits flow through here; .md files are generated artifacts.
/// </summary>
public class CanonDocumentService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly IPathProvider paths;
    private readonly CanonDocumentTypeRegistry typeRegistry;
    private readonly ContinuityExtractionService? continuityExtraction;
    private readonly ILogger<CanonDocumentService>? log;

    public CanonDocumentService(
        IDbContextFactory<ProseDbContext> dbFactory,
        IPathProvider paths,
        CanonDocumentTypeRegistry typeRegistry,
        ContinuityExtractionService? continuityExtraction = null,
        ILogger<CanonDocumentService>? log = null)
    {
        this.dbFactory    = dbFactory;
        this.paths        = paths;
        this.typeRegistry = typeRegistry;
        this.continuityExtraction = continuityExtraction;
        this.log          = log;
    }

    // ── Resolve a universe slug or id string → Guid ───────────────────────────

    /// <summary>
    /// Fast-path aliases for the two universes with well-known constants, plus a raw GUID
    /// passthrough — kept static/sync since these cases need no DB hit.
    /// </summary>
    static Guid? ResolveWellKnownAlias(string universeSlug) => universeSlug.ToLowerInvariant() switch
    {
        "glmz" or "cyberpunk" => Universe.GlmzId,
        "scry" or "fantasy" or "caul" or "cauld" => Universe.FantasyId,
        "nonfiction" => Universe.NonfictionId,
        _ => Guid.TryParse(universeSlug, out var id) ? id : null,
    };

    /// <summary>
    /// Resolves a universe slug (or raw GUID string) to its Guid. Was a hardcoded switch
    /// covering only glmz/scry (plus their aliases) — every universe added after those two
    /// (nonfiction, fiction, horror, erotica) silently returned null ("unknown_universe") from
    /// every canon-document MCP tool no matter how the universe was spelled, since there was no
    /// fallback path at all. Falls back to a live, case-insensitive slug lookup against the
    /// Universe table (IgnoreQueryFilters defensively, in case a future global filter is ever
    /// added to this entity — Universe itself carries none today) so a newly-registered universe
    /// resolves immediately, with no code change required.
    /// </summary>
    public async Task<Guid?> ResolveUniverseIdAsync(string universeSlug, CancellationToken ct = default)
    {
        var wellKnown = ResolveWellKnownAlias(universeSlug);
        if (wellKnown != null) return wellKnown;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var slug = universeSlug.ToLowerInvariant();
        return await db.Universes.IgnoreQueryFilters()
            .Where(u => u.Slug.ToLower() == slug)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct);
    }

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

    /// <summary>
    /// Looks up a <see cref="CanonDocument"/> (with its sections, ordered) by document type and a
    /// caller-requested universe id — resolving through <see cref="CanonDocumentTypeRegistry.
    /// ResolveEffectiveUniverseIdAsync"/> first, exactly as <see cref="UpsertSectionAsync"/> and
    /// <see cref="GenerateMdAsync"/> already do.
    ///
    /// 2026-08-23 fix: extracted because two independent read call sites (the MCP tool
    /// <c>list_canon_sections</c> and the CLI's <c>--list-canon-sections</c>, added the same day)
    /// had each queried <c>CanonDocuments</c> directly against the caller's raw requested
    /// universe id, without the effective-scope resolution — so a "base"-scope type (e.g.
    /// <c>EngineGuide</c>, stored under <see cref="Universe.SharedId"/> regardless of what
    /// universe is asked for) could never be found by either read path, even though the write
    /// path (<see cref="UpsertSectionAsync"/>) already handled it correctly. Same "two
    /// independent implementations of the same lookup drift apart" bug class as the write-gate
    /// initiative's <c>DeleteNodeCli</c>/<c>CloneNodeCli</c> findings — fixed by giving both
    /// callers one shared, correct implementation instead of two copies.
    /// </summary>
    public async Task<CanonDocument?> FindDocumentAsync(string documentType, Guid requestedUniverseId, CancellationToken ct = default)
    {
        var effectiveUniverseId = await typeRegistry.ResolveEffectiveUniverseIdAsync(documentType, requestedUniverseId, ct);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.CanonDocuments
            .Include(d => d.Sections.OrderBy(s => s.SortKey))
            .FirstOrDefaultAsync(d => d.UniverseId == effectiveUniverseId && d.DocumentType == documentType, ct);
    }

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
                $"No {documentType} document for universe {universeId}. Run prose --migrate-canon-docs first.");

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
                $"No CanonDocumentTypes row for document type '{documentType}'. Run prose --list-canon-types to see what's registered.");

        var assembled = AssembleDocument(doc.Title ?? documentType, doc.Sections);
        var checksum  = ComputeChecksum(assembled);

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var fmBase = await typeRegistry.GetFrontMatterAsync(documentType, universeId, ct);
        var fm     = $"---\n{fmBase}updated: {today}\n---\n\n";
        var withHeader = $"{fm}<!-- GENERATED — do not hand-edit. Regenerate with: prose --generate-canon-md --type {documentType} -->\n\n{assembled}";
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

    // ── NodeOutlineSection upsert ───────────────────────────────────────────────

    public async Task<UpsertResult> SetNodeOutlineSectionAsync(
        Guid nodeId,
        string sectionType,
        string content,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // IgnoreQueryFilters(): explicit nodeId, not an ambient scope (same bug class found and
        // fixed in BookArchiveService.ArchiveAsync, 2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == nodeId, ct);
        if (node == null)
            return new UpsertResult(false, null, "node_not_found", $"Node {nodeId} not found.");

        var section = await db.NodeOutlineSections
            .FirstOrDefaultAsync(s => s.NodeId == nodeId
                                   && s.SectionType == sectionType, ct);

        bool isNew = section == null;
        if (isNew)
        {
            section = new NodeOutlineSection
            {
                NodeId      = nodeId,
                SectionType = sectionType,
            };
            db.NodeOutlineSections.Add(section);
        }

        section!.Content   = content;
        section.UpdatedAt  = DateTime.UtcNow;

        // NodeDocService.GenerateAsync (the cascade every caller of this method runs immediately
        // after) reads hand-authored content exclusively from Nodes.NodeOutline — it never reads
        // NodeOutlineSections. sectionType "Full" is documented as "replace the entire hand-authored
        // bible blob", so it must also land here or the cascade regenerates from the stale blob
        // and silently discards this write. (Typed sections below Full have no downstream reader
        // yet — recording them in NodeOutlineSections is honest storage, not a composed bible edit.)
        if (string.Equals(sectionType, "Full", StringComparison.OrdinalIgnoreCase))
        {
            node.NodeOutline = string.IsNullOrEmpty(content) ? null : content;
            node.NodeOutlineGeneratedAt = DateTime.UtcNow;
            node.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        // Fire-and-forget: keep the continuity ledger fresh for any book that's already opted in.
        // Closes the loop for free on every Trinity bible-patch and every bible revert, not just
        // hand-authored edits — see ContinuityExtractionCursor's doc comment for why this exists.
        if (continuityExtraction != null)
        {
            _ = Task.Run(() => continuityExtraction.ReExtractOutlineSectionIfChangedAsync(nodeId, sectionType, ct: CancellationToken.None), CancellationToken.None)
                .ContinueWith(t => log?.LogError(t.Exception, "ReExtractOutlineSectionIfChangedAsync background task failed"),
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        return new UpsertResult(true, nodeId, null, null, isNew ? "created" : "updated", sectionType);
    }

    // ── Get all NodeOutlineSections for a node ──────────────────────────────────

    public async Task<List<NodeOutlineSection>> GetNodeOutlineSectionsAsync(
        Guid nodeId,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.NodeOutlineSections
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
