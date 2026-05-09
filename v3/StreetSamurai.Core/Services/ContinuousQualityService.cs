using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Autonomous quality monitor. Subscribes to <see cref="IChapterRepository.OnChapterSaved"/>
/// and runs scoped contradiction + cliché checks on each saved chapter.
///
/// Grounding comes from SQL: for each chapter we resolve the entities the prose
/// mentions via <see cref="WorldGraphService"/> and pull dossiers via
/// <see cref="WorldStateService"/> so the contradiction prompt is anchored to
/// canon. The cliché scan needs no grounding — it goes straight to the LLM.
/// Findings land in <see cref="FindingsService"/> for triage at /findings.
/// </summary>
public class ContinuousQualityService
{
    private const int MaxConcurrent = 1;

    private readonly ILlmService llm;
    private readonly WorldGraphService graph;
    private readonly WorldStateService worldState;
    private readonly EmbeddingService embeddings;
    private readonly FindingsService findings;
    private readonly ILogger<ContinuousQualityService> log;

    private readonly SemaphoreSlim gate = new(MaxConcurrent, MaxConcurrent);
    private readonly ConcurrentDictionary<string, byte> inFlight = new();

    public bool Enabled { get; set; } = true;

    public ContinuousQualityService(
        ILlmService llm,
        WorldGraphService graph,
        WorldStateService worldState,
        EmbeddingService embeddings,
        FindingsService findings,
        IChapterRepository chapters,
        ILogger<ContinuousQualityService> log)
    {
        this.llm        = llm;
        this.graph      = graph;
        this.worldState = worldState;
        this.embeddings = embeddings;
        this.findings   = findings;
        this.log        = log;

        chapters.OnChapterSaved += OnChapterSaved;
    }

    private void OnChapterSaved(Chapter chapter)
    {
        if (!Enabled) return;
        if (chapter == null || string.IsNullOrEmpty(chapter.Id)) return;
        var key = "chapter:" + chapter.Id;
        if (!inFlight.TryAdd(key, 0)) return;

        _ = Task.Run(async () =>
        {
            try
            {
                await gate.WaitAsync();
                try { await AnalyzeChapterAsync(chapter); }
                finally { gate.Release(); }
            }
            catch (Exception ex) { log.LogWarning(ex, "Quality analysis failed for chapter {Id}", chapter.Id); }
            finally { inFlight.TryRemove(key, out _); }
        });
    }

    /// <summary>
    /// Scan a chapter loaded from SQL. Used by the OnChapterSaved hook and
    /// by callers that already hold a Chapter (avoids a redundant DB read).
    /// </summary>
    public async Task AnalyzeChapterAsync(Chapter chapter, CancellationToken ct = default)
    {
        log.LogInformation("Quality scan: chapter {Id} '{Title}'", chapter.Id, chapter.Title);
        var pseudoPath = "chapter:" + chapter.Id;
        var text = chapter.PlainText;
        await Task.WhenAll(
            ScanContradictionsTextAsync(pseudoPath, chapter.Id, text, ct),
            ScanClichesTextAsync(pseudoPath, chapter.Id, text, ct));
    }

    /// <summary>
    /// Legacy entry point. Reads a chapter.json file path and runs the same
    /// scan against it. Kept for the <c>ss --findings scan &lt;path&gt;</c> CLI.
    /// </summary>
    public async Task AnalyzeFileAsync(string filePath, CancellationToken ct = default)
    {
        log.LogInformation("Quality scan: {File}", filePath);
        var raw = SafeRead(filePath);
        if (string.IsNullOrWhiteSpace(raw)) return;
        var chapterId = TryGetChapterId(raw);
        var text = TryGetChapterText(raw) ?? raw;
        await Task.WhenAll(
            ScanContradictionsTextAsync(filePath, chapterId, text, ct),
            ScanClichesTextAsync(filePath, chapterId, text, ct));
    }

    // ── Contradiction scan ──────────────────────────────────────────────────────

    private async Task ScanContradictionsTextAsync(string filePath, string? chapterId, string chapterText, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(chapterText)) return;

        var grounding = await BuildGroundingContextAsync(chapterText, ct);

        var system =
            "You are a canon-consistency auditor for the StreetSamurai world. The user shows " +
            "you a chapter and dossier excerpts for the entities the chapter names. " +
            "Find statements in the chapter that contradict the dossiers. Reply ONLY with a " +
            "JSON array; empty array [] if none. Each item: " +
            "{\"severity\":\"high|medium|low\",\"summary\":\"...\",\"snippet\":\"...\",\"fix\":\"...\"}. " +
            "Do not invent contradictions; if the chapter is consistent with the dossiers, return [].";

        var prompt = new StringBuilder()
            .AppendLine("DOSSIERS (canon excerpts for entities mentioned):")
            .AppendLine(grounding)
            .AppendLine()
            .AppendLine("CHAPTER:")
            .AppendLine(Truncate(chapterText, 12000))
            .ToString();

        string answer;
        try { answer = await llm.GenerateAsync(system, prompt, temperature: 0.1, maxTokens: 2048, ct: ct); }
        catch (Exception ex) { log.LogWarning(ex, "Contradiction scan LLM call failed"); return; }

