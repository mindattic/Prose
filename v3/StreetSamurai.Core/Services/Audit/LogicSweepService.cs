using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Services;

namespace StreetSamurai.Core.Services.Audit;

/// <summary>
/// Codifies docs/LOGIC.md's six-dimension sweep (SS-A44) as six independent
/// <see cref="ILlmAuditRule"/>s on the shared <see cref="AuditRunner"/> — causality chain,
/// knowledge states, timeline, plant/payoff (two-way), orphan references, bible agreement.
///
/// <b>Honest scope note:</b> this is a single LLM call per dimension over the WHOLE node's
/// prose (truncated for an oversized book, like BookAuditService's ClampProse). The
/// <c>/logic-sweep</c> Claude Code skill — what actually ran on VIGL's 321-beat sweep this
/// session — splits a big book across several range-scoped subagents that each read their
/// slice closely, then a mechanical quote-verification pass, then triage, then a separate
/// fix pass, then re-verification. That's real thoroughness a single prompt over a clamped
/// 100k-character corpus cannot match on a long book. Use THIS service for a small-to-medium
/// book or as an automatable coarse gate (CI, a scheduled check, "has anything gotten
/// obviously worse since last time") — reach for the skill when you actually need the
/// thorough version.
///
/// Findings persist through the standard delete-then-recreate lifecycle
/// (AuditRunner.RunAsync), same as every other rule on this abstraction — a re-run
/// automatically clears findings for a dimension that's gone clean.
/// </summary>
public class LogicSweepService(
    AuditRunner auditRunner,
    PlantPayoffService plantPayoffs,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory)
{
    public async Task<LogicSweepReport> RunAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        var childChapters = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == nodeId && n is ChapterNode)
            .Select(n => n.Id)
            .ToListAsync(ct);
        var nodeIds = childChapters.Count > 0 ? childChapters : [nodeId];

        var beatRows = await db.BeatNodes.AsNoTracking().Include(bn => bn.Beat)
            .Where(bn => nodeIds.Contains(bn.NodeId) && bn.IsEnabled && bn.Beat != null
                      && bn.Beat!.Text != null && bn.Beat.Text != "")
            .OrderBy(bn => bn.SortKey)
            .Select(bn => new { bn.Beat!.Id, bn.Beat.Number, bn.Beat.Text, bn.SortKey })
            .ToListAsync(ct);

        if (beatRows.Count == 0)
            return new LogicSweepReport(nodeId, node.Slug, node.Title, 0, []);

        var beats = beatRows.Select(b => new AuditBeat(b.Id, b.Number, b.Text, b.SortKey)).ToList();
        var prose = string.Join("\n\n", beats.Select(b => $"[Beat #{b.Number}]\n{b.Text}"));

        // A few distinctive disabled-beat snippets so OrphanReferencesRule can spot a live beat
        // still referencing something a cut beat established — an approximation of the skill's
        // "grep every disabled beat's distinctive phrase" step, not a full replacement for it.
        var disabledSnippets = await db.BeatNodes.AsNoTracking().Include(bn => bn.Beat)
            .Where(bn => nodeIds.Contains(bn.NodeId) && !bn.IsEnabled && bn.Beat != null && bn.Beat!.Text != null)
            .OrderBy(bn => bn.SortKey)
            .Select(bn => bn.Beat!.Text.Length > 200 ? bn.Beat.Text.Substring(0, 200) : bn.Beat.Text)
            .Take(40)
            .ToListAsync(ct);

        var plants = await plantPayoffs.GetByNodeAsync(nodeId, ct);

        var extra = new Dictionary<string, object?>
        {
            ["bible"]             = node.NodeBible,
            ["plants"]            = plants,
            ["disabledSnippets"]  = disabledSnippets,
        };
        var ctx = new AuditContext(nodeId, node.UniverseId, AuditProseUtils.ClampProse(prose), beats, extra);

        IReadOnlyList<IAuditRule> rules =
        [
            new CausalityChainRule(),
            new KnowledgeStatesRule(),
            new TimelineRule(),
            new PlantPayoffRule(),
            new OrphanReferencesRule(),
            new BibleAgreementRule(),
            new InsertedBeatDriftRule(),
        ];

        var verdicts = await auditRunner.RunAsync(
            "LOGICSWEEP", $"node:{node.Slug}", FindingCategory.Contradiction, rules, ctx, ct: ct);

        return new LogicSweepReport(nodeId, node.Slug, node.Title, beats.Count, verdicts);
    }

    // ── Shared JSON-array parsing for all six dimensions ──────────────────────────

    /// <summary>Every dimension asks for the same finding shape — a JSON array of
    /// {beat_number, severity, evidence, fix} — so there is one parser instead of six.</summary>
    internal static IReadOnlyList<AuditVerdict> ParseFindingsArray(
        string ruleKey, string title, string raw, IReadOnlyList<AuditBeat> beats)
    {
        try
        {
            var start = raw.IndexOf('[');
            var end   = raw.LastIndexOf(']');
            if (start < 0 || end < start) return [];
            using var doc = JsonDocument.Parse(raw[start..(end + 1)]);
            var results = new List<AuditVerdict>();
            foreach (var f in doc.RootElement.EnumerateArray())
            {
                try
                {
                    // beat_number is documented to the LLM as "<int or null>" for whole-book
                    // findings (plant/payoff, bible agreement) — JsonElement.TryGetInt32 THROWS
                    // InvalidOperationException on a JSON null (it doesn't fail soft like the
                    // name implies), so this must check ValueKind first or one whole-book finding
                    // silently discards every other finding in the same response via the outer
                    // catch below.
                    var beatNumber = f.TryGetProperty("beat_number", out var bn)
                        && bn.ValueKind == JsonValueKind.Number
                        && bn.TryGetInt32(out var n) ? n : (int?)null;
                    var location = beatNumber.HasValue
                        ? beats.FirstOrDefault(b => b.Number == beatNumber.Value)?.Id.ToString()
                        : null;
                    var severity = f.TryGetProperty("severity", out var sv) ? sv.GetString()?.ToUpperInvariant() : null;
                    severity = severity is "BLOCKER" or "MODERATE" or "MINOR" or "DEVIATION" ? severity : "MODERATE";
                    var evidence = f.TryGetProperty("evidence", out var ev) ? ev.GetString() ?? "" : "";
                    if (evidence.Length == 0) continue; // don't persist an empty/malformed entry
                    var fix = f.TryGetProperty("fix", out var fx) ? fx.GetString() : null;
                    var evidenceWithBeat = beatNumber.HasValue ? $"Beat #{beatNumber}: {evidence}" : evidence;
                    results.Add(new AuditVerdict(ruleKey, title, severity, evidenceWithBeat, location, fix));
                }
                catch
                {
                    // One malformed entry must not discard every other finding in the same
                    // response — skip just this entry, not the whole array.
                }
            }
            return results;
        }
        catch { return []; }
    }

    // ── The six dimensions ────────────────────────────────────────────────────────

    sealed class CausalityChainRule : ILlmAuditRule
    {
        public string Key => "causality";
        public string Title => "Causality chain";
        public int MaxResponseTokens => 4096;

        public (string System, string User) BuildPrompt(AuditContext ctx) => (
            """
            You are auditing one dimension of a story: the CAUSALITY CHAIN.
            Every event must have an established cause; every decision, a motivation; every
            capability, an on-page origin. Find breaks: an effect with no shown cause, a
            decision the text gives no motivation for, a character doing something they were
            never shown able to do.

            Return ONLY a JSON array (no prose wrapper), one entry per real problem found:
            [{"beat_number": <int>, "severity": "BLOCKER"|"MODERATE"|"MINOR", "evidence": "cite what happens and why it has no established cause", "fix": "one concrete sentence or null"}]
            Return [] if the causality chain holds. Do not invent problems you cannot cite a
            specific beat for. When uncertain, err toward fewer findings.
            """,
            $"Beats:\n{ctx.Prose}");
        public IReadOnlyList<AuditVerdict> ParseResponse(string raw, AuditContext ctx) => ParseFindingsArray(Key, Title, raw, ctx.Beats);
    }

    sealed class KnowledgeStatesRule : ILlmAuditRule
    {
        public string Key => "knowledge_states";
        public string Title => "Knowledge states";
        public int MaxResponseTokens => 4096;

        public (string System, string User) BuildPrompt(AuditContext ctx) => (
            """
            You are auditing one dimension of a story: KNOWLEDGE STATES.
            Track who knows what, and when they learned it. Nobody may act on knowledge they
            have not yet been shown to possess — a character referencing a fact, name, or event
            before the text establishes they learned it is a violation.

            Return ONLY a JSON array (no prose wrapper), one entry per real problem found:
            [{"beat_number": <int>, "severity": "BLOCKER"|"MODERATE"|"MINOR", "evidence": "name who acts on knowledge they shouldn't have and cite what they say/do", "fix": "one concrete sentence or null"}]
            Return [] if knowledge states are consistent. Do not invent problems you cannot cite
            a specific beat for. When uncertain, err toward fewer findings.
            """,
            $"Beats:\n{ctx.Prose}");
        public IReadOnlyList<AuditVerdict> ParseResponse(string raw, AuditContext ctx) => ParseFindingsArray(Key, Title, raw, ctx.Beats);
    }

    sealed class TimelineRule : ILlmAuditRule
    {
        public string Key => "timeline";
        public string Title => "Timeline";
        public int MaxResponseTokens => 4096;

        public (string System, string User) BuildPrompt(AuditContext ctx) => (
            """
            You are auditing one dimension of a story: the TIMELINE.
            Reconstruct the book's internal clock from every date, duration, age, and
            "N days/months/years" claim in the text. Find impossibilities: a claimed year
            that's after or before the story's own established present, an age that
            contradicts a stated birth year or tenure, two elapsed-time claims that can't both
            be true, an event cited as happening before the character doing the citing could
            have known about it.

            Return ONLY a JSON array (no prose wrapper), one entry per real problem found:
            [{"beat_number": <int>, "severity": "BLOCKER"|"MODERATE"|"MINOR", "evidence": "quote the conflicting time claims and do the arithmetic", "fix": "one concrete sentence or null"}]
            Return [] if the timeline holds. Do not invent problems you cannot cite a specific
            beat for. When uncertain, err toward fewer findings.
            """,
            $"Beats:\n{ctx.Prose}");
        public IReadOnlyList<AuditVerdict> ParseResponse(string raw, AuditContext ctx) => ParseFindingsArray(Key, Title, raw, ctx.Beats);
    }

    sealed class PlantPayoffRule : ILlmAuditRule
    {
        public string Key => "plant_payoff";
        public string Title => "Plant/payoff ledger";
        public int MaxResponseTokens => 4096;

        public (string System, string User) BuildPrompt(AuditContext ctx)
        {
            var plants = ctx.Extra.TryGetValue("plants", out var p) ? (List<PlantPayoff>)p! : [];
            var registry = plants.Count > 0
                ? "\n\nRegistered plant/payoff pairs for this node:\n" + string.Join("\n", plants.Select(pl =>
                    $"  [{pl.Category}] PLANT: {pl.PlantDescription} | PAYOFF: {pl.PayoffDescription} | " +
                    $"plant beat set: {pl.PlantBeatId != null} | payoff beat set: {pl.PayoffBeatId != null}"))
                : "\n\n(No plants registered for this node.)";
            return (
                $$"""
                You are auditing one dimension of a story: the PLANT/PAYOFF LEDGER, checked
                TWO WAYS. Every plant (a detail seeded early) must pay off later; every payoff
                (a reveal, a callback, a "of course") must have been genuinely planted earlier,
                not asserted cold. Cross-reference the registered plant/payoff pairs below
                against the actual prose — a registered plant whose payoff beat is unset is a
                candidate orphan; a payoff whose plant beat is unset needs the prose checked for
                whether it was actually seeded on the page.{{registry}}

                Return ONLY a JSON array (no prose wrapper), one entry per real problem found:
                [{"beat_number": <int or null>, "severity": "BLOCKER"|"MODERATE"|"MINOR", "evidence": "name the plant or payoff and what's missing on which side", "fix": "one concrete sentence or null"}]
                Return [] if the ledger is clean both ways. Do not invent problems you cannot
                cite a specific beat or registered pair for. When uncertain, err toward fewer
                findings.
                """,
                $"Beats:\n{ctx.Prose}");
        }
        public IReadOnlyList<AuditVerdict> ParseResponse(string raw, AuditContext ctx) => ParseFindingsArray(Key, Title, raw, ctx.Beats);
    }

    sealed class OrphanReferencesRule : ILlmAuditRule
    {
        public string Key => "orphan_refs";
        public string Title => "Orphan references";
        public int MaxResponseTokens => 4096;

        public (string System, string User) BuildPrompt(AuditContext ctx)
        {
            var disabled = ctx.Extra.TryGetValue("disabledSnippets", out var d) ? (List<string>)d! : [];
            var disabledBlock = disabled.Count > 0
                ? "\n\nSnippets from CUT/DISABLED beats (no longer part of the book — flag any live beat that still depends on or references content that only appeared in one of these):\n"
                  + string.Join("\n---\n", disabled)
                : "\n\n(No disabled beats recorded for this node.)";
            return (
                $$"""
                You are auditing one dimension of a story: ORPHAN REFERENCES.
                Find anything in the live prose that references content which no longer exists
                in the book — a name, object, or event that was apparently cut, merged, or
                renamed elsewhere, leaving a dangling reference behind (e.g. a character
                mentioned once and never again in a way that reads like a plan changed
                mid-draft, not a deliberate open thread).{{disabledBlock}}

                Return ONLY a JSON array (no prose wrapper), one entry per real problem found:
                [{"beat_number": <int>, "severity": "BLOCKER"|"MODERATE"|"MINOR", "evidence": "cite the dangling reference and what it seems to be missing", "fix": "one concrete sentence or null"}]
                Return [] if there are no orphan references. Do not flag a deliberately
                unresolved mystery as an orphan reference — only flag what reads like leftover
                debris from a cut plan. When uncertain, err toward fewer findings.
                """,
                $"Beats:\n{ctx.Prose}");
        }
        public IReadOnlyList<AuditVerdict> ParseResponse(string raw, AuditContext ctx) => ParseFindingsArray(Key, Title, raw, ctx.Beats);
    }

    sealed class BibleAgreementRule : ILlmAuditRule
    {
        public string Key => "bible_agreement";
        public string Title => "Bible agreement";
        public int MaxResponseTokens => 4096;

        public (string System, string User) BuildPrompt(AuditContext ctx)
        {
            var bible = ctx.Extra.TryGetValue("bible", out var b) ? (string?)b : null;
            var bibleBlock = string.IsNullOrWhiteSpace(bible)
                ? "\n\n(No NodeBible recorded for this node.)"
                : $"\n\nNode bible (hand-authored facts, arc, locks):\n{Clamp(bible!, 30000)}";
            return (
                $$"""
                You are auditing one dimension of a story: BIBLE AGREEMENT.
                The prose and the node's hand-authored bible must tell the same story. Find
                contradictions — a locked fact the bible states that the prose contradicts, or
                prose that establishes something the bible doesn't know about and should.
                Per house rule: prose wins on facts (the bible is the one that's stale) UNLESS
                the bible is explicitly marking something as a locked constraint the prose must
                honor — say which side you think is stale in your evidence.{{bibleBlock}}

                Return ONLY a JSON array (no prose wrapper), one entry per real problem found:
                [{"beat_number": <int or null>, "severity": "BLOCKER"|"MODERATE"|"MINOR", "evidence": "quote the bible claim and the contradicting prose, and say which side is stale", "fix": "one concrete sentence or null"}]
                Return [] if bible and prose agree. Do not invent problems you cannot cite
                specific bible text and prose for. When uncertain, err toward fewer findings.
                """,
                $"Beats:\n{ctx.Prose}");
        }
        public IReadOnlyList<AuditVerdict> ParseResponse(string raw, AuditContext ctx) => ParseFindingsArray(Key, Title, raw, ctx.Beats);

        static string Clamp(string s, int max) => s.Length <= max ? s : s[..max] + "\n[...elided...]";
    }

    /// <summary>
    /// Seventh dimension (extends docs/LOGIC.md's original six): INSERTED-BEAT DRIFT.
    ///
    /// <b>Why this exists.</b> Beats written in one pass sit on an even <c>BeatNodes.SortKey</c>
    /// grid (50, 100, 150…). A beat added later is wedged between two existing ones at their
    /// midpoint, so repeated insertion leaves a binary-subdivision trail — 75, then 87.5, then
    /// 93.75, then 96.875. Those inserted beats are written against a neighbourhood that is
    /// already fixed, and nothing forces the author to re-read what the surrounding prose already
    /// asserted. That is precisely where state contradictions accumulate, and the other six
    /// dimensions miss them because each reads the book as a flat sequence with no memory of
    /// which beats arrived late.
    ///
    /// Empirically (TRNY, 2026-08-02) three real defects survived THREE full logic sweeps and all
    /// three sat on fractional keys: a character holding "three coins" at 93.75/96.875 after the
    /// coins were stolen on-page at 50 and while 100 said "empty pockets"; a beat at 96.875
    /// recalling a guard encounter "an hour ago" that does not occur until 100–200; and a scene
    /// that was chronologically first sorting last, contradicting three beats about where an
    /// object was stored.
    ///
    /// <b>Selection is deterministic, judgement is not.</b> Which beats arrived late is a fact
    /// about the sort keys and is computed here in code — no model is asked to guess it. Only the
    /// narrow question "does this late beat contradict its fixed neighbours" goes to the LLM, and
    /// only the late beats plus their bracketing neighbours are sent, not the whole book. When a
    /// node has no inserted beats the rule costs one trivial call and returns [].
    /// </summary>
    internal sealed class InsertedBeatDriftRule : ILlmAuditRule
    {
        public string Key => "inserted_beat_drift";
        public string Title => "Inserted-beat drift";
        public int MaxResponseTokens => 4096;

        /// <summary>
        /// A beat is "late-inserted" when its gap to BOTH neighbours is at most half the node's
        /// original grid spacing — i.e. it was squeezed in rather than laid down with the run.
        ///
        /// <b>The grid estimate is the median of the LARGER HALF of gaps, not the median of all
        /// of them.</b> Insertions contribute their own small gaps, so on a heavily-subdivided
        /// node they outnumber the original spacing and drag a plain median down below the very
        /// insertions being looked for — with TRNY Ch4's real keys (50, 75, 87.5, 93.75, 96.875,
        /// 100, 150, 200, 250) a plain median gives 25, which silently misses 75 and 87.5, the
        /// first two insertions. Taking the upper half recovers the true 50-step grid. It is also
        /// steadier than max-gap, which one unusually long jump would blow out.
        ///
        /// Comparison is <c>&lt;=</c> half the grid so a FIRST insertion — landing on the exact
        /// midpoint, gaps of exactly grid/2 on both sides — is caught; that is the single most
        /// common case.
        ///
        /// Deliberately NOT "SortKey has a fractional part": a node whose original grid is 0.5, or
        /// which has been wholly renumbered, would false-positive on every beat, and a node using
        /// integer midpoints (100, 150, 175) would false-negative. Relative spacing is the signal.
        /// Endpoints are never candidates — a first or last beat has only one neighbour.
        /// </summary>
        internal static IReadOnlyList<AuditBeat> FindInserted(IReadOnlyList<AuditBeat> beats)
        {
            if (beats.Count < 4) return [];
            var ordered = beats.OrderBy(b => b.SortKey).ToList();

            var gaps = new List<double>();
            for (var i = 1; i < ordered.Count; i++) gaps.Add(ordered[i].SortKey - ordered[i - 1].SortKey);
            var positive = gaps.Where(g => g > 0).OrderBy(g => g).ToList();
            if (positive.Count == 0) return [];

            var upperHalf = positive.Skip(positive.Count / 2).ToList();
            var grid = upperHalf[upperHalf.Count / 2];
            if (grid <= 0) return [];

            var threshold = grid / 2.0;
            var hits = new List<AuditBeat>();
            for (var i = 1; i < ordered.Count - 1; i++)
            {
                var before = ordered[i].SortKey - ordered[i - 1].SortKey;
                var after  = ordered[i + 1].SortKey - ordered[i].SortKey;
                if (before <= threshold && after <= threshold) hits.Add(ordered[i]);
            }
            return hits;
        }

        /// <summary>The fixed beats immediately before and after each inserted run — the state a
        /// late beat must not contradict.</summary>
        internal static IReadOnlyList<AuditBeat> FindAnchors(
            IReadOnlyList<AuditBeat> beats, IReadOnlyList<AuditBeat> inserted)
        {
            var insertedIds = inserted.Select(b => b.Id).ToHashSet();
            var ordered = beats.OrderBy(b => b.SortKey).ToList();
            var anchors = new List<AuditBeat>();
            for (var i = 0; i < ordered.Count; i++)
            {
                if (insertedIds.Contains(ordered[i].Id)) continue;
                var touchesRun = (i > 0 && insertedIds.Contains(ordered[i - 1].Id))
                              || (i < ordered.Count - 1 && insertedIds.Contains(ordered[i + 1].Id));
                if (touchesRun) anchors.Add(ordered[i]);
            }
            return anchors;
        }

        public (string System, string User) BuildPrompt(AuditContext ctx)
        {
            var inserted = FindInserted(ctx.Beats);
            if (inserted.Count == 0)
                return ("Reply with exactly: []", "No inserted beats to check. Reply []");

            var anchors = FindAnchors(ctx.Beats, inserted);
            var insertedIds = inserted.Select(b => b.Id).ToHashSet();
            var window = inserted.Concat(anchors).OrderBy(b => b.SortKey)
                .Select(b => $"[Beat #{b.Number}] ({(insertedIds.Contains(b.Id) ? "INSERTED LATER" : "pre-existing anchor")})\n{b.Text}");

            return (
                """
                You are auditing one dimension of a story: INSERTED-BEAT DRIFT.

                Some beats below are marked INSERTED LATER — they were added between beats that
                already existed. The others are pre-existing anchors. An inserted beat must not
                contradict the anchors around it.

                Check ONLY these, and only against the beats shown:
                1. STATE — money and possessions carried, injuries, what a character is holding or
                   wearing, where an object is stored, who is present, time of day.
                2. RETROSPECTIVE REFERENCES POINTING FORWARD — an inserted beat recalling an event
                   ("an hour ago…", "he remembered…") that only happens in a LATER beat.
                3. ORDERING — an inserted beat that can only be true before an anchor it is placed
                   after (or vice versa).

                Return ONLY a JSON array (no prose wrapper), one entry per real contradiction:
                [{"beat_number": <int>, "severity": "BLOCKER"|"MODERATE"|"MINOR", "evidence": "quote the inserted beat AND the anchor it contradicts", "fix": "one concrete sentence or null"}]
                Cite the specific inserted beat and the specific anchor text it conflicts with.
                Return [] if the inserted beats are consistent with their anchors. Do not report a
                difference that is merely later in time and consistent. When uncertain, return [].
                """,
                $"Beats:\n{string.Join("\n\n", window)}");
        }

        public IReadOnlyList<AuditVerdict> ParseResponse(string raw, AuditContext ctx) => ParseFindingsArray(Key, Title, raw, ctx.Beats);
    }
}

public record LogicSweepReport(
    Guid NodeId, string NodeSlug, string NodeTitle, int BeatCount, IReadOnlyList<AuditVerdict> Findings)
{
    public int BlockerCount   => Findings.Count(f => f.Severity == "BLOCKER");
    public int ModerateCount  => Findings.Count(f => f.Severity == "MODERATE");
    public int MinorCount     => Findings.Count(f => f.Severity == "MINOR");
    public bool Clean         => Findings.Count == 0;
}
