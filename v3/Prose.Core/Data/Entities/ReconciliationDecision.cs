namespace Prose.Core.Data.Entities;

/// <summary>
/// One autonomous Trinity Reconciliation decision — a permanent audit/undo record, distinct from
/// <c>Findings</c> (which gets purge-and-refiled by every sweep; wrong semantics for "what did we
/// auto-edit and how do I undo it"). Written by
/// <see cref="Prose.Core.Services.TrinityReconciliationService"/> every time it resolves a
/// <c>ContinuityService.ContradictionGroup</c> or an applied-claim-drift result by picking a
/// winning source and editing the losing source(s) to match. See
/// <c>Prose.Cli.Cli.ReconcileTrinityCli</c> for the CLI surface, including
/// <c>--reconcile-trinity --undo --decision-id</c> which dispatches to
/// <see cref="Prose.Core.Services.TrinityReconciliationService.RevertDecisionAsync"/> keyed by
/// <see cref="EditMechanism"/>.
/// </summary>
public class ReconciliationDecision
{
    public Guid Id { get; set; }

    /// <summary>Code of the BookNode this decision belongs to (e.g. "BCODA").</summary>
    public string BookSlug { get; set; } = "";

    /// <summary>"contradiction_group" (N-way ContinuityService.ContradictionGroup) |
    /// "applied_claim_drift" (ContinuityApplyService.AppliedClaimDriftResult).</summary>
    public string DivergenceType { get; set; } = "";

    /// <summary>The entity the divergence is about — same shape as ContinuityClaim.EntityId.</summary>
    public string EntityId { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string Predicate { get; set; } = "";

    /// <summary>"outline" | "prose" | "entity_record" — which source's value won.</summary>
    public string WinningSourceType { get; set; } = "";
    public string WinningValue { get; set; } = "";

    /// <summary>The panel's editorial reasoning for the pick (Legion DecideAsync.Reasoning).</summary>
    public string DecisionReasoning { get; set; } = "";
    public double DecisionConfidence { get; set; }

    /// <summary>JSON array of ClaimUid strings for every losing claim in the group/drift check.</summary>
    public string LosingClaimUidsJson { get; set; } = "[]";

    /// <summary>"beat_repair" | "outline_section" | "entity_record" — which mechanism performed the
    /// edit to bring the losing source(s) into agreement with <see cref="WinningValue"/>.</summary>
    public string EditMechanism { get; set; } = "";

    /// <summary>JSON describing exactly what was edited — shape depends on
    /// <see cref="EditMechanism"/> (e.g. {"beatId":...,"nodeId":...} for beat_repair;
    /// {"nodeId":...,"sectionType":...} for outline_section; {"claimUid":...} for entity_record).</summary>
    public string EditTargetJson { get; set; } = "{}";

    /// <summary>Pre-edit content snapshot, populated ONLY for <c>outline_section</c> edits —
    /// <c>NodeOutlineSections</c> is not a system-versioned temporal table (confirmed via
    /// <c>sys.tables</c>, unlike Nodes/Records/Beats/ContinuityClaims), so this is the only way to
    /// recover the section's prior content on <c>RevertDecisionAsync</c>. Null for prose/entity_record
    /// edits, which restore from their own tables' temporal history instead.</summary>
    public string? PreEditSnapshotJson { get; set; }

    /// <summary>True when this row was produced by a <c>--dry-run</c> — no edit or ledger call was
    /// actually made; the row exists purely as a record of what WOULD have happened.</summary>
    public bool DryRun { get; set; }

    /// <summary>True once <c>RevertDecisionAsync</c> has successfully undone this decision's edit
    /// and ledger flip. A reverted decision is never re-reverted.</summary>
    public bool Reverted { get; set; }
    public DateTime? RevertedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>"cli-manual" (default — a human ran --reconcile-trinity) | "scheduled-auto" (the
    /// unattended ContinuityLongSweepService auto-reconcile path). Lets anyone reviewing history
    /// immediately tell which edits were unattended vs. deliberate.</summary>
    public string TriggeredBy { get; set; } = "cli-manual";
}
