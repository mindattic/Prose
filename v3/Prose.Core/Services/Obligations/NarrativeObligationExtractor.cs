using System.Text.Json;
using Microsoft.Extensions.Logging;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services.Audit;

namespace Prose.Core.Services.Obligations;

/// <summary>
/// The one LLM call of the obligation ledger (RFC 0013): given a beat, the promises the book
/// still owes, and deterministic hints, return what THIS beat opened and which open promises it
/// advanced or closed — each with a verbatim quote. Reason-before-verdict at both levels (house
/// rule, <c>BriefVerifier</c>). Every quote is checked against the beat text in code
/// (<see cref="QuoteGrounding"/>); an item that cannot quote the text is discarded and counted,
/// never argued with.
/// </summary>
public class NarrativeObligationExtractor(ILlmService llm, ILogger<NarrativeObligationExtractor> log)
{
    /// <summary>Folded into <c>Beat.ObligationScanHash</c> by <see cref="NarrativeObligationService.ScanBeatAsync"/>,
    /// so changing how this extractor reads a beat invalidates every stamp and the next rescan
    /// actually rescans. v2 (2026-09-15): long beats are scanned in windows instead of being cut
    /// at <see cref="MaxBeatChars"/> — the v1 cut silently dropped the tail of every beat over
    /// 6,000 chars, which is where the GCSH calibration injected its defects (recall 0.25).
    /// v3 (same day): the OPEN OBLIGATIONS list is chosen by <c>SelectForListing</c> (urgency +
    /// lexical relevance) instead of urgency alone — what the model is shown changed, so the
    /// stamp changes.
    /// v4 (2026-09-16): added an explicit rule that a detail the prose says went unnoticed
    /// ("nobody set it going again", "none of them recognised it") is a strong plant signal — GCSH
    /// calibration run 3 missed a seeded "violet seal ... no one remarked upon it" plant three
    /// runs in a row even after v2's windowing fix, so this was a recall gap in the prompt itself,
    /// not truncation.
    /// v5 (2026-09-16): <c>SelectForListing</c> leads with LOCALITY (debts opened in this chapter)
    /// instead of urgency — run 5 showed closing degrades as the open pile grows (ch1 82%, later
    /// chapters 15–59% on twelve identical self-contained stories). What the model is shown
    /// changed, so the stamp changes.</summary>
    public const string PromptVersion = "obl-extract-v5";

    /// <summary>Characters of beat text per LLM window. A beat longer than this is scanned in
    /// consecutive windows cut at sentence boundaries (<see cref="SplitIntoWindows"/>); every quote
    /// is still gated against the WHOLE beat. Nothing is ever dropped.</summary>
    public const int MaxBeatChars = 6000;
    public const int MaxPreviousTailChars = 1500;
    /// <summary>Open obligations shown to the model per beat. The model can only pay a debt it is
    /// shown, so <see cref="NarrativeObligationService.SelectForListing"/> fills slots by locality,
    /// then lexical relevance, then urgency — on a 257-outstanding book (GCSH run 2) pure urgency
    /// never listed a single book-end plant, so no payoff could ever be recognised at scan time.</summary>
    public const int MaxOpenListed = 40;
    /// <summary>Slots reserved for debts opened in the CURRENT chapter, newest origin first. A
    /// story pays what it has just promised. GCSH run 5 (the first uncontaminated measurement,
    /// 2026-09-16) closed 82% of what chapter 1 opened and 15–59% thereafter across twelve
    /// structurally identical self-contained stories — the only variable being how many older
    /// debts were competing for the same 40 slots. Locality is the fix for that starvation.</summary>
    public const int MaxLocalListed = 15;
    public const int MaxLexicalListed = 15;

    public sealed record OpenItem(
        string Kind, string Description, string Quote,
        string? ReferentLabel, string? ReferentType, string? Trigger, string? DueHint);

    public sealed record TouchedItem(int Index, string Verdict, string Quote);

    public sealed record Result(
        IReadOnlyList<OpenItem> Opened,
        IReadOnlyList<TouchedItem> Touched,
        int DiscardedUngrounded,
        string Reasoning,
        bool Evaluated)
    {
        /// <summary>Why <see cref="Evaluated"/> is false, for the log. Null when the read succeeded.
        /// Before 2026-09-16 every parse failure returned <c>Evaluated: false</c> silently, so a beat
        /// the extractor could not read was indistinguishable from a beat that owed nothing — and
        /// GCSH run 6 left 41 of 96 beats unread with nothing in the output saying so.</summary>
        public string? Failure { get; init; }
    }

