using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Single read path for "which file does this DocumentType write to, under which title/scope/
/// frontmatter" — replaces the hardcoded dictionaries formerly duplicated in
/// <c>CanonDocumentService</c>, <c>CanonDocumentCli</c>, <c>MigrateCanonDocsCli</c>, and
/// <c>MarkdownFileService</c>. Backed by <see cref="CanonDocumentType"/> rows.
/// </summary>
public class CanonDocumentTypeRegistry(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    public async Task<CanonDocumentType?> GetAsync(string documentType, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.CanonDocumentTypes
            .FirstOrDefaultAsync(t => t.DocumentType == documentType && t.IsActive, ct);
    }

    /// <summary>Resolved project-relative path (forward slashes) for a (documentType,
    /// universeId) pair, or null if the type is unknown/inactive. Substitutes <c>{slug}</c>
    /// with the target universe's uppercased <c>Slug</c> — required for a <c>Scope="universe"</c>
    /// type to serve more than one universe under distinct filenames.</summary>
    public async Task<string?> GetRelativePathAsync(string documentType, Guid universeId, CancellationToken ct = default)
    {
        var type = await GetAsync(documentType, ct);
        if (type == null) return null;

        var relative = type.PathTemplate;
        if (relative.Contains("{slug}"))
        {
            var slug = await ResolveUniverseSlugAsync(universeId, ct);
            relative = relative.Replace("{slug}", slug.ToUpperInvariant());
        }
        return relative;
    }

    /// <summary>Resolved absolute file path — <see cref="GetRelativePathAsync"/> combined with
    /// <paramref name="dataRoot"/>.</summary>
    public async Task<string?> GetFilePathAsync(string documentType, Guid universeId, string dataRoot, CancellationToken ct = default)
    {
        var relative = await GetRelativePathAsync(documentType, universeId, ct);
        return relative == null ? null : Path.Combine(dataRoot, relative.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>Resolved title for a (documentType, universeId) pair. Substitutes <c>{name}</c>
    /// with the target universe's display <c>Name</c>.</summary>
    public async Task<string?> GetTitleAsync(string documentType, Guid universeId, CancellationToken ct = default)
    {
        var type = await GetAsync(documentType, ct);
        if (type == null) return null;

        if (!type.TitleTemplate.Contains("{name}")) return type.TitleTemplate;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var name = await db.Universes.Where(u => u.Id == universeId).Select(u => u.Name).FirstOrDefaultAsync(ct);
        return type.TitleTemplate.Replace("{name}", name ?? "");
    }

    /// <summary>Full YAML frontmatter block (without the enclosing <c>---</c> fences) for one
    /// document: the shared boilerplate, this type's <c>layer:</c>, then the type's own
    /// <c>ExtraFrontMatter</c> (the common case — identical across every document of this type),
    /// then finally this SPECIFIC document's own <see cref="CanonDocument.ExtraFrontMatter"/> if
    /// set — needed when a type has more than one universe's row and each needs its own
    /// <c>scope:</c>/<c>triggers:</c>/<c>related:</c> (e.g. GLMZ.md and SCRY.md are both
    /// "UniverseCraft" but obviously don't share a trigger-keyword list).</summary>
    public async Task<string> GetFrontMatterAsync(string documentType, Guid universeId, CancellationToken ct = default)
    {
        var type = await GetAsync(documentType, ct);
        var sb = new System.Text.StringBuilder("codex: SS\nproject: StreetSamurai\ncode: SS\n");
        if (type?.FrontMatterLayer is { Length: > 0 } layer)
            sb.Append($"layer: {layer}\n");
        sb.Append("status: live\n");
        if (type?.ExtraFrontMatter is { Length: > 0 } extra)
            sb.Append(extra.TrimEnd('\n')).Append('\n');

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var docExtra = await db.CanonDocuments
            .Where(d => d.DocumentType == documentType && d.UniverseId == universeId)
            .Select(d => d.ExtraFrontMatter)
            .FirstOrDefaultAsync(ct);
        if (docExtra is { Length: > 0 })
            sb.Append(docExtra.TrimEnd('\n')).Append('\n');

        return sb.ToString();
    }

    /// <summary>A <c>Scope="base"</c> type is shared across all fiction — always stamp
    /// <see cref="Universe.SharedId"/> regardless of what the caller requested, so a caller
    /// passing a real universe slug for a base-scope type (e.g. asking for CRAFT.md "as GLMZ")
    /// can never accidentally create a second, duplicate document. A <c>Scope="universe"</c>
    /// type (or an unknown type — fail closed to the caller's own request) passes the requested
    /// id through unchanged.</summary>
    public async Task<Guid> ResolveEffectiveUniverseIdAsync(string documentType, Guid requestedUniverseId, CancellationToken ct = default)
    {
        var type = await GetAsync(documentType, ct);
        return type?.Scope == "base" ? Universe.SharedId : requestedUniverseId;
    }

    /// <summary>Every active type, with the (type, universeId) pairs it currently has rows for
    /// in <c>CanonDocuments</c> — i.e. what <c>--generate-canon-md --all</c> should actually
    /// regenerate, driven by what's really migrated rather than a compile-time list.</summary>
    public async Task<IReadOnlyList<(string DocumentType, Guid UniverseId)>> ListMigratedAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.CanonDocuments
            .Join(db.CanonDocumentTypes.Where(t => t.IsActive), d => d.DocumentType, t => t.DocumentType,
                (d, t) => new { d.DocumentType, d.UniverseId, t.SortKey })
            .OrderBy(x => x.SortKey)
            .ToListAsync(ct);
        return rows.Select(x => (x.DocumentType, x.UniverseId)).ToList();
    }

    public async Task<IReadOnlyList<string>> ListActiveTypeNamesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.CanonDocumentTypes.Where(t => t.IsActive).OrderBy(t => t.SortKey)
            .Select(t => t.DocumentType).ToListAsync(ct);
    }

    private async Task<string> ResolveUniverseSlugAsync(Guid universeId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Universes.Where(u => u.Id == universeId).Select(u => u.Slug).FirstOrDefaultAsync(ct) ?? "unknown";
    }
}
