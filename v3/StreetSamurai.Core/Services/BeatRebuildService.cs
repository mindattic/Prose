using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

/// <summary>
/// Rebuilds a node's beats to follow the codified beat doctrine
/// (<see cref="StreetSamurai.Core.Models.Canon.BeatDoctrineRules"/>): re-segments
/// the node's full prose into STORY BEATS via the LLM, applies the prose
/// mechanics (dialogue on its own line, '?' on questions, asks/asked), and
/// assigns inter-beat GAPS — then replaces the node's beats.
///
/// Safety:
/// <list type="bullet">
/// <item>The node's full text is exported to markdown BEFORE any mutation.</item>
/// <item>A word-retention guard compares the LLM output against the source; if it
/// diverges past <see cref="MinWordRetention"/> (truncation / dropped scenes /
/// hallucination) the node is NOT mutated and is flagged for review.</item>
/// <item>The node prose is processed in bounded windows (sentence-aligned) so a
/// single LLM call never has to swallow a whole novel.</item>
/// </list>
/// Source of truth stays the relational beats; this is an author-invoked rewrite,
/// gated per node by the report it returns.
/// </summary>
public class BeatRebuildService
{
    private readonly IDbContextFactory<StreetSamuraiDbContext> dbFactory;
    private readonly NodeWorkbenchService workbench;
    private readonly ILlmService llm;
    private readonly DatabaseService canonDb;
    private readonly ManuscriptExportService export;
    private readonly ILogger<BeatRebuildService> log;

    /// <summary>Minimum fraction of the source's word tokens that must survive in the
    /// rebuilt prose for the result to be auto-applied. Below this = refuse + flag.</summary>
    public const double MinWordRetention = 0.90;

    /// <summary>Sentence-aligned window size fed to the LLM per call.</summary>
    private const int WindowChars = 3500;

    public BeatRebuildService(
        IDbContextFactory<StreetSamuraiDbContext> dbFactory,
        NodeWorkbenchService workbench,
        ILlmService llm,
        DatabaseService canonDb,
        ManuscriptExportService export,
        ILogger<BeatRebuildService> log)
    {
        this.dbFactory = dbFactory;
        this.workbench = workbench;
        this.llm = llm;
        this.canonDb = canonDb;
        this.export = export;
        this.log = log;
    }

    public record RebuiltBeat(string Text, bool SceneEnd);

    public record BeatRebuildReport(
        Guid NodeId, string Slug, string Title, bool Applied,
        int OldBeats, int NewBeats, double WordRetention, bool GuardPassed,
        string? BackupPath, string? Note);

    private sealed class LlmBeat
    {
        [JsonPropertyName("text")] public string Text { get; set; } = "";
        [JsonPropertyName("sceneEnd")] public bool SceneEnd { get; set; }
    }