    public sealed record Input(
        string BeatText,
        string? PreviousTail,
        IReadOnlyList<NarrativeObligation> OpenObligations,
        IReadOnlyList<string> TaggedEntityNames,
        IReadOnlyList<UnnamedReferentScanner.Referent> Hints,
        IReadOnlyList<string> StopListExamples);

    private const string SystemPrompt = """
        You are the continuity ledger of a novel. You read ONE beat of prose and record what the
        story now OWES the reader because of it, and which existing debts this beat pays.

        A debt ("obligation") is anything a careful reader will expect the book to come back to:
        a person or object introduced with weight but no name or explanation (a girl watching from
        behind a curtain; a child hidden in a vent with no reason to be there), a question raised,
        a promise or threat made, a wound taken, a detail planted to pay off later. Background
        texture — a passer-by who does nothing, weather, furniture — is NOT a debt.

        One signal is easy to miss and almost always real: the prose explicitly saying a detail
        went UNNOTICED — "nobody set it going again", "none of them recognised it", "no one
        remarked upon it", "he did not ask why". A narrator does not spend a sentence telling you
        a thing was ignored unless it matters later. Treat this phrasing as a strong plant signal
        even when the detail itself (a clock, a seal, a color) looks like minor texture.

        Return ONE JSON object and nothing else, keys in this order:
          "reasoning": 3-6 sentences. What this beat sets up that is not resolved inside it; which
                       of the OPEN OBLIGATIONS it touches and how.
          "opened": array, one object per NEW debt this beat creates, each with keys in order:
              "reasoning": one sentence — why a reader will expect this to return.
              "quote": the VERBATIM sentence (or clause, at least 12 characters) from THIS BEAT
                       that makes the promise. Copy it exactly; do not paraphrase.
              "kind": one of promise | question | plant | wound | foreshadow |
                      introduced-referent | unexplained-presence
              "description": what is owed, one line, max 200 characters.
              "referent": null, or {"label": "girl behind the curtain", "type": "character"|"place"|"object"}
                          when the debt is about an unnamed person/place/thing.
              "trigger": null, or the narrative condition under which paying this becomes natural.
              "due_hint": one of "soon" (within a few beats) | "chapter" (by chapter end) |
                          "book" (any time before the end) | null.
          "touched": array, one object per OPEN OBLIGATION (by its listed number) this beat
                     advances or closes:
              "reasoning": one sentence.
              "index": the number from the OPEN OBLIGATIONS list.
              "quote": the VERBATIM sentence from THIS BEAT that advances or closes it.
              "verdict": "advanced" | "closed"  — "closed" only when the debt is definitively paid.
        Rules: quote from THIS BEAT only, never from the previous text. Empty arrays when nothing
        applies. Do not list something as opened if it is already in OPEN OBLIGATIONS — list it
        under "touched" instead. Do not invent debts to seem thorough; a beat that only dramatises
        a stated event usually opens nothing.
        """;

    public async Task<Result> ExtractAsync(Input input, CancellationToken ct = default)
    {
        var windows = SplitIntoWindows(input.BeatText, MaxBeatChars);
        if (windows.Count == 0) return new Result([], [], 0, "", Evaluated: false);
        if (windows.Count > 1)
            log.LogInformation("Obligation extraction: {Chars}-char beat scanned in {Windows} windows of ≤{Max} chars", input.BeatText.Length, windows.Count, MaxBeatChars);

        var parts = new List<Result>(windows.Count);
        for (var w = 0; w < windows.Count; w++)
        {
            // Each window sees the tail of the previous one as context so a promise that straddles
            // the cut is still legible; quotes are gated against the full beat, not the window.
            var prev = w == 0 ? input.PreviousTail : windows[w - 1];
            var part = await ExtractWindowAsync(input, windows[w], prev, w, windows.Count, ct);
            if (!part.Evaluated)
            {
                // One unreadable window voids the whole beat, including the windows that DID read —
                // the beat goes unstamped so a later pass retries it as a unit rather than banking a
                // partial read as complete.
                log.LogWarning("Obligation extraction UNREAD: window {Window}/{Count} of a {Chars}-char beat — {Failure}. The whole beat is discarded and left unstamped.",
                    w + 1, windows.Count, input.BeatText.Length, part.Failure ?? "no reason recorded");
                return new Result([], [], part.DiscardedUngrounded, part.Reasoning, Evaluated: false) { Failure = part.Failure };
            }
            parts.Add(part);
        }
        return parts.Count == 1 ? parts[0] : Merge(parts);
    }

