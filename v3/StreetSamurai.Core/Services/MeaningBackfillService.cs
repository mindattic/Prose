using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public record MeaningBackfillReport(string NodeCode, int Missing, int Filled, int Failed);

/// <summary>
/// Backfills the MEANING coordinate (Beat.Description) for beats that have prose but
/// no recorded meaning — the gap the coordination pass surfaces. Reads each beat's
/// prose and writes a one-sentence statement of what the beat accomplishes, in the
/// authorial-intent register the existing Descriptions use. Batched (~10 beats/call)
/// on Sonnet. Metadata migration — updates Description only, never prose.
/// </summary>
public class MeaningBackfillService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly ILogger<MeaningBackfillService> log;

    private const int BatchSize = 10;
    private const int ProseClip = 1600;

    public MeaningBackfillService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILlmService llm,
        ILogger<MeaningBackfillService> log)
    {
        this.dbFactory = dbFactory;
        this.llm = llm;
        this.log = log;
    }

    public async Task<MeaningBackfillReport> BackfillAsync(
        string slugOrCode, int? limit = null, bool dryRun = false,
        Action<string>? progress = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(
            n => n.Slug == slugOrCode || (n.NodeCode != null && n.NodeCode.ToUpper() == slugOrCode.ToUpper()), ct)
            ?? throw new InvalidOperationException($"Node not found: {slugOrCode}");
        var nodeCode = node.NodeCode?.ToUpperInvariant() ?? node.Slug.ToUpperInvariant();

        var missing = await (
            from bn in db.BeatNodes.AsNoTracking()
            join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
            join c in db.Nodes.AsNoTracking() on bn.NodeId equals c.Id
            where bn.IsEnabled && (bn.NodeId == node.Id || c.ParentNodeId == node.Id)
                  && (b.Description == null || b.Description == "")
                  && b.Text != null && b.Text != ""
            orderby c.SortKey, bn.SortKey
            select new { b.Id, b.Number, b.Text, Chapter = c.Title }).ToListAsync(ct);

        if (limit.HasValue) missing = missing.Take(limit.Value).ToList();

        if (missing.Count == 0)
            return new MeaningBackfillReport(nodeCode, 0, 0, 0);

        int filled = 0, failed = 0;
        var system = """
You are a story editor recording each beat's narrative PURPOSE — what it accomplishes in the
story, not a recap of its plot. Read each beat's prose and write ONE sentence (max ~30 words)
in the third-person authorial-intent register, e.g. "Kyle concedes the touch to establish
mutual respect with an opponent who has proven himself." Name what the beat DOES for the reader
(establishes, escalates, reveals, pays off, turns). No purple prose, no quoting the beat.

Output STRICT JSON, no fences, no commentary:
{"items":[{"ref":N,"meaning":"..."}]}
""";

        for (int start = 0; start < missing.Count; start += BatchSize)
        {
            ct.ThrowIfCancellationRequested();
            var batch = missing.Skip(start).Take(BatchSize).ToList();
            var refMap = new Dictionary<int, Guid>();
            var sb = new StringBuilder();
            for (int i = 0; i < batch.Count; i++)
            {
                refMap[i] = batch[i].Id;
                var prose = batch[i].Text!.Length > ProseClip ? batch[i].Text![..ProseClip] : batch[i].Text!;
                sb.AppendLine($"[ref {i} · {batch[i].Chapter}]");
                sb.AppendLine(prose);
                sb.AppendLine();
            }

            try
            {
                var raw = await llm.GenerateAsync(system, sb.ToString(), temperature: 0.3,
                    maxTokens: 1500, model: LlmModels.Sonnet, ct: ct);
                using var doc = JsonDocument.Parse(StripFences(raw));
                if (!doc.RootElement.TryGetProperty("items", out var arr)) { failed += batch.Count; continue; }

                // Reload tracked beats for this batch and update
                var ids = batch.Select(b => b.Id).ToList();
                var tracked = await db.Beats.Where(b => ids.Contains(b.Id)).ToListAsync(ct);
                var trackedById = tracked.ToDictionary(b => b.Id);

                foreach (var el in arr.EnumerateArray())
                {
                    if (!el.TryGetProperty("ref", out var refEl)) continue;
                    var meaning = el.TryGetProperty("meaning", out var m) ? m.GetString() : null;
                    if (string.IsNullOrWhiteSpace(meaning)) continue;
                    if (!refMap.TryGetValue(refEl.GetInt32(), out var beatId)) continue;
                    if (trackedById.TryGetValue(beatId, out var beat))
                    {
                        if (!dryRun) beat.Description = meaning.Trim();
                        filled++;
                    }
                }

                if (!dryRun) await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "MeaningBackfill batch {Start} failed", start);
                failed += batch.Count;
            }

            progress?.Invoke($"  {Math.Min(start + BatchSize, missing.Count)}/{missing.Count} processed ({filled} filled, {failed} failed)");
        }

        return new MeaningBackfillReport(nodeCode, missing.Count, filled, failed);
    }

    private static string StripFences(string s)
    {
        s = s.Trim();
        if (s.StartsWith("```"))
        {
            int nl = s.IndexOf('\n');
            if (nl >= 0) s = s[(nl + 1)..];
            if (s.EndsWith("```")) s = s[..^3];
        }
        return s.Trim();
    }
}
