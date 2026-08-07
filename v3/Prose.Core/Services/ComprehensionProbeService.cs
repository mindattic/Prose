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
/// Reader-Proxy QA Instrument 1 — comprehension probes (docs/READER-QA.md).
///
/// <para>The core idea: don't ask a big model to SIMULATE an average reader (persona
/// prompting fakes diversity); use a small model's GENUINE reading as the instrument.
/// Haiku reads each chapter cold — with only a rolling recap of prior chapters — and
/// reports what it understood: summary, structured facts, explicit confusion points.
/// That reading is diffed against the fidelity-strict Sonnet synopsis
/// (<see cref="SynopsisExportService"/>, the chapter-altitude ground truth). Where the
/// cheap reader misreads, a median human reader plausibly misreads too.</para>
///
/// <para>Because Haiku also has model-specific failure habits (documented: upgrading
/// "stopped" to "killed", inferring unstated motives), every candidate mismatch goes
/// through a Sonnet ARBITER holding the actual chapter text: "does the text plausibly
/// support this misreading?" Only arbiter-confirmed, reader-plausible defects are
/// filed as <see cref="FindingCategory.ComprehensionDefect"/> findings; probe-side
/// hallucinations are recorded in the cache but never filed.</para>
///
/// <para>This is a MEASUREMENT, not a vote — it emits no scores and is deliberately
/// outside the SS-A44 <see cref="VotingGate"/> (same exemption as craft_audit and the
/// logic sweep). Cost gate: probe results are cached per (chapter-source-hash, probe
/// model) in <see cref="NodeChapterSummary.ComprehensionJson"/> — unchanged chapters
/// never re-bill.</para>
/// </summary>
public sealed class ComprehensionProbeService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ILlmService llm,
    SynopsisExportService synopsis,
    FindingsService findings,
    SettingsService settings,
    ILogger<ComprehensionProbeService> log)
{
    private const int MaxSourceChars = 150_000;
    private const string FindingSummaryPrefix = "COMPREHENSION";

    public sealed record ChapterProbeResult(
        int ChapterIndex, string ChapterTitle, string Status /* clean | defects | cached-clean | cached-defects | skipped */,
        IReadOnlyList<ProbeDefect> Defects, IReadOnlyList<string> Confusions, bool FromCache);

    public sealed record ProbeDefect(
        string Kind /* missed-fact | misread | confusion | hallucination */,
        string Description, string Evidence, string Severity /* blocker | moderate | minor */,
        bool ReaderPlausible);

    public sealed record ProbeRunResult(
        Guid NodeId, string Slug, string Title,
        IReadOnlyList<ChapterProbeResult> Chapters,
        int FindingsFiled, int ChaptersProbed, int ChaptersFromCache);

    /// <summary>Probe every chapter of the book. Ensures the Sonnet ground-truth
    /// synopsis exists first (hash-cached — only changed chapters bill), then runs
    /// the Haiku probe + arbiter per chapter, files confirmed defects as findings,
    /// and supersedes stale COMPREHENSION findings per chapter (delete-then-recreate,
    /// same lifecycle as the craft audit).</summary>
    public async Task<ProbeRunResult> RunAsync(Guid bookNodeId, bool force = false, CancellationToken ct = default)
    {
        string slug, title;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var node = await db.Nodes.AsNoTracking().Where(n => n.Id == bookNodeId)
                .Select(n => new { n.Slug, n.Title }).FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException($"No node {bookNodeId}.");
            slug = node.Slug ?? bookNodeId.ToString("N");
            title = node.Title;
        }

        // Ground truth first (Sonnet, hash-cached). Also yields the rolling recaps.
        var summaries = await synopsis.GetChapterSummariesAsync(bookNodeId, force: false, ct);
        var sources = await synopsis.GetChapterSourcesAsync(bookNodeId, ct);
        var summaryByIndex = summaries.ToDictionary(s => s.Index, s => s.Synopsis);

        var results = new List<ChapterProbeResult>();
        int filed = 0, probed = 0, cached = 0;

        foreach (var ch in sources)
        {
            ct.ThrowIfCancellationRequested();
            if (!summaryByIndex.ContainsKey(ch.Index))
            {
                results.Add(new ChapterProbeResult(ch.Index, ch.Title, "skipped",
                    Array.Empty<ProbeDefect>(), Array.Empty<string>(), false));
                continue;
            }

            var result = await ProbeChapterAsync(bookNodeId, slug, ch, summaryByIndex, force, ct);
            results.Add(result);
            if (result.FromCache) cached++; else probed++;
            // Cached chapters' defects were filed on their original run — count only fresh filings.
            if (!result.FromCache)
                filed += result.Defects.Count(d => d.ReaderPlausible && d.Kind != "hallucination");
        }

        return new ProbeRunResult(bookNodeId, slug, title, results, filed, probed, cached);
    }

    private async Task<ChapterProbeResult> ProbeChapterAsync(
        Guid bookNodeId, string slug, SynopsisExportService.ChapterUnit ch,
        IReadOnlyDictionary<int, string> summaryByIndex, bool force, CancellationToken ct)
    {
        var probeModel = settings.ComprehensionProbeModel;
        var arbiterModel = settings.ComprehensionArbiterModel;
        var sourceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ch.SourceText)));

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.NodeChapterSummaries
            .FirstOrDefaultAsync(s => s.NodeId == bookNodeId && s.ChapterIndex == ch.Index, ct);

        // ── cache hit: unchanged chapter + same instruments = free re-run ──────────
        if (!force && row?.ComprehensionJson is { Length: > 0 } cachedJson)
        {
            var parsed = TryParseCache(cachedJson);
            if (parsed != null
                && string.Equals(parsed.Value.hash, sourceHash, StringComparison.OrdinalIgnoreCase)
                && string.Equals(parsed.Value.probeModel, probeModel, StringComparison.OrdinalIgnoreCase))
            {
                var (defects, confusions) = parsed.Value.payload;
                return new ChapterProbeResult(ch.Index, ch.Title,
                    defects.Any(d => d.ReaderPlausible) ? "cached-defects" : "cached-clean",
                    defects, confusions, FromCache: true);
            }
        }

        // ── 1. the probe: a cold cheap reader with only the recap for context ─────
        var recap = BuildRollingRecap(ch.Index, summaryByIndex);
        var probe = await ProbeOnceAsync(ch, recap, probeModel, ct);

        // ── 2. deterministic prescreen: is arbitration even needed? ───────────────
        var groundFacts = ParseFacts(row?.FactsJson ?? "{}");
        var flags = Prescreen(groundFacts, probe);

        // ── 3. arbiter: candidate mismatches judged against the actual text ───────
        var allDefects = new List<ProbeDefect>();
        if (flags.Count > 0 || probe.Confusions.Count > 0)
            allDefects = await ArbitrateAsync(ch, summaryByIndex.GetValueOrDefault(ch.Index, ""), probe, flags, arbiterModel, ct);

        // ── 4. file confirmed reader-plausible defects; supersede stale ones ──────
        var filePath = $"node:{slug}/ch:{ch.Index:D2}";
        findings.DeleteBySummaryPrefix(filePath, FindingSummaryPrefix);
        foreach (var d in allDefects.Where(d => d.ReaderPlausible && d.Kind != "hallucination"))
        {
            findings.Upsert(
                filePath,
                chapterId: ch.NodeId.ToString("N"),
                FindingCategory.ComprehensionDefect,
                d.Severity switch { "blocker" => FindingSeverity.High, "moderate" => FindingSeverity.Medium, _ => FindingSeverity.Low },
                $"{FindingSummaryPrefix} [{d.Kind}] {ch.Title}: {d.Description}",
                snippet: d.Evidence,
                suggestedFix: null);
        }

        // ── 5. persist the cache row ───────────────────────────────────────────────
        var cache = new
        {
            probeHash = sourceHash,
            probeModel,
            arbiterModel,
            probe = new { summary = probe.Summary, facts = probe.FactsRaw, confusions = probe.Confusions, prediction = probe.Prediction },
            defects = allDefects,
            evaluatedAt = DateTime.UtcNow,
        };
        if (row != null)
        {
            row.ComprehensionJson = JsonSerializer.Serialize(cache);
            row.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        var live = allDefects.Where(d => d.ReaderPlausible).ToList();
        log.LogInformation("Comprehension probe ch{Index} '{Title}': {Defects} defect(s), {Confusions} confusion(s){Halluc}",
            ch.Index, ch.Title, live.Count, probe.Confusions.Count,
            allDefects.Count(d => d.Kind == "hallucination") is var h and > 0 ? $", {h} probe hallucination(s) discarded" : "");

        return new ChapterProbeResult(ch.Index, ch.Title, live.Count > 0 ? "defects" : "clean",
            allDefects, probe.Confusions, FromCache: false);
    }

    // ── the probe call ─────────────────────────────────────────────────────────────

    private sealed record ProbeReading(
        string Summary, JsonElement? FactsRaw, List<string> Entities, List<string> StateChanges,
        List<string> Confusions, string Prediction);

    private async Task<ProbeReading> ProbeOnceAsync(
        SynopsisExportService.ChapterUnit ch, string recap, string probeModel, CancellationToken ct)
    {
        const string system = """
            You are reading one chapter of a novel for the first time. You have NOT read the
            rest of the book — only the recap provided. Read like a normal reader: no
            re-reading, no detective work. Then report what you actually understood.
            Return STRICT JSON only, no markdown fence:
            {"summary":"what happened, in order, as you understood it",
             "facts":{"entities":["named characters/factions who act in this chapter"],
                      "locations":["places the chapter visits"],
                      "events":["main events in order"],
                      "state_changes":["durable changes: deaths, injuries, items gained/lost, secrets exposed, relationship shifts"]},
             "confusions":["anything you could not follow: who did what, why someone acted, where a scene is, how an outcome happened. Empty array if nothing confused you."],
             "prediction":"one sentence: what you expect to happen next"}
            Report honestly. If you lost the thread somewhere, SAY SO in confusions — that is
            the single most valuable thing you can report. Do not fill gaps with guesses.
            """;

        var user = $"RECAP OF THE BOOK SO FAR:\n{recap}\n\nCHAPTER: {ch.Title}\n\n{Truncate(ch.SourceText, MaxSourceChars)}";
        var raw = await llm.GenerateAsync(system, user, temperature: 0.3, maxTokens: 3000, model: probeModel, ct: ct);
        raw = StripFence(raw);

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement.Clone();
            var facts = root.TryGetProperty("facts", out var f) ? f : (JsonElement?)null;
            return new ProbeReading(
                Summary: root.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
                FactsRaw: facts,
                Entities: StringList(facts, "entities"),
                StateChanges: StringList(facts, "state_changes"),
                Confusions: root.TryGetProperty("confusions", out var c) ? StringList(c) : new List<string>(),
                Prediction: root.TryGetProperty("prediction", out var p) ? p.GetString() ?? "" : "");
        }
        catch (JsonException)
        {
            log.LogWarning("Comprehension probe returned non-JSON for '{Title}' — treating raw text as summary.", ch.Title);
            return new ProbeReading(raw, null, new List<string>(), new List<string>(), new List<string>(), "");
        }
    }

    // ── deterministic prescreen (no LLM) ───────────────────────────────────────────

    private sealed record GroundFacts(List<string> Entities, List<string> StateChanges);

    private static GroundFacts ParseFacts(string factsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(factsJson);
            var root = doc.RootElement;
            return new GroundFacts(StringList(root, "entities"), StringList(root, "state_changes"));
        }
        catch { return new GroundFacts(new List<string>(), new List<string>()); }
    }

    /// <summary>Cheap red flags that justify spending an arbiter call. Names are
    /// compared as normalized token sets; state changes only by count (semantic
    /// equivalence is the arbiter's job, not string math's).</summary>
    private static List<string> Prescreen(GroundFacts ground, ProbeReading probe)
    {
        var flags = new List<string>();
        var probeTokens = TokenSet(probe.Entities.Concat(new[] { probe.Summary }));
        foreach (var e in ground.Entities)
        {
            var nameTokens = Tokens(e).ToList();
            if (nameTokens.Count > 0 && !nameTokens.Any(probeTokens.Contains))
                flags.Add($"Ground-truth actor '{e}' absent from the probe's reading — possibly invisible to a casual reader.");
        }
        var groundTokens = TokenSet(ground.Entities);
        foreach (var e in probe.Entities)
        {
            var nameTokens = Tokens(e).ToList();
            if (nameTokens.Count > 0 && !nameTokens.Any(groundTokens.Contains))
                flags.Add($"Probe reports actor '{e}' that ground truth does not — possible misattribution.");
        }
        if (ground.StateChanges.Count > 0 && probe.StateChanges.Count < ground.StateChanges.Count)
            flags.Add($"Probe caught {probe.StateChanges.Count}/{ground.StateChanges.Count} durable state changes — some may not land on the page.");
        return flags;
    }

    // ── the arbiter call ───────────────────────────────────────────────────────────

    private async Task<List<ProbeDefect>> ArbitrateAsync(
        SynopsisExportService.ChapterUnit ch, string groundSynopsis, ProbeReading probe,
        List<string> flags, string arbiterModel, CancellationToken ct)
    {
        const string system = """
            You arbitrate between a book chapter's GROUND-TRUTH synopsis (written with the
            full text, fidelity-strict) and a CHEAP READER's first-pass reading of the same
            chapter. The cheap reader is a proxy for a median human reader. Your job: for
            each candidate mismatch and each confusion the reader reported, decide whether
            THE CHAPTER TEXT ITSELF plausibly supports the misreading/confusion.
            - readerPlausible=true → the text under-establishes the fact (buried, ambiguous,
              off-page, contradicted). A typical reader would stumble here too. This is a
              real defect in the chapter.
            - readerPlausible=false → the text is clear; the cheap reader hallucinated or
              got sloppy (kind="hallucination"). Not the chapter's fault.
            Judge against the TEXT, not against what the author intended.
            STRICT BAR for kind=missed-fact / misread: a summary OMITTING a fact is NOT a
            defect when the chapter states that fact plainly at the scene where it matters —
            summaries compress; that is normal reading, not confusion. Confirm missed-fact
            ONLY when the fact appears just once, in passing, away from its point of use, or
            is contradicted elsewhere — and your evidence must QUOTE the sole/oblique mention
            to prove it. Deliberately open mysteries the text itself marks as unresolved
            (a character saying they don't know; an explicitly unexplained signal) are the
            text working as intended — reject, do not confirm.
            (Calibration: BCODA 2026-08-03 — 7 of 7 arbiter-confirmed missed-fact defects
            were dismissed on human read; every "missed" fact was explicit on the page.)
            Return STRICT JSON only, no markdown fence:
            {"defects":[{"kind":"missed-fact|misread|confusion|hallucination",
                         "description":"what a reader gets wrong or cannot follow",
                         "evidence":"short quote or precise pointer from the chapter text",
                         "severity":"blocker|moderate|minor",
                         "readerPlausible":true}]}
            Empty defects array if every mismatch is reader sloppiness and the text is clean.
            severity: blocker = plot-critical fact unreachable; moderate = named-character
            action/motive muddled; minor = texture-level.
            """;

        var sb = new StringBuilder();
        sb.AppendLine("GROUND-TRUTH SYNOPSIS (fidelity-strict, from full text):");
        sb.AppendLine(groundSynopsis);
        sb.AppendLine();
        sb.AppendLine("CHEAP READER'S FIRST-PASS READING:");
        sb.AppendLine(probe.Summary);
        if (probe.Confusions.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("READER'S SELF-REPORTED CONFUSIONS:");
            foreach (var c in probe.Confusions) sb.AppendLine($"- {c}");
        }
        if (flags.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("AUTOMATED MISMATCH FLAGS:");
            foreach (var f in flags) sb.AppendLine($"- {f}");
        }
        sb.AppendLine();
        sb.AppendLine($"CHAPTER TEXT ({ch.Title}):");
        sb.AppendLine(Truncate(ch.SourceText, MaxSourceChars));

        var raw = await llm.GenerateAsync(system, sb.ToString(), temperature: 0.1, maxTokens: 2500, model: arbiterModel, ct: ct);
        raw = StripFence(raw);
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var list = new List<ProbeDefect>();
            if (doc.RootElement.TryGetProperty("defects", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var d in arr.EnumerateArray())
                {
                    list.Add(new ProbeDefect(
                        Kind: d.TryGetProperty("kind", out var k) ? k.GetString() ?? "misread" : "misread",
                        Description: d.TryGetProperty("description", out var de) ? de.GetString() ?? "" : "",
                        Evidence: d.TryGetProperty("evidence", out var ev) ? ev.GetString() ?? "" : "",
                        Severity: d.TryGetProperty("severity", out var sv) ? sv.GetString() ?? "minor" : "minor",
                        ReaderPlausible: d.TryGetProperty("readerPlausible", out var rp) && rp.ValueKind == JsonValueKind.True));
                }
            }
            return list;
        }
        catch (JsonException)
        {
            log.LogWarning("Comprehension arbiter returned non-JSON for '{Title}' — no defects filed this run.", ch.Title);
            return new List<ProbeDefect>();
        }
    }

    // ── helpers ────────────────────────────────────────────────────────────────────

    /// <summary>Recap = full synopses of the previous 3 chapters + titles-only earlier.
    /// Costs nothing (reuses the stored Sonnet summaries) and mirrors what a real
    /// reader retains: recent chapters vividly, older ones as gist.</summary>
    private static string BuildRollingRecap(int chapterIndex, IReadOnlyDictionary<int, string> summaryByIndex)
    {
        if (chapterIndex == 0) return "(This is the first chapter — no recap.)";
        var sb = new StringBuilder();
        for (int i = 0; i < chapterIndex; i++)
        {
            if (!summaryByIndex.TryGetValue(i, out var syn)) continue;
            if (i >= chapterIndex - 3) sb.AppendLine($"Chapter {i + 1}: {syn}");
            else
            {
                var gist = syn.Length > 160 ? syn[..160] + "…" : syn;
                sb.AppendLine($"Chapter {i + 1} (gist): {gist}");
            }
        }
        return sb.Length > 0 ? sb.ToString() : "(No recap available.)";
    }

    private (string hash, string probeModel, (List<ProbeDefect>, List<string>) payload)? TryParseCache(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var hash = root.TryGetProperty("probeHash", out var h) ? h.GetString() ?? "" : "";
            var model = root.TryGetProperty("probeModel", out var m) ? m.GetString() ?? "" : "";
            var defects = new List<ProbeDefect>();
            if (root.TryGetProperty("defects", out var arr) && arr.ValueKind == JsonValueKind.Array)
                defects = JsonSerializer.Deserialize<List<ProbeDefect>>(arr.GetRawText()) ?? new();
            var confusions = root.TryGetProperty("probe", out var p) && p.TryGetProperty("confusions", out var c)
                ? StringList(c) : new List<string>();
            return (hash, model, (defects, confusions));
        }
        catch { return null; }
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "\n[SOURCE TRUNCATED]";

    private static string StripFence(string raw)
    {
        raw = raw.Trim();
        if (raw.StartsWith("```"))
            raw = Regex.Replace(Regex.Replace(raw, @"^```(json)?\s*", ""), @"\s*```$", "");
        return raw;
    }

    private static List<string> StringList(JsonElement? parent, string property)
    {
        if (parent is not { } p || p.ValueKind != JsonValueKind.Object) return new List<string>();
        return p.TryGetProperty(property, out var el) ? StringList(el) : new List<string>();
    }

    private static List<string> StringList(JsonElement el) =>
        el.ValueKind == JsonValueKind.Array
            ? el.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList()
            : new List<string>();

    private static IEnumerable<string> Tokens(string s) =>
        Regex.Split(s.ToLowerInvariant(), @"[^\p{L}\p{N}]+")
            .Where(t => t.Length >= 3 && !StopWords.Contains(t));

    private static HashSet<string> TokenSet(IEnumerable<string> items) =>
        items.SelectMany(Tokens).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    { "the", "and", "her", "his", "she", "him", "who", "with", "that", "this", "for", "was", "are", "one", "man", "woman" };
}
