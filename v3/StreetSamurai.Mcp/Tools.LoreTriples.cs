using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Mcp;

// ── Lore-triple extraction & contradiction resolution ──────────────────────
// extract_lore_triples pulls atomic (entity, predicate, object) claims out of
// a chapter's prose using a Legion Quorum vote, validates each triple's
// snippet against the source, and upserts into the per-entity continuity
// store at engine/data/continuity/<entity_id>.json. Conflicting triples get
// marked CONTRADICTED; resolve_contradiction picks a winner (A | B | custom)
// and records the resolution without destroying audit history.
//
// Implementation note: shells out to the Node prototype at
// tools/extract-lore-triples.js for the same reasons as ContinuityTools — the
// Node script is the canonical reference implementation and runs the same
// Legion CLI underneath. A C# inlining can come later.

[McpServerToolType]
public class LoreTripleTools
{
    private readonly IPathProvider paths;

    public LoreTripleTools(IPathProvider paths)
    {
        this.paths = paths;
    }

    [McpServerTool, Description(
        "Extract atomic lore triples from a chapter's prose using a Legion Quorum vote. " +
        "Each triple is validated against the source prose (snippet must exist) and " +
        "upserted into the per-entity continuity store. Returns a diff: new / confirmed / " +
        "contradicted / unknown_entity. ok=true when no contradictions were created.")]
    public async Task<string> ExtractLoreTriples(
        [Description("Chapter id (32-char hex). The chapter must exist in engine/data/chapters/<id>/chapter.json.")]
            string chapterId,
        [Description("Quorum requirement: plurality | simplemajority | twothirds | unanimous. Default plurality.")]
            string quorum = "plurality",
        [Description("Max tokens per voter response. Default 4096.")]
            int maxTokens = 4096,
        [Description("Minimum voters that must propose a triple for it to be stored. Default 1. Raise to 2+ for stricter Quorum filtering.")]
            int minVoters = 1)
        => await ShellOut(new[]
        {
            chapterId,
            "--quorum", quorum,
            "--max-tokens", maxTokens.ToString(),
            "--min-voters", minVoters.ToString(),
        }, expectFindings: true);

    [McpServerTool, Description(
        "Extract lore triples from every chapter in a book — pairwise upsert into the " +
        "continuity store, surfacing every new / confirmed / contradicted triple across " +
        "the entire book. Expensive (one Legion vote per chapter). Returns a per-chapter " +
        "and total diff. ok=true when no contradictions were created.")]
    public async Task<string> ExtractLoreTriplesFromBook(
        [Description("Book id (32-char hex). The book must exist in engine/data/books/<id>.json with a non-empty chapter_ids list.")]
            string bookId,
        [Description("Quorum requirement: plurality | simplemajority | twothirds | unanimous. Default plurality.")]
            string quorum = "plurality",
        [Description("Max tokens per voter response. Default 4096.")]
            int maxTokens = 4096,
        [Description("Minimum voters that must propose a triple for it to be stored. Default 1.")]
            int minVoters = 1)
        => await ShellOut(new[]
        {
            bookId,
            "--mode", "book",
            "--quorum", quorum,
            "--max-tokens", maxTokens.ToString(),
            "--min-voters", minVoters.ToString(),
        }, expectFindings: true);

    [McpServerTool, Description(
        "List lore triples in the continuity store. Optional filters: entity (id or name), predicate, status. " +
        "Status values: NEW | CONFIRMED | CONTRADICTED | CANONICAL | REJECTED | SUPERSEDED.")]
    public async Task<string> GetLoreTriples(
        [Description("Optional: entity id or name to filter to one entity.")]
            string entity = "",
        [Description("Optional: predicate name to filter (e.g. weapon_carry_location).")]
            string predicate = "",
        [Description("Optional: status filter — NEW | CONFIRMED | CONTRADICTED | CANONICAL | REJECTED | SUPERSEDED.")]
            string status = "")
    {
        var args = new List<string> { "--mode", "list" };
        if (!string.IsNullOrWhiteSpace(entity))    { args.Add("--entity");    args.Add(entity); }
        if (!string.IsNullOrWhiteSpace(predicate)) { args.Add("--predicate"); args.Add(predicate); }
        if (!string.IsNullOrWhiteSpace(status))    { args.Add("--status");    args.Add(status); }
        return await ShellOut(args.ToArray(), expectFindings: false);
    }

