using Microsoft.Extensions.Logging.Abstractions;
using Prose.Core.Interfaces;
using Prose.Core.Services.Audit;
using Prose.Core.Services.Obligations;

namespace Prose.UnitTests;

/// <summary>
/// The extractor's contract on LONG beats and LONG quotes, pinned after the 2026-09-15 GCSH
/// calibration: v1 cut every beat at 6,000 chars before both the prompt and the quote gate, so a
/// promise in the tail of a 6,500-char beat could never be opened (recall 0.25 on injected
/// defects); and a paragraph-length "quote" that passed the gate overflowed the nvarchar(400)
/// columns and failed the whole save batch after $2 of judge calls. Neither may recur.
/// </summary>
[TestFixture]
public class NarrativeObligationExtractorTests
{
    private const string Promise = "A girl in a grey shawl watched from behind the curtain and did not move when he looked at her.";

    private static string LongBeat(int sentences)
    {
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < sentences; i++)
            sb.Append($"Sentence {i} of the long beat rolled on through the rain toward the corner, and nobody said anything. ");
        sb.Append(Promise);
        return sb.ToString();
    }

    private static string OpenedJson(string quote) =>
        $$"""
        {"reasoning":"A watcher is introduced and never explained.",
         "opened":[{"reasoning":"weighted unnamed watcher","quote":"{{quote}}","kind":"introduced-referent","description":"the girl in the grey shawl behind the curtain",
                    "referent":{"label":"girl in the grey shawl","type":"character"},"trigger":null,"due_hint":"chapter"}],
         "touched":[]}
        """;

    private const string Nothing = """{"reasoning":"nothing","opened":[],"touched":[]}""";

    // ── windows ───────────────────────────────────────────────────────────────────

    [Test]
    public void SplitIntoWindows_KeepsEveryCharacter_CutsAtSentenceEnds_NeverExceedsMax()
    {
        var text = LongBeat(140);   // ~14k chars
        Assert.That(text.Length, Is.GreaterThan(2 * NarrativeObligationExtractor.MaxBeatChars));

        var windows = NarrativeObligationExtractor.SplitIntoWindows(text, NarrativeObligationExtractor.MaxBeatChars);

        Assert.That(windows.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(windows.All(w => w.Length <= NarrativeObligationExtractor.MaxBeatChars), Is.True);
        Assert.That(string.Join(" ", windows), Is.EqualTo(text.Trim()), "nothing dropped, nothing duplicated");
        foreach (var w in windows.Take(windows.Count - 1))
            Assert.That(w, Does.EndWith("."), "each cut lands on a sentence end");
        Assert.That(windows[^1], Does.EndWith(Promise), "the tail — where the calibration injects — is in the last window");
    }

    [Test]
    public void SplitIntoWindows_ShortBeat_IsOneWindow()
    {
        var windows = NarrativeObligationExtractor.SplitIntoWindows("Short beat. " + Promise, 6000);
        Assert.That(windows, Has.Count.EqualTo(1));
        Assert.That(NarrativeObligationExtractor.SplitIntoWindows("   ", 6000), Is.Empty);
    }

    [Test]
    public void SplitIntoWindows_ABeatJustOverTheLimit_IsOneWindow_NotAWindowPlusAScrap()
    {
        // The GCTOC shape: 6,255 chars against a 6,000 limit. Before the fold this split into
        // ~5,900 + a 355-char scrap. The model answers a scrap in prose, not JSON, and ONE
        // unreadable window voids the whole beat — which is how GCSH run 6 lost 41 of 96 beats
        // and GCTOC 5 of 17, in both cases exactly the over-length ones.
        var sentence = "The mail coach laboured up the hill through the mud and the steam of its own horses. ";
        var text = string.Concat(Enumerable.Repeat(sentence, 75));   // ~6,300 chars
        Assert.That(text.Length, Is.GreaterThan(6000).And.LessThan(7200), "the just-over-the-limit shape");

        var windows = NarrativeObligationExtractor.SplitIntoWindows(text, 6000);

        Assert.That(windows, Has.Count.EqualTo(1), "a 4% overflow must not cost the beat its whole read");
        Assert.That(windows[0], Is.EqualTo(text.Trim()), "nothing dropped by the fold");
    }

    [Test]
    public void SplitIntoWindows_FoldsOnlyAScrap_NotASubstantialTail()
    {
        var sentence = "The mail coach laboured up the hill through the mud and the steam of its own horses. ";
        var text = string.Concat(Enumerable.Repeat(sentence, 200));   // ~16,800 chars
        var windows = NarrativeObligationExtractor.SplitIntoWindows(text, 6000);

        Assert.That(windows.Count, Is.GreaterThanOrEqualTo(3), "a genuinely long beat still windows");
        Assert.That(windows[^1].Length, Is.GreaterThanOrEqualTo(6000 / 5), "a real tail is left alone");
        Assert.That(string.Join(" ", windows), Is.EqualTo(text.Trim()), "nothing dropped, nothing duplicated");
    }

    [Test]
    public void SplitIntoWindows_NoSentenceBoundary_HardCutsRatherThanDropping()
    {
        var text = new string('x', 15000);
        var windows = NarrativeObligationExtractor.SplitIntoWindows(text, 6000);
        Assert.That(windows.Select(w => w.Length).Sum(), Is.EqualTo(15000));
        Assert.That(windows.All(w => w.Length <= 6000), Is.True);
    }

    [Test]
    public async Task ExtractAsync_LongBeat_PromiseInTheTail_IsOpenedAndGroundedAgainstTheWholeBeat()
    {
        var text = LongBeat(140);
        var llm = new ScriptedLlm();
        var windows = NarrativeObligationExtractor.SplitIntoWindows(text, NarrativeObligationExtractor.MaxBeatChars).Count;
        for (var i = 0; i < windows - 1; i++) llm.Enqueue(Nothing);
        llm.Enqueue(OpenedJson(Promise));
        var extractor = new NarrativeObligationExtractor(llm, NullLogger<NarrativeObligationExtractor>.Instance);

        var r = await extractor.ExtractAsync(new NarrativeObligationExtractor.Input(text, null, [], [], [], []));

        Assert.That(r.Evaluated, Is.True);
        Assert.That(llm.Calls, Is.EqualTo(windows), "one call per window, no more");
        Assert.That(r.Opened, Has.Count.EqualTo(1));
        Assert.That(QuoteGrounding.Contains(text, r.Opened[0].Quote, QuoteGrounding.MinObligationQuoteLength), Is.True);
        Assert.That(llm.LastUser, Does.Contain("part " + windows + " of " + windows), "the model is told it is reading a part");
        Assert.That(llm.LastUser, Does.Contain(Promise), "the tail actually reached the prompt");
    }

    // ── v8: retry, halve, and bank what read ──────────────────────────────────────
    // Until v8 a window got exactly one attempt and one bad window discarded the whole beat. There
    // was no retry anywhere in this pipeline, and LlmRouter's provider failover does not cover it:
    // a response truncated at the token ceiling is a *successful* HTTP call, so nothing asked again.

    [Test]
    public async Task AFailedWindow_IsRetried_AndASecondGoodAnswerIsAccepted()
    {
        var text = "A short beat. " + Promise;
        var llm = new ScriptedLlm();
        llm.Enqueue("I'm afraid I can't answer that.");   // unparseable — no JSON object
        llm.Enqueue(OpenedJson(Promise));
        var extractor = new NarrativeObligationExtractor(llm, NullLogger<NarrativeObligationExtractor>.Instance);

        var r = await extractor.ExtractAsync(new NarrativeObligationExtractor.Input(text, null, [], [], [], []));

        Assert.That(r.Evaluated, Is.True, "a retry that lands is a read, not a failure");
        Assert.That(r.Opened, Has.Count.EqualTo(1));
        Assert.That(llm.Calls, Is.EqualTo(2));
        Assert.That(r.WindowsRead, Is.EqualTo(1));
        Assert.That(r.WindowsTotal, Is.EqualTo(1));
    }

    [Test]
    public async Task AShortWindowThatNeverParses_IsUnread_AndIsNotHalved()
    {
        var text = "A short beat that never parses. " + Promise;
        Assert.That(text.Length, Is.LessThan(NarrativeObligationExtractor.MinResplitChars),
            "this test is about the no-halving path; keep the beat under the threshold");
        var llm = new ScriptedLlm();
        llm.Enqueue("not json"); llm.Enqueue("still not json");
        var extractor = new NarrativeObligationExtractor(llm, NullLogger<NarrativeObligationExtractor>.Instance);

        var r = await extractor.ExtractAsync(new NarrativeObligationExtractor.Input(text, null, [], [], [], []));

        Assert.That(r.Evaluated, Is.False);
        Assert.That(r.WindowsRead, Is.EqualTo(0), "nothing was read, so this is UNREAD and not PARTIAL");
        Assert.That(r.Failure, Is.Not.Null.And.Contains("window 1/1"));
        Assert.That(llm.Calls, Is.EqualTo(2), "two attempts, then give up — a short window is not worth halving");
    }

    [Test]
    public async Task OneUnreadableWindow_NoLongerDiscardsTheWindowsThatRead()
    {
        // The GCTOC defect in miniature: a beat whose first window reads perfectly and whose
        // second cannot be parsed. Before v8 this returned nothing at all, throwing away a
        // completely good read of ~5,900 characters because of what came after it.
        var text = LongBeat(140);
        var total = NarrativeObligationExtractor.SplitIntoWindows(text, NarrativeObligationExtractor.MaxBeatChars).Count;
        Assert.That(total, Is.GreaterThanOrEqualTo(3));

        var llm = new ScriptedLlm();
        llm.Enqueue(OpenedJson(Promise));        // window 1 reads, and opens something
        llm.Enqueue(null); llm.Enqueue(null); llm.Enqueue(null);  // window 2: attempt, retry, first half
        var extractor = new NarrativeObligationExtractor(llm, NullLogger<NarrativeObligationExtractor>.Instance);

        var r = await extractor.ExtractAsync(new NarrativeObligationExtractor.Input(text, null, [], [], [], []));

        Assert.That(r.Evaluated, Is.False, "a partial read must never stamp the beat as done");
        Assert.That(r.WindowsRead, Is.EqualTo(total - 1), "every window but the broken one was read");
        Assert.That(r.WindowsTotal, Is.EqualTo(total));
        Assert.That(r.Opened, Has.Count.EqualTo(1),
            "the opened item from the window that DID read is banked rather than discarded");
        Assert.That(r.Failure, Is.Not.Null.And.Contains("window 2/"));
    }

    [Test]
    public void Merge_DedupesOpenedAcrossWindows_AndPrefersClosedForTouched()
    {
        var a = new NarrativeObligationExtractor.OpenItem("promise", "the same debt", "quote one that is long enough", null, null, null, null);
        var b = new NarrativeObligationExtractor.OpenItem("promise", "THE SAME DEBT", "quote two that is long enough", null, null, null, null);
        var c = new NarrativeObligationExtractor.OpenItem("plant", "another debt", "quote one that is long enough", null, null, null, null);
        var p1 = new NarrativeObligationExtractor.Result([a], [new(3, "advanced", "advance quote long enough")], 1, "r1", true);
        var p2 = new NarrativeObligationExtractor.Result([b, c], [new(3, "closed", "close quote long enough"), new(5, "advanced", "q5 long enough here")], 2, "r2", true);

        var m = NarrativeObligationExtractor.Merge([p1, p2]);

        Assert.That(m.Evaluated, Is.True);
        Assert.That(m.Opened, Has.Count.EqualTo(1), "same description (case-insensitive) or same quote = one debt");
        Assert.That(m.Touched.Select(t => (t.Index, t.Verdict)), Is.EqualTo(new[] { (3, "closed"), (5, "advanced") }));
        Assert.That(m.DiscardedUngrounded, Is.EqualTo(3));
        Assert.That(m.Reasoning, Is.EqualTo("r1 r2"));
    }

    // ── quote length ──────────────────────────────────────────────────────────────

    [Test]
    public void Parse_ParagraphLengthQuote_IsClampedTo400_AndStillGrounds()
    {
        var paragraph = string.Concat(Enumerable.Range(0, 12).Select(i => $"Clause {i} of a very long sentence the model pasted whole instead of quoting, "));
        var beat = "Before. " + paragraph + "after.";
        Assert.That(paragraph.Length, Is.GreaterThan(QuoteGrounding.MaxStoredQuoteLength));

        var r = NarrativeObligationExtractor.Parse(OpenedJson(paragraph.Trim()), beat, 0);

        Assert.That(r.Opened, Has.Count.EqualTo(1));
        Assert.That(r.Opened[0].Quote.Length, Is.LessThanOrEqualTo(QuoteGrounding.MaxStoredQuoteLength));
        Assert.That(r.Opened[0].Quote, Does.Not.EndWith(" "));
        Assert.That(QuoteGrounding.Contains(beat, r.Opened[0].Quote, QuoteGrounding.MinObligationQuoteLength), Is.True, "a prefix of a substring is a substring");
    }

    [TestCase(10)]
    [TestCase(399)]
    [TestCase(400)]
    public void ClampForStorage_ShortEnough_IsJustNormalised(int length)
    {
        var q = new string('a', length);
        Assert.That(QuoteGrounding.ClampForStorage("  " + q + "\n "), Is.EqualTo(q));
    }

    [Test]
    public void ClampForStorage_CutsAtAWordBoundary_UnderTheLimit()
    {
        var words = string.Join(" ", Enumerable.Range(0, 120).Select(i => $"word{i}"));   // ~800 chars
        var clamped = QuoteGrounding.ClampForStorage(words);
        Assert.That(clamped.Length, Is.LessThanOrEqualTo(QuoteGrounding.MaxStoredQuoteLength));
        Assert.That(words, Does.StartWith(clamped));
        Assert.That(words[clamped.Length], Is.EqualTo(' '), "cut lands on a word boundary");
    }

    [Test]
    public void ClampForStorage_NoSpaces_HardCutsAtTheLimit()
    {
        var clamped = QuoteGrounding.ClampForStorage(new string('z', 1000));
        Assert.That(clamped.Length, Is.EqualTo(QuoteGrounding.MaxStoredQuoteLength));
    }

    // ── doubles ───────────────────────────────────────────────────────────────────

    private sealed class ScriptedLlm : ILlmService
    {
        private readonly Queue<string?> responses = new();
        public int Calls;
        public string LastUser = "";
        public void Enqueue(string? response) => responses.Enqueue(response);
        public Task<bool> IsConfiguredAsync() => Task.FromResult(true);
        public Task<string> GenerateAsync(string system, string user, double temperature = 0.8, int maxTokens = 4096, string? model = null, CancellationToken ct = default)
        {
            Calls++;
            LastUser = user;
            if (responses.Count == 0) return Task.FromResult("""{"reasoning":"nothing","opened":[],"touched":[]}""");
            var next = responses.Dequeue();
            if (next == null) throw new InvalidOperationException("simulated provider failure");
            return Task.FromResult(next);
        }
    }
}
