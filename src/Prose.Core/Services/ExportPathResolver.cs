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
            // Sanitized like a title: a code is free text at the write surface, and one carrying a
            // separator, a rooted path or ".." would otherwise land the export outside baseDir.
            // A normal code ("BCODA", "MATTHEW") passes through unchanged.
            var code = SanitizeTitle(node.NodeCode.Trim());
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
            // IgnoreQueryFilters(): explicit pid, not an ambient scope (same bug class found and
            // fixed in BookArchiveService.ArchiveAsync/WalkAsync, 2026-08-17).
            var parent = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
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
        // IgnoreQueryFilters(): explicit node.ParentNodeId, not an ambient scope — a true sibling
        // shares node's own universe by construction (parent-child is same-universe by design), so
        // no extra UniverseId filter is needed here, unlike a new-root-node creation (same bug
        // class found and fixed in BookArchiveService.ArchiveAsync/WalkAsync, 2026-08-17).
        var siblings = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
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
        // The Windows set explicitly: GetInvalidFileNameChars() is only '\0' and '/' off Windows,
        // and the exported folder is synced/opened on Windows either way.
        foreach (var c in "\\/:*?\"<>|") invalid.Add(c);
        invalid.Add('\''); invalid.Add('’');
        var kept = new string((title ?? "").Where(c => !invalid.Contains(c) && !char.IsControl(c)).ToArray()).Trim();
        kept = Regex.Replace(kept, @"\s+", " ").Trim();
        // Trailing dots/spaces are silently dropped by Windows (so "And Then..." and "And Then"
        // collided), and a title of "." or ".." would climb out of the export folder.
        kept = kept.TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(kept)) return "untitled";
        return IsReservedDeviceName(kept) ? "_" + kept : kept;
    }

    /// <summary>True for a Windows reserved device name (CON, PRN, AUX, NUL, COM0-9, LPT0-9), with
    /// or without an extension — "Con" or "nul.txt" as a folder/file name opens the device
    /// instead of creating the file.</summary>
    public static bool IsReservedDeviceName(string name)
    {
        var stem = name.Split('.')[0].TrimEnd(' ');
        return stem.ToUpperInvariant() is "CON" or "PRN" or "AUX" or "NUL"
            || (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                                     || stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
                && (char.IsAsciiDigit(stem[3]) || stem[3] is '¹' or '²' or '³'));
    }
}
