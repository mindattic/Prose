using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;
using Prose.Core.Services.Audit;

namespace Prose.Core.Services;

/// <summary>
/// The two DETERMINISTIC craft rules that survived the 2026-09-06 cut (RFC 0010).
///
/// <para>They used to be nested inside <c>BeatChecklistGateService</c>, the LLM-driven binary craft
/// checklist. That service was deleted — corpus apply rate zero, one more instrument with an opinion
/// about a page. These two are different in kind: no model in the loop, a regex over the prose, an
/// answer that is the same every time. The author's 2026-09-06 instrument review kept
/// "CraftChecklist:Native" explicitly. They are hosted here so the deletion of their former parent
/// did not take them with it.</para>
///
/// <para>Contract unchanged: <see cref="Audit.IDeterministicAuditRule"/>, evaluated over an
/// <see cref="Audit.AuditContext"/>, returning <see cref="Audit.AuditVerdict"/> rows.</para>
/// </summary>
public static class CraftNativeRules
{
    /// <summary>
    /// CRAFT §2 / §8.6 — the italic-thought crutch, checked by counting instead of by opinion.
    ///
    /// §2 budgets interiority at "one or two flat lines per scene" and calls italic inner
    /// monologue "a last resort — a single sentence, never a paragraph, never a crutch"; §8.6
    /// bans it outright. Those are countable claims, so no model is asked to judge them.
    ///
    /// <b>Why this is deterministic.</b> TRNY (2026-08-02) shipped as "publication ready" with
    /// 298 italic segments across 91 beats — 3.27 per beat, 84 of 91 beats over budget, one beat
    /// with six. It had passed three logic sweeps and a craft audit. A per-mannerism LLM rule is
    /// asked "does the manuscript do this?" and answers yes-or-no about a clamped blob; it has no
    /// way to say "yes, 298 times, which is 10x the rest of the corpus". A counter does.
    ///
    /// Thresholds are calibrated against the real corpus, not invented: every other book measured
    /// between 0.02 and 0.76 italic segments per beat (median well under 0.3). Above 1.0 per beat
    /// is outside anything else that has shipped, so that is MODERATE; a single beat carrying 3+
    /// is the per-scene "never a paragraph" clause and is cited individually as MINOR.
    /// </summary>
    internal sealed class InteriorityDensityRule : IDeterministicAuditRule
    {
        public string Key => "interiority_density";
        public string Title => "Interiority density (italic-thought crutch)";

        /// <summary>Single-asterisk spans only. Doubles are bold and are not interiority, so
        /// <c>**bold**</c> must not be counted — hence the negative look-around on both sides.</summary>
        static readonly Regex ItalicSpan = new(@"(?<!\*)\*(?!\*)([^*\n]+)\*(?!\*)", RegexOptions.Compiled);

        internal const double PerBeatCeiling = 1.0;
        internal const int SingleBeatCeiling = 3;

        internal static int CountItalics(string text) => ItalicSpan.Matches(text).Count;

        public Task<IReadOnlyList<AuditVerdict>> EvaluateAsync(AuditContext ctx, CancellationToken ct)
        {
            var results = new List<AuditVerdict>();
            if (ctx.Beats.Count == 0) return Task.FromResult<IReadOnlyList<AuditVerdict>>(results);

            var counts = ctx.Beats.Select(b => (Beat: b, N: CountItalics(b.Text))).ToList();
            var total = counts.Sum(c => c.N);
            var perBeat = (double)total / ctx.Beats.Count;

            if (perBeat > PerBeatCeiling)
                results.Add(new AuditVerdict(Key, Title, "MODERATE",
                    $"{total} italic inner-monologue segments across {ctx.Beats.Count} beats " +
                    $"({perBeat:N2} per beat). CRAFT §2 budgets italics as a last resort; §8.6 bans them " +
                    $"as a recurring device. Every other book in the corpus measures under 0.8 per beat.",
                    Location: null,
                    Fix: "Keep at most the single strongest italic per beat; convert the rest to a physical " +
                         "action, to plain free-indirect narration without asterisks, or delete where the " +
                         "surrounding prose already shows it."));

            foreach (var (beat, n) in counts.Where(c => c.N >= SingleBeatCeiling).OrderByDescending(c => c.N))
                results.Add(new AuditVerdict(Key, Title, "MINOR",
                    $"Beat #{beat.Number} carries {n} italic inner-monologue segments in one scene.",
                    Location: beat.Id.ToString(),
                    Fix: "Reduce to one at most; italics are never a paragraph and never a running device."));

            return Task.FromResult<IReadOnlyList<AuditVerdict>>(results);
        }
    }

