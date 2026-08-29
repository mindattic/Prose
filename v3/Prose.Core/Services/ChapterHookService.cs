using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

public record ChapterHookResult(int ChapterIndex, string ChapterTitle, string HookType, int Strength, string Rationale);
public record ChapterHookReport(string NodeCode, int ChaptersAudited, int WeakEndings, IReadOnlyList<ChapterHookResult> Results);

/// <summary>
/// Chapter-hook / cliffhanger strength analysis (2026-08-28 — previously nonexistent anywhere
/// in the engine). Classifies each chapter's FINAL passage: what kind of hook does it end on
/// (question / danger / decision / revelation / arrival / none) and how strong is the pull to
/// turn the page (0-3). A non-final chapter ending with no hook (strength 0-1) is a craft
/// finding; the last chapter is exempt (endings are allowed to end).
///
/// Findings: "HOOK " prefix, FindingCategory.CraftChecklist, FilePath "node:{slug}" — loops
/// back into generation via ProseWriterRouter's findings-guidance mechanism. One Haiku call
/// per book (all chapter tails batched); also invoked per chapter at chapter close.
/// </summary>
public class ChapterHookService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly FindingsService findings;
    private readonly ILogger<ChapterHookService> log;

    private const int TailChars = 3_000;
    private const string HookPrefix = "HOOK ";

    public ChapterHookService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILlmService llm,
        FindingsService findings,
        ILogger<ChapterHookService> log)
    {
        this.dbFactory = dbFactory;
        this.llm = llm;
        this.findings = findings;
        this.log = log;
    }

    private const string System = """
You evaluate chapter endings for page-turn pull. For each chapter's FINAL passage below,
classify the hook it ends on:
- hookType: one of question | danger | decision | revelation | arrival | emotional | none
- strength: 0 (no forward pull — summary, reflection, or a scene that simply stops),
  1 (mild — a faint open thread), 2 (solid — a clear unresolved pressure),
  3 (strong — the reader cannot reasonably stop here)
- rationale: one short clause naming what does (or fails to do) the pulling.

Judge only the ending as written — do not invent context. Output STRICT JSON, no fences:
{"items":[{"ref":N,"hookType":"...","strength":N,"rationale":"..."}]}
""";

    /// <summary>Audit every chapter of a book in one batched call.</summary>
    public async Task<ChapterHookReport> AuditAsync(
        string slugOrCode, bool dryRun = false, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(
            n => n.Slug == slugOrCode || (n.NodeCode != null && n.NodeCode.ToUpper() == slugOrCode.ToUpper()), ct)
            ?? throw new InvalidOperationException($"Node not found: {slugOrCode}");
        var nodeCode = node.NodeCode?.ToUpperInvariant() ?? node.Slug.ToUpperInvariant();
        var fp = $"node:{node.Slug}";

        var searchIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, node.Id, ct);
        var beatRows = await (
            from bn in db.BeatNodes.AsNoTracking()
            join b in db.Beats.AsNoTracking() on bn.BeatId equals b.Id
            join c in db.Nodes.AsNoTracking() on bn.NodeId equals c.Id
            where searchIds.Contains(bn.NodeId) && b.Text != null && b.Text != ""
            orderby c.SortKey, bn.SortKey
            select new { ChapterId = c.Id, ChapterTitle = c.Title, ChapterSort = c.SortKey, Text = b.Text! }
        ).ToListAsync(ct);
        if (beatRows.Count == 0) return new ChapterHookReport(nodeCode, 0, 0, []);

        // Final passage per chapter = tail of the concatenated chapter prose.
        var chapters = beatRows
            .GroupBy(r => new { r.ChapterId, r.ChapterTitle, r.ChapterSort })
            .OrderBy(g => g.Key.ChapterSort)
            .Select((g, idx) => new
            {
                Index = idx,
                g.Key.ChapterTitle,
                Tail = Tail(BeatMarkup.StripEntityTags(string.Join("\n\n", g.Select(r => r.Text)))),
            })
            .ToList();

        var sb = new StringBuilder();
        foreach (var chapter in chapters)
        {
            sb.AppendLine($"[ref {chapter.Index} · {chapter.ChapterTitle}]");
            sb.AppendLine(chapter.Tail);
            sb.AppendLine();
        }

        var results = new List<ChapterHookResult>();
        try
        {
            var raw = await llm.GenerateAsync(System, sb.ToString(), temperature: 0.1,
                maxTokens: 1600, model: LlmModels.Haiku, ct: ct);
            foreach (var (refIdx, hookType, strength, rationale) in ParseHooks(raw))
            {
                var chapter = chapters.FirstOrDefault(c => c.Index == refIdx);
                if (chapter == null) continue;
                results.Add(new ChapterHookResult(refIdx, chapter.ChapterTitle, hookType, strength, rationale));
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[ChapterHook] audit call failed for {Code}", nodeCode);
            return new ChapterHookReport(nodeCode, 0, 0, []);
        }

        if (!dryRun) findings.DeleteBySummaryPrefix(fp, HookPrefix);
        int weak = 0;
        var lastIndex = chapters.Count - 1;
        foreach (var r in results.OrderBy(r => r.ChapterIndex))
        {
            if (r.ChapterIndex == lastIndex) continue; // the book is allowed to end
            if (r.Strength <= 1)
            {
                weak++;
                if (!dryRun) findings.Upsert(fp, chapterId: null,
                    FindingCategory.CraftChecklist,
                    r.Strength == 0 ? FindingSeverity.Medium : FindingSeverity.Low,
                    $"{HookPrefix}chapter '{r.ChapterTitle}' ends with {(r.Strength == 0 ? "no hook" : "only a faint hook")} ({r.HookType}) — {r.Rationale}",
                    snippet: null,
                    suggestedFix: "End the chapter one beat earlier or later — on the unresolved pressure, not after it settles.");
            }
        }

        log.LogInformation("[ChapterHook] {Code}: {Count} chapters audited, {Weak} weak endings", nodeCode, results.Count, weak);
        return new ChapterHookReport(nodeCode, results.Count, weak, results);
    }

    /// <summary>One-chapter check for the chapter-close pipeline. Returns null on any failure —
    /// never blocks a chapter close.</summary>
    public async Task<ChapterHookResult?> CheckCloseAsync(
        string chapterProse, int chapterIndex, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(chapterProse)) return null;
        try
        {
            var user = $"[ref 0 · chapter {chapterIndex + 1}]\n{Tail(BeatMarkup.StripEntityTags(chapterProse))}";
            var raw = await llm.GenerateAsync(System, user, temperature: 0.1,
                maxTokens: 200, model: LlmModels.Haiku, ct: ct);
            var parsed = ParseHooks(raw).FirstOrDefault();
            return parsed == default ? null
                : new ChapterHookResult(chapterIndex, $"chapter {chapterIndex + 1}", parsed.HookType, parsed.Strength, parsed.Rationale);
        }
        catch (OperationCanceledException) { throw; }
        catch { return null; }
    }

    /// <summary>Chapter-close integration: classify this chapter's ending and, when weak
    /// (strength ≤ 1) and not obviously the book's final chapter, file the same "HOOK "
    /// finding the batch audit files. Non-fatal; returns the classification or null.
    /// <paramref name="totalChapters"/>: when known, suppresses filing for the book's last
    /// chapter (0-based <paramref name="chapterIndex"/> == totalChapters - 1) — the same
    /// "the book is allowed to end" exemption <see cref="AuditAsync"/> applies to its own
    /// last chapter. Omit when the caller doesn't know the book's total chapter count yet.</summary>
    public async Task<ChapterHookResult?> CheckCloseAndFileAsync(
        Guid parentNodeId, int chapterIndex, string chapterProse, int? totalChapters = null, CancellationToken ct = default)
    {
        var result = await CheckCloseAsync(chapterProse, chapterIndex, ct);
        if (result == null || result.Strength > 1) return result;
        if (totalChapters.HasValue && chapterIndex == totalChapters.Value - 1) return result;
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            var slug = await db.Nodes.AsNoTracking()
                .Where(n => n.Id == parentNodeId).Select(n => n.Slug).FirstOrDefaultAsync(ct);
            if (string.IsNullOrEmpty(slug)) return result;
            findings.Upsert($"node:{slug}", chapterId: null,
                FindingCategory.CraftChecklist,
                result.Strength == 0 ? FindingSeverity.Medium : FindingSeverity.Low,
                $"{HookPrefix}chapter {chapterIndex + 1} closed with {(result.Strength == 0 ? "no hook" : "only a faint hook")} ({result.HookType}) — {result.Rationale}",
                snippet: null,
                suggestedFix: "End the chapter one beat earlier or later — on the unresolved pressure, not after it settles.");
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[ChapterHook] close-finding filing failed for node {NodeId} ch {Index}", parentNodeId, chapterIndex);
        }
        return result;
    }

    private static string Tail(string text) =>
        text.Length <= TailChars ? text : text[^TailChars..];

    internal static List<(int Ref, string HookType, int Strength, string Rationale)> ParseHooks(string raw)
    {
        var results = new List<(int, string, int, string)>();
        try
        {
            using var doc = JsonDocument.Parse(JsonDefaults.StripCodeFences(raw));
            if (!doc.RootElement.TryGetProperty("items", out var arr)) return results;
            foreach (var el in arr.EnumerateArray())
            {
                try
                {
                    if (!el.TryGetProperty("ref", out var refEl) || refEl.ValueKind != JsonValueKind.Number) continue;
                    var hookType = el.TryGetProperty("hookType", out var h) ? h.GetString() ?? "none" : "none";
                    var strength = el.TryGetProperty("strength", out var s) && s.ValueKind == JsonValueKind.Number
                        ? Math.Clamp(s.GetInt32(), 0, 3) : 0;
                    var rationale = el.TryGetProperty("rationale", out var r) ? r.GetString() ?? "" : "";
                    results.Add((refEl.GetInt32(), hookType, strength, rationale));
                }
                catch { /* skip malformed entry */ }
            }
        }
        catch { /* malformed JSON — return what parsed */ }
        return results;
    }
}
