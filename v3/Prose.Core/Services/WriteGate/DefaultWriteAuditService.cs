using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;

namespace Prose.Core.Services.WriteGate;

/// <summary>
/// The write-gate's first real post-save check (2026-08-22). Dispatches an <see cref="EntityCore"/>
/// or <see cref="EntityOrigin"/> write to <see cref="DuplicateEntityScanService.CheckSingleEntityAsync"/>
/// — built in Phase 0 specifically for this hook but never wired to anything until now — and files
/// any hit as a <see cref="FindingCategory.NearDuplicate"/> finding, in the exact format
/// <see cref="AutoCorrectOrchestratorService"/>'s corpus-wide nightly scan already uses
/// (<c>"universe:{slug}"</c> file path, <c>"DUPLICATE-ENTITY [{entityType}] ..."</c> summary) so a
/// write-gate hit and a nightly-scan hit on the same pair dedupe into one row via
/// <see cref="FindingsService.Upsert"/>'s existing dedup key, rather than doubling the inbox.
///
/// Deliberately does NOT handle Beat/Node subjects here — those already get their own narrow
/// blast-radius/logic-sweep dispatch from <c>NodeWorkbenchService</c>'s own hooks (Layer B).
/// Duplicating that here would double-fire the same checks for every write that already goes
/// through <c>NodeWorkbenchService</c>, while doing nothing for writes that still bypass it
/// (which need their own Phase 1/2/3 remediation, not a generic catch-all here).
/// </summary>
public sealed class DefaultWriteAuditService : IWriteAuditService
{
    private readonly DuplicateEntityScanService dupScan;
    private readonly FindingsService findings;
    private readonly IDbContextFactory<ProseDbContext> dbFactory;
    private readonly ILogger<DefaultWriteAuditService> log;

    public DefaultWriteAuditService(
        DuplicateEntityScanService dupScan,
        FindingsService findings,
        IDbContextFactory<ProseDbContext> dbFactory,
        ILogger<DefaultWriteAuditService> log)
    {
        this.dupScan = dupScan;
        this.findings = findings;
        this.dbFactory = dbFactory;
        this.log = log;
    }

    public async Task DispatchAsync(WriteEvent evt, CancellationToken ct)
    {
        try
        {
            switch (evt.Subject)
            {
                case WriteSubject.EntityCore:
                case WriteSubject.EntityOrigin:
                    await CheckDuplicateAsync(evt, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Never let a failed audit check look like a failed write — the save already
            // committed by the time this fires (see ProseDbContext.DispatchWriteEvents).
            log.LogError(ex, "Write-gate audit dispatch failed for {Subject} {Id}", evt.Subject, evt.PrimaryId);
        }
    }

    private async Task CheckDuplicateAsync(WriteEvent evt, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var target = await db.Entities.AsNoTracking().IgnoreQueryFilters()
            .Where(e => e.Id == evt.PrimaryId)
            .Select(e => new { e.UniverseId, e.EntityType })
            .FirstOrDefaultAsync(ct);
        if (target == null) return;

        var groups = await dupScan.CheckSingleEntityAsync(evt.PrimaryId, ct);
        if (groups.Count == 0) return;

        var slug = await db.Universes.AsNoTracking()
            .Where(u => u.Id == target.UniverseId).Select(u => u.Slug).FirstOrDefaultAsync(ct)
            ?? target.UniverseId.ToString();

        foreach (var group in groups)
        {
            var suggested = group.Candidates.Count == 2
                ? "Auto-mergeable — the lower-mention-count row can merge into the higher-mention-count one."
                : "3+ candidates — which pair is the real duplicate is ambiguous; needs a human to pick.";
            findings.Upsert($"universe:{slug}", chapterId: null, FindingCategory.NearDuplicate, FindingSeverity.Medium,
                $"DUPLICATE-ENTITY [{target.EntityType}] {group.MatchedOn} — {string.Join(" / ", group.Candidates.Select(c => c.Name))}",
                snippet: null, suggestedFix: suggested);
        }
    }
}
