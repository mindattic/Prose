using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Chapter-by-chapter synopsis layer — the middle altitude of the three-tier story
/// understanding (story = bible/blueprint, chapter = this, beat = prose).
///
/// For every chapter of a story node, generates a concrete what-happens summary from
/// the live prose (events, decisions, reveals — spoiler-complete, no marketing tone),
/// persists it to <see cref="NodeChapterSummary"/> (content-hash cached, so unchanged
/// chapters never re-bill), and writes the assembled <c>story-synopsis.txt</c> into the
/// story's export folder beside its .docx/.epub/.pdf exports. Runs as part of
/// <c>ss --export-node</c> and standalone via <c>ss --export-synopsis</c>.
/// </summary>
public sealed class SynopsisExportService(
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    ILlmService llm,
    SettingsService settings,
    ILogger<SynopsisExportService> log)
{
    // Synopses feed the altitude audit, so fate/motive fidelity is the job — Haiku
    // repeatedly upgraded "stopped" to "killed" and inferred motives (RTR, 2026-07-18);
    // Sonnet holds the fidelity rules. Cost is per changed chapter only (hash cache).
    private const string SynopsisModel = "claude-sonnet-5";
    private const int MaxSourceChars = 180_000;

    private sealed record ChapterUnit(Guid NodeId, int Index, string Title, string SourceText, int BeatCount);

    /// <summary>Generates/refreshes all chapter summaries for the story (content-hash
    /// cached in NodeChapterSummaries) and returns them in reading order. This is the
    /// chapter-altitude view — consumed by story-synopsis.txt and the altitude audit.</summary>
    public async Task<List<(int Index, string Title, string Synopsis)>> GetChapterSummariesAsync(
        Guid storyNodeId, bool force = false, CancellationToken ct = default)
    {
        var chapters = await LoadChapterUnitsAsync(storyNodeId, ct);
        var sections = new List<(int Index, string Title, string Synopsis)>(chapters.Count);
        foreach (var ch in chapters)
        {
            ct.ThrowIfCancellationRequested();
            var synopsis = await GetOrGenerateAsync(storyNodeId, ch, force, ct);
            sections.Add((ch.Index, ch.Title, synopsis));
        }
        return sections;
    }

    /// <summary>Generates/refreshes all chapter summaries for the story and writes
    /// story-synopsis.txt to its publish folder. Returns the file path, or null when
    /// the story has no enabled prose.</summary>
    public async Task<string?> ExportAsync(Guid storyNodeId, bool force = false, CancellationToken ct = default)
    {
        var sections = await GetChapterSummariesAsync(storyNodeId, force, ct);
        if (sections.Count == 0) return null;

        string storyTitle;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
            storyTitle = await db.Nodes.AsNoTracking().Where(n => n.Id == storyNodeId)
                .Select(n => n.Title).FirstAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine(storyTitle.ToUpperInvariant());
        sb.AppendLine($"Chapter-by-chapter synopsis — generated {DateTime.UtcNow:yyyy-MM-dd} from the live prose.");
        sb.AppendLine(new string('=', 72));
        foreach (var (_, title, synopsis) in sections)
        {
            sb.AppendLine();
            sb.AppendLine(title);
            sb.AppendLine(new string('-', Math.Min(72, title.Length)));
            sb.AppendLine(synopsis.Trim());
        }

        var dir = await NodePublishDirAsync(storyNodeId, ct);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "story-synopsis.txt");
        // BOM on purpose: this .txt is opened by humans in arbitrary Windows editors,
        // and the em-dash-heavy prose garbles under an ANSI fallback.
        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(true), ct);
        log.LogInformation("Synopsis: wrote {Count} chapter(s) to {Path}", sections.Count, path);
        return path;
    }

    // ── chapter loading ──────────────────────────────────────────────────────

    private async Task<List<ChapterUnit>> LoadChapterUnitsAsync(Guid storyNodeId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var chapterNodes = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == storyNodeId)
            .OrderBy(n => n.SortKey)
            .Select(n => new { n.Id, n.Title })
            .ToListAsync(ct);

        // Flat story (no chapter children): the story node is one unit.
        var sources = chapterNodes.Count > 0
            ? chapterNodes.Select(c => (c.Id, Title: c.Title ?? "")).ToList()
            : new List<(Guid Id, string Title)> { (storyNodeId, "") };

        var units = new List<ChapterUnit>();
        for (int i = 0; i < sources.Count; i++)
        {
            var beats = await db.BeatNodes.AsNoTracking()
                .Where(bn => bn.NodeId == sources[i].Id && bn.IsEnabled)
                .OrderBy(bn => bn.SortKey)
                .Select(bn => bn.Beat!.Text)
                .ToListAsync(ct);

            var text = string.Join("\n\n", beats.Where(t => !string.IsNullOrWhiteSpace(t)));
            if (string.IsNullOrWhiteSpace(text)) continue;
            if (text.Length > MaxSourceChars) text = text[..MaxSourceChars] + "\n[SOURCE TRUNCATED]";

            var title = string.IsNullOrWhiteSpace(sources[i].Title)
                ? (sources.Count == 1 ? "The Story" : $"Chapter {i + 1}")
                : sources[i].Title;
            units.Add(new ChapterUnit(sources[i].Id, i, title, text, beats.Count));
        }
        return units;
    }

    // ── per-chapter generation with content-hash cache ───────────────────────

    private async Task<string> GetOrGenerateAsync(Guid storyNodeId, ChapterUnit ch, bool force, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ch.SourceText)));

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.NodeChapterSummaries
            .FirstOrDefaultAsync(s => s.NodeId == storyNodeId && s.ChapterIndex == ch.Index, ct);

        // Cache key = source hash + generating model, so a model upgrade regenerates
        // stale-quality summaries incrementally (resumable after provider hiccups).
        if (!force && existing != null && !string.IsNullOrWhiteSpace(existing.SummaryText)
            && existing.FactsJson.Contains(hash, StringComparison.OrdinalIgnoreCase)
            && existing.FactsJson.Contains(SynopsisModel, StringComparison.OrdinalIgnoreCase))
            return existing.SummaryText;

        var (synopsis, factsJson) = await GenerateAsync(ch, ct);
        // Never persist an empty/stub synopsis — 15 blank rows (pre-Legion-19 thinking-block
        // responses) poisoned the altitude audit with phantom missing-chapter BLOCKERs.
        if (string.IsNullOrWhiteSpace(synopsis) || synopsis.Length < 200)
            throw new InvalidOperationException(
                $"Synopsis generation returned {synopsis?.Length ?? 0} chars for chapter '{ch.Title}' — not storing.");
        // Gentle pacing — bulk regens tripped the provider circuit breaker at full speed.
        await Task.Delay(750, ct);

        // Stamp the source hash into FactsJson so re-publishing unchanged prose is free.
        var facts = ParseOrEmpty(factsJson);
        facts["sourceHash"] = hash;
        facts["model"] = SynopsisModel;
        var storedFacts = JsonSerializer.Serialize(facts);

        if (existing == null)
        {
            db.NodeChapterSummaries.Add(new NodeChapterSummary
            {
                Id = Guid.CreateVersion7(),
                NodeId = storyNodeId,
                ChapterIndex = ch.Index,
                SummaryText = synopsis,
                FactsJson = storedFacts,
            });
        }
        else
        {
            existing.SummaryText = synopsis;
            existing.FactsJson = storedFacts;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        return synopsis;
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
            it). Your summary is used to audit the story against its bible, so precision on
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
            log.LogWarning("Synopsis: non-JSON response for chapter {Title}; storing raw text", ch.Title);
        }
        return (raw, "{}");
    }

    private static Dictionary<string, object> ParseOrEmpty(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
        }
        catch { return new(); }
    }

    // ── publish folder resolution (byte-for-byte the exporters' layout) ───────

    private async Task<string> NodePublishDirAsync(Guid nodeId, CancellationToken ct)
    {
        var ancestors = new List<string>();
        string? universeSlug;
        string title;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var self = await db.Nodes.AsNoTracking().Where(s => s.Id == nodeId)
                .Select(s => new { s.ParentNodeId, s.UniverseId, s.Title }).FirstAsync(ct);
            title = self.Title;
            universeSlug = await db.Universes.AsNoTracking()
                .Where(u => u.Id == self.UniverseId).Select(u => u.Slug).FirstOrDefaultAsync(ct);
            var parentId = self.ParentNodeId;
            for (var guard = 0; parentId is Guid pid && guard < 8; guard++)
            {
                var parent = await db.Nodes.AsNoTracking().Where(s => s.Id == pid)
                    .Select(s => new { s.Title, s.ParentNodeId }).FirstOrDefaultAsync(ct);
                if (parent is null) break;
                ancestors.Insert(0, SanitizeTitle(parent.Title));
                parentId = parent.ParentNodeId;
            }
        }
        var parts = new List<string> { settings.GetExportDirectory(universeSlug) };
        parts.AddRange(ancestors);
        parts.Add(SanitizeTitle(title));
        return Path.Combine(parts.ToArray());
    }

    private static string SanitizeTitle(string? title)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        invalid.Add('\''); invalid.Add('’');
        var kept = new string((title ?? "").Where(c => !invalid.Contains(c)).ToArray()).Trim();
        kept = Regex.Replace(kept, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(kept) ? "untitled" : kept;
    }
}