    /// <summary>Cut <paramref name="text"/> into consecutive pieces of at most <paramref name="max"/>
    /// chars, each ending at a sentence boundary where one exists in the back half of the window.
    /// The pieces concatenate (modulo trimmed spaces) back to the input — nothing is dropped.</summary>
    public static List<string> SplitIntoWindows(string text, int max)
    {
        var windows = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return windows;
        var pos = 0;
        while (text.Length - pos > max)
        {
            var end = pos + max;
            var cut = LastSentenceEnd(text, pos + max / 2, end);
            if (cut < 0) cut = end;
            var piece = text[pos..cut].Trim();
            if (piece.Length > 0) windows.Add(piece);
            pos = cut;
        }
        var last = text[pos..].Trim();
        if (last.Length > 0) windows.Add(last);
        return windows;
    }

    /// <summary>Index just past the last sentence terminator (. ! ? plus any closing quote marks)
    /// that is followed by whitespace, searching <c>[min, end)</c>; -1 when none.</summary>
    private static int LastSentenceEnd(string text, int min, int end)
    {
        for (var i = end - 2; i >= min; i--)
        {
            if (text[i] is not ('.' or '!' or '?')) continue;
            var j = i + 1;
            while (j < end && text[j] is '"' or '”' or '’' or '\'' or ')' ) j++;
            if (j < text.Length && char.IsWhiteSpace(text[j])) return j;
        }
        return -1;
    }

