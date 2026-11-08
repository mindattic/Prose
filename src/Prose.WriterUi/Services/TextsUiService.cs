using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using Prose.Core.Services;

namespace Prose.WriterUi.Services;

/// <summary>
/// RFC 0018 §9 (PRS-T922): the Writer's view of a universe's game texts (<c>prose-text/1</c>) at <c>/texts</c>:
/// browse by kind, key or entity; each text's versions and the findings the game-mode verifiers filed on them; and
/// the approval queue, with bulk approve by kind.
///
/// <para><b>Approval stays the author's action.</b> Every approval here goes through the one approval path the CLI
/// and HTTP use (<see cref="ProseTextService.ApproveAsync"/>, <see cref="ProseTextService.BulkApproveAsync"/>, the
/// PRS-T918 service), needs the author's name, and runs only on the author's click; a bulk approval is always
/// previewed as a dry run first. Nothing here approves on load, on a timer or because of a finding (RFC 0009).</para>
///
/// <para>Like <see cref="ProseTextService"/> every call is explicit about the universe, so this service needs no
/// flow-universe pin. Scoped like the Writer's other UI services.</para>
/// </summary>
public sealed class TextsUiService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ProseTextService texts,
    ProseTextVerifier verifier,
    FindingsService findings)
{
    public sealed record UniverseSummary(string Slug, string Name, int Total, IReadOnlyDictionary<string, int> ByStatus, int Pending);

    /// <summary>One text in the browse list: its newest version, the live and pending versions, and its open findings.</summary>
    public sealed record TextRow(
        string Id, string Kind, string Key, string Status, int Version, int? LiveVersion, int? DraftVersion,
        string Title, IReadOnlyList<string> About, IReadOnlyList<string> Speakers, string? Place, double? Order,
        string? Chapter, int OpenFindings, string? WorstSeverity);

    /// <summary>One kind's pending drafts in the approval queue.</summary>
    public sealed record QueueGroup(string Kind, IReadOnlyList<TextRow> Drafts, int WithFindings);

    /// <summary>Every universe that has game texts, with counts by status.</summary>
    public async Task<IReadOnlyList<UniverseSummary>> ListUniversesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var withTexts = await db.Entities.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.EntityType == ProseTextService.EntityType)
            .Select(e => e.UniverseId).Distinct().ToListAsync(ct);
        var universes = await db.Universes.AsNoTracking()
            .Where(u => withTexts.Contains(u.Id)).OrderBy(u => u.Slug).ToListAsync(ct);

        var list = new List<UniverseSummary>();
        foreach (var u in universes)
        {
            var all = await texts.ListAsync(u.Slug, status: "all", ct: ct);
            var drafts = await texts.ListAsync(u.Slug, status: "draft", ct: ct);
            list.Add(new UniverseSummary(u.Slug, string.IsNullOrWhiteSpace(u.Name) ? u.Slug : u.Name, all.Count,
                all.GroupBy(d => d.Status).ToDictionary(g => g.Key, g => g.Count()), drafts.Count));
        }
        return list;
    }

    /// <summary>
    /// A universe's texts at their newest version, filtered by kind, key (substring), entity (an id or name
    /// substring matched against about, speakers and place) and status (all, draft, approved, published, retired).
    /// </summary>
    public async Task<IReadOnlyList<TextRow>> ListTextsAsync(string universe, string? kind = null, string? key = null,
        string? entity = null, string? status = null, CancellationToken ct = default)
    {
        var docs = await texts.ListAsync(universe, string.IsNullOrWhiteSpace(kind) ? null : kind, "all", ct: ct);
        var open = OpenFindingsByText(universe);
        var rows = docs.Select(d => ToRow(d, open)).AsEnumerable();
        if (!string.IsNullOrWhiteSpace(key))
            rows = rows.Where(r => r.Key.Contains(key.Trim(), StringComparison.OrdinalIgnoreCase)
                                || r.Id.Contains(key.Trim(), StringComparison.OrdinalIgnoreCase)
                                || r.Title.Contains(key.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(entity))
        {
            var e = entity.Trim();
            rows = rows.Where(r => r.About.Concat(r.Speakers).Append(r.Place ?? "")
                .Any(x => x.Contains(e, StringComparison.OrdinalIgnoreCase)));
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "all")
            rows = status == "draft"
                ? rows.Where(r => r.Status == "draft" || r.DraftVersion != null)
                : rows.Where(r => r.Status == status);
        return rows.ToList();
    }

    /// <summary>The approval queue: every pending draft, grouped by kind.</summary>
    public async Task<IReadOnlyList<QueueGroup>> QueueAsync(string universe, CancellationToken ct = default)
    {
        var drafts = await texts.ListAsync(universe, status: "draft", ct: ct);
        var open = OpenFindingsByText(universe, onlyVersionOf: drafts.ToDictionary(d => d.Id, d => d.Version));
        return drafts.Select(d => ToRow(d, open))
            .GroupBy(r => r.Kind)
            .OrderBy(g => Array.IndexOf(ProseTextService.Kinds, g.Key))
            .Select(g => new QueueGroup(g.Key, g.OrderBy(r => r.Key, StringComparer.Ordinal).ToList(), g.Count(r => r.OpenFindings > 0)))
            .ToList();
    }

    /// <summary>Every version of one text, oldest first.</summary>
    public Task<ProseTextService.TextRecord?> GetRecordAsync(string universe, string id, CancellationToken ct = default) =>
        texts.GetRecordAsync(universe, id, ct);

    /// <summary>Every finding filed on one text, every version, every status.</summary>
    public IReadOnlyList<ProseTextVerifier.FindingRow> Findings(string universe, string id) =>
        verifier.ListFindingRows(universe, id);

    /// <summary>The author approves one version (the CLI's <c>--texts-approve … --by</c>).</summary>
    public Task<ProseTextService.ActionResult> ApproveAsync(string universe, string id, int version, string by, CancellationToken ct = default) =>
        texts.ApproveAsync(universe, [id], by, version, ct);

    /// <summary>What "approve all of this kind" would do, changing nothing (the CLI's <c>--texts-approve-all --dry-run</c>).</summary>
    public Task<ProseTextService.BulkResult> PreviewBulkApproveAsync(string universe, string kind, string by, CancellationToken ct = default) =>
        texts.BulkApproveAsync(universe, kind, by, dryRun: true, ct: ct);

    /// <summary>The author approves every pending draft of one kind (the CLI's <c>--texts-approve-all</c>), after a preview.</summary>
    public Task<ProseTextService.BulkResult> BulkApproveAsync(string universe, string kind, string by, CancellationToken ct = default) =>
        texts.BulkApproveAsync(universe, kind, by, ct: ct);

    /// <summary>Run the verifiers (one text, or the whole universe) and file what they find. Writes findings only.</summary>
    public Task<ProseTextVerifier.VerifyResult> VerifyAsync(string universe, string? id = null, CancellationToken ct = default) =>
        verifier.VerifyAsync(universe, id == null ? null : [id], ct: ct);

    public void Triage(long findingId) => findings.SetStatus(findingId, FindingStatus.Triaged);

    public void Dismiss(long findingId) => findings.SetStatus(findingId, FindingStatus.Dismissed);

    /// <summary>Open (new or triaged) findings per text. With <paramref name="onlyVersionOf"/>, only those on the given
    /// version of each text (the queue counts the draft's findings, not an older version's).</summary>
    private Dictionary<string, (int Count, string? Worst)> OpenFindingsByText(string universe, IReadOnlyDictionary<string, int>? onlyVersionOf = null)
    {
        var rows = verifier.ListFindingRows(universe, status: FindingStatus.New, limit: 5000)
            .Concat(verifier.ListFindingRows(universe, status: FindingStatus.Triaged, limit: 5000));
        if (onlyVersionOf != null)
            rows = rows.Where(f => onlyVersionOf.TryGetValue(f.TextId, out var v) && f.Version == v);
        return rows.GroupBy(f => f.TextId).ToDictionary(g => g.Key, g => (g.Count(),
            g.Select(f => f.Severity).OrderBy(SeverityRank).FirstOrDefault()));
    }

    public static int SeverityRank(string? s) => s switch { "High" => 0, "Medium" => 1, "Low" => 2, _ => 3 };

    private static TextRow ToRow(ProseTextService.TextDoc d, Dictionary<string, (int Count, string? Worst)> open)
    {
        open.TryGetValue(d.Id, out var f);
        return new TextRow(d.Id, d.Kind, d.Key, d.Status, d.Version, d.LiveVersion, d.DraftVersion, TitleOf(d),
            d.About, d.Speakers, d.Context?.Place, d.Context?.Order, d.Context?.Chapter, f.Count, f.Worst);
    }

    /// <summary>A one-line label: the payload's title or term, else the start of its first line.</summary>
    public static string TitleOf(ProseTextService.TextDoc d)
    {
        var p = d.Payload;
        if (p.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            foreach (var field in new[] { "title", "term", "short", "brief" })
                if (p.TryGetProperty(field, out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString()))
                    return Clip(v.GetString()!, 90);
            foreach (var field in new[] { "lines", "paragraphs", "pages" })
                if (p.TryGetProperty(field, out var arr) && arr.ValueKind == System.Text.Json.JsonValueKind.Array && arr.GetArrayLength() > 0
                    && arr[0].ValueKind == System.Text.Json.JsonValueKind.String)
                    return Clip(arr[0].GetString()!, 90);
            if (p.TryGetProperty("nodes", out var nodes) && nodes.ValueKind == System.Text.Json.JsonValueKind.Array && nodes.GetArrayLength() > 0
                && nodes[0].TryGetProperty("line", out var line) && line.ValueKind == System.Text.Json.JsonValueKind.String)
                return Clip(line.GetString()!, 90);
        }
        return d.Key;
    }

    private static string Clip(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
