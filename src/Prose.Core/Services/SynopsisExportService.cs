using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Chapter segmentation of a book's live prose (<see cref="GetChapterSourcesAsync"/>, free, no
/// LLM), plus an on-demand per-chapter what-happens synopsis for the audits that diff against one.
///
/// <para>Nothing here is STORED (author ruling 2026-09-22: the book is the beats, drawing on
/// entities — no stored summary or retelling of the story). Until then every synopsis was
/// persisted to <c>NodeChapterSummaries</c> and written to the export folder as
/// <c>story-synopsis.txt</c>; both are gone. <see cref="GetChapterSummariesAsync"/> now generates
/// fresh each call, so its caller pays for it — only the (deactivated) comprehension probe calls it.</para>
/// </summary>
public sealed class SynopsisExportService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILlmService llm,
    ILogger<SynopsisExportService> log)
{
    // Synopses feed the altitude audit, so fate/motive fidelity is the job — Haiku
    // repeatedly upgraded "stopped" to "killed" and inferred motives (RTR, 2026-07-18);
    // Sonnet holds the fidelity rules.
    private const string SynopsisModel = "claude-sonnet-5";
    private const int MaxSourceChars = 180_000;

    /// <summary>One chapter's live prose, in reading order — shared with
    /// <see cref="ComprehensionProbeService"/> so probes and synopses always segment
    /// the book identically (same indexes, same source text, same cache keys).</summary>
    public sealed record ChapterUnit(Guid NodeId, int Index, string Title, string SourceText, int BeatCount);

    /// <summary>Public access to the chapter segmentation (no LLM calls).</summary>
    public Task<List<ChapterUnit>> GetChapterSourcesAsync(Guid bookNodeId, CancellationToken ct = default) =>
        LoadChapterUnitsAsync(bookNodeId, ct);

    /// <summary>Generates a synopsis (plus its structured facts JSON) for every chapter of the
    /// book, in reading order. Never stored — one LLM call per chapter, every call.</summary>
    public async Task<List<(int Index, string Title, string Synopsis, string FactsJson)>> GetChapterSummariesAsync(
        Guid bookNodeId, CancellationToken ct = default)
    {
        var chapters = await LoadChapterUnitsAsync(bookNodeId, ct);
        var sections = new List<(int Index, string Title, string Synopsis, string FactsJson)>(chapters.Count);
        foreach (var ch in chapters)
        {
            ct.ThrowIfCancellationRequested();
            var (synopsis, factsJson) = await GenerateAsync(ch, ct);
            if (string.IsNullOrWhiteSpace(synopsis) || synopsis.Length < 200)
            {
                log.LogWarning("Synopsis: chapter '{Title}' came back {Chars} chars — skipped.", ch.Title, synopsis?.Length ?? 0);
                continue;
            }
            sections.Add((ch.Index, ch.Title, synopsis, factsJson));
            // Gentle pacing — bulk runs tripped the provider circuit breaker at full speed.
            await Task.Delay(750, ct);
        }
        return sections;
    }

    // ── chapter loading ──────────────────────────────────────────────────────

    private async Task<List<ChapterUnit>> LoadChapterUnitsAsync(Guid bookNodeId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        // Recurses past any nested Collection to the actual leaf chapters, in reading order
        // (2026-08-09 fix) — a direct-children-only query here silently dropped a split
        // chapter's sub-chapters from the synopsis (used for chapter-altitude planning/review).
        // IMPORTANT: re-sort by fetching-then-OrderBy(SortKey) would be WRONG here — SortKey is
        // only comparable among siblings under the SAME parent, not globally across branches, so
        // re-sorting leaves from different parents by raw SortKey scrambles cross-branch order.
        // leafIds is already in correct global reading order (depth-first, SortKey per level) —
        // preserve THAT order by looking titles up into it, not by re-querying with OrderBy.
        var leafIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, bookNodeId, ct);
        var isFlatBook = leafIds.Count == 1 && leafIds[0] == bookNodeId;
        List<(Guid Id, string? Title)> chapterNodes = [];
        if (!isFlatBook)
        {
            // IgnoreQueryFilters(): leafIds is an already-resolved explicit id set (from
            // GetLeafDescendantIdsAsync, itself IgnoreQueryFilters-safe) — re-filtering by ambient
            // scope here would silently drop titles for a cross-universe book (same bug class
            // found and fixed in BookArchiveService.ArchiveAsync, 2026-08-17).
            var titleById = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
                .Where(n => leafIds.Contains(n.Id))
                .Select(n => new { n.Id, n.Title })
                .ToDictionaryAsync(n => n.Id, n => n.Title, ct);
            chapterNodes = leafIds.Select(id => (id, titleById.GetValueOrDefault(id))).ToList();
        }

        // Flat book (no chapter children): the book node is one unit.
        var sources = chapterNodes.Count > 0
            ? chapterNodes.Select(c => (c.Id, Title: c.Title ?? "")).ToList()
            : new List<(Guid Id, string Title)> { (bookNodeId, "") };

        var units = new List<ChapterUnit>();
        for (int i = 0; i < sources.Count; i++)
        {
            var beats = (await db.BeatNodes.AsNoTracking()
                .Where(bn => bn.NodeId == sources[i].Id)
                .OrderBy(bn => bn.SortKey)
                .Select(bn => bn.Beat!.Text)
                .ToListAsync(ct))
                .Select(BeatMarkup.StripEntityTags)
                .ToList();

            var text = string.Join("\n\n", beats.Where(t => !string.IsNullOrWhiteSpace(t)));
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (text.Length > MaxSourceChars) text = text[..MaxSourceChars] + "\n[SOURCE TRUNCATED]";

            var title = string.IsNullOrWhiteSpace(sources[i].Title)
                ? (sources.Count == 1 ? "The Book" : $"Chapter {i + 1}")
                : sources[i].Title;
            units.Add(new ChapterUnit(sources[i].Id, i, title, text, beats.Count));
        }
        return units;
    }

    private async Task<(string Synopsis, string FactsJson)> GenerateAsync(ChapterUnit ch, CancellationToken ct)
    {
        // Word budget scales with chapter size — a 14-beat single-chapter book compressed
        // to 180 words loses fates and stages, which manufactures false audit findings.
        var budget = Math.Clamp(120 + 12 * ch.BeatCount, 150, 450);

        const string systemTemplate = """
            You summarize one chapter of a novel so its author can review the whole book at
            chapter altitude. Write WHAT HAPPENS: concrete events, decisions, reveals, and
            consequences, in the order they occur. Spoilers are required. No evaluation, no
            marketing tone, no rhetorical questions. [WORD-BUDGET] words.
            FIDELITY RULES: state outcomes exactly as the text renders them — never upgrade
            "stopped/wounded/down" to "killed", never infer a motive the text doesn't state
            (if the text shows an accident or a misread, do not recast it as intent). When a
            fate or motive is explicit, mirror its wording. End with one sentence stating the
            explicit final fate of every named character who was harmed, captured, or
            neutralized in this chapter (alive/wounded/dead/stopped — exactly as the text has
            it). Your summary is the ground truth a reader's comprehension is checked against, so precision on
            fates, motives, and counts is the job.
            Return STRICT JSON only, no markdown fence:
            {"synopsis":"...","facts":{"entities":["..."],"locations":["..."],"events":["..."],"state_changes":["..."]}}
            facts.state_changes = durable changes to the world or cast (deaths, injuries,
            relationship shifts, items gained/lost, secrets exposed).
            """;
        var system = systemTemplate.Replace("[WORD-BUDGET]", $"{budget - 40}-{budget}");

        // 4k output budget: thinking-tier models spend tokens on reasoning BEFORE the text
        // block — complex chapters burned the whole 1200 on thinking and returned 0 chars.
        var raw = await llm.GenerateAsync(system, $"CHAPTER: {ch.Title}\n\n{ch.SourceText}",
            temperature: 0.2, maxTokens: 4000, model: SynopsisModel, ct: ct);

        raw = raw.Trim();
        if (raw.StartsWith("```"))
            raw = Regex.Replace(Regex.Replace(raw, @"^```(json)?\s*", ""), @"\s*```$", "");

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var synopsis = doc.RootElement.GetProperty("synopsis").GetString() ?? "";
            var factsJson = doc.RootElement.TryGetProperty("facts", out var f) ? f.GetRawText() : "{}";
            if (!string.IsNullOrWhiteSpace(synopsis)) return (synopsis, factsJson);
        }
        catch (JsonException)
        {
            log.LogWarning("Synopsis: non-JSON response for chapter {Title}; using raw text", ch.Title);
        }
        return (raw, "{}");
    }
}
