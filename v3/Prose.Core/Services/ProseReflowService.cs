using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Bounded copy-editor for node prose. Beats are stored as run-on blocks with
/// no paragraph breaks, declarative-looking questions (no "?"), and "says" where
/// a question wants "asks". This pass repairs ONLY those three mechanical things:
///   1. paragraph + dialogue line breaks (manuscript convention),
///   2. a "?" on a question that lacks terminal interrogative punctuation,
///   3. say↔ask attribution on a line of dialogue that is a question.
///
/// It is NOT a rewrite. Every result is gated by a word-token guard: the lowercased
/// word sequence must be IDENTICAL except for permitted say↔ask verb swaps. Any beat
/// where the model touched an actual word (a typo "fix", a reword, an insertion) is
/// REJECTED and left exactly as it was, so canon prose can never silently drift.
/// </summary>
public class ProseReflowService
{
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILlmService llm;
    private readonly NodeWorkbenchService workbench;
    private readonly ILogger<ProseReflowService> log;

    public ProseReflowService(
        IDbContextFactory<ProseDbContext> dbFactory,
        ILlmService llm,
        NodeWorkbenchService workbench,
        ILogger<ProseReflowService> log)
    {
        this.dbFactory = dbFactory;
        this.llm = llm;
        this.workbench = workbench;
        this.log = log;
    }

    public sealed record BeatReflowResult(
        Guid BeatId, int Position, string Status,
        int QuestionMarksAdded, int AttributionSwaps,
        string? Reason, string BeforePreview, string AfterPreview);

    public sealed record NodeReflowReport(
        Guid NodeId, string Slug, bool Applied,
        int Total, int Changed, int Unchanged, int Rejected, int Errors,
        List<BeatReflowResult> Beats);

    private const string System =
        "You are a meticulous print copy-editor preparing a passage of fiction for a manuscript. " +
        "You make ONLY mechanical formatting and punctuation corrections. You must NEVER reword, " +
        "rewrite, paraphrase, add, delete, reorder, or respell any word, and you must NOT fix grammar " +
        "or typos. Output ONLY the corrected passage — no preamble, no code fences, no commentary.";

    private static string BuildUser(string original) =>
        "Apply ONLY these three corrections to the PASSAGE below:\n\n" +
        "1. PARAGRAPHS: Break the run-on text into proper paragraphs, separated by a single blank line. " +
        "Start a NEW paragraph each time a different character begins to speak (standard fiction convention) " +
        "and at natural narrative shifts. Keep a speaker's dialogue and its attribution together in one paragraph.\n" +
        "2. QUESTION MARKS: If a sentence is genuinely a question but ends without one, change its terminal " +
        "punctuation to '?'. Touch no other punctuation.\n" +
        "3. DIALOGUE ATTRIBUTION: When a line of dialogue is a question, its attribution verb must be " +
        "'asks'/'asked', not 'says'/'said'. Change only that single verb, only in that case.\n\n" +
        "Do NOT change, add, remove, reorder, or respell any word. Do NOT fix spelling, grammar, or wording. " +
        "Preserve every word exactly as written, including any *asterisks* used for emphasis.\n\n" +
        "PASSAGE:\n" + original;

    /// <summary>Fallback prompt used only when the full copy-edit is rejected for
    /// touching a word: insert paragraph breaks and NOTHING else — not one
    /// character of text may change.</summary>
    private static string BuildParagraphOnlyUser(string original) =>
        "Re-flow the PASSAGE below into proper paragraphs ONLY. Separate paragraphs with a single blank line; " +
        "start a new paragraph each time a different character begins to speak. " +
        "You may ONLY insert line breaks. Do NOT change, add, remove, reorder, or respell a single character of " +
        "text — not one word, not one punctuation mark. The text between the breaks must be byte-for-byte identical.\n\n" +
        "PASSAGE:\n" + original;

