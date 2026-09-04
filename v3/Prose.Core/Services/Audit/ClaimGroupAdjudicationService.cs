using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Prose.Core.Data;
using Prose.Core.Data.Entities;
using Prose.Core.Interfaces;

namespace Prose.Core.Services.Audit;

/// <summary>
/// Adjudicates the ledger's SAME-PREDICATE contradiction groups against the prose they came from —
/// the half of contradiction detection that has always produced candidates and never produced a
/// judgement.
///
/// <para><b>The gap.</b> <c>ContinuityService.Upsert</c> flags a pair whenever one
/// <c>(entity, predicate)</c> key holds two different object strings, and nothing has ever asked
/// whether they actually disagree. Three mechanical exemptions now strip the classes that were
/// never contradictions at all — volatile predicates, set-valued predicates, and paraphrase — and
/// those cleared 964 of 3,776 rows corpus-wide. What survives still is not a defect list: reading
/// a sample showed it is dominated by <b>complementary facets</b> (<c>augmentation_type</c>:
/// "four-armed combat specialist" against "quadrupedal with four arms") and <b>temporal states</b>
/// (<c>beacon_status</c>: "live" against "dormant for 3 months, recovered"). Telling those from a
/// genuine conflict is a judgement about the story, and no string comparison loose enough to catch
/// them is tight enough to be safe — a rule that merges complementary values is a rule that hides
/// the next fabricated fact.</para>
///
/// <para><b>So the discrimination is bought, not guessed.</b> One narrow LLM call per group, with
/// the actual prose each value was read from in front of it, and the same mechanical quote gate the
/// rest of the Story Ledger uses: a verdict that cannot quote the text is discarded. This is only
/// affordable because the anchor backfill took prose-claim beat coverage from 0.1% to 99.1% — an
/// unanchored claim has no prose to show, and the question could not have been asked at all.</para>
///
/// <para><b>What it writes.</b> Claim STATUS only, never prose (docs/LOGIC.md §4). A group judged
/// compatible has its members moved out of <c>CONTRADICTED</c> back to <c>NEW</c> — the claims were
/// never wrong, only the verdict about them. A group judged to be a real conflict keeps its status
/// and gains a finding carrying the adjudicator's quoted evidence. <c>CANONICAL</c> is never
/// demoted: a fact the author has settled stays settled until a human re-resolves it.</para>
/// </summary>
public sealed class ClaimGroupAdjudicationService(
    IDbContextFactory<ProseDbContext> dbFactory,
    ContinuityService store,
    NodeWorkbenchService workbench,
    FindingsService findings,
    ILlmService llm,
    ILogger<ClaimGroupAdjudicationService> log)
{
    /// <summary>Prefix on every finding this service files, so the delete-then-recreate lifecycle
    /// and staleness reporting work unchanged and these never collide with TUNEDREAD's.</summary>
    public const string SummaryPrefix = "LEDGER-CONFLICT ";

    /// <summary>Beats either side of each anchor. Far tighter than the Tuned Read's ±10: the
    /// question here is whether two stated values of ONE named fact can both be true, which the
    /// sentence and its immediate neighbours answer. ±10 per anchor across three values would be
    /// ~60 beats of prose per call, most of it never read.</summary>
    private const int CarrierRadius = 2;

    /// <summary>At most this many distinct values shown per group. A key with more than this is
    /// nearly always a set-valued predicate that slipped the exemption rather than a genuine
    /// N-way disagreement, and paying to render all of them buys nothing.</summary>
    private const int MaxValuesPerGroup = 4;

    private const string Model = "claude-sonnet-5";

    public sealed record Options(bool Apply = true, int MaxGroups = 400);

    public sealed record GroupVerdict(
        string EntityName, string Predicate, string[] Values,
        bool IsConflict, string Severity, string Note, string Quote, int[] BeatNumbers);

    public sealed record Report(
        Guid NodeId, string Slug, string Title,
        int Groups, int Adjudicated, int CacheHits, int Conflicts, int Compatible,
        int GroundingRejected, int Unanchored, int ClaimsCleared,
        List<GroupVerdict> Conflicting, List<string> Notes);

    private const string System = """
You are judging ONE question about a story's internal consistency, and nothing else.

A story ledger recorded several values for the SAME named fact about the SAME entity. You are
shown each value and the prose it was read from. Decide whether they genuinely CONTRADICT.

The prose may refer to the entity by a callsign or nickname rather than the name in the ledger.
If an ALSO CALLED line is given, those names are the SAME PERSON — a passage that assigns
something to "Stash" is assigning it to Jude Adeyemi. Never treat a different name for the same
entity as evidence the fact belongs to somebody else.

Answer NO (not a contradiction) when:
- They are COMPLEMENTARY — different partial descriptions of one thing that fit together
  ("four-armed combat specialist" and "quadrupedal with four arms"; "red hair in a loose braid"
  and "dark red hair"). Two angles on one fact is not a disagreement.
- They are TEMPORAL — true at different points, with the story showing the change between them
  ("live" then "dormant for three months, recovered"). A thing changing is a story, not an error.
- They are the SAME assertion in different words, or one restates the other with more detail.
- One is what a character believes, says, lies about, or guesses, rather than what is true.

Answer YES only when the story asserts both as literally true of the same entity at the same
time, and they cannot both hold — a number that is two different numbers, a person in two places
at one moment, a fact and its negation.

You MUST cite a verbatim quote copied EXACTLY from the prose you were given, demonstrating the
conflict. A verdict whose quote is not found in that prose is discarded. Do not quote the value
summaries; quote the prose.

Output STRICT JSON, no fences, no commentary:
{"contradiction": true|false, "severity": "BLOCKER"|"MODERATE"|"MINOR", "quote": "verbatim span", "note": "one sentence naming what conflicts, or why they are compatible"}

NO is a correct and common answer. Most of what you are shown will be complementary or temporal.
""";

    public async Task<Report> RunAsync(Guid bookNodeId, Options? options = null, CancellationToken ct = default)
    {
        var opts = options ?? new Options();
        var notes = new List<string>();

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var book = await db.Nodes.IgnoreQueryFilters().AsNoTracking()
            .Where(n => n.Id == bookNodeId)
            .Select(n => new { n.Id, n.Slug, n.Title })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Node {bookNodeId} not found.");

        // GetContradictionGroups already applies the volatile/set-valued/paraphrase exemptions,
        // so this only ever pays for groups the deterministic layer could not settle.
        var groups = store.GetContradictionGroups(book.Slug);
        var ordered = await workbench.GetOrderedBeatsAsync(bookNodeId, ct);
        var beatOrder = ordered.Select(o => o.Beat).ToList();
        var beatById = beatOrder.ToDictionary(b => b.Id, b => b);

        if (groups.Count > opts.MaxGroups)
        {
            notes.Add($"{groups.Count} group(s) found; adjudicating the first {opts.MaxGroups}. " +
                      "Re-run to continue — verdicts are cached, so nothing already judged is re-billed.");
            groups = groups.Take(opts.MaxGroups).ToList();
        }

        int adjudicated = 0, cacheHits = 0, conflicts = 0, compatible = 0, rejected = 0,
            unanchored = 0, cleared = 0;
        var conflicting = new List<GroupVerdict>();

        foreach (var g in groups)
        {
            ct.ThrowIfCancellationRequested();

            // One representative claim per distinct assertion — the group can hold a dozen rows
            // expressing three values, and the question is about the values.
            var distinct = new List<ContinuityClaim>();
            foreach (var c in g.Claims)
                if (!distinct.Any(d => ContinuityService.ObjectsSayTheSameThing(d.Object, c.Object)))
                    distinct.Add(c);
            distinct = distinct.Take(MaxValuesPerGroup).ToList();
            if (distinct.Count < 2) continue;

            var anchors = distinct
                .Select(c => c.SourceBeatId.HasValue ? beatById.GetValueOrDefault(c.SourceBeatId.Value) : null)
                .ToList();

            if (anchors.All(a => a == null))
            {
                // No prose to judge against. Ruling anyway would be ruling on two summaries —
                // the paraphrase-only reasoning that produced the defect this system exists for.
                unanchored++;
                continue;
            }

            var aliases = await LoadAliasesAsync(db, g.EntityId, g.EntityName, ct);
            var cacheKey = ComputeCacheKey(distinct, anchors, aliases);
            var cached = await db.TunedReadAdjudications.AsNoTracking()
                .FirstOrDefaultAsync(a => a.CacheKey == cacheKey, ct);

            TunedReadAdjudication verdict;
            if (cached != null) { verdict = cached; cacheHits++; }
            else
            {
                verdict = await AdjudicateAsync(g, distinct, anchors, beatOrder, book.Slug, cacheKey, aliases, ct);
                adjudicated++;
                db.TunedReadAdjudications.Add(verdict);
                try { await db.SaveChangesAsync(ct); }
                catch (DbUpdateException ex)
                {
                    log.LogDebug(ex, "[ledger-adjudicate] verdict cache collision on {Key}.", cacheKey);
                    db.ChangeTracker.Clear();
                }
            }

            if (!string.IsNullOrEmpty(verdict.RejectedReason)) rejected++;

            if (verdict.IsContradiction)
            {
                conflicts++;
                conflicting.Add(new GroupVerdict(
                    g.EntityName, g.Predicate, distinct.Select(c => c.Object).ToArray(),
                    true, verdict.Severity ?? "MODERATE", verdict.Note ?? "", verdict.EvidenceQuote ?? "",
                    anchors.Where(a => a != null).Select(a => a!.Number).ToArray()));
            }
            else
            {
                compatible++;
                // A verdict that could not be reached (call failed, unparseable, ungrounded) is
                // NOT evidence of compatibility — leave those rows exactly as they are.
                if (opts.Apply && string.IsNullOrEmpty(verdict.RejectedReason))
                    cleared += await ClearGroupAsync(db, g, verdict.Note ?? "", ct);
            }
        }

        if (opts.Apply)
        {
            findings.DeleteBySummaryPrefix($"node:{book.Slug}", SummaryPrefix);
            foreach (var v in conflicting) FileFinding(book.Slug, v);
        }

        if (unanchored > 0)
            notes.Add($"{unanchored} group(s) skipped: no member carries a beat anchor, so there is no prose " +
                      "to judge them against. Run prose --continuity anchor-beats first.");

        return new Report(book.Id, book.Slug ?? "", book.Title,
            groups.Count, adjudicated, cacheHits, conflicts, compatible, rejected, unanchored, cleared,
            conflicting, notes);
    }

    /// <summary>
    /// Every name the prose might use for this entity: its own, plus its character aliases.
    ///
    /// <para><b>Without this the adjudicator invents contradictions.</b> Found live 2026-09-04 on
    /// the first finding triaged: the ledger records <c>Jude Adeyemi → role_on_crew → "gear
    /// knowledge"</c> and the prose says "Stash knew the gear" — so the model, shown only the
    /// formal name, concluded the prose assigned that role to somebody else and filed a High
    /// finding. The entity is <c>Dr. Jude "Stash" Adeyemi</c>; claim and prose agree completely.
    /// In GLMZ most of the cast is referred to by callsign (Pixel, Shroud, Sift, Boost, Doc
    /// Stash), so an adjudicator that cannot connect the two names is wrong about a whole class
    /// of entity rather than an unlucky one.</para>
    /// </summary>
    private static async Task<List<string>> LoadAliasesAsync(
        ProseDbContext db, string entityId, string entityName, CancellationToken ct)
    {
        if (!Guid.TryParse(entityId, out var id)) return [];
        var aliases = await db.CharacterAliases.AsNoTracking()
            .Where(a => a.CharacterId == id)
            .OrderBy(a => a.Position)
            .Select(a => a.Value)
            .ToListAsync(ct);

        // The canonical Entity.Name too: a claim's EntityName is a copy taken at extraction time
        // and can be a stale or shortened form of what the entity is actually called now.
        var canonical = await db.Entities.IgnoreQueryFilters().AsNoTracking()
            .Where(e => e.Id == id).Select(e => e.Name).FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(canonical)) aliases.Add(canonical);

        return aliases
            .Where(a => !string.IsNullOrWhiteSpace(a)
                     && !string.Equals(a, entityName, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(a => a, StringComparer.Ordinal)   // stable, so the cache key is stable
            .ToList();
    }

    private async Task<TunedReadAdjudication> AdjudicateAsync(
        ContradictionGroup g, List<ContinuityClaim> distinct, List<Beat?> anchors,
        List<Beat> beatOrder, string? bookSlug, string cacheKey, List<string> aliases,
        CancellationToken ct)
    {
        var row = new TunedReadAdjudication
        {
            CacheKey = cacheKey,
            ClaimAUid = distinct[0].ClaimUid,
            ClaimBUid = distinct[1].ClaimUid,
            ExclusionRuleId = null, // same-predicate origin, per this entity's own remarks
            BookSlug = bookSlug ?? "",
        };

        var user = new StringBuilder();
        user.AppendLine($"ENTITY: {g.EntityName}");
        if (aliases.Count > 0)
            user.AppendLine($"ALSO CALLED (the prose usually uses these): {string.Join(", ", aliases)}");
        user.AppendLine($"FACT: {g.Predicate}");
        user.AppendLine();
        user.AppendLine("VALUES RECORDED FOR THIS FACT:");
        for (var i = 0; i < distinct.Count; i++)
        {
            var beat = anchors[i];
            user.AppendLine($"  [{i + 1}] {distinct[i].Object}"
                + (beat != null ? $"   (beat #{beat.Number})" : "   (no beat recorded)"));
        }
        user.AppendLine();

        var carriers = new List<string>();
        for (var i = 0; i < distinct.Count; i++)
        {
            var c = TunedReadService.BuildCarrier(anchors[i], beatOrder, CarrierRadius);
            if (c.Length == 0) continue;
            carriers.Add(c);
            user.AppendLine($"PROSE BEHIND VALUE [{i + 1}] (verbatim — quote only from these blocks):");
            user.AppendLine(c);
            user.AppendLine();
        }

        string raw;
        try
        {
            raw = await llm.GenerateAsync(System, user.ToString(),
                temperature: 0.1, maxTokens: 500, model: Model, ct: ct);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "[ledger-adjudicate] call failed for {Entity}.{Predicate}", g.EntityName, g.Predicate);
            row.IsContradiction = false;
            row.RejectedReason = "adjudication call failed: " + ex.Message;
            return row;
        }

        if (!TunedReadService.TryParseVerdict(raw, out var isConflict, out var severity, out var quote, out var note))
        {
            row.IsContradiction = false;
            row.RejectedReason = "adjudicator response was not parseable JSON";
            return row;
        }

        row.Severity = severity;
        row.EvidenceQuote = quote;
        row.Note = note;

        if (!isConflict) { row.IsContradiction = false; return row; }

        // Same mechanical gate as everywhere else: an unquotable assertion about the text is
        // exactly the failure mode this whole system was built to stop committing.
        if (!carriers.Any(c => TunedReadService.QuoteAppearsIn(quote, c)))
        {
            row.IsContradiction = false;
            row.RejectedReason = "cited quote does not appear verbatim in the prose supplied — verdict discarded";
            log.LogInformation("[ledger-adjudicate] Discarded ungrounded verdict for {Entity}.{Predicate}.",
                g.EntityName, g.Predicate);
            return row;
        }

        row.IsContradiction = true;
        return row;
    }

    /// <summary>Moves a compatible group's members out of CONTRADICTED. CANONICAL is left alone —
    /// the same rule <c>ContinuityService.Upsert</c> follows.</summary>
    private static async Task<int> ClearGroupAsync(
        ProseDbContext db, ContradictionGroup g, string note, CancellationToken ct)
    {
        var uids = g.Claims.Select(c => c.ClaimUid).ToHashSet(StringComparer.Ordinal);
        var rows = await db.ContinuityClaims
            .Where(c => uids.Contains(c.ClaimUid) && c.Status == "CONTRADICTED")
            .ToListAsync(ct);
        foreach (var r in rows)
        {
            r.Status = "NEW";
            r.ResolutionNote = "Adjudicated against the prose and found compatible: " + Clip(note, 400);
        }
        if (rows.Count > 0) await db.SaveChangesAsync(ct);
        return rows.Count;
    }

    private void FileFinding(string? bookSlug, GroupVerdict v)
    {
        var severity = v.Severity switch
        {
            "BLOCKER" => FindingSeverity.High,
            "MINOR" => FindingSeverity.Low,
            _ => FindingSeverity.Medium,
        };
        var where = v.BeatNumbers.Length > 0 ? $" (beats {string.Join(", ", v.BeatNumbers.Select(n => "#" + n))})" : "";

        findings.Upsert(
            filePath: $"node:{bookSlug}",
            chapterId: null,
            category: FindingCategory.Contradiction,
            severity: severity,
            summary: $"{SummaryPrefix}[{v.EntityName}] {v.Predicate}: "
                   + string.Join(" vs ", v.Values.Select(x => $"\"{Clip(x, 60)}\"")) + $"{where}: {Clip(v.Note, 300)}",
            // No Snippet, deliberately — docs/LOGIC.md §4 and the no-bulk-rewriter rule: without a
            // Snippet/SuggestedFix pair no apply path can splice a machine "fix" over prose.
            snippet: null,
            suggestedFix: $"Evidence: \"{Clip(v.Quote, 300)}\". Decide which value is load-bearing and fix that "
                        + "one beat by hand, or resolve the claim (prose --continuity resolve). Do not rewrite "
                        + "prose to satisfy the ledger.");
    }

    /// <summary>
    /// Keyed on the claim uids, every anchor beat's CURRENT text, and the entity's alias set.
    ///
    /// <para>Aliases belong in the key for the same reason the anchor text does: they change what
    /// the model was asked, so a verdict reached without them is stale. Including them rather than
    /// bumping a blanket prompt-version constant is deliberate and saves real money — an entity
    /// with no aliases produces a byte-identical key, so its cached verdict survives and only the
    /// entities the alias fix actually affects get re-billed.</para>
    /// </summary>
    private static string ComputeCacheKey(
        List<ContinuityClaim> claims, List<Beat?> anchors, List<string> aliases)
    {
        var raw = string.Join("|", claims.Select(c => c.ClaimUid))
                + "||" + string.Join("|", anchors.Select(a => a?.TextHash ?? "-"))
                + (aliases.Count > 0 ? "||aka:" + string.Join(",", aliases) : "");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant()[..40];
    }

    private static string Clip(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..(max - 1)] + "…";
}
