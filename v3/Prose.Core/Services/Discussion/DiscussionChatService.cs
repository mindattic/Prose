using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Prose.Core.Data.Entities;
using Prose.Core.Services.Operator;

namespace Prose.Core.Services.Discussion;

/// <summary>What one exchange produced, what it cost, and who answered.</summary>
/// <param name="CredentialsMissing">No provider has a key. The panel says so and points at
/// Settings rather than throwing — and nothing is spent.</param>
/// <param name="ResolvedIntent">What the request turned out to be — <c>clarify</c> or
/// <c>edit</c>, never <c>auto</c>. When the author picked one, this is that; when they left it on
/// auto, this is what the assistant decided, and it is what gets written to the turn.</param>
/// <param name="Draft">The fenced replacement the assistant offered for the selected passage, if
/// any. Extracted here rather than left in the prose so nothing downstream has to re-parse an
/// answer to find out whether it contained an edit.</param>
public sealed record DiscussionReply(
    IReadOnlyList<DiscussionBlock> Blocks,
    double Cost,
    string? Provider = null,
    bool CredentialsMissing = false,
    string ResolvedIntent = DiscussionIntent.Clarify,
    string? Draft = null);

/// <summary>
/// One turn of conversation about a span of the book.
///
/// <para><b>Transport: <see cref="IToolCallingLlm"/>, not <see cref="Interfaces.ILlmService"/>.</b>
/// That one choice carries four requirements at once — the BYO key pool (resolved live per call,
/// so a key set in Settings works without a restart), sticky failover across a pool,
/// Claude → OpenAI provider fallback, and tool calls, which are how the assistant goes and looks
/// something up instead of recalling it.</para>
///
/// <para>The cost of leaving <c>LlmRouter</c> is that nothing prices the call for us, so this
/// service records real reported usage into the <see cref="TokenLedger"/> itself. An interactive
/// surface showing the author a running total must not show them an estimate.</para>
///
/// <para><b>It answers, argues, or proposes — it never edits.</b> No method here writes prose.
/// A change comes back as a proposal the author approves (RFC 0009), and an edit request may
/// legitimately come back as a refusal instead.</para>
/// </summary>
public sealed class DiscussionChatService(
    IReadOnlyList<IToolCallingLlm> providers,
    DiscussionContextBuilder contextBuilder,
    TokenLedger ledger)
{
    /// <summary>Enough rounds for the assistant to look something up, read the result, and
    /// answer. Not an agent loop — a conversation that can check itself.</summary>
    private const int MaxToolRounds = 4;

    private const string SystemBase = """
        You are helping a novelist interrogate their own manuscript, one passage at a time.

        The author has selected a passage and asked about it. Answer that question — plainly,
        about this passage, in at most a few short paragraphs.

        What you are for:
          - Explain what a passage is doing: what it establishes, what it pays off, what it costs.
          - Defend it when it is doing real work, and say so directly. "This earns its place
            because…" is a complete and useful answer.
          - Say when it does not. A detail that appears all over the book attached to nothing is
            filler; a promise with no payoff is a debt. Name it.
          - Point out contradictions with the beats around it, the stated intent, or the record.

        How to judge whether a detail is held or merely repeated: a detail that belongs to ONE
        referent — one room, one person, one machine — is doing its job however often it recurs.
        The same detail spread across many unrelated places is almost always texture the writer
        reached for rather than something true of the world. The measured counts you are given are
        real; use them, and do not invent others. When you want to know whether something appears
        elsewhere, call find_in_book rather than guessing.

        Hard rules:
          - Do not praise. Do not restate the question. Start with the answer.
          - If what you were given does not settle it, say which part is missing rather than
            guessing. "I cannot tell from this beat alone" is a legitimate answer.
          - Refer to beats by their number, as given.
        """;

    private const string ClarifyRules = """

        This is a CLARIFY request. The author wants to understand the passage, not change it.
        NEVER rewrite it or offer replacement prose. You are reading with them, not editing.
        """;

    private const string EditRules = """

        This is an EDIT request: the author wants this passage changed.
        """
        + EditBody;

    private const string EditBody = """

        Before drafting anything, work out what the passage is carrying. If changing it would
        break something — it plants an obligation paid off later, it is the only place a fact is
        established, it carries a motif, it is load-bearing for a scene that follows — then SAY SO
        AND DO NOT DRAFT. Name what would break and where. A reasoned refusal is a complete answer
        and is often the more useful one; the author can still overrule you.

        If the change is safe, draft a replacement for THE SELECTED PASSAGE ONLY. Match the
        surrounding voice exactly. Do not touch a word outside the selection, and do not smuggle
        in new facts, objects or names that are not already established — inventing detail at
        edit time is precisely how this manuscript acquired the defects you are being asked about.
        Give the replacement on its own, in a fenced block, with one line saying what changed.
        """;

    /// <summary>
    /// The prompt used when the author left the toggle on <c>auto</c> — which is the default, and
    /// in a spoken turn effectively the only setting, since reaching for a control mid-sentence is
    /// the thing hands-free exists to avoid.
    ///
    /// <para><b>One call, not two.</b> Classifying first and answering second would double the
    /// latency of every turn, and perceived silence is the thing most likely to make this loop
    /// unusable. So the model decides and acts in the same generation, and reports which it did in
    /// a trailer.</para>
    ///
    /// <para><b>The trailer comes last on purpose.</b> A verdict emitted before the reasoning is a
    /// verdict the reasoning then rationalises; every structured judgement in this codebase puts
    /// the thinking first. Here that falls out for free: a request the model decides it should
    /// argue against is answered in prose and simply carries no trailer, which is the same shape
    /// as declining to draft.</para>
    /// </summary>
    private const string AutoRules = """

        The author has NOT told you whether this is a question or a request. Work it out.

        A QUESTION wants understanding: "why is this here", "does this land", "what is this
        doing", "is this the only place X appears". Answer it and stop. NEVER rewrite the passage
        or offer replacement prose for a question, and never ask the author to confirm a question —
        answering costs nothing and changes nothing, and a loop that confirms everything trains
        them to confirm without reading.

        A REQUEST wants the passage changed: "cut this", "tighten it", "make her colder", "lose
        the second sentence". Treat an instruction as a request even when it is phrased mildly.

        For a REQUEST only:
        """
        + EditBody
        + """

        Then, and ONLY for a request you are willing to carry out, end your whole reply with this
        trailer, exactly, after everything else:

        ---REQUEST---
        RESTATEMENT: one sentence, in your own words, of what you understood them to want.
        STEPS:
        1. the first thing you would do
        2. the second, and so on — as few as the change really needs
        CAUTION: one line, only if this is worth doing but carries a risk. Omit the line otherwise.

        Do not restate their words back verbatim in RESTATEMENT. Their words may have come from a
        microphone and may be wrong; a verbatim echo of a mis-hearing reads as correct on a
        skim, and a paraphrase does not.

        Omit the trailer entirely when this was a question, or when you are arguing against the
        change rather than offering to make it. No trailer means nothing is waiting on the author.
        """;

    /// <summary>Opens the machine-readable trailer. Chosen to be something no prose answer about a
    /// manuscript would produce on its own.</summary>
    private const string TrailerMarker = "---REQUEST---";

    private static readonly ToolDefinition FindInBook = new(
        "find_in_book",
        "Search this book's prose for an exact phrase. Use it to check whether a detail, name or " +
        "image appears anywhere else before claiming that it does or does not. Returns the beat " +
        "number, the place the beat is set in, and an excerpt for each hit.",
        JsonNode.Parse("""
        {
          "type": "object",
          "properties": {
            "query": { "type": "string", "description": "Exact phrase or word to look for." }
          },
          "required": ["query"]
        }
        """)!);

    /// <summary>The first provider with usable credentials, or null when none has any.</summary>
    private async Task<IToolCallingLlm?> PickProviderAsync()
    {
        foreach (var p in providers)
            if (await p.IsConfiguredAsync()) return p;
        return null;
    }

    /// <summary>Whether anything can answer at all. Callers check this <i>before</i> opening a
    /// thread, so an unconfigured install does not litter the book with questions that were never
    /// asked of anyone.</summary>
    public async Task<bool> IsConfiguredAsync() => await PickProviderAsync() is not null;

    /// <param name="onTextDelta">Fed each fragment as it is generated, for a caller that wants to
    /// start speaking or showing the answer before it is finished. Null asks for the whole turn at
    /// once, which is what a non-interactive caller should do.</param>
    public async Task<DiscussionReply> AskAsync(
        Guid bookNodeId,
        Guid beatId,
        string selection,
        string question,
        string intent,
        IReadOnlyList<DiscussionTurn> priorTurns,
        CancellationToken ct = default,
        Func<string, Task>? onTextDelta = null)
    {
        var provider = await PickProviderAsync();
        if (provider is null)
            return new DiscussionReply([], 0, null, CredentialsMissing: true);

        // Everything this exchange spends is attributed to one scope, so the panel can show the
        // author what the conversation has cost rather than leaving it to a monthly bill.
        using var scope = LlmActionContext.BeginCostScope();
        var previousAction = LlmActionContext.Current;
        LlmActionContext.Current = "discussion";
        LlmActionContext.CurrentBeatId = beatId;

        try
        {
            var context = await contextBuilder.BuildAsync(bookNodeId, beatId, selection, ct);
            var system = SystemBase + intent switch
            {
                DiscussionIntent.Edit => EditRules,
                DiscussionIntent.Auto => AutoRules,
                _ => ClarifyRules,
            };

            var history = new List<ToolLoopMessage>();
            foreach (var t in priorTurns)
            {
                // Transcribe, not Summarize: the latter returns only the first text block, which
                // hid every confirmation the assistant had offered from its own history.
                var line = DiscussionContent.Transcribe(DiscussionContent.Deserialize(t.ContentJson));
                if (string.IsNullOrWhiteSpace(line)) continue;
                history.Add(t.Role == DiscussionRole.Author
                    ? new ToolLoopMessage.UserText(line)
                    : new ToolLoopMessage.AssistantTurn([new AssistantPart.Text(line)]));
            }

            var opening = new StringBuilder();
            opening.AppendLine(DiscussionContextBuilder.ToPrompt(context));
            opening.AppendLine().AppendLine("THE AUTHOR ASKS:").AppendLine(question);
            history.Add(new ToolLoopMessage.UserText(opening.ToString()));

            var blocks = new List<DiscussionBlock>();
            DiscussionBlock.Confirm? confirm = null;

            for (var round = 0; round < MaxToolRounds; round++)
            {
                // Streamed only when someone is listening. A caller with no delta handler gets the
                // buffered path, which is one fewer moving part for anything non-interactive.
                //
                // The gate is what keeps the confirmation protocol out of the author's ears: the
                // trailer arrives in the same token stream as the prose, and a voice reading
                // "dash dash dash REQUEST" aloud is the obvious failure. The full text still
                // reaches the parse below, which works on the assembled turn.
                var gate = new TrailerGate(TrailerMarker);
                var turn = onTextDelta is null
                    ? await provider.CreateTurnAsync(system, history, [FindInBook], 1500, ct)
                    : await provider.CreateTurnStreamingAsync(
                        system, history, [FindInBook], 1500,
                        async fragment =>
                        {
                            var visible = gate.Admit(fragment);
                            if (visible.Length > 0) await onTextDelta(visible);
                        },
                        ct);

                if (onTextDelta is not null)
                {
                    var tail = gate.Flush();
                    if (tail.Length > 0) await onTextDelta(tail);
                }

                // Real reported usage, priced by the shared rate table. Null usage records
                // nothing rather than recording zero — an unknown cost must not read as free.
                if (turn.Usage is { } u)
                    ledger.RecordActual(provider.Name.ToLowerInvariant(), provider.Model,
                                        u.InputTokens, u.OutputTokens);

                foreach (var text in turn.Parts.OfType<AssistantPart.Text>())
                {
                    if (string.IsNullOrWhiteSpace(text.Value)) continue;

                    // The trailer is protocol, not prose. Split it off before the value is ever
                    // rendered — leaving "---REQUEST---" in the panel would be both ugly and a
                    // standing invitation for the author to edit a machine-readable block by hand.
                    var (prose, parsed) = SplitTrailer(text.Value);
                    if (!string.IsNullOrWhiteSpace(prose)) blocks.Add(new DiscussionBlock.Text(prose));
                    confirm ??= parsed;
                }

                var calls = turn.Parts.OfType<AssistantPart.ToolCall>().ToList();
                if (calls.Count == 0) break;

                history.Add(new ToolLoopMessage.AssistantTurn(turn.Parts));

                var results = new List<ToolResultPart>();
                foreach (var call in calls)
                    results.Add(await RunToolAsync(bookNodeId, call, ct));
                history.Add(new ToolLoopMessage.ToolResults(results));
            }

            if (blocks.Count == 0)
                blocks.Add(new DiscussionBlock.Text(
                    "*(No answer came back — the assistant kept looking things up without concluding. "
                    + "Ask again, more narrowly.)*"));

            // The measured echoes ride along as their own blocks — clickable, and impossible to
            // have been imagined, because nothing generated them.
            foreach (var echo in context.Echoes.Where(e => e.PlaceCount > 1).Take(3))
                foreach (var (sampleBeatId, number, place) in echo.Samples.Take(3))
                    blocks.Add(new DiscussionBlock.Quote(
                        sampleBeatId,
                        $"\"{echo.Term}\" also appears here",
                        $"#{number}{(place is null ? "" : $" · {place}")}"));

            // Last, so the thing waiting on the author is the last thing they read — and after the
            // echoes, which are part of what they are being asked to judge.
            if (confirm is not null) blocks.Add(confirm);

            // A trailer is the assistant saying "this was a request and I will act on it", which
            // is the only evidence of the inference there is. Without one, whatever it did was an
            // answer. An explicit toggle always wins: the author has already decided.
            var resolved = intent == DiscussionIntent.Auto
                ? (confirm is not null ? DiscussionIntent.Edit : DiscussionIntent.Clarify)
                : intent;

            // A fenced block in an edit reply is the replacement prose — the system prompt asks for
            // it on its own, exactly so it can be lifted out whole rather than guessed at from the
            // surrounding sentences. A reply still asking for confirmation carries no draft: it has
            // not been agreed to yet.
            var draft = confirm is null && resolved == DiscussionIntent.Edit
                ? FencedBlock.Last(blocks.OfType<DiscussionBlock.Text>().LastOrDefault()?.Markdown)
                : null;

            return new DiscussionReply(blocks, ledger.CostForScope(scope.Id), provider.Name,
                                       ResolvedIntent: resolved, Draft: draft);
        }
        finally
        {
            LlmActionContext.Current = previousAction;
            LlmActionContext.CurrentBeatId = null;
        }
    }

    /// <summary>
    /// Split an assistant reply into the prose the author reads and the confirmation they act on.
    ///
    /// <para><b>Forgiving on the way in, strict on the way out.</b> A model asked for a fixed
    /// trailer will still bold the labels, number the steps with parentheses, or lose the blank
    /// line before the marker — so the parse tolerates all of that. But it returns a
    /// <see cref="DiscussionBlock.Confirm"/> only when a restatement genuinely came back: a
    /// confirmation with nothing in it is a button that asks the author to approve a blank, which
    /// is strictly worse than no confirmation at all, because they will press it.</para>
    ///
    /// <para>Pure and public so it can be tested without a provider. The failure it guards against
    /// — a malformed trailer producing an empty or half-read confirmation — is invisible from the
    /// UI and would only ever be noticed as the assistant "sometimes not offering to act".</para>
    /// </summary>
    public static (string Prose, DiscussionBlock.Confirm? Confirm) SplitTrailer(string? reply)
    {
        var text = reply ?? "";
        var at = text.IndexOf(TrailerMarker, StringComparison.Ordinal);
        if (at < 0) return (text.Trim(), null);

        var prose = text[..at].TrimEnd();
        var trailer = text[(at + TrailerMarker.Length)..];

        string? restatement = null;
        string? caution = null;
        var steps = new List<string>();
        var inSteps = false;

        foreach (var raw in trailer.Split('\n'))
        {
            // Bold and stray markdown around the labels are the model being helpful; they must not
            // decide whether the author gets a confirmation.
            var line = raw.Replace("*", "").Replace("`", "").Trim();
            if (line.Length == 0) continue;

            if (StartsWithLabel(line, "RESTATEMENT", out var r)) { restatement = r; inSteps = false; }
            else if (StartsWithLabel(line, "CAUTION", out var c)) { caution = c; inSteps = false; }
            else if (StartsWithLabel(line, "STEPS", out var first))
            {
                inSteps = true;
                if (first.Length > 0) steps.Add(StripBullet(first));
            }
            else if (inSteps)
            {
                var step = StripBullet(line);
                if (step.Length > 0) steps.Add(step);
            }
        }

        // No restatement means the trailer did not survive whatever the model did to it. Falling
        // back to the author's own words here would be the worst possible behaviour: it would echo
        // a mis-transcription back as confirmation of itself. The raw text is handed back whole
        // rather than trimmed of its broken trailer — an unparsed reply should look wrong, not
        // look like an answer with a piece quietly missing.
        if (string.IsNullOrWhiteSpace(restatement)) return (text.Trim(), null);

        return (prose, new DiscussionBlock.Confirm(
            restatement.Trim(),
            steps,
            string.IsNullOrWhiteSpace(caution) ? null : caution.Trim()));
    }

    /// <summary>A <c>LABEL:</c> line, with whatever followed the colon.</summary>
    private static bool StartsWithLabel(string line, string label, out string rest)
    {
        rest = "";
        if (!line.StartsWith(label, StringComparison.OrdinalIgnoreCase)) return false;

        var after = line[label.Length..].TrimStart();
        if (!after.StartsWith(':')) return false;

        rest = after[1..].Trim();
        return true;
    }

    /// <summary>Drop "1.", "2)", "-" or "•" from the front of a step — the renderer numbers them,
    /// and a step that arrives pre-numbered would come out as "1. 1. …".</summary>
    private static string StripBullet(string line)
    {
        var i = 0;
        while (i < line.Length && char.IsDigit(line[i])) i++;
        if (i > 0 && i < line.Length && (line[i] == '.' || line[i] == ')')) i++;
        else if (i == 0 && line.Length > 0 && (line[0] == '-' || line[0] == '*' || line[0] == '•')) i = 1;
        else i = 0;
        return line[i..].Trim();
    }

    private async Task<ToolResultPart> RunToolAsync(
        Guid bookNodeId, AssistantPart.ToolCall call, CancellationToken ct)
    {
        try
        {
            if (call.Name != FindInBook.Name)
                return new ToolResultPart(call.Id, $"Unknown tool '{call.Name}'.", IsError: true);

            var query = JsonNode.Parse(call.ArgumentsJson)?["query"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(query))
                return new ToolResultPart(call.Id, "A 'query' string is required.", IsError: true);

            var hits = await contextBuilder.FindInBookAsync(bookNodeId, query, 12, ct);
            if (hits.Count == 0)
                return new ToolResultPart(call.Id,
                    $"\"{query}\" appears nowhere else in this book.", IsError: false);

            var places = hits.Select(h => h.Place ?? "(unplaced)").Distinct(StringComparer.OrdinalIgnoreCase).Count();
            var sb = new StringBuilder();
            sb.AppendLine($"\"{query}\" — {hits.Count} beat(s), {places} distinct place(s):");
            foreach (var h in hits)
                sb.AppendLine($"  #{h.Number}{(h.Place is null ? "" : $" [{h.Place}]")}: {h.Excerpt}");
            return new ToolResultPart(call.Id, sb.ToString(), IsError: false);
        }
        catch (JsonException ex)
        {
            return new ToolResultPart(call.Id, $"Arguments were not valid JSON: {ex.Message}", IsError: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A failing tool must come back as a tool result, not an exception: the model can say
            // "I could not check that" and carry on, where a throw loses the whole exchange.
            return new ToolResultPart(call.Id, $"The search failed: {ex.Message}", IsError: true);
        }
    }
}
