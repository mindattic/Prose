using System.Text.Json;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// The one LLM call of the gate (RFC 0012 §3.4 step 2). Given the brief, the canon facts the
/// writer was shown, and the draft, answers three questions with reasons FIRST (house rule:
/// reason before verdict in every LLM JSON contract): (a) does the draft cross the stop line,
/// (b) which events did it add that the brief does not imply, (c) which shown facts does it
/// contradict. Replaces <c>ContinuityEnforcer</c>, <c>LibertyReportService</c> and
/// <c>SemanticFidelityService</c> as the writer-side check — those ran after the save and could
/// only file findings; this runs before the save and can refuse it.
///
/// <para>Throws on an empty or unparseable response, same posture as ContinuityEnforcer: the
/// caller must not mistake "could not evaluate" for "passed". The router records such a case as
/// <c>unverified</c> and saves (an infrastructure hiccup must not block the author's work), which
/// is a different thing from a pass and is visible on the trace.</para>
/// </summary>
public class BriefVerifier
{
    private readonly ILlmService llm;

    public BriefVerifier(ILlmService llm) { this.llm = llm; }

    public sealed record Verdict(
        string Reasoning,
        bool CrossesStop,
        string? StopEvidence,
        IReadOnlyList<string> AddedEvents,
        IReadOnlyList<string> Contradictions,
        IReadOnlyList<string> MissingFromGoal)
    {
        public bool Passed => !CrossesStop && AddedEvents.Count == 0 && Contradictions.Count == 0;

        /// <summary>The failures phrased as constraints for a retry.</summary>
        public IReadOnlyList<string> AsConstraints()
        {
            var list = new List<string>();
            if (CrossesStop) list.Add("Stop earlier: the draft ran into the next beat" + (string.IsNullOrWhiteSpace(StopEvidence) ? "." : $" — cut everything from: \"{StopEvidence}\""));
            foreach (var e in AddedEvents) list.Add($"Remove this event; the brief does not imply it: {e}");
            foreach (var c in Contradictions) list.Add($"Fix this contradiction of established canon: {c}");
            foreach (var m in MissingFromGoal) list.Add($"The brief requires this and the draft lacks it: {m}");
            return list;
        }
    }

    private const string SystemPrompt = """
        You are the gate on a novel's writing pipeline. You are given THE BRIEF a writer was held
        to, the ESTABLISHED FACTS it was shown, and the DRAFT it produced. You do not judge style.
        You answer three factual questions about the draft, and you reason before you answer.

        Return ONE JSON object and nothing else, with these keys in this order:
          "reasoning": 3-8 sentences. Walk through the draft against the brief: what it does, where
                       it ends, whether that is before or after the STOP line, what happens in it
                       that the brief does not imply, and any fact it contradicts.
          "crosses_stop": true if the draft narrates (not merely anticipates) what the STOP line
                       describes, or ends the chapter/book when told not to. false otherwise.
          "stop_evidence": the first sentence of the draft that belongs to the next beat, or null.
          "added_events": array of short strings, one per event the draft introduces that the brief
                       does not state or clearly imply AND that changes the story's state — something
                       a later beat would have to honour: a new named character or place, an arrival
                       or departure, a message or call that carries plot information, a fight, an
                       injury, a reversal, a decision the brief did not call for. The MECHANICS OF
                       DRAMATIZING the brief's own event are NOT added events: how a stated deal is
                       structured, what the stated conversation says, the gestures, the weather, a
                       passer-by who does nothing, an object noticed. When the brief lists WHAT
                       HAPPENS, anything on that list and anything needed to stage it is implied.
                       Test each candidate: would a later chapter be WRONG if it ignored this? If
                       not — if you would call it minor, logistical, texture, a gesture, a passing
                       remark, or "minimal impact" — it is NOT an added event; leave it out. Only
                       events that fail that test belong here. Empty array if none.
          "contradictions": array of short strings, one per clear factual contradiction of an
                       ESTABLISHED FACT (not interpretive differences, not omissions). The brief's
                       own lines (POV, names) are instructions, not facts — a departure from them is
                       reported under missing_from_goal, never here. Empty if none.
          "missing_from_goal": array of short strings for anything the brief says HAPPENS that the
                       draft does not contain at all. Empty if none.
        Be strict about added_events and crosses_stop; be conservative about contradictions.
        """;

    public async Task<Verdict> VerifyAsync(BeatBrief brief, string? canonFacts, string draft, IReadOnlyList<string>? hints, CancellationToken ct = default)
    {
        var facts = string.IsNullOrWhiteSpace(canonFacts) ? "(none shown)" : canonFacts.Trim();
        var hintBlock = hints is { Count: > 0 } ? "\n\nDETERMINISTIC HINTS (verify, do not assume):\n- " + string.Join("\n- ", hints) : "";
        var user = $"""
            {brief.ToPromptBlock()}

            ESTABLISHED FACTS SHOWN TO THE WRITER:
            {facts}{hintBlock}

            DRAFT:
            {draft}
            """;

        var raw = await llm.GenerateAsync(SystemPrompt, user, temperature: 0.0, maxTokens: 900, model: LlmModels.Haiku, ct: ct);
        return Parse(raw);
    }

    /// <summary>Throws on empty / no-object / malformed JSON — see class remarks.</summary>
    public static Verdict Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException("Empty verifier response.");
        var t = raw.Trim();
        var start = t.IndexOf('{');
        var end = t.LastIndexOf('}');
        if (start < 0 || end <= start) throw new InvalidOperationException($"No JSON object in verifier response: {t[..Math.Min(80, t.Length)]}");
        using var doc = JsonDocument.Parse(t[start..(end + 1)]);
        var r = doc.RootElement;

        static IReadOnlyList<string> Arr(JsonElement e, string key)
        {
            if (!e.TryGetProperty(key, out var a) || a.ValueKind != JsonValueKind.Array) return [];
            return a.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() ?? "" : x.ToString())
                    .Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }

        var reasoning = r.TryGetProperty("reasoning", out var re) && re.ValueKind == JsonValueKind.String ? re.GetString() ?? "" : "";
        var crosses = r.TryGetProperty("crosses_stop", out var cs) && cs.ValueKind == JsonValueKind.True;
        var evidence = r.TryGetProperty("stop_evidence", out var se) && se.ValueKind == JsonValueKind.String ? se.GetString() : null;
        return new Verdict(reasoning, crosses, evidence, Arr(r, "added_events"), Arr(r, "contradictions"), Arr(r, "missing_from_goal"));
    }
}
