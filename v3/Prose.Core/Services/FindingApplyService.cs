using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

public enum ApplyOutcome
{
    Applied,            // snippet → fix substitution succeeded; file updated
    SnippetNotFound,    // snippet not present in the file (LLM paraphrased — needs manual edit)
    NoSuggestedFix,     // finding has no suggested fix to apply
    NoSnippet,          // finding has no snippet to anchor the replacement
    FileMissing,        // the source file no longer exists
    Failed,             // unexpected error during write
}

public record FindingApplyResult(ApplyOutcome Outcome, string? Detail = null);

/// <summary>
/// Applies a finding's suggested fix to the source file. The edit strategy is
/// straightforward: locate <see cref="Finding.Snippet"/> in the file's text
/// and replace it with <see cref="Finding.SuggestedFix"/>. For chapter files,
/// the substitution targets the html field (where prose lives).
///
/// On success, writes a backup to engine/data/archives/findings/ and marks
/// the finding Applied. On failure (snippet paraphrased / not found), leaves
/// the finding untouched so the user can edit manually.
/// </summary>
public class FindingApplyService
{
    private readonly FindingsService findings;
    private readonly IPathProvider paths;
    private readonly ILogger<FindingApplyService> log;

    public FindingApplyService(
        FindingsService findings,
        IPathProvider paths,
        ILogger<FindingApplyService> log)
    {
        this.findings = findings;
        this.paths    = paths;
        this.log      = log;
    }

    public async Task<FindingApplyResult> ApplyAsync(long findingId, CancellationToken ct = default)
    {
        var f = findings.Get(findingId);
        if (f is null) return new(ApplyOutcome.Failed, "Finding not found.");
        if (string.IsNullOrWhiteSpace(f.SuggestedFix)) return new(ApplyOutcome.NoSuggestedFix);
        if (string.IsNullOrWhiteSpace(f.Snippet))      return new(ApplyOutcome.NoSnippet);
        if (!File.Exists(f.FilePath))                  return new(ApplyOutcome.FileMissing);

        try
        {
            var original = await File.ReadAllTextAsync(f.FilePath, ct);

            // For chapter files, substitute inside the html field; for everything
            // else, do a direct substring swap on the raw file content.
            string updated;
            if (TryUpdateChapterHtml(original, f.Snippet, f.SuggestedFix, out var newContent))
            {
                updated = newContent;
            }
            else if (original.Contains(f.Snippet))
            {
                updated = original.Replace(f.Snippet, f.SuggestedFix);
            }
            else
            {
                return new(ApplyOutcome.SnippetNotFound,
                    "The exact snippet wasn't found in the file. Edit manually.");
            }

            if (updated == original)
                return new(ApplyOutcome.SnippetNotFound, "Replacement made no change.");

            await BackupAsync(f.FilePath, original, ct);
            await File.WriteAllTextAsync(f.FilePath, updated, ct);
            findings.SetStatus(f.Id, FindingStatus.Applied);
            log.LogInformation("Applied finding {Id} to {Path}", f.Id, f.FilePath);
            return new(ApplyOutcome.Applied);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Apply failed for finding {Id}", findingId);
            return new(ApplyOutcome.Failed, ex.Message);
        }
    }

    private static bool TryUpdateChapterHtml(string original, string snippet, string fix, out string updated)
    {
        updated = "";
        try
        {
            var node = JsonNode.Parse(original);
            if (node is not JsonObject obj) return false;
            if (!obj.TryGetPropertyValue("html", out var htmlNode)) return false;
            if (htmlNode is not JsonValue v) return false;
            var html = v.GetValue<string>();
            if (!html.Contains(snippet)) return false;

            obj["html"] = html.Replace(snippet, fix);
            updated = obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            return true;
        }
        catch { return false; }
    }

    private async Task BackupAsync(string filePath, string content, CancellationToken ct)
    {
        var dir = Path.Combine(paths.ArchiveDir, "findings");
        Directory.CreateDirectory(dir);
        var name = $"{Path.GetFileNameWithoutExtension(filePath)}_{DateTime.UtcNow:yyyyMMddTHHmmss}_pre-apply.json";
        var bakPath = Path.Combine(dir, name);
        await File.WriteAllTextAsync(bakPath, content, ct);
    }
}
