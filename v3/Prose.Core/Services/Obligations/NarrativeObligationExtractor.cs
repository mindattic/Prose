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
    public const string PromptVersion = "obl-extract-v1";
    public const int MaxBeatChars = 6000;
    public const int MaxPreviousTailChars = 1500;
    public const int MaxOpenListed = 40;

    public sealed record OpenItem(
        string Kind, string Description, string Quote,
        string? ReferentLabel, string? ReferentType, string? Trigger, string? DueHint);

    public sealed record TouchedItem(int Index, string Verdict, string Quote);

    public sealed record Result(
        IReadOnlyList<OpenItem> Opened,
        IReadOnlyList<TouchedItem> Touched,
        int DiscardedUngrounded,
        string Reasoning,
        bool Evaluated);

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
        var beat = input.BeatText.Length > MaxBeatChars ? input.BeatText[..MaxBeatChars] : input.BeatText;
        var prev = input.PreviousTail is { Length: > MaxPreviousTailChars } p ? p[^MaxPreviousTailChars..] : input.PreviousTail;

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

            THIS BEAT:
            {beat}
            """;

        string raw;
        try
        {
            raw = await llm.GenerateAsync(SystemPrompt, user, temperature: 0.1, maxTokens: 1400, model: LlmModels.Haiku, ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Obligation extraction call failed");
            return new Result([], [], 0, "", Evaluated: false);
        }

        return Parse(raw, beat, input.OpenObligations.Count);
    }

    /// <summary>Parse leniently (first '{' to last '}') and apply the quote gate. Internal so the
    /// contract is unit-testable without an LLM.</summary>
    internal static Result Parse(string? raw, string beatText, int openCount)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new Result([], [], 0, "", Evaluated: false);
        var start = raw.IndexOf('{');
        var end   = raw.LastIndexOf('}');
        if (start < 0 || end <= start) return new Result([], [], 0, "", Evaluated: false);

        JsonDocument doc;
        try { doc = JsonDocument.Parse(raw[start..(end + 1)]); }
        catch (JsonException) { return new Result([], [], 0, "", Evaluated: false); }

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
                        kind!, Truncate(desc!.Trim(), 500), QuoteGrounding.Normalize(quote),
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
                    touched.Add(new TouchedItem(idx, verdict!, QuoteGrounding.Normalize(quote)));
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