    /// <summary>
    /// Re-segment one node. With <paramref name="apply"/>=false this is a dry run:
    /// it computes the proposed beats + retention and mutates nothing. With
    /// <paramref name="apply"/>=true it exports a backup, then (only if the guard
    /// passes) replaces the node's beats and assigns gaps.
    /// </summary>
    public async Task<BeatRebuildReport> RebuildAsync(Guid nodeId, bool apply, CancellationToken ct = default)
    {
        string slug, title;
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var s = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == nodeId, ct);
            if (s == null) return new(nodeId, "", "", false, 0, 0, 0, false, null, "Node not found.");
            slug = s.Slug; title = s.Title;
        }

        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);
        if (ordered.Count == 0)
            return new(nodeId, slug, title, false, 0, 0, 0, false, null, "Node has no beats — nothing to rebuild.");

        var sourceText = AssembleText(ordered.Select(o => o.Beat.Text ?? ""));
        if (sourceText.Trim().Length == 0)
            return new(nodeId, slug, title, false, ordered.Count, 0, 0, false, null, "Node beats are empty — nothing to rebuild.");

        // ── LLM re-segmentation, windowed ──
        var system = BuildSystemPrompt();
        var rebuilt = new List<RebuiltBeat>();
        foreach (var window in ChunkBySentence(sourceText, WindowChars))
        {
            ct.ThrowIfCancellationRequested();
            var beats = await SegmentWindowAsync(system, window, ct);
            rebuilt.AddRange(beats);
        }
        rebuilt = rebuilt.Where(b => !string.IsNullOrWhiteSpace(b.Text)).ToList();
        if (rebuilt.Count == 0)
            return new(nodeId, slug, title, false, ordered.Count, 0, 0, false, null, "LLM returned no beats — left untouched.");

        // ── Word-retention guard ──
        var retention = WordRetention(sourceText, string.Join(" ", rebuilt.Select(b => b.Text)));
        var guardPassed = retention >= MinWordRetention;

        if (!apply)
            return new(nodeId, slug, title, false, ordered.Count, rebuilt.Count, retention, guardPassed, null,
                guardPassed ? "Dry run — re-run with --apply to commit." : $"Dry run — GUARD WOULD BLOCK (retention {retention:P0} < {MinWordRetention:P0}).");

        if (!guardPassed)
            return new(nodeId, slug, title, false, ordered.Count, rebuilt.Count, retention, false, null,
                $"BLOCKED: word retention {retention:P0} < {MinWordRetention:P0} — likely truncation/hallucination. Left untouched; review manually.");

        // ── Backup, then replace ──
        string? backupPath = null;
        try { backupPath = await export.ExportMarkdownAsync(nodeId, ct: ct); }
        catch (Exception ex) { log.LogWarning(ex, "Backup export failed for {Slug}; aborting rebuild.", slug);
            return new(nodeId, slug, title, false, ordered.Count, rebuilt.Count, retention, true, null, $"Backup export failed ({ex.Message}) — left untouched."); }

        await ReplaceBeatsAsync(nodeId, rebuilt, ct);
        log.LogInformation("Rebuilt node {Slug}: {Old} → {New} beats (retention {Ret:P0}).", slug, ordered.Count, rebuilt.Count, retention);
        return new(nodeId, slug, title, true, ordered.Count, rebuilt.Count, retention, true, backupPath, "Rebuilt and gaps assigned.");
    }

    // ── prose assembly / chunking ──

    private static string AssembleText(IEnumerable<string> beatTexts)
    {
        // Join into continuous prose so the LLM re-paragraphs freely; collapse the
        // run-together whitespace the old sentence-beats produced.
        var joined = string.Join(" ", beatTexts.Select(t => (t ?? "").Trim()).Where(t => t.Length > 0));
        return System.Text.RegularExpressions.Regex.Replace(joined, @"[ \t]{2,}", " ").Trim();
    }

    /// <summary>Split text into ≤<paramref name="size"/>-char windows, never mid-sentence.</summary>
    private static IEnumerable<string> ChunkBySentence(string text, int size)
    {
        int i = 0, n = text.Length;
        while (i < n)
        {
            if (n - i <= size) { yield return text[i..].Trim(); break; }
            int end = i + size;
            // Walk back to the nearest sentence terminator (. ! ? optionally + quote).
            int cut = -1;
            for (int j = end; j > i + size / 2; j--)
            {
                char c = text[j];
                if (c is '.' or '!' or '?')
                {
                    int k = j + 1;
                    while (k < n && (text[k] is '"' or '\'' or '”' or '’' or ')')) k++;
                    cut = k; break;
                }
            }
            if (cut < 0) cut = end;            // no boundary found — hard cut
            yield return text[i..cut].Trim();
            i = cut;
        }
    }

    private string BuildSystemPrompt()
    {
        var rules = canonDb.GetLiteraryRulesPrompt();   // leads with "WHAT A BEAT IS" (the doctrine)
        var tone = canonDb.GetToneBiblePrompt();
        return
            "You are a structural editor for a fiction engine. Your ONLY job is to re-segment an existing passage " +
            "into STORY BEATS and apply mechanical formatting — NOT to rewrite it.\n\n" +
            rules + "\n\n" + tone + "\n\n" +
            "TASK: Divide the passage below into story beats per the doctrine above (a beat is one unit of story " +
            "that leads to the next; usually a paragraph, sometimes a single line of dialogue or one moment). " +
            "Apply ONLY these mechanical fixes: put each speaker's dialogue on its own beat/line; end questions with '?'; " +
            "use asks/asked (not says/said) for question attribution.\n" +
            "HARD CONSTRAINTS:\n" +
            "- PRESERVE THE AUTHOR'S WORDING. Do not add, remove, summarize, or rephrase content. Keep every sentence.\n" +
            "- Do not invent transitions or commentary.\n" +
            "- Mark sceneEnd=true ONLY on a beat that clearly ends a scene or section (a time/place jump follows).\n" +
            "OUTPUT: a single JSON array, nothing else: [{\"text\":\"<beat prose>\",\"sceneEnd\":false}, ...]";
    }

    private async Task<List<RebuiltBeat>> SegmentWindowAsync(string system, string window, CancellationToken ct)
    {
        string raw;
        try { raw = await llm.GenerateAsync(system, window, temperature: 0.1, maxTokens: 8000, ct: ct); }
        catch (Exception ex) { log.LogWarning(ex, "LLM segmentation call failed; keeping window as one beat.");
            return [new RebuiltBeat(window.Trim(), false)]; }

        var json = JsonDefaults.StripCodeFences(raw);
        try
        {
            var arr = JsonSerializer.Deserialize<List<LlmBeat>>(json, JsonDefaults.LlmParsing) ?? [];
            var beats = arr.Where(b => !string.IsNullOrWhiteSpace(b.Text))
                           .Select(b => new RebuiltBeat(b.Text.Trim(), b.SceneEnd)).ToList();
            // If the model ignored the format, don't lose the prose — fall back to the window.
            return beats.Count > 0 ? beats : [new RebuiltBeat(window.Trim(), false)];
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Could not parse LLM beat JSON; keeping window as one beat.");
            return [new RebuiltBeat(window.Trim(), false)];
        }
    }

    // ── guard ──

    private static readonly char[] WordSep = " \t\r\n.,;:!?\"'“”‘’()[]—-…*".ToCharArray();

    /// <summary>Fraction of the source's word-token multiset preserved in the rebuilt text.
    /// says↔asks swaps are normalized so the permitted mechanical fix doesn't count as loss.</summary>
    private static double WordRetention(string source, string rebuilt)
    {
        var src = Tokenize(source);
        if (src.Count == 0) return 1.0;
        var dst = Tokenize(rebuilt);

        var dstCounts = new Dictionary<string, int>();
        foreach (var w in dst) dstCounts[w] = dstCounts.GetValueOrDefault(w) + 1;

        int preserved = 0;
        foreach (var w in src)
            if (dstCounts.TryGetValue(w, out var c) && c > 0) { dstCounts[w] = c - 1; preserved++; }
        return (double)preserved / src.Count;
    }

    private static List<string> Tokenize(string text)
    {
        var toks = text.ToLowerInvariant().Split(WordSep, StringSplitOptions.RemoveEmptyEntries);
        // Normalize the one permitted swap so it isn't scored as a lost/added word.
        for (int i = 0; i < toks.Length; i++)
            toks[i] = toks[i] switch { "asks" => "says", "asked" => "said", _ => toks[i] };
        return toks.ToList();
    }

    // ── replace ──

    private async Task ReplaceBeatsAsync(Guid nodeId, List<RebuiltBeat> beats, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        // Drop the node's existing beats. Beats are referenced only by BeatNodes
        // (verified by FK survey); remove links first, then the now-orphan Beat rows.
        var oldBeatIds = await db.BeatNodes.Where(sb => sb.NodeId == nodeId)
            .Select(sb => sb.BeatId).ToListAsync(ct);
        await db.BeatNodes.Where(sb => sb.NodeId == nodeId).ExecuteDeleteAsync(ct);
        if (oldBeatIds.Count > 0)
        {
            var stillReferenced = await db.BeatNodes
                .Where(sb => oldBeatIds.Contains(sb.BeatId)).Select(sb => sb.BeatId).Distinct().ToListAsync(ct);
            var toDelete = oldBeatIds.Except(stillReferenced).ToList();
            await db.Beats.Where(b => toDelete.Contains(b.Id)).ExecuteDeleteAsync(ct);
        }

        var baseNumber = (await db.Beats.MaxAsync(b => (int?)b.Number, ct) ?? 0) + 1;
        double sortKey = 100.0;
        for (int i = 0; i < beats.Count; i++)
        {
            var rb = beats[i];
            var text = rb.Text.Trim();
            bool isLast = i == beats.Count - 1;
            var beat = new Beat
            {
                Id = Guid.CreateVersion7(),
                Number = baseNumber + i,
                Text = text,
                TextHash = NodeWorkbenchService.ComputeTextHash(text),
                Kind = "prose",
                SceneType = rb.SceneEnd ? "scene-end" : "scene",
                Stale = true,
                GapAfterMs = isLast ? null : GapFor(rb, text),
            };
            db.Beats.Add(beat);
            db.BeatNodes.Add(new BeatNode { NodeId = nodeId, BeatId = beat.Id, SortKey = sortKey });
            sortKey += 100.0;
        }

        var node = await db.Nodes.FirstOrDefaultAsync(s => s.Id == nodeId, ct);
        if (node != null) node.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <summary>Gap (ms) of narration silence after a beat: longest at a scene break,
    /// short after a line of dialogue, medium after narration. Mirrors the doctrine.</summary>
    private static int GapFor(RebuiltBeat rb, string text)
    {
        if (rb.SceneEnd) return 1200;
        var t = text.TrimStart();
        bool dialogue = t.StartsWith('"') || t.StartsWith('“') || t.StartsWith('\'');
        return dialogue ? 350 : 450;
    }
}
