using Microsoft.EntityFrameworkCore;
using Prose.Core.Data;
using System.Text;

namespace Prose.Core.Services;

/// <summary>
/// Queries ML-PROSE-SCORE findings filed by the nightly Python audit pipeline
/// and formats them as a generation guidance block for ProseWriterRouter.
///
/// Mirrors the BuildEmotionalGuidanceAsync pattern — findings with the
/// "ML-PROSE-SCORE" prefix are pulled from the Findings table and injected
/// into BeatContext.MlProseGuidanceContext before prose generation so the
/// LLM is warned about recurring weaknesses in the node.
/// </summary>
public class MlProseGuidanceService(IDbContextFactory<ProseDbContext> dbFactory)
{
    public const string FindingPrefix = "ML-PROSE-SCORE";

    public async Task<string> BuildGuidanceAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var slug = await db.Nodes.AsNoTracking()
            .Where(s => s.Id == nodeId)
            .Select(s => s.Slug)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrEmpty(slug)) return "";

        var fp        = $"node:{slug}";
        var catKey    = FindingCategory.Other.ToString();
        var statusKey = FindingStatus.New.ToString();

        var findings = await db.Findings.AsNoTracking()
            .Where(f => f.FilePath == fp
                        && f.Category == catKey
                        && f.Status == statusKey
                        && f.Summary.StartsWith(FindingPrefix))
            .OrderBy(f => f.Severity == "High" ? 0 : f.Severity == "Medium" ? 1 : 2)
            .ThenByDescending(f => f.DetectedAt)
            .Take(5)
            .Select(f => new { f.Summary, f.SuggestedFix })
            .ToListAsync(ct);

        if (findings.Count == 0) return "";

        var sb = new StringBuilder();
        sb.AppendLine("ML QUALITY GUIDANCE — nightly model audit flagged these beats; avoid the same patterns in this beat:");
        foreach (var f in findings)
        {
            var label = f.Summary.Replace(FindingPrefix + ": ", "").Trim();
            sb.AppendLine($"• {label}");
            if (!string.IsNullOrWhiteSpace(f.SuggestedFix))
                sb.AppendLine($"  → {f.SuggestedFix.Trim()}");
        }
        return sb.ToString().TrimEnd();
    }
}
