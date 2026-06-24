using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;
using StreetSamurai.Core.Models.Canon;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Continuity / contradiction tooling ─────────────────────────────────────
// find_contradictions runs the canon-grounded contradiction sweep over a
// chapter — pulls character profiles, book state_at_end, and prior-chapter
// synopses, then dispatches a Legion Quorum vote with a structured rubric
// (EPISTEMIC / TEMPORAL / CAPABILITY / CANON).
//
// Implementation note: this MCP tool currently shells out to the Node
// prototype at tools/check-contradictions.js because that prototype is the
// validated reference implementation. A future refactor will inline the
// logic into a proper C# ContradictionFinderService backed by
// LlmVotingService.ScoreAsync — that work is queued in the architecture
// proposal saved at memory/project_contradiction_detector.md.

/// <summary>
/// Continuity / contradiction-finding tools. <c>find_contradictions</c> runs the
/// canon-grounded contradiction sweep over a chapter — pulling character profiles,
/// the book's <c>state_at_end</c>, and prior-chapter synopses, then dispatching a
/// Legion Quorum vote with a structured EPISTEMIC / TEMPORAL / CAPABILITY / CANON
/// rubric. <c>find_contradictions_book</c> does a pairwise sweep across the whole
/// book, catching cross-chapter drift a single-chapter check would miss.
/// <para/>
/// Implementation note: this MCP tool currently shells out to the Node prototype
/// at <c>tools/check-contradictions.js</c> because that prototype is the validated
/// reference implementation. A future refactor will inline the logic into a
/// proper C# ContradictionFinderService backed by <c>LlmVotingService.ScoreAsync</c>.
/// </summary>
[McpServerToolType]
public class ContinuityTools
{
    private readonly IPathProvider paths;
    private readonly IBookRepository books;
    private readonly IChapterRepository chapters;
    private readonly CharacterRepository characters;

    public ContinuityTools(
        IPathProvider paths,
        IBookRepository books,
        IChapterRepository chapters,
        CharacterRepository characters)
    {
        this.paths = paths;
        this.books = books;
        this.chapters = chapters;
        this.characters = characters;
    }

    // Builds a self-contained JSON bundle of book + chapters + character profiles
    // from the SQL Server canon, written to a temp file the Node script can read.
    // Post-2026-05-08 the legacy engine/data/{books,chapters,people}/*.json layout
    // is gone; the contradiction detector reads its canon from this bundle
    // instead of crawling disk.
    private string WriteBundleForBook(Book book)
    {
        var chapterRecords = new List<object>();
        var characterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cid in book.ChapterIds)
        {
            var c = chapters.LoadChapter(cid);
            if (c == null) continue;
            chapterRecords.Add(new
            {
                id = c.Id,
                book_id = c.BookId,
                number = c.Number,
                title = c.Title,
                synopsis = c.Synopsis,
                html = c.Html,
                characters = c.Characters,
            });
            foreach (var name in c.Characters)
            {
                if (!string.IsNullOrWhiteSpace(name)) characterNames.Add(name.Trim());
            }
        }
        foreach (var p in book.Protagonists)
        {
            if (!string.IsNullOrWhiteSpace(p)) characterNames.Add(p.Trim());
        }

        var characterRecords = new List<CharacterData>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        characters.Reload();
        foreach (var rawName in characterNames)
        {
            var cleaned = System.Text.RegularExpressions.Regex.Replace(rawName, @"\s*\([^)]*\)\s*$", "").Trim();
            var c = characters.GetByName(cleaned) ?? characters.GetByName(rawName);
            if (c == null || !seen.Add(c.Id)) continue;
            characterRecords.Add(c);
        }

        var bundle = new
        {
            book   = book,
            chapters = chapterRecords,
            characters = characterRecords,
        };
        var path = Path.Combine(Path.GetTempPath(), $"contradiction-bundle-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(bundle, CanonTools.JsonOpts));
        return path;
    }

