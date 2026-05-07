using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Interfaces;
using StreetSamurai.Core.Models;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Autonomous quality monitor. Subscribes to <see cref="IChapterRepository.OnChapterSaved"/>
/// (primary, post-SQL-cutover) and <see cref="EmbeddingIndexService.FileReindexed"/>
/// (legacy, for `ss --findings scan &lt;path&gt;`). For each chapter save it runs
/// scoped contradiction + cliché checks against the live corpus via
/// <see cref="LocalRagService"/>. Findings land in <see cref="FindingsService"/>
/// for user triage at /findings.
///
/// Cost: zero — local Qwen via Ollama. Throughput cap: <see cref="MaxConcurrent"/>
/// to keep the GPU from saturating during burst saves.
/// </summary>
public class ContinuousQualityService
{
    private const int MaxConcurrent = 1;

    private readonly EmbeddingIndexService index;
    private readonly LocalRagService rag;
    private readonly FindingsService findings;
    private readonly ILogger<ContinuousQualityService> log;

    private readonly SemaphoreSlim gate = new(MaxConcurrent, MaxConcurrent);
    private readonly ConcurrentDictionary<string, byte> inFlight = new();

    public bool Enabled { get; set; } = true;

    public ContinuousQualityService(
        EmbeddingIndexService index,
        LocalRagService rag,
        FindingsService findings,
        IChapterRepository chapters,
        ILogger<ContinuousQualityService> log)
    {
        this.index    = index;
        this.rag      = rag;
        this.findings = findings;
        this.log      = log;

        // Primary trigger — fires whenever the EF chapter repo commits a save.
        chapters.OnChapterSaved += OnChapterSaved;
        // Legacy trigger — kept so `ss --findings scan <path>` and any
        // ad-hoc filesystem rescans still hit the analyzer.
        index.FileReindexed += OnFileReindexed;
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

    private void OnFileReindexed(string path)
    {
        if (!Enabled) return;
        if (!IsChapterFile(path)) return;
        if (!inFlight.TryAdd(path, 0)) return;

        _ = Task.Run(async () =>
        {
            try
            {
                await gate.WaitAsync();
                try { await AnalyzeFileAsync(path); }
                finally { gate.Release(); }
            }
            catch (Exception ex) { log.LogWarning(ex, "Quality analysis failed for {Path}", path); }
            finally { inFlight.TryRemove(path, out _); }
        });
    }

    private static bool IsChapterFile(string path)
        => path.Contains(Path.DirectorySeparatorChar + "chapters" + Path.DirectorySeparatorChar,
                         StringComparison.OrdinalIgnoreCase)
           && Path.GetFileName(path).Equals("chapter.json", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Scan a chapter loaded from SQL. Used by the OnChapterSaved hook and
    /// by callers that already hold a Chapter (avoids a redundant DB read).
    /// </summary>
    public async Task AnalyzeChapterAsync(Chapter chapter, CancellationToken ct = default)
    {
        log.LogInformation("Quality scan: chapter {Id} '{Title}'", chapter.Id, chapter.Title);
        // Synthetic file_path so existing FindingsService rows stay queryable
        // (column is indexed on file_path; "chapter:<id>" is a stable key).
        var pseudoPath = "chapter:" + chapter.Id;
        var text = chapter.PlainText;
        await Task.WhenAll(
            ScanContradictionsTextAsync(pseudoPath, chapter.Id, text, ct),
            ScanClichesTextAsync(pseudoPath, chapter.Id, text, ct));
    }

    public async Task AnalyzeFileAsync(string filePath, CancellationToken ct = default)
    {
        log.LogInformation("Quality scan: {File}", filePath);
        await Task.WhenAll(
            ScanContradictionsAsync(filePath, ct),
            ScanClichesAsync(filePath, ct));
    }

    // ── Contradiction scan ──────────────────────────────────────────────────────

    private async Task ScanContradictionsAsync(string filePath, CancellationToken ct)
    {
        var chapterText = SafeRead(filePath);
        if (string.IsNullOrWhiteSpace(chapterText)) return;
        var chapterId = TryGetChapterId(chapterText);
        await ScanContradictionsTextAsync(filePath, chapterId, chapterText, ct);
    }

    private async Task ScanContradictionsTextAsync(string filePath, string? chapterId, string chapterText, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(chapterText)) return;

        var system =
            "You are a canon-consistency auditor for the StreetSamurai world. The user shows " +
            "you a chapter and the most relevant grounded context retrieved from other files. " +
            "Find statements in the chapter that contradict the context. Reply ONLY with a " +
            "JSON array; empty array [] if none. Each item: " +
            "{\"severity\":\"high|medium|low\",\"summary\":\"...\",\"snippet\":\"...\",\"fix\":\"...\"}. " +
            "Do not invent contradictions; if the chapter is consistent with the context, return [].";

        var question =
            "Audit this chapter for contradictions against the corpus. Chapter content:\n\n" +
            Truncate(chapterText, 12000);

        var hits = await index.SearchAsync(question, k: 8, ct);
        var answer = await rag.AnswerWithHitsAsync(question, hits, systemRole: system,
            maxTokens: 2048, temperature: 0.1, ct: ct);

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

    // ── Cliché / voice scan ─────────────────────────────────────────────────────

    private async Task ScanClichesAsync(string filePath, CancellationToken ct)
    {
        var chapterText = SafeRead(filePath);
        if (string.IsNullOrWhiteSpace(chapterText)) return;
        var chapterId = TryGetChapterId(chapterText);
        await ScanClichesTextAsync(filePath, chapterId, chapterText, ct);
    }

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

        var question = "Find clichés in this chapter:\n\n" + Truncate(chapterText, 12000);

        // Cliché scan doesn't need broad corpus retrieval — give it the chapter alone.
        var answer = await rag.AnswerWithHitsAsync(question, Array.Empty<SearchHit>(),
            systemRole: system, maxTokens: 1500, temperature: 0.2, ct: ct);

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

    private static FindingSeverity ParseSeverity(string? s) => (s ?? "").ToLowerInvariant() switch
    {
        "high"   => FindingSeverity.High,
        "medium" => FindingSeverity.Medium,
        "low"    => FindingSeverity.Low,
        _        => FindingSeverity.Medium,
    };

    private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max);
}
