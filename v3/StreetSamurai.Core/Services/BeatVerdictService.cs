using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

public record VerdictFinding(
    Guid BeatId, int Number, string Chapter,
    string Type, string Severity, string? Quote, string Note, string? Fix);

public record VerdictReport(
    string NodeCode, int BeatsScanned, int Clean,
    List<VerdictFinding> Findings, string JsonPath);

/// <summary>
/// Per-beat quality verdict toward "90+ with no gripes, contradictions, or clichés".
/// Reads prose (+ its recorded meaning + chapter) and flags intrinsic defects:
///   CLICHE           — stock phrasing/imagery/beat, quoted
///   GRIPE            — weak/awkward/pseudo-profound/filler-wit line (house voice rules)
///   CONTRADICTION    — a fact that conflicts inside the beat
///   MEANING-MISMATCH — prose does not accomplish the beat's stated meaning
/// Output-only: never edits prose. Cross-beat continuity/contradiction is the logic
/// sweep's job (docs/LOGIC.md); this is the intrinsic per-beat pass. Sonnet, batched.
/// </summary>
public class BeatVerdictService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly IPathProvider paths;
    private readonly ILogger<BeatVerdictService> log;

    private const int BatchSize = 4;
    private const int ProseClip = 2600;

    public BeatVerdictService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        ILlmService llm,
        IPathProvider paths,
        ILogger<BeatVerdictService> log)
    {
        this.dbFactory = dbFactory;
        this.llm = llm;
        this.paths = paths;
        this.log = log;
    }

    public async Task<VerdictReport> RunAsync(
        string slugOrCode, int? limit = null,
        Action<string>? progress = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(
            n => n.Slug == slugOrCode || (n.NodeCode != null && n.NodeCode.ToUpper() == slugOrCode.ToUpper()), ct)
            ?? throw new InvalidOperationException($"Node not found: {slugOrCode}");
        var nodeCode = node.NodeCode?.ToUpperInvariant() ?? node.Slug.ToUpperInvariant();

        var beats = await (
            from bn in db.BeatNodes.AsNoTracking()
            join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
            join c in db.Nodes.AsNoTracking() on bn.NodeId equals c.Id
            where bn.IsEnabled && (bn.NodeId == node.Id || c.ParentNodeId == node.Id)
                  && b.Text != null && b.Text != ""
            orderby c.SortKey, bn.SortKey
            select new { b.Id, b.Number, b.Text, b.Description, Chapter = c.Title }).ToListAsync(ct);

        if (limit.HasValue) beats = beats.Take(limit.Value).ToList();

        var findings = new List<VerdictFinding>();
        int scanned = 0;

        var system = """
You are a ruthless line editor for a graphic-adult cyberpunk novel with a disciplined house
voice. For each beat, flag ONLY concrete, quotable defects — do not invent problems, do not
praise. Defect types:
- CLICHE: stock phrasing, worn imagery, or a stock beat ("a single tear", "time slowed",
  "little did he know", heartbeat-in-throat, etc.). Quote the exact offending text.
- GRIPE: a weak line — pseudo-profound aphorism, filler wit / universal-truth quip, purple
  metaphor that fails literal scrutiny, telling-not-showing an emotion, or clunky mechanics.
  Quote it.
- CONTRADICTION: two statements inside this beat that cannot both be true.
- MEANING-MISMATCH: the prose does not accomplish the beat's stated purpose (given below).
Severity: BLOCKER (breaks the read) | MODERATE (noticeably weakens it) | MINOR (polish).
A clean beat returns an empty findings array. Be sparing: a strong beat has zero findings.

Output STRICT JSON, no fences, no commentary:
{"beats":[{"ref":N,"findings":[{"type":"CLICHE|GRIPE|CONTRADICTION|MEANING-MISMATCH","severity":"BLOCKER|MODERATE|MINOR","quote":"...","note":"why it fails","fix":"minimal splice suggestion"}]}]}
""";

        for (int start = 0; start < beats.Count; start += BatchSize)
        {
            ct.ThrowIfCancellationRequested();
            var batch = beats.Skip(start).Take(BatchSize).ToList();
            var refMap = new Dictionary<int, (Guid Id, int Number, string Chapter)>();
            var sb = new StringBuilder();
            for (int i = 0; i < batch.Count; i++)
            {
                refMap[i] = (batch[i].Id, batch[i].Number, batch[i].Chapter);
                var prose = batch[i].Text!.Length > ProseClip ? batch[i].Text![..ProseClip] : batch[i].Text!;
                sb.AppendLine($"[ref {i} · {batch[i].Chapter}]");
                sb.AppendLine($"Purpose: {batch[i].Description ?? "(none recorded)"}");
                sb.AppendLine("Prose:");
                sb.AppendLine(prose);
                sb.AppendLine();
            }

            try
            {
                var raw = await llm.GenerateAsync(system, sb.ToString(), temperature: 0.2,
                    maxTokens: 2200, model: LlmModels.Sonnet, ct: ct);
                using var doc = JsonDocument.Parse(StripFences(raw));
                if (doc.RootElement.TryGetProperty("beats", out var arr))
                {
                    foreach (var el in arr.EnumerateArray())
                    {
                        if (!el.TryGetProperty("ref", out var refEl)) continue;
                        if (!refMap.TryGetValue(refEl.GetInt32(), out var meta)) continue;
                        if (!el.TryGetProperty("findings", out var fArr)) continue;
                        foreach (var f in fArr.EnumerateArray())
                        {
                            findings.Add(new VerdictFinding(
                                meta.Id, meta.Number, meta.Chapter,
                                Str(f, "type") ?? "GRIPE",
                                Str(f, "severity") ?? "MINOR",
                                Str(f, "quote"),
                                Str(f, "note") ?? "",
                                Str(f, "fix")));
                        }
                    }
                }
                scanned += batch.Count;
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "BeatVerdict batch {Start} failed", start);
            }

            progress?.Invoke($"  {Math.Min(start + BatchSize, beats.Count)}/{beats.Count} scanned · {findings.Count} findings");
        }

        // Rank: BLOCKER > MODERATE > MINOR, then by beat number
        int Rank(string s) => s switch { "BLOCKER" => 0, "MODERATE" => 1, _ => 2 };
        findings = findings.OrderBy(f => Rank(f.Severity)).ThenBy(f => f.Number).ToList();

        var beatsWithFindings = findings.Select(f => f.BeatId).Distinct().Count();
        var jsonPath = Path.Combine(paths.DataRoot, "reports", "coordination", $"{nodeCode}.verdict.json");
        Directory.CreateDirectory(Path.GetDirectoryName(jsonPath)!);
        var summary = new
        {
            nodeCode,
            generatedAtUtc = DateTime.UtcNow.ToString("u"),
            beatsScanned = scanned,
            cleanBeats = scanned - beatsWithFindings,
            totalFindings = findings.Count,
            bySeverity = findings.GroupBy(f => f.Severity).ToDictionary(g => g.Key, g => g.Count()),
            byType = findings.GroupBy(f => f.Type).ToDictionary(g => g.Key, g => g.Count()),
            findings,
        };
        await File.WriteAllTextAsync(jsonPath,
            JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }), ct);

        return new VerdictReport(nodeCode, scanned, scanned - beatsWithFindings, findings, jsonPath);
    }

    private static string? Str(JsonElement el, string prop)
        => el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

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
