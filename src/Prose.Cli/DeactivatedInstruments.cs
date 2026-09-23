namespace Prose.Cli;

/// <summary>
/// The QA/audit instruments switched off on 2026-09-22 under RFC 0014.
///
/// <para>Measured corpus-wide that day: <b>30,745 findings filed, all time; 8 ever applied</b>
/// (0.026%). Exactly three producers earned those 8 — LEDGER-CONFLICT (<c>--ledger-adjudicate</c>,
/// 5), LOGICSWEEP (<c>--logic-sweep</c>, 2) and LINT (<c>--lint-prose</c>, 1). Those three stay
/// live. Everything listed here has filed findings for months and had none applied, ever.
/// The author's ruling: <i>"just comment out all of these, they've never proven their worth …
/// tired of it burning $400 and fixing nothing."</i></para>
///
/// <para>This is one list rather than 35 commented-out dispatch blocks on purpose. The dispatch
/// chain in <c>Program.cs</c> is ~3,200 lines of top-level statements that share locals across
/// blocks, so commenting them in place risks breaking the build in ways nobody notices until a
/// redeploy. <b>Nothing is deleted.</b> Every handler class is intact and untouched; deleting a
/// line from <see cref="Flags"/> brings its command straight back.</para>
///
/// <para>The measurement path is deliberately NOT deactivated: <c>--findings</c> (including
/// <c>--findings stats --by-instrument</c>, the rollup that produced these numbers),
/// <c>--calibrate-obligations</c> and <c>--inject-calibration-defects</c> are how an instrument
/// earns its way back in. So is <c>--publish-readiness</c>, which still computes the gate report
/// on demand — it just no longer blocks <c>--export-node</c>.</para>
/// </summary>
public static class DeactivatedInstruments
{
    /// <summary>Every CLI flag that is switched off, with the filed/applied counts behind it.</summary>
    public static readonly IReadOnlyList<string> Flags =
    [
        // Reader-proxy / craft panels — COMPREHENSION 237, GRIPE 3, ENGAGEMENT 4, CRAFT 12,
        // HOOK 10, THEME 4, SWAIN 4, FIVEACT 6, SACRED-FLAW 6, EMOTIONAL-DEPTH 7, DRAMATIC-Q 8,
        // CHECKLIST 3,052, READABILITY 4,840 filed. 0 applied between them.
        "--reader-qa", "--review-node", "--review-entity", "--review-report",
        "--dual-read", "--duel", "--character-depth-audit", "--narrative-health",

        // Beat lenses — CAUSALITY 22, AFFECT-BEHAVIOR 9, INTERPERSONAL 11 filed, 0 applied.
        "--causality-check", "--affect-check", "--interpersonal-check",

        // Continuity / contradiction finders — CONTINUITY-VIOLATION 528, CANON-CONTRADICTION 81,
        // ENTITY-CONFLICT 328, SEMANTIC-DRIFT 1,055, Entity 656 filed, 0 applied.
        "--continuity", "--cross-book-consistency-audit",
        "--timeline-check", "--description-drift",

        // Obligation ledger — RFC 0013 §8 measured 96% false positives on prose engineered to owe
        // nothing (precision 0.037 on the one valid calibration run); 140 filed, 0 applied.
        "--reconcile-obligations", "--ground-entity-records",

        // Fact ledger (BookHealthService.FactLedgerAsync — NOT the proven LEDGER-CONFLICT, which
        // is a different instrument) — 129 filed, 0 applied. TUNEDREAD never separately measured.
        "--fact-ledger-refresh", "--tuned-read",

        // Scanners — SANITY 371, BEAT-NEAR-DUPLICATE 39, DUPLICATE-ENTITY 4, COORDINATE 6,
        // NOUNCONSISTENCY 1, [PROSE-HEALTH 226, LIBERTY-CONSIDER 2,143,
        // CANON-ADDITION-CANDIDATE 1,275, VERIFY 3 filed. 0 applied between them.
        "--sanity-scan", "--check-duplicate-beats", "--duplicate-entity-scan",
        "--duplicate-entity-scan-broad", "--location-scan", "--validate-nouns",
        "--unresolved-nouns", "--scan-unnamed-referents", "--prose-health",
        "--liberty-report", "--gear-check",

        // The full battery — RFC 0010 measured $27–135 a run for zero applied findings.
        "--auto-run", "--auto-correct-nightly",
    ];

    /// <summary>True when <paramref name="flag"/> names a deactivated instrument.</summary>
    public static bool Contains(string flag) =>
        Flags.Contains(flag, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The message printed when someone invokes one. It names the flag, says why, and says
    /// exactly where to go to turn it back on — a dead command that does not tell you how to
    /// revive it is how a temporary deactivation becomes a permanent, undocumented hole.
    /// </summary>
    public static string[] ExplainLines(string flag) =>
    [
        $"[prose] {flag} is DEACTIVATED (2026-09-22, RFC 0014).",
        // ASCII only: the Windows console renders this stream in the OEM codepage, so an em dash
        // here comes out as mojibake in the one message whose whole job is to be read and acted on.
        "[prose] It filed findings for months and had none applied, ever. The handler is intact -",
        "[prose] delete its line from src/Prose.Cli/DeactivatedInstruments.cs to restore it.",
        "[prose] Still live: --logic-sweep, --ledger-adjudicate and --lint-prose (the only three",
        "[prose] producers that have ever had a finding applied), --findings, --publish-readiness.",
    ];
}