    [McpServerTool, Description(
        "List every CONTRADICTED lore triple awaiting resolution. Each entry pairs with " +
        "at least one other triple via its `contradicts` field. Use resolve_contradiction " +
        "to pick a winner. ok=true when zero contradictions remain.")]
    public async Task<string> ListUnresolvedContradictions()
        => await ShellOut(new[] { "--mode", "contradictions" }, expectFindings: true);

    [McpServerTool, Description(
        "Resolve a contradiction between two lore triples. Winner = A | B (one triple " +
        "wins, the other becomes REJECTED) or `custom` (both are rejected and a new " +
        "writer-asserted CANONICAL triple takes their place — pass customObject with " +
        "the agreed value). The full audit trail is preserved.")]
    public async Task<string> ResolveContradiction(
        [Description("Triple A id (the first conflicting triple).")]
            string tripleAId,
        [Description("Triple B id (the second conflicting triple). Must belong to the same entity as A.")]
            string tripleBId,
        [Description("Winner: A | B | custom. A or B promotes that triple to CANONICAL; custom rejects both and inserts a new writer-asserted triple.")]
            string winner,
        [Description("Required when winner=custom: the agreed-upon value to record as CANONICAL.")]
            string customObject = "",
        [Description("Optional resolution note (why this resolution was chosen — appears in the audit trail).")]
            string note = "")
    {
        var args = new List<string>
        {
            "--mode", "resolve",
            "--triple-a", tripleAId,
            "--triple-b", tripleBId,
            "--winner", winner,
        };
        if (!string.IsNullOrWhiteSpace(customObject)) { args.Add("--custom-object"); args.Add(customObject); }
        if (!string.IsNullOrWhiteSpace(note))         { args.Add("--note");          args.Add(note); }
        return await ShellOut(args.ToArray(), expectFindings: false);
    }

    // ── shared shell-out plumbing ──────────────────────────────────────────────

    private async Task<string> ShellOut(string[] extraArgs, bool expectFindings)
    {
        var scriptPath = Path.Combine("tools", "extract-lore-triples.js");
        var resolvedScriptPath = Path.GetFullPath(scriptPath);
        if (!File.Exists(resolvedScriptPath))
        {
            return JsonSerializer.Serialize(new
            {
                error = "lore_triple_extractor_script_not_found",
                expected_path = resolvedScriptPath,
                hint = "Make sure tools/extract-lore-triples.js exists in the StreetSamurai repo root and the MCP server's working directory is the repo root.",
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
        foreach (var a in extraArgs) psi.ArgumentList.Add(a);

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

            // Exit 2 = pipeline error; 0 = clean; 1 = contradictions present.
            if (proc.ExitCode == 2)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "lore_triple_extractor_pipeline_error",
                    stderr = stderr,
                    stdout_preview = stdout.Length > 500 ? stdout[..500] : stdout,
                }, CanonTools.JsonOpts);
            }

            try
            {
                using var doc = JsonDocument.Parse(stdout);
                var root = doc.RootElement;

                // For extraction modes: ok=true if no contradictions surfaced.
                // For list / resolve modes: just pass through the report.
                bool ok = true;
                int contradictionCount = 0;
                if (expectFindings)
                {
                    if (root.TryGetProperty("contradictions", out var cc))           contradictionCount = cc.GetInt32();
                    else if (root.TryGetProperty("count", out var cn))               contradictionCount = cn.GetInt32();
                    else if (root.TryGetProperty("totals", out var tt) && tt.TryGetProperty("contradicted", out var tc))
                                                                                     contradictionCount = tc.GetInt32();
                    ok = contradictionCount == 0;
                }

                return JsonSerializer.Serialize(new
                {
                    ok,
                    contradictions = expectFindings ? contradictionCount : (int?)null,
                    report = JsonSerializer.Deserialize<JsonElement>(stdout),
                }, CanonTools.JsonOpts);
            }
            catch (JsonException)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "lore_triple_extractor_returned_non_json",
                    stdout_preview = stdout.Length > 1000 ? stdout[..1000] : stdout,
                    stderr = stderr,
                }, CanonTools.JsonOpts);
            }
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = "lore_triple_extractor_exception",
                detail = ex.Message,
            }, CanonTools.JsonOpts);
        }
    }
}