    /// <summary>Combine per-window results for one beat: opened items de-duplicated by description
    /// or quote; a touched index reported by several windows keeps "closed" over "advanced".</summary>
    internal static Result Merge(IReadOnlyList<Result> parts)
    {
        var discarded = parts.Sum(p => p.DiscardedUngrounded);
        var reasoning = string.Join(" ", parts.Select(p => p.Reasoning).Where(r => !string.IsNullOrWhiteSpace(r)));
        if (parts.Any(p => !p.Evaluated)) return new Result([], [], discarded, reasoning, Evaluated: false);

        var opened = new List<OpenItem>();
        var seenDesc = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenQuote = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var o in parts.SelectMany(p => p.Opened))
        {
            if (!seenDesc.Add(o.Description) || !seenQuote.Add(o.Quote)) continue;
            opened.Add(o);
        }
        var touched = parts.SelectMany(p => p.Touched)
            .GroupBy(t => t.Index)
            .Select(g => g.FirstOrDefault(t => t.Verdict == "closed") ?? g.First())
            .OrderBy(t => t.Index)
            .ToList();
        return new Result(opened, touched, discarded, reasoning, Evaluated: true);
    }

    private async Task<Result> ExtractWindowAsync(Input input, string window, string? previous, int index, int count, CancellationToken ct)
    {
        var prev = previous is { Length: > MaxPreviousTailChars } p ? p[^MaxPreviousTailChars..] : previous;
        var windowNote = count == 1 ? "" : $" (part {index + 1} of {count} of one long beat — the earlier parts are the PREVIOUS TEXT)";

        var openBlock = input.OpenObligations.Count == 0
            ? "None."
            : string.Join("\n", input.OpenObligations.Take(MaxOpenListed).Select((o, i) =>
                $"{i + 1}. [{o.Kind}] {o.Description}" + (string.IsNullOrEmpty(o.OriginQuote) ? "" : $" — \"{Truncate(o.OriginQuote, 120)}\"")));

        var hintBlock = input.Hints.Count == 0
            ? "None."
            : string.Join("\n", input.Hints.Select(h => $"- \"{h.Label}\" ({h.Mentions} mention(s){(h.SubjectPosition ? ", acts" : "")})"));

        var stopBlock = input.StopListExamples.Count == 0
            ? ""
            : "\n\nNOT OBLIGATIONS (the author has ruled these background texture in this universe):\n" +
              string.Join("\n", input.StopListExamples.Take(12).Select(s => $"- {s}"));

        var user = $"""
            OPEN OBLIGATIONS (numbered; what the book already owes):
            {openBlock}

            ENTITIES TAGGED IN THIS BEAT (named, already on record — not unnamed referents):
            {(input.TaggedEntityNames.Count == 0 ? "None." : string.Join(", ", input.TaggedEntityNames))}

            UNNAMED REFERENTS A SCANNER NOTICED (hints only; decide for yourself):
            {hintBlock}{stopBlock}

            PREVIOUS TEXT (context only — never quote from it):
            {(string.IsNullOrWhiteSpace(prev) ? "(start of chapter)" : prev)}

            THIS BEAT{windowNote}:
            {window}
            """;

        string raw;
        try
        {
            raw = await llm.GenerateAsync(SystemPrompt, user, temperature: 0.1, maxTokens: 1400, model: LlmModels.Haiku, ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Obligation extraction call failed (window {Index}/{Count})", index + 1, count);
            return new Result([], [], 0, "", Evaluated: false);
        }

        // Gate against the WHOLE beat: a quote is a literal substring of the text the row anchors to.
        return Parse(raw, input.BeatText, input.OpenObligations.Count);
    }

    /// <summary>Parse leniently (first '{' to last '}') and apply the quote gate. Internal so the
    /// contract is unit-testable without an LLM.</summary>
    internal static Result Parse(string? raw, string beatText, int openCount)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new Result([], [], 0, "", Evaluated: false) { Failure = "empty response" };
        var start = raw.IndexOf('{');
        var end   = raw.LastIndexOf('}');
        // No closing brace after an opening one is the signature of a response cut off at the token
        // ceiling — which happens on exactly the beats that open the MOST obligations, so the reads
        // this drops are the richest ones in the book, not a random sample.
        if (start < 0 || end <= start)
            return new Result([], [], 0, "", Evaluated: false)
                { Failure = $"no JSON object in {raw.Length}-char response{(start >= 0 ? " (opened but never closed — truncated at the token ceiling)" : "")}" };

        JsonDocument doc;
        try { doc = JsonDocument.Parse(raw[start..(end + 1)]); }
        catch (JsonException ex) { return new Result([], [], 0, "", Evaluated: false) { Failure = $"malformed JSON in {raw.Length}-char response: {ex.Message}" }; }

        using (doc)
        {
            var root = doc.RootElement;
            var reasoning = root.TryGetProperty("reasoning", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString() ?? "" : "";
            var opened = new List<OpenItem>();
            var touched = new List<TouchedItem>();
            var discarded = 0;

            if (root.TryGetProperty("opened", out var openedEl) && openedEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in openedEl.EnumerateArray())
                {
                    var quote = Str(item, "quote");
                    var kind  = Str(item, "kind")?.Trim().ToLowerInvariant();
                    var desc  = Str(item, "description");
                    if (!ObligationKind.IsValid(kind) || string.IsNullOrWhiteSpace(desc)) { discarded++; continue; }
                    if (!QuoteGrounding.Contains(beatText, quote, QuoteGrounding.MinObligationQuoteLength)) { discarded++; continue; }

                    string? refLabel = null, refType = null;
                    if (item.TryGetProperty("referent", out var refEl) && refEl.ValueKind == JsonValueKind.Object)
                    {
                        refLabel = Str(refEl, "label");
                        refType  = Str(refEl, "type")?.Trim().ToLowerInvariant();
                        if (refType is not ("character" or "place" or "object")) refType = null;
                        if (string.IsNullOrWhiteSpace(refLabel)) { refLabel = null; refType = null; }
                    }

                    var due = Str(item, "due_hint")?.Trim().ToLowerInvariant();
                    if (due is not ("soon" or "chapter" or "book")) due = null;

                    opened.Add(new OpenItem(
                        kind!, Truncate(desc!.Trim(), 500), QuoteGrounding.ClampForStorage(quote),
                        refLabel, refType, NullIfBlank(Str(item, "trigger")), due));
                }
            }

            if (root.TryGetProperty("touched", out var touchedEl) && touchedEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in touchedEl.EnumerateArray())
                {
                    var idx = item.TryGetProperty("index", out var i) && i.ValueKind == JsonValueKind.Number && i.TryGetInt32(out var n) ? n : 0;
                    var verdict = Str(item, "verdict")?.Trim().ToLowerInvariant();
                    var quote = Str(item, "quote");
                    if (idx < 1 || idx > openCount || verdict is not ("advanced" or "closed")) { discarded++; continue; }
                    if (!QuoteGrounding.Contains(beatText, quote, QuoteGrounding.MinObligationQuoteLength)) { discarded++; continue; }
                    touched.Add(new TouchedItem(idx, verdict!, QuoteGrounding.ClampForStorage(quote)));
                }
            }

            return new Result(opened, touched, discarded, reasoning, Evaluated: true);
        }
    }

    private static string? Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
