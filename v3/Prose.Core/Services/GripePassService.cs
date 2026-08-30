using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Reader-Proxy QA Instrument 4 — the findings-only gripe jury (docs/READER-QA.md).
///
/// <para>Replaces the persona score panel as the "what do readers dislike" instrument.
/// A small jury (default 4) of full-book readers — one per live model FAMILY, because
/// same-model voters make correlated errors — emits NO scores, only page-anchored
/// complaints: beat number + verbatim quote + what's wrong. Complaints are deduped,
/// quote-grounded deterministically (a complaint whose quote does not appear in the
/// manuscript is discarded before any arbiter spend), then a Sonnet arbiter confirms
/// each against the actual beat text and triages BLOCKER/MODERATE/MINOR. Confirmed
/// gripes land as <see cref="FindingCategory.ReaderGripe"/> findings.</para>
///
/// <para>This closes the two dead seams of the legacy pipeline: ConsolidateGripesAsync
/// printed grouped gripes to console and threw them away; ProposeEditsAsync wrote
/// proposals to a temp JSON no apply arm ever read. Gripes now persist, supersede on
/// re-run, and can be applied through the duel gate
/// (<see cref="ProposeAndDuelFixAsync"/> — SS-A44: the duel is a vote, apply passes
/// allowVotes under the explicit user action).</para>
///
/// <para>The report-only pass emits no scores and is not vote-gated.</para>
/// </summary>
public sealed class GripePassService(
    IDbContextFactory<ProseDbContext> dbFactory,
    NodeMarkdownExporter exporter,
    ReviewLlmTransport transport,
    ILlmService llm,
    FindingsService findings,
    SettingsService settings,
    BeatDuelService duels,
    NodeWorkbenchService workbench,
    ILogger<GripePassService> log)
{
    private const string FindingSummaryPrefix = "GRIPE";
    private const string EngagementFindingSummaryPrefix = "ENGAGEMENT";

    // A dip that recovers within this many beats reads as a brief stumble (minor); beyond it, the
    // book stayed flat long enough to cost real reader trust (moderate). Never recovering at all
    // (RecoveredAtBeat null) is categorically worse than either — a dead stretch the book never
    // pulls out of — so that case is blocker regardless of how far from the end it starts.
    private const int MinorRecoveryWindowBeats = 3;

    public sealed record Gripe(
        int BeatNumber, Guid BeatId, string Quote, string Complaint,
        int Voters, string Providers,
        string Severity /* blocker|moderate|minor */, bool Confirmed, string ArbiterRationale);

    public sealed record GripeRunResult(
        Guid NodeId, string Slug, string Title, int Readers, string ReaderSeats,
        IReadOnlyList<Gripe> Confirmed, IReadOnlyList<Gripe> Rejected,
        int RawComplaints, int QuoteGroundingKills, int FindingsFiled);

    /// <summary>One reader's account of where the manuscript lost them and whether it ever got
    /// them back — the felt-pass unit of measure (docs/LOGIC.md §10), as opposed to
    /// <see cref="Gripe"/>'s itemized craft complaint.</summary>
    public sealed record EngagementSpan(
        int StartBeat, Guid BeatId, string Quote, string Note, int? RecoveredAtBeat,
        int Voters, string Providers,
        string Severity /* blocker|moderate|minor */, bool Confirmed, string ArbiterRationale);

    public sealed record EngagementRunResult(
        Guid NodeId, string Slug, string Title, int Readers, string ReaderSeats,
        IReadOnlyList<EngagementSpan> Confirmed, IReadOnlyList<EngagementSpan> Rejected,
        int RawSpans, int QuoteGroundingKills, int FindingsFiled);

    // docs/LOGIC.md §10, practice #3: "weight-by-length, not weight-by-adjective." Static and
    // generic by design — RunFullOrderReadAsync has no plant/payoff or page-distance data in
    // scope at this point, and computing it per-finding would be a second query per finding for
    // marginal specificity over what the finding's own beat number + note already tell a fixer.
    private const string WeightByLengthFix =
        "Fix structurally, not stylistically: give this beat more page-time to accrue pressure " +
        "before it lands, or cut the correct-but-inert scene immediately in front of it. Do NOT " +
        "rewrite this beat's prose to sound more intense at the same length — that is the wrong " +
        "fix (docs/LOGIC.md §10, weight-by-length not weight-by-adjective).";

    public async Task<GripeRunResult> RunAsync(Guid nodeId, int readers = 4, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        // IgnoreQueryFilters(): explicit nodeId, not an ambient scope (same bug class found and
        // fixed in BookArchiveService.ArchiveAsync, 2026-08-17).
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var slug = node.Slug ?? nodeId.ToString("N");

        // Ordered enabled beats — the beat-number → BeatId map for anchoring gripes.
        // Recurses past any nested Collection; returns leaves in reading order, which
        // chapterOrder below relies on (2026-08-09 fix).
        var sourceIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var beatRows = await db.BeatNodes.AsNoTracking()
            .Where(bn => sourceIds.Contains(bn.NodeId) && true && bn.Beat != null)
            .Select(bn => new { bn.NodeId, bn.SortKey, bn.Beat!.Id, bn.Beat.Text })
            .ToListAsync(ct);
        var chapterOrder = sourceIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        var ordered = beatRows.OrderBy(b => chapterOrder[b.NodeId]).ThenBy(b => b.SortKey).ToList();
        if (ordered.Count == 0)
            return new GripeRunResult(nodeId, slug, node.Title, 0, "", Array.Empty<Gripe>(), Array.Empty<Gripe>(), 0, 0, 0);

        var export = await exporter.ExportAsync(nodeId, numberBeats: true, ct: ct);

        // ── 1. the jury: N full-read complaint passes, one per live family ────────
        var seats = await transport.AssignJuryAsync(readers, ct);
        if (seats.Count == 0)
            throw new InvalidOperationException("No live jury providers — cannot run a gripe pass. Check API keys.");

        var perReader = await Task.WhenAll(seats.Select(async (seat, i) =>
        {
            try { return (seat, complaints: await ReadOnceAsync(seat, export.Markdown, node.Title, ct)); }
            catch (Exception ex)
            {
                log.LogWarning(ex, "[gripe] reader {Provider}/{Model} failed — continuing without it.", seat.Provider, seat.Model);
                return (seat, complaints: new List<RawComplaint>());
            }
        }));

        var raw = perReader.SelectMany(r => r.complaints.Select(c => (r.seat, c))).ToList();

        // ── 2. deterministic quote-grounding: hallucinated quotes die free ────────
        int groundingKills = 0;
        var groundingBeats = ordered.Select(b => (b.Id, BeatMarkup.StripEntityTags(b.Text))).ToList();
        var grounded = new List<(ReviewLlmTransport.JurySeat Seat, RawComplaint C, Guid BeatId, string BeatText)>();
        foreach (var (seat, c) in raw)
        {
            var result = GroundQuote(groundingBeats, c.BeatNumber, c.Quote);
            if (result == null) { groundingKills++; continue; }
            var complaint = result.Value.CorrectedBeatNumber == c.BeatNumber ? c : c with { BeatNumber = result.Value.CorrectedBeatNumber };
            grounded.Add((seat, complaint, result.Value.BeatId, result.Value.BeatText));
        }

        // ── 3. dedupe: same beat + overlapping complaint tokens = one gripe ───────
        var deduped = new List<(RawComplaint C, Guid BeatId, string BeatText, List<string> Providers)>();
        foreach (var g in grounded)
        {
            var existing = deduped.FirstOrDefault(d =>
                d.C.BeatNumber == g.C.BeatNumber && TokenJaccard(d.C.Complaint, g.C.Complaint) > 0.4);
            if (existing.C != null) existing.Providers.Add(g.Seat.Provider);
            else deduped.Add((g.C, g.BeatId, g.BeatText, new List<string> { g.Seat.Provider }));
        }

        // ── 4. arbiter: confirm each unique gripe against the actual beat text ────
        var confirmed = new List<Gripe>();
        var rejected = new List<Gripe>();
        foreach (var d in deduped)
        {
            ct.ThrowIfCancellationRequested();
            var (isConfirmed, severity, rationale) = await ArbitrateAsync(d.C, d.BeatText, ct);
            var gripe = new Gripe(d.C.BeatNumber, d.BeatId, d.C.Quote, d.C.Complaint,
                d.Providers.Count, string.Join(",", d.Providers.Distinct()), severity, isConfirmed, rationale);
            (isConfirmed ? confirmed : rejected).Add(gripe);
        }

        // ── 5. findings: delete-then-recreate, ReaderGripe ────────────────────────
        var filePathPrefix = $"node:{slug}";
        findings.DeleteBySummaryPrefix(filePathPrefix, FindingSummaryPrefix);
        foreach (var g in confirmed)
        {
            findings.Upsert(
                $"{filePathPrefix}/beat:{g.BeatId:N}", chapterId: null, FindingCategory.ReaderGripe,
                g.Severity switch { "blocker" => FindingSeverity.High, "moderate" => FindingSeverity.Medium, _ => FindingSeverity.Low },
                $"{FindingSummaryPrefix} beat #{g.BeatNumber} ({g.Voters} voter(s)): {g.Complaint}",
                snippet: g.Quote,
                suggestedFix: null);
        }

        log.LogInformation("[gripe] {Slug}: {Raw} raw → {Grounded} grounded → {Unique} unique → {Confirmed} confirmed ({Kills} quote-grounding kills).",
            slug, raw.Count, grounded.Count, deduped.Count, confirmed.Count, groundingKills);

        return new GripeRunResult(nodeId, slug, node.Title, seats.Count,
            string.Join(" · ", seats.Select(s => $"{s.Provider}:{s.Model}")),
            confirmed, rejected, raw.Count, groundingKills, confirmed.Count);
    }

    /// <summary>
    /// The Full-Order Read (docs/LOGIC.md §10) — an automated proxy for the felt-pass ritual's
    /// one sacred instrument: reading straight through at reader speed and marking only where
    /// engagement died. This is deliberately NOT the same instrument as <see cref="RunAsync"/>
    /// (the gripe jury) — that lists concrete craft complaints; this asks each juror to narrate
    /// a continuous read and report only where their own attention drifted and whether it came
    /// back. An LLM doesn't get bored the way a human reader does, but can be prompted to notice
    /// textual flatness — this makes running an approximation of the ritual unattended and cheap;
    /// it does not replace an author's own full-order read (docs/READER-QA.md §2, instrument 5).
    /// Shares every mechanical stage with <see cref="RunAsync"/> (export, jury, quote-grounding,
    /// dedup, findings persistence) under its own finding scope so neither instrument's re-run
    /// ever clears the other's findings.
    /// </summary>
    public async Task<EngagementRunResult> RunFullOrderReadAsync(Guid nodeId, int readers = 4, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var slug = node.Slug ?? nodeId.ToString("N");

        var sourceIds = await NodeWorkbenchService.GetLeafDescendantIdsAsync(db, nodeId, ct);
        var beatRows = await db.BeatNodes.AsNoTracking()
            .Where(bn => sourceIds.Contains(bn.NodeId) && true && bn.Beat != null)
            .Select(bn => new { bn.NodeId, bn.SortKey, bn.Beat!.Id, bn.Beat.Text })
            .ToListAsync(ct);
        var chapterOrder = sourceIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        var ordered = beatRows.OrderBy(b => chapterOrder[b.NodeId]).ThenBy(b => b.SortKey).ToList();
        if (ordered.Count == 0)
            return new EngagementRunResult(nodeId, slug, node.Title, 0, "", Array.Empty<EngagementSpan>(), Array.Empty<EngagementSpan>(), 0, 0, 0);

        var export = await exporter.ExportAsync(nodeId, numberBeats: true, ct: ct);

        var seats = await transport.AssignJuryAsync(readers, ct);
        if (seats.Count == 0)
            throw new InvalidOperationException("No live jury providers — cannot run a full-order read. Check API keys.");

        var perReader = await Task.WhenAll(seats.Select(async (seat, i) =>
        {
            try { return (seat, spans: await ReadForEngagementAsync(seat, export.Markdown, node.Title, ct)); }
            catch (Exception ex)
            {
                log.LogWarning(ex, "[full-order-read] reader {Provider}/{Model} failed — continuing without it.", seat.Provider, seat.Model);
                return (seat, spans: new List<RawEngagementSpan>());
            }
        }));

        var raw = perReader.SelectMany(r => r.spans.Select(s => (r.seat, s))).ToList();

        int groundingKills = 0;
        var groundingBeats = ordered.Select(b => (b.Id, BeatMarkup.StripEntityTags(b.Text))).ToList();
        var grounded = new List<(ReviewLlmTransport.JurySeat Seat, RawEngagementSpan S, Guid BeatId, string BeatText)>();
        foreach (var (seat, s) in raw)
        {
            var result = GroundQuote(groundingBeats, s.StartBeat, s.Quote);
            if (result == null) { groundingKills++; continue; }
            var span = result.Value.CorrectedBeatNumber == s.StartBeat ? s : s with { StartBeat = result.Value.CorrectedBeatNumber };
            grounded.Add((seat, span, result.Value.BeatId, result.Value.BeatText));
        }

        var deduped = new List<(RawEngagementSpan S, Guid BeatId, string BeatText, List<string> Providers)>();
        foreach (var g in grounded)
        {
            var existing = deduped.FirstOrDefault(d =>
                d.S.StartBeat == g.S.StartBeat && TokenJaccard(d.S.Note, g.S.Note) > 0.4);
            if (existing.S != null) existing.Providers.Add(g.Seat.Provider);
            else deduped.Add((g.S, g.BeatId, g.BeatText, new List<string> { g.Seat.Provider }));
        }

        var confirmed = new List<EngagementSpan>();
        var rejected = new List<EngagementSpan>();
        foreach (var d in deduped)
        {
            ct.ThrowIfCancellationRequested();
            var (isConfirmed, rationale) = await ArbitrateEngagementAsync(d.S, d.BeatText, ct);
            var severity = DeriveEngagementSeverity(d.S.StartBeat, d.S.RecoveredAtBeat);
            var span = new EngagementSpan(d.S.StartBeat, d.BeatId, d.S.Quote, d.S.Note, d.S.RecoveredAtBeat,
                d.Providers.Count, string.Join(",", d.Providers.Distinct()), severity, isConfirmed, rationale);
            (isConfirmed ? confirmed : rejected).Add(span);
        }

        // Own scope (`#fullorderread`), never the gripe jury's `node:{slug}` — a re-run of either
        // instrument must never clear the other's findings.
        var filePathPrefix = $"node:{slug}#fullorderread";
        findings.DeleteBySummaryPrefix(filePathPrefix, EngagementFindingSummaryPrefix);
        foreach (var s in confirmed)
        {
            var recovery = s.RecoveredAtBeat is int r ? $"recovered at B{r}" : "never recovered";
            findings.Upsert(
                $"{filePathPrefix}/beat:{s.BeatId:N}", chapterId: null, FindingCategory.ReaderGripe,
                s.Severity switch { "blocker" => FindingSeverity.High, "moderate" => FindingSeverity.Medium, _ => FindingSeverity.Low },
                $"{EngagementFindingSummaryPrefix} beat #{s.StartBeat} ({s.Voters} voter(s), {recovery}): {s.Note}",
                snippet: s.Quote,
                suggestedFix: WeightByLengthFix);
        }

        log.LogInformation("[full-order-read] {Slug}: {Raw} raw → {Grounded} grounded → {Unique} unique → {Confirmed} confirmed ({Kills} quote-grounding kills).",
            slug, raw.Count, grounded.Count, deduped.Count, confirmed.Count, groundingKills);

        return new EngagementRunResult(nodeId, slug, node.Title, seats.Count,
            string.Join(" · ", seats.Select(s => $"{s.Provider}:{s.Model}")),
            confirmed, rejected, raw.Count, groundingKills, confirmed.Count);
    }

    /// <summary>Apply arm: generate a minimal splice for a confirmed gripe, put it
    /// through the duel gate (SS-A44 — duels are votes; <paramref name="allowVotes"/>
    /// must carry an explicit user instruction), and only on REPLACE write the beat.
    /// On KEEP the gripe's finding stays open with the dissent attached — revision
    /// fuel, not a silent dismissal.</summary>
    public async Task<(bool Applied, DuelResult Duel, string CandidateText)> ProposeAndDuelFixAsync(
        Guid beatId, string complaint, string quote, bool allowVotes, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beat = await db.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct)
            ?? throw new InvalidOperationException($"Beat {beatId} not found.");

        const string system = """
            You revise ONE beat of a finished novel to address ONE specific reader complaint.
            Minimal splice: change as little as possible — keep voice, length, events, and
            every fact identical except what the complaint names. Never add scenes, never
            summarize, never explain. Return ONLY the full revised beat text, no commentary.
            """;
        var user = $"READER COMPLAINT: {complaint}\nOFFENDING PASSAGE: {quote}\n\nBEAT TEXT:\n{beat.Text}";
        var candidate = (await llm.GenerateAsync(system, user, temperature: 0.4,
            maxTokens: Math.Max(1200, beat.Text.Length / 2), model: settings.ComprehensionArbiterModel, ct: ct)).Trim();
        if (candidate.Length < beat.Text.Length / 3)
            throw new InvalidOperationException("Candidate splice came back suspiciously short — not dueling it.");

        var duel = await duels.DuelAsync(beat.Text, candidate,
            new DuelContext(StoryTitle: "", Goal: $"Address reader complaint: {complaint}", BeatId: beatId),
            allowVotes: allowVotes, ct: ct);

        if (duel.Replace)
        {
            await workbench.UpdateBeatTextAsync(beatId, candidate, ct: ct);
            log.LogInformation("[gripe] beat {BeatId} spliced (duel {B}/{W}/{S}).",
                beatId, duel.BetterVotes, duel.WorseVotes, duel.SameVotes);
        }
        return (duel.Replace, duel, candidate);
    }

    // ── one reader's full pass ─────────────────────────────────────────────────────

    private sealed record RawComplaint(int BeatNumber, string Quote, string Complaint);

    private async Task<List<RawComplaint>> ReadOnceAsync(
        ReviewLlmTransport.JurySeat seat, string manuscript, string title, CancellationToken ct)
    {
        const string system = """
            You are reading a complete novel manuscript. Beats are numbered [Beat N].
            Report ONLY concrete complaints — moments where the text failed you as a reader:
            confusing, flat, unearned, repetitive, tonally broken, or unintentionally funny.
            NO scores, NO ratings, NO praise, NO general impressions. Each complaint must be
            anchored: the beat number and a short VERBATIM quote of the offending text.
            Quality over quantity — report only what genuinely bothered you (typically 2-8
            complaints for a whole book; zero is a legitimate answer).
            Return STRICT JSON only, no markdown fence:
            {"complaints":[{"beatNumber":N,"quote":"verbatim text from that beat","complaint":"what is wrong, specifically"}]}
            """;
        var raw = await transport.CallSeatAsync(seat, system, $"NOVEL: {title}\n\n{manuscript}",
            maxTokens: 2500, temperature: 0.4, ct);
        raw = StripFence(raw);
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var list = new List<RawComplaint>();
            if (doc.RootElement.TryGetProperty("complaints", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var c in arr.EnumerateArray())
                    list.Add(new RawComplaint(
                        c.TryGetProperty("beatNumber", out var n) && n.ValueKind == JsonValueKind.Number ? n.GetInt32() : -1,
                        c.TryGetProperty("quote", out var q) ? q.GetString() ?? "" : "",
                        c.TryGetProperty("complaint", out var m) ? m.GetString() ?? "" : ""));
            return list.Where(c => !string.IsNullOrWhiteSpace(c.Complaint)).ToList();
        }
        catch (JsonException)
        {
            log.LogWarning("[gripe] reader {Provider} returned non-JSON — 0 complaints taken.", seat.Provider);
            return new List<RawComplaint>();
        }
    }

    private async Task<(bool Confirmed, string Severity, string Rationale)> ArbitrateAsync(
        RawComplaint c, string beatText, CancellationToken ct)
    {
        const string system = """
            You arbitrate one reader complaint against the actual text of the beat it cites.
            Confirm it ONLY if the text genuinely exhibits the problem — a defect a careful
            editor would also flag. Reject vague taste ("I'd prefer more action"), complaints
            the text already answers, and misreadings.
            severity: blocker = breaks comprehension or continuity; moderate = a real craft
            defect worth a splice; minor = texture-level.
            Return STRICT JSON only: {"confirmed":true,"severity":"blocker|moderate|minor","rationale":"one sentence"}
            """;
        var raw = await llm.GenerateAsync(system,
            $"COMPLAINT: {c.Complaint}\nCITED QUOTE: {c.Quote}\n\nBEAT TEXT:\n{beatText}",
            temperature: 0.1, maxTokens: 250, model: settings.ComprehensionArbiterModel, ct: ct);
        raw = StripFence(raw);
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            return (
                root.TryGetProperty("confirmed", out var cf) && cf.ValueKind == JsonValueKind.True,
                root.TryGetProperty("severity", out var s) ? s.GetString() ?? "minor" : "minor",
                root.TryGetProperty("rationale", out var r) ? r.GetString() ?? "" : "");
        }
        catch (JsonException) { return (false, "minor", "(arbiter returned non-JSON — rejected by default)"); }
    }

    // ── one reader's full-order read (engagement, not complaints) ─────────────────

    private sealed record RawEngagementSpan(int StartBeat, int? RecoveredAtBeat, string Quote, string Note);

    private async Task<List<RawEngagementSpan>> ReadForEngagementAsync(
        ReviewLlmTransport.JurySeat seat, string manuscript, string title, CancellationToken ct)
    {
        const string system = """
            You are reading a complete novel manuscript, straight through, the way a stranger
            would — not auditing it for facts, auditing your own engagement. Beats are numbered
            [Beat N].

            Report ONLY the moment(s) where you noticed your own attention drift or your interest
            die — where you would have started skimming, or put the book down, if this weren't
            your job. For each such span, name the beat where it started, and whether your
            interest ever came back before the book ended (name the beat where it recovered, or
            say it never did).

            Do NOT report: a slow start that pays off later, a deliberately quiet chapter that
            earns its quiet, or a scene you simply didn't enjoy stylistically. Report only genuine
            flatness — a stretch where nothing was actually at stake, or the prose itself went inert.

            Quality over quantity — most books have zero or one such span; several is unusual and
            should make you suspect you're pattern-matching rather than genuinely losing interest.

            Return STRICT JSON only, no markdown fence:
            {"spans":[{"startBeat":N,"recoveredAtBeat":N,"quote":"verbatim text from the beat where it started","note":"what specifically lost you"}]}
            Use recoveredAtBeat:null if interest never came back before the book ended.
            """;
        var raw = await transport.CallSeatAsync(seat, system, $"NOVEL: {title}\n\n{manuscript}",
            maxTokens: 2500, temperature: 0.4, ct);
        raw = StripFence(raw);
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var list = new List<RawEngagementSpan>();
            if (doc.RootElement.TryGetProperty("spans", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var s in arr.EnumerateArray())
                    list.Add(new RawEngagementSpan(
                        s.TryGetProperty("startBeat", out var n) && n.ValueKind == JsonValueKind.Number ? n.GetInt32() : -1,
                        s.TryGetProperty("recoveredAtBeat", out var rb) && rb.ValueKind == JsonValueKind.Number ? rb.GetInt32() : null,
                        s.TryGetProperty("quote", out var q) ? q.GetString() ?? "" : "",
                        s.TryGetProperty("note", out var m) ? m.GetString() ?? "" : ""));
            return list.Where(s => !string.IsNullOrWhiteSpace(s.Note)).ToList();
        }
        catch (JsonException)
        {
            log.LogWarning("[full-order-read] reader {Provider} returned non-JSON — 0 spans taken.", seat.Provider);
            return new List<RawEngagementSpan>();
        }
    }

    private async Task<(bool Confirmed, string Rationale)> ArbitrateEngagementAsync(
        RawEngagementSpan s, string beatText, CancellationToken ct)
    {
        const string system = """
            You arbitrate one reader's claim that their engagement died at a specific beat,
            against the actual text there.
            Confirm it ONLY if the text genuinely goes flat or inert at that point — nothing at
            stake, no forward pull, or prose that stalls. Reject a stylistic preference, and
            reject a deliberate slow-burn the text itself is clearly building toward something
            with (tension accruing even if the surface action is quiet).
            Return STRICT JSON only: {"confirmed":true,"rationale":"one sentence"}
            """;
        var raw = await llm.GenerateAsync(system,
            $"WHERE ENGAGEMENT DIED: {s.Note}\nCITED QUOTE: {s.Quote}\n\nBEAT TEXT:\n{beatText}",
            temperature: 0.1, maxTokens: 200, model: settings.ComprehensionArbiterModel, ct: ct);
        raw = StripFence(raw);
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            return (
                root.TryGetProperty("confirmed", out var cf) && cf.ValueKind == JsonValueKind.True,
                root.TryGetProperty("rationale", out var r) ? r.GetString() ?? "" : "");
        }
        catch (JsonException) { return (false, "(arbiter returned non-JSON — rejected by default)"); }
    }

    // ── helpers ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Severity for one confirmed engagement span, from the recovery signal alone (the arbiter
    /// only confirms genuineness — it never sets severity, unlike <see cref="ArbitrateAsync"/>'s
    /// gripe-jury counterpart). Never recovering before the book ends is categorically worse than
    /// any recovered dip, however long: a book that never pulls itself back is the only case
    /// that can't be waved off as "the middle sagged a little."
    /// </summary>
    internal static string DeriveEngagementSeverity(int startBeat, int? recoveredAtBeat) =>
        recoveredAtBeat is int r
            ? (r - startBeat <= MinorRecoveryWindowBeats ? "minor" : "moderate")
            : "blocker";

    /// <summary>
    /// Grounds a reader-cited quote against the manuscript. Returns null (grounding kill) when
    /// the cited beat number is out of range, or the quote appears nowhere in the manuscript at
    /// all. Previously, a quote not found in the CITED beat but found elsewhere in the
    /// manuscript was accepted while still using the wrong cited beat's id/text/number — so a
    /// complaint correctly grounded at beat 46 but cited as beat 45 was filed against the wrong
    /// beat, AND deduped only against the (wrong) cited number, letting the same real defect
    /// through twice under two different reader citations. This corrects both the beat
    /// reference and the reported beat number to the beat where the quote actually appears.
    /// </summary>
    internal static (Guid BeatId, string BeatText, int CorrectedBeatNumber)? GroundQuote(
        List<(Guid Id, string Text)> orderedBeats, int citedBeatNumber, string quote)
    {
        var idx = citedBeatNumber - 1;
        if (idx < 0 || idx >= orderedBeats.Count) return null;
        var beat = orderedBeats[idx];
        var normalizedQuote = Normalize(quote);
        if (normalizedQuote.Length >= 12 && !Normalize(beat.Text).Contains(normalizedQuote, StringComparison.OrdinalIgnoreCase))
        {
            var actualIdx = orderedBeats.FindIndex(b => Normalize(b.Text).Contains(normalizedQuote, StringComparison.OrdinalIgnoreCase));
            if (actualIdx < 0) return null;
            return (orderedBeats[actualIdx].Id, orderedBeats[actualIdx].Text, actualIdx + 1);
        }
        return (beat.Id, beat.Text, citedBeatNumber);
    }

    private static string Normalize(string s) => Regex.Replace(s, @"\s+", " ").Trim();

    private static double TokenJaccard(string a, string b)
    {
        var ta = Tokens(a); var tb = Tokens(b);
        if (ta.Count == 0 || tb.Count == 0) return 0;
        var inter = ta.Intersect(tb, StringComparer.OrdinalIgnoreCase).Count();
        return (double)inter / (ta.Count + tb.Count - inter);
    }

    private static HashSet<string> Tokens(string s) =>
        Regex.Split(s.ToLowerInvariant(), @"[^\p{L}\p{N}]+").Where(t => t.Length >= 4).ToHashSet();

    private static string StripFence(string raw)
    {
        raw = raw.Trim();
        if (raw.StartsWith("```"))
            raw = Regex.Replace(Regex.Replace(raw, @"^```(json)?\s*", ""), @"\s*```$", "");
        return raw;
    }
}
