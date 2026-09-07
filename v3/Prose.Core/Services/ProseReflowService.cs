using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Paragraph re-flow for node prose that arrived as run-on blocks. Inserts paragraph and
/// dialogue line breaks — and does nothing else.
///
/// <para><b>Reduced to paragraph-only on 2026-09-06 (RFC 0009).</b> Until then this pass also
/// added "?" to sentences a model judged to be questions and swapped said→asked on those
/// lines. Both changes were bounded by a word-token guard, but both were still an LLM
/// altering accepted prose on its own reading of a sentence, and the author's ruling is that
/// no instrument may do that. What remains cannot: the only accepted output is one whose
/// text is <b>byte-for-byte identical</b> to the original once whitespace runs are collapsed
/// (<see cref="WhitespaceOnlyChange"/>). A model that touches a single character — a word, a
/// comma, a question mark — is refused and the beat is left exactly as it was. The model
/// decides only where the breaks go; it cannot decide what the words are.</para>
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

    /// <summary><c>QuestionMarksAdded</c> and <c>AttributionSwaps</c> are always 0 since the
    /// 2026-09-06 reduction; the fields are kept so existing report consumers keep compiling.</summary>
    public sealed record BeatReflowResult(
        Guid BeatId, int Position, string Status,
        int QuestionMarksAdded, int AttributionSwaps,
        string? Reason, string BeforePreview, string AfterPreview);

    public sealed record NodeReflowReport(
        Guid NodeId, string Slug, bool Applied,
        int Total, int Changed, int Unchanged, int Rejected, int Errors,
        List<BeatReflowResult> Beats);

    private const string System =
        "You are a print typesetter re-flowing a passage of fiction into paragraphs. You may ONLY " +
        "insert line breaks. You must NEVER change, add, remove, reorder, or respell a single character " +
        "of text — not one word, not one punctuation mark. Output ONLY the re-flowed passage — no " +
        "preamble, no code fences, no commentary.";

    private static string BuildUser(string original) =>
        "Re-flow the PASSAGE below into proper paragraphs ONLY. Separate paragraphs with a single blank line; " +
        "start a new paragraph each time a different character begins to speak, and at natural narrative " +
        "shifts. Keep a speaker's dialogue and its attribution together in one paragraph. " +
        "You may ONLY insert line breaks. The text between the breaks must be byte-for-byte identical " +
        "to the original.\n\n" +
        "PASSAGE:\n" + original;

    /// <summary>Re-flow every beat in the node. With <paramref name="apply"/> false this is a
    /// dry run (nothing written) — the report carries before/after previews so a caller can
    /// show the diff before committing.</summary>
    public async Task<NodeReflowReport> ReflowNodeAsync(Guid nodeId, bool apply, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): explicit nodeId, not an ambient scope (same bug class found and
        // fixed in BookArchiveService.ArchiveAsync/WalkAsync, 2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(s => s.Id == nodeId, ct)
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
                var raw = await llm.GenerateAsync(System, BuildUser(original), temperature: 0.1, maxTokens: 4096, ct: ct);
                edited = StripFences((raw ?? "").Trim());
            }
            catch (Exception ex)
            {
                errors++;
                results.Add(new(beat.Id, pos, "error", 0, 0, ex.Message, Preview(original), ""));
                continue;
            }

            if (edited.Length == 0 || !WhitespaceOnlyChange(original, edited))
            {
                // The model touched a character. That is the one thing this pass exists to
                // refuse — the beat stays exactly as it was.
                rejected++;
                results.Add(new(beat.Id, pos, "rejected", 0, 0,
                    edited.Length == 0 ? "empty model output" : "text changed (only whitespace may differ)",
                    Preview(original), Preview(edited)));
                continue;
            }
            if (edited == original)
            {
                unchanged++;
                results.Add(new(beat.Id, pos, "unchanged", 0, 0, null, Preview(original), Preview(edited)));
                continue;
            }

            if (apply)
                await workbench.UpdateBeatTextAsync(beat.Id, edited, BeatWriteReason.Reflow, expectedUpdatedAt: beat.UpdatedAt, ct);
            changed++;
            results.Add(new(beat.Id, pos, "changed", 0, 0, "paragraphs only", Preview(original), Preview(edited)));
        }

        log.LogInformation("Reflow {Mode} node {Slug}: {Changed} changed, {Unchanged} unchanged, {Rejected} rejected, {Errors} errors",
            apply ? "APPLIED" : "dry-run", node.Slug, changed, unchanged, rejected, errors);
        return new NodeReflowReport(nodeId, node.Slug, apply, ordered.Count, changed, unchanged, rejected, errors, results);
    }

    // ── guard ─────────────────────────────────────────────────────────────

    /// <summary>True iff the two strings differ ONLY in whitespace — same characters, same
    /// order, once every whitespace run is collapsed to a single space and trimmed. This is
    /// the entire acceptance test: not even a punctuation mark may change.</summary>
    internal static bool WhitespaceOnlyChange(string a, string b)
    {
        static string Collapse(string s) => Regex.Replace(s, @"\s+", " ").Trim();
        return Collapse(a) == Collapse(b);
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
