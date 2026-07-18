using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// The three-altitudes agreement audit (docs/LOGIC.md §8): compares the DESIGNED story
/// (10,000 ft — hand-authored NodeBible + structural blueprint headline) against the TOLD
/// story (100 ft — chapter synopses from <see cref="SynopsisExportService"/>) and reports
/// divergences. Beat-altitude (10 ft) checking stays with the logic sweep.
///
/// Findings land in two places: a sweep-style report at
/// <c>audit-outlines-&lt;date&gt;/logic/&lt;CODE&gt;-ALTITUDE.md</c> and rows via
/// <see cref="FindingsService"/> (category OutlineDrift; Upsert dedups re-runs).
/// </summary>
public sealed class AltitudeAuditService(
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    SynopsisExportService synopsis,
    FindingsService findings,
    ILlmService llm,
    ILogger<AltitudeAuditService> log)
{
    public sealed record AltitudeFinding(
        string Severity, string TenKftClaim, string HundredFtReality,
        string[] Chapters, string Recommendation);

    public sealed record AuditResult(
        string Slug, string NodeCode, string ReportPath, IReadOnlyList<AltitudeFinding> Findings);

    public async Task<AuditResult?> AuditAsync(Guid storyNodeId, bool forceSynopsis = false, CancellationToken ct = default)
    {
        string slug, nodeCode, title;
        string? bible;
        string blueprintHeadline;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var node = await db.Nodes.AsNoTracking().Where(n => n.Id == storyNodeId)
                .Select(n => new { n.Slug, n.NodeCode, n.Title, n.NodeBible })
                .FirstOrDefaultAsync(ct);
            if (node == null) return null;
            slug = node.Slug; nodeCode = node.NodeCode ?? node.Slug.ToUpperInvariant(); title = node.Title;
            bible = NodeDocService.ExtractHandAuthored(node.NodeBible);

            var bp = await db.NodeStructuralBlueprints.AsNoTracking()
                .Where(b => b.NodeId == storyNodeId).FirstOrDefaultAsync(ct);
            blueprintHeadline = bp == null ? "(no structural blueprint)" :
                $"Temporal: {bp.TemporalScheme}; Resolution: {bp.ResolutionMode}; " +
                $"Ending: {bp.EndingStyle}{(bp.NoEpilogue ? ", no epilogue" : "")}; " +
                $"Subplot: {(bp.HasSubplot ? bp.SubplotSummary : "none")}";
        }

        if (string.IsNullOrWhiteSpace(bible))
        {
            log.LogWarning("AltitudeAudit: {Slug} has no hand-authored bible — nothing to compare", slug);
            return null;
        }

        var chapters = await synopsis.GetChapterSummariesAsync(storyNodeId, forceSynopsis, ct);
        if (chapters.Count == 0)
        {
            log.LogWarning("AltitudeAudit: {Slug} has no enabled prose — nothing to compare", slug);
            return null;
        }

        var chapterBlock = new StringBuilder();
        foreach (var (i, chTitle, syn) in chapters)
            chapterBlock.AppendLine($"[{i + 1}] {chTitle}\n{syn.Trim()}\n");

        const string system = """
            You audit ALTITUDE AGREEMENT for a novel: does the DESIGNED story (the hand-authored
            bible: arc, characters, locks, register + the structural blueprint headline) match
            the TOLD story (chapter-by-chapter synopses generated from the live prose)?

            Report only CONCRETE divergences — a fact, event, character state, lock, or arc
            promise that one altitude asserts and the other contradicts or omits where it is
            load-bearing. Do not report tone, style, or level-of-detail differences; synopses
            compress. Do not invent problems — if the altitudes agree, return an empty list.

            Arbitration rule for recommendations: prose wins on FACTS (the bible is stale —
            recommend a bible update); the bible wins on LOCKS (explicitly locked arcs, endings,
            prohibitions — recommend a prose fix naming the chapters).

            Return STRICT JSON only, no markdown fence:
            {"findings":[{"severity":"BLOCKER|MODERATE|MINOR","tenKftClaim":"what the bible/blueprint says","hundredFtReality":"what the chapters actually tell","chapters":["Chapter N — Title"],"recommendation":"which side bends and the minimal change"}]}
            Severity: BLOCKER = the designed and told stories are different stories on this point;
            MODERATE = a load-bearing detail diverges; MINOR = small drift worth recording.
            """;

        var user = $"STORY: {title} ({nodeCode})\n\n== 10,000 FT — HAND-AUTHORED BIBLE ==\n{bible}\n\n" +
                   $"== 10,000 FT — BLUEPRINT HEADLINE ==\n{blueprintHeadline}\n\n" +
                   $"== 100 FT — CHAPTER SYNOPSES (from live prose) ==\n{chapterBlock}";

        var raw = (await llm.GenerateAsync(system, user, temperature: 0.1, maxTokens: 6000, ct: ct)).Trim();
        if (raw.StartsWith("```"))
            raw = Regex.Replace(Regex.Replace(raw, @"^```(json)?\s*", ""), @"\s*```$", "");
        // Thinking-tier models sometimes narrate before the JSON — cut to the outermost object.
        var first = raw.IndexOf('{');
        var last = raw.LastIndexOf('}');
        if (first > 0 && last > first) raw = raw[first..(last + 1)];

        List<AltitudeFinding> parsed;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            parsed = doc.RootElement.GetProperty("findings").EnumerateArray().Select(f => new AltitudeFinding(
                Normalize(f.GetProperty("severity").GetString()),
                f.GetProperty("tenKftClaim").GetString() ?? "",
                f.GetProperty("hundredFtReality").GetString() ?? "",
                f.TryGetProperty("chapters", out var chs) && chs.ValueKind == JsonValueKind.Array
                    ? chs.EnumerateArray().Select(c => c.GetString() ?? "").ToArray() : Array.Empty<string>(),
                f.GetProperty("recommendation").GetString() ?? "")).ToList();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            log.LogError(ex, "AltitudeAudit: unparseable response for {Slug}: {Head}", slug,
                raw[..Math.Min(200, raw.Length)]);
            throw new InvalidOperationException($"Altitude audit returned unparseable JSON for {slug}.");
        }

        // Findings table (OutlineDrift; Upsert dedups on filePath|category|summary).
        foreach (var f in parsed)
        {
            var sev = f.Severity switch
            {
                "BLOCKER" => FindingSeverity.High,
                "MODERATE" => FindingSeverity.Medium,
                _ => FindingSeverity.Low,
            };
            findings.Upsert(
                filePath: $"story:{slug}",
                chapterId: f.Chapters.FirstOrDefault(),
                category: FindingCategory.OutlineDrift,
                severity: sev,
                summary: $"[altitude] {f.TenKftClaim}",
                snippet: f.HundredFtReality,
                suggestedFix: f.Recommendation);
        }

        var reportPath = WriteReport(nodeCode, title, slug, blueprintHeadline, chapters.Count, parsed);
        log.LogInformation("AltitudeAudit: {Slug} — {Count} finding(s); report {Path}", slug, parsed.Count, reportPath);
        return new AuditResult(slug, nodeCode, reportPath, parsed);
    }

    private static string Normalize(string? severity) =>
        severity?.ToUpperInvariant() is "BLOCKER" or "MODERATE" or "MINOR"
            ? severity!.ToUpperInvariant() : "MINOR";

    private static string WriteReport(
        string nodeCode, string title, string slug, string blueprintHeadline,
        int chapterCount, IReadOnlyList<AltitudeFinding> parsed)
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(),
            $"audit-outlines-{DateTime.UtcNow:yyyy-MM-dd}", "logic");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{nodeCode}-ALTITUDE.md");

        var sb = new StringBuilder();
        sb.AppendLine($"# {nodeCode} — Altitude Agreement Audit ({DateTime.UtcNow:yyyy-MM-dd})");
        sb.AppendLine();
        sb.AppendLine($"Story: **{title}** (`{slug}`) — designed (bible + blueprint) vs told ({chapterCount} chapter synopses).");
        sb.AppendLine($"Blueprint headline: {blueprintHeadline}");
        sb.AppendLine();
        if (parsed.Count == 0)
        {
            sb.AppendLine("## Verdict: CLEAN — the designed and told stories agree.");
        }
        else
        {
            foreach (var f in parsed)
            {
                sb.AppendLine($"### {f.Severity}");
                sb.AppendLine($"- **10,000 ft says:** {f.TenKftClaim}");
                sb.AppendLine($"- **100 ft tells:** {f.HundredFtReality}");
                if (f.Chapters.Length > 0) sb.AppendLine($"- **Chapters:** {string.Join("; ", f.Chapters)}");
                sb.AppendLine($"- **Recommendation:** {f.Recommendation}");
                sb.AppendLine();
            }
            var b = parsed.Count(f => f.Severity == "BLOCKER");
            var m = parsed.Count(f => f.Severity == "MODERATE");
            var n = parsed.Count(f => f.Severity == "MINOR");
            sb.AppendLine($"## Verdict: FINDINGS ({b} BLOCKER / {m} MODERATE / {n} MINOR)");
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        return path;
    }
}