        foreach (var item in ParseJsonArray(answer))
        {
            var sev     = ParseSeverity(item.GetValueOrDefault("severity"));
            var summary = (item.GetValueOrDefault("summary") ?? "").Trim();
            if (string.IsNullOrWhiteSpace(summary)) continue;
            findings.Upsert(filePath, chapterId,
                FindingCategory.Contradiction, sev, summary,
                snippet: item.GetValueOrDefault("snippet"),
                suggestedFix: item.GetValueOrDefault("fix"));
        }
    }

    /// <summary>
    /// Build a dossier context block for the contradiction prompt by
    /// pulling the top-K most semantically related entities to the chapter
    /// text from <see cref="EmbeddingService"/>. Falls back to substring
    /// name matching against the in-memory graph when the embedding index
    /// is empty (cold start) or the embedding API is unavailable.
    /// </summary>
    private async Task<string> BuildGroundingContextAsync(string chapterText, CancellationToken ct)
    {
        // Path 1 (preferred): semantic retrieval via OpenAI embeddings.
        var hits = await embeddings.FindSimilarAsync(chapterText, k: 12, ct: ct);
        if (hits.Count > 0)
            return RenderDossiers(hits.Select(h => h.EntityName));

        // Path 2 (fallback): substring name match against the QuikGraph.
        // Used during the embedding-index cold start and when the API call
        // failed (logged inside EmbeddingService).
        var fallback = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in graph.AllNodes())
        {
            if (string.IsNullOrWhiteSpace(node.Name)) continue;
            if (node.Name.Length < 3) continue;
            if (chapterText.IndexOf(node.Name, StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (!seen.Add(node.Name)) continue;
            fallback.Add(node.Name);
            if (fallback.Count >= 12) break;
        }
        return fallback.Count == 0
            ? "(no canon entities mentioned)"
            : RenderDossiers(fallback);
    }

    private string RenderDossiers(IEnumerable<string> names)
    {
        var sb = new StringBuilder();
        var asOf = AsOfCursor.Current;
        foreach (var name in names)
        {
            var dossier = worldState.GetDossier(name, asOf);
            if (dossier == null) continue;
            sb.AppendLine(dossier.ToPromptString()).AppendLine();
        }
        return sb.Length == 0 ? "(no dossiers available)" : sb.ToString();
    }

    // ── Cliché / voice scan ─────────────────────────────────────────────────────

    private async Task ScanClichesTextAsync(string filePath, string? chapterId, string chapterText, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(chapterText)) return;

        var system =
            "You are a prose quality reviewer for the StreetSamurai world — a precise, " +
            "muscular, restrained register that avoids cyberpunk clichés (no neon-soaked, " +
            "no chrome-and-shadow, no rain-slicked, no jacked-in, no Matrix-ese). Find " +
            "specific cliché phrases or sentences in the chapter. Reply ONLY with a JSON " +
            "array; empty array [] if none. Each item: " +
            "{\"severity\":\"high|medium|low\",\"summary\":\"<phrase>\",\"snippet\":\"<sentence>\",\"fix\":\"<rewrite>\"}.";

        var prompt = "Find clichés in this chapter:\n\n" + Truncate(chapterText, 12000);

        string answer;
        try { answer = await llm.GenerateAsync(system, prompt, temperature: 0.2, maxTokens: 1500, ct: ct); }
        catch (Exception ex) { log.LogWarning(ex, "Cliché scan LLM call failed"); return; }

        foreach (var item in ParseJsonArray(answer))
        {
            var sev     = ParseSeverity(item.GetValueOrDefault("severity"));
            var summary = (item.GetValueOrDefault("summary") ?? "").Trim();
            if (string.IsNullOrWhiteSpace(summary)) continue;
            findings.Upsert(filePath, chapterId,
                FindingCategory.Cliche, sev, summary,
                snippet: item.GetValueOrDefault("snippet"),
                suggestedFix: item.GetValueOrDefault("fix"));
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static string SafeRead(string path)
    {
        try { return File.ReadAllText(path); }
        catch { return ""; }
    }

    private static string? TryGetChapterId(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                return id.GetString();
        }
        catch { }
        return null;
    }

    private static string? TryGetChapterText(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var key in new[] { "plain_text", "text", "body", "prose" })
                if (doc.RootElement.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                    return v.GetString();
        }
        catch { }
        return null;
    }

    private static IEnumerable<Dictionary<string, string?>> ParseJsonArray(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) yield break;
        var start = raw.IndexOf('[');
        var end   = raw.LastIndexOf(']');
        if (start < 0 || end < start) yield break;
        var slice = raw.Substring(start, end - start + 1);

        JsonDocument? doc = null;
        try { doc = JsonDocument.Parse(slice); }
        catch { yield break; }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Array) yield break;
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var d = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in item.EnumerateObject())
                    d[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString()
                        : prop.Value.ToString();
                yield return d;
            }
        }
    }

    private static FindingSeverity ParseSeverity(string? raw) => (raw ?? "").ToLowerInvariant() switch
    {
        "high" or "critical" => FindingSeverity.High,
        "low"                => FindingSeverity.Low,
        _                    => FindingSeverity.Medium,
    };

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..(n - 1)] + "…";
}
