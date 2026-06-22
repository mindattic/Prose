namespace StreetSamurai.Core.Services;

/// <summary>
/// RFC 0009 §2 — review cost tiers. A review's job is to <i>guide prose</i>, not to mint a
/// number, so the dial is "what signal do I need right now?" — scaled to the importance of
/// the task rather than spent flat on every check.
///
/// <list type="bullet">
///   <item><b>Draft</b> — mid-draft spot checks / per-beat iteration. Cheapest: tiny ballot
///   panel, no diagnosis, no prose upgrades. ~6 calls (−84% vs the old flat default). The
///   score CI is intentionally wide; Draft is for "which beats drag", not gate decisions.</item>
///   <item><b>Standard</b> — the per-strand standalone gate (≥82%) and routine iteration.
///   Balanced workhorse. ~15 calls (−60%).</item>
///   <item><b>Deep</b> — cumulative-prefix gate (≥85%), pre-publish, flagship. Full panel +
///   structural diagnosis + prose critique. ~37 calls (the historical default).</item>
/// </list>
///
/// Only the call-count knobs are scaled here — the dominant token driver. Per-provider model
/// tiering (running ballots on haiku/flash/nano under Draft) is a documented follow-up:
/// <see cref="AllowedProviders"/> carries the intent, but applying it requires a per-run
/// override on <c>StrandReviewService</c> rather than mutating persisted settings.
/// </summary>
public sealed record ReviewEffortProfile(
    string Name,
    int Ballots,
    int Prose,
    bool SkipDiagnosis,
    string? AllowedProviders,
    string Note)
{
    public static readonly ReviewEffortProfile Draft = new(
        "draft", Ballots: 6, Prose: 0, SkipDiagnosis: true,
        AllowedProviders: "claude,gemini",
        "mid-draft spot check — per-beat gripes + a rough score; NOT for gate decisions");

    public static readonly ReviewEffortProfile Standard = new(
        "standard", Ballots: 12, Prose: 2, SkipDiagnosis: true,
        AllowedProviders: null,
        "standalone gate (≥82%) — trustworthy score + top fixes");

    public static readonly ReviewEffortProfile Deep = new(
        "deep", Ballots: 20, Prose: 4, SkipDiagnosis: false,
        AllowedProviders: null,
        "cumulative/publish gate (≥85%) — full panel + diagnosis + prose critique");

    /// <summary>Resolve a tier name; null for an unrecognised value (caller keeps its defaults).</summary>
    public static ReviewEffortProfile? Resolve(string? name) => name?.Trim().ToLowerInvariant() switch
    {
        "draft"    => Draft,
        "standard" => Standard,
        "deep"     => Deep,
        _          => null,
    };

    public static string KnownTiers => "draft | standard | deep";
}
