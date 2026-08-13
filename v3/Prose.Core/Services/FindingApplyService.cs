using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

public enum ApplyOutcome
{
    Applied,            // snippet → fix substitution succeeded; Beat.Text (or file) updated
    SnippetNotFound,    // snippet not present in the target (LLM paraphrased — needs manual edit)
    NoSuggestedFix,     // finding has no suggested fix to apply
    NoSnippet,          // finding has no snippet to anchor the replacement
    FileMissing,        // the source file no longer exists (legacy on-disk findings only)
    BeatMissing,        // the beat this finding was anchored to no longer exists
    Failed,             // unexpected error during write
}

public record FindingApplyResult(ApplyOutcome Outcome, string? Detail = null);

/// <summary>
/// Applies a finding's suggested fix to its source. The edit strategy is straightforward:
/// locate <see cref="Finding.Snippet"/> and replace it with <see cref="Finding.SuggestedFix"/>.
///
/// Two targets, dispatched by FilePath shape:
///   - "node:{slug}/beat:{guid}" or "beat:{guid}" — current convention (2026-05-09 SQL Server
///     migration onward). The beat's prose lives in Beat.Text; the substitution targets that
///     DB column directly. Found 2026-08-13: this path used to fall through to the file-I/O
///     branch below, which called File.Exists on a "node:.../beat:..." string — structurally
///     never a real path, so every current finding's Apply call failed with FileMissing no
///     matter what. Only 103 findings were ever marked Applied all-time, and every one of them
///     got there via a direct SetFindingStatus call bypassing this method entirely, not through
///     a successful apply.
///   - An actual on-disk file path — legacy chapter-JSON convention, kept for any Findings that
///     predate the SQL Server migration and still carry a real path. For chapter files, the
///     substitution targets the html field (where prose lives); otherwise a raw substring swap.
///
/// On success, marks the finding Applied. On failure (snippet paraphrased / not found), leaves
/// the finding untouched so the user can edit manually.
/// </summary>
public class FindingApplyService
{
    private readonly FindingsService findings;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly IPathProvider paths;
    private readonly ILogger<FindingApplyService> log;

    public FindingApplyService(
        FindingsService findings,
        IDbContextFactory<ProseDbContext> dbFactory,
        IPathProvider paths,
        ILogger<FindingApplyService> log)
    {
        this.findings  = findings;
        this.dbFactory = dbFactory;
        this.paths     = paths;
        this.log       = log;
    }

    public async Task<FindingApplyResult> ApplyAsync(long findingId, CancellationToken ct = default)
    {
        var f = findings.Get(findingId);
        if (f is null) return new(ApplyOutcome.Failed, "Finding not found.");
        if (string.IsNullOrWhiteSpace(f.SuggestedFix)) return new(ApplyOutcome.NoSuggestedFix);
        if (string.IsNullOrWhiteSpace(f.Snippet))      return new(ApplyOutcome.NoSnippet);

        var beatId = ExtractBeatId(f.FilePath);
        return beatId.HasValue
            ? await ApplyToBeatAsync(f, beatId.Value, ct)
            : await ApplyToFileAsync(f, ct);
    }

    /// <summary>Pulls a beat guid out of "node:{slug}/beat:{guid}" or "beat:{guid}" — the two
    /// shapes every current-schema (DB-backed) Finding's FilePath actually takes.</summary>
    private static Guid? ExtractBeatId(string filePath)
    {
        const string marker = "beat:";
        var i = filePath.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return null;
        var candidate = filePath[(i + marker.Length)..];
        return Guid.TryParseExact(candidate, "N", out var g) || Guid.TryParse(candidate, out g) ? g : null;
    }

    private async Task<FindingApplyResult> ApplyToBeatAsync(Finding f, Guid beatId, CancellationToken ct)
    {
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var beat = await db.Beats.FirstOrDefaultAsync(b => b.Id == beatId, ct);
            if (beat is null) return new(ApplyOutcome.BeatMissing);
            if (!beat.Text.Contains(f.Snippet!, StringComparison.Ordinal))
                return new(ApplyOutcome.SnippetNotFound,
                    "The exact snippet wasn't found in the beat's current text. Edit manually.");

            var updated = beat.Text.Replace(f.Snippet!, f.SuggestedFix!);
            if (updated == beat.Text) return new(ApplyOutcome.SnippetNotFound, "Replacement made no change.");

            beat.Text = updated;
            await db.SaveChangesAsync(ct);
            findings.SetStatus(f.Id, FindingStatus.Applied);
            log.LogInformation("Applied finding {Id} to beat {BeatId}", f.Id, beatId);
            return new(ApplyOutcome.Applied);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Apply failed for finding {Id} (beat {BeatId})", f.Id, beatId);
            return new(ApplyOutcome.Failed, ex.Message);
        }
    }

    private async Task<FindingApplyResult> ApplyToFileAsync(Finding f, CancellationToken ct)
    {
        if (!File.Exists(f.FilePath)) return new(ApplyOutcome.FileMissing);

        try
        {
            var original = await File.ReadAllTextAsync(f.FilePath, ct);

            // For chapter files, substitute inside the html field; for everything
            // else, do a direct substring swap on the raw file content.
            string updated;
            if (TryUpdateChapterHtml(original, f.Snippet!, f.SuggestedFix!, out var newContent))
            {
                updated = newContent;
            }
            else if (original.Contains(f.Snippet!))
            {
                updated = original.Replace(f.Snippet!, f.SuggestedFix!);
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
            log.LogWarning(ex, "Apply failed for finding {Id}", f.Id);
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
