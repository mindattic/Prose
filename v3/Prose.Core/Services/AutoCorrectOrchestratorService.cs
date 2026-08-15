using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;

namespace Prose.Core.Services;

public sealed record AutoCorrectOptions(Guid? UniverseId = null, bool DryRun = false);

public sealed record AutoCorrectBookResult(Guid NodeId, string Slug, int FindingsRefreshed, List<string> Notes);

public sealed record AutoCorrectRunReport(
    Guid RunId, DateTime StartedAt, DateTime FinishedAt,
    int UniverseProfilesRefreshed,
    List<AutoCorrectBookResult> Books,
    int EntitiesMerged, int ConsistencyFixesApplied, int ContinuityResolutions,
    List<string> Notes);

/// <summary>
/// The nightly AutoCorrect orchestrator — pure ML/deterministic, zero LLM calls (see the "Scope
/// boundary" section of the AutoCorrect plan, 2026-08-14). Ties together detectors that already
/// exist (<see cref="SanityScanService"/>, <see cref="NightlyHealthService"/>,
/// <see cref="BeatDuplicateService"/>) plus a small, explicit whitelist of AUTO-FIXABLE categories:
///
///   - Duplicate character/faction/place Entity rows (unambiguous pairs only) → merged via
///     <see cref="DuplicateEntityScanService.MergeAsync"/>.
///   - Dangling EntityStateEvents / stale affiliation-alias / hometurf-alias drift → applied via
///     <see cref="DataConsistencyService.ApplyDeterministicFixesWithLedgerAsync"/> (a fully-built
///     method that had zero call sites anywhere in the codebase before this).
///   - Cross-book continuity contradictions with a clean, non-tied majority (exactly two claimed
///     values) → resolved via <see cref="ContinuityService.Resolve"/>.
///
/// Everything else these detectors surface (prose-quality findings: code-leaks, mojibake, low
/// readability, voice drift, near-duplicate BEATS) stays flag-only — fixing those means rewriting
/// prose, which by definition needs the LLM (<see cref="ProseWriterRouter"/>/
/// <see cref="BeatRepairService"/>), so a "no LLM" pass never attempts it. BEAT-NEAR-DUPLICATE in
/// particular was deliberately excluded from auto-fix even though it looked whitelist-eligible in
/// the original plan: <see cref="BeatDuplicateService"/>'s own doc comment says a high-similarity
/// pair can be a legitimate intentional callback, not a bug — "a candidate generator, not a
/// verdict" — and auto-deleting the wrong side would destroy authored content a mechanical
/// tie-break rule has no way to judge.
///
/// Every mutation is logged to <see cref="SelfHealLedgerService"/> BEFORE the run finishes, so
/// `prose --auto-correct-undo` can rewind it — see that service's doc comment for why this is a
/// bounded per-action ledger and not a return to database-wide temporal tables.
/// </summary>
public class AutoCorrectOrchestratorService(
    IDbContextFactory<ProseDbContext> dbFactory,
    BookArchiveService archiveSvc,
    SelfHealLedgerService ledger,
    SanityScanService sanityScan,
    NightlyHealthService nightlyHealth,
    BeatDuplicateService beatDup,
    DuplicateEntityScanService dupEntityScan,
    DataConsistencyService dataConsistency,
    CrossBookConsistencyService crossBook,
    ContinuityService continuityService,
    UniverseProfileService universeProfiles,
    FindingsService findingsSvc,
    ILogger<AutoCorrectOrchestratorService> log)
{
    /// <summary>Refuse-rather-than-cascade guard, same spirit as
    /// <see cref="BeatRepairService.IsUnsafeShrink"/> — once this many entity merges have gone
    /// through in one universe on one run, stop and leave the rest flagged, don't keep going on
    /// the theory that "more of the same fix" is automatically safe.</summary>
    private const int MaxEntityMergesPerUniversePerRun = 20;

    private static readonly string[] EntityTypesToDedupe = ["character", "faction", "place"];

    public async Task<AutoCorrectRunReport> RunAsync(AutoCorrectOptions opts, CancellationToken ct = default)
    {
        var runId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;
        int seq = 0;
        int NextSeq() => ++seq;

        var notes = new List<string>();
        var bookResults = new List<AutoCorrectBookResult>();
        int profilesRefreshed = 0, entitiesMerged = 0;

        await using (var db = await dbFactory.CreateDbContextAsync(ct))
        {
            var universes = opts.UniverseId.HasValue
                ? await db.Universes.AsNoTracking().Where(u => u.Id == opts.UniverseId.Value).ToListAsync(ct)
                : await db.Universes.AsNoTracking().Where(u => u.IsActive).ToListAsync(ct);

            foreach (var universe in universes)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var sampleSize = await universeProfiles.RefreshDensityBaselinesAsync(universe.Id, ct);
                    if (sampleSize > 0) profilesRefreshed++;
                }
                catch (Exception ex) { log.LogWarning(ex, "AutoCorrect: universe profile refresh failed for {Slug}", universe.Slug); }

                entitiesMerged += await RunEntityDedupAsync(universe, runId, NextSeq, opts, notes, ct);

                var books = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
                    .OfType<BookNode>()
                    .Where(b => b.UniverseId == universe.Id)
                    .Select(b => new { b.Id, b.Slug })
                    .ToListAsync(ct);

                foreach (var book in books)
                {
                    ct.ThrowIfCancellationRequested();
                    var slug = book.Slug ?? book.Id.ToString("N");
                    try
                    {
                        bookResults.Add(await RefreshBookFindingsAsync(book.Id, slug, opts, ct));
                    }
                    catch (Exception ex)
                    {
                        log.LogWarning(ex, "AutoCorrect: book {Slug} detection pass failed, skipping", slug);
                        bookResults.Add(new AutoCorrectBookResult(book.Id, slug, 0, [$"FAILED: {ex.Message}"]));
                    }
                }
            }
        }

        var consistencyFixed = await RunConsistencyFixesAsync(runId, NextSeq, opts, notes, ct);
        var continuityResolved = await RunContinuityMajorityResolveAsync(runId, NextSeq, opts, notes, ct);

        return new AutoCorrectRunReport(
            runId, startedAt, DateTime.UtcNow, profilesRefreshed, bookResults,
            entitiesMerged, consistencyFixed, continuityResolved, notes);
    }

    // ── Per-book: detection/finding-refresh only — NO fixes happen here (see class doc comment) ──

    private async Task<AutoCorrectBookResult> RefreshBookFindingsAsync(Guid nodeId, string slug, AutoCorrectOptions opts, CancellationToken ct)
    {
        var notes = new List<string>();
        int refreshed = 0;

        if (!opts.DryRun)
        {
            try { await archiveSvc.ArchiveAsync(nodeId, "autocorrect-pre-run", ct); }
            catch (Exception ex) { notes.Add($"archive failed: {ex.Message}"); }
        }

        try
        {
            var report = await sanityScan.ScanAsync(nodeId, ct);
            if (report.BeatCount > 2)
            {
                SanityScanService.FileFindings(findingsSvc, slug, report);
                refreshed++;
            }
        }
        catch (Exception ex) { notes.Add($"sanity-scan failed: {ex.Message}"); }

        try { await nightlyHealth.RunAsync(slug, ct); refreshed++; }
        catch (Exception ex) { notes.Add($"nightly-health failed: {ex.Message}"); }

        try { await beatDup.CheckNodeAsync(nodeId, ct: ct); refreshed++; }
        catch (Exception ex) { notes.Add($"beat-duplicate-check failed: {ex.Message}"); }

        return new AutoCorrectBookResult(nodeId, slug, refreshed, notes);
    }

    // ── Universe-scoped fix: duplicate entity merge ──────────────────────────

    private async Task<int> RunEntityDedupAsync(
        Universe universe, Guid runId, Func<int> nextSeq, AutoCorrectOptions opts, List<string> notes, CancellationToken ct)
    {
        int merged = 0;
        foreach (var entityType in EntityTypesToDedupe)
        {
            if (merged >= MaxEntityMergesPerUniversePerRun) break;

            IReadOnlyList<DuplicateEntityGroup> groups;
            try { groups = await dupEntityScan.ScanAsync(universe.Id, entityType, ct); }
            catch (Exception ex)
            {
                log.LogWarning(ex, "AutoCorrect: duplicate-entity scan failed for {Universe}/{Type}", universe.Slug, entityType);
                continue;
            }

            foreach (var group in groups)
            {
                if (merged >= MaxEntityMergesPerUniversePerRun) break;

                var suggested = group.Candidates.Count == 2
                    ? "Auto-mergeable — AutoCorrect merges the lower-mention-count row into the higher-mention-count one."
                    : "3+ candidates — which pair is the real duplicate is ambiguous; needs a human to pick.";
                var findingId = findingsSvc.Upsert($"universe:{universe.Slug}", chapterId: null, FindingCategory.NearDuplicate, FindingSeverity.Medium,
                    $"DUPLICATE-ENTITY [{entityType}] {group.MatchedOn} — {string.Join(" / ", group.Candidates.Select(c => c.Name))}",
                    snippet: null, suggestedFix: suggested);

                if (group.Candidates.Count != 2) continue; // ambiguous — stays flag-only

                var winner = group.Candidates.OrderByDescending(c => c.MentionCount).ThenBy(c => c.Id).First();
                var loser = group.Candidates.First(c => c.Id != winner.Id);

                if (opts.DryRun)
                {
                    notes.Add($"[dry-run] would merge {entityType} '{loser.Name}' ({loser.Id}) into '{winner.Name}' ({winner.Id})");
                    continue;
                }

                try
                {
                    var result = await dupEntityScan.MergeAsync(winner.Id, loser.Id, ct);
                    await ledger.LogAsync(runId, nextSeq(), nodeId: null, "entity-merge", result.UndoLog,
                        $"Merged duplicate {entityType} '{loser.Name}' into '{winner.Name}' ({group.MatchedOn}) — " +
                        $"{result.RowsRelinked} row(s) relinked, {result.RowsDeletedForCollision} row(s) collision-deleted.",
                        findingId, ct);
                    findingsSvc.SetStatus(findingId, FindingStatus.Applied);
                    notes.Add($"Merged {entityType} '{loser.Name}' → '{winner.Name}'");
                    merged++;
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "AutoCorrect: entity merge failed ({Loser} -> {Winner})", loser.Id, winner.Id);
                    notes.Add($"Merge FAILED for '{loser.Name}' → '{winner.Name}': {ex.Message}");
                }
            }
        }
        return merged;
    }

    // ── Corpus-wide fix: DataConsistencyService's already-implemented, never-called fixer ───────

    private static readonly string[] ConsistencyFixCodes = ["ESE-DANGLING", "CHAR-AFFIL-ALIAS-DRIFT", "CHAR-HOMETURF-ALIAS-DRIFT"];

    private async Task<int> RunConsistencyFixesAsync(Guid runId, Func<int> nextSeq, AutoCorrectOptions opts, List<string> notes, CancellationToken ct)
    {
        DataConsistencyService.ConsistencyReport report;
        try { report = await dataConsistency.RunAsync(ct); }
        catch (Exception ex) { log.LogWarning(ex, "AutoCorrect: consistency audit failed"); return 0; }

        var findingIdByCode = new Dictionary<string, long>();
        foreach (var f in report.Findings)
        {
            var severity = f.Severity switch { "error" => FindingSeverity.High, "warn" => FindingSeverity.Medium, _ => FindingSeverity.Low };
            var id = findingsSvc.Upsert("corpus:data-consistency", chapterId: null, FindingCategory.Other, severity,
                $"{f.Code}: {f.Title} ({f.DriftCount} row(s))", snippet: null, suggestedFix: f.FixHint);
            findingIdByCode[f.Code] = id;
        }

        var fixableCodes = report.Findings.Select(f => f.Code).Where(c => ConsistencyFixCodes.Contains(c)).ToList();
        if (fixableCodes.Count == 0 || opts.DryRun)
        {
            if (opts.DryRun && fixableCodes.Count > 0)
                notes.Add($"[dry-run] would apply consistency fixes: {string.Join(", ", fixableCodes)}");
            return 0;
        }

        Dictionary<string, DataConsistencyService.LedgeredFixResult> applied;
        try { applied = await dataConsistency.ApplyDeterministicFixesWithLedgerAsync(fixableCodes, ct); }
        catch (Exception ex) { log.LogWarning(ex, "AutoCorrect: consistency fix apply failed"); return 0; }

        int totalRows = 0;
        foreach (var (code, fixResult) in applied)
        {
            if (fixResult.Count == 0) continue;
            findingIdByCode.TryGetValue(code, out var findingId);
            await ledger.LogAsync(runId, nextSeq(), nodeId: null, "consistency-fix", fixResult.Undo,
                $"{code}: fixed {fixResult.Count} row(s).", findingId == 0 ? null : findingId, ct);
            if (findingId != 0) findingsSvc.SetStatus(findingId, FindingStatus.Applied);
            notes.Add($"{code}: {fixResult.Count} row(s) fixed");
            totalRows += fixResult.Count;
        }
        return totalRows;
    }

    // ── Corpus-wide fix: cross-book continuity majority resolution ──────────────────────────────

    private async Task<int> RunContinuityMajorityResolveAsync(Guid runId, Func<int> nextSeq, AutoCorrectOptions opts, List<string> notes, CancellationToken ct)
    {
        CrossBookConsistencyReport report;
        try { report = await crossBook.GetCrossBookConflictsAsync(since: null, ct: ct); }
        catch (Exception ex) { log.LogWarning(ex, "AutoCorrect: cross-book consistency check failed"); return 0; }

        int resolved = 0;
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        foreach (var conflict in report.Conflicts)
        {
            var isCleanMajority = conflict.VariantCount == 2 && conflict.MajorityCount > conflict.MinorityCount
                && conflict.MajorityClaimUids.Count > 0 && conflict.MinorityClaimUids.Count > 0;

            var findingId = findingsSvc.Upsert($"cross-book:{conflict.EntityName}", chapterId: null, FindingCategory.Contradiction, FindingSeverity.Medium,
                $"CROSS-BOOK-CONTRADICTION {conflict.EntityName} {conflict.Predicate}: \"{conflict.MajorityObject}\" ({conflict.MajorityCount}x, {string.Join("/", conflict.MajorityBooks)}) " +
                $"vs \"{conflict.MinorityObject}\" ({conflict.MinorityCount}x, {string.Join("/", conflict.MinorityBooks)})",
                snippet: null,
                suggestedFix: isCleanMajority
                    ? "Auto-resolvable — AutoCorrect promotes the majority claim to CANONICAL and rejects the minority."
                    : "3+ distinct claimed values — genuinely ambiguous, needs a human to pick.");

            if (!isCleanMajority || opts.DryRun)
            {
                if (opts.DryRun && isCleanMajority)
                    notes.Add($"[dry-run] would resolve continuity majority for {conflict.EntityName}/{conflict.Predicate}");
                continue;
            }

            var winnerUid = conflict.MajorityClaimUids[0];
            foreach (var loserUid in conflict.MinorityClaimUids)
            {
                try
                {
                    var winnerOld = await db.ContinuityClaims.AsNoTracking().FirstOrDefaultAsync(c => c.ClaimUid == winnerUid, ct);
                    var loserOld = await db.ContinuityClaims.AsNoTracking().FirstOrDefaultAsync(c => c.ClaimUid == loserUid, ct);
                    if (winnerOld == null || loserOld == null) continue;

                    continuityService.Resolve(winnerUid, loserUid, "a", note: "AutoCorrect: unambiguous cross-book majority");

                    var undo = new List<RowMutationUndo>
                    {
                        new("update", "ContinuityClaims", "ClaimUid", winnerUid,
                            new Dictionary<string, string?> { ["Status"] = winnerOld.Status, ["ResolvedAt"] = winnerOld.ResolvedAt, ["ResolutionNote"] = winnerOld.ResolutionNote }),
                        new("update", "ContinuityClaims", "ClaimUid", loserUid,
                            new Dictionary<string, string?> { ["Status"] = loserOld.Status, ["ResolvedAt"] = loserOld.ResolvedAt, ["ResolutionNote"] = loserOld.ResolutionNote }),
                    };
                    await ledger.LogAsync(runId, nextSeq(), nodeId: null, "continuity-resolve", undo,
                        $"Resolved {conflict.EntityName}/{conflict.Predicate}: '{conflict.MajorityObject}' wins over '{conflict.MinorityObject}' (majority {conflict.MajorityCount} vs {conflict.MinorityCount}).",
                        findingId, ct);
                    resolved++;
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "AutoCorrect: continuity resolve failed ({Winner} vs {Loser})", winnerUid, loserUid);
                    notes.Add($"Continuity resolve FAILED for {conflict.EntityName}/{conflict.Predicate}: {ex.Message}");
                }
            }
            if (resolved > 0) findingsSvc.SetStatus(findingId, FindingStatus.Applied);
        }
        return resolved;
    }
}
