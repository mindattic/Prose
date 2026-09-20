namespace Prose.Core.Data.Entities;

/// <summary>
/// Cached LLM verdict for one adjudicated contradiction candidate.
///
/// <para><b>This is the hash gate for the Tuned Read.</b> Candidate generation is deterministic
/// and free; adjudication is one narrow LLM call per candidate and is the only thing the tuned
/// read spends money on. Keying the verdict on <see cref="CacheKey"/> — a hash over the two
/// claims, the axiom that paired them, and the CURRENT text of both anchor beats — makes a
/// re-run on an unchanged book cost exactly zero, and makes a re-run after a real prose edit
/// re-adjudicate only the candidates that edit actually touched.</para>
///
/// <para>Anchor-beat text is part of the key on purpose. A verdict is a judgement about specific
/// prose; if that prose changes, the verdict is stale even though both claim rows are byte-identical
/// (a claim's object can survive a rewrite that inverts its meaning). Keying on claim uids alone
/// would have cached the wrong answer past the fix.</para>
///
/// <para>The plan called for a <c>TunedReadCursor</c> keyed on <c>Beat.TextHash</c>. This is that
/// idea at the right granularity: the unit of work is a candidate PAIR, not a beat, and a
/// beat-keyed cursor cannot express "these two claims, 290 beats apart, were judged against each
/// other" — which is the entire point of the instrument.</para>
/// </summary>
public class TunedReadAdjudication
{
    public long Id { get; set; }

    /// <summary>SHA-256 over (claimAUid, claimBUid, exclusionRuleId, anchorATextHash,
    /// anchorBTextHash). Unique — one cached verdict per exact question.</summary>
    public string CacheKey { get; set; } = "";

    public string ClaimAUid { get; set; } = "";
    public string ClaimBUid { get; set; } = "";

    /// <summary>The <see cref="PredicateExclusion"/> that paired these two, or null when the pair
    /// came from the existing same-predicate/different-object collision instead.</summary>
    public int? ExclusionRuleId { get; set; }

    public string? BookSlug { get; set; }

    /// <summary>True when the adjudicator judged the two claims genuinely incompatible AND its
    /// cited quote survived the mechanical grounding gate. A false verdict is cached too — a
    /// candidate the adjudicator cleared must not be re-billed on every subsequent run.</summary>
    public bool IsContradiction { get; set; }

    /// <summary>"BLOCKER" | "MODERATE" | "MINOR", or null when not a contradiction.</summary>
    public string? Severity { get; set; }

    /// <summary>The verbatim quote the adjudicator cited as evidence. A verdict whose quote does
    /// not appear in the anchor beat's text is rejected before it is ever cached as a
    /// contradiction (see <c>LogicSweepService.QuotedEvidenceAppearsInBeat</c>) — an
    /// unquotable claim about the prose is exactly the failure this whole program exists to
    /// stop.</summary>
    public string? EvidenceQuote { get; set; }

    /// <summary>One sentence on what actually conflicts. Becomes the finding's summary.</summary>
    public string? Note { get; set; }

    /// <summary>Set when the adjudicator's quote failed the grounding gate — the verdict is
    /// recorded as NOT a contradiction, and this says why, so a silently-dropped verdict can
    /// never look the same as a genuinely clean pair.</summary>
    public string? RejectedReason { get; set; }

    public DateTime AdjudicatedAt { get; set; } = DateTime.UtcNow;
}
