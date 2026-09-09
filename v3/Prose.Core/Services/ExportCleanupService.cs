using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>Archives the existing per-book export bundle before a new export writes files.</summary>
public class ExportCleanupService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly SettingsService settings;
    public ExportCleanupService(IDbContextFactory<ProseDbContext> dbFactory, SettingsService settings)
    { this.dbFactory = dbFactory; this.settings = settings; }

    public async Task<string> CleanAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().Where(s => s.Id == nodeId)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var universeSlug = await db.Universes.AsNoTracking().Where(u => u.Id == node.UniverseId)
            .Select(u => u.Slug).FirstOrDefaultAsync(ct);
        var baseDir = settings.GetExportDirectory(universeSlug);
        var (nodeDir, _) = await ExportPathResolver.ResolveAsync(db, node, baseDir, ct);
        Clean(nodeDir, node.Version);
        return nodeDir;
    }

    /// <summary>Copies existing export artifacts into Archives\v&lt;version&gt;, then removes live copies.</summary>
    public void Clean(string nodeDir, int? fallbackVersion = null)
    {
        Directory.CreateDirectory(nodeDir);
        foreach (var file in Directory.EnumerateFiles(nodeDir, "*", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(file);
            var version = ExtractVersion(name) ?? fallbackVersion ?? 0;
            var archiveDir = Path.Combine(nodeDir, "Archives", $"v{version}");
            Directory.CreateDirectory(archiveDir);
            var destination = Path.Combine(archiveDir, name);
            if (File.Exists(destination)) destination = Path.Combine(archiveDir, $"{Path.GetFileNameWithoutExtension(name)}__{DateTime.UtcNow:yyyyMMddHHmmssfff}{Path.GetExtension(name)}");
            try
            {
                File.Copy(file, destination, overwrite: false);
                if (!name.Equals("cover.jpg", StringComparison.OrdinalIgnoreCase)) File.Delete(file);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    /// <summary>Copies the completed live export bundle into its permanent version archive.</summary>
    public void ArchiveCurrent(string nodeDir, int version)
    {
        Directory.CreateDirectory(nodeDir);
        var archiveDir = Path.Combine(nodeDir, "Archives", $"v{version}");
        Directory.CreateDirectory(archiveDir);
        foreach (var file in Directory.EnumerateFiles(nodeDir, "*", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(file);
            var destination = Path.Combine(archiveDir, name);
            if (File.Exists(destination))
                destination = Path.Combine(archiveDir, $"{Path.GetFileNameWithoutExtension(name)}__{DateTime.UtcNow:yyyyMMddHHmmssfff}{Path.GetExtension(name)}");
            try { File.Copy(file, destination, overwrite: false); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static int? ExtractVersion(string fileName)
    {
        var match = Regex.Match(fileName, @"(?:^|\s)[Vv](\d+)(?:\.|\s|$)");
        return match.Success && int.TryParse(match.Groups[1].Value, out var v) ? v : null;
    }
}
