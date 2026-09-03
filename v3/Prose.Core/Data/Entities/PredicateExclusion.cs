namespace Prose.Core.Data.Entities;

/// <summary>
/// One disjointness axiom over the Story Ledger: a declaration that two
/// <c>(entity, predicate, object)</c> claim shapes cannot both be true of the same entity.
///
/// <para><b>Why this table exists.</b> <c>ContinuityService.Upsert</c> can only detect a
/// contradiction of one shape: <b>same predicate, different object</b> ("age 40" vs "age 52").
/// The defect that motivated the Story Ledger was a different shape entirely — <b>different
/// predicate, incompatible meaning</b>. "Kyle → <c>father</c> → Dae-jung Seo" and "Kyle →
/// <c>origin</c> → constructed, no prior life" never collide under a same-predicate rule, so a
/// live contradiction spanning ~290 beats of a finished, swept book was undetectable <i>by
/// construction</i>, not by bad luck. This table is how that shape becomes expressible.</para>
///
/// <para><b>What it is, honestly.</b> Disjointness axioms plus constraint checking over a fact
/// base — a small, well-understood corner of knowledge representation, not an invention. Its
/// limit is equally plain: it catches only contradictions whose predicates somebody thought to
/// declare mutually exclusive. It will not catch everything and must not be sold as if it will.
/// Its value is that it converts a whole class of currently-undetectable defects into detectable
/// ones, and that it sharpens per incident (see <see cref="Source"/> = <c>learned</c>) rather
/// than per patch.</para>
///
/// <para><b>Deliberately seeded sparse.</b> Only axioms that are logically or canonically
/// airtight ship as <c>builtin</c>. Every false-positive flood this project has hit came from a
/// rule that was <i>nearly</i> right applied corpus-wide (see
/// <c>ContinuityService.VolatilePredicates</c> for the last one, and
/// <c>SemanticFidelityService.IsIntentOutlier</c> for the one before). A thin ontology that
/// grows by confirmed incident beats a broad one that buries real findings.</para>
/// </summary>
public class PredicateExclusion
{
    public int Id { get; set; }

    /// <summary>The universe this axiom applies to. <see cref="Guid.Empty"/> means EVERY universe
    /// — correct for logical/biological axioms ("a constructed being has no biological parents"),
    /// which are not GLMZ facts. A canon-declared axiom (e.g. GLMZ's "Iowan Behemoths are
    /// autonomous machines, NOT synthetic life") is scoped to its own universe.</summary>
    public Guid UniverseId { get; set; }

    /// <summary>
    /// Left-hand predicate, as a <c>|</c>-separated set of alternatives (e.g.
    /// <c>"origin|nature|true_nature"</c>) — one axiom row covering the several predicate names
    /// extraction actually uses for one idea, since passes have historically disagreed
    /// (<c>life_status</c> / <c>life status</c> / <c>life-status</c>).
    ///
    /// <para>Alternatives match by EQUALITY after normalization, NOT substring — unlike
    /// <see cref="ObjectPatternA"/>. A predicate is an identifier: substring matching would make
    /// <c>father</c> also match <c>grandfather</c> and <c>stepfather</c>, quietly widening every
    /// axiom past what its author approved.</para>
    /// </summary>
    public string PredicateA { get; set; } = "";

    /// <summary>Optional object filter for the left-hand side: a <c>|</c>-separated set of
    /// alternatives, each matched as a case-insensitive SUBSTRING of the claim's object. Null or
    /// empty means "any object for this predicate".
    ///
    /// <para>Substring alternatives rather than regex, on purpose: a regex in a canon table is a
    /// footgun no author can safely review, and every pattern here has to be legible to the
    /// person approving it. <c>"constructed|construct|no prior life"</c> is a rule someone can
    /// read and judge; <c>"^(?:con)struct(?:ed)?\b"</c> is not.</para></summary>
    public string? ObjectPatternA { get; set; }

    /// <summary>Right-hand predicate. See <see cref="PredicateA"/>.</summary>
    public string PredicateB { get; set; } = "";

    /// <summary>Optional object filter for the right-hand side. See <see cref="ObjectPatternA"/>.</summary>
    public string? ObjectPatternB { get; set; }

    /// <summary>When true (the default), the axiom matches with the two sides swapped as well —
    /// claim order in the ledger is an accident of extraction order, not meaning. Set false only
    /// for a genuinely directional rule.</summary>
    public bool Symmetric { get; set; } = true;

    /// <summary>Where the axiom came from, in increasing specificity:
    /// <list type="bullet">
    /// <item><c>builtin</c> — a logical or biological axiom, true in every universe.</item>
    /// <item><c>canon</c> — declared by a universe's own Bible (a canon law made machine-readable).</item>
    /// <item><c>learned</c> — proposed by the adjudicator after it confirmed a real contradiction,
    /// pending author approval. This is the mechanism by which the ontology gets sharper per
    /// incident instead of per patch.</item>
    /// </list></summary>
    public string Source { get; set; } = "learned";

    /// <summary><c>active</c> (generates candidates), <c>proposed</c> (awaits author approval —
    /// generates nothing), or <c>rejected</c> (the author judged it wrong; kept so the same
    /// proposal is not re-raised on the next run).</summary>
    public string Status { get; set; } = "proposed";

    /// <summary>Why these two shapes are incompatible, in one plain sentence. This is what the
    /// author reads when approving a <c>learned</c> proposal, and what the finding quotes when
    /// the axiom fires — so it has to justify itself, not restate the predicates.</summary>
    public string Rationale { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set when a human moved this out of <c>proposed</c>. Null for a builtin (which
    /// ships active) and for anything still awaiting review.</summary>
    public DateTime? ApprovedAt { get; set; }

    public string? ApprovedBy { get; set; }
}
