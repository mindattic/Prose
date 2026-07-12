using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using System.Text.RegularExpressions;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Wipes all manuscript formats (.docx, .epub, .pdf, .txt) from a node's publish folder
/// before any export writes happen — ensures only the current version survives regardless
/// of which export path is taken.
/// </summary>
public class PublishCleanupService
{
    static readonly string[] ManuscriptPatterns = ["*.docx", "*.epub", "*.pdf", "*.txt"];

    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly SettingsService settings;

    public PublishCleanupService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        SettingsService settings)
    {
        this.dbFactory = dbFactory;
        this.settings = settings;
    }

    /// <summary>
    /// Resolves the node's publish folder (honouring the ancestor-walk path nesting)
    /// and deletes all prior-version manuscript files. Returns the resolved directory path.
    /// </summary>
    public async Task<string> CleanAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking()
            .Where(s => s.Id == nodeId)
            .Select(s => new { s.Title, s.ParentNodeId })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var ancestors = new List<string>();
        var parentId = node.ParentNodeId;
        for (var guard = 0; parentId is Guid pid && guard < 8; guard++)
        {
            var parent = await db.Nodes.AsNoTracking()
                .Where(s => s.Id == pid)
                .Select(s => new { s.Title, s.ParentNodeId })
                .FirstOrDefaultAsync(ct);
            if (parent is null) break;
            ancestors.Insert(0, SanitizeTitle(parent.Title));
            parentId = parent.ParentNodeId;
        }

        var baseDir = ResolveBaseDir();
        var safeTitle = SanitizeTitle(node.Title);
        var pathParts = new List<string> { baseDir };
        pathParts.AddRange(ancestors);
        pathParts.Add(safeTitle);
        var nodeDir = Path.Combine(pathParts.ToArray());

        Clean(nodeDir);
        return nodeDir;
    }

    /// <summary>
    /// Creates the directory (if absent) and deletes all manuscript formats from it.
    /// Safe to call on a dir that has already been cleaned.
    /// </summary>
    public void Clean(string nodeDir)
    {
        Directory.CreateDirectory(nodeDir);
        foreach (var pattern in ManuscriptPatterns)
        foreach (var file in Directory.EnumerateFiles(nodeDir, pattern))
        {
            if (Path.GetFileName(file).Equals("description.txt", StringComparison.OrdinalIgnoreCase)) continue;
            try { File.Delete(file); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private string ResolveBaseDir()
    {
        var dir = (settings.PublishExportDirectory ?? string.Empty).Trim().Trim('"', '\'').Trim();
        return string.IsNullOrWhiteSpace(dir)
            ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            : dir;
    }

    private static string SanitizeTitle(string title)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        invalid.Add('\''); invalid.Add('’');
        var kept = new string((title ?? "").Where(c => !invalid.Contains(c)).ToArray()).Trim();
        kept = Regex.Replace(kept, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(kept) ? "untitled" : kept;
    }
}
