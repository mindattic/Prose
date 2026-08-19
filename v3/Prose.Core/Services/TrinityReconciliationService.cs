using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MindAttic.Legion;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services;

/// <summary>
/// Trinity Reconciliation — "autonomous but reversible" resolution of divergences between the
/// three Bible/Book/Entity sources the fact ledger (<see cref="ContinuityService"/>) already
/// detects but only reports. For GLMZ/SCRY/FICTION books (never NONFICTION/GOSPEL — real
/// historical/scriptural content has no "which is coolest" judgment to make), this orchestrator
/// asks an LLM panel to pick which source is right for a given (entity, predicate) divergence,
/// then edits every losing source to match — no per-edit human approval, but every edit is
/// undoable via <see cref="RevertDecisionAsync"/>.
///
/// Pure orchestration over already-shipped services: <see cref="ContinuityService"/> (ledger),
/// <see cref="ContinuityApplyService"/> (entity-record apply + drift check),
/// <see cref="BeatRepairService"/> (prose repair), <see cref="CanonDocumentService"/> (bible
/// section edit), <see cref="BookArchiveService"/> (pre-edit snapshot), <see cref="NodeWorkbenchService"/>
/// (beat text write). Every decision is logged as a permanent <see cref="ReconciliationDecision"/>
/// row — distinct from <c>Findings</c>, which gets purge-and-refiled by every sweep.
/// </summary>
public class TrinityReconciliationService(
    ContinuityService continuityStore,
    ContinuityApplyService continuityApply,
    ContinuityExtractionService extraction,
    BeatRepairService beatRepair,
    CanonDocumentService canonDocs,
    BookArchiveService bookArchive,
    NodeWorkbenchService workbench,
    LlmVotingService voting,
    ILlmService llm,
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<TrinityReconciliationService> log)
{
    /// <summary>The only universes Trinity Reconciliation ever touches. NONFICTION/GOSPEL are real
    /// historical/scriptural content — no editorial "which is more compelling" judgment applies.
    /// HORROR exists (6 nodes) but is deliberately excluded — a one-line addition if ever asked for.</summary>
    private static readonly string[] ScopeUniverseSlugs = ["glmz", "scry", "fiction"];

    public record BookScopeEntry(Guid NodeId, string Slug, string Title, Guid UniverseId);

    /// <summary>Resolves the GLMZ/SCRY/FICTION + NarrativeMode=="original" scope. Pass
    /// <paramref name="slug"/> to resolve one book (by Slug or NodeCode); pass <paramref name="all"/>
    /// to resolve every in-scope book. Universe-slug filtering is the PRIMARY gate — NarrativeMode
    /// alone cannot gate scope (95 NONFICTION + 113 GOSPEL nodes still default to "original").</summary>
    public async Task<List<BookScopeEntry>> ResolveScopeAsync(string? slug, bool all, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(slug) && !all)
            throw new ArgumentException("ResolveScopeAsync requires either a slug or all:true.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var universeIds = new List<Guid>();
        foreach (var u in ScopeUniverseSlugs)
        {
            var id = await canonDocs.ResolveUniverseIdAsync(u, ct);
            if (id != null) universeIds.Add(id.Value);
            else log.LogWarning("[trinity] Universe slug '{Slug}' did not resolve — skipping from scope.", u);
        }

        // IgnoreQueryFilters(): scanning across THREE universes at once, not one ambient scope.
        var query = db.Nodes.AsNoTracking().IgnoreQueryFilters().OfType<BookNode>()
            .Where(n => universeIds.Contains(n.UniverseId) && n.NarrativeMode == "original");

        if (!string.IsNullOrEmpty(slug))
            query = query.Where(n => n.Slug == slug || n.NodeCode == slug);

        return await query
            .Select(n => new BookScopeEntry(n.Id, n.Slug, n.Title, n.UniverseId))
            .ToListAsync(ct);
    }

    // ── Phase 1: extraction sweep ────────────────────────────────────────────

    /// <summary>Extracts prose + bible claims for one book, but ONLY if it has never had any
    /// claims extracted (<see cref="ContinuityService.HasAnyClaimsForBook"/>) — re-running this on
    /// an already-extracted book would just re-confirm existing claims at LLM cost for no new
    /// signal. No voting gate: extraction is a single-LLM analyzer, not a ballot.</summary>
    public async Task<ExtractionSweepEntry> ExtractBookIfNeededAsync(Guid nodeId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");

        if (continuityStore.HasAnyClaimsForBook(node.Slug))
            return new ExtractionSweepEntry(node.Slug, Skipped: true, 0, 0, 0, 0);

        var proseResults = await extraction.ExtractFromBookNodeAsync(nodeId, ct: ct);
        var bibleResult = await extraction.ExtractFromBibleAsync(nodeId, ct: ct);

        var all = proseResults.Append(bibleResult).ToList();
        return new ExtractionSweepEntry(
            node.Slug, Skipped: false,
            ChaptersProcessed: proseResults.Count,
            NewClaims: all.Sum(r => r.NewClaims),
            ConfirmedClaims: all.Sum(r => r.ConfirmedClaims),
            ContradictedClaims: all.Sum(r => r.ContradictedClaims));
    }

    public record ExtractionSweepEntry(string BookSlug, bool Skipped, int ChaptersProcessed, int NewClaims, int ConfirmedClaims, int ContradictedClaims);

    // ── Phase 2: survey checkpoint (read-only, zero DecideAsync calls) ───────

    public record SurveyEntry(
        string BookSlug, int ContradictionGroups, int AppliedDriftFindings,
        int ProseVsBible, int ProseVsEntity, int BibleVsEntity, int OtherPairing,
        int WouldHitBeatRepair);

    /// <summary>Read-only survey: contradiction groups + applied-claim drift for one book, with a
    /// source-pair breakdown so the caller can see how many groups would hit the expensive
    /// prose-repair path vs. the cheap bible/entity paths BEFORE any DecideAsync call is made.</summary>
    public async Task<SurveyEntry> SurveyBookAsync(string bookSlug, CancellationToken ct = default)
    {
        var groups = continuityStore.GetContradictionGroups(bookSlug);
        var drift = (await continuityApply.CheckAppliedClaimsAsync(bookSlug, ct)).Count(d => d.Drifted);

        int proseVsBible = 0, proseVsEntity = 0, bibleVsEntity = 0, other = 0, wouldHitBeatRepair = 0;
        foreach (var g in groups)
        {
            var sources = g.Claims.Select(c => c.SourceType).Distinct().OrderBy(s => s).ToList();
            if (sources.Contains("prose")) wouldHitBeatRepair++;

            var pair = string.Join("+", sources);
            if (sources.Count == 2 && sources.Contains("prose") && sources.Contains("bible")) proseVsBible++;
            else if (sources.Count == 2 && sources.Contains("prose") && sources.Contains("entity_record")) proseVsEntity++;
            else if (sources.Count == 2 && sources.Contains("bible") && sources.Contains("entity_record")) bibleVsEntity++;
            else other++;
        }

        return new SurveyEntry(bookSlug, groups.Count, drift, proseVsBible, proseVsEntity, bibleVsEntity, other, wouldHitBeatRepair);
    }

    // ── Phase 3: reconciliation ───────────────────────────────────────────────

    public record BookReconciliationResult(string BookSlug, Guid NodeId, List<ReconciliationDecision> Decisions);

    /// <summary>Archives the book (pre-edit snapshot) then reconciles every contradiction group and
    /// applied-claim drift finding for it. In dry-run mode, no archive, no DecideAsync, no edits —
    /// every decision row is a plan preview only.</summary>
    public async Task<BookReconciliationResult> ReconcileBookAsync(Guid nodeId, bool dryRun, CancellationToken ct = default)
    {
        await using var db0 = await dbFactory.CreateDbContextAsync(ct);
        var node = await db0.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var bookSlug = node.Slug;

        if (!dryRun)
            await bookArchive.ArchiveAsync(nodeId, "pre-trinity-reconciliation", ct);

        var decisions = new List<ReconciliationDecision>();

        foreach (var group in continuityStore.GetContradictionGroups(bookSlug))
        {
            var row = await ReconcileContradictionGroupAsync(nodeId, bookSlug, group, dryRun, ct);
            if (row != null) decisions.Add(row);
        }

        var drifted = (await continuityApply.CheckAppliedClaimsAsync(bookSlug, ct)).Where(d => d.Drifted);
        foreach (var d in drifted)
        {
            var row = await ReconcileAppliedDriftAsync(nodeId, bookSlug, d, dryRun, ct);
            if (row != null) decisions.Add(row);
        }

        return new BookReconciliationResult(bookSlug, nodeId, decisions);
    }

    private static string SourceTypeToMechanism(string sourceType) => sourceType switch
    {
        "prose"         => "beat_repair",
        "bible"         => "bible_section",
        "entity_record" => "entity_record",
        _               => "unknown",
    };

    /// <summary>Resolves one N-way <see cref="ContradictionGroup"/>: asks the panel to pick a
    /// winning VALUE (not a winning claim — several claims can already share the winning value),
    /// then edits every source whose claim disagrees with the winner. Dry-run makes zero
    /// DecideAsync calls and prints the plan only.</summary>
    public async Task<ReconciliationDecision?> ReconcileContradictionGroupAsync(
        Guid bookNodeId, string bookSlug, ContradictionGroup group, bool dryRun, CancellationToken ct = default)
    {
        var byObject = group.Claims
            .GroupBy(c => c.Object, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        if (byObject.Count < 2) return null; // group invariant violated — nothing to reconcile

        if (dryRun)
        {
            var distinctSources = group.Claims.Select(c => c.SourceType).Distinct().ToList();
            var mechanisms = distinctSources.Select(SourceTypeToMechanism).Distinct().ToList();
            return new ReconciliationDecision
            {
                Id = Guid.NewGuid(),
                BookSlug = bookSlug,
                DivergenceType = "contradiction_group",
                EntityId = group.EntityId,
                EntityName = group.EntityName,
                Predicate = group.Predicate,
                WinningSourceType = "(dry run — not decided)",
                WinningValue = "(dry run — not decided)",
                DecisionReasoning = $"DRY RUN: {byObject.Count} competing values across sources [{string.Join(", ", distinctSources)}]. " +
                    $"Whichever loses the panel vote would be edited via mechanism(s): [{string.Join(", ", mechanisms)}].",
                DecisionConfidence = 0,
                LosingClaimUidsJson = JsonSerializer.Serialize(group.Claims.Select(c => c.ClaimUid)),
                EditMechanism = string.Join(",", mechanisms),
                EditTargetJson = "{}",
                DryRun = true,
            };
        }

        var options = byObject.Select(c => c.Object).ToList();
        var question =
            $"You are the story's continuity editor with full creative authority to pick a winner, not merely " +
            $"a fact-checker: choose the value that makes {group.EntityName} and the story MORE compelling, more " +
            $"internally coherent, and easier for a reader to track for the fact \"{group.Predicate}\" — not simply " +
            $"whichever was asserted first, most often, or most recently.";
        var context = BuildGroupContext(group, byObject);

        var decision = await voting.DecideAsync(question, options, context, Quorum.Plurality, maxTokens: 512, ct: ct);
        var winner = byObject.FirstOrDefault(c => string.Equals(c.Object, decision.Choice, StringComparison.OrdinalIgnoreCase))
            ?? byObject[0];

        var losingClaims = group.Claims
            .Where(c => !string.Equals(c.Object, winner.Object, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var editTargets = new Dictionary<string, List<object>>();
        var editTimestamp = DateTime.UtcNow;

        // prose loses → repair the specific beat(s) that carry the wrong fact.
        foreach (var losing in losingClaims.Where(c => c.SourceType == "prose"))
        {
            var located = await LocateBeatForClaimAsync(losing, ct);
            if (located == null)
            {
                log.LogWarning("[trinity] Could not locate beat for losing prose claim {Uid} ({Entity}.{Predicate}) — leaving prose untouched.",
                    losing.ClaimUid, losing.EntityName, losing.Predicate);
                continue;
            }
            var (beatId, chapterNodeId) = located.Value;
            var issue = new LensIssue(null, "continuity-drift",
                $"Prose says \"{losing.Object}\" for {group.EntityName}.{group.Predicate}, but the reconciled canon value is \"{winner.Object}\".",
                $"Rewrite so {group.EntityName}'s {group.Predicate} reads as \"{winner.Object}\" instead of \"{losing.Object}\".",
                "high");
            var repaired = await beatRepair.RepairAsync(beatId, bookNodeId, [issue], ct: ct);
            if (repaired == null)
            {
                log.LogWarning("[trinity] BeatRepairService refused repair for beat {BeatId} (claim {Uid}) — beat left unchanged.", beatId, losing.ClaimUid);
                continue;
            }
            await workbench.UpdateBeatTextAsync(beatId, repaired, expectedUpdatedAt: null, ct);
            AddEditTarget(editTargets, "beat_repair", new { beatId, chapterNodeId, claimUid = losing.ClaimUid });
        }

        // bible loses → snapshot the section, patch it, write it back.
        string? preEditSnapshotJson = null;
        var patchedSections = new HashSet<string>();
        foreach (var losing in losingClaims.Where(c => c.SourceType == "bible"))
        {
            var sectionType = ParseBibleSectionType(losing.SourcePath);
            if (!patchedSections.Add(sectionType)) continue; // already patched this section this pass

            var sections = await canonDocs.GetNodeBibleSectionsAsync(bookNodeId, ct);
            var section = sections.FirstOrDefault(s => s.SectionType == sectionType);
            if (section == null)
            {
                log.LogWarning("[trinity] No NodeBibleSection '{Section}' found for node {NodeId} — cannot patch bible.", sectionType, bookNodeId);
                continue;
            }

            var snapshot = new { nodeId = bookNodeId, sectionType, content = section.Content };
            preEditSnapshotJson = preEditSnapshotJson == null
                ? JsonSerializer.Serialize(new[] { snapshot })
                : JsonSerializer.Serialize(JsonSerializer.Deserialize<List<object>>(preEditSnapshotJson)!.Append(snapshot));

            var patched = await PatchBibleSectionAsync(section.Content, losing, winner, group, ct);
            await canonDocs.SetNodeBibleSectionAsync(bookNodeId, sectionType, patched, ct);
            AddEditTarget(editTargets, "bible_section", new { nodeId = bookNodeId, sectionType });
        }

        // entity_record loses → apply the WINNING claim's value onto the entity record (same
        // EntityId for every claim in the group, since GetContradictionGroups groups by it).
        if (losingClaims.Any(c => c.SourceType == "entity_record"))
        {
            var applied = await continuityApply.ApplyAsync(winner.ClaimUid, ct);
            if (applied.Ok)
                AddEditTarget(editTargets, "entity_record", new { claimUid = winner.ClaimUid, field = applied.FieldPath });
            else
                log.LogWarning("[trinity] ContinuityApplyService.ApplyAsync failed for winning claim {Uid}: {Error}", winner.ClaimUid, applied.Error);
        }

        continuityStore.MakeCanonical(winner.ClaimUid, decision.Reasoning ?? "Trinity Reconciliation auto-resolved.");

        var row = new ReconciliationDecision
        {
            Id = Guid.NewGuid(),
            BookSlug = bookSlug,
            DivergenceType = "contradiction_group",
            EntityId = group.EntityId,
            EntityName = group.EntityName,
            Predicate = group.Predicate,
            WinningSourceType = winner.SourceType,
            WinningValue = winner.Object,
            DecisionReasoning = decision.Reasoning ?? "",
            DecisionConfidence = decision.Confidence,
            LosingClaimUidsJson = JsonSerializer.Serialize(losingClaims.Select(c => c.ClaimUid)),
            EditMechanism = string.Join(",", editTargets.Keys),
            EditTargetJson = JsonSerializer.Serialize(editTargets),
            PreEditSnapshotJson = preEditSnapshotJson,
            DryRun = false,
            CreatedAt = editTimestamp,
        };

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.ReconciliationDecisions.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    /// <summary>Narrower reconciliation for one already-applied claim whose entity-record field has
    /// since drifted (<see cref="ContinuityApplyService.CheckAppliedClaimsAsync"/>). Only handles
    /// <c>value_changed</c> — the sole drift reason with two comparable values to choose between;
    /// the other reasons (field/entry removed, record missing) describe a deletion, not a competing
    /// authorial fact, so they're logged and left for human review rather than force-fit here.</summary>
    public async Task<ReconciliationDecision?> ReconcileAppliedDriftAsync(
        Guid bookNodeId, string bookSlug, AppliedClaimDriftResult drift, bool dryRun, CancellationToken ct = default)
    {
        if (drift.Reason != "value_changed")
        {
            log.LogInformation(
                "[trinity] Skipping applied-claim drift ({Reason}) for {Entity}.{Predicate} — no comparable current value; needs human review.",
                drift.Reason, drift.Claim.EntityName, drift.Claim.Predicate);
            return null;
        }

        var claim = drift.Claim;
        var field = claim.AppliedToField ?? "";

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var record = await ContinuityApplyService.LocateRecordAsync(db, claim, ct);
        if (record == null) return null;
        var root = JsonNode.Parse(record.Json) as JsonObject;
        var currentValue = root?[field]?.ToString() ?? "";

        if (dryRun)
        {
            return new ReconciliationDecision
            {
                Id = Guid.NewGuid(), BookSlug = bookSlug, DivergenceType = "applied_claim_drift",
                EntityId = claim.EntityId, EntityName = claim.EntityName, Predicate = claim.Predicate,
                WinningSourceType = "(dry run — not decided)", WinningValue = "(dry run — not decided)",
                DecisionReasoning = $"DRY RUN: applied value \"{claim.Object}\" vs current record value \"{currentValue}\" — would ask the panel to pick.",
                LosingClaimUidsJson = JsonSerializer.Serialize(new[] { claim.ClaimUid }),
                EditMechanism = "entity_record", EditTargetJson = "{}", DryRun = true,
            };
        }

        var options = new[] { claim.Object, currentValue };
        var question =
            $"For {claim.EntityName}, the continuity ledger's applied value for \"{claim.Predicate}\" was \"{claim.Object}\", " +
            $"but the entity record now reads \"{currentValue}\". As the story's continuity editor with full creative " +
            $"authority, which value should stand as canon going forward?";
        var context =
            $"Entity: {claim.EntityName} ({claim.EntityKind})\nPredicate: {claim.Predicate}\n" +
            $"Originally applied: \"{claim.Object}\"\nCurrently on record: \"{currentValue}\"\n";

        var editTimestamp = DateTime.UtcNow;
        var decisionResult = await voting.DecideAsync(question, options, context, Quorum.Plurality, maxTokens: 256, ct: ct);
        var appliedWins = string.Equals(decisionResult.Choice, claim.Object, StringComparison.OrdinalIgnoreCase);

        string winningValue;
        if (appliedWins)
        {
            root![field] = claim.Object;
            record.Json = root.ToJsonString();
            record.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            continuityStore.MarkApplied(claim.ClaimUid, field);
            winningValue = claim.Object;
        }
        else
        {
            if (Guid.TryParse(claim.EntityId, out var eid) || Guid.TryParseExact(claim.EntityId, "N", out eid))
                await extraction.ExtractFromEntityRecordAsync(eid, ct: ct);
            continuityStore.RejectClaim(claim.ClaimUid, "Superseded by current entity-record value via Trinity Reconciliation.");
            winningValue = currentValue;
        }

        var row = new ReconciliationDecision
        {
            Id = Guid.NewGuid(),
            BookSlug = bookSlug,
            DivergenceType = "applied_claim_drift",
            EntityId = claim.EntityId,
            EntityName = claim.EntityName,
            Predicate = claim.Predicate,
            WinningSourceType = "entity_record",
            WinningValue = winningValue,
            DecisionReasoning = decisionResult.Reasoning ?? "",
            DecisionConfidence = decisionResult.Confidence,
            LosingClaimUidsJson = JsonSerializer.Serialize(new[] { claim.ClaimUid }),
            EditMechanism = "entity_record",
            EditTargetJson = JsonSerializer.Serialize(new { claimUid = claim.ClaimUid, field }),
            DryRun = false,
            CreatedAt = editTimestamp,
        };
        db.ReconciliationDecisions.Add(row);
        await db.SaveChangesAsync(ct);
        return row;
    }

    // ── Revert ────────────────────────────────────────────────────────────────

    /// <summary>Undoes one decision's edit(s) and flips the ledger side back. A prose/entity edit
    /// restores from its own table's temporal history (<c>FOR SYSTEM_TIME AS OF</c> just before the
    /// edit); a bible edit restores from <see cref="ReconciliationDecision.PreEditSnapshotJson"/>
    /// directly, since <c>NodeBibleSections</c> is not a system-versioned table.</summary>
    public async Task<bool> RevertDecisionAsync(Guid decisionId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var row = await db.ReconciliationDecisions.FirstOrDefaultAsync(d => d.Id == decisionId, ct)
            ?? throw new InvalidOperationException($"ReconciliationDecision {decisionId} not found.");
        if (row.Reverted) return false;
        if (row.DryRun) throw new InvalidOperationException("Cannot revert a dry-run decision — no edit was ever made.");

        var asOf = row.CreatedAt.AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:ss.fffffff");

        foreach (var mechanism in row.EditMechanism.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            switch (mechanism)
            {
                case "beat_repair":
                    await RevertBeatRepairAsync(db, row, asOf, ct);
                    break;
                case "bible_section":
                    await RevertBibleSectionAsync(db, row, ct);
                    break;
                case "entity_record":
                    await RevertEntityRecordAsync(db, row, asOf, ct);
                    break;
                default:
                    log.LogWarning("[trinity] Unknown EditMechanism '{Mechanism}' on decision {Id} — skipping that segment of the revert.", mechanism, row.Id);
                    break;
            }
        }

        // Flip the ledger side back: the winning claim comes off CANONICAL, and every losing claim
        // is reset to NEW so it re-surfaces for triage. Losers' exact PRE-resolution status is
        // recoverable from ContinuityClaims_History (also temporal) if ever needed; NEW is the
        // honest "unresolved again" state rather than silently hiding them.
        var losingUids = JsonSerializer.Deserialize<List<string>>(row.LosingClaimUidsJson) ?? new();
        var winnerClaim = await db.ContinuityClaims.FirstOrDefaultAsync(c => c.EntityId == row.EntityId && c.Predicate == row.Predicate && c.Object == row.WinningValue, ct);
        if (winnerClaim != null && winnerClaim.Status == "CANONICAL")
        {
            winnerClaim.Status = "NEW";
            winnerClaim.ResolvedAt = null;
        }
        foreach (var uid in losingUids)
        {
            var loser = await db.ContinuityClaims.FirstOrDefaultAsync(c => c.ClaimUid == uid, ct);
            if (loser == null) continue;
            loser.Status = "NEW";
            loser.SupersededBy = null;
            loser.ResolvedAt = null;
        }

        row.Reverted = true;
        row.RevertedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task RevertBeatRepairAsync(ProseDbContext db, ReconciliationDecision row, string asOf, CancellationToken ct)
    {
        var targets = ExtractEditTargets(row.EditTargetJson, "beat_repair");
        foreach (var t in targets)
        {
            if (!t.TryGetValue("beatId", out var beatIdRaw) || !Guid.TryParse(beatIdRaw?.ToString(), out var beatId)) continue;
            var priorText = db.Database.SqlQueryRaw<string>(
                $"SELECT [Text] FROM [dbo].[Beats] FOR SYSTEM_TIME AS OF '{asOf}' WHERE [Id] = @p0", beatId.ToString())
                .AsEnumerable().FirstOrDefault();
            if (priorText == null)
            {
                log.LogWarning("[trinity] No Beats_History row for beat {BeatId} as of {AsOf} — cannot revert this beat.", beatId, asOf);
                continue;
            }
            await workbench.UpdateBeatTextAsync(beatId, priorText, expectedUpdatedAt: null, ct);
        }
    }

    private async Task RevertBibleSectionAsync(ProseDbContext db, ReconciliationDecision row, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(row.PreEditSnapshotJson)) return;
        var snapshots = JsonSerializer.Deserialize<List<JsonElement>>(row.PreEditSnapshotJson) ?? new();
        foreach (var snap in snapshots)
        {
            var nodeId = Guid.Parse(snap.GetProperty("nodeId").GetString()!);
            var sectionType = snap.GetProperty("sectionType").GetString()!;
            var content = snap.GetProperty("content").GetString() ?? "";
            await canonDocs.SetNodeBibleSectionAsync(nodeId, sectionType, content, ct);
        }
    }

    private async Task RevertEntityRecordAsync(ProseDbContext db, ReconciliationDecision row, string asOf, CancellationToken ct)
    {
        var targets = ExtractEditTargets(row.EditTargetJson, "entity_record");
        foreach (var t in targets)
        {
            if (!t.TryGetValue("claimUid", out var uidRaw)) continue;
            var claim = await db.ContinuityClaims.AsNoTracking().FirstOrDefaultAsync(c => c.ClaimUid == uidRaw!.ToString(), ct);
            if (claim == null) continue;
            var record = await ContinuityApplyService.LocateRecordAsync(db, claim, ct);
            if (record == null) continue;

            var priorJson = db.Database.SqlQueryRaw<string>(
                $"SELECT [Json] FROM [dbo].[Records] FOR SYSTEM_TIME AS OF '{asOf}' WHERE [EntityId] = @p0", record.EntityId.ToString())
                .AsEnumerable().FirstOrDefault();
            if (priorJson == null)
            {
                log.LogWarning("[trinity] No Records_History row for entity {EntityId} as of {AsOf} — cannot revert this record.", record.EntityId, asOf);
                continue;
            }
            var live = await db.Records.FirstOrDefaultAsync(r => r.EntityId == record.EntityId, ct);
            if (live == null) continue;
            live.Json = priorJson;
            live.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    private static List<Dictionary<string, object?>> ExtractEditTargets(string editTargetJson, string mechanism)
    {
        try
        {
            var doc = JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, object?>>>>(editTargetJson);
            return doc != null && doc.TryGetValue(mechanism, out var list) ? list : new();
        }
        catch { return new(); }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static void AddEditTarget(Dictionary<string, List<object>> targets, string mechanism, object target)
    {
        if (!targets.TryGetValue(mechanism, out var list)) targets[mechanism] = list = new();
        list.Add(target);
    }

    private static string ParseBibleSectionType(string? sourcePath)
    {
        const string prefix = "bible-section:";
        if (!string.IsNullOrEmpty(sourcePath) && sourcePath.StartsWith(prefix, StringComparison.Ordinal))
            return sourcePath[prefix.Length..];
        return "Characters"; // ExtractFromBibleAsync's own default section
    }

    /// <summary>Finds which beat under a chapter-scoped continuity claim's <c>SourceChapterId</c>
    /// actually contains its <c>Snippet</c> — <see cref="BeatRepairService.RepairAsync"/> needs one
    /// specific beat id, but prose claims are extracted per-chapter (all beats concatenated), so
    /// there is no existing lookup from claim → beat before this.</summary>
    internal async Task<(Guid beatId, Guid chapterNodeId)?> LocateBeatForClaimAsync(ContinuityClaim claim, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(claim.SourceChapterId) || !Guid.TryParse(claim.SourceChapterId, out var chapterNodeId))
            return null;
        if (string.IsNullOrEmpty(claim.Snippet)) return null;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var beats = await db.BeatNodes.AsNoTracking()
            .Where(bn => bn.NodeId == chapterNodeId)
            .OrderBy(bn => bn.SortKey)
            .Join(db.Beats, bn => bn.BeatId, b => b.Id, (bn, b) => b)
            .ToListAsync(ct);

        foreach (var beat in beats)
        {
            var stripped = BeatMarkup.StripEntityTags(beat.Text);
            if (!string.IsNullOrEmpty(stripped) &&
                (stripped.Contains(claim.Snippet, StringComparison.Ordinal) || stripped.Contains(claim.Snippet, StringComparison.OrdinalIgnoreCase)))
                return (beat.Id, chapterNodeId);
        }
        return null;
    }

    /// <summary>Single-call LLM patch: rewrite the bible section's text so the flagged snippet's
    /// fact matches the winning value, changing nothing else. Grounded on the section's own current
    /// content (same discipline as <see cref="ContinuityExtractionService"/>'s snippet-quote
    /// extraction) rather than a free rewrite.</summary>
    private async Task<string> PatchBibleSectionAsync(string sectionContent, ContinuityClaim losingClaim, ContinuityClaim winner, ContradictionGroup group, CancellationToken ct)
    {
        var question =
            "Rewrite the section below so it reflects the corrected fact, changing NOTHING else — same structure, " +
            "same voice, same every other sentence. Output ONLY the full corrected section text, no commentary.";
        var context =
            $"CORRECTION: {group.EntityName}'s \"{group.Predicate}\" should read as \"{winner.Object}\", not \"{losingClaim.Object}\".\n" +
            $"The section currently asserts the wrong value in a passage containing this exact text: \"{losingClaim.Snippet}\"\n\n" +
            $"=== SECTION TEXT ===\n{sectionContent}";
        var patched = await llm.GenerateAsync(question, context, temperature: 0.1, maxTokens: Math.Max(2048, sectionContent.Length / 2), ct: ct);
        return string.IsNullOrWhiteSpace(patched) ? sectionContent : patched.Trim();
    }

    private static string BuildGroupContext(ContradictionGroup group, List<ContinuityClaim> byObject)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Entity: {group.EntityName} ({group.EntityKind})");
        sb.AppendLine($"Predicate: {group.Predicate}");
        sb.AppendLine();
        foreach (var c in byObject)
        {
            sb.AppendLine($"Option: \"{c.Object}\"");
            sb.AppendLine($"  Source: {SourceLabel(c)}");
            if (!string.IsNullOrEmpty(c.Snippet)) sb.AppendLine($"  Snippet: \"{c.Snippet}\"");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string SourceLabel(ContinuityClaim c) => c.SourceType switch
    {
        "bible"         => "the story bible — authorial intent",
        "prose"         => $"the prose (ch.{c.SourceChapterNumber} {c.SourceChapterTitle}) — what actually happened on the page, the reader's lived experience",
        "entity_record" => "the entity's structured canon sheet — what other tools read as ground truth",
        _               => c.SourceType,
    };
}
