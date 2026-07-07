using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StreetSamurai.Core.Data;
using StreetSamurai.Core.Data.Entities;
using StreetSamurai.Core.Interfaces;

namespace StreetSamurai.Core.Services;

// ─────────────────────────────────────────────────────────────────────────────
// BeatDuelService — escalating blind A/B adjudication for beat rewrites.
//
// The gate that lets a fix pass replace prose in a finished story without
// regressing it. Round 1 is a 3-voter panel with DIVERSE LENSES (register
// fidelity / structural goal / cold-reader experience — diversity catches what
// redundancy can't), each voter seeing the two versions blind in randomized
// order. Ballot is three-way: better / worse / same.
//
// Decision rule (conservative — never degrade a finished story on a coin flip):
//   REPLACE   ≥2 "better" and 0 "worse"
//   KEEP      ≥2 "worse" or ≥2 "same"
//   ESCALATE  any split with dissent (2-1 with a worse vote, or 1-1-1)
//
// Escalation is a 7-voter panel (4 added lenses) with REQUIRED written
// rationale; ≥5/7 "better" replaces, anything else keeps — and the dissenting
// rationales are returned as revision fuel so a contested rewrite gets revised
// with feedback rather than force-decided.
//
// Verdicts are cached by the SHA-256 pair of both texts (Beats.TextHash
// scheme): identical comparisons are free, and any text change re-keys.
//
// SS-A44 (voting law): duels are votes. AllowVotes must be passed true by a
// caller holding an explicit user instruction; it defaults to refusing.
// ─────────────────────────────────────────────────────────────────────────────

