using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Mcp;

// ── Local-LLM Q&A and findings inbox ────────────────────────────────────────
// Wraps the same EmbeddingIndexService / LocalRagService / FindingsService /
// FindingApplyService / ContinuousQualityService used by /ask and /findings,
// so any MCP client (Claude Code, Claude Desktop) can query the corpus and
// triage findings without opening the web UI.

/// <summary>
/// Local-LLM Q&amp;A tools. Wraps the same EmbeddingIndexService / LocalRagService
/// the /ask UI uses so any MCP client can query the live StreetSamurai corpus
/// (entity JSON, chapter prose, continuity claims) without opening the web UI.
/// Free, local, and always reads the current state of disk.
/// </summary>
[McpServerToolType]
public class AskTools
{
    private readonly EmbeddingIndexService index;
    private readonly LocalRagService rag;

    public AskTools(EmbeddingIndexService index, LocalRagService rag)
    {
        this.index = index;
        this.rag   = rag;
    }

    /// <summary>
    /// Ask a natural-language question against the live StreetSamurai corpus.
    /// Retrieves the top-k relevant chunks via embeddings, prepends them as
    /// context, and asks the local Qwen model for a grounded answer. Returns the
    /// answer plus the cited chunk paths. Free (local), private, and always reads
    /// the current state of disk — no stale snapshots.
    /// </summary>
    [McpServerTool, Description(
        "Ask a natural-language question against the live StreetSamurai corpus. " +
        "Retrieves the top-k relevant chunks (entity JSON, chapter prose, " +
        "continuity claims) via embeddings, prepends them as context, and asks " +
        "the local Qwen model for a grounded answer. Returns the answer plus " +
        "the cited chunk paths. Free (local), private, and always reads the " +
        "current state of disk — no stale snapshots.")]
    public async Task<string> AskCorpus(
        [Description("Natural-language question. Examples: 'What color is Sable's hair?', 'Where does Kyle eat?', 'What is Hua's title?'")]
            string question,
        [Description("How many corpus chunks to retrieve as context. Default 8.")]
            int retrieveK = 8)
    {
        if (string.IsNullOrWhiteSpace(question))
            return JsonSerializer.Serialize(new { error = "question is required" });

        if (!await index.OllamaReachableAsync())
            return JsonSerializer.Serialize(new
            {
                error = "ollama_unreachable",
                hint  = "Local Ollama is not responding at localhost:11434. Start it and pull qwen3:1.7b + bge-m3.",
            });

        var hits = await index.SearchAsync(question, retrieveK);
        var answer = await rag.AnswerWithHitsAsync(question, hits);

        return JsonSerializer.Serialize(new
        {
            ok = true,
            answer,
            citations = hits.Select(h => new
            {
                file = h.FilePath,
                chunkIndex = h.ChunkIndex,
                score = h.Score,
            }),
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Re-embed any files whose content has changed since last indexing. Normally
    /// the FileSystemWatcher keeps the index current automatically; use this when
    /// files were edited while the Blazor server was offline.
    /// </summary>
    [McpServerTool, Description(
        "Re-embed any files whose content has changed since last indexing. " +
        "Normally the FileSystemWatcher keeps the index current automatically; " +
        "use this when you've edited files while the Blazor server was offline.")]
    public async Task<string> ReindexCorpus()
    {
        var n = await index.ReindexAllAsync();
        var s = index.GetStats();
        return JsonSerializer.Serialize(new
        {
            ok = true,
            reembedded = n,
            files  = s.FileCount,
            chunks = s.ChunkCount,
            lastIndexed = s.LastIndexed,
        }, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Get current corpus index status: file count, chunk count, last-indexed
    /// timestamp, and whether the local Ollama server is reachable.
    /// </summary>
    [McpServerTool, Description(
        "Get current corpus index status: file count, chunk count, last-indexed " +
        "timestamp, and whether the local Ollama server is reachable.")]
    public async Task<string> CorpusStatus()
    {
        var s = index.GetStats();
        return JsonSerializer.Serialize(new
        {
            files  = s.FileCount,
            chunks = s.ChunkCount,
            lastIndexed = s.LastIndexed,
            ollamaUp = await index.OllamaReachableAsync(),
        }, new JsonSerializerOptions { WriteIndented = true });
    }
}

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
    /// auto-detects contradictions and clichés on every chapter save via local
    /// Qwen; results land here for triage. Sorted high-severity-first.
    /// </summary>
    [McpServerTool, Description(
        "List findings from the autonomous quality inbox. ContinuousQualityService " +
        "auto-detects contradictions and clichés on every chapter save via local " +
        "Qwen; results land here for triage. Sorted high-severity-first.")]
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
    /// chapter file via local Qwen. Normally the autonomous monitor runs this on
    /// every save; use this for ad-hoc rescans without modifying the file.
    /// </summary>
    [McpServerTool, Description(
        "Manually trigger a quality scan (contradiction + cliché) on a single " +
        "chapter file via local Qwen. Normally the autonomous monitor runs this " +
        "on every save; use this for ad-hoc rescans without modifying the file.")]
    public async Task<string> ScanChapterQuality(
        [Description("Absolute path to a chapter.json file.")] string filePath)
    {
        if (!File.Exists(filePath))
            return JsonSerializer.Serialize(new { error = "file_not_found", filePath });
        await monitor.AnalyzeFileAsync(filePath);
        return JsonSerializer.Serialize(new { ok = true, scanned = filePath });
    }
}
