using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;

namespace StreetSamurai.Core.Services;

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
    string? RecordedTitleId
);

/// <summary>
/// Closes the loop after a human (or an agent) actually finishes republishing a book on KDP.
/// Sets <c>KdpPublishedAt = now</c> and <c>PublicationStatus = "Published"</c> so the book drops
/// off <see cref="KdpManifestService"/>'s "needs republish" list. Optionally updates
/// <see cref="StreetSamurai.Core.Data.Entities.Node.PublishUrl"/> (only needed the first time a
/// book goes live, or if the URL changed) and upserts <c>tools/kdp/title-ids.json</c> with a
/// titleId so future manifests can deep-link straight to this book's KDP edit page.
///
/// Shared by <c>ss --kdp-mark-published</c> (CLI: <c>KdpMarkPublishedCli</c>, a thin wrapper
/// that prints the result) and the KdpPublish WPF app's <c>mark_published</c> tool.
/// </summary>
public class KdpMarkPublishedService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;

    public KdpMarkPublishedService(IDbContextFactory<StreetSamuraiDbContext> dbFactory)
    {
        this.dbFactory = dbFactory;
    }

    public async Task<KdpMarkPublishedResult> MarkPublishedAsync(
        string slug, string? url, string? titleId, string repoRoot, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Slug == slug, ct);
        if (node == null)
            return new KdpMarkPublishedResult(false, $"No node with slug '{slug}'.", null, null, null, null, null);

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

        return new KdpMarkPublishedResult(true, null, node.NodeCode ?? node.Slug, node.Title, node.KdpPublishedAt, node.PublishUrl, recordedTitleId);
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