public class BeatDuelService(
    ILlmService llm,
    IDbContextFactory<StreetSamuraiDbContext> dbFactory,
    ILogger<BeatDuelService> log)
{
    // ── Lenses ────────────────────────────────────────────────────────────────

    static readonly (string Key, string Brief)[] Round1Lenses =
    [
        ("register",
         "REGISTER FIDELITY: which version better preserves the story's established narrative voice and the POV character's documented register? Penalize any version that drifts generic, explains its own emotions, or breaks the story's tonal discipline."),
        ("structural",
         "STRUCTURAL GOAL: judge only against the stated goal of the revision. Which version better achieves it — without creating a new structural problem (flat stakes, repeated event type, narrated moral)?"),
        ("reader",
         "COLD READER: which version lands harder for a reader with no knowledge of the editing history? Momentum, clarity, felt weight. Ignore the goal; judge the experience."),
    ];

    static readonly (string Key, string Brief)[] EscalationLenses =
    [
        ("causality",
         "CAUSALITY: which version better preserves the because-chain — events caused by prior events, decisions motivated on the page?"),
        ("emotion",
         "EMOTIONAL TRUTH: which version carries more genuine felt weight — body-before-mind, involuntary responses, no pseudo-profundity?"),
        ("mechanics",
         "PROSE MECHANICS: active voice, attribution discipline, paragraph rhythm, metaphor that illuminates. Which is cleaner craft?"),
        ("pacing",
         "PACING: which version better serves the beat's position in the story — tension held or released at the right moment, ending on the right note?"),
    ];

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Run a duel between a beat's current text and a candidate revision.
    /// Returns the verdict plus ballots; does NOT modify the beat — callers
    /// apply the replacement themselves on "replace".
    /// </summary>
    /// <param name="allowVotes">SS-A44 gate: must be true, passed consciously by a
    /// caller holding an explicit user instruction to run votes. Defaults to refusing.</param>
    public async Task<DuelResult> DuelAsync(
        string originalText,
        string revisionText,
        DuelContext context,
        bool allowVotes = false,
        CancellationToken ct = default)
    {
        if (!allowVotes)
            throw new InvalidOperationException(
                "Beat duels are votes (SS-A44). Pass allowVotes=true only under an explicit user instruction.");

        var originalHash = NodeWorkbenchService.ComputeTextHash(originalText);
        var revisionHash = NodeWorkbenchService.ComputeTextHash(revisionText);

        // Cache: same pair → same verdict, free.
        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var hit = await db.BeatDuelVerdicts.AsNoTracking()
                .FirstOrDefaultAsync(v => v.OriginalHash == originalHash && v.RevisionHash == revisionHash, ct);
            if (hit != null)
            {
                log.LogInformation("[duel] cache hit for {Orig}/{Rev}: {Verdict}",
                    originalHash[..8], revisionHash[..8], hit.Verdict);
                return new DuelResult(hit.Verdict == "replace", hit.RoundsRun,
                    hit.BetterVotes, hit.WorseVotes, hit.SameVotes,
                    JsonSerializer.Deserialize<List<DuelBallot>>(hit.BallotsJson) ?? [], FromCache: true);
            }
        }

        // Round 1: three lenses.
        var round1 = await CastBallotsAsync(Round1Lenses, originalText, revisionText, context, requireRationale: false, ct);
        var decision = DecideRound1(round1);
        var allBallots = new List<DuelBallot>(round1);
        var roundsRun = 1;

        if (decision == "escalate")
        {
            log.LogInformation("[duel] round 1 split ({B}/{W}/{S}) — escalating to 7 voters",
                round1.Count(b => b.Vote == "better"), round1.Count(b => b.Vote == "worse"), round1.Count(b => b.Vote == "same"));
            var round2 = await CastBallotsAsync(
                Round1Lenses.Concat(EscalationLenses).ToArray(),
                originalText, revisionText, context, requireRationale: true, ct);
            allBallots = round2.ToList();
            decision = DecideEscalation(round2);
            roundsRun = 2;
        }

        var result = new DuelResult(
            Replace:     decision == "replace",
            RoundsRun:   roundsRun,
            BetterVotes: allBallots.Count(b => b.Vote == "better"),
            WorseVotes:  allBallots.Count(b => b.Vote == "worse"),
            SameVotes:   allBallots.Count(b => b.Vote == "same"),
            Ballots:     allBallots,
            FromCache:   false);

        // Persist the verdict.
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            db.BeatDuelVerdicts.Add(new BeatDuelVerdict
            {
                OriginalHash = originalHash,
                RevisionHash = revisionHash,
                Verdict      = result.Replace ? "replace" : "keep",
                RoundsRun    = roundsRun,
                BetterVotes  = result.BetterVotes,
                WorseVotes   = result.WorseVotes,
                SameVotes    = result.SameVotes,
                BallotsJson  = JsonSerializer.Serialize(allBallots),
                Goal         = Truncate(context.Goal, 500),
                BeatId       = context.BeatId,
            });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[duel] verdict cache write failed — result still returned");
        }

        return result;
    }

    // ── Decision rules (static + internal for unit tests) ────────────────────

    /// <summary>REPLACE ≥2 better + 0 worse; KEEP ≥2 worse or ≥2 same; else ESCALATE.</summary>
    internal static string DecideRound1(IReadOnlyList<DuelBallot> ballots)
    {
        var better = ballots.Count(b => b.Vote == "better");
        var worse  = ballots.Count(b => b.Vote == "worse");
        var same   = ballots.Count(b => b.Vote == "same");

        if (better >= 2 && worse == 0) return "replace";
        if (worse >= 2 || same >= 2)   return "keep";
        return "escalate";
    }

    /// <summary>≥5/7 better replaces; anything else keeps (rationales = revision fuel).</summary>
    internal static string DecideEscalation(IReadOnlyList<DuelBallot> ballots)
        => ballots.Count(b => b.Vote == "better") >= 5 ? "replace" : "keep";

    // ── Ballot casting ────────────────────────────────────────────────────────

    async Task<List<DuelBallot>> CastBallotsAsync(
        (string Key, string Brief)[] lenses,
        string originalText,
        string revisionText,
        DuelContext context,
        bool requireRationale,
        CancellationToken ct)
    {
        // Deterministic-but-varied order per lens: even-indexed lenses see the
        // original first, odd-indexed see the revision first. Position bias
        // cancels across the panel without needing nondeterministic RNG.
        var tasks = lenses.Select((lens, i) =>
            CastOneAsync(lens.Key, lens.Brief, originalText, revisionText,
                originalFirst: i % 2 == 0, context, requireRationale, ct));
        return (await Task.WhenAll(tasks)).ToList();
    }

    async Task<DuelBallot> CastOneAsync(
        string lensKey, string lensBrief,
        string originalText, string revisionText, bool originalFirst,
        DuelContext context, bool requireRationale, CancellationToken ct)
    {
        var (v1, v2) = originalFirst ? (originalText, revisionText) : (revisionText, originalText);

        var system = $$"""
            You are one voter on a blind A/B panel judging two versions of the same story beat.
            You do not know which version is the incumbent and which is the revision — judge the
            text alone, through ONE lens:

            {{lensBrief}}

            Respond as JSON only:
            {
              "verdict": "version1" | "version2" | "same",
              "confidence": 0.0-1.0,
              "rationale": "{{(requireRationale ? "2-4 sentences: the concrete textual reason for your verdict — cite the versions" : "one sentence")}}"
            }
            "same" means no meaningful difference through your lens — use it honestly.
            """;

        var goalBlock = lensKey == "structural" && context.Goal is { Length: > 0 }
            ? $"\nREVISION GOAL (judge against this): {context.Goal}\n"
            : "";
        var registerBlock = lensKey == "register" && context.RegisterNotes is { Length: > 0 }
            ? $"\nSTORY REGISTER NOTES:\n{Truncate(context.RegisterNotes, 3000)}\n"
            : "";
        var precedingBlock = context.PrecedingText is { Length: > 0 }
            ? $"\nPRECEDING PROSE (for continuity — not under judgment):\n…{Truncate(context.PrecedingText, 1500)}\n"
            : "";

        var user = $"""
            STORY: {context.StoryTitle}{goalBlock}{registerBlock}{precedingBlock}
            ──── VERSION 1 ────
            {v1}

            ──── VERSION 2 ────
            {v2}

            Which version is better through your lens?
            """;

        try
        {
            var raw = await llm.GenerateAsync(system, user, temperature: 0.3, maxTokens: 350, ct: ct);
            var parsed = ParseJson<BallotRaw>(raw);
            var verdict = parsed?.Verdict?.ToLowerInvariant() ?? "same";

            // Map version1/version2 back to better/worse relative to the REVISION.
            var vote = verdict switch
            {
                "version1" => originalFirst ? "worse" : "better",
                "version2" => originalFirst ? "better" : "worse",
                _           => "same",
            };
            return new DuelBallot(lensKey, vote, parsed?.Confidence ?? 0.5, parsed?.Rationale ?? "");
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[duel] ballot failed for lens {Lens} — counting as 'same'", lensKey);
            return new DuelBallot(lensKey, "same", 0, $"(ballot failed: {ex.Message})");
        }
    }

    static string? Truncate(string? s, int max) =>
        s == null ? null : (s.Length <= max ? s : s[..max]);

    static T? ParseJson<T>(string raw)
    {
        try
        {
            var start = raw.IndexOf('{');
            var end   = raw.LastIndexOf('}');
            if (start < 0 || end < start) return default;
            return JsonSerializer.Deserialize<T>(raw[start..(end + 1)], new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch { return default; }
    }

    class BallotRaw
    {
        [JsonPropertyName("verdict")]    public string? Verdict { get; set; }
        [JsonPropertyName("confidence")] public double? Confidence { get; set; }
        [JsonPropertyName("rationale")]  public string? Rationale { get; set; }
    }
}

// ── Models ────────────────────────────────────────────────────────────────────

/// <summary>Context handed to every voter. Goal is what the revision tries to fix;
/// RegisterNotes ground the register lens (node bible excerpt); PrecedingText gives
/// continuity context without being under judgment.</summary>
public record DuelContext(
    string StoryTitle,
    string? Goal          = null,
    string? RegisterNotes = null,
    string? PrecedingText = null,
    Guid?   BeatId        = null);

/// <summary>One voter's ballot. Vote is relative to the REVISION: better/worse/same.</summary>
public record DuelBallot(string Lens, string Vote, double Confidence, string Rationale);

/// <summary>Duel outcome. Replace=false with 2 rounds run means the panel stayed
/// contested — the escalation ballots' rationales are the revision fuel.</summary>
public record DuelResult(
    bool Replace,
    int RoundsRun,
    int BetterVotes,
    int WorseVotes,
    int SameVotes,
    IReadOnlyList<DuelBallot> Ballots,
    bool FromCache);
