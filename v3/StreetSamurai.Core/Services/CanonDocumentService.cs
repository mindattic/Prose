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

    public CanonDocumentService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        IPathProvider paths)
    {
        this.dbFactory = dbFactory;
        this.paths     = paths;
    }

    // ── Document-type → file path mapping ────────────────────────────────────

    private static readonly Dictionary<string, Func<string, string>> FilePaths = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["WorldBible"]    = root => Path.Combine(root, "docs", "BIBLE.md"),
        ["WorldMaster"]   = root => Path.Combine(root, "docs", "WORLD.md"),
        ["Franchise"]     = root => Path.Combine(root, "docs", "FRANCHISE.md"),
        ["UniverseCanon"] = root => Path.Combine(root, "docs", "universes", "CAUL.md"),
    };

    public static string? GetFilePath(string documentType, string dataRoot)
        => FilePaths.TryGetValue(documentType, out var fn) ? fn(dataRoot) : null;

    private static readonly Dictionary<string, string> FrontMatter = new(
        StringComparer.OrdinalIgnoreCase)
    {
        ["WorldBible"]    = "codex: SS\nproject: StreetSamurai\ncode: SS\nlayer: bible\nstatus: live\n",
        ["WorldMaster"]   = "codex: SS\nproject: StreetSamurai\ncode: SS\nlayer: world\nstatus: live\n",
        ["Franchise"]     = "codex: SS\nproject: StreetSamurai\ncode: SS\nlayer: franchise\nstatus: live\n",
        ["UniverseCanon"] = "codex: SS\nproject: StreetSamurai\ncode: SS\nlayer: universe\nstatus: live\n",
    };

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
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var doc = await db.CanonDocuments
            .Include(d => d.Sections.OrderBy(s => s.SortKey))
            .FirstOrDefaultAsync(d => d.UniverseId == universeId
                                   && d.DocumentType == documentType, ct);

        if (doc == null)
            return new GenerateResult(false, null, "document_not_found",
                $"No {documentType} document for universe {universeId}.");

        var filePath = GetFilePath(documentType, paths.DataRoot);
        if (filePath == null)
            return new GenerateResult(false, null, "unknown_document_type",
                $"No file-path mapping for document type '{documentType}'.");

        var assembled = AssembleDocument(doc.Title ?? documentType, doc.Sections);
        var checksum  = ComputeChecksum(assembled);

        // Write disk mirror with generated-file header (delete-then-rewrite + ReadOnly, matching NodeDocService)
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        if (File.Exists(filePath))
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
            File.Delete(filePath);
        }
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var fm    = FrontMatter.TryGetValue(documentType, out var fmBase)
            ? $"---\n{fmBase}updated: {today}\n---\n\n"
            : "";
        var withHeader = $"{fm}<!-- GENERATED — do not hand-edit. Regenerate with: ss --generate-canon-md --type {documentType} -->\n\n{assembled}";
        await File.WriteAllTextAsync(filePath, withHeader, Encoding.UTF8, ct);
        File.SetAttributes(filePath, FileAttributes.ReadOnly);

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

        // Verify node exists
        bool nodeExists = await db.Nodes.AnyAsync(n => n.Id == nodeId, ct);
        if (!nodeExists)
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
                sb.AppendLine(section.Content);
                sb.AppendLine();
            }
            else
            {
                var heading = section.SectionTitle ?? section.SectionKey;
                var anchor  = section.SectionKey.Contains('§') ? $" {{#{section.SectionKey}}}" : "";
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
