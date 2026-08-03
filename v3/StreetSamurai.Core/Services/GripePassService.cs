using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

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
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
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

    public sealed record Gripe(
        int BeatNumber, Guid BeatId, string Quote, string Complaint,
        int Voters, string Providers,
        string Severity /* blocker|moderate|minor */, bool Confirmed, string ArbiterRationale);

    public sealed record GripeRunResult(
        Guid NodeId, string Slug, string Title, int Readers, string ReaderSeats,
        IReadOnlyList<Gripe> Confirmed, IReadOnlyList<Gripe> Rejected,
        int RawComplaints, int QuoteGroundingKills, int FindingsFiled);

    public async Task<GripeRunResult> RunAsync(Guid nodeId, int readers = 4, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var slug = node.Slug ?? nodeId.ToString("N");

        // Ordered enabled beats — the beat-number → BeatId map for anchoring gripes.
        var chapterIds = await db.Nodes.AsNoTracking()
            .Where(n => n.ParentNodeId == nodeId && n is Data.Entities.ChapterNode)
            .OrderBy(n => n.SortKey).Select(n => n.Id).ToListAsync(ct);
        var sourceIds = chapterIds.Count > 0 ? chapterIds : new List<Guid> { nodeId };
        var beatRows = await db.BeatNodes.AsNoTracking()
            .Where(bn => sourceIds.Contains(bn.NodeId) && bn.IsEnabled && bn.Beat != null)
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
        var grounded = new List<(ReviewLlmTransport.JurySeat Seat, RawComplaint C, Guid BeatId, string BeatText)>();
        foreach (var (seat, c) in raw)
        {
            var idx = c.BeatNumber - 1;
            if (idx < 0 || idx >= ordered.Count) { groundingKills++; continue; }
            var beat = ordered[idx];
            var normalizedQuote = Normalize(c.Quote);
            if (normalizedQuote.Length >= 12 && !Normalize(beat.Text).Contains(normalizedQuote, StringComparison.OrdinalIgnoreCase))
            {
                // Quote not in the cited beat — try the whole manuscript before killing
                // (readers sometimes cite an off-by-one beat number).
                if (!Normalize(export.Markdown).Contains(normalizedQuote, StringComparison.OrdinalIgnoreCase))
                { groundingKills++; continue; }
            }
            grounded.Add((seat, c, beat.Id, beat.Text));
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

    // ── helpers ────────────────────────────────────────────────────────────────────

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