    /// <summary>
    /// CRAFT §8.2 / §8.3 — retired cognitive-architecture and observation tics, matched literally.
    ///
    /// §8.2 retires "the arithmetic" / "did the math" and any ledger-or-filing framing of how a
    /// character THINKS; §8.3 retires "noted / logged / catalogued / filed" as thought-verbs.
    /// These are named phrases, so they are matched as phrases rather than described to a model.
    ///
    /// <b>The distinction this rule must not get wrong.</b> Literal, diegetic bookkeeping is not a
    /// violation and is frequently the plot — a real ledger chained to a belt, a clerk doing sums,
    /// a toll-master counting coins. Only the metaphorical use (a mind "doing the arithmetic",
    /// a thought being "filed") is retired. The patterns below therefore anchor on a subject
    /// doing the thinking, and the surrounding sentence is always emitted as evidence so a human
    /// can overrule a diegetic hit rather than being told a number with no context. Severity is
    /// MINOR for that reason: this rule points, it does not convict.
    /// </summary>
    internal sealed class RetiredTicRule : IDeterministicAuditRule
    {
        public string Key => "retired_tics";
        public string Title => "Retired cognitive-architecture tics";

        static readonly Regex[] Tics =
        [
            // "the arithmetic" / "the math" only — deliberately NOT "the sum(s)". CRAFT §8.2 names
            // the first two; "did the sum" is overwhelmingly literal in this corpus (a clerk, a
            // quartermaster, a character actually adding a debt up) and flagging it produced a
            // verified false positive on TRNY Ch1's "old Ferrin did the sum twice".
            new(@"\b(?:do(?:es|ing)?|did)\s+the\s+(?:arithmetic|math)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\bthe\s+arithmetic\s+of\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\b(?:arithmetical|arithmetic)\s+(?:clarity|knowledge|certainty|precision)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\bfiled\s+(?:it|that|them|this)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\b(?:noted|logged|catalogued|cataloged)\s+and\s+filed\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\bfiled\s+(?:it\s+)?(?:away\s+)?(?:under|in\s+the\s+same\s+column)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new(@"\bledger\s+in\s+(?:his|her|their|its)\s+head\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        ];

        internal static IReadOnlyList<string> FindTics(string text) =>
            Tics.SelectMany(rx => rx.Matches(text).Select(m => Excerpt(text, m.Index, m.Length))).ToList();

        static string Excerpt(string text, int index, int length)
        {
            var start = Math.Max(0, index - 70);
            var end = Math.Min(text.Length, index + length + 70);
            return "…" + Regex.Replace(text[start..end], @"\s+", " ").Trim() + "…";
        }

        public Task<IReadOnlyList<AuditVerdict>> EvaluateAsync(AuditContext ctx, CancellationToken ct)
        {
            var results = new List<AuditVerdict>();
            foreach (var beat in ctx.Beats)
                foreach (var hit in FindTics(beat.Text))
                    results.Add(new AuditVerdict(Key, Title, "MINOR",
                        $"Beat #{beat.Number}: retired cognitive tic (CRAFT §8.2/§8.3) — {hit}",
                        Location: beat.Id.ToString(),
                        Fix: "Replace with the plain statement or the physical action. If this is literal, " +
                             "diegetic bookkeeping (a real ledger, a clerk actually doing sums), it is not a " +
                             "violation — dismiss the finding."));
            return Task.FromResult<IReadOnlyList<AuditVerdict>>(results);
        }
    }

    // ── rule loading ───────────────────────────────────────────────────────────────
}