    private string WriteBundleForChapter(Chapter chapter)
    {
        var book = string.IsNullOrEmpty(chapter.BookId) ? null : books.LoadBook(chapter.BookId);
        // For chapter mode we still want every prior-chapter synopsis available, so
        // bundle the parent book's full chapter set when one exists. The Node
        // script will index by id and only surface the priors it needs.
        var chapterRecords = new List<object>();
        var characterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddChapter(Chapter c)
        {
            chapterRecords.Add(new
            {
                id = c.Id,
                book_id = c.BookId,
                number = c.Number,
                title = c.Title,
                synopsis = c.Synopsis,
                html = c.Html,
                characters = c.Characters,
            });
            foreach (var name in c.Characters)
            {
                if (!string.IsNullOrWhiteSpace(name)) characterNames.Add(name.Trim());
            }
        }

        if (book != null)
        {
            foreach (var cid in book.ChapterIds)
            {
                var c = chapters.LoadChapter(cid);
                if (c != null) AddChapter(c);
            }
            foreach (var p in book.Protagonists)
            {
                if (!string.IsNullOrWhiteSpace(p)) characterNames.Add(p.Trim());
            }
        }
        else
        {
            AddChapter(chapter);
        }

        var characterRecords = new List<CharacterData>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        characters.Reload();
        foreach (var rawName in characterNames)
        {
            var cleaned = System.Text.RegularExpressions.Regex.Replace(rawName, @"\s*\([^)]*\)\s*$", "").Trim();
            var c = characters.GetByName(cleaned) ?? characters.GetByName(rawName);
            if (c == null || !seen.Add(c.Id)) continue;
            characterRecords.Add(c);
        }

        var bundle = new
        {
            book = book,
            chapters = chapterRecords,
            characters = characterRecords,
        };
        var path = Path.Combine(Path.GetTempPath(), $"contradiction-bundle-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(bundle, CanonTools.JsonOpts));
        return path;
    }

    /// <summary>
    /// Find contradictions in a chapter against established canon. Pulls the
    /// chapter's characters plus the book's state_at_end and all prior chapters'
    /// synopses, builds a canon-context bundle, and dispatches a Legion Quorum vote
    /// with a contradiction-finding rubric (EPISTEMIC / TEMPORAL / CAPABILITY /
    /// CANON). Returns a JSON report with findings, citations, severity, and
    /// suggested fixes. ok=true means no contradictions; ok=false means findings exist.
    /// </summary>
    [McpServerTool, Description(
        "Find contradictions in a chapter against established canon. Pulls the " +
        "characters from the chapter's `characters` field, plus the book's " +
        "state_at_end and all prior chapters' synopses, builds a canon-context " +
        "bundle, and dispatches a Legion Quorum vote with a contradiction-finding " +
        "rubric (EPISTEMIC / TEMPORAL / CAPABILITY / CANON). Returns a JSON report " +
        "with findings, citations, severity, and suggested fixes. Exit-code-equivalent " +
        "convention: ok=true means no contradictions; ok=false means findings exist.")]
    public async Task<string> FindContradictions(
        [Description("Chapter id (32-char hex). The chapter must exist in engine/data/chapters/<id>/chapter.json.")]
            string chapterId,
        [Description("Quorum requirement for the contradiction vote: plurality | simplemajority | twothirds | unanimous. Default plurality (most permissive — surfaces every voter's concerns).")]
            string quorum = "plurality",
        [Description("Max tokens per voter response. Default 4096. Larger values produce more thorough reports but cost more.")]
            int maxTokens = 4096,
        [Description("Hard cap on canon-context characters before the draft text is appended. Default 80000. Lower this if hitting provider context limits.")]
            int maxContextChars = 80000)
    {
        // The Node prototype lives at <repo-root>/tools/check-contradictions.js.
        // The MCP server runs from the repo root, so the relative path resolves.
        var scriptPath = Path.Combine("tools", "check-contradictions.js");
        var resolvedScriptPath = Path.GetFullPath(scriptPath);
        if (!File.Exists(resolvedScriptPath))
        {
            return JsonSerializer.Serialize(new
            {
                error = "contradiction_detector_script_not_found",
                expected_path = resolvedScriptPath,
                hint = "Make sure tools/check-contradictions.js exists in the StreetSamurai repo root and the MCP server's working directory is the repo root.",
            }, CanonTools.JsonOpts);
        }

        var psi = new ProcessStartInfo
        {
            FileName = "node",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        // Resolve the chapter from SQL Server and bundle book+chapters+characters
        // into a temp JSON file the Node script reads via --bundle-file. The legacy
        // disk layout was retired 2026-05-08; the Node script's only remaining job
        // is the Legion vote dispatch and finding extraction.
        var chapter = chapters.LoadChapter(chapterId);
        if (chapter == null)
        {
            return JsonSerializer.Serialize(new { error = "chapter_not_found", chapterId }, CanonTools.JsonOpts);
        }
        var bundlePath = WriteBundleForChapter(chapter);

        psi.ArgumentList.Add(resolvedScriptPath);
        psi.ArgumentList.Add(chapterId);
        psi.ArgumentList.Add("--bundle-file");
        psi.ArgumentList.Add(bundlePath);
        psi.ArgumentList.Add("--quorum");
        psi.ArgumentList.Add(quorum);
        psi.ArgumentList.Add("--max-tokens");
        psi.ArgumentList.Add(maxTokens.ToString());
        psi.ArgumentList.Add("--max-context-chars");
        psi.ArgumentList.Add(maxContextChars.ToString());

        try
        {
            using var proc = Process.Start(psi);
            if (proc == null)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "node_process_failed_to_start",
                    hint = "Ensure 'node' is on the PATH where the MCP server is running.",
                }, CanonTools.JsonOpts);
            }

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            try { File.Delete(bundlePath); } catch (Exception ex) { log.LogWarning(ex, "Failed to delete temporary bundle {Path}", bundlePath); }

            // The Node script returns a JSON report on stdout regardless of exit code.
            // Exit 0 = no findings, exit 1 = findings flagged, exit 2 = pipeline error.
            if (proc.ExitCode == 2)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "contradiction_detector_pipeline_error",
                    stderr = stderr,
                    stdout_preview = stdout.Length > 500 ? stdout[..500] : stdout,
                }, CanonTools.JsonOpts);
            }

            // Parse the Node script's output and re-emit it with an `ok` flag.
            try
            {
                using var doc = JsonDocument.Parse(stdout);
                var root = doc.RootElement;
                var findingCount = root.TryGetProperty("findings_count", out var fc) ? fc.GetInt32() : 0;
                return JsonSerializer.Serialize(new
                {
                    ok = findingCount == 0,
                    finding_count = findingCount,
                    report = JsonSerializer.Deserialize<JsonElement>(stdout),
                }, CanonTools.JsonOpts);
            }
            catch (JsonException)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "contradiction_detector_returned_non_json",
                    stdout_preview = stdout.Length > 1000 ? stdout[..1000] : stdout,
                    stderr = stderr,
                }, CanonTools.JsonOpts);
            }
        }
        catch (Exception ex)
        {
            try { File.Delete(bundlePath); } catch (Exception delEx) { log.LogWarning(delEx, "Failed to delete bundle during exception handling"); }
            return JsonSerializer.Serialize(new
            {
                error = "contradiction_detector_exception",
                detail = ex.Message,
            }, CanonTools.JsonOpts);
        }
    }

    /// <summary>
    /// Find contradictions across an entire book by running a pairwise sweep — every
    /// chapter is graded against the full prose of every other chapter (forward AND
    /// backward). Catches things a single-chapter check misses: a character who dies
    /// in chapter 3 but speaks in chapter 5, a stated age that drifts between
    /// chapters, etc. Cross-chapter findings are consolidated. Expensive — dispatches
    /// N Legion votes per book. Use synopsisOnly=true for cheaper triage.
    /// </summary>
    [McpServerTool, Description(
        "Find contradictions across an entire book by running a pairwise sweep — every chapter " +
        "is graded against the FULL PROSE of every OTHER chapter (forward AND backward). " +
        "Catches things a single-chapter check misses: a character who dies in chapter 3 but " +
        "speaks in chapter 5, a character revealed left-handed in chapter 6 catching a ball " +
        "right-handed in chapter 2, a stated age that drifts between chapters, etc. " +
        "Cross-chapter findings are consolidated so the same contradiction surfaces once with " +
        "all chapter numbers attached. Expensive — dispatches N Legion votes per book. Use " +
        "synopsisOnly=true for cheaper triage that skips prose-level facts. Returns a JSON " +
        "report with per-chapter findings and a consolidated cross-book finding list. " +
        "Exit-code-equivalent convention: ok=true means no contradictions; ok=false means findings exist.")]
    public async Task<string> FindContradictionsBook(
        [Description("Book id (32-char hex). The book must exist in engine/data/books/<id>.json with a non-empty chapter_ids list.")]
            string bookId,
        [Description("Quorum requirement for the contradiction vote: plurality | simplemajority | twothirds | unanimous. Default plurality (most permissive — surfaces every voter's concerns).")]
            string quorum = "plurality",
        [Description("Max tokens per voter response. Default 4096. Larger values produce more thorough reports but cost more.")]
            int maxTokens = 4096,
        [Description("Hard cap on canon-context characters per chapter pass. Default 0 = let the script choose (400000 with prose, 120000 with synopsisOnly). Lower this if hitting provider context limits.")]
            int maxContextChars = 0,
        [Description("If true, feed only chapter synopses (not full prose) as canon. Cheaper but misses prose-level facts like handedness or specific physical actions. Default false (prose included).")]
            bool synopsisOnly = false)
    {
        var scriptPath = Path.Combine("tools", "check-contradictions.js");
        var resolvedScriptPath = Path.GetFullPath(scriptPath);
        if (!File.Exists(resolvedScriptPath))
        {
            return JsonSerializer.Serialize(new
            {
                error = "contradiction_detector_script_not_found",
                expected_path = resolvedScriptPath,
                hint = "Make sure tools/check-contradictions.js exists in the StreetSamurai repo root and the MCP server's working directory is the repo root.",
            }, CanonTools.JsonOpts);
        }

        var psi = new ProcessStartInfo
        {
            FileName = "node",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        // Build the bundle from SQL Server; the legacy engine/data/books/<id>.json
        // layout was retired 2026-05-08. Node script reads canon from --bundle-file.
        var book = books.LoadBook(bookId);
        if (book == null)
        {
            return JsonSerializer.Serialize(new { error = "book_not_found", bookId }, CanonTools.JsonOpts);
        }
        var bundlePath = WriteBundleForBook(book);

        psi.ArgumentList.Add(resolvedScriptPath);
        psi.ArgumentList.Add(bookId);
        psi.ArgumentList.Add("--mode");
        psi.ArgumentList.Add("book");
        psi.ArgumentList.Add("--bundle-file");
        psi.ArgumentList.Add(bundlePath);
        psi.ArgumentList.Add("--quorum");
        psi.ArgumentList.Add(quorum);
        psi.ArgumentList.Add("--max-tokens");
        psi.ArgumentList.Add(maxTokens.ToString());
        if (maxContextChars > 0)
        {
            psi.ArgumentList.Add("--max-context-chars");
            psi.ArgumentList.Add(maxContextChars.ToString());
        }
        if (synopsisOnly)
        {
            psi.ArgumentList.Add("--synopsis-only");
        }

        try
        {
            using var proc = Process.Start(psi);
            if (proc == null)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "node_process_failed_to_start",
                    hint = "Ensure 'node' is on the PATH where the MCP server is running.",
                }, CanonTools.JsonOpts);
            }

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            try { File.Delete(bundlePath); } catch (Exception ex) { log.LogWarning(ex, "Failed to delete temporary bundle {Path}", bundlePath); }

            // Exit 0 = no findings, 1 = findings flagged, 2 = pipeline error.
            // The Node script emits per-chapter progress on stderr; only treat
            // exit code 2 as a hard failure.
            if (proc.ExitCode == 2)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "contradiction_detector_pipeline_error",
                    stderr = stderr,
                    stdout_preview = stdout.Length > 500 ? stdout[..500] : stdout,
                }, CanonTools.JsonOpts);
            }

            try
            {
                using var doc = JsonDocument.Parse(stdout);
                var root = doc.RootElement;
                // Book-mode reports use findings_total_consolidated (cross-chapter dedupe).
                // Fall back to findings_count for compatibility with chapter-mode shape.
                var findingCount =
                    root.TryGetProperty("findings_total_consolidated", out var ftc) ? ftc.GetInt32() :
                    root.TryGetProperty("findings_count", out var fc) ? fc.GetInt32() : 0;
                return JsonSerializer.Serialize(new
                {
                    ok = findingCount == 0,
                    finding_count = findingCount,
                    report = JsonSerializer.Deserialize<JsonElement>(stdout),
                }, CanonTools.JsonOpts);
            }
            catch (JsonException)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "contradiction_detector_returned_non_json",
                    stdout_preview = stdout.Length > 1000 ? stdout[..1000] : stdout,
                    stderr = stderr,
                }, CanonTools.JsonOpts);
            }
        }
        catch (Exception ex)
        {
            try { File.Delete(bundlePath); } catch (Exception delEx) { log.LogWarning(delEx, "Failed to delete bundle during exception handling"); }
            return JsonSerializer.Serialize(new
            {
                error = "contradiction_detector_exception",
                detail = ex.Message,
            }, CanonTools.JsonOpts);
        }
    }
}
