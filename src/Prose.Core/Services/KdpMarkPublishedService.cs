using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;

namespace Prose.Core.Services;

/// <summary>Result of <see cref="KdpMarkPublishedService.MarkPublishedAsync"/>. <see cref="Ok"/>
/// false means <see cref="Error"/> explains why (node not found, etc.) — everything else is
/// null in that case.</summary>
public record KdpMarkPublishedResult(
    bool Ok,
    string? Error,
    string? Code,
    string? Title,
    DateTime? KdpPublishedAt,
    string? PublishUrl,
    string? RecordedTitleId,
    string? Asin
);

/// <summary>
/// Closes the loop after a human (or an agent) actually finishes republishing a book on KDP.
/// Sets <c>KdpPublishedAt = now</c> and <c>PublicationStatus = "Published"</c> so the book drops
/// off <see cref="KdpManifestService"/>'s "needs republish" list. Optionally updates
/// <see cref="Prose.Core.Data.Entities.Node.PublishUrl"/> (only needed the first time a
/// book goes live, or if the URL changed) and upserts <c>tools/kdp/title-ids.json</c> with a
/// titleId so future manifests can deep-link straight to this book's KDP edit page.
///
/// Shared by <c>prose --kdp-mark-published</c> (CLI: <c>KdpMarkPublishedCli</c>, a thin wrapper
/// that prints the result) and the KdpPublish WPF app's <c>mark_published</c> tool.
/// </summary>
public class KdpMarkPublishedService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly SettingsService settings;

    public KdpMarkPublishedService(IDbContextFactory<ProseDbContext> dbFactory, SettingsService settings)
    {
        this.dbFactory = dbFactory;
        this.settings = settings;
    }

    public async Task<KdpMarkPublishedResult> MarkPublishedAsync(
        string slug, string? url, string? titleId, string repoRoot, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Slug == slug, ct);
        if (node == null)
            return new KdpMarkPublishedResult(false, $"No node with slug '{slug}'.", null, null, null, null, null, null);

        node.KdpPublishedAt = DateTime.UtcNow;
        node.PublicationStatus = "Published";
        if (!string.IsNullOrWhiteSpace(url))
            node.PublishUrl = url.Split("/ref=")[0];
        if (!string.IsNullOrWhiteSpace(titleId))
            node.KdpTitleId = titleId;
        if (string.IsNullOrWhiteSpace(node.Asin) && !string.IsNullOrWhiteSpace(node.PublishUrl))
        {
            var asinMatch = System.Text.RegularExpressions.Regex.Match(node.PublishUrl, @"/dp/([A-Z0-9]{10})");
            if (asinMatch.Success) node.Asin = asinMatch.Groups[1].Value;
        }
        await db.SaveChangesAsync(ct);

        // Hard-coded rule: the .publish marker's JSON body must be refreshed with exactly what
        // just went live — Filename, Version, ASIN, PublishedAtUtc — every time mark_published
        // fires, before the caller's book loop advances to the next book. This runs synchronously
        // inside this call (never speculatively — mark_published itself only fires after a real
        // confirmed publish, per its own tool description), so "before starting the next" is
        // satisfied by ordinary sequencing: the caller's foreach can't reach book N+1 until this
        // returns.
        try
        {
            var universeSlug = await db.Set<Prose.Core.Data.Entities.Universe>().AsNoTracking()
                .Where(u => u.Id == node.UniverseId).Select(u => u.Slug).FirstOrDefaultAsync(ct) ?? "glmz";
            var baseDir = settings.GetExportDirectory(universeSlug);
            string nodeDir; string fileBaseName;
            try { (nodeDir, fileBaseName) = await ExportPathResolver.ResolveAsync(db, node, baseDir, ct); }
            catch { nodeDir = Path.Combine(baseDir, node.NodeCode ?? node.Slug); fileBaseName = node.NodeCode ?? node.Slug; }

            string? epubFile = null;
            var version = node.Version;
            if (Directory.Exists(nodeDir))
            {
                var best = Directory.GetFiles(nodeDir)
                    .Select(f => KdpManifestService.VersionFileRx.Match(Path.GetFileName(f)))
                    .Where(m => m.Success && string.Equals(m.Groups["ext"].Value, "epub", StringComparison.OrdinalIgnoreCase))
                    .Select(m => (Ver: int.Parse(m.Groups["ver"].Value), File: m.Value))
                    .OrderByDescending(x => x.Ver)
                    .FirstOrDefault();
                if (best.File != null) { version = best.Ver; epubFile = best.File; }
            }

            if (epubFile != null && Directory.Exists(nodeDir))
            {
                var marker = new PublishMarker(
                    File: epubFile,
                    Asin: node.Asin,
                    PublishedAtUtc: node.KdpPublishedAt?.ToString("O"),
                    Version: version);
                await File.WriteAllTextAsync(Path.Combine(nodeDir, ".publish"),
                    JsonSerializer.Serialize(marker, new JsonSerializerOptions { WriteIndented = true }), ct);
            }
        }
        catch
        {
            // Marker refresh is best-effort bookkeeping, never a reason to fail a publish that
            // has already gone live and been recorded in the DB above.
        }

        string? recordedTitleId = null;
        if (!string.IsNullOrWhiteSpace(titleId) && !string.IsNullOrWhiteSpace(node.NodeCode))
        {
            var path = Path.Combine(repoRoot, "tools", "kdp", "title-ids.json");
            Dictionary<string, JsonElement> raw = File.Exists(path)
                ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(await File.ReadAllTextAsync(path, ct)) ?? new()
                : new();

            var entry = new Dictionary<string, string?> { ["titleId"] = titleId };
            if (node.PublishUrl != null)
            {
                var m = System.Text.RegularExpressions.Regex.Match(node.PublishUrl, @"/dp/([A-Z0-9]{10})");
                if (m.Success) entry["asin"] = m.Groups[1].Value;
            }

            var merged = raw.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
            merged[node.NodeCode] = entry;
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true }), ct);
            recordedTitleId = titleId;
        }

        return new KdpMarkPublishedResult(true, null, node.NodeCode ?? node.Slug, node.Title, node.KdpPublishedAt, node.PublishUrl, recordedTitleId, node.Asin);
    }

    /// <summary>
    /// Records that <c>find_and_open_book</c> observed KDP hiding a book's edit-content link —
    /// the "Live - Updates publishing" state, up to ~72 hours after a recent republish. Merges
    /// <c>PublishingDetectedAtUtc = now</c> into the book's existing <c>.publish</c> marker
    /// (preserving whatever publish history it already recorded) so <see cref="KdpManifestService"/>
    /// reports "Publishing" instead of "Outdated" while the timestamp stays fresh. A no-op if the
    /// book's export folder doesn't exist yet — there is nothing to annotate.
    /// </summary>
    public async Task<bool> MarkPublishingDetectedAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Slug == slug, ct);
        if (node == null) return false;

        var universeSlug = await db.Set<Prose.Core.Data.Entities.Universe>().AsNoTracking()
            .Where(u => u.Id == node.UniverseId).Select(u => u.Slug).FirstOrDefaultAsync(ct) ?? "glmz";
        var baseDir = settings.GetExportDirectory(universeSlug);
        string nodeDir;
        try { (nodeDir, _) = await ExportPathResolver.ResolveAsync(db, node, baseDir, ct); }
        catch { nodeDir = Path.Combine(baseDir, node.NodeCode ?? node.Slug); }
        if (!Directory.Exists(nodeDir)) return false;

        var markerPath = Path.Combine(nodeDir, ".publish");
        var existing = File.Exists(markerPath)
            ? TryDeserializeMarker(await File.ReadAllTextAsync(markerPath, ct))
            : null;

        var updated = (existing ?? new PublishMarker(null, null, null))
            with { PublishingDetectedAtUtc = DateTime.UtcNow.ToString("O") };
        await File.WriteAllTextAsync(markerPath,
            JsonSerializer.Serialize(updated, new JsonSerializerOptions { WriteIndented = true }), ct);
        return true;
    }

    private static PublishMarker? TryDeserializeMarker(string raw)
    {
        try { return JsonSerializer.Deserialize<PublishMarker>(raw, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch { return null; }
    }

    /// <summary>
    /// The "Mark Unpublished" panel action: clears <c>PublicationStatus</c> and
    /// <c>KdpPublishedAt</c> for the given NodeCodes so <see cref="KdpManifestService"/> stops
    /// treating them as already current (falls into the "Unknown (no baseline)" / stale branch
    /// since <c>PublishUrl</c> is left alone) and a subsequent Start run will actually attempt to
    /// republish them instead of short-circuiting on the version pre-check. Deliberately does NOT
    /// touch <c>PublishUrl</c>/<c>Asin</c>/<c>KdpTitleId</c> — those are how the book gets found
    /// again, forgetting them would defeat the point of this action.
    /// </summary>
    public async Task<int> UnmarkPublishedAsync(IEnumerable<string> codes, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var codeSet = codes.ToHashSet();
        var nodes = await db.Nodes.IgnoreQueryFilters()
            .Where(n => n.NodeCode != null && codeSet.Contains(n.NodeCode))
            .ToListAsync(ct);
        foreach (var node in nodes)
        {
            node.PublicationStatus = null;
            node.KdpPublishedAt = null;
        }
        await db.SaveChangesAsync(ct);
        return nodes.Count;
    }
}
