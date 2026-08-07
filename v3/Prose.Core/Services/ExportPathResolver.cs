using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

/// <summary>
/// Resolves the export folder and file base-name for a node, shared by
/// <see cref="DocxExportService"/> and <see cref="ManuscriptExportService"/> so the two
/// formats never disagree on where a book lands.
///
/// <b>NodeCode-first (current convention, 2026-07-27):</b> when a node has a
/// <c>NodeCode</c> (e.g. "MATTHEW", "BCODA", "VIGL"), it publishes flat, directly under the
/// universe's export directory, folder and file base-name both the code — e.g.
/// ".../GSPL/MATTHEW/MATTHEW V3.docx". The full descriptive title ("Gospel: History vs.
/// Heritage — Book 1: Matthew") is reserved for the title page inside the document; it has
/// no bearing on the folder or file name once a code is assigned.
///
/// <b>Legacy fallback (no NodeCode):</b> nodes without a code keep the older
/// title-derived, series-ancestry-nested path (".../Street Samurai/Bushido Coda/Bushido
/// Coda V5.docx") with a sibling-collision de-dup prefix. This exists only so older or
/// not-yet-coded book nodes keep exporting somewhere sane — assign every book a NodeCode
/// to move it onto the flat convention.
/// </summary>
public static class ExportPathResolver
{
    public static async Task<(string NodeDir, string FileBaseName)> ResolveAsync(
        ProseDbContext db, Node node, string baseDir, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(node.NodeCode))
        {
            var code = node.NodeCode.Trim();
            return (Path.Combine(baseDir, code), code);
        }

        // Legacy path: mirror the node's series/book ancestry so a book that belongs to a
        // series publishes one (or more) levels deeper — e.g. "<base>/Street
        // Samurai/Bushido Coda/Bushido Coda V5.docx" — while a standalone book stays at
        // "<base>/<Title>/...".
        var ancestors = new List<string>();
        var parentId = node.ParentNodeId;
        for (var guard = 0; parentId is Guid pid && guard < 8; guard++)
        {
            var parent = await db.Nodes.AsNoTracking()
                .Where(s => s.Id == pid)
                .Select(s => new { s.Title, s.ParentNodeId })
                .FirstOrDefaultAsync(ct);
            if (parent is null) break;
            ancestors.Insert(0, SanitizeTitle(parent.Title));   // top-down order
            parentId = parent.ParentNodeId;
        }

        var safeTitle = SanitizeTitle(node.Title);

        // De-dup: if a sibling node produces the same folder name, prefix with
        // NodeCode — or GUID7 if NodeCode is null or shared with a colliding sibling.
        var siblings = await db.Nodes.AsNoTracking()
            .Where(s => s.Id != node.Id && s.ParentNodeId == node.ParentNodeId)
            .Select(s => new { s.Title, s.NodeCode })
            .ToListAsync(ct);
        if (siblings.Any(s => SanitizeTitle(s.Title) == safeTitle))
        {
            var code = node.NodeCode;
            if (string.IsNullOrWhiteSpace(code) ||
                siblings.Any(s => SanitizeTitle(s.Title) == safeTitle && s.NodeCode == code))
                code = node.Id.ToString("N")[..7];
            safeTitle = $"[{code}] {safeTitle}";
        }

        var pathParts = new List<string> { baseDir };
        pathParts.AddRange(ancestors);
        pathParts.Add(safeTitle);
        return (Path.Combine(pathParts.ToArray()), safeTitle);
    }

    public static string SanitizeTitle(string title)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        invalid.Add('\''); invalid.Add('’');
        var kept = new string((title ?? "").Where(c => !invalid.Contains(c)).ToArray()).Trim();
        kept = Regex.Replace(kept, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(kept) ? "untitled" : kept;
    }
}