    /// <summary>Copy-edit every beat in the node. With <paramref name="apply"/> false
    /// this is a dry run (nothing written) — the report carries before/after previews
    /// so a caller can show the diff before committing.</summary>
    public async Task<NodeReflowReport> ReflowNodeAsync(Guid nodeId, bool apply, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var ordered = await workbench.GetOrderedBeatsAsync(nodeId, ct);

        var results = new List<BeatReflowResult>();
        int changed = 0, unchanged = 0, rejected = 0, errors = 0, pos = 0;
        foreach (var ob in ordered)
        {
            pos++;
            ct.ThrowIfCancellationRequested();
            var beat = ob.Beat;
            var original = (beat.Text ?? "").Trim();
            if (original.Length == 0)
            {
                results.Add(new(beat.Id, pos, "empty", 0, 0, null, "", ""));
                continue;
            }

            string edited;
            try
            {
                var raw = await llm.GenerateAsync(System, BuildUser(original), temperature: 0.2, maxTokens: 4096, ct: ct);
                edited = StripFences((raw ?? "").Trim());
            }
            catch (Exception ex)
            {
                errors++;
                results.Add(new(beat.Id, pos, "error", 0, 0, ex.Message, Preview(original), ""));
                continue;
            }

            var (ok, reason, swaps) = Guard(original, edited);
            if (!ok)
            {
                // The full copy-edit touched a real word — refuse it, then fall back
                // to a paragraphing-ONLY pass (whitespace-strict guard) so the beat
                // at least gets its paragraphs without any risk of a reworded line.
                string paraOnly;
                try
                {
                    var raw2 = await llm.GenerateAsync(System, BuildParagraphOnlyUser(original), temperature: 0.1, maxTokens: 4096, ct: ct);
                    paraOnly = StripFences((raw2 ?? "").Trim());
                }
                catch { paraOnly = ""; }

                if (paraOnly.Length > 0 && WhitespaceOnlyChange(original, paraOnly) && paraOnly != original)
                {
                    if (apply)
                        await workbench.UpdateBeatTextAsync(beat.Id, paraOnly, expectedUpdatedAt: beat.UpdatedAt, ct);
                    changed++;
                    results.Add(new(beat.Id, pos, "changed", 0, 0, "paragraphs only (full edit rejected: " + reason + ")",
                        Preview(original), Preview(paraOnly)));
                    continue;
                }

                rejected++;
                results.Add(new(beat.Id, pos, "rejected", 0, 0, reason, Preview(original), Preview(edited)));
                continue;
            }
            if (edited == original)
            {
                unchanged++;
                results.Add(new(beat.Id, pos, "unchanged", 0, 0, null, Preview(original), Preview(edited)));
                continue;
            }

            var qAdded = Math.Max(0, CountChar(edited, '?') - CountChar(original, '?'));
            if (apply)
                await workbench.UpdateBeatTextAsync(beat.Id, edited, expectedUpdatedAt: beat.UpdatedAt, ct);
            changed++;
            results.Add(new(beat.Id, pos, "changed", qAdded, swaps, null, Preview(original), Preview(edited)));
        }

        log.LogInformation("Reflow {Mode} node {Slug}: {Changed} changed, {Unchanged} unchanged, {Rejected} rejected, {Errors} errors",
            apply ? "APPLIED" : "dry-run", node.Slug, changed, unchanged, rejected, errors);
        return new NodeReflowReport(nodeId, node.Slug, apply, ordered.Count, changed, unchanged, rejected, errors, results);
    }

    // ── guard ─────────────────────────────────────────────────────────────

    /// <summary>Permitted attribution-verb swaps (tense-matched), say-family ↔ ask-family.</summary>
    private static readonly Dictionary<string, string> SayToAsk = new()
    {
        ["say"] = "ask", ["says"] = "asks", ["said"] = "asked", ["saying"] = "asking",
    };

    private static bool IsAllowedVerbSwap(string a, string b)
        => (SayToAsk.TryGetValue(a, out var ax) && ax == b)
        || (SayToAsk.TryGetValue(b, out var bx) && bx == a);

    /// <summary>The edit is in-bounds iff the lowercased word-token sequence is
    /// identical except for permitted say↔ask swaps. Punctuation and whitespace
    /// (the only things this pass is allowed to add) are invisible to word tokens,
    /// so adding "?" and paragraph breaks passes; touching any real word fails.</summary>
    private static (bool ok, string reason, int swaps) Guard(string original, string edited)
    {
        if (edited.Length == 0) return (false, "empty model output", 0);
        var a = WordTokens(original);
        var b = WordTokens(edited);
        if (a.Count != b.Count)
            return (false, $"word count changed {a.Count}→{b.Count} (rewrite/insert/delete)", 0);
        int swaps = 0;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] == b[i]) continue;
            if (IsAllowedVerbSwap(a[i], b[i])) { swaps++; continue; }
            return (false, $"word changed at token {i + 1}: '{a[i]}' → '{b[i]}'", swaps);
        }
        if (CountChar(edited, '?') < CountChar(original, '?'))
            return (false, "question mark removed", swaps);
        return (true, "", swaps);
    }

    /// <summary>True iff the two strings differ ONLY in whitespace — same characters,
    /// same order, once every whitespace run is collapsed to a single space and
    /// trimmed. The strictest guard: used for the paragraphing-only fallback, where
    /// not even a punctuation mark may change.</summary>
    private static bool WhitespaceOnlyChange(string a, string b)
    {
        static string Collapse(string s) => Regex.Replace(s, @"\s+", " ").Trim();
        return Collapse(a) == Collapse(b);
    }

    private static readonly Regex WordRe = new(@"[a-z0-9']+", RegexOptions.Compiled);
    private static List<string> WordTokens(string text)
    {
        var list = new List<string>();
        foreach (Match m in WordRe.Matches(text.ToLowerInvariant())) list.Add(m.Value);
        return list;
    }

    private static int CountChar(string s, char c)
    {
        int n = 0;
        foreach (var ch in s) if (ch == c) n++;
        return n;
    }

    /// <summary>Strip a ```fence``` the model may wrap the passage in despite instructions.</summary>
    private static string StripFences(string s)
    {
        if (!s.StartsWith("```")) return s;
        var lines = s.Replace("\r\n", "\n").Split('\n').ToList();
        if (lines.Count > 0 && lines[0].StartsWith("```")) lines.RemoveAt(0);
        if (lines.Count > 0 && lines[^1].Trim() == "```") lines.RemoveAt(lines.Count - 1);
        return string.Join("\n", lines).Trim();
    }

    private static string Preview(string s, int n = 160)
    {
        var flat = Regex.Replace(s, @"\s+", " ").Trim();
        return flat.Length > n ? flat[..n] + "…" : flat;
    }
}
