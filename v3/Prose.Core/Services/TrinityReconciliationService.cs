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
/// <see cref="CanonDocumentService"/> (bible section edit), <see cref="BookArchiveService"/>
/// (pre-edit snapshot), <see cref="NodeWorkbenchService"/> (beat text write — also used for the
/// surgical single-paragraph prose patch, see <see cref="PatchBeatAsync"/>). Every decision is
/// logged as a permanent <see cref="ReconciliationDecision"/> row — distinct from <c>Findings</c>,
/// which gets purge-and-refiled by every sweep.
/// </summary>
public class TrinityReconciliationService(
    ContinuityService continuityStore,
    ContinuityApplyService continuityApply,
    ContinuityExtractionService extraction,
    CanonDocumentService canonDocs,
    BookArchiveService bookArchive,
    NodeWorkbenchService workbench,
    LlmVotingService voting,
    ILlmService llm,
    IDbContextFactory<ProseDbContext> dbFactory,
    ILogger<TrinityReconciliationService> log,
    ContinuityCompatibilityService? compatibility = null)
{
    /// <summary>Falls back to a same-process instance (no test double registered) so existing
    /// call sites/tests built before this filter existed keep compiling and running unchanged;
    /// production DI always supplies the real singleton.</summary>
    private ContinuityCompatibilityService Compatibility => compatibility ??=
        new ContinuityCompatibilityService(continuityStore, llm, dbFactory, Microsoft.Extensions.Logging.Abstractions.NullLogger<ContinuityCompatibilityService>.Instance);

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

    /// <param name="WouldHitBeatRepair">Despite the name, counts groups with a losing prose claim
    /// that would attempt the surgical <c>beat_patch</c> mechanism (a single-paragraph edit) — NOT
    /// the full-beat-regeneration <see cref="BeatRepairService"/> path the name originally
    /// referred to before that path was replaced as the prose-losing default (2026-08-19).</param>
    public record SurveyEntry(
        string BookSlug, int ContradictionGroups, int AppliedDriftFindings,
        int ProseVsBible, int ProseVsEntity, int BibleVsEntity, int OtherPairing,
        int WouldHitBeatRepair);

    /// <summary>Read-only survey: contradiction groups + applied-claim drift for one book, with a
    /// source-pair breakdown so the caller can see how many groups have a losing prose claim
    /// (would attempt the surgical <c>beat_patch</c> mechanism) vs. the bible/entity paths BEFORE
    /// any DecideAsync call is made.</summary>
    public async Task<SurveyEntry> SurveyBookAsync(string bookSlug, CancellationToken ct = default)
    {
        var groups = await Compatibility.GetGenuineContradictionGroupsAsync(bookSlug, ct);
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
    /// every decision row is a plan preview only.
    /// <paramref name="onlyEntityId"/>/<paramref name="onlyPredicate"/>, when both set, restrict this
    /// call to the ONE matching contradiction group and skip the applied-drift loop entirely — the
    /// narrow-pilot safety valve used to prove the mechanism on a single hand-picked divergence
    /// without touching every other divergence in the same book.</summary>
    public async Task<BookReconciliationResult> ReconcileBookAsync(
        Guid nodeId, bool dryRun, CancellationToken ct = default, string? onlyEntityId = null, string? onlyPredicate = null,
        string triggeredBy = "cli-manual")
    {
        await using var db0 = await dbFactory.CreateDbContextAsync(ct);
        var node = await db0.Nodes.AsNoTracking().IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == nodeId, ct)
            ?? throw new InvalidOperationException($"Node {nodeId} not found.");
        var bookSlug = node.Slug;

        var singleGroupOnly = !string.IsNullOrEmpty(onlyEntityId) && !string.IsNullOrEmpty(onlyPredicate);

        if (!dryRun)
            await bookArchive.ArchiveAsync(nodeId, "pre-trinity-reconciliation", ct);

        var decisions = new List<ReconciliationDecision>();

        // --only-entity/--only-predicate is a deliberate, hand-picked target — the operator named
        // this exact group, so the genuine-vs-restatement filter (which governs auto-discovery)
        // does not apply here; go straight to the raw group. Auto-discovered runs (the --all/
        // --slug-without-narrowing path, and the scheduled auto-reconcile) DO apply it — a group
        // that's just a different-granularity restatement of the same fact (found live 2026-08-19/20
        // to be the majority case) never reaches a panel vote or an edit attempt there.
        var groups = singleGroupOnly
            ? continuityStore.GetContradictionGroups(bookSlug).Where(g => g.EntityId == onlyEntityId && g.Predicate == onlyPredicate).ToList()
            : await Compatibility.GetGenuineContradictionGroupsAsync(bookSlug, ct);

        foreach (var group in groups)
        {
            var row = await ReconcileContradictionGroupAsync(nodeId, bookSlug, group, dryRun, ct, triggeredBy);
            if (row != null) decisions.Add(row);
        }

        if (!singleGroupOnly)
        {
            var drifted = (await continuityApply.CheckAppliedClaimsAsync(bookSlug, ct)).Where(d => d.Drifted);
            foreach (var d in drifted)
            {
                var row = await ReconcileAppliedDriftAsync(nodeId, bookSlug, d, dryRun, ct, triggeredBy);
                if (row != null) decisions.Add(row);
            }
        }

        return new BookReconciliationResult(bookSlug, nodeId, decisions);
    }

    private static string SourceTypeToMechanism(string sourceType) => sourceType switch
    {
        "prose"         => "beat_patch",
        "bible"         => "bible_section",
        "entity_record" => "entity_record",
        _               => "unknown",
    };

    /// <summary>Resolves one N-way <see cref="ContradictionGroup"/>: asks the panel to pick a
    /// winning VALUE (not a winning claim — several claims can already share the winning value),
    /// then edits every source whose claim disagrees with the winner. Dry-run makes zero
    /// DecideAsync calls and prints the plan only.</summary>
    public async Task<ReconciliationDecision?> ReconcileContradictionGroupAsync(
        Guid bookNodeId, string bookSlug, ContradictionGroup group, bool dryRun, CancellationToken ct = default,
        string triggeredBy = "cli-manual")
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
                TriggeredBy = triggeredBy,
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
        // Only claims in here get rejected below — a losing claim whose edit was refused stays at
        // its current live status so it resurfaces on the next pass instead of being silently
        // marked resolved while its source still carries the wrong fact.
        var resolvedLosingClaimUids = new HashSet<string>();

        // prose loses → surgically patch the one paragraph that carries the wrong fact.
        foreach (var losing in losingClaims.Where(c => c.SourceType == "prose"))
        {
            var located = await LocateBeatForClaimAsync(losing, ct);
            if (located == null)
            {
                // A claim with no BookSlug can only have come from the legacy pre-SS-A43
                // ExtractFromBookAsync/ExtractFromChapterAsync path (IBookRepository/IChapterRepository
                // model), which stamps SourceChapterId from that repo's own id scheme — never a real
                // Nodes.Id — and hardcodes BookSlug: null. Such a claim can never resolve here no
                // matter how many times Trinity runs; leaving it at NEW just makes it resurface
                // identically on every future pass forever. Retire it now instead of re-discovering
                // the same permanent dead end each time (found live 2026-08-19: 3 of 19 zero-mechanism
                // decisions were exactly this, all bushido_coda-family).
                if (string.IsNullOrEmpty(losing.BookSlug))
                {
                    continuityStore.RejectClaim(losing.ClaimUid,
                        "Trinity Reconciliation: permanently unlocatable — claim has no BookSlug, indicating " +
                        "extraction via the legacy chapter-repo path whose SourceChapterId cannot resolve to a " +
                        "modern Nodes row. Retired rather than re-surfaced every pass.");
                    log.LogWarning("[trinity] Losing prose claim {Uid} ({Entity}.{Predicate}) has no BookSlug and cannot be located — " +
                        "retired as permanently unresolvable (legacy extraction artifact), not left to resurface.",
                        losing.ClaimUid, losing.EntityName, losing.Predicate);
                }
                else
                    log.LogWarning("[trinity] Could not locate beat for losing prose claim {Uid} ({Entity}.{Predicate}) — leaving prose untouched.",
                        losing.ClaimUid, losing.EntityName, losing.Predicate);
                continue;
            }
            var (beatId, chapterNodeId) = located.Value;

            // Fresh, uncached fetch on every iteration — required, not just style: if two losing
            // prose claims in this pass land on the same beat, the second must see the first
            // patch's result. A hoisted/cached load here would search a stale copy.
            await using var beatDb = await dbFactory.CreateDbContextAsync(ct);
            var currentBeat = await beatDb.Beats.AsNoTracking().FirstOrDefaultAsync(b => b.Id == beatId, ct);
            if (string.IsNullOrEmpty(currentBeat?.Text))
            {
                log.LogWarning("[trinity] Beat {BeatId} missing/empty when attempting surgical patch (claim {Uid}) — leaving prose untouched.", beatId, losing.ClaimUid);
                continue;
            }

            var patched = await PatchBeatAsync(currentBeat.Text, losing, winner, group, ct);
            if (patched == null)
            {
                log.LogWarning(
                    "[trinity] Surgical beat patch refused for beat {BeatId} (claim {Uid}) — snippet not found verbatim or guard rejected the rewrite; " +
                    "beat left unchanged (no automatic fall back to full-beat regeneration).", beatId, losing.ClaimUid);
                continue;
            }

            await workbench.UpdateBeatTextAsync(beatId, patched, expectedUpdatedAt: null, ct);
            AddEditTarget(editTargets, "beat_patch", new { beatId, chapterNodeId, claimUid = losing.ClaimUid });
            resolvedLosingClaimUids.Add(losing.ClaimUid);
        }

        // bible loses → snapshot the section, patch it, write it back.
        string? preEditSnapshotJson = null;
        var patchedSections = new HashSet<(Guid NodeId, string SectionType)>();
        foreach (var losing in losingClaims.Where(c => c.SourceType == "bible"))
        {
            var sectionType = ParseBibleSectionType(losing.SourcePath);

            // GetContradictionGroups groups purely by (EntityId, Predicate), not by book — a
            // crossover character asserted in two books (e.g. Auda Vane in both high-five and
            // the-fall-down) can put a losing claim from a DIFFERENT book than the one currently
            // being reconciled into this same group. Using the outer bookNodeId here would patch
            // the wrong book's bible and always refuse (snippet never present there) — found live
            // 2026-08-19: 4 of 19 zero-mechanism decisions were exactly this. Resolve the node the
            // losing claim actually belongs to instead.
            var targetNodeId = await ResolveClaimBookNodeIdAsync(losing.BookSlug, bookSlug, bookNodeId, ct);
            if (targetNodeId == null)
            {
                log.LogWarning("[trinity] Losing bible claim {Uid} belongs to book '{Book}', which could not be resolved to a node — cannot patch bible.",
                    losing.ClaimUid, losing.BookSlug);
                continue;
            }

            if (!patchedSections.Add((targetNodeId.Value, sectionType))) continue; // already patched this section this pass

            var sections = await canonDocs.GetNodeBibleSectionsAsync(targetNodeId.Value, ct);
            var section = sections.FirstOrDefault(s => s.SectionType == sectionType);
            if (section == null)
            {
                log.LogWarning("[trinity] No NodeBibleSection '{Section}' found for node {NodeId} — cannot patch bible.", sectionType, targetNodeId.Value);
                continue;
            }

            var patched = await PatchBibleSectionAsync(section.Content, losing, winner, group, ct);
            if (patched == null)
            {
                log.LogWarning(
                    "[trinity] Losing bible claim {Uid}'s snippet is no longer present verbatim in section '{Section}' " +
                    "(node {NodeId}) — refusing to guess which line to patch; bible left untouched.",
                    losing.ClaimUid, sectionType, targetNodeId.Value);
                continue;
            }

            var snapshot = new { nodeId = targetNodeId.Value, sectionType, content = section.Content };
            preEditSnapshotJson = preEditSnapshotJson == null
                ? JsonSerializer.Serialize(new[] { snapshot })
                : JsonSerializer.Serialize(JsonSerializer.Deserialize<List<object>>(preEditSnapshotJson)!.Append(snapshot));

            await canonDocs.SetNodeBibleSectionAsync(targetNodeId.Value, sectionType, patched, ct);
            AddEditTarget(editTargets, "bible_section", new { nodeId = targetNodeId.Value, sectionType });
            resolvedLosingClaimUids.Add(losing.ClaimUid);
        }

        // entity_record loses → apply the WINNING claim's value onto the entity record (same
        // EntityId for every claim in the group, since GetContradictionGroups groups by it).
        var losingEntityRecordClaims = losingClaims.Where(c => c.SourceType == "entity_record").ToList();
        if (losingEntityRecordClaims.Count > 0)
        {
            var applied = await continuityApply.ApplyAsync(winner.ClaimUid, ct);
            if (applied.Ok)
            {
                AddEditTarget(editTargets, "entity_record", new { claimUid = winner.ClaimUid, field = applied.FieldPath });
                foreach (var losing in losingEntityRecordClaims) resolvedLosingClaimUids.Add(losing.ClaimUid);
            }
            else
                log.LogWarning("[trinity] ContinuityApplyService.ApplyAsync failed for winning claim {Uid}: {Error}", winner.ClaimUid, applied.Error);
        }

        if (resolvedLosingClaimUids.Count < losingClaims.Count)
            log.LogWarning(
                "[trinity] {Unresolved}/{Total} losing claim(s) for {Entity}.{Predicate} could not be edited this pass — " +
                "left at their current live status so they resurface on the next reconciliation instead of being marked resolved.",
                losingClaims.Count - resolvedLosingClaimUids.Count, losingClaims.Count, group.EntityName, group.Predicate);

        continuityStore.MakeCanonical(winner.ClaimUid, decision.Reasoning ?? "Trinity Reconciliation auto-resolved.", resolvedLosingClaimUids);

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
            TriggeredBy = triggeredBy,
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
        Guid bookNodeId, string bookSlug, AppliedClaimDriftResult drift, bool dryRun, CancellationToken ct = default,
        string triggeredBy = "cli-manual")
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
                TriggeredBy = triggeredBy,
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
            TriggeredBy = triggeredBy,
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
                    await RevertBeatTextEditAsync(db, row, "beat_repair", asOf, ct);
                    break;
                case "beat_patch":
                    await RevertBeatTextEditAsync(db, row, "beat_patch", asOf, ct);
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

    /// <summary>Restores <c>Beats.Text</c> from its <c>FOR SYSTEM_TIME AS OF</c> temporal snapshot,
    /// keyed only on <c>beatId</c> — identical mechanics regardless of which mechanism
    /// (<c>beat_repair</c>'s full regen or <c>beat_patch</c>'s surgical single-paragraph edit)
    /// produced the edit being undone.</summary>
    private async Task RevertBeatTextEditAsync(ProseDbContext db, ReconciliationDecision row, string mechanism, string asOf, CancellationToken ct)
    {
        var targets = ExtractEditTargets(row.EditTargetJson, mechanism);
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

    /// <summary>Resolves which BookNode a losing bible claim's edit should target. Most of the
    /// time the claim's own <see cref="ContinuityClaim.BookSlug"/> matches the book currently being
    /// reconciled and this is a no-op lookup; it only diverges for a crossover-entity contradiction
    /// group spanning two books (see the caller's remarks). Returns null if the claim's book can't
    /// be resolved to a live node at all (e.g. the orphaned pre-SS-A43 legacy-extraction claims that
    /// were never tagged with a BookSlug in the first place).</summary>
    internal async Task<Guid?> ResolveClaimBookNodeIdAsync(string? claimBookSlug, string currentBookSlug, Guid currentBookNodeId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(claimBookSlug) || string.Equals(claimBookSlug, currentBookSlug, StringComparison.OrdinalIgnoreCase))
            return currentBookNodeId;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var node = await db.Nodes.AsNoTracking().IgnoreQueryFilters()
            .FirstOrDefaultAsync(n => n.Slug == claimBookSlug || n.NodeCode == claimBookSlug, ct);
        return node?.Id;
    }

    private static string ParseBibleSectionType(string? sourcePath)
    {
        const string sectionPrefix = "bible-section:";
        if (!string.IsNullOrEmpty(sourcePath) && sourcePath.StartsWith(sectionPrefix, StringComparison.Ordinal))
            return sourcePath[sectionPrefix.Length..];
        // "bible-full:fallback" (ExtractFromBibleAsync's fallback when no typed NodeBibleSection row
        // exists yet) extracted from the raw Nodes.NodeBible blob — CanonDocumentService.
        // SetNodeBibleSectionAsync's "Full" sectionType is the one that writes back to that same
        // blob, so that's the section to patch, not the "Characters" default (found live 2026-08-19:
        // patching "Characters" here silently no-opped for every book whose bible predates a typed
        // Characters section, since GetNodeBibleSectionsAsync never has one to match against).
        if (string.Equals(sourcePath, "bible-full:fallback", StringComparison.Ordinal))
            return "Full";
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

    /// <summary>Surgical single-LINE patch: locates the exact line containing the losing claim's
    /// <c>Snippet</c> (exact-substring grounded, same discipline as
    /// <see cref="ContinuityExtractionService"/>'s snippet-quote extraction), asks the LLM to
    /// rewrite ONLY that one line, then replaces it via a plain string swap. Returns null when the
    /// snippet is no longer present verbatim — refuse rather than guess which passage to touch.
    ///
    /// Replaces an earlier whole-section-rewrite approach that handed the LLM the entire section
    /// and trusted it to reproduce everything else byte-for-byte except the flagged fact: proven
    /// unsafe live 2026-08-19 on the very first hand-picked-divergence proof run — it rewrote an
    /// UNRELATED line about a different character ("Ren's unregistered status") into the corrected
    /// fact instead of touching the line that actually asserted the wrong value, because the
    /// section-wide prompt gave the model too much surface to misattribute the fix to. Scoping the
    /// LLM call to exactly one already-located line removes that failure mode structurally: the
    /// call can only ever change the one line handed to it, and a plain <see cref="string.Replace"/>
    /// on the ORIGINAL line text (not on positional line index) confirms that exact line is what's
    /// swapped, immune to any reflow the LLM's response introduces.</summary>
    /// <summary>Rejects a <see cref="PatchBeatAsync"/> rewrite whose length moved too far from the
    /// original paragraph's. Upper bound is 2x-or-+200 chars (not the 3x a naive port of
    /// <see cref="BeatRepairService.IsUnsafeShrink"/> might suggest) — the incident that motivated
    /// this whole mechanism was a 3.68x whole-beat blowup, so 2x leaves real margin even at
    /// paragraph scale, where corpus paragraphs already run up to ~2,700 chars and thus have more
    /// surface to drift on than a bible bullet line. Lower bound (0.4x) only applies above 20
    /// chars — short dialogue lines ("Don't.") can legitimately swing far past that ratio on any
    /// real single-fact edit without being unsafe.</summary>
    internal static bool IsUnsafeLinePatch(int oldLength, int newLength) =>
        newLength > Math.Max(oldLength * 2, oldLength + 200)
        || (oldLength >= 20 && newLength < oldLength * 0.4);

    /// <summary>Surgical single-PARAGRAPH patch for the prose-losing case — the direct mirror of
    /// <see cref="PatchBibleSectionAsync"/> below, applied to beat text instead of a bible section.
    /// Confirmed against the live corpus that beats store exactly one paragraph per non-empty
    /// <c>\n</c>-delimited line, so line-granularity is paragraph-granularity here, not an
    /// arbitrary split. Operates on <see cref="BeatMarkup.StripEntityTags"/>-stripped plain text
    /// and returns plain text — <see cref="NodeWorkbenchService.UpdateBeatTextAsync"/> strips and
    /// re-tags from its own candidate index on every write regardless of what it's handed, so this
    /// method must not (and does not need to) do any tagging of its own.
    ///
    /// Replaces the earlier default of routing the whole beat through
    /// <see cref="BeatRepairService.RepairAsync"/> (a full <see cref="ProseWriterRouter"/>
    /// regeneration) for every prose-losing claim: proven unsafe live 2026-08-19 on the very first
    /// hand-picked-divergence proof run — it silently replaced a 2,848-char beat with an unrelated
    /// 10,482-char invented scene, dropping the fact it was meant to fix. Scoping the LLM call to
    /// exactly one already-located paragraph removes that failure mode the same way
    /// <see cref="PatchBibleSectionAsync"/> already removed it for the bible-losing case.</summary>
    private async Task<string?> PatchBeatAsync(string beatText, ContinuityClaim losingClaim, ContinuityClaim winner, ContradictionGroup group, CancellationToken ct)
    {
        var snippet = losingClaim.Snippet ?? "";
        if (string.IsNullOrEmpty(snippet)) return null;

        var plainText = BeatMarkup.StripEntityTags(beatText);
        var lines = plainText.Split('\n');
        var lineIndex = Array.FindIndex(lines, l =>
            l.Contains(snippet, StringComparison.Ordinal) || l.Contains(snippet, StringComparison.OrdinalIgnoreCase));
        if (lineIndex < 0)
        {
            // LocateBeatForClaimAsync already confirmed the snippet is a substring of this same
            // stripped beat text, so reaching here means the snippet spans a paragraph break (two
            // \n-delimited lines) rather than living wholly inside one — a real, if rare, case
            // distinct from "snippet not present at all."
            log.LogWarning("[trinity] PatchBeatAsync refused for claim {Uid}: snippet is present in the beat but spans a paragraph break — no single line contains it whole.", losingClaim.ClaimUid);
            return null;
        }

        var oldLine = lines[lineIndex];
        var question =
            $"Rewrite ONLY this one paragraph so {group.EntityName}'s \"{group.Predicate}\" reads as \"{winner.Object}\" " +
            $"instead of \"{losingClaim.Object}\", changing nothing else about the paragraph's structure, punctuation, or voice. " +
            "Output ONLY the corrected paragraph — no commentary, no surrounding quotes.";
        var context = $"PARAGRAPH TO CORRECT:\n{oldLine}";
        var newLine = await llm.GenerateAsync(question, context, temperature: 0.1, maxTokens: 2048, ct: ct);
        if (string.IsNullOrWhiteSpace(newLine))
        {
            log.LogWarning("[trinity] PatchBeatAsync refused for claim {Uid}: LLM returned empty/whitespace.", losingClaim.ClaimUid);
            return null;
        }
        newLine = newLine.Trim().Trim('"');

        if (newLine.Contains('\n') || newLine.Contains('\r'))
        {
            log.LogWarning("[trinity] PatchBeatAsync refused for claim {Uid}: LLM output was multi-line — would corrupt the line-array rejoin.", losingClaim.ClaimUid);
            return null;
        }
        if (string.Equals(newLine, oldLine.Trim(), StringComparison.Ordinal))
        {
            log.LogWarning("[trinity] PatchBeatAsync refused for claim {Uid}: LLM's rewrite was a no-op (identical to the original paragraph).", losingClaim.ClaimUid);
            return null;
        }
        if (IsUnsafeLinePatch(oldLine.Length, newLine.Length))
        {
            log.LogWarning("[trinity] PatchBeatAsync refused for claim {Uid}: IsUnsafeLinePatch guard rejected the length swing ({OldLen} → {NewLen} chars).",
                losingClaim.ClaimUid, oldLine.Length, newLine.Length);
            return null;
        }

        lines[lineIndex] = newLine;
        return string.Join('\n', lines);
    }

    /// <summary>Strips markdown bold/backtick decoration (<c>**</c>, <c>`</c>) so bible-line
    /// matching survives a bible regenerating its character-bullet formatting (e.g. <c>Name (slug)
    /// - desc</c> → <c>**Name** (`slug`) - desc</c>) without the underlying fact changing. Found
    /// live 2026-08-19: 5 losing bible claims (Ruslan Adeyinka, Breckenridge, Ferko Nzambe, Auda
    /// Vane, Coeli Vantanen) all refused with "snippet no longer present verbatim" purely because
    /// of this decoration, not because the asserted fact had actually changed. Only ever makes the
    /// match MORE permissive than a raw <see cref="string.Contains(string)"/> — never a new false
    /// refusal.</summary>
    internal static string StripMarkdownDecoration(string s) => s.Replace("**", "").Replace("`", "");

    private async Task<string?> PatchBibleSectionAsync(string sectionContent, ContinuityClaim losingClaim, ContinuityClaim winner, ContradictionGroup group, CancellationToken ct)
    {
        var snippet = losingClaim.Snippet ?? "";
        if (string.IsNullOrEmpty(snippet)) return null;

        var lines = sectionContent.Split('\n');
        var normalizedSnippet = StripMarkdownDecoration(snippet);
        var lineIndex = Array.FindIndex(lines, l =>
        {
            var normalizedLine = StripMarkdownDecoration(l);
            return normalizedLine.Contains(normalizedSnippet, StringComparison.Ordinal) || normalizedLine.Contains(normalizedSnippet, StringComparison.OrdinalIgnoreCase);
        });
        if (lineIndex < 0) return null;

        var oldLine = lines[lineIndex];
        var question =
            $"Rewrite ONLY this one line so {group.EntityName}'s \"{group.Predicate}\" reads as \"{winner.Object}\" " +
            $"instead of \"{losingClaim.Object}\", changing nothing else about the line's structure, punctuation, or voice. " +
            "Output ONLY the corrected line — no commentary, no surrounding quotes.";
        var context = $"LINE TO CORRECT:\n{oldLine}";
        var newLine = await llm.GenerateAsync(question, context, temperature: 0.1, maxTokens: 512, ct: ct);
        if (string.IsNullOrWhiteSpace(newLine)) return null;
        newLine = newLine.Trim().Trim('"');

        lines[lineIndex] = newLine;
        return string.Join('\n', lines);
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
