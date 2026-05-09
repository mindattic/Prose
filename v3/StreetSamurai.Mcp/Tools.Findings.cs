using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

/// <summary>
/// Tools for the autonomous quality-findings inbox. Wraps the same
/// FindingsService / FindingApplyService / ContinuousQualityService used by the
/// /findings UI so MCP clients can list, triage, and apply suggested fixes
/// without opening the web UI.
/// </summary>
[McpServerToolType]
public class FindingsTools
{
    private readonly FindingsService store;
    private readonly FindingApplyService apply;
    private readonly ContinuousQualityService monitor;

    public FindingsTools(FindingsService store, FindingApplyService apply, ContinuousQualityService monitor)
    {
        this.store   = store;
        this.apply   = apply;
        this.monitor = monitor;
    }

    /// <summary>
    /// List findings from the autonomous quality inbox. ContinuousQualityService
    /// auto-detects contradictions and clichés on every chapter save; results
    /// land here for triage. Sorted high-severity-first.
    /// </summary>
    [McpServerTool, Description(
        "List findings from the autonomous quality inbox. ContinuousQualityService " +
        "auto-detects contradictions and clichés on every chapter save; results land " +
        "here for triage. Sorted high-severity-first.")]
    public string ListFindings(
        [Description("Filter by status: New, Triaged, Applied, Dismissed. Omit for all.")]
            string? status = null,
        [Description("Max number of findings to return. Default 100.")]
            int limit = 100)
    {
        FindingStatus? filter = null;
        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<FindingStatus>(status, ignoreCase: true, out var parsed))
            filter = parsed;

        var items = store.List(filter, limit);
        return JsonSerializer.Serialize(new
        {
            ok = true,
            count = items.Count,
            items = items.Select(f => new
            {
                id           = f.Id,
                detectedAt   = f.DetectedAt,
                category     = f.Category.ToString(),
                severity     = f.Severity.ToString(),
                status       = f.Status.ToString(),
                file         = f.FilePath,
                chapterId    = f.ChapterId,
                summary      = f.Summary,
                snippet      = f.Snippet,
                suggestedFix = f.SuggestedFix,
            }),
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>Counts of findings per status (new / triaged / applied / dismissed).</summary>
    [McpServerTool, Description("Counts of findings per status (new / triaged / applied / dismissed).")]
    public string FindingsStats() => JsonSerializer.Serialize(new
    {
        @new      = store.CountByStatus(FindingStatus.New),
        triaged   = store.CountByStatus(FindingStatus.Triaged),
        applied   = store.CountByStatus(FindingStatus.Applied),
        dismissed = store.CountByStatus(FindingStatus.Dismissed),
    }, new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// Apply a finding's suggested fix to the source file. Locates the snippet,
    /// replaces it with the suggested rewrite, writes a backup to
    /// engine/data/archives/findings/, and marks the finding Applied. Returns the
    /// outcome: Applied, SnippetNotFound (LLM paraphrased — edit manually),
    /// NoSuggestedFix, NoSnippet, FileMissing, or Failed.
    /// </summary>
    [McpServerTool, Description(
        "Apply a finding's suggested fix to the source file. Locates the snippet " +
        "in the file, replaces it with the suggested rewrite, writes a backup to " +
        "engine/data/archives/findings/, and marks the finding Applied. Returns " +
        "the outcome: Applied, SnippetNotFound (LLM paraphrased — edit manually), " +
        "NoSuggestedFix, NoSnippet, FileMissing, or Failed.")]
    public async Task<string> ApplyFinding(
        [Description("Finding id from list_findings.")] long id)
    {
        var result = await apply.ApplyAsync(id);
        return JsonSerializer.Serialize(new
        {
            ok = result.Outcome == ApplyOutcome.Applied,
            outcome = result.Outcome.ToString(),
            detail = result.Detail,
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>Mark a finding triaged / applied / dismissed without writing to source files.</summary>
    [McpServerTool, Description("Mark a finding triaged / applied / dismissed without writing to source files.")]
    public string SetFindingStatus(
        [Description("Finding id.")] long id,
        [Description("Target status: Triaged, Applied, or Dismissed.")] string status)
    {
        if (!Enum.TryParse<FindingStatus>(status, ignoreCase: true, out var s))
            return JsonSerializer.Serialize(new { error = $"unknown status: {status}" });
        store.SetStatus(id, s);
        return JsonSerializer.Serialize(new { ok = true, id, status = s.ToString() });
    }

    /// <summary>
    /// Manually trigger a quality scan (contradiction + cliché) on a single
    /// chapter file. Normally the autonomous monitor runs this on every save;
    /// use this for ad-hoc rescans without modifying the file.
    /// </summary>
    [McpServerTool, Description(
        "Manually trigger a quality scan (contradiction + cliché) on a single " +
        "chapter file. Normally the autonomous monitor runs this on every save; " +
        "use this for ad-hoc rescans without modifying the file.")]
    public async Task<string> ScanChapterQuality(
        [Description("Absolute path to a chapter.json file.")] string filePath)
    {
        if (!File.Exists(filePath))
            return JsonSerializer.Serialize(new { error = "file_not_found", filePath });
        await monitor.AnalyzeFileAsync(filePath);
        return JsonSerializer.Serialize(new { ok = true, scanned = filePath });
    }
}
