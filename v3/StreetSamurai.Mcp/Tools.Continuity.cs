using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Interfaces;

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
// LLMVotingService.ScoreAsync — that work is queued in the architecture
// proposal saved at memory/project_contradiction_detector.md.

[McpServerToolType]
public class ContinuityTools
{
    private readonly IPathProvider paths;

    public ContinuityTools(IPathProvider paths)
    {
        this.paths = paths;
    }

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
        psi.ArgumentList.Add(resolvedScriptPath);
        psi.ArgumentList.Add(chapterId);
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
            return JsonSerializer.Serialize(new
            {
                error = "contradiction_detector_exception",
                detail = ex.Message,
            }, CanonTools.JsonOpts);
        }
    }
}
